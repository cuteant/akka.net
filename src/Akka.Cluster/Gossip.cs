﻿//-----------------------------------------------------------------------
// <copyright file="Gossip.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Remote;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Cluster
{
    /// <summary>
    /// Represents the state of the cluster; cluster ring membership, ring convergence -
    /// all versioned by a vector clock.
    ///
    /// When a node is joining the `Member`, with status `Joining`, is added to `members`.
    /// If the joining node was downed it is moved from `overview.unreachable` (status `Down`)
    /// to `members` (status `Joining`). It cannot rejoin if not first downed.
    ///
    /// When convergence is reached the leader change status of `members` from `Joining`
    /// to `Up`.
    ///
    /// When failure detector consider a node as unavailable it will be moved from
    /// `members` to `overview.unreachable`.
    ///
    /// When a node is downed, either manually or automatically, its status is changed to `Down`.
    /// It is also removed from `overview.seen` table. The node will reside as `Down` in the
    /// `overview.unreachable` set until joining again and it will then go through the normal
    /// joining procedure.
    ///
    /// When a `Gossip` is received the version (vector clock) is used to determine if the
    /// received `Gossip` is newer or older than the current local `Gossip`. The received `Gossip`
    /// and local `Gossip` is merged in case of conflicting version, i.e. vector clocks without
    /// same history.
    ///
    /// When a node is told by the user to leave the cluster the leader will move it to `Leaving`
    /// and then rebalance and repartition the cluster and start hand-off by migrating the actors
    /// from the leaving node to the new partitions. Once this process is complete the leader will
    /// move the node to the `Exiting` state and once a convergence is complete move the node to
    /// `Removed` by removing it from the `members` set and sending a `Removed` command to the
    /// removed node telling it to shut itself down.
    /// </summary>
    [MessagePackObject]
    internal sealed class Gossip
    {
        /// <summary>
        /// An empty set of members
        /// </summary>
        public static readonly ImmutableSortedSet<Member> EmptyMembers = ImmutableSortedSet<Member>.Empty;

        /// <summary>
        /// An empty <see cref="Gossip"/> object.
        /// </summary>
        public static readonly Gossip Empty = new Gossip(EmptyMembers);

        /// <summary>
        /// Creates a new <see cref="Gossip"/> from the given set of members.
        /// </summary>
        /// <param name="members">The current membership of the cluster.</param>
        /// <returns>A gossip object for the given members.</returns>
        public static Gossip Create(ImmutableSortedSet<Member> members)
        {
            if (members.IsEmpty) return Empty;
            return Empty.Copy(members: members);
        }

        private static readonly ImmutableHashSet<MemberStatus> LeaderMemberStatus =
            ImmutableHashSet.Create(MemberStatus.Up, MemberStatus.Leaving);

        private static readonly ImmutableHashSet<MemberStatus> ConvergenceMemberStatus =
            ImmutableHashSet.Create(MemberStatus.Up, MemberStatus.Leaving);

        /// <summary>
        /// If there are unreachable members in the cluster with any of these statuses, they will be skipped during convergence checks.
        /// </summary>
        public static readonly ImmutableHashSet<MemberStatus> ConvergenceSkipUnreachableWithMemberStatus =
            ImmutableHashSet.Create(MemberStatus.Down, MemberStatus.Exiting);

        /// <summary>
        /// If there are unreachable members in the cluster with any of these statuses, they will be pruned from the local gossip
        /// </summary>
        public static readonly ImmutableHashSet<MemberStatus> RemoveUnreachableWithMemberStatus =
            ImmutableHashSet.Create(MemberStatus.Down, MemberStatus.Exiting);

        /// <summary>
        /// The current members of the cluster
        /// </summary>
        [Key(0)]
        public readonly ImmutableSortedSet<Member> Members;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly GossipOverview Overview;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public readonly VectorClock Version;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="members">TBD</param>
        public Gossip(ImmutableSortedSet<Member> members) : this(members, new GossipOverview(), VectorClock.Create()) { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="members">TBD</param>
        /// <param name="overview">TBD</param>
        public Gossip(ImmutableSortedSet<Member> members, GossipOverview overview) : this(members, overview, VectorClock.Create()) { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="members">TBD</param>
        /// <param name="overview">TBD</param>
        /// <param name="version">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        [SerializationConstructor]
        public Gossip(ImmutableSortedSet<Member> members, GossipOverview overview, VectorClock version)
        {
            Members = members;
            Overview = overview;
            Version = version;

            _membersMap = new Lazy<ImmutableDictionary<UniqueAddress, Member>>(
                () => members.ToImmutableDictionary(m => m.UniqueAddress, m => m, UniqueAddressComparer.Instance));

            ReachabilityExcludingDownedObservers = new Lazy<Reachability>(() =>
            {
                var downed = Members.Where(m => m.Status == MemberStatus.Down).ToList();
                return Overview.Reachability.RemoveObservers(downed.Select(m => m.UniqueAddress).ToImmutableHashSet(UniqueAddressComparer.Instance));
            });

            if (Cluster.IsAssertInvariantsEnabled) AssertInvariants();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="members">TBD</param>
        /// <param name="overview">TBD</param>
        /// <param name="version">TBD</param>
        /// <returns>TBD</returns>
        public Gossip Copy(ImmutableSortedSet<Member> members = null, GossipOverview overview = null,
            VectorClock version = null)
        {
            return new Gossip(members ?? Members, overview ?? Overview, version ?? Version);
        }

        private void AssertInvariants()
        {
            if (Members.Any(m => m.Status == MemberStatus.Removed))
            {
                ThrowHelper.ThrowArgumentException_ExpectedRemoveStatus(Members);
            }

            var inReachabilityButNotMember = Overview.Reachability.AllObservers.Except(Members.Select(m => m.UniqueAddress));
            if (!inReachabilityButNotMember.IsEmpty)
            {
                ThrowHelper.ThrowArgumentException_NodesNotPartOfClusterInReachabilityTable(inReachabilityButNotMember);
            }

            var seenButNotMember = Overview.Seen.Except(Members.Select(m => m.UniqueAddress));
            if (!seenButNotMember.IsEmpty)
            {
                ThrowHelper.ThrowArgumentException_NodesNotPartOfClusterHaveMarkedTheGossipAsSeen(seenButNotMember);
            }
        }

        [IgnoreMember]
        Lazy<ImmutableDictionary<UniqueAddress, Member>> _membersMap;

        /// <summary>
        /// Increments the version for this 'Node'.
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public Gossip Increment(VectorClock.Node node)
        {
            return Copy(version: Version.Increment(node));
        }

        /// <summary>
        /// Adds a member to the member node ring.
        /// </summary>
        /// <param name="member">TBD</param>
        /// <returns>TBD</returns>
        public Gossip AddMember(Member member)
        {
            if (Members.Contains(member)) return this;
            return Copy(members: Members.Add(member));
        }

        /// <summary>
        /// Marks the gossip as seen by this node (address) by updating the address entry in the 'gossip.overview.seen'
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public Gossip Seen(UniqueAddress node)
        {
            if (SeenByNode(node)) return this;
            return Copy(overview: Overview.Copy(seen: Overview.Seen.Add(node)));
        }

        /// <summary>
        /// Marks the gossip as seen by only this node (address) by replacing the 'gossip.overview.seen'
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public Gossip OnlySeen(UniqueAddress node)
        {
            return Copy(overview: Overview.Copy(seen: ImmutableHashSet.Create(UniqueAddressComparer.Instance, node)));
        }

        /// <summary>
        /// Removes all seen entries from the gossip.
        /// </summary>
        /// <returns>A copy of the current gossip with no seen entries.</returns>
        public Gossip ClearSeen()
        {
            return Copy(overview: Overview.Copy(seen: ImmutableHashSet<UniqueAddress>.Empty));
        }

        /// <summary>
        /// The nodes that have seen the current version of the Gossip.
        /// </summary>
        [IgnoreMember]
        public ImmutableHashSet<UniqueAddress> SeenBy
        {
            get { return Overview.Seen; }
        }

        /// <summary>
        /// Has this Gossip been seen by this node.
        /// </summary>
        /// <param name="node">The unique address of the node.</param>
        /// <returns><c>true</c> if this gossip has been seen by the given node, <c>false</c> otherwise.</returns>
        public bool SeenByNode(UniqueAddress node)
        {
            return Overview.Seen.Contains(node);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="that">TBD</param>
        /// <returns>TBD</returns>
        public Gossip MergeSeen(Gossip that)
        {
            return Copy(overview: Overview.Copy(seen: Overview.Seen.Union(that.Overview.Seen)));
        }

        /// <summary>
        /// Merges two <see cref="Gossip"/> objects together into a consistent view of the <see cref="Cluster"/>.
        /// </summary>
        /// <param name="that">The other gossip object to be merged.</param>
        /// <returns>A combined gossip object that uses the underlying <see cref="VectorClock"/> to determine which items are newest.</returns>
        public Gossip Merge(Gossip that)
        {
            //TODO: Member ordering import?
            // 1. merge vector clocks
            var mergedVClock = Version.Merge(that.Version);

            // 2. merge members by selecting the single Member with highest MemberStatus out of the Member groups
            var mergedMembers = EmptyMembers.Union(Member.PickHighestPriority(this.Members, that.Members));

            // 3. merge reachability table by picking records with highest version
            var mergedReachability = this.Overview.Reachability.Merge(mergedMembers.Select(m => m.UniqueAddress),
                that.Overview.Reachability);

            // 4. Nobody can have seen this new gossip yet
            var mergedSeen = ImmutableHashSet<UniqueAddress>.Empty;

            return new Gossip(mergedMembers, new GossipOverview(mergedSeen, mergedReachability), mergedVClock);
        }


        /// <summary>
        /// First check that:
        ///   1. we don't have any members that are unreachable, or
        ///   2. all unreachable members in the set have status DOWN or EXITING
        /// Else we can't continue to check for convergence. When that is done 
        /// we check that all members with a convergence status is in the seen 
        /// table and has the latest vector clock version.
        /// </summary>
        /// <param name="selfUniqueAddress">The unique address of the node checking for convergence.</param>
        /// <param name="exitingConfirmed">The set of nodes who have been confirmed to be exiting.</param>
        /// <returns><c>true</c> if convergence has been achieved. <c>false</c> otherwise.</returns>
        public bool Convergence(UniqueAddress selfUniqueAddress, HashSet<UniqueAddress> exitingConfirmed)
        {
            var unreachable = ReachabilityExcludingDownedObservers.Value.AllUnreachableOrTerminated
                .Where(node => node != selfUniqueAddress && !exitingConfirmed.Contains(node))
                .Select(GetMember);

            return unreachable.All(m => ConvergenceSkipUnreachableWithMemberStatus.Contains(m.Status))
                && !Members.Any(m => ConvergenceMemberStatus.Contains(m.Status)
                && !(SeenByNode(m.UniqueAddress) || exitingConfirmed.Contains(m.UniqueAddress)));
        }

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public Lazy<Reachability> ReachabilityExcludingDownedObservers { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <param name="selfUniqueAddress">TBD</param>
        /// <returns>TBD</returns>
        public bool IsLeader(UniqueAddress node, UniqueAddress selfUniqueAddress)
        {
            return Leader(selfUniqueAddress) == node && node is object;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="selfUniqueAddress">TBD</param>
        /// <returns>TBD</returns>
        public UniqueAddress Leader(UniqueAddress selfUniqueAddress)
        {
            return LeaderOf(Members, selfUniqueAddress);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="role">TBD</param>
        /// <param name="selfUniqueAddress">TBD</param>
        /// <returns>TBD</returns>
        public UniqueAddress RoleLeader(string role, UniqueAddress selfUniqueAddress)
        {
            var roleMembers = Members
                .Where(m => m.HasRole(role))
                .ToImmutableSortedSet();

            return LeaderOf(roleMembers, selfUniqueAddress);
        }

        /// <summary>
        /// Determine which node is the leader of the given range of members.
        /// </summary>
        /// <param name="mbrs">All members in the cluster.</param>
        /// <param name="selfUniqueAddress">The address of the current node.</param>
        /// <returns><c>null</c> if <paramref name="mbrs"/> is empty. The <see cref="UniqueAddress"/> of the leader otherwise.</returns>
        public UniqueAddress LeaderOf(ImmutableSortedSet<Member> mbrs, UniqueAddress selfUniqueAddress)
        {
            var reachableMembers = (Overview.Reachability.IsAllReachable
                ? mbrs.Where(m => m.Status != MemberStatus.Down)
                : mbrs
                    .Where(m => m.Status != MemberStatus.Down && Overview.Reachability.IsReachable(m.UniqueAddress) || m.UniqueAddress == selfUniqueAddress))
                    .ToImmutableSortedSet();

            if (reachableMembers.Count <= 0) return null;

            var member = reachableMembers.FirstOrDefault(m => LeaderMemberStatus.Contains(m.Status)) ??
                         reachableMembers.Min(Member.LeaderStatusOrdering);

            return member.UniqueAddress;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public ImmutableHashSet<string> AllRoles
        {
            get { return Members.SelectMany(m => m.Roles).ToImmutableHashSet(StringComparer.Ordinal); }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public bool IsSingletonCluster
        {
            get { return Members.Count == 1; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public Member GetMember(UniqueAddress node)
        {
            return _membersMap.Value.GetOrElse(node,
                Member.Removed(node)); // placeholder for removed member
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public bool HasMember(UniqueAddress node)
        {
            return _membersMap.Value.ContainsKey(node);
        }


        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="Exception">
        /// This exception is thrown when there are no members in the cluster.
        /// </exception>
        [IgnoreMember]
        public Member YoungestMember
        {
            get
            {
                //TODO: Akka exception?
                if (Members.Count <= 0) ThrowHelper.ThrowException_NoYoungestWhenNoMembers();
                return Members.MaxBy(m => m.UpNumber == int.MaxValue ? 0 : m.UpNumber);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="removedNode">TBD</param>
        /// <returns>TBD</returns>
        public Gossip Prune(VectorClock.Node removedNode)
        {
            var newVersion = Version.Prune(removedNode);
            if (newVersion.Equals(Version))
                return this;
            else
                return new Gossip(Members, Overview, newVersion);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var members = string.Join(", ", Members.Select(m => m.ToString()));
            return $"Gossip(members = [{members}], overview = {Overview}, version = {Version}";
        }
    }

    /// <summary>
    /// Represents the overview of the cluster, holds the cluster convergence table and set with unreachable nodes.
    /// </summary>
    [MessagePackObject]
    class GossipOverview
    {
        /// <summary>
        /// TBD
        /// </summary>
        public GossipOverview() : this(ImmutableHashSet<UniqueAddress>.Empty, Reachability.Empty) { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="reachability">TBD</param>
        public GossipOverview(Reachability reachability) : this(ImmutableHashSet<UniqueAddress>.Empty, reachability) { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="seen">TBD</param>
        /// <param name="reachability">TBD</param>
        [SerializationConstructor]
        public GossipOverview(ImmutableHashSet<UniqueAddress> seen, Reachability reachability)
        {
            Seen = seen;
            Reachability = reachability;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="seen">TBD</param>
        /// <param name="reachability">TBD</param>
        /// <returns>TBD</returns>
        public GossipOverview Copy(ImmutableHashSet<UniqueAddress> seen = null, Reachability reachability = null)
        {
            return new GossipOverview(seen ?? Seen, reachability ?? Reachability);
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly ImmutableHashSet<UniqueAddress> Seen;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly Reachability Reachability;

        /// <inheritdoc/>
        public override string ToString() => $"GossipOverview(seen=[{string.Join(", ", Seen)}], reachability={Reachability})";
    }

    /// <summary>
    /// Envelope adding a sender and receiver address to the gossip.
    /// The reason for including the receiver address is to be able to
    /// ignore messages that were intended for a previous incarnation of
    /// the node with same host:port. The `uid` in the `UniqueAddress` is
    /// different in that case.
    /// </summary>
    [MessagePackObject]
    class GossipEnvelope : IClusterMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="from">TBD</param>
        /// <param name="to">TBD</param>
        /// <param name="gossip">TBD</param>
        /// <param name="deadline">TBD</param>
        /// <returns>TBD</returns>
        [SerializationConstructor]
        public GossipEnvelope(UniqueAddress from, UniqueAddress to, Gossip gossip, Deadline deadline = null)
        {
            From = from;
            To = to;
            Gossip = gossip;
            Deadline = deadline;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public UniqueAddress From { get; }
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public UniqueAddress To { get; }
        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public Gossip Gossip { get; set; }
        /// <summary>
        /// TBD
        /// </summary>
        [Key(3)]
        public Deadline Deadline { get; set; }
    }

    /// <summary>
    /// When there are no known changes to the node ring a `GossipStatus`
    /// initiates a gossip chat between two members. If the receiver has a newer
    /// version it replies with a `GossipEnvelope`. If receiver has older version
    /// it replies with its `GossipStatus`. Same versions ends the chat immediately.
    /// </summary>
    [MessagePackObject]
    class GossipStatus : IClusterMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly UniqueAddress From;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly VectorClock Version;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="from">TBD</param>
        /// <param name="version">TBD</param>
        [SerializationConstructor]
        public GossipStatus(UniqueAddress from, VectorClock version)
        {
            From = from;
            Version = version;
        }

        /// <inheritdoc/>
        protected bool Equals(GossipStatus other)
        {
            return From.Equals(other.From) && Version.IsSameAs(other.Version);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GossipStatus)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (From.GetHashCode() * 397) ^ Version.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"GossipStatus(from={From}, version={Version})";
    }
}
