//-----------------------------------------------------------------------
// <copyright file="BatchingSqlJournal.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Annotations;
using Akka.Configuration;
using Akka.Event;
using Akka.Pattern;
using Akka.Persistence.Journal;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using CuteAnt;
using CuteAnt.Text;
using CuteAnt.Wings;

namespace Akka.Persistence.Wings.Journal
{
    /// <summary>Settings used for managing filter rules during event replay.</summary>
    public sealed class ReplayFilterSettings
    {
        /// <summary>Mode used when detecting invalid events.</summary>
        public readonly ReplayFilterMode Mode;

        /// <summary>Size (in number of events) of the look ahead buffer used for analyzing the events.</summary>
        public readonly int WindowSize;

        /// <summary>Maximum number of writerUuid to remember.</summary>
        public readonly int MaxOldWriters;

        /// <summary>Determine if the debug logging is enabled for each replayed event.</summary>
        public readonly bool IsDebug;

        /// <summary>Determine if the replay filter feature is enabled</summary>
        public bool IsEnabled => Mode != ReplayFilterMode.Disabled;

        /// <summary>Initializes a new instance of the <see cref="ReplayFilterSettings"/> class.</summary>
        /// <param name="config">The configuration used to configure the replay filter.</param>
        /// <exception cref="Akka.Configuration.ConfigurationException">
        /// This exception is thrown when an invalid <c>replay-filter.mode</c> is read from the
        /// specified <paramref name="config"/>. Acceptable <c>replay-filter.mode</c> values include:
        /// off | repair-by-discard-old | fail | warn
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="config"/> is undefined.
        /// </exception>
        public ReplayFilterSettings(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "No HOCON config was provided for replay filter settings");

            ReplayFilterMode mode;
            var replayModeString = config.GetString("mode", "off");
            switch (replayModeString)
            {
                case "off": mode = ReplayFilterMode.Disabled; break;
                case "repair-by-discard-old": mode = ReplayFilterMode.RepairByDiscardOld; break;
                case "fail": mode = ReplayFilterMode.Fail; break;
                case "warn": mode = ReplayFilterMode.Warn; break;
                default: throw new Akka.Configuration.ConfigurationException($"Invalid replay-filter.mode [{replayModeString}], supported values [off, repair-by-discard-old, fail, warn]");
            }

            Mode = mode;
            WindowSize = config.GetInt("window-size", 100);
            MaxOldWriters = config.GetInt("max-old-writers", 10);
            IsDebug = config.GetBoolean("debug", false);
        }

