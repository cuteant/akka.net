using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Persistence.Snapshot;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using CuteAnt;
using CuteAnt.AsyncEx;
using CuteAnt.Text;
using CuteAnt.Wings;

namespace Akka.Persistence.Wings.Snapshot
{
    /// <summary>
    /// Abstract snapshot store implementation, customized to work with SQL-based persistence providers.
    /// </summary>
    public class WingSnapshotStore : SnapshotStore, IWithUnboundedStash
    {
        #region messages

        private sealed class Initialized
        {
            public static readonly Initialized Instance = new Initialized();
            private Initialized() { }
        }

        #endregion

        private static readonly Type s_snapshotEntityType = typeof(Snapshots);

        private readonly WingPersistence Extension = WingPersistence.Get(Context.System);

        /// <summary>
        /// List of cancellation tokens for all pending asynchronous database operations.
        /// </summary>
        private readonly CancellationTokenSource _pendingRequestsCancellation;

        private readonly WingSnapshotStoreSettings _settings;
        private readonly IWingsConnectionFactory _factory;

        /// <summary>TBD</summary>
        protected readonly Akka.Serialization.Serialization Serialization;

        /// <summary>Initializes a new instance of the <see cref="WingSnapshotStore"/> class.</summary>
        /// <param name="snapshotConfig">The configuration used to configure the snapshot store.</param>
        public WingSnapshotStore(Config snapshotConfig)
        {
            var config = snapshotConfig.WithFallback(Extension.DefaultSnapshotConfig);
            _settings = new WingSnapshotStoreSettings(config);

            if (_settings.Factory == null)
            {
                _factory = WingsConnection.Factory.GetFactory(_settings.ConnectionName);
            }
            else
            {
                _factory = _settings.Factory;
            }

            Serialization = Context.System.Serialization;
            _pendingRequestsCancellation = new CancellationTokenSource();
        }

        /// <summary>TBD</summary>
        protected ILoggingAdapter Log => _log ?? (_log = Context.GetLogger());
        private ILoggingAdapter _log;

        /// <summary>TBD</summary>
        public IStash Stash { get; set; }

        /// <summary>TBD</summary>
        protected override void PreStart()
        {
            base.PreStart();
            if (_settings.AutoInitialize)
            {
                Initialize().PipeTo(Self);
                BecomeStacked(WaitingForInitialization);
            }
        }

        /// <summary>TBD</summary>
        protected override void PostStop()
        {
            base.PostStop();

            // stop all operations executed in the background
            _pendingRequestsCancellation.Cancel();
        }

        private async Task<object> Initialize()
        {
            try
            {
                //DbShardingManager.InstallOrUpgrade(s_snapshotEntityType, _settings);
                using (var db = _factory.CreateDbConnection())
                {
                    db.Open();
                    db.CheckSchema(s_snapshotEntityType);
                }

                return Initialized.Instance;
            }
            catch (Exception e)
            {
                await TaskConstants.Completed;
                return new Failure { Exception = e };
            }
        }

        private bool WaitingForInitialization(object message)
        {
            switch (message)
            {
                case Initialized _:
                    UnbecomeStacked();
                    Stash.UnstashAll();
                    break;

                case Failure failure:
                    Log.Error(failure.Exception, "Error during snapshot store initialization");
                    Context.Stop(Self);
                    break;

                default:
                    Stash.Stash();
                    break;
            }
            return true;
        }

