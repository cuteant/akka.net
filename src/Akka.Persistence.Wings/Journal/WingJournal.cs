using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Persistence.Journal;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using CuteAnt;
using CuteAnt.AsyncEx;
using CuteAnt.Text;
using CuteAnt.Wings;

namespace Akka.Persistence.Wings.Journal
{
    /// <summary>TBD</summary>
    public class WingJournal : AsyncWriteJournal, IWithUnboundedStash
    {
        #region messages

        private sealed class Initialized
        {
            public static readonly Initialized Instance = new Initialized();
            private Initialized() { }
        }

        #endregion

        public static readonly WingPersistence Extension = WingPersistence.Get(Context.System);

        private static readonly Type s_eventEntityType = typeof(JournalEvent);
        private static readonly Type s_metaEntityType = typeof(JournalMetadata);

        private readonly Dictionary<string, ISet<IActorRef>> _persistenceIdSubscribers = new Dictionary<string, ISet<IActorRef>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ISet<IActorRef>> _tagSubscribers = new Dictionary<string, ISet<IActorRef>>(StringComparer.Ordinal);

        private readonly CancellationTokenSource _pendingRequestsCancellation;
        private readonly WingJournalSettings _settings;
        private readonly IWingsConnectionFactory _factory;

        private readonly Akka.Serialization.Serialization _serialization;
        private readonly string _defaultSerializer;
        private readonly ITimestampProvider _timestampProvider;
        private readonly IShardingOption _shardingOption;

        private ILoggingAdapter _log;

        /// <summary>TBD</summary>
        /// <param name="journalConfig">TBD</param>
        public WingJournal(Config journalConfig)
        {
            var config = journalConfig.WithFallback(Extension.DefaultJournalConfig);

            _settings = new WingJournalSettings(config);

            if (_settings.Factory == null)
            {
                _factory = WingsConnection.Factory.GetFactory(_settings.ConnectionName);
            }
            else
            {
                _factory = _settings.Factory;
            }

            _shardingOption = _settings;
            _defaultSerializer = config.GetString("serializer");
            _timestampProvider = GetTimestampProvider(config.GetString("timestamp-provider"));

            _serialization = Context.System.Serialization;
            _pendingRequestsCancellation = new CancellationTokenSource();
        }

        /// <summary>TBD</summary>
        public IStash Stash { get; set; }

        /// <summary>TBD</summary>
        protected bool HasPersistenceIdSubscribers => _persistenceIdSubscribers.Count != 0;

        /// <summary>TBD</summary>
        protected bool HasTagSubscribers => _tagSubscribers.Count != 0;

        /// <summary>Returns a HOCON config path to associated journal.</summary>
        protected string JournalConfigPath => WingJournalSettings.ConfigPath;

        /// <summary>System logger.</summary>
        protected ILoggingAdapter Log => _log ?? (_log = Context.GetLogger());

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool ReceivePluginInternal(object message)
        {
            return message.Match()
                .With<ReplayTaggedMessages>(replay =>
                {
                    ReplayTaggedMessagesAsync(replay)
                    .PipeTo(replay.ReplyTo, success: h => new RecoverySuccess(h), failure: e => new ReplayMessagesFailure(e));
                })
                .With<SubscribePersistenceId>(subscribe =>
                {
                    AddPersistenceIdSubscriber(Sender, subscribe.PersistenceId);
                    Context.Watch(Sender);
                })
                .With<SubscribeTag>(subscribe =>
                {
                    AddTagSubscriber(Sender, subscribe.Tag);
                    Context.Watch(Sender);
                })
                .With<Terminated>(terminated => RemoveSubscriber(terminated.ActorRef))
                .WasHandled;
        }

        /// <summary>
        /// Asynchronously writes all persistent <paramref name="messages"/> inside SQL Server database.
        /// 
        /// Specific table used for message persistence may be defined through configuration within 
        /// 'akka.persistence.journal.sql-server' scope with 'schema-name' and 'table-name' keys.
        /// </summary>
        /// <param name="messages">TBD</param>
        /// <exception cref="InvalidOperationException">TBD</exception>
        /// <returns>TBD</returns>
        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var persistenceIds = new HashSet<string>(StringComparer.Ordinal);
            var allTags = new HashSet<string>(StringComparer.Ordinal);

            var writeTasks = messages.Select(async message =>
            {
                using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_settings.ConnectionName))
                {
                    WingsTransaction trans = null;
                    try
                    {
                        dbConn.Open();
                        trans = dbConn.OpenTransaction();

                        var eventToTags = new Dictionary<IPersistentRepresentation, IImmutableSet<string>>();
                        var persistentMessages = ((IImmutableList<IPersistentRepresentation>)message.Payload).ToArray();
                        for (int i = 0; i < persistentMessages.Length; i++)
                        {
                            var p = persistentMessages[i];
                            if (p.Payload is Tagged tagged)
                            {
                                persistentMessages[i] = p = p.WithPayload(tagged.Payload);
                                if (tagged.Tags.Count != 0)
                                {
                                    allTags.UnionWith(tagged.Tags);
                                    eventToTags.Add(p, tagged.Tags);
                                }
                                else eventToTags.Add(p, ImmutableHashSet<string>.Empty);
                            }
                            else eventToTags.Add(p, ImmutableHashSet<string>.Empty);
                        }

                        using (var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                        {
                            var journalEvts = ToJournalEvents(eventToTags);
                            await dbConn.InsertAllAsync<JournalEvent>(journalEvts, cancellationToken.Token);
                        }

                        trans.Commit();
                    }
                    finally
                    {
                        trans?.Dispose();
                    }
                }
            }).ToArray();

            var result = await Task<IImmutableList<Exception>>
                .Factory
                .ContinueWhenAll(writeTasks,
                    tasks => tasks.Select(t => t.IsFaulted ? TryUnwrapException(t.Exception) : null).ToImmutableList());

            if (HasPersistenceIdSubscribers)
            {
                foreach (var persistenceId in persistenceIds)
                {
                    NotifyPersistenceIdChange(persistenceId);
                }
            }

            if (HasTagSubscribers && allTags.Count != 0)
            {
                foreach (var tag in allTags)
                {
                    NotifyTagChange(tag);
                }
            }