        /// <summary>Initializes a new instance of the <see cref="ReplayFilterSettings"/> class.</summary>
        /// <param name="mode">The mode used when detecting invalid events.</param>
        /// <param name="windowSize">The size of the replay filter's buffer.</param>
        /// <param name="maxOldWriters">The maximum number of writerUuid to remember.</param>
        /// <param name="isDebug">If set to <c>true</c>, debug logging is enabled for each replayed event.</param>
        public ReplayFilterSettings(ReplayFilterMode mode, int windowSize, int maxOldWriters, bool isDebug)
        {
            Mode = mode;
            WindowSize = windowSize;
            MaxOldWriters = maxOldWriters;
            IsDebug = isDebug;
        }
    }

    /// <summary>Settings used by <see cref="CircuitBreaker"/> used internally by the batching journal when executing event batches.</summary>
    public sealed class CircuitBreakerSettings
    {
        /// <summary>Maximum number of failures that can happen before the circuit opens.</summary>
        public int MaxFailures { get; }

        /// <summary>Maximum time available for operation to execute before <see cref="CircuitBreaker"/> considers it a failure.</summary>
        public TimeSpan CallTimeout { get; }

        /// <summary>Timeout that has to pass before <see cref="CircuitBreaker"/> moves into half-closed
        /// state, trying to eventually close after sampling an operation.</summary>
        public TimeSpan ResetTimeout { get; }

        /// <summary>Creates a new instance of the <see cref="CircuitBreakerSettings"/> from provided HOCON <paramref name="config"/>.</summary>
        /// <param name="config">The configuration used to configure the circuit breaker.</param>
        /// <exception cref="ArgumentNullException">This exception is thrown when the specified <paramref name="config"/> is undefined.</exception>
        public CircuitBreakerSettings(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            MaxFailures = config.GetInt("max-failures", 5);
            CallTimeout = config.GetTimeSpan("call-timeout", TimeSpan.FromSeconds(20));
            ResetTimeout = config.GetTimeSpan("reset-timeout", TimeSpan.FromSeconds(60));
        }

        /// <summary>Initializes a new instance of the <see cref="CircuitBreakerSettings"/> class.</summary>
        /// <param name="maxFailures">The maximum number of failures that can happen before the circuit opens.</param>
        /// <param name="callTimeout">The maximum time available for operation to execute before <see cref="CircuitBreaker"/>
        /// considers it a failure.</param>
        /// <param name="resetTimeout">The amount of time before <see cref="CircuitBreaker"/> moves into the half-closed state.</param>
        public CircuitBreakerSettings(int maxFailures, TimeSpan callTimeout, TimeSpan resetTimeout)
        {
            MaxFailures = maxFailures;
            CallTimeout = callTimeout;
            ResetTimeout = resetTimeout;
        }
    }

    /// <summary>All settings that can be used by implementations of <see cref="BatchingWingJournal"/>.</summary>
    public class BatchingWingJournalSetup : IShardingOption
    {
        /// <summary>
        /// Maximum number of batch operations allowed to be executed at the same time. Each batch
        /// operation must acquire a <see cref="DbConnection"/>, so this setting can be effectively
        /// used to limit the usage of ADO.NET connection pool by current journal.
        /// </summary>
        public readonly int MaxConcurrentOperations;

        /// <summary>Maximum size of single batch of operations to be executed over a single <see cref="WingsConnection"/>.</summary>
        public readonly int MaxBatchSize;

        /// <summary>Maximum size of requests stored in journal buffer. Once buffer will be surpassed, it will
        /// start to apply <see cref="BatchingWingJournal"/> method to incoming requests.</summary>
        public readonly int MaxBufferSize;

        /// <summary>If true, once created, journal will run all SQL scripts.
        /// In most implementation this is used to initialize necessary tables.</summary>
        public readonly bool AutoInitialize;

        /// <summary>Maximum time given for executed <see cref="DbCommand"/> to complete.</summary>
        public readonly TimeSpan ConnectionTimeout;

        /// <summary>Isolation level of transactions used during query execution.</summary>
        public readonly IsolationLevel IsolationLevel;

        /// <summary>Settings specific to <see cref="CircuitBreaker"/>, which is used internally for executing request batches.</summary>
        public readonly CircuitBreakerSettings CircuitBreakerSettings;

        /// <summary>Settings specific to replay filter rules used when replaying events from database back to the persistent actors.</summary>
        public readonly ReplayFilterSettings ReplayFilterSettings;

        /// <summary>The default serializer used when not type override matching is found</summary>
        public readonly string DefaultSerializer;

        public bool UseOneDbServer { get; }
        public ShardingKeyMode DbServerShardingMode { get; }

        public bool UseOneDatabase { get; }
        public ShardingKeyMode DatabaseShardingMode { get; }

        public bool UseOneTable { get; }
        public ShardingKeyMode TableShardingMode { get; }

        public string ConnectionName { get; set; }

        public string DatabaseName { get; set; }

        [InternalApi]
        internal readonly IWingsConnectionFactory Factory;

        /// <summary>Initializes a new instance of the <see cref="BatchingWingJournalSetup"/> class.</summary>
        /// <param name="config">The configuration used to configure the journal.</param>
        /// <exception cref="Akka.Configuration.ConfigurationException">
        /// This exception is thrown for a couple of reasons. <ul><li>A connection string for the SQL
        /// event journal was not specified.</li><li> An unknown <c>isolation-level</c> value was
        /// specified. Acceptable <c>isolation-level</c> values include: chaos | read-committed |
        /// read-uncommitted | repeatable-read | serializable | snapshot | unspecified </li></ul>
        /// </exception>
        /// <exception cref="ArgumentNullException">This exception is thrown when the specified <paramref name="config"/> is undefined.</exception>
        public BatchingWingJournalSetup(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "Sql journal settings cannot be initialized, because required HOCON section couldn't been found");

            IsolationLevel level;
            switch (config.GetString("isolation-level", "unspecified"))
            {
                case "chaos": level = IsolationLevel.Chaos; break;
                case "read-committed": level = IsolationLevel.ReadCommitted; break;
                case "read-uncommitted": level = IsolationLevel.ReadUncommitted; break;
                case "repeatable-read": level = IsolationLevel.RepeatableRead; break;
                case "serializable": level = IsolationLevel.Serializable; break;
                case "snapshot": level = IsolationLevel.Snapshot; break;
                case "unspecified": level = IsolationLevel.Unspecified; break;
                default: throw new Akka.Configuration.ConfigurationException("Unknown isolation-level value. Should be one of: chaos | read-committed | read-uncommitted | repeatable-read | serializable | snapshot | unspecified");
            }

            MaxConcurrentOperations = config.GetInt("max-concurrent-operations", 64);
            MaxBatchSize = config.GetInt("max-batch-size", 100);
            MaxBufferSize = config.GetInt("max-buffer-size", 500000);
            AutoInitialize = config.GetBoolean("auto-initialize", false);
            ConnectionTimeout = config.GetTimeSpan("connection-timeout", TimeSpan.FromSeconds(30));
            IsolationLevel = level;
            CircuitBreakerSettings = new CircuitBreakerSettings(config.GetConfig("circuit-breaker"));
            ReplayFilterSettings = new ReplayFilterSettings(config.GetConfig("replay-filter"));
            DefaultSerializer = config.GetString("serializer");

            UseOneDbServer = config.GetBoolean("singleton-db-server", true);
            if (Utils.TryParseShardingKey(config.GetString("db-server-sharding-mode"), out var mode))
            {
                DbServerShardingMode = mode;
            }
            else
            {
                UseOneDbServer = true;
            }
            UseOneDatabase = config.GetBoolean("singleton-database", true);
            if (Utils.TryParseShardingKey(config.GetString("database-sharding-mode"), out mode))
            {
                DatabaseShardingMode = mode;
            }
            else
            {
                UseOneDatabase = true;
            }
            UseOneTable = config.GetBoolean("singleton-table", true);
            if (Utils.TryParseShardingKey(config.GetString("table-sharding-mode"), out mode))
            {
                TableShardingMode = mode;
            }
            else
            {
                UseOneTable = true;
            }

            ConnectionName = config.GetString("connection-name");
            DatabaseName = config.GetString("database-name");

            var connStr = config.GetString("connection-string");
            var dialectProviderType = config.GetString("dialect-provider");
            if (!string.IsNullOrEmpty(connStr) && !string.IsNullOrEmpty(dialectProviderType))
            {
                Factory = new WingsConnectionFactory(connStr, Utils.GetDialectProvider(dialectProviderType));
                Factory.TryRegister("Default", Factory);
                WingsConnection.Factory = Factory;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="BatchingWingJournalSetup"/> class.</summary>
        /// <param name="maxConcurrentOperations">The maximum number of batch operations allowed to be executed at the same time.</param>
        /// <param name="maxBatchSize">The maximum size of single batch of operations to be executed over a single <see cref="DbConnection"/>.</param>
        /// <param name="maxBufferSize">The maximum size of requests stored in journal buffer.</param>
        /// <param name="autoInitialize"></param>
        /// <param name="connectionTimeout">The maximum time given for executed <see cref="DbCommand"/> to complete.</param>
        /// <param name="isolationLevel">The isolation level of transactions used during query execution.</param>
        /// <param name="circuitBreakerSettings">The settings used by the <see cref="CircuitBreaker"/> when for executing request batches.</param>
        /// <param name="replayFilterSettings">The settings used when replaying events from database back to the persistent actors.</param>
        /// <param name="defaultSerializer">The serializer used when no specific type matching can be found.</param>
        protected BatchingWingJournalSetup(int maxConcurrentOperations, int maxBatchSize, int maxBufferSize, bool autoInitialize, TimeSpan connectionTimeout, IsolationLevel isolationLevel, CircuitBreakerSettings circuitBreakerSettings, ReplayFilterSettings replayFilterSettings, string defaultSerializer)
        {
            MaxConcurrentOperations = maxConcurrentOperations;
            MaxBatchSize = maxBatchSize;
            MaxBufferSize = maxBufferSize;
            AutoInitialize = autoInitialize;
            ConnectionTimeout = connectionTimeout;
            IsolationLevel = isolationLevel;
            CircuitBreakerSettings = circuitBreakerSettings;
            ReplayFilterSettings = replayFilterSettings;
            DefaultSerializer = defaultSerializer;
        }
    }

    /// <summary>
    /// An abstract journal used by <see cref="PersistentActor"/> s to read/write events to a database.
    ///
    /// This implementation uses horizontal batching to recycle usage of the <see
    /// cref="DbConnection"/> and to optimize writes made to a database. Batching journal is not
    /// going to acquire a new DB connection on every request. Instead it will batch incoming
    /// requests and execute them only when a previous operation batch has been completed. This means
    /// that requests coming from many actors at the same time will be executed in one batch.
    ///
    /// Maximum number of batches executed at the same time is defined by <see
    /// cref="BatchingWingJournalSetup.MaxConcurrentOperations"/> setting, while max allowed batch
    /// size is defined by <see cref="BatchingWingJournalSetup.MaxBatchSize"/> setting.
    ///
    /// Batching journal also defines <see cref="BatchingWingJournalSetup.MaxBufferSize"/>, which
    /// defines a maximum number of all requests stored at once in memory. Once that value is
    /// surpassed, journal will start to apply <see cref="OnBufferOverflow"/> logic on each incoming
    /// requests, until a buffer gets freed again. This may be used for overflow strategies, request
    /// denials or backpressure.
    /// </summary>
    public class BatchingWingJournal : WriteJournalBase
    {
        #region internal classes

        private sealed class BatchComplete
        {
            public readonly int ChunkId;
            public readonly int OperationCount;
            public readonly TimeSpan TimeSpent;
            public readonly Exception Cause;

            public BatchComplete(int chunkId, int operationCount, TimeSpan timeSpent, Exception cause = null)
            {
                ChunkId = chunkId;
                TimeSpent = timeSpent;
                OperationCount = operationCount;
                Cause = cause;
            }
        }

        private struct RequestChunk
        {
            public readonly int ChunkId;
            public readonly IJournalRequest[] Requests;

            public RequestChunk(int chunkId, IJournalRequest[] requests)
            {
                ChunkId = chunkId;
                Requests = requests;
            }
        }

        #endregion

        private static readonly Type s_eventEntityType = typeof(JournalEvent);
        private static readonly Type s_metaEntityType = typeof(JournalMetadata);

        /// <summary>All configurable settings defined for a current batching journal.</summary>
        protected readonly BatchingWingJournalSetup Setup;

        private readonly IWingsConnectionFactory _factory;

        /// <summary>
        /// Flag determining if current journal has any subscribers for <see cref="EventAppended"/> events.
        /// </summary>
        protected bool HasPersistenceIdSubscribers => _persistenceIdSubscribers.Count != 0;

        /// <summary>Flag determining if current journal has any subscribers for <see cref="TaggedEventAppended"/> events.</summary>
        protected bool HasTagSubscribers => _tagSubscribers.Count != 0;

        /// <summary>Flag determining if incoming journal requests should be published in current actor system
        /// event stream. Useful mostly for tests.</summary>
        protected readonly bool CanPublish;

        /// <summary>Logging adapter for current journal actor .</summary>
        protected readonly ILoggingAdapter Log;

        /// <summary>Buffer for requests that are waiting to be served when next DB connection will be
        /// released. This object access is NOT thread safe.</summary>
        protected readonly Queue<IJournalRequest> Buffer;

        private readonly Dictionary<string, HashSet<IActorRef>> _persistenceIdSubscribers;
        private readonly Dictionary<string, HashSet<IActorRef>> _tagSubscribers;

        private readonly Akka.Serialization.Serialization _serialization;
        private readonly CircuitBreaker _circuitBreaker;
        private int _remainingOperations;

        /// <summary>Initializes a new instance of the <see cref="BatchingWingJournal"/> class.</summary>
        /// <param name="config">The settings used to configure the journal.</param>
        public BatchingWingJournal(Config config)
        {
            Setup = new BatchingWingJournalSetup(config);
            CanPublish = Persistence.Instance.Apply(Context.System).Settings.Internal.PublishPluginCommands;

            if (Setup.Factory == null)
            {
                _factory = WingsConnection.Factory.GetFactory(Setup.ConnectionName);
            }
            else
            {
                _factory = Setup.Factory;
            }

            _persistenceIdSubscribers = new Dictionary<string, HashSet<IActorRef>>(StringComparer.Ordinal);
            _tagSubscribers = new Dictionary<string, HashSet<IActorRef>>(StringComparer.Ordinal);

            _remainingOperations = Setup.MaxConcurrentOperations;
            Buffer = new Queue<IJournalRequest>(Setup.MaxBatchSize);
            _serialization = Context.System.Serialization;
            Log = Context.GetLogger();
            _circuitBreaker = CircuitBreaker.Create(
                maxFailures: Setup.CircuitBreakerSettings.MaxFailures,
                callTimeout: Setup.CircuitBreakerSettings.CallTimeout,
                resetTimeout: Setup.CircuitBreakerSettings.ResetTimeout);
        }

        /// <summary>TBD</summary>
        protected override void PreStart()
        {
            if (Setup.AutoInitialize)
            {
                //DbShardingManager.InstallOrUpgrade(s_eventEntityType, Setup);
                //DbShardingManager.InstallOrUpgrade(s_metaEntityType, Setup);
                using (var db = _factory.CreateDbConnection())
                {
                    db.Open();
                    db.CheckSchema(s_eventEntityType);
                    db.CheckSchema(s_metaEntityType);
                }
            }

            base.PreStart();
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected sealed override bool Receive(object message)
        {
            switch (message)
            {
                case WriteMessages write:
                    BatchRequest(write);
                    return true;

                case ReplayMessages replay:
                    BatchRequest(replay);
                    return true;

                case BatchComplete batch:
                    CompleteBatch(batch);
                    return true;

                case DeleteMessagesTo delete:
                    BatchRequest(delete);
                    return true;

                case ReplayTaggedMessages replayTagged:
                    BatchRequest(replayTagged);
                    return true;

                case SubscribePersistenceId subscribePersistenceId:
                    AddPersistenceIdSubscriber(subscribePersistenceId);
                    return true;

                case SubscribeTag subscribeTag:
                    AddTagSubscriber(subscribeTag);
                    return true;

                case Terminated terminated:
                    RemoveSubscriber(terminated.ActorRef);
                    return true;

                default:
                    return false;
            }
        }

        #region subscriptions

        private void RemoveSubscriber(IActorRef subscriberRef)
        {
            _persistenceIdSubscribers.RemoveItem(subscriberRef);
            _tagSubscribers.RemoveItem(subscriberRef);
        }

        private void AddTagSubscriber(SubscribeTag message)
        {
            var subscriber = Sender;
            _tagSubscribers.AddItem(message.Tag, subscriber);
            Context.Watch(subscriber);
        }

        private void AddPersistenceIdSubscriber(SubscribePersistenceId message)
        {
            var subscriber = Sender;
            _persistenceIdSubscribers.AddItem(message.PersistenceId, subscriber);
            Context.Watch(subscriber);
        }

        private void NotifyTagChanged(string tag)
        {
            if (_tagSubscribers.TryGetValue(tag, out var bucket))
            {
                var changed = new TaggedEventAppended(tag);
                foreach (var subscriber in bucket)
                    subscriber.Tell(changed);
            }
        }

        private void NotifyPersistenceIdChanged(string persistenceId)
        {
            if (_persistenceIdSubscribers.TryGetValue(persistenceId, out var bucket))
            {
                var changed = new EventAppended(persistenceId);
                foreach (var subscriber in bucket)
                    subscriber.Tell(changed);
            }
        }

        #endregion

        /// <summary>
        /// Tries to add incoming <paramref name="message"/> to <see cref="Buffer"/>. Also checks if
        /// any DB connection has been released and next batch can be processed.
        /// </summary>
        /// <param name="message">TBD</param>
        protected void BatchRequest(IJournalRequest message)
        {
            if (Buffer.Count > Setup.MaxBufferSize)
                OnBufferOverflow(message);
            else
                Buffer.Enqueue(message);

            TryProcess();
        }

        /// <summary>
        /// Method called, once given <paramref name="request"/> couldn't be added to <see
        /// cref="Buffer"/> due to buffer overflow. Overflow is controlled by max buffer size and can
        /// be set using <see cref="BatchingWingJournalSetup.MaxBufferSize"/> setting.
        /// </summary>
        /// <param name="request">TBD</param>
        protected virtual void OnBufferOverflow(IJournalMessage request)
        {
            Log.Warning("Batching journal buffer limit has been reached. Denying a request [{0}].", request);

            switch (request)
            {
                case WriteMessages write:
                    write.PersistentActor.Tell(new WriteMessagesFailed(JournalBufferOverflowException.Instance), ActorRefs.NoSender);
                    break;

                case ReplayMessages replay:
                    replay.PersistentActor.Tell(new ReplayMessagesFailure(JournalBufferOverflowException.Instance), ActorRefs.NoSender);
                    break;

                case DeleteMessagesTo delete:
                    delete.PersistentActor.Tell(new DeleteMessagesFailure(JournalBufferOverflowException.Instance, delete.ToSequenceNr), ActorRefs.NoSender);
                    break;

                case ReplayTaggedMessages replayTagged:
                    replayTagged.ReplyTo.Tell(new ReplayMessagesFailure(JournalBufferOverflowException.Instance), ActorRefs.NoSender);
                    break;
            }
        }

        private void TryProcess()
        {
            if (_remainingOperations > 0 && Buffer.Count > 0)
            {
                _remainingOperations--;

                var chunk = DequeueChunk(_remainingOperations);
                var context = Context;
                _circuitBreaker.WithCircuitBreaker(() => ExecuteChunk(chunk, context)).PipeTo(Self);
            }
        }

        private async Task<BatchComplete> ExecuteChunk(RequestChunk chunk, IActorContext context)
        {
            Exception cause = null;
            var stopwatch = new Stopwatch();
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(Setup.ConnectionName)
            {
                dbConn.Open();

                using (var tx = dbConn.OpenTransaction(Setup.IsolationLevel))
                {
                    try
                    {
                        stopwatch.Start();
                        for (int i = 0; i < chunk.Requests.Length; i++)
                        {
                            var req = chunk.Requests[i];

                            switch (req)
                            {
                                case WriteMessages write:
                                    await HandleWriteMessages(write, dbConn);
                                    break;

                                case ReplayMessages replay:
                                    await HandleReplayMessages(replay, dbConn, context);
                                    break;

                                case DeleteMessagesTo delete:
                                    await HandleDeleteMessagesTo(delete, dbConn);
                                    break;

                                case ReplayTaggedMessages replayTagged:
                                    await HandleReplayTaggedMessages(replayTagged, dbConn);
                                    break;

                                default:
                                    Unhandled(req);
                                    break;
                            }
                        }

                        tx.Commit();

                        if (CanPublish)
                        {
                            for (int i = 0; i < chunk.Requests.Length; i++)
                            {
                                context.System.EventStream.Publish(chunk.Requests[i]);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        cause = e;
                        tx.Rollback();
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                }
            }

            return new BatchComplete(chunk.ChunkId, chunk.Requests.Length, stopwatch.Elapsed, cause);
        }

        protected virtual async Task HandleDeleteMessagesTo(DeleteMessagesTo req, WingsConnection dbConn)
        {
            var toSequenceNr = req.ToSequenceNr;
            var persistenceId = req.PersistenceId;

            try
            {
                var highestSequenceNr = await ReadHighestSequenceNr(dbConn, persistenceId);

                await dbConn.DeleteAsync<JournalEvent>(_ => _.PersistenceId == persistenceId && _.SequenceNr <= toSequenceNr);

                if (highestSequenceNr <= toSequenceNr)
                {
                    await dbConn.InsertAsync<JournalMetadata>(new JournalMetadata
                    {
                        Id = CombGuid.NewComb(),
                        PersistenceId = persistenceId,
                        SequenceNr = highestSequenceNr
                    });
                }

                var response = new DeleteMessagesSuccess(toSequenceNr);
                req.PersistentActor.Tell(response);
            }
            catch (Exception cause)
            {
                var response = new DeleteMessagesFailure(cause, toSequenceNr);
                req.PersistentActor.Tell(response, ActorRefs.NoSender);
            }
        }

        protected virtual async Task HandleReplayTaggedMessages(ReplayTaggedMessages req, WingsConnection dbConn)
        {
            var replyTo = req.ReplyTo;

            try
            {
                var maxSequenceNr = 0L;
                var tag = req.Tag;
                var toOffset = req.ToOffset;
                var fromOffset = req.FromOffset;
                var take = (int)Math.Min(toOffset - fromOffset, req.Max);

                var evtDtos = await dbConn.SelectAsync<JournalEventDTO, JournalEvent>(
                    predicate: _ => _.Id > fromOffset && _.Tags.Contains(tag),
                    orderByKeysSelector: _ => _.Id,
                    rows: take);

                foreach (var evtDto in evtDtos)
                {
                    var persistent = ToPersistent(evtDto);
                    var ordering = evtDto.Id;
                    maxSequenceNr = Math.Max(maxSequenceNr, persistent.SequenceNr);
                    foreach (var adapted in AdaptFromJournal(persistent))
                    {
                        replyTo.Tell(new ReplayedTaggedMessage(adapted, tag, ordering), ActorRefs.NoSender);
                    }
                }

                replyTo.Tell(new RecoverySuccess(maxSequenceNr));
            }
            catch (Exception cause)
            {
                replyTo.Tell(new ReplayMessagesFailure(cause));
            }
        }

        protected virtual async Task HandleReplayMessages(ReplayMessages req, WingsConnection dbConn, IActorContext context)
        {
            var replaySettings = Setup.ReplayFilterSettings;
            var replyTo = replaySettings.IsEnabled
                ? context.ActorOf(ReplayFilter.Props(
                    persistentActor: req.PersistentActor,
                    mode: replaySettings.Mode,
                    windowSize: replaySettings.WindowSize,
                    maxOldWriters: replaySettings.MaxOldWriters,
                    debugEnabled: replaySettings.IsDebug))
                : req.PersistentActor;
            var persistenceId = req.PersistenceId;

            try
            {
                var highestSequenceNr = await ReadHighestSequenceNr(dbConn, persistenceId);
                var toSequenceNr = Math.Min(req.ToSequenceNr, highestSequenceNr);

                var evtDtos = await dbConn.SelectAsync<JournalEventDTO, JournalEvent>(
                    predicate: _ => _.PersistenceId == persistenceId && _.SequenceNr >= req.FromSequenceNr && _.SequenceNr <= toSequenceNr,
                    orderByKeysSelector: _ => _.SequenceNr,
                    rows: (int)req.Max);

                foreach (var evtDto in evtDtos)
                {
                    var persistent = ToPersistent(evtDto);
                    if (!persistent.IsDeleted) // old records from pre 1.5 may still have the IsDeleted flag
                    {
                        foreach (var adaptedRepresentation in AdaptFromJournal(persistent))
                        {
                            replyTo.Tell(new ReplayedMessage(adaptedRepresentation), ActorRefs.NoSender);
                        }
                    }
                }

                var response = new RecoverySuccess(highestSequenceNr);
                replyTo.Tell(response, ActorRefs.NoSender);
            }
            catch (Exception cause)
            {
                var response = new ReplayMessagesFailure(cause);
                replyTo.Tell(response, ActorRefs.NoSender);
            }
        }

        private async Task HandleWriteMessages(WriteMessages req, WingsConnection dbConn)
        {
            IJournalResponse summary = null;
            var responses = new List<IJournalResponse>();
            var tags = new HashSet<string>(StringComparer.Ordinal);
            var persistenceIds = new HashSet<string>(StringComparer.Ordinal);
            var actorInstanceId = req.ActorInstanceId;

            try
            {
                var tagBuilder = new StringBuilder(16); // magic number

                foreach (var envelope in req.Messages)
                {
                    if (envelope is AtomicWrite write)
                    {
                        var writes = (IImmutableList<IPersistentRepresentation>)write.Payload;
                        foreach (var unadapted in writes)
                        {
                            try
                            {
                                tagBuilder.Clear();

                                var persistent = AdaptToJournal(unadapted);
                                if (persistent.Payload is Tagged tagged)
                                {
                                    if (tagged.Tags.Count != 0)
                                    {
                                        tagBuilder.Append(';');
                                        foreach (var tag in tagged.Tags)
                                        {
                                            tags.Add(tag);
                                            tagBuilder.Append(tag).Append(';');
                                        }
                                    }
                                    persistent = persistent.WithPayload(tagged.Payload);
                                }

                                var journalEvent = ToJournalEvent(persistent, tagBuilder.ToString());

                                await dbConn.InsertAsync(journalEvent);

                                var response = new WriteMessageSuccess(unadapted, actorInstanceId);
                                responses.Add(response);
                                persistenceIds.Add(persistent.PersistenceId);
                            }
                            catch (DbException cause)
                            {
                                // database-related exceptions should result in failure
                                summary = new WriteMessagesFailed(cause);
                                var response = new WriteMessageFailure(unadapted, cause, actorInstanceId);
                                responses.Add(response);
                            }
                            catch (Exception cause)
                            {
                                //TODO: this scope wraps atomic write. Atomic writes have all-or-nothing commits.
                                // so we should revert transaction here. But we need to check how this affect performance.
                                var response = new WriteMessageRejected(unadapted, cause, actorInstanceId);
                                responses.Add(response);
                            }
                        }
                    }
                    else
                    {
                        //TODO: other cases?
                        var response = new LoopMessageSuccess(envelope.Payload, actorInstanceId);
                        responses.Add(response);
                    }
                }

                if (HasTagSubscribers && tags.Count != 0)
                {
                    foreach (var tag in tags)
                    {
                        NotifyTagChanged(tag);
                    }
                }

                if (HasPersistenceIdSubscribers)
                {
                    foreach (var persistenceId in persistenceIds)
                    {
                        NotifyPersistenceIdChanged(persistenceId);
                    }
                }

                summary = summary ?? WriteMessagesSuccessful.Instance;
            }
            catch (Exception cause)
            {
                summary = new WriteMessagesFailed(cause);
            }

            var aref = req.PersistentActor;

            aref.Tell(summary);
            foreach (var response in responses)
            {
                aref.Tell(response);
            }
        }

        private RequestChunk DequeueChunk(int chunkId)
        {
            var operationsCount = Math.Min(Buffer.Count, Setup.MaxBatchSize);
            var array = new IJournalRequest[operationsCount];
            for (int i = 0; i < operationsCount; i++)
            {
                var req = Buffer.Dequeue();
                array[i] = req;
            }

            return new RequestChunk(chunkId, array);
        }

        private void CompleteBatch(BatchComplete msg)
        {
            _remainingOperations++;
            if (msg.Cause != null)
            {
                Log.Error(msg.Cause, "An error occurred during event batch processing (chunkId: {0})", msg.ChunkId);
            }
            else
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Completed batch (chunkId: {0}) of {1} operations in {2} milliseconds", msg.ChunkId, msg.OperationCount, msg.TimeSpent.TotalMilliseconds);
                }
            }

            TryProcess();
        }

        private IPersistentRepresentation ToPersistent(JournalEventDTO evtDto)
        {
            var manifest = evtDto.Manifest;
            var deserializedMsg = new Payload(evtDto.Payload, evtDto.SerializerId, StringHelper.UTF8NoBOM.GetBytes(manifest), evtDto.ExtensibleData);
            var deserialized = _serialization.Deserialize(deserializedMsg);
            return new Persistent(deserialized, evtDto.SequenceNr, evtDto.PersistenceId, manifest, evtDto.IsDeleted, ActorRefs.NoSender, null);
        }

        private JournalEvent ToJournalEvent(IPersistentRepresentation persistent, string tags)
        {
            var serializedMsg = _serialization.Serialize(persistent.Payload, Setup.DefaultSerializer);
            return new JournalEvent
            {
                PersistenceId = persistent.PersistenceId,
                SequenceNr = persistent.SequenceNr,
                Tags = tags,
                Payload = serializedMsg.Message,
                SerializerId = serializedMsg.SerializerId,
                Manifest = serializedMsg.Manifest != null ? Encoding.UTF8.GetString(serializedMsg.Manifest) : string.Empty,
                ExtensibleData = serializedMsg.ExtensibleData,
                IsDeleted = false,
                CreatedAt = 0L
            };
        }

        private static async Task<long> ReadHighestSequenceNr(WingsConnection dbConn, string persistenceId)
        {
            var exprEvt = dbConn
                .From<JournalEvent>()
                .Select(_ => Sql.Max(_.SequenceNr))
                .Where(_ => _.PersistenceId == persistenceId);
            var evtSequenceNr = await dbConn.ScalarAsync<long>(exprEvt);
            var exprMeta = dbConn
                .From<JournalMetadata>()
                .Select(_ => Sql.Max(_.SequenceNr))
                .Where(_ => _.PersistenceId == persistenceId);
            var metaSequenceNr = await dbConn.ScalarAsync<long>(exprMeta);
            return Math.Max(evtSequenceNr, metaSequenceNr);
        }
    }

    /// <summary>TBD</summary>
    public class JournalBufferOverflowException : AkkaException
    {
        /// <summary>TBD</summary>
        public static readonly JournalBufferOverflowException Instance = new JournalBufferOverflowException();

        /// <summary>
        /// Initializes a new instance of the <see cref="JournalBufferOverflowException"/> class.
        /// </summary>
        public JournalBufferOverflowException() : base(
            "Batching journal buffer has been overflowed. This may happen as an effect of burst of persistent actors "
            + "requests incoming faster than the underlying database is able to fulfill them. You may modify "
            + "`max-buffer-size`, `max-batch-size` and `max-concurrent-operations` HOCON settings in order to "
            + " change it.")
        {
        }
    }
}