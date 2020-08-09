// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData.Internal;
using Akka.Event;

namespace Akka.DistributedData
{
    internal static class DistributedDataLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WriteAcksRemainingDoneWhen(this ILoggingAdapter logger, int count, int done)
        {
            logger.Debug("write acks remaining: {0}, done when: {1}", count, done);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadAcksRemainingDoneWhenCurrentState(this ILoggingAdapter logger, int count, int done, DataEnvelope result)
        {
            logger.Debug("read acks remaining: {0}, done when: {1}, current state: {2}", count, done, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingToPrimaryReplica(this ILoggingAdapter logger, Read read, ActorSelection replica)
        {
            logger.Debug("Sending {0} to primary replica {1}", read, replica);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PerformPruningOfFromTo(this ILoggingAdapter logger, string key, UniqueAddress removed, UniqueAddress selfUniqueAddress)
        {
            logger.Debug("Perform pruning of [{0}] from [{1}] to [{2}]", key, removed, selfUniqueAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InitiatingPruningOfWithData(this ILoggingAdapter logger, UniqueAddress removed, string key)
        {
            logger.Debug("Initiating pruning of {0} with data {1}", removed, key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AddingRemovedNodeFromData(this ILoggingAdapter logger, UniqueAddress node)
        {
            logger.Debug("Adding removed node [{0}] from data", node);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AddingRemovedNodeFromMemberRemoved(this ILoggingAdapter logger, Member m)
        {
            logger.Debug("Adding removed node [{0}] from MemberRemoved", m.UniqueAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedGossipFrom(this ILoggingAdapter logger, IActorRef sender, IImmutableDictionary<string, DataEnvelope> updatedData)
        {
            logger.Debug("Received gossip from [{0}], containing [{1}]", sender.Path.Address, string.Join(", ", updatedData.Keys));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingGossipStatusTo(this ILoggingAdapter logger, IActorRef sender, ImmutableHashSet<string> myMissingKeys)
        {
            logger.Debug("Sending gossip status to {0}, requesting missing {1}", sender.Path.Address, string.Join(", ", myMissingKeys));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingGossipTo(this ILoggingAdapter logger, IActorRef sender, string[] keys)
        {
            logger.Debug("Sending gossip to [{0}]: {1}", sender.Path.Address, string.Join(", ", keys));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedGossipStatusFrom(this ILoggingAdapter logger, IActorRef sender, int chunk, int totChunks, IImmutableDictionary<string, byte[]> otherDigests)
        {
            logger.Debug("Received gossip status from [{0}], chunk {1}/{2} containing [{3}]", sender.Path.Address, chunk + 1, totChunks, string.Join(", ", otherDigests.Keys));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SkippingDeltaPropagationBecauseThatNodeHasBeenRemoved(this ILoggingAdapter logger, UniqueAddress from)
        {
            logger.Debug("Skipping DeltaPropagation from [{0}] because that node has been removed", from.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedDeltaPropagationFromContaining(this ILoggingAdapter logger, UniqueAddress from, ImmutableDictionary<string, Delta> deltas)
        {
            logger.Debug("Received DeltaPropagation from [{0}], containing [{1}]", from.Address,
                string.Join(", ", deltas.Select(d => $"{d.Key}:{d.Value.FromSeqNr}->{d.Value.ToSeqNr}")));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ApplyingDeltaPropagationFromWithSequenceNumbers(this ILoggingAdapter logger, UniqueAddress from, string key, Delta delta, long currentSeqNr)
        {
            logger.Debug("Applying DeltaPropagation from [{0}] for [{1}] with sequence numbers [{2}-{3}], current was [{4}]", from.Address, key, delta.FromSeqNr, delta.ToSeqNr, currentSeqNr);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SkippingDeltaPropagationBecauseMissingDeltas(this ILoggingAdapter logger, UniqueAddress from, string key, long currentSeqNr, Delta delta)
        {
            logger.Debug("Skipping DeltaPropagation from [{0}] for [{1}] because missing deltas between [{2}-{3}]", from.Address, key, currentSeqNr + 1, delta.FromSeqNr - 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SkippingDeltaPropagationBecauseToSeqNrAlreadyHandled(this ILoggingAdapter logger, UniqueAddress from, string key, Delta delta, long currentSeqNr)
        {
            logger.Debug("Skipping DeltaPropagation from [{0}] for [{1}] because toSeqNr [{2}] already handled [{3}]", from.Address, key, delta.FromSeqNr, currentSeqNr);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedUpdateForDeletedKey(this ILoggingAdapter logger, IKey key)
        {
            logger.Debug("Received update for deleted key {0}", key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedUpdateForKey(this ILoggingAdapter logger, IKey key)
        {
            logger.Debug("Received Update for key {0}", key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedUpdateForKey(this ILoggingAdapter logger, IKey key, Exception ex)
        {
            logger.Debug("Received update for key {0}, failed {1}", key, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedGetForKey(this ILoggingAdapter logger, IKey key, DataEnvelope localValue, IReadConsistency consistency)
        {
            logger.Debug("Received get for key {0}, local value {1}, consistency: {2}", key.Id, localValue, consistency);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LoadingEntriesFromDurableStoreTookMs(this ILoggingAdapter logger, int count, DateTime startTime)
        {
            logger.Debug("Loading {0} entries from durable store took {1} ms", count, (DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReplicatorPointsToDeadLetters(this ILoggingAdapter logger)
        {
            logger.Warning("Replicator points to dead letters: Make sure the cluster node is not terminated and has the proper role!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldnotMergeDueTo(this ILoggingAdapter logger, string key, ArgumentException e)
        {
            logger.Warning("Couldn't merge [{0}] due to: {1}", key, e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldnotProcessDeltaPropagation(this ILoggingAdapter logger, UniqueAddress from, Exception e)
        {
            logger.Warning("Couldn't process DeltaPropagation from [{0}] due to {1}", from, e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StoppingDistributedDataReplicatorBecauseDurableStoreTerminated(this ILoggingAdapter logger)
        {
            logger.Error("Stopping distributed-data replicator because durable store terminated");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StoppingDistributedDataReplicatorDueToLoadOrStartupFailureInDurableStore(this ILoggingAdapter logger, Exception e)
        {
            logger.Error(e,
                "Stopping distributed-data Replicator due to load or startup failure in durable store, caused by: {0}",
                e.Message);
        }
    }
}