            return result;
        }

        /// <summary>Replays all events with given tag withing provided boundaries from current database.</summary>
        /// <param name="replay">TBD</param>
        /// <returns>TBD</returns>
        protected virtual async Task<long> ReplayTaggedMessagesAsync(ReplayTaggedMessages replay)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_shardingOption.ConnectionName))
            {
                dbConn.Open();

                var fromOffset = replay.FromOffset;
                var tag = replay.Tag;
                var take = (int)Math.Min(replay.ToOffset - fromOffset, replay.Max);
                using (var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                {
                    var evtDtos = await dbConn.SelectAsync<JournalEventDTO, JournalEvent>(
                    predicate: _ => _.Id > fromOffset && _.Tags.Contains(tag),
                    orderByKeysSelector: _ => _.Id,
                    rows: take,
                    token: cancellationToken.Token);

                    var maxSequenceNr = 0L;
                    foreach (var evtDto in evtDtos)
                    {
                        var persistent = ToPersistent(evtDto);
                        var ordering = evtDto.Id;
                        maxSequenceNr = Math.Max(maxSequenceNr, persistent.SequenceNr);

                        foreach (var adapted in AdaptFromJournal(persistent))
                        {
                            replay.ReplyTo.Tell(new ReplayedTaggedMessage(adapted, tag, ordering), ActorRefs.NoSender);
                        }
                    }
                    return maxSequenceNr;
                }
            }
        }

        /// <summary>TBD</summary>
        /// <param name="context">TBD</param>
        /// <param name="persistenceId">TBD</param>
        /// <param name="fromSequenceNr">TBD</param>
        /// <param name="toSequenceNr">TBD</param>
        /// <param name="max">TBD</param>
        /// <param name="recoveryCallback">TBD</param>
        /// <returns>TBD</returns>
        public override async Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr,
            long max, Action<IPersistentRepresentation> recoveryCallback)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_shardingOption.ConnectionName))
            {
                dbConn.Open();

                using (var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                {
                    var evtDtos = await dbConn.SelectAsync<JournalEventDTO, JournalEvent>(
                    predicate: _ => _.PersistenceId == persistenceId && _.SequenceNr >= fromSequenceNr && _.SequenceNr <= toSequenceNr,
                    orderByKeysSelector: _ => _.SequenceNr,
                    rows: (int)max,
                    token: cancellationToken.Token);

                    foreach (var evtDto in evtDtos)
                    {
                        recoveryCallback(ToPersistent(evtDto));
                    }
                }
            }
        }

        /// <summary>TBD</summary>
        protected override void PreStart()
        {
            base.PreStart();
            Initialize().PipeTo(Self);
            BecomeStacked(WaitingForInitialization);
        }

        /// <summary>TBD</summary>
        protected override void PostStop()
        {
            base.PostStop();
            _pendingRequestsCancellation.Cancel();
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected bool WaitingForInitialization(object message)
        {
            switch (message)
            {
                case Initialized _:
                    UnbecomeStacked();
                    Stash.UnstashAll();
                    break;

                case Failure fail:
                    Log.Error(fail.Exception, "Failure during {0} initialization.", Self);
                    Context.Stop(Self);
                    break;

                default:
                    Stash.Stash();
                    break;
            }
            return true;
        }

        private async Task<object> Initialize()
        {
            try
            {
                //DbShardingManager.InstallOrUpgrade(s_eventEntityType, _settings);
                //DbShardingManager.InstallOrUpgrade(s_metaEntityType, _settings);
                using (var db = _factory.CreateDbConnection())
                {
                    db.Open();
                    db.CheckSchema(s_eventEntityType);
                    db.CheckSchema(s_metaEntityType);
                }

                return Initialized.Instance;
            }
            catch (Exception e)
            {
                await TaskConstants.Completed;
                return new Failure { Exception = e };
            }
        }

        /// <summary>TBD</summary>
        /// <param name="subscriber">TBD</param>
        public void RemoveSubscriber(IActorRef subscriber)
        {
            var pidSubscriptions = _persistenceIdSubscribers.Values.Where(x => x.Contains(subscriber));
            foreach (var subscription in pidSubscriptions)
                subscription.Remove(subscriber);

            var tagSubscriptions = _tagSubscribers.Values.Where(x => x.Contains(subscriber));
            foreach (var subscription in tagSubscriptions)
                subscription.Remove(subscriber);
        }

        /// <summary>TBD</summary>
        /// <param name="subscriber">TBD</param>
        /// <param name="tag">TBD</param>
        public void AddTagSubscriber(IActorRef subscriber, string tag)
        {
            if (!_tagSubscribers.TryGetValue(tag, out var subscriptions))
            {
                subscriptions = new HashSet<IActorRef>(ActorRefComparer.Instance);
                _tagSubscribers.Add(tag, subscriptions);
            }

            subscriptions.Add(subscriber);
        }

        /// <summary>TBD</summary>
        /// <param name="subscriber">TBD</param>
        /// <param name="persistenceId">TBD</param>
        public void AddPersistenceIdSubscriber(IActorRef subscriber, string persistenceId)
        {
            if (!_persistenceIdSubscribers.TryGetValue(persistenceId, out var subscriptions))
            {
                subscriptions = new HashSet<IActorRef>(ActorRefComparer.Instance);
                _persistenceIdSubscribers.Add(persistenceId, subscriptions);
            }

            subscriptions.Add(subscriber);
        }

        private void NotifyPersistenceIdChange(string persistenceId)
        {
            if (_persistenceIdSubscribers.TryGetValue(persistenceId, out var subscribers))
            {
                var changed = new EventAppended(persistenceId);
                foreach (var subscriber in subscribers)
                    subscriber.Tell(changed);
            }
        }

        private void NotifyTagChange(string tag)
        {
            if (_tagSubscribers.TryGetValue(tag, out var subscribers))
            {
                var changed = new TaggedEventAppended(tag);
                foreach (var subscriber in subscribers)
                    subscriber.Tell(changed);
            }
        }

        /// <summary>Asynchronously deletes all persisted messages identified by provided <paramref name="persistenceId"/>
        /// up to provided message sequence number (inclusive).</summary>
        /// <param name="persistenceId">TBD</param>
        /// <param name="toSequenceNr">TBD</param>
        /// <returns>TBD</returns>
        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_shardingOption.ConnectionName))
            {
                WingsTransaction trans = null;
                try
                {
                    dbConn.Open();
                    trans = dbConn.OpenTransaction();

                    using (var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                    {
                        var highestSeqNr = await ReadHighestSequenceNr(dbConn, persistenceId, cancellationToken.Token);

                        await dbConn.DeleteAsync<JournalEvent>(_ => _.PersistenceId == persistenceId && _.SequenceNr <= toSequenceNr, cancellationToken.Token);

                        if (highestSeqNr <= toSequenceNr)
                        {
                            await dbConn.InsertAsync<JournalMetadata>(new JournalMetadata
                            {
                                Id = CombGuid.NewComb(),
                                PersistenceId = persistenceId,
                                SequenceNr = highestSeqNr
                            }, token: cancellationToken.Token);
                        }
                    }

                    trans.Commit();
                }
                finally
                {
                    trans?.Dispose();
                }
            }
        }

        /// <summary>Asynchronously reads a highest sequence number of the event stream related with provided <paramref name="persistenceId"/>.</summary>
        /// <param name="persistenceId">TBD</param>
        /// <param name="fromSequenceNr">TBD</param>
        /// <returns>TBD</returns>
        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_shardingOption.ConnectionName))
            {
                dbConn.Open();

                using (var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                {
                    return await ReadHighestSequenceNr(dbConn, persistenceId, cancellationToken.Token);
                }
            }
        }

        private IPersistentRepresentation ToPersistent(JournalEventDTO evtDto)
        {
            var manifest = evtDto.Manifest;
            var deserializedMsg = new Payload(evtDto.Payload, evtDto.SerializerId, StringHelper.UTF8NoBOM.GetBytes(manifest), evtDto.ExtensibleData);
            var deserialized = _serialization.Deserialize(deserializedMsg);
            return new Persistent(deserialized, evtDto.SequenceNr, evtDto.PersistenceId, manifest, evtDto.IsDeleted, ActorRefs.NoSender, null);
        }

        private IList<JournalEvent> ToJournalEvents(Dictionary<IPersistentRepresentation, IImmutableSet<string>> entryTags)
        {
            var evtlist = new List<JournalEvent>(entryTags.Count);

            foreach (var entry in entryTags)
            {
                var evt = entry.Key;
                var tags = entry.Value;
                string strTags = string.Empty;
                if (tags.Count != 0)
                {
                    var tagBuilder = new StringBuilder(";", tags.Sum(x => x.Length) + tags.Count + 1);
                    foreach (var tag in tags)
                    {
                        tagBuilder.Append(tag).Append(';');
                    }

                    strTags = tagBuilder.ToString();
                }
                var serializedMsg = _serialization.Serialize(evt.Payload, _settings.DefaultSerializer);
                evtlist.Add(new JournalEvent
                {
                    PersistenceId = evt.PersistenceId,
                    SequenceNr = evt.SequenceNr,
                    Tags = strTags,
                    Payload = serializedMsg.Message,
                    SerializerId = serializedMsg.SerializerId,
                    Manifest = serializedMsg.Manifest != null ? Encoding.UTF8.GetString(serializedMsg.Manifest) : string.Empty,
                    ExtensibleData = serializedMsg.ExtensibleData,
                    IsDeleted = false,
                    CreatedAt = _timestampProvider.GenerateTimestamp(evt)
                });
            }
            return evtlist;
        }

        private static async Task<long> ReadHighestSequenceNr(WingsConnection dbConn, string persistenceId, CancellationToken cancellationToken)
        {
            var exprEvt = dbConn
                .From<JournalEvent>()
                .Select(_ => Sql.Max(_.SequenceNr))
                .Where(_ => _.PersistenceId == persistenceId);
            var evtSequenceNr = await dbConn.ScalarAsync<long>(exprEvt, cancellationToken);
            var exprMeta = dbConn
                .From<JournalMetadata>()
                .Select(_ => Sql.Max(_.SequenceNr))
                .Where(_ => _.PersistenceId == persistenceId);
            var metaSequenceNr = await dbConn.ScalarAsync<long>(exprMeta, cancellationToken);
            return Math.Max(evtSequenceNr, metaSequenceNr);
        }

        #region obsoleted

        /// <summary>TBD</summary>
        /// <param name="typeName">TBD</param>
        /// <returns>TBD</returns>
        protected ITimestampProvider GetTimestampProvider(string typeName)
        {
            var type = Type.GetType(typeName, true);
            try
            {
                return (ITimestampProvider)Activator.CreateInstance(type, Context.System);
            }
            catch (Exception)
            {
                return (ITimestampProvider)Activator.CreateInstance(type);
            }
        }
        #endregion
    }
}
