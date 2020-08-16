// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Event;

namespace Akka.Cluster
{
    internal static class ClusterLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterIsTakingOverResponsibilityOfNode(this ILoggingAdapter logger, Address address)
        {
            logger.Debug("Cluster is taking over responsibility of node: {0}", address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeTriggerExtraExpectedHeartbeatFrom(this ILoggingAdapter logger, Cluster cluster, UniqueAddress from)
        {
            logger.Debug("Cluster Node [{0}] - Trigger extra expected heartbeat from [{1}]", cluster.SelfAddress, from.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeHeartbeatResponseFrom(this ILoggingAdapter logger, Cluster cluster, UniqueAddress from)
        {
            logger.Debug("Cluster Node [{0}] - Heartbeat response from [{1}]", cluster.SelfAddress, from.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeHeartbeatTo(this ILoggingAdapter logger, Cluster cluster, UniqueAddress to)
        {
            logger.Debug("Cluster Node [{0}] - Heartbeat to [{1}]", cluster.SelfAddress, to.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeFirstHeartbeatTo(this ILoggingAdapter logger, Cluster cluster, UniqueAddress to)
        {
            logger.Debug("Cluster Node [{0}] - First Heartbeat to [{1}]", cluster.SelfAddress, to.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeReceivingGossipFrom(this ILoggingAdapter logger, Cluster cluster, UniqueAddress from)
        {
            logger.Debug("Cluster Node [{0}] - Receiving gossip from [{1}]", cluster.SelfAddress, from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldNotEstablishACausalRelationship(this ILoggingAdapter logger, Gossip remoteGossip, Gossip localGossip, Gossip winningGossip)
        {
            logger.Debug(@"""Couldn't establish a causal relationship between ""remote"" gossip and ""local"" gossip - Remote[{0}] - Local[{1}] - merged them into [{2}]""",
                remoteGossip, localGossip, winningGossip);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodePrunedConflictingLocalGossip(this ILoggingAdapter logger, Cluster cluster, Member m)
        {
            logger.Debug("Cluster Node [{0}] - Pruned conflicting local gossip: {1}", cluster.SelfAddress, m);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodePrunedConflictingRemoteGossip(this ILoggingAdapter logger, Cluster cluster, Member m)
        {
            logger.Debug("Cluster Node [{0}] - Pruned conflicting remote gossip: {1}", cluster.SelfAddress, m);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeIgnoringReceivedGossip(this ILoggingAdapter logger, Cluster cluster, UniqueAddress from)
        {
            logger.Debug("Cluster Node [{0}] - Ignoring received gossip from [{1}] to protect against overload", cluster.SelfAddress, from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeReceivedGossip(this ILoggingAdapter logger, Cluster cluster, GossipEnvelope ge, ClusterCoreDaemon.ReceiveGossipType receivedType)
        {
            logger.Debug("Cluster Node [{0}] - Received gossip from [{1}] which was {2}.", cluster.SelfAddress, ge.From, receivedType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Cluster_Node_Gossip_exiting_members_to_the_two_oldest(this ILoggingAdapter logger, UniqueAddress selfUniqueAddress, IEnumerable<Member> exitingMembers, IEnumerable<Member> targets)
        {
            logger.Debug(
              "Cluster Node [{0}] - Gossip exiting members [{1}] to the two oldest (per role) [{2}] (singleton optimization).",
              selfUniqueAddress, string.Join(", ", exitingMembers), string.Join(", ", targets));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Is_no_longer_leader(this Cluster cluster)
        {
            cluster.LogInfo("is no longer leader");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Is_the_new_leader_among_reachable_nodes(this Cluster cluster)
        {
            cluster.LogInfo("is the new leader among reachable nodes (more leaders may exist)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogClusterEventName(this Cluster cluster, ClusterEvent.IClusterDomainEvent clusterDomainEvent)
        {
            cluster.LogInfo("event {0}", clusterDomainEvent.GetType().Name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ANetworkPartitionHasBeenDetected(this ILoggingAdapter logger, ISplitBrainStrategy strategy, ImmutableArray<Member> nodesToDown)
        {
            logger.Info("A network partition has been detected. {0} decided to down following nodes: [{1}]", strategy, string.Join(", ", nodesToDown));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ANetworkPartitionDetected(this ILoggingAdapter logger, ImmutableSortedSet<Member> unreachable, ImmutableSortedSet<Member> reachable)
        {
            logger.Info("A network partition detected - unreachable nodes: [{0}], remaining: [{1}]", string.Join(", ", unreachable.Select(m => m.Address)), string.Join(", ", reachable.Select(m => m.Address)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LeaderIsAutoDowningUnreachableNode(this Cluster cluster, Address node)
        {
            cluster.LogInfo("Leader is auto-downing unreachable node [{0}]", node);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkingNodesAsREACHABLE(this Cluster cluster, ImmutableHashSet<Member> newlyDetectedReachableMembers)
        {
            cluster.LogInfo("Marking node(s) as REACHABLE [{0}]. Node roles [{1}]", newlyDetectedReachableMembers.Select(m => m.ToString()).Aggregate((a, b) => a + ", " + b), string.Join(",", cluster.SelfRoles));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkingExitingNodesAsUNREACHABLE(this Cluster cluster, ImmutableSortedSet<Member> exiting)
        {
            cluster.LogInfo("Cluster Node [{0}] - Marking exiting node(s) as UNREACHABLE [{1}]. This is expected and they will be removed.",
                cluster.SelfAddress, exiting.Select(m => m.ToString()).Aggregate((a, b) => a + ", " + b));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LeaderIsRemovingConfirmedExitingNode(this Cluster cluster, UniqueAddress m)
        {
            cluster.LogInfo("Leader is removing confirmed Exiting node [{0}]", m.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LeaderIsRemovingNode(this Cluster cluster, Member m)
        {
            var status = m.Status == MemberStatus.Exiting ? "exiting" : "unreachable";
            cluster.LogInfo("Leader is removing {0} node [{1}]", status, m.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LeaderIsMovingNode(this Cluster cluster, Member m)
        {
            cluster.LogInfo("Leader is moving node [{0}] to [{1}]", m.Address, m.Status);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExitingLeaderStartingCoordinatedShutdown(this Cluster cluster)
        {
            cluster.LogInfo("Exiting (leader), starting coordinated shutdown.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShuttingDownMyself(this Cluster cluster)
        {
            cluster.LogInfo("Shutting down myself");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LeaderCanCurrentlyNotPerformItsDuties(this Cluster cluster, Gossip latestGossip)
        {
            cluster.LogInfo(
                "Leader can currently not perform its duties, reachability status: [{0}], member status: [{1}]",
                latestGossip.ReachabilityExcludingDownedObservers,
                string.Join(", ", latestGossip.Members
                    .Select(m => string.Format("${0} ${1} seen=${2}",
                        m.Address,
                        m.Status,
                        latestGossip.SeenByNode(m.UniqueAddress)))));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LeaderCanPerformItsDutiesAgain(this Cluster cluster)
        {
            cluster.LogInfo("Leader can perform its duties again");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExitingStartingCoordinatedShutdown(this Cluster cluster)
        {
            cluster.LogInfo("Exiting, starting coordinated shutdown.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringReceivedGossipThatDoesNotContainMyself(this Cluster cluster, UniqueAddress from)
        {
            cluster.LogInfo("Ignoring received gossip that does not contain myself, from [{0}]", from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringReceivedGossipFromUnknown(this Cluster cluster, UniqueAddress from)
        {
            cluster.LogInfo("Cluster Node [{0}] - Ignoring received gossip from unknown [{1}]", cluster.SelfAddress, from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringReceivedGossipGromUnreachable(this Cluster cluster, UniqueAddress from)
        {
            cluster.LogInfo("Ignoring received gossip from unreachable [{0}]", from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringReceivedGossipIntendedForSomeone(this Cluster cluster, UniqueAddress from, GossipEnvelope envelope)
        {
            cluster.LogInfo("Ignoring received gossip intended for someone else, from [{0}] to [{1}]", from.Address, envelope.To);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringReceivedGossipStatusFromUnknown(this Cluster cluster, UniqueAddress from)
        {
            cluster.LogInfo("Cluster Node [{0}] - Ignoring received gossip status from unknown [{1}]", cluster.SelfAddress, from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringReceivedGossipStatusFromUnreachable(this Cluster cluster, UniqueAddress from)
        {
            cluster.LogInfo("Ignoring received gossip status from unreachable [{0}]", from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringDownOfUnknownNode(this Cluster cluster, Address address)
        {
            cluster.LogInfo("Ignoring down of unknown node [{0}]", address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkingUnreachableNode(this Cluster cluster, Member member)
        {
            cluster.LogInfo("Marking unreachable node [{0}] as [{1}]", member.Address, MemberStatus.Down);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkingNode(this Cluster cluster, Member member)
        {
            cluster.LogInfo("Marking node [{0}] as [{1}]", member.Address, MemberStatus.Down);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkedAddress(this Cluster cluster, Address address)
        {
            cluster.LogInfo("Marked address [{0}] as [{1}]", address, MemberStatus.Leaving);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WelcomeFrom(this Cluster cluster, UniqueAddress from)
        {
            cluster.LogInfo("Welcome from [{0}]", from.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringWelcomeFromWhenTryingToJoinWith(this Cluster cluster, UniqueAddress from, Address joinWith)
        {
            cluster.LogInfo("Ignoring welcome from [{0}] when trying to join with [{1}]", from.Address, joinWith);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NodeIsJOININGRoles(this Cluster cluster, UniqueAddress node, ImmutableHashSet<string> roles)
        {
            cluster.LogInfo("Node [{0}] is JOINING, roles [{1}]", node.Address, string.Join(",", roles));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NodeIsJOININGItself(this Cluster cluster, UniqueAddress node, ImmutableHashSet<string> roles)
        {
            cluster.LogInfo("Node [{0}] is JOINING itself (with roles [{1}]) and forming a new cluster", node.Address, string.Join(",", roles));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NewIncarnationOfExistingMemberIsTryingToJoin(this Cluster cluster, UniqueAddress node)
        {
            cluster.LogInfo("New incarnation of existing member [{0}] is trying to join. Existing will be removed from the cluster and then new member will be allowed to join.", node);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExistingMemberIsJoiningAgain(this Cluster cluster, UniqueAddress node)
        {
            cluster.LogInfo("Existing member [{0}] is joining again.", node);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToJoinToMemberIgnoring(this Cluster cluster, UniqueAddress node, MemberStatus selfStatus)
        {
            cluster.LogInfo("Trying to join [{0}] to [{1}] member, ignoring. Use a member that is Up instead.", node, selfStatus);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingInitJoinNackMessageFromNode(this Cluster cluster, UniqueAddress selfUniqueAddress, IActorRef sender)
        {
            cluster.LogInfo("Sending InitJoinNack message from node [{0}] to [{1}]", selfUniqueAddress, sender);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToJoinSeedNodesWhenAlreadyPartOfAClusterIgnoring(this Cluster cluster, InternalClusterAction.JoinSeedNodes joinSeedNodes)
        {
            cluster.LogInfo("Trying to join seed nodes [{0}] when already part of a cluster, ignoring",
                joinSeedNodes.SeedNodes.Select(a => a.ToString()).Aggregate((a, b) => a + ", " + b));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToJoinWhenAlreadyPartOfAClusterIgnoring(this Cluster cluster, ClusterUserAction.JoinTo jt)
        {
            cluster.LogInfo("Trying to join [{0}] when already part of a cluster, ignoring", jt.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedInitJoinMessageFrom(this Cluster cluster, IActorRef sender, UniqueAddress selfUniqueAddress)
        {
            cluster.LogInfo("Received InitJoin message from [{0}] to [{1}]", sender, selfUniqueAddress.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedInitJoinMessageFromButThisNodeIsNotInitializedYet(this Cluster cluster, IActorRef sender)
        {
            cluster.LogInfo("Received InitJoin message from [{0}], but this node is not initialized yet", sender);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExitingConfirmed(this Cluster cluster, UniqueAddress node)
        {
            cluster.LogInfo("Exiting confirmed [{0}]", node.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExitingCompleted(this Cluster cluster)
        {
            cluster.LogInfo("Exiting completed.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoSeedNodesConfiguredManualClusterJoinRequired(this Cluster cluster)
        {
            cluster.LogInfo("No seed-nodes configured, manual cluster join required");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldnotJoinSeedNodesAfterAttempts(this ILoggingAdapter logger, int attempts, ImmutableList<Address> seeds, Address selfAddress)
        {
            logger.Warning("Couldn't join seed nodes after [{0}] attempts, will try again. seed-nodes=[{1}]",
                attempts, string.Join(",", seeds.Where(x => !x.Equals(selfAddress))));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkingNodesAsUNREACHABLE(this ILoggingAdapter logger, Cluster cluster, ImmutableSortedSet<Member> nonExiting)
        {
            logger.Warning("Cluster Node [{0}] - Marking node(s) as UNREACHABLE [{1}]. Node roles [{2}]",
                cluster.SelfAddress, nonExiting.Select(m => m.ToString()).Aggregate((a, b) => a + ", " + b), string.Join(",", cluster.SelfRoles));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkingNodeAsTERMINATED(this ILoggingAdapter logger, IActorRef self, UniqueAddress node, Cluster cluster)
        {
            logger.Warning("Cluster Node [{0}] - Marking node as TERMINATED [{1}], due to quarantine. Node roles [{2}]. It must still be marked as down before it's removed.",
                self, node.Address, string.Join(",", cluster.SelfRoles));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemberWithWrongActorSystemNameTriedToJoin(this ILoggingAdapter logger, Cluster cluster, UniqueAddress node)
        {
            logger.Warning("Member with wrong ActorSystem name tried to join, but was ignored, expected [{0}] but was [{1}]",
                cluster.SelfAddress.System, node.Address.System);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemberWithWrongProtocolTriedToJoin(this ILoggingAdapter logger, Cluster cluster, UniqueAddress node)
        {
            logger.Warning("Member with wrong protocol tried to join, but was ignored, expected [{0}] but was [{1}]",
                cluster.SelfAddress.Protocol, node.Address.Protocol);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToJoinMemberWithWrongActorSystemName(this ILoggingAdapter logger, Cluster cluster, Address address)
        {
            logger.Warning("Trying to join member with wrong ActorSystem name, but was ignored, expected [{0}] but was [{1}]",
                cluster.SelfAddress.System, address.System);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToJoinMemberWithWrongProtocol(this ILoggingAdapter logger, Cluster cluster, Address address)
        {
            logger.Warning("Trying to join member with wrong protocol, but was ignored, expected [{0}] but was [{1}]",
                cluster.SelfAddress.Protocol, address.Protocol);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterNodeScheduledSendingOfHeartbeatWasDelayed(this ILoggingAdapter logger, Cluster cluster, DateTime tickTimestamp)
        {
            logger.Warning(
                "Cluster Node [{0}] - Scheduled sending of heartbeat was delayed. " +
                "Previous heartbeat was sent [{1}] ms ago, expected interval is [{2}] ms. This may cause failure detection " +
                "to mark members as unreachable. The reason can be thread starvation, e.g. by running blocking tasks on the " +
                "default dispatcher, CPU overload, or GC.",
                cluster.SelfAddress, (DateTime.UtcNow - tickTimestamp).TotalMilliseconds, cluster.Settings.HeartbeatInterval.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void JoiningOfSeedNodesWasUnsuccessful(this ILoggingAdapter logger, Cluster cluster, ImmutableList<Address> seedNodes)
        {
            logger.Warning("Joining of seed-nodes [{0}] was unsuccessful after configured shutdown-after-unsuccessful-join-seed-nodes [{1}]. Running CoordinatedShutdown.",
                string.Join(", ", seedNodes), cluster.Settings.ShutdownAfterUnsuccessfulJoinSeedNodes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CallbackFailedWith(this ILoggingAdapter logger, Exception ex, Type to)
        {
            logger.Error(ex, "[{0}] callback failed with [{1}]", to.Name, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToStartupCluster(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, "Failed to startup Cluster. You can try to increase 'akka.actor.creation-timeout'.");
        }
    }
}
