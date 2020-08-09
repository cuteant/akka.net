// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.DistributedData;
using Akka.Event;
using Akka.Persistence;

namespace Akka.Cluster.Sharding
{
    internal static class ClusterShardingLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheCoordinatorShardsStateWasSuccessfullyUpdatedWith(this ILoggingAdapter logger, string newShard)
        {
            logger.Debug("The coordinator shards state was successfully updated with {0}", newShard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheCoordinatorStateWasSuccessfullyUpdatedWith<TEvent>(this ILoggingAdapter logger, TEvent e)
        {
            logger.Debug("The coordinator state was successfully updated with {0}", e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheDDataShardStateWasSuccessfullyUpdatedWith<TEvent>(this ILoggingAdapter logger, TEvent e)
        {
            logger.Debug("The DDataShard state was successfully updated with {0}", e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DDataShardRecoveryCompletedShardWithEntities(this ILoggingAdapter logger, string shardId, int count)
        {
            logger.Debug("DDataShard recovery completed shard [{0}] with [{1}] entities", shardId, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingShardRegionProxy(this ILoggingAdapter logger, string typeName)
        {
            logger.Debug("Starting Shard Region Proxy [{0}] (no actors will be hosted on this node)...", typeName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardTerminatedWhileNotBeingHandedOff(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("Shard [{0}] terminated while not being handed off", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardHandoffComplete(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("Shard [{0}] handoff complete", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RegionWithShardsTerminated(this ILoggingAdapter logger, Terminated terminated, IImmutableSet<string> shards)
        {
            logger.Debug("Region [{0}] with shards [{1}] terminated", terminated.ActorRef, string.Join(", ", shards));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingShardInRegion(this ILoggingAdapter logger, string id)
        {
            logger.Debug("Starting shard [{0}] in region", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DeliverBufferedMessagesForShard(this ILoggingAdapter logger, int count, string shardId)
        {
            logger.Debug("Deliver [{0}] buffered messages for shard [{1}]", count, shardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryRequestForShardHomesFromCoordinator(this ILoggingAdapter logger, string key, IActorRef coordinator, int count)
        {
            var logMsg = "Retry request for shard [{0}] homes from coordinator at [{1}]. [{2}] buffered messages.";
            logger.Debug(logMsg, key, coordinator, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryRequestForShardHomesFromCoordinator2(this ILoggingAdapter logger, string key, IActorRef coordinator, int count)
        {
            var logMsg = "Retry request for shard [{0}] homes from coordinator at [{1}]. [{2}] buffered messages.";
            logger.Warning(logMsg, key, coordinator, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HandOffShard(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("Hand off shard [{0}]", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BeginHandOffShard(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("Begin hand off shard [{0}]", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HostShard(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("Host shard [{0}]", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardLocatedAt(this ILoggingAdapter logger, PersistentShardCoordinator.ShardHome home)
        {
            logger.Debug("Shard [{0}] located at [{1}]", home.Shard, home.Ref);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingGracefulShutdownOfRegionAndAllItsShards(this ILoggingAdapter logger)
        {
            logger.Debug("Starting graceful shutdown of region and all its shards");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BufferIsFullDroppingMessageForShard(this ILoggingAdapter logger, string shardId, bool isDebug)
        {
            if (isDebug)
            {
                logger.Debug("Buffer is full, dropping message for shard [{0}]", shardId);
            }
            else
            {
                logger.Warning("Buffer is full, dropping message for shard [{0}]", shardId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RequestShard(this ILoggingAdapter logger, string shardId)
        {
            logger.Debug("Request shard [{0}]", shardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ForwardingRequestForShard(this ILoggingAdapter logger, string shardId, IActorRef region)
        {
            logger.Debug("Forwarding request for shard [{0}] to [{1}]", shardId, region);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BufferMessageForShard(this ILoggingAdapter logger, string shardId, int count)
        {
            logger.Debug("Buffer message for shard [{0}]. Total [{1}] buffered messages.", shardId, count + 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardWasInitialized(this ILoggingAdapter logger, string id)
        {
            logger.Debug("Shard was initialized [{0}]", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CoordinatorMoved(this ILoggingAdapter logger, Member before, Member after)
        {
            logger.Debug("Coordinator moved from [{0}] to [{1}]",
                before?.Address.ToString() ?? string.Empty,
                after?.Address.ToString() ?? string.Empty);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AllocatedRegionForShardIsNotOneOfTheRegisteredRegions(this ILoggingAdapter logger, IActorRef region, string shard)
        {
            logger.Debug("Allocated region {0} for shard [{1}] is not (any longer) one of the registered regions", region, shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardAllocatedAt(this ILoggingAdapter logger, PersistentShardCoordinator.ShardHomeAllocated e)
        {
            logger.Debug("Shard [{0}] allocated at [{1}]", e.Shard, e.Region);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RebalanceOfNonExistingShardIsIgnored(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("Rebalance of non-existing shard [{0}] is ignored", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RebalanceShardFrom(this ILoggingAdapter logger, string shard, IActorRef rebalanceFromRegion)
        {
            logger.Debug("Rebalance shard [{0}] from [{1}]", shard, rebalanceFromRegion);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionWasNotRegistered(this ILoggingAdapter logger, IActorRef region)
        {
            logger.Debug("ShardRegion [{0}] was not registered since the coordinator currently does not know about a node of that region", region);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionRegistered(this ILoggingAdapter logger, IActorRef region)
        {
            logger.Debug("ShardRegion registered: [{0}]", region);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionProxyRegistered(this ILoggingAdapter logger, IActorRef proxy)
        {
            logger.Debug("ShardRegion proxy registered: [{0}]", proxy);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionProxyTerminated(this ILoggingAdapter logger, IActorRef proxyRef)
        {
            logger.Debug("ShardRegion proxy terminated: [{0}]", proxyRef);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionTerminated(this ILoggingAdapter logger, IActorRef terminatedRef)
        {
            logger.Debug("ShardRegion terminated: [{0}]", terminatedRef);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GetShardHomeRequestIgnored(this ILoggingAdapter logger, string shard, IActorRef region)
        {
            logger.Debug("GetShardHome [{0}] request ignored, due to region [{1}] termination in progress.", shard, region);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GetShardHomeRequestIgnored(this ILoggingAdapter logger, string shard)
        {
            logger.Debug("GetShardHome [{0}] request ignored, because not all regions have registered yet.", shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardAllocationFailed(this ILoggingAdapter logger, PersistentShardCoordinator.AllocateShardResult allocateResult)
        {
            logger.Debug("Shard [{0}] allocation failed. It will be retried", allocateResult.Shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GetShardHomeRequestFromDeferred(this ILoggingAdapter logger, string shard, IActorRef from)
        {
            logger.Debug("GetShardHome [{0}] request from [{1}] deferred, because rebalance is in progress for this shard. It will be handled when rebalance is done.", shard, from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RebalanceShardDone(this ILoggingAdapter logger, string shard, bool ok)
        {
            logger.Debug("Rebalance shard [{0}] done [{1}]", shard, ok);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GracefulShutdownOfRegion(this ILoggingAdapter logger, PersistentShardCoordinator.GracefulShutdownRequest request, IImmutableList<string> shards)
        {
            logger.Debug("Graceful shutdown of region [{0}] with shards [{1}]", request.ShardRegion, string.Join(", ", shards));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShuttingDownShardCoordinator(this ILoggingAdapter logger)
        {
            logger.Debug("Shutting down shard coordinator");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingEntityInShard(this ILoggingAdapter logger, string id, IShard shard)
        {
            logger.Debug("Starting entity [{0}] in shard [{1}]", id, shard.ShardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MessageForEntityBuffered(this ILoggingAdapter logger, string id)
        {
            logger.Debug("Message for entity [{0}] buffered", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingMessageBufferForEntity(this ILoggingAdapter logger, string id, int count)
        {
            logger.Debug("Sending message buffer for entity [{0}] ([{1}] messages)", id, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntityStoppedAfterPassivation(this ILoggingAdapter logger, Shard.EntityStopped evt)
        {
            logger.Debug("Entity stopped after passivation [{0}]", evt.EntityId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnknownEntityNotSendingStopMessageBackToEntity(this ILoggingAdapter logger, IActorRef entity)
        {
            logger.Debug("Unknown entity {0}. Not sending stopMessage back to entity.", entity);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PassivatingIdleEntities(this ILoggingAdapter logger, int idleEntitiesCount)
        {
            logger.Debug($"Passivating [{idleEntitiesCount}] idle entities");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PassivationAlreadyInProgress(this ILoggingAdapter logger, IActorRef entity)
        {
            logger.Debug("Passivation already in progress for {0}. Not sending stopMessage back to entity.", entity);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PassivatingStartedOnEntity(this ILoggingAdapter logger, string id)
        {
            logger.Debug("Passivating started on entity {0}", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HandOffShard(this ILoggingAdapter logger, IShard shard)
        {
            logger.Debug("HandOff shard [{0}]", shard.ShardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntityPreviouslyOwnedByShardStartedInShard(this ILoggingAdapter logger, ShardRegion.StartEntityAck ack, IShard shard)
        {
            logger.Debug("Entity [{0}] previously owned by shard [{1}] started in shard [{2}]", ack.EntityId, shard.ShardId, ack.ShardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GotRequestFromToStartEntityInShard(this ILoggingAdapter logger, IShard shard, ShardRegion.StartEntity start)
        {
            logger.Debug("Got a request from [{0}] to start entity [{1}] in shard [{2}]", shard.Sender, start.EntityId, shard.ShardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingEntityAgainThereAreBufferedMessagesForIt(this ILoggingAdapter logger, string id)
        {
            logger.Debug("Starting entity [{0}] again, there are buffered messages for it", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SavingSnapshotSequenceNumber(this ILoggingAdapter logger, long snapshotSequenceNr)
        {
            logger.Debug("Saving snapshot, sequence number [{0}]", snapshotSequenceNr);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentSnapshotsMatchingDeletedSuccessfully(this ILoggingAdapter logger, DeleteSnapshotsSuccess m)
        {
            logger.Debug("Persistent snapshots matching {0} deleted successfully", m.Criteria);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentMessagesToDeletedSuccessfully(this ILoggingAdapter logger, DeleteMessagesSuccess m)
        {
            logger.Debug("Persistent messages to {0} deleted successfully", m.ToSequenceNr);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentSnapshotSavedSuccessfully(this ILoggingAdapter logger)
        {
            logger.Debug("Persistent snapshot saved successfully");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceiveRecoverSnapshotOffer(this ILoggingAdapter logger, PersistentShardCoordinator.State state)
        {
            logger.Debug("ReceiveRecover SnapshotOffer {0}", state);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionTerminatedButRegionWasNotRegistered(this ILoggingAdapter logger, PersistentShardCoordinator.ShardRegionTerminated regionTerminated)
        {
            logger.Debug("ShardRegionTerminated but region {0} was not registered", regionTerminated.Region);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceiveRecover(this ILoggingAdapter logger, PersistentShardCoordinator.IDomainEvent evt)
        {
            logger.Debug("ReceiveRecover {0}", evt);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntityStoppedWithoutPassivatingWillRestartAfterBackoff(this ILoggingAdapter logger, string id)
        {
            logger.Debug("Entity [{0}] stopped without passivating, will restart after backoff", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardRecoveryCompletedShard(this ILoggingAdapter logger, string shardId, int count)
        {
            logger.Debug("PersistentShard recovery completed shard [{0}] with [{1}] entities", shardId, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardSnapshotsMatchingDeletedSuccessfully(this ILoggingAdapter logger, DeleteSnapshotsSuccess m)
        {
            logger.Debug("PersistentShard snapshots matching [{0}] deleted successfully", m.Criteria);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardMessagesToDeletedSuccessfully(this ILoggingAdapter logger, DeleteMessagesSuccess m, long deleteFrom, long deleteTo)
        {
            logger.Debug("PersistentShard messages to [{0}] deleted successfully. Deleting snapshots from [{1}] to [{2}]", m.ToSequenceNr, deleteFrom, deleteTo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardSnapshotSavedSuccessfully(this ILoggingAdapter logger)
        {
            logger.Debug("PersistentShard snapshot saved successfully");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionForTypeName(this ILoggingAdapter logger, string typeName, int total, int bufferSize)
        {
            const string logMsg = "ShardRegion for [{0}] is using [{1}] of it's buffer capacity";
            logger.Info(logMsg, typeName, 100 * total / bufferSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdleEntitiesWillBePassivatedAfter(this ILoggingAdapter logger, TimeSpan passivateIdleEntityAfter)
        {
            logger.Info($"Idle entities will be passivated after [{passivateIdleEntityAfter}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardingCoordinatorWasMovedToTheActiveState(this ILoggingAdapter logger, PersistentShardCoordinator.State currentState)
        {
            logger.Info("Sharding Coordinator was moved to the active state {0}", currentState);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SelfDownedStoppingShardRegion(this ILoggingAdapter logger, IActorRef self)
        {
            logger.Info("Self downed, stopping ShardRegion [{0}]", self.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HandOffShardReceivedDuringExistingHandOff(this ILoggingAdapter logger, IShard shard)
        {
            logger.Warning("HandOff shard [{0}] received during existing handOff", shard.ShardId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardRegionForTyp1eNameTheCoordinatorMightNotBeAvailable(this ILoggingAdapter logger, string typeName, int total, int bufferSize)
        {
            const string logMsg = "ShardRegion for [{0}] is using [{1}] of it's buffer capacity";
            logger.Warning(logMsg + " The coordinator might not be available. You might want to check cluster membership status.", typeName, 100 * total / bufferSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardMustNotBeEmptyDroppingMessage(this ILoggingAdapter logger, object message)
        {
            logger.Warning("Shard must not be empty, dropping message [{0}]", message.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MessageDoesNotHaveAnExtractorDefinedInShard(this ILoggingAdapter logger, string typeName, object message)
        {
            logger.Warning("Message does not have an extractor defined in shard [{0}] so it was ignored: {1}", typeName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoCoordinatorFoundToRegister(this ILoggingAdapter logger, int totalBufferSize)
        {
            logger.Warning("No coordinator found to register. Probably, no seed-nodes configured and manual cluster join not performed? Total [{0}] buffered messages.", totalBufferSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToRregisterToCoordinatorAtButNoAcknowledgement(this ILoggingAdapter logger, Cluster cluster, ActorSelection coordinator, IImmutableSet<Member> membersByAge, int totalBufferSize)
        {
            var coordinatorMessage = cluster.State.Unreachable.Contains(membersByAge.First()) ? $"Coordinator [{membersByAge.First()}] is unreachable." : $"Coordinator [{membersByAge.First()}] is reachable.";

            logger.Warning("Trying to register to coordinator at [{0}], but no acknowledgement. Total [{1}] buffered messages. [{2}]",
                coordinator != null ? coordinator.PathString : string.Empty, totalBufferSize, coordinatorMessage);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BufferIsFullDroppingMessageForEntity(this ILoggingAdapter logger, string id)
        {
            logger.Warning("Buffer is full, dropping message for entity [{0}]", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdMustNotBeEmptyDroppingMessage(this ILoggingAdapter logger, object message)
        {
            logger.Warning("Id must not be empty, dropping message [{0}]", message.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShardCanNotHandOffForAnotherShard(this ILoggingAdapter logger, IShard shard, PersistentShardCoordinator.HandOff handOff)
        {
            logger.Warning("Shard [{0}] can not hand off for another Shard [{1}]", shard.ShardId, handOff.Shard);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentSnapshotsMatchingDeletionFailure(this ILoggingAdapter logger, DeleteSnapshotsFailure m)
        {
            logger.Warning("Persistent snapshots matching {0} deletion failure: {1}", m.Criteria, m.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentMessagesToDeletionFailure(this ILoggingAdapter logger, DeleteMessagesFailure m)
        {
            logger.Warning("Persistent messages to {0} deletion failure: {1}", m.ToSequenceNr, m.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentSnapshotFailure(this ILoggingAdapter logger, SaveSnapshotFailure m)
        {
            logger.Warning("Persistent snapshot failure: {0}", m.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardSnapshotsMatchingDeletionFailure(this ILoggingAdapter logger, DeleteSnapshotsFailure m)
        {
            logger.Warning("PersistentShard snapshots matching [{0}] deletion failure: [{1}]", m.Criteria, m.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardMessagesToDeletionFailure(this ILoggingAdapter logger, DeleteMessagesFailure m)
        {
            logger.Warning("PersistentShard messages to [{0}] deletion failure: [{1}]", m.ToSequenceNr, m.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentShardSnapshotFailure(this ILoggingAdapter logger, SaveSnapshotFailure m)
        {
            logger.Warning("PersistentShard snapshot failure: [{0}]", m.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WhenUsingRememberEntitiesTheShardIdExtractorMustHandleShardRegionStartEntity(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, "When using remember-entities the shard id extractor must handle ShardRegion.StartEntity(id).");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheShardCoordinatorWasUnableToUpdateADistributedState(this ILoggingAdapter logger, ModifyFailure failure, PersistentShardCoordinator.IDomainEvent e)
        {
            logger.Error("The ShardCoordinator was unable to update a distributed state {0} with error {1} and event {2}.Coordinator will be restarted", failure.Key, failure.Cause, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheShardCoordinatorWasUnableToUpdateShardsDistributedState(this ILoggingAdapter logger, IWriteConsistency writeConsistency, PersistentShardCoordinator.IDomainEvent e)
        {
            logger.Error("The ShardCoordinator was unable to update shards distributed state within 'updating-state-timeout': {0} (retrying), event={1}", writeConsistency.Timeout, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheShardCoordinatorWasUnableToUpdateADistributedState(this ILoggingAdapter logger, IWriteConsistency writeConsistency, PersistentShardCoordinator.IDomainEvent e)
        {
            logger.Error("The ShardCoordinator was unable to update a distributed state within 'updating-state-timeout': {0} (retrying), event={1}", writeConsistency.Timeout, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheShardCoordinatorWasUnableToGetAllShardsState(this ILoggingAdapter logger, IReadConsistency readConsistency)
        {
            logger.Error("The ShardCoordinator was unable to get all shards state within 'waiting-for-state-timeout': {0} (retrying)", readConsistency.Timeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheShardCoordinatorWasUnableToGetAnInitialState(this ILoggingAdapter logger, IReadConsistency readConsistency)
        {
            logger.Error("The ShardCoordinator was unable to get an initial state within 'waiting-for-state-timeout': {0} (retrying)", readConsistency.Timeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheDDataShardWasUnableToUpdateStateWithError(this ILoggingAdapter logger, ModifyFailure failure, Shard.StateChange e)
        {
            logger.Error("The DDataShard was unable to update state with error {0} and event {1}. Shard will be restarted", failure.Cause, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheDDataShardWasUnableToUpdateState(this ILoggingAdapter logger, int retryCount, int maxUpdateAttempts, ClusterShardingSettings settings, Shard.StateChange e)
        {
            logger.Error("The DDataShard was unable to update state, attempt {0} of {1}, within 'updating-state-timeout'={2}, event={3}",
                retryCount, maxUpdateAttempts, settings.TunningParameters.UpdatingStateTimeout, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheDDataShardWasUnableToUpdateState(this ILoggingAdapter logger, int maxUpdateAttempts, ClusterShardingSettings settings, Shard.StateChange e)
        {
            logger.Error("The DDataShard was unable to update state after {0} attempts, within 'updating-state-timeout'={1}, event={2}. " +
                "Shard will be restarted after backoff.", maxUpdateAttempts, settings.TunningParameters.UpdatingStateTimeout, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheDDataShardWasUnableToGetAnInitialState(this ILoggingAdapter logger, ClusterShardingSettings settings)
        {
            logger.Error("The DDataShard was unable to get an initial state within 'waiting-for-state-timeout': {0}", settings.TunningParameters.WaitingForStateTimeout);
        }
    }
}
