using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Extension.EventStore;
using Akka.Persistence.Journal;
using EventStore.ClientAPI;
using IEsEventAdapter = EventStore.ClientAPI.IEventAdapter;

namespace Akka.Persistence.EventStore.Journal
{
    public class EventStoreJournal : AsyncWriteJournal
    {
        private readonly IEventStoreConnection2 _conn;
        private readonly IEsEventAdapter _eventAdapter;
        private readonly EventStoreJournalSettings _settings;
        private readonly ILoggingAdapter _log;

        public EventStoreJournal()
        {
            var system = Context.System;
            _settings = EventStorePersistence.Get(system).JournalSettings;
            _log = Context.GetLogger();
            _conn = EventStoreConnector.Get(system).Connection;
            _eventAdapter = _conn.EventAdapter;
        }

        /// <inheritdoc />
        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            try
            {
                var readResult = await _conn.GetLastEventAsync(persistenceId, false).ConfigureAwait(false);

                long sequence = 0L;

                if (readResult.Status == EventReadStatus.Success)
                {
                    var resolvedEvent = readResult.Event.Value.Event;
                    var adapted = Adapt(resolvedEvent);
                    sequence = adapted.SequenceNr;
                }
                else
                {
                    var metadata = await _conn.GetStreamMetadataAsync(persistenceId).ConfigureAwait(false);
                    if (metadata.StreamMetadata.TruncateBefore != null)
                    {
                        sequence = metadata.StreamMetadata.TruncateBefore.Value;
                    }
                }

                return sequence;
            }
            catch (Exception e)
            {
                _log.Error(e, e.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task ReplayMessagesAsync(IActorContext context, string persistenceId,
            long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> recoveryCallback)
        {
            try
            {
                if (toSequenceNr < fromSequenceNr || max == 0L) { return; }

                if (fromSequenceNr == toSequenceNr) { max = 1L; }

                if (toSequenceNr > fromSequenceNr && max == toSequenceNr)
                {
                    max = toSequenceNr - fromSequenceNr + 1L;
                }

                var count = 0L;

                var start = fromSequenceNr <= 0L ? 0L : fromSequenceNr - 1L;

                var localBatchSize = _settings.ReadBatchSize;

                StreamEventsSlice<object> slice;
                do
                {
                    if (max == long.MaxValue && toSequenceNr > fromSequenceNr)
                    {
                        max = toSequenceNr - fromSequenceNr + 1L;
                    }

                    if (max < localBatchSize) { localBatchSize = (int)max; }

                    slice = await _conn.GetStreamEventsForwardAsync(persistenceId, start, localBatchSize, false).ConfigureAwait(false);

                    foreach (var @event in slice.Events)
                    {
                        var representation = Adapt(@event.Event, s =>
                        {
                            //TODO: Is this correct?
                            var selection = context.ActorSelection(s);
                            return selection.Anchor;
                        });

                        recoveryCallback(representation);
                        count++;

                        if (count == max) { return; }
                    }

                    start = slice.NextEventNumber;
                } while (!slice.IsEndOfStream);
            }
            catch (Exception e)
            {
                _log.ErrorReplayingMessages(e, persistenceId);
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> atomicWrites)
        {
            var results = new List<Exception>();
            foreach (var atomicWrite in atomicWrites)
            {
                var persistentMessages = (IImmutableList<IPersistentRepresentation>)atomicWrite.Payload;
                var persistenceId = atomicWrite.PersistenceId;
                try
                {
                    Adapt(_eventAdapter, persistentMessages, out var eventDatas, out var eventMetas);
                    await _conn.SendEventsAsync(persistenceId, eventDatas, eventMetas).ConfigureAwait(false);
                    results.Add(null);
                }
                catch (Exception e)
                {
                    results.Add(TryUnwrapException(e));
                }
            }

            return results.ToImmutableList();
        }

        /// <inheritdoc />
        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            if (toSequenceNr == long.MaxValue)
            {
                var readResult = await _conn.GetLastEventAsync(persistenceId, false).ConfigureAwait(false);
                if (readResult.Status == EventReadStatus.Success)
                {
                    var highestEventPosition = readResult.Event.Value.Event.EventNumber;
                    await _conn.SetStreamMetadataAsync(persistenceId, ExpectedVersion.Any,
                        StreamMetadata.Create(truncateBefore: highestEventPosition + 1)).ConfigureAwait(false);
                }
            }
            else
            {
                await _conn.SetStreamMetadataAsync(persistenceId, ExpectedVersion.Any,
                    StreamMetadata.Create(truncateBefore: toSequenceNr)).ConfigureAwait(false);
            }
        }

        private static void Adapt(IEsEventAdapter eventAdapter, IImmutableList<IPersistentRepresentation> persistentMessages, out object[] eventDatas, out IEventMetadata[] eventMetas)
        {
            eventDatas = new object[persistentMessages.Count];
            eventMetas = new IEventMetadata[persistentMessages.Count];
            for (var idx = 0; idx < persistentMessages.Count; idx++)
            {
                var persistentMessage = persistentMessages[idx];

                eventDatas[idx] = persistentMessage.Payload;
                var metadata = new Dictionary<string, object>(7)
                {
                    [MetadataConstants.PersistenceId] = persistentMessage.PersistenceId,
                    [MetadataConstants.OccurredOn] = DateTime.UtcNow,
                    [MetadataConstants.Manifest] = persistentMessage.Manifest,
                    [MetadataConstants.SenderPath] = persistentMessage.Sender?.Path?.ToStringWithoutAddress() ?? string.Empty,
                    [MetadataConstants.SequenceNr] = persistentMessage.SequenceNr,
                    [MetadataConstants.WriterGuid] = persistentMessage.WriterGuid,
                    [MetadataConstants.JournalType] = JournalTypes.WriteJournal
                };

                eventMetas[idx] = eventAdapter.ToEventMetadata(metadata);
            }
        }

        private static IPersistentRepresentation Adapt(RecordedEvent<object> resolvedEvent, Func<string, IActorRef> actorSelection = null)
        {
            var fullEvent = resolvedEvent.FullEvent;
            var eventDescriptor = fullEvent.Descriptor;

            var journalType = eventDescriptor.GetValue<string>(MetadataConstants.JournalType, null);
            if (!string.Equals(JournalTypes.WriteJournal, journalType, StringComparison.Ordinal))
            {
                // since we are reading from "$streams" stream, there could be other kind of event linked, e.g. snapshot
                // events, since IEventAdapter is storing in metadata "journalType" using Adopt while event
                // should be adopted to EventStore message EventData.
                // Return null in case journalType != "WriteJournal" which means some other extension stored that event in
                // database but $streams projection picked up since it is at position 0
                return null;
            }
            var stream = eventDescriptor.GetValue<string>(MetadataConstants.PersistenceId);
            var manifest = eventDescriptor.GetValue<string>(MetadataConstants.Manifest);
            var sequenceNr = eventDescriptor.GetValue<long>(MetadataConstants.SequenceNr);
            var senderPath = eventDescriptor.GetValue<string>(MetadataConstants.SenderPath);

            var sender = actorSelection?.Invoke(senderPath);

            return new Persistent(fullEvent.Value, sequenceNr, stream, manifest, false, sender);
        }
    }
}