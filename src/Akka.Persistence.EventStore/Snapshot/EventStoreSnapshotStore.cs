using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Event;
using Akka.Extension.EventStore;
using Akka.Persistence.Snapshot;
using CuteAnt.Text;
using EventStore.ClientAPI;

namespace Akka.Persistence.EventStore.Snapshot
{
    /// <summary>TBD</summary>
    public class EventStoreSnapshotStore : SnapshotStore
    {
        #region ** class SelectedSnapshotResult **

        private sealed class SelectedSnapshotResult
        {
            public readonly long EventNumber;
            public readonly SelectedSnapshot Snapshot;

            public SelectedSnapshotResult(long eventNumver, SelectedSnapshot snapshot)
            {
                EventNumber = eventNumver;
                Snapshot = snapshot;
            }

            public static readonly SelectedSnapshotResult Empty = new SelectedSnapshotResult(-1L, null);
        }

        #endregion

        private readonly IEventStoreConnection2 _conn;
        private readonly IEventAdapter _eventAdapter;
        private readonly EventStoreSnapshotSettings _settings;
        private readonly ILoggingAdapter _log;

        /// <summary>TBD</summary>
        public EventStoreSnapshotStore()
        {
            var system = Context.System;
            _settings = EventStorePersistence.Get(system).SnapshotStoreSettings;
            _log = Context.GetLogger();
            _conn = EventStoreConnector.Get(system).Connection;
            _eventAdapter = _conn.EventAdapter;
        }

        /// <inheritdoc />
        protected override async Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            var streamName = GetStreamName(persistenceId);
            DateTime? timestamp = criteria.MaxTimeStamp;
            if (timestamp == DateTime.MinValue || timestamp == DateTime.MaxValue) { timestamp = null; }
            var result = await FindSnapshotAsync(streamName, criteria.MaxSequenceNr, timestamp);
            return result.Snapshot;
        }

        /// <inheritdoc />
        protected override async Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            var streamName = GetStreamName(metadata.PersistenceId);
            var context = new Dictionary<string, object>(5)
            {
                [MetadataConstants.PersistenceId] = metadata.PersistenceId,
                //[MetadataConstants.OccurredOn] = DateTime.UtcNow, // EventStore已有[Created Date]
                [MetadataConstants.SequenceNr] = metadata.SequenceNr,
                [MetadataConstants.Timestamp] = metadata.Timestamp,
                [MetadataConstants.JournalType] = JournalTypes.SnapshotJournal
            };
            try
            {
                var writeResult = await _conn.SendEventAsync(streamName, snapshot, context).ConfigureAwait(false);
                if (_log.IsDebugEnabled) _log.SnapshotCommittedAtLogPosition(metadata, writeResult);
            }
            catch (Exception e)
            {
                if (_log.IsWarningEnabled) _log.FailedToMakeASnapshot(metadata, e);
            }
        }

        /// <inheritdoc />
        protected override async Task DeleteAsync(SnapshotMetadata metadata)
        {
            var streamName = GetStreamName(metadata.PersistenceId);
            var m = await _conn.GetStreamMetadataAsync(streamName).ConfigureAwait(false);
            if (m.IsStreamDeleted) { return; }

            var streamMetadata = m.StreamMetadata.Copy();
            DateTime? timestamp = metadata.Timestamp;
            if (timestamp == DateTime.MinValue || timestamp == DateTime.MaxValue) { timestamp = null; }

            var result = await FindSnapshotAsync(streamName, metadata.SequenceNr, timestamp);

            if (result.Snapshot == null) { return; }

            streamMetadata = streamMetadata.SetTruncateBefore(result.EventNumber + 1);
            await _conn.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, streamMetadata.Build()).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            var streamName = GetStreamName(persistenceId);
            var m = await _conn.GetStreamMetadataAsync(streamName).ConfigureAwait(false);
            if (m.IsStreamDeleted) { return; }

            var streamMetadata = m.StreamMetadata.Copy();

            DateTime? timestamp = criteria.MaxTimeStamp;
            if (timestamp == DateTime.MinValue || timestamp == DateTime.MaxValue) { timestamp = null; }

            var result = await FindSnapshotAsync(streamName, criteria.MaxSequenceNr, timestamp);

            if (result.Snapshot == null) { return; }

            streamMetadata = streamMetadata.SetTruncateBefore(result.EventNumber + 1);
            await _conn.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, streamMetadata.Build()).ConfigureAwait(false);
        }

        private async Task<SelectedSnapshotResult> FindSnapshotAsync(string streamName, long maxSequenceNr, DateTime? maxTimeStamp)
        {
            var readResult = await _conn.GetLastEventAsync(streamName, false).ConfigureAwait(false);
            if (readResult.Status != EventReadStatus.Success)
            {
                return SelectedSnapshotResult.Empty;
            }

            var resolvedEvent = readResult.Event.Value.Event;
            var snapshot = Adapt(resolvedEvent);
            var snapshotMetadata = snapshot.Metadata;
            if ((!maxTimeStamp.HasValue || snapshotMetadata.Timestamp <= maxTimeStamp.Value) && snapshotMetadata.SequenceNr <= maxSequenceNr)
            {
                return new SelectedSnapshotResult(resolvedEvent.EventNumber, snapshot);
            }

            var from = resolvedEvent.EventNumber - 1L;
            if (from <= 0L) // 当前只有一条记录
            {
                return SelectedSnapshotResult.Empty;
            }

            return await LoopSnapshotAsync(streamName, from, maxSequenceNr, maxTimeStamp);
        }

        private async Task<SelectedSnapshotResult> LoopSnapshotAsync(string streamName, long from, long maxSequenceNr, DateTime? maxTimeStamp)
        {
            SelectedSnapshotResult snapshotResult = null;

            StreamEventsSlice<object> slice = null;
            var take = _settings.ReadBatchSize;
            do
            {
                if (from <= 0L) break;

                take = from > take ? take : (int)from;
                slice = await _conn.GetStreamEventsBackwardAsync(streamName, from, take, false).ConfigureAwait(false);
                from -= take;

                snapshotResult = slice.Events
                                .Select(e => new SelectedSnapshotResult(e.Event.EventNumber, Adapt(e.Event)))
                                .FirstOrDefault(s =>
                                        (!maxTimeStamp.HasValue || s.Snapshot.Metadata.Timestamp <= maxTimeStamp.Value) &&
                                        s.Snapshot.Metadata.SequenceNr <= maxSequenceNr);
            } while (snapshotResult == null && slice.Status == SliceReadStatus.Success);

            return snapshotResult ?? SelectedSnapshotResult.Empty;
        }

        private static SelectedSnapshot Adapt(RecordedEvent<object> resolvedEvent)
        {
            var fullEvent = resolvedEvent.FullEvent;
            var eventDescriptor = fullEvent.Descriptor;
            var persistenceId = eventDescriptor.GetValue<string>(MetadataConstants.PersistenceId);
            var sequenceNr = eventDescriptor.GetValue<long>(MetadataConstants.SequenceNr);
            var timestamp = eventDescriptor.GetValue<DateTime>(MetadataConstants.Timestamp);

            var snapshotMetadata = new SnapshotMetadata(persistenceId, sequenceNr, timestamp);
            return new SelectedSnapshot(snapshotMetadata, fullEvent.Value);
        }

        private string GetStreamName(string persistenceId)
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append(_settings.Prefix);
            sb.Append(persistenceId);
            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}