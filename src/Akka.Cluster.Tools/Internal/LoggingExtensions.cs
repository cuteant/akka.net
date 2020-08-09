// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Event;

namespace Akka.Cluster.Tools
{
    internal static class ClusterToolsLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingBufferedMessagesToCurrentSingletonInstance(this ILoggingAdapter logger)
        {
            logger.Debug("Sending buffered messages to current singleton instance");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToIdentifySingletonAt(this ILoggingAdapter logger, ActorPath singletonAddress)
        {
            logger.Debug("Trying to identify singleton at [{0}]", singletonAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScheduleDelayedMemberRemovedFor(this ILoggingAdapter logger, Member member)
        {
            logger.Debug("Schedule DelayedMemberRemoved for {0}", member.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ForwardingMessageOfTypeToCurrentSingletonInstanceAt(this ILoggingAdapter logger, object msg, IActorRef singleton)
        {
            logger.Debug("Forwarding message of type [{0}] to current singleton instance at [{1}]", msg.GetType(), singleton.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CreatingSingletonIdentificationTimer(this ILoggingAdapter logger)
        {
            logger.Debug("Creating singleton identification timer...");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonNotAvailableBufferingMessageType(this ILoggingAdapter logger, object message)
        {
            logger.Debug("Singleton not available, buffering message type [{0}]", message.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonNotAvailableAndBufferingIsDisabled(this ILoggingAdapter logger, object message)
        {
            logger.Debug("Singleton not available and buffering is disabled, dropping message [{0}]", message.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonNotAvailableBufferIsFull(this ILoggingAdapter logger, object key)
        {
            logger.Debug("Singleton not available, buffer is full, dropping first message [{0}]", key.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClientResponseTunnelForClientStoppedDueToInactivity(this ILoggingAdapter logger, IActorRef client)
        {
            logger.Debug("ClientResponseTunnel for client [{0}] stopped due to inactivity", client.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LostContactWith(this ILoggingAdapter logger, IActorRef c)
        {
            logger.Debug($"Lost contact with [{c.Path}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedNewContactFrom(this ILoggingAdapter logger, IActorRef client)
        {
            logger.Debug($"Received new contact from [{client.Path}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClientGetsContactPoints(this ILoggingAdapter logger, IActorRef sender, ClusterReceptionist.Contacts contacts)
        {
            logger.Debug("Client [{0}] gets ContactPoints [{1}]", sender.Path, string.Join(", ", contacts.ContactPoints));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClientGetsContactPointsAllNodes(this ILoggingAdapter logger, IActorRef sender, ClusterReceptionist.Contacts contacts)
        {
            logger.Debug("Client [{0}] gets contactPoints [{1}] (all nodes)", sender.Path, string.Join(", ", contacts));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HeartbeatFromClient(this ILoggingAdapter logger, IActorRef sender)
        {
            logger.Debug("Heartbeat from client [{0}]", sender.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingBufferedMessagesToReceptionist(this ILoggingAdapter logger)
        {
            logger.Debug("Sending buffered messages to receptionist");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceptionistNotAvailableBufferingMessageType(this ILoggingAdapter logger, object message)
        {
            logger.Debug("Receptionist not available, buffering message type [{0}]", message.GetType().Name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingGetContactsTo(this ILoggingAdapter logger, ActorSelection[] sendTo)
        {
            logger.Debug("Sending GetContacts to [{0}]", string.Join(", ", sendTo.AsEnumerable()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringTakeOverRequest(this ILoggingAdapter logger,
            ClusterSingletonState stateName, IActorRef sender)
        {
            logger.Debug("Ignoring TakeOver request in [{0}] from [{1}].", stateName, sender.Path.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Receptionist_is_shutting_down_reestablishing_connection(this ILoggingAdapter logger, IActorRef receptionist)
        {
            logger.Info("Receptionist [{0}] is shutting down, reestablishing connection", receptionist);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonIdentifiedAt(this ILoggingAdapter logger, IActorRef subject)
        {
            logger.Info("Singleton identified at [{0}]", subject.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LostContactWithReestablishingConnection(this ILoggingAdapter logger, IActorRef receptionist)
        {
            logger.Info("Lost contact with [{0}], reestablishing connection", receptionist);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectedTo(this ILoggingAdapter logger, IActorRef receptionist)
        {
            logger.Info("Connected to [{0}]", receptionist.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExitedCluster(this ILoggingAdapter logger, Cluster cluster)
        {
            logger.Info("Exited [{0}]", cluster.SelfAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SelfRemovedStoppingClusterSingletonManager(this ILoggingAdapter logger)
        {
            logger.Info("Self removed, stopping ClusterSingletonManager");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClusterSingletonManagerStateChange(this ILoggingAdapter logger,
            ClusterSingletonState from, ClusterSingletonState to, IClusterSingletonData data)
        {
            logger.Info("ClusterSingletonManager state change [{0} -> {1}] {2}", from, to, data.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemberRemoved(this ILoggingAdapter logger, DelayedMemberRemoved delayedMemberRemoved)
        {
            logger.Info("Member removed [{0}]", delayedMemberRemoved.Member.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemberRemoved(this ILoggingAdapter logger, ClusterEvent.MemberRemoved removed)
        {
            logger.Info("Member removed [{0}]", removed.Member.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetrySendingTakeOverFromMeTo(this ILoggingAdapter logger, int count, WasOldestData wasOldestData)
        {
            logger.Info("Retry [{0}], sending TakeOverFromMe to [{1}]", count, wasOldestData.NewOldest?.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OldestObservedOldestChanged(this ILoggingAdapter logger, Cluster cluster, OldestChangedBuffer.OldestChanged oldestChanged)
        {
            logger.Info("Oldest observed OldestChanged: [{0} -> {1}]", cluster.SelfAddress, oldestChanged.Oldest?.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TimeoutInBecomingOldest(this ILoggingAdapter logger)
        {
            logger.Info("Timeout in BecomingOldest. Previous oldest unknown, removed and no TakeOver request.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetrySendingHandOverToMeTo(this ILoggingAdapter logger, int count, BecomingOldestData becomingOldest)
        {
            logger.Info("Retry [{0}], sending HandOverToMe to [{1}]", count, becomingOldest.PreviousOldest?.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringTakeOverRequestInBecomingOldest(this ILoggingAdapter logger, IActorRef sender, UniqueAddress previousOldest)
        {
            logger.Info("Ignoring TakeOver request in BecomingOldest from [{0}]. Expected previous oldest [{1}]", sender.Path.Address, previousOldest.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringTakeOverRequestFromUnknownNode(this ILoggingAdapter logger, Address senderAddress)
        {
            logger.Info("Ignoring TakeOver request from unknown node in BecomingOldest from [{0}]", senderAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PreviousOldestRemoved(this ILoggingAdapter logger, BecomingOldestData becoming)
        {
            logger.Info("Previous oldest [{0}] removed", becoming.PreviousOldest);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringHandOverDoneInBecomingOldest(this ILoggingAdapter logger, IActorRef sender, BecomingOldestData b)
        {
            logger.Info("Ignoring HandOverDone in BecomingOldest from [{0}]. Expected previous oldest [{1}]",
                sender.Path.Address, b.PreviousOldest.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HandoverInProgressAt(this ILoggingAdapter logger, IActorRef sender)
        {
            logger.Info("Hand-over in progress at [{0}]", sender.Path.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PreviousOldestRemoved(this ILoggingAdapter logger, DelayedMemberRemoved removed)
        {
            logger.Info("Previous oldest removed [{0}]", removed.Member.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void YoungerObservedOldestChanged(this ILoggingAdapter logger, YoungerData youngerData, OldestChangedBuffer.OldestChanged oldestChanged)
        {
            logger.Info("Younger observed OldestChanged: [{0} -> {1}]", youngerData.Oldest?.Address, oldestChanged.Oldest?.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void YoungerObservedOldestChanged(this ILoggingAdapter logger, YoungerData youngerData)
        {
            logger.Info("Younger observed OldestChanged: [{0} -> myself]", youngerData.Oldest?.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonTerminatedHandoverDone(this ILoggingAdapter logger, Cluster cluster, Address newOldest)
        {
            logger.Info("Singleton terminated, hand-over done [{0} -> {1}]", cluster.SelfAddress, newOldest);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonManagerStartedSingletonActor(this ILoggingAdapter logger, IActorRef singleton)
        {
            logger.Info("Singleton manager started singleton actor [{0}] ", singleton.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonManagerStoppingSingletonActor(this ILoggingAdapter logger, IActorRef singleton)
        {
            logger.Info("Singleton manager stopping singleton actor [{0}]", singleton.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SelfDownedStoppingClusterSingletonManager(this ILoggingAdapter logger)
        {
            logger.Info("Self downed, stopping ClusterSingletonManager");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SelfDownedStopping(this ILoggingAdapter logger)
        {
            logger.Info("Self downed, stopping");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SelfDownedWaitingForRemoval(this ILoggingAdapter logger)
        {
            logger.Info("Self downed, waiting for removal");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SingletonActorWasTerminated(this ILoggingAdapter logger, IActorRef singleton)
        {
            logger.Info("Singleton actor [{0}] was terminated", singleton.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RegisteredActorMustBeLocal(this ILoggingAdapter logger, Put put)
        {
            logger.Warning("Registered actor must be local: [{0}]", put.Ref);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceptionistNotAvailableBufferIsFull(this ILoggingAdapter logger, object obj)
        {
            logger.Warning("Receptionist not available, buffer is full, dropping first message [{0}]", obj.GetType().Name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceptionistNotAvailableAndBufferingIsDisabled(this ILoggingAdapter logger, object message)
        {
            logger.Warning("Receptionist not available and buffering is disabled, dropping message [{0}]", message.GetType().Name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceptionistReconnectNotSuccessful(this ILoggingAdapter logger, ClusterClientSettings settings)
        {
            logger.Warning("Receptionist reconnect not successful within {0} stopping cluster client", settings.ReconnectTimeout);
        }
    }
}
