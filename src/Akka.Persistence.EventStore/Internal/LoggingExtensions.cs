using System;
using System.Runtime.CompilerServices;
using Akka.Event;
using EventStore.ClientAPI;

namespace Akka.Persistence.EventStore
{
    internal static class PersistenceEventStoreLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SnapshotCommittedAtLogPosition(this ILoggingAdapter logger, SnapshotMetadata metadata, in WriteResult writeResult)
        {
            logger.Debug("Snapshot for `{0}` committed at log position (commit: {1}, prepare: {2})",
                metadata.PersistenceId,
                writeResult.LogPosition.CommitPosition,
                writeResult.LogPosition.PreparePosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToMakeASnapshot(this ILoggingAdapter logger, SnapshotMetadata metadata, Exception e)
        {
            logger.Warning("Failed to make a snapshot for {0}, failed with message `{1}`",
                metadata.PersistenceId,
                e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorReplayingMessages(this ILoggingAdapter logger, Exception e, string persistenceId)
        {
            logger.Error(e, "Error replaying messages for: {0}", persistenceId);
        }
    }
}
