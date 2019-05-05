// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence.Journal;
using static Akka.Persistence.Fsm.PersistentFSM;

namespace Akka.Persistence
{
    internal static class PersistenceLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DrainedPendingRecoveryPermitRequests(this ILoggingAdapter logger, int usedPermits, int maxPendingStats)
        {
            logger.Debug("Drained pending recovery permit requests, max in progress was [{0}], still [{1}] in progress", usedPermits + maxPendingStats, usedPermits);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceededMaxConcurrentRecoveries(this ILoggingAdapter logger, int maxPermits, IActorRef sender)
        {
            logger.Debug("Exceeded max-concurrent-recoveries [{0}]. First pending {1}", maxPermits, sender);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReplayCompleted(this ILoggingAdapter logger, object message)
        {
            logger.Debug($"Replay completed: {message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Replay(this ILoggingAdapter logger, ReplayedMessage r)
        {
            logger.Debug($"Replay: {r.Persistent}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Transition<TState, TData, TEvent>(this ILoggingAdapter logger, State<TState, TData, TEvent> oldState, State<TState, TData, TEvent> upcomingState)
        {
            logger.Debug("transition {0} -> {1}", oldState, upcomingState);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProcessingFromInState<TEvent, TState>(this ILoggingAdapter logger, TEvent fsmEvent, string srcStr, TState StateName)
        {
            logger.Debug("processing {0} from {1} in state {2}", fsmEvent, srcStr, StateName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CancellingTimer(this ILoggingAdapter logger, string name)
        {
            logger.Debug($"Cancelling timer {name}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SettingRepeatingTimer(this ILoggingAdapter logger, string name, object msg, TimeSpan timeout, bool repeat)
        {
            logger.Debug($"setting {(repeat ? "repeating" : "")} timer {name}/{timeout}: {msg}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AutoStartingSnapshotStore(this ILoggingAdapter logger, string id)
        {
            logger.Info("Auto-starting snapshot store `{0}`", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AutoStartingJournalPlugin(this ILoggingAdapter logger, string id)
        {
            logger.Info("Auto-starting journal plugin `{0}`", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FoundTargetAt(this ILoggingAdapter logger, PersistencePluginProxy.IPluginType pluginType, Address address)
        {
            logger.Info("Found target {0} at [{1}]", pluginType.Qualifier, address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InitializationTimedOut(this ILoggingAdapter logger, TimeSpan initTimeout, PersistencePluginProxy.IPluginType pluginType)
        {
            logger.Info("Initialization timed-out (after {0}s), use `PersistencePluginProxy.SetTargetLocation` or set `target-{1}-address`",
                initTimeout.TotalSeconds, pluginType.Qualifier);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToIdentifyTargetAt(this ILoggingAdapter logger, PersistencePluginProxy.IPluginType pluginType, ActorSelection sel)
        {
            logger.Info("Trying to identify target + {0} at {1}", pluginType.Qualifier, sel);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SettingTargetAddressTo(this ILoggingAdapter logger, PersistencePluginProxy.IPluginType pluginType, string targetAddress)
        {
            logger.Info("Setting target {0} address to {1}", pluginType.Qualifier, targetAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingTargetSnapshotStore(this ILoggingAdapter logger, string targetPluginId)
        {
            logger.Info("Starting target snapshot-store [{0}]", targetPluginId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingTargetJournal(this ILoggingAdapter logger, string targetPluginId)
        {
            logger.Info("Starting target journal [{0}]", targetPluginId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SavingSnapshotSequenceNumber(this ILoggingAdapter logger, long snapshotSequenceNr)
        {
            logger.Info($"Saving snapshot, sequence number [{snapshotSequenceNr}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoDefaultSnapshotStoreConfigured(this ILoggingAdapter logger)
        {
            logger.Warning("No default snapshot store configured! " +
                           "To configure a default snapshot-store plugin set the `akka.persistence.snapshot-store.plugin` key. " +
                           "For details see 'persistence.conf'");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteMessagesToSequenceNrForPersistenceId(this ILoggingAdapter logger, DeleteMessagesFailure deleteMessagesFailure, string persistenceId)
        {
            logger.Warning("Failed to DeleteMessages ToSequenceNr [{0}] for PersistenceId [{1}] due to: [{2}: {3}]", deleteMessagesFailure.ToSequenceNr, persistenceId, deleteMessagesFailure.Cause, deleteMessagesFailure.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteSnapshotsGivenCriteria(this ILoggingAdapter logger, DeleteSnapshotsFailure deleteSnapshotsFailure)
        {
            logger.Warning("Failed to DeleteSnapshots given criteria [{0}] due to: [{1}: {2}]", deleteSnapshotsFailure.Criteria, deleteSnapshotsFailure.Cause, deleteSnapshotsFailure.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteSnapshotGivenMetadata(this ILoggingAdapter logger, DeleteSnapshotFailure deleteSnapshotFailure)
        {
            logger.Warning("Failed to DeleteSnapshot given metadata [{0}] due to: [{1}: {2}]", deleteSnapshotFailure.Metadata, deleteSnapshotFailure.Cause, deleteSnapshotFailure.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToSaveSnapshotGivenMetadata(this ILoggingAdapter logger, SaveSnapshotFailure saveSnapshotFailure)
        {
            logger.Warning("Failed to SaveSnapshot given metadata [{0}] due to: [{1}: {2}]", saveSnapshotFailure.Metadata, saveSnapshotFailure.Cause, saveSnapshotFailure.Cause.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidURLProvidedForTarget(this ILoggingAdapter logger, PersistencePluginProxy.IPluginType pluginType, string targetAddress)
        {
            logger.Warning("Invalid URL provided for target {0} address: {1}", pluginType.Qualifier, targetAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnhandledEventInState<TData, TState>(this ILoggingAdapter logger, FSMBase.Event<TData> @event, TState stateName)
        {
            logger.Warning("unhandled event {0} in state {1}", @event.FsmEvent, stateName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorLoadingSnapshotRemainingAttempts(this ILoggingAdapter logger, Exception ex, SnapshotMetadata last, int count)
        {
            logger.Error(ex, $"Error loading snapshot [{last}], remaining attempts: [{count}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedPersistencePluginProxyRequest(this ILoggingAdapter logger, TimeoutException exception)
        {
            logger.Error(exception, "Failed PersistencePluginProxyRequest: {0}", exception.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TerminatingDueToFailure(this ILoggingAdapter logger, Exception exc)
        {
            logger.Error(exc, "terminating due to Failure");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TerminatingFailure(this ILoggingAdapter logger, FSMBase.Failure failure)
        {
            logger.Error(failure.Cause.ToString());
        }
    }
}