        /// <summary>Asynchronously loads snapshot with the highest sequence number for a persistent actor/view matching specified criteria.</summary>
        /// <param name="persistenceId">TBD</param>
        /// <param name="criteria">TBD</param>
        /// <returns>TBD</returns>
        protected override async Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_settings.ConnectionName))
            //var aggregateRootId = persistenceId.ToAggregateRootSequentialGuidId();
            //using (var dbConn = DbShardingManager.CreateDbConnection(s_snapshotEntityType, _settings, aggregateRootId))
            {
                //using (var ns = TableSplittingManager.CreateContext(dbConn.DialectProvider, aggregateRootId.ToTableSplittingKey(_settings)))
                {
                    dbConn.Open();

                    var expr = dbConn.From<Snapshots>();
                    if (criteria.MaxTimeStamp == DateTime.MinValue || criteria.MaxTimeStamp == DateTime.MaxValue)
                    {
                        expr.Where(_ => _.PersistenceId == persistenceId && _.SequenceNr <= criteria.MaxSequenceNr);
                    }
                    else
                    {
                        expr.Where(_ => _.PersistenceId == persistenceId && _.SequenceNr <= criteria.MaxSequenceNr && _.CreatedAt <= criteria.MaxTimeStamp);
                    }
                    expr.OrderByDescending(_ => _.SequenceNr);

                    using (var nestedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                    {
                        var snapshot = await dbConn.SingleAsync<Snapshots>(expr, nestedCancellationTokenSource.Token);
                        if (null == snapshot) { return null; }

                        var metadata = new SnapshotMetadata(persistenceId, snapshot.SequenceNr, snapshot.CreatedAt);
                        byte[] manifest = null;
                        if (!string.IsNullOrWhiteSpace(snapshot.Manifest))
                        {
                            manifest = StringHelper.UTF8NoBOM.GetBytes(snapshot.Manifest);
                        }
                        var serializedMessage = new Payload(snapshot.Payload, snapshot.SerializerId, manifest, snapshot.ExtensibleData);
                        return new SelectedSnapshot(metadata, Serialization.Deserialize(serializedMessage));
                    }
                }
            }
        }

        /// <summary>Asynchronously stores a snapshot with metadata as record in SQL table.</summary>
        /// <param name="metadata">TBD</param>
        /// <param name="snapshot">TBD</param>
        /// <returns>TBD</returns>
        protected override async Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_settings.ConnectionName))
            //var aggregateRootId = metadata.PersistenceId.ToAggregateRootSequentialGuidId();
            //using (var dbConn = DbShardingManager.CreateDbConnection(s_snapshotEntityType, _settings, aggregateRootId))
            {
                //using (var ns = TableSplittingManager.CreateContext(dbConn.DialectProvider, aggregateRootId.ToTableSplittingKey(_settings)))
                {
                    WingsTransaction trans = null;
                    try
                    {
                        dbConn.Open();
                        trans = dbConn.OpenTransaction();

                        var serializedMessage = Serialization.Serialize(snapshot, _settings.DefaultSerializer);

                        var exists = await dbConn.ExistsAsync<Snapshots>(_ => _.PersistenceId == metadata.PersistenceId && _.SequenceNr == metadata.SequenceNr);
                        if (!exists)
                        {
                            var entity = new Snapshots
                            {
                                Id = CombGuid.NewComb(),
                                PersistenceId = metadata.PersistenceId,
                                SequenceNr = metadata.SequenceNr,
                                SerializerId = serializedMessage.SerializerId,
                                Payload = serializedMessage.Message,
                                Manifest = serializedMessage.Manifest != null ? Encoding.UTF8.GetString(serializedMessage.Manifest) : string.Empty,
                                ExtensibleData = serializedMessage.ExtensibleData,
                                CreatedAt = metadata.Timestamp
                            };

                            using (var nestedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                            {
                                await dbConn.InsertAsync<Snapshots>(entity, token: nestedCancellationTokenSource.Token);
                            }
                        }
                        else
                        {
                            using (var nestedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                            {
                                await dbConn.UpdateOnlyAsync<Snapshots>(
                                    updateFields: () => new Snapshots
                                    {
                                        SerializerId = serializedMessage.SerializerId,
                                        Payload = serializedMessage.Message,
                                        Manifest = serializedMessage.Manifest != null ? Encoding.UTF8.GetString(serializedMessage.Manifest) : string.Empty,
                                        ExtensibleData = serializedMessage.ExtensibleData,
                                        CreatedAt = metadata.Timestamp
                                    },
                                    where: _ => _.PersistenceId == metadata.PersistenceId && _.SequenceNr == metadata.SequenceNr,
                                    commandFilter: null,
                                    token: nestedCancellationTokenSource.Token);
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
        }

        /// <summary>TBD</summary>
        /// <param name="metadata">TBD</param>
        /// <returns>TBD</returns>
        protected override async Task DeleteAsync(SnapshotMetadata metadata)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_settings.ConnectionName))
            //var aggregateRootId = metadata.PersistenceId.ToAggregateRootSequentialGuidId();
            //using (var dbConn = DbShardingManager.CreateDbConnection(s_snapshotEntityType, _settings, aggregateRootId))
            {
                //using (var ns = TableSplittingManager.CreateContext(dbConn.DialectProvider, aggregateRootId.ToTableSplittingKey(_settings)))
                {
                    WingsTransaction trans = null;
                    try
                    {
                        dbConn.Open();
                        trans = dbConn.OpenTransaction();

                        using (var nestedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                        {
                            if (metadata.Timestamp == DateTime.MinValue || metadata.Timestamp == DateTime.MaxValue)
                            {
                                await dbConn.DeleteAsync<Snapshots>(_ => _.PersistenceId == metadata.PersistenceId && _.SequenceNr == metadata.SequenceNr, token: nestedCancellationTokenSource.Token);
                            }
                            else
                            {
                                await dbConn.DeleteAsync<Snapshots>(_ => _.PersistenceId == metadata.PersistenceId && _.SequenceNr == metadata.SequenceNr && _.CreatedAt <= metadata.Timestamp, token: nestedCancellationTokenSource.Token);
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
        }

        /// <summary>TBD</summary>
        /// <param name="persistenceId">TBD</param>
        /// <param name="criteria">TBD</param>
        /// <returns>TBD</returns>
        protected override async Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            using (var dbConn = _factory.CreateDbConnection()) // WingsConnection.Factory.CreateDbConnection(_settings.ConnectionName))
            //var aggregateRootId = persistenceId.ToAggregateRootSequentialGuidId();
            //using (var dbConn = DbShardingManager.CreateDbConnection(s_snapshotEntityType, _settings, aggregateRootId))
            {
                //using (var ns = TableSplittingManager.CreateContext(dbConn.DialectProvider, aggregateRootId.ToTableSplittingKey(_settings)))
                {
                    WingsTransaction trans = null;
                    try
                    {
                        dbConn.Open();
                        trans = dbConn.OpenTransaction();

                        using (var nestedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_pendingRequestsCancellation.Token))
                        {
                            if (criteria.MaxTimeStamp == DateTime.MinValue || criteria.MaxTimeStamp == DateTime.MaxValue)
                            {
                                await dbConn.DeleteAsync<Snapshots>(_ => _.PersistenceId == persistenceId && _.SequenceNr <= criteria.MaxSequenceNr, token: nestedCancellationTokenSource.Token);
                            }
                            else
                            {
                                await dbConn.DeleteAsync<Snapshots>(_ => _.PersistenceId == persistenceId && _.SequenceNr <= criteria.MaxSequenceNr && _.CreatedAt <= criteria.MaxTimeStamp, token: nestedCancellationTokenSource.Token);
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
        }
    }
}
