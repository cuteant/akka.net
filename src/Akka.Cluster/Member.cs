﻿//-----------------------------------------------------------------------
// <copyright file="Member.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Cluster
{
    /// <summary>
    /// Represents the address, current status, and roles of a cluster member node.
    /// </summary>
    /// <remarks>
    /// NOTE: <see cref="GetHashCode"/> and <see cref="Equals(object)"/> are solely based on the underlying <see cref="Address"/>,
    /// not its <see cref="MemberStatus"/> and roles.
    /// </remarks>
    [MessagePackObject]
    public class Member : IComparable<Member>, IComparable, IEquatable<Member>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="uniqueAddress">TBD</param>
        /// <param name="roles">TBD</param>
        /// <returns>TBD</returns>
        internal static Member Create(UniqueAddress uniqueAddress, ImmutableHashSet<string> roles)
        {
            return new Member(uniqueAddress, int.MaxValue, MemberStatus.Joining, roles);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        internal static Member Removed(UniqueAddress node)
        {
            return new Member(node, int.MaxValue, MemberStatus.Removed, ImmutableHashSet<string>.Empty);
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly UniqueAddress UniqueAddress;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        internal readonly int UpNumber;

        /// <summary>
        /// The status of the current member.
        /// </summary>
        [Key(2)]
        public readonly MemberStatus Status;

        /// <summary>
        /// The set of roles for the current member. Can be empty.
        /// </summary>
        [Key(3)]
        public readonly ImmutableHashSet<string> Roles;

        /// <summary>
        /// Creates a new <see cref="Member"/>.
        /// </summary>
        /// <param name="uniqueAddress">The address of the member.</param>
        /// <param name="upNumber">The upNumber of the member, as assigned by the leader at the time the node joined the cluster.</param>
        /// <param name="status">The status of this member.</param>
        /// <param name="roles">The roles for this member. Can be empty.</param>
        /// <returns>A new member instance.</returns>
        internal static Member Create(UniqueAddress uniqueAddress, int upNumber, MemberStatus status, ImmutableHashSet<string> roles)
        {
            return new Member(uniqueAddress, upNumber, status, roles);
        }

        /// <summary>
        /// Creates a new <see cref="Member"/>.
        /// </summary>
        /// <param name="uniqueAddress">The address of the member.</param>
        /// <param name="upNumber">The upNumber of the member, as assigned by the leader at the time the node joined the cluster.</param>
        /// <param name="status">The status of this member.</param>
        /// <param name="roles">The roles for this member. Can be empty.</param>
        [SerializationConstructor]
        public Member(UniqueAddress uniqueAddress, int upNumber, MemberStatus status, ImmutableHashSet<string> roles)
        {
            UniqueAddress = uniqueAddress;
            UpNumber = upNumber;
            Status = status;
            Roles = roles;
        }

        /// <summary>
        /// The <see cref="Address"/> for this member.
        /// </summary>
        [IgnoreMember]
        public Address Address { get { return UniqueAddress.Address; } }

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            return UniqueAddress.GetHashCode();
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (obj is Member m) { return UniqueAddress.Equals(m.UniqueAddress); }
            return false;
        }

        public bool Equals(Member other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return UniqueAddress.Equals(other.UniqueAddress);
        }

        /// <inheritdoc cref="IComparable.CompareTo"/>
        public int CompareTo(Member other) => Ordering.Compare(this, other);

        int IComparable.CompareTo(object obj)
        {
            if (obj is Member member) return CompareTo(member);

            return ThrowHelper.ThrowArgumentException_MemberCompare(obj);
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return $"Member(address = {Address}, Uid={UniqueAddress.Uid} status = {Status}, role=[{string.Join(",", Roles)}], upNumber={UpNumber})";
        }

        /// <summary>
        /// Checks to see if a member supports a particular role.
        /// </summary>
        /// <param name="role">The rolename to check.</param>
        /// <returns><c>true</c> if this member supports the role. <c>false</c> otherwise.</returns>
        public bool HasRole(string role)
        {
            return Roles.Contains(role);
        }

        /// <summary>
        /// Is this member older, has been part of cluster longer, than another
        /// member. It is only correct when comparing two existing members in a
        /// cluster. A member that joined after removal of another member may be
        /// considered older than the removed member.
        /// </summary>
        /// <param name="other">The other member to check.</param>
        /// <returns><c>true</c> if this member is older than the other member. <c>false</c> otherwise.</returns>
        public bool IsOlderThan(Member other)
        {
            if (UpNumber.Equals(other.UpNumber))
            {
                return Member.AddressOrdering.Compare(Address, other.Address) < 0;
            }

            return UpNumber < other.UpNumber;
        }

        /// <summary>
        /// Creates a copy of this member with the status provided.
        /// </summary>
        /// <param name="status">The new status of this member.</param>
        /// <exception cref="InvalidOperationException">TBD</exception>
        /// <returns>A new copy of this member with the provided status.</returns>
        public Member Copy(MemberStatus status)
        {
            var oldStatus = Status;
            if (status == oldStatus) return this;

            //TODO: Akka exception?
            if (!AllowedTransitions[oldStatus].Contains(status))
                ThrowHelper.ThrowInvalidOperationException_MemberCopy(Status, status);

            return new Member(UniqueAddress, UpNumber, status, Roles);
        }

        /// <summary>
        /// Creates a copy of this member with the provided upNumber.
        /// </summary>
        /// <param name="upNumber">The new upNumber for this member.</param>
        /// <returns>A new copy of this member with the provided upNumber.</returns>
        public Member CopyUp(int upNumber)
        {
            return new Member(UniqueAddress, upNumber, Status, Roles).Copy(status: MemberStatus.Up);
        }

        /// <summary>
        ///  <see cref="Address"/> ordering type class, sorts addresses by host and port.
        /// </summary>
        public static readonly IComparer<Address> AddressOrdering = new AddressComparer();

        /// <summary>
        /// INTERNAL API
        /// </summary>
        internal class AddressComparer : IComparer<Address>
        {
            /// <inheritdoc cref="IComparer{Address}.Compare"/>
            public int Compare(Address x, Address y)
            {
                if (x is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.x);
                if (y is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.y);

                if (ReferenceEquals(x, y)) return 0;
                var result = string.CompareOrdinal(x.Host ?? "", y.Host ?? "");
                if (result != 0) return result;
                return Nullable.Compare(x.Port, y.Port);
            }
        }

        /// <summary>
        /// Compares members by their upNumber to determine which is oldest / youngest.
        /// </summary>
        internal static readonly AgeComparer AgeOrdering = new AgeComparer();

        /// <summary>
        ///  INTERNAL API
        /// </summary>
        internal class AgeComparer : IComparer<Member>
        {
            /// <inheritdoc cref="IComparer{Member}.Compare"/>
            public int Compare(Member a, Member b)
            {
                if (a.Equals(b)) return 0;
                if (a.IsOlderThan(b)) return -1;
                return 1;
            }
        }

        /// <summary>
        /// Orders the members by their address except that members with status
        /// Joining, Exiting and Down are ordered last (in that order).
        /// </summary>
        internal static readonly LeaderStatusMemberComparer LeaderStatusOrdering = new LeaderStatusMemberComparer();

        /// <summary>
        /// INTERNAL API
        /// </summary>
        internal class LeaderStatusMemberComparer : IComparer<Member>
        {
            /// <inheritdoc cref="IComparer{Member}.Compare"/>
            public int Compare(Member a, Member b)
            {
                var @as = a.Status;
                var bs = b.Status;
                if (@as == bs) return Ordering.Compare(a, b);
                if (@as == MemberStatus.Down) return 1;
                if (@bs == MemberStatus.Down) return -1;
                if (@as == MemberStatus.Exiting) return 1;
                if (@bs == MemberStatus.Exiting) return -1;
                if (@as == MemberStatus.Joining) return 1;
                if (@bs == MemberStatus.Joining) return -1;
                if (@as == MemberStatus.WeaklyUp) return 1;
                if (@bs == MemberStatus.WeaklyUp) return -1;
                return Ordering.Compare(a, b);
            }
        }

        /// <summary>
        /// <see cref="Member"/> ordering type class, sorts members by host and port.
        /// </summary>
        internal static readonly MemberComparer Ordering = new MemberComparer();

        /// <summary>
        /// INTERNAL API
        /// </summary>
        internal class MemberComparer : IComparer<Member>
        {
            /// <inheritdoc cref="IComparer{Member}.Compare"/>
            public int Compare(Member x, Member y)
            {
                if (x is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.x);
                if (y is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.y);

                return x.UniqueAddress.CompareTo(y.UniqueAddress, AddressOrdering);
            }
        }

        /// <summary>
        /// Combines and sorts two lists of <see cref="Member"/> into a single list ordered by Member's valid transitions
        /// </summary>
        /// <param name="a">The first collection of members.</param>
        /// <param name="b">The second collection of members.</param>
        /// <returns>An immutable hash set containing the members with the next logical transition.</returns>
        public static ImmutableSortedSet<Member> PickNextTransition(IEnumerable<Member> a, IEnumerable<Member> b)
        {
            // group all members by Address => Seq[Member]
            var groupedByAddress = (a.Concat(b)).GroupBy(x => x.UniqueAddress);

            var acc = new HashSet<Member>(Akka.Cluster.MemberComparer.Instance);

            foreach (var g in groupedByAddress)
            {
                if (g.Count() == 2) acc.Add(PickNextTransition(g.First(), g.Skip(1).First()));
                else
                {
                    var m = g.First();
                    acc.Add(m);
                }
            }

            return acc.ToImmutableSortedSet();
        }

        /// <summary>
        /// Compares two copies OF THE SAME MEMBER and returns whichever one is a valid transition of the other.
        /// </summary>
        /// <param name="a">First member instance.</param>
        /// <param name="b">Second member instance.</param>
        /// <returns>If a and b are different members, this method will return <c>null</c>.
        /// Otherwise, will return a or b depending on which one is a valid transition of the other.
        /// If neither are a valid transition, we return <c>null</c></returns>
        public static Member PickNextTransition(Member a, Member b)
        {
            if (a == null || b == null || !a.Equals(b))
                return null;

            // if the member statuses are equal, then it doesn't matter which one we return
            if (a.Status.Equals(b.Status))
                return a;

            if (Member.AllowedTransitions[a.Status].Contains(b.Status))
                return b;
            if (Member.AllowedTransitions[b.Status].Contains(a.Status))
                return a;

            return null; // illegal transition
        }

        /// <summary>
        /// Combines and sorts two lists of <see cref="Member"/> into a single list ordered by highest prioirity
        /// </summary>
        /// <param name="a">The first collection of members.</param>
        /// <param name="b">The second collection of members.</param>
        /// <returns>An immutable hash set containing the members with the highest priority.</returns>
        public static ImmutableHashSet<Member> PickHighestPriority(IEnumerable<Member> a, IEnumerable<Member> b)
        {
            // group all members by Address => Seq[Member]
            var groupedByAddress = (a.Concat(b)).GroupBy(x => x.UniqueAddress);

            var acc = new HashSet<Member>(Akka.Cluster.MemberComparer.Instance);

            foreach (var g in groupedByAddress)
            {
                if (g.Count() == 2) acc.Add(HighestPriorityOf(g.First(), g.Skip(1).First()));
                else
                {
                    var m = g.First();
                    if (!Gossip.RemoveUnreachableWithMemberStatus.Contains(m.Status)) acc.Add(m);
                }
            }
            return acc.ToImmutableHashSet(Akka.Cluster.MemberComparer.Instance);
        }

        /// <summary>
        /// Picks the Member with the highest "priority" MemberStatus.
        /// </summary>
        /// <param name="m1">The first member to compare.</param>
        /// <param name="m2">The second member to compare.</param>
        /// <returns>The higher priority of the two members.</returns>
        public static Member HighestPriorityOf(Member m1, Member m2)
        {
            if (m1.Status.Equals(m2.Status))
            {
                return m1.IsOlderThan(m2) ? m1 : m2;
            }

            var m1Status = m1.Status;
            var m2Status = m2.Status;
            if (m1Status == MemberStatus.Removed) return m1;
            if (m2Status == MemberStatus.Removed) return m2;
            if (m1Status == MemberStatus.Down) return m1;
            if (m2Status == MemberStatus.Down) return m2;
            if (m1Status == MemberStatus.Exiting) return m1;
            if (m2Status == MemberStatus.Exiting) return m2;
            if (m1Status == MemberStatus.Leaving) return m1;
            if (m2Status == MemberStatus.Leaving) return m2;
            if (m1Status == MemberStatus.Joining) return m2;
            if (m2Status == MemberStatus.Joining) return m1;
            if (m1Status == MemberStatus.WeaklyUp) return m2;
            if (m2Status == MemberStatus.WeaklyUp) return m1;
            return m1;
        }

        /// <summary>
        /// All of the legal state transitions for a cluster member
        /// </summary>
        internal static readonly ImmutableDictionary<MemberStatus, ImmutableHashSet<MemberStatus>> AllowedTransitions =
            new Dictionary<MemberStatus, ImmutableHashSet<MemberStatus>>
            {
                {MemberStatus.Joining, ImmutableHashSet.Create(MemberStatus.WeaklyUp, MemberStatus.Up,MemberStatus.Leaving, MemberStatus.Down, MemberStatus.Removed)},
                {MemberStatus.WeaklyUp, ImmutableHashSet.Create(MemberStatus.Up, MemberStatus.Leaving, MemberStatus.Down, MemberStatus.Removed) },
                {MemberStatus.Up, ImmutableHashSet.Create(MemberStatus.Leaving, MemberStatus.Down, MemberStatus.Removed)},
                {MemberStatus.Leaving, ImmutableHashSet.Create(MemberStatus.Exiting, MemberStatus.Down, MemberStatus.Removed)},
                {MemberStatus.Down, ImmutableHashSet.Create(MemberStatus.Removed)},
                {MemberStatus.Exiting, ImmutableHashSet.Create(MemberStatus.Removed, MemberStatus.Down)},
                {MemberStatus.Removed, ImmutableHashSet.Create<MemberStatus>()}
            }.ToImmutableDictionary();
    }

    /// <summary>
    /// Defines the current status of a cluster member node
    ///
    /// Can be one of: Joining, Up, WeaklyUp, Leaving, Exiting and Down.
    /// </summary>
    public enum MemberStatus
    {
        /// <summary>
        /// Indicates that a new node is joining the cluster.
        /// </summary>
        Joining = 0,
        /// <summary>
        /// Indicates that a node is a current member of the cluster.
        /// </summary>
        Up = 1,
        /// <summary>
        /// Indicates that a node is beginning to leave the cluster.
        /// </summary>
        Leaving = 2,
        /// <summary>
        /// Indicates that all nodes are aware that this node is leaving the cluster.
        /// </summary>
        Exiting = 3,
        /// <summary>
        /// Node was forcefully removed from the cluster by means of <see cref="Cluster.Down"/>
        /// </summary>
        Down = 4,
        /// <summary>
        /// Node was removed as a member from the cluster.
        /// </summary>
        Removed = 5,
        /// <summary>
        /// Indicates that new node has already joined, but it cannot be set to <see cref="Up"/>
        /// because cluster convergence cannot be reached i.e. because of unreachable nodes.
        /// </summary>
        WeaklyUp = 6,
    }

    /// <summary>MemberComparer</summary>
    public sealed class MemberComparer : IEqualityComparer<Member>
    {
        /// <summary>MemberComparer.Instance</summary>
        public static readonly MemberComparer Instance = new MemberComparer();

        /// <summary>Determines whether the specified <see cref="Member"/>s are equal.</summary>
        public bool Equals(Member x, Member y)
        {
            if (ReferenceEquals(x, y)) { return true; }
            if (null == x/* || null == y*/) { return false; }
            return x.Equals(y);
        }

        /// <summary>Returns a hash code for the specified <see cref="Member"/>.</summary>
        public int GetHashCode(Member obj) => obj.GetHashCode();
    }

    /// <summary>
    /// Member identifier consisting of address and random `uid`.
    /// The `uid` is needed to be able to distinguish different
    /// incarnations of a member with same hostname and port.
    /// </summary>
    [MessagePackObject]
    public class UniqueAddress : IComparable<UniqueAddress>, IEquatable<UniqueAddress>, IComparable
    {
        /// <summary>
        /// The bound listening address for Akka.Remote.
        /// </summary>
        [Key(0)]
        public Address Address { get; }

        /// <summary>
        /// A random long integer used to signal the incarnation of this cluster instance.
        /// </summary>
        [Key(1)]
        public int Uid { get; }

        /// <summary>
        /// Creates a new unique address instance.
        /// </summary>
        /// <param name="address">The original Akka <see cref="Address"/></param>
        /// <param name="uid">The UID for the cluster instance.</param>
        public UniqueAddress(Address address, int uid)
        {
            Uid = uid;
            Address = address;
        }

        /// <summary>
        /// Compares two unique address instances to each other.
        /// </summary>
        /// <param name="other">The other address to compare to.</param>
        /// <returns><c>true</c> if equal, <c>false</c> otherwise.</returns>
        public bool Equals(UniqueAddress other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Uid == other.Uid && Address.Equals(other.Address);
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj) => obj is UniqueAddress uniqueAddress && Equals(uniqueAddress);

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            return Uid;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="uniqueAddress">TBD</param>
        /// <returns>TBD</returns>
        public int CompareTo(UniqueAddress uniqueAddress) => CompareTo(uniqueAddress, Address.Comparer);

        int IComparable.CompareTo(object obj)
        {
            if (obj is UniqueAddress address) return CompareTo(address);

            return ThrowHelper.ThrowArgumentException_UniqueAddressCompare(obj);
        }

        internal int CompareTo(UniqueAddress uniqueAddress, IComparer<Address> addresComparer)
        {
            if (uniqueAddress == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.uniqueAddress);

            var result = addresComparer.Compare(Address, uniqueAddress.Address);
            return result == 0 ? Uid.CompareTo(uniqueAddress.Uid) : result;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() => $"UniqueAddress: ({Address}, {Uid})";

        #region operator overloads

        /// <summary>
        /// Compares two specified unique addresses for equality.
        /// </summary>
        /// <param name="left">The first unique address used for comparison</param>
        /// <param name="right">The second unique address used for comparison</param>
        /// <returns><c>true</c> if both unique addresses are equal; otherwise <c>false</c></returns>
        public static bool operator ==(UniqueAddress left, UniqueAddress right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Compares two specified unique addresses for inequality.
        /// </summary>
        /// <param name="left">The first unique address used for comparison</param>
        /// <param name="right">The second unique address used for comparison</param>
        /// <returns><c>true</c> if both unique addresses are not equal; otherwise <c>false</c></returns>
        public static bool operator !=(UniqueAddress left, UniqueAddress right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    /// <summary>UniqueAddressComparer</summary>
    public sealed class UniqueAddressComparer : IEqualityComparer<UniqueAddress>
    {
        /// <summary>UniqueAddressComparer.Instance</summary>
        public static readonly UniqueAddressComparer Instance = new UniqueAddressComparer();

        /// <summary>Determines whether the specified <see cref="UniqueAddress"/>s are equal.</summary>
        public bool Equals(UniqueAddress x, UniqueAddress y)
        {
            if (ReferenceEquals(x, y)) { return true; }
            if (null == x/* || null == y*/) { return false; }
            return x.Equals(y);
        }

        /// <summary>Returns a hash code for the specified <see cref="UniqueAddress"/>.</summary>
        public int GetHashCode(UniqueAddress obj) => obj.GetHashCode();
    }
}

