//-----------------------------------------------------------------------
// <copyright file="ShardRegion.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Util;
using MessagePack;

namespace Akka.Cluster.Sharding
{
    using ShardId = String;
    using EntityId = String;
    using Msg = Object;

    /// <summary>
    /// This actor creates children entity actors on demand for the shards that it is told to be
    /// responsible for. It delegates messages targeted to other shards to the responsible
    /// <see cref="ShardRegion"/> actor on other nodes.
    /// </summary>
    public class ShardRegion : ActorBase
    {
        #region messages

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class Retry : IShardRegionCommand, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Retry Instance = new Retry();
            private Retry() { }
        }

        /// <summary>
        /// When an remembering entities and the shard stops unexpected (e.g. persist failure), we
        /// restart it after a back off using this message.
        /// </summary>
        [MessagePackObject]
        internal sealed class RestartShard
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly ShardId ShardId;
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shardId">TBD</param>
            [SerializationConstructor]
            public RestartShard(ShardId shardId)
            {
                ShardId = shardId;
            }
        }

        /// <summary>
        /// When remembering entities and a shard is started, each entity id that needs to
        /// be running will trigger this message being sent through sharding. For this to work
        /// the message *must* be handled by the shard id extractor.
        /// </summary>
        [MessagePackObject]
        public sealed class StartEntity : IClusterShardingSerializable
        {
            /// <summary>
            /// An identifier of an entity to be started. Unique in scope of a given shard.
            /// </summary>
            [Key(0)]
            public readonly EntityId EntityId;

            /// <summary>
            /// Creates a new instance of a <see cref="StartEntity"/> class, used for requesting
            /// to start an entity with provided <paramref name="entityId"/>.
            /// </summary>
            /// <param name="entityId">An identifier of an entity to be started on a given shard.</param>
            [SerializationConstructor]
            public StartEntity(EntityId entityId)
            {
                EntityId = entityId;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as StartEntity;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return EntityId.Equals(other.EntityId);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return EntityId?.GetHashCode() ?? 0;
                }
            }

            #endregion
        }

        /// <summary>
        /// Sent back when a <see cref="StartEntity"/> message was received and triggered the entity
        /// to start(it does not guarantee the entity successfully started)
        /// </summary>
        [MessagePackObject]
        public sealed class StartEntityAck : IClusterShardingSerializable
        {
            /// <summary>
            /// An identifier of a newly started entity. Unique in scope of a given shard.
            /// </summary>
            [Key(0)]
            public readonly EntityId EntityId;

            /// <summary>
            /// An identifier of a shard, on which an entity identified by <see cref="EntityId"/> is hosted.
            /// </summary>
            [Key(1)]
            public readonly ShardId ShardId;

            /// <summary>
            /// Creates a new instance of a <see cref="StartEntityAck"/> class, used to confirm that
            /// <see cref="StartEntity"/> request has succeed.
            /// </summary>
            /// <param name="entityId">An identifier of a newly started entity.</param>
            /// <param name="shardId">An identifier of a shard hosting started entity.</param>
            [SerializationConstructor]
            public StartEntityAck(EntityId entityId, ShardId shardId)
            {
                EntityId = entityId;
                ShardId = shardId;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as StartEntityAck;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return EntityId.Equals(other.EntityId)
                    && ShardId.Equals(other.ShardId);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = EntityId?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ (ShardId?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// INTERNAL API. Sends stopMessage (e.g. <see cref="PoisonPill"/>) to the entities and when all of them have terminated it replies with `ShardStopped`.
        /// </summary>
        internal class HandOffStopper : ReceiveActor2
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            /// <param name="replyTo">TBD</param>
            /// <param name="entities">TBD</param>
            /// <param name="stopMessage">TBD</param>
            /// <returns>TBD</returns>
            public static Props Props(ShardId shard, IActorRef replyTo, IEnumerable<IActorRef> entities, object stopMessage)
            {
                return Actor.Props.Create(() => new HandOffStopper(shard, replyTo, entities, stopMessage)).WithDeploy(Deploy.Local);
            }

            private readonly HashSet<IActorRef> _remaining;
            private readonly ShardId _shard;
            private readonly IActorRef _replyTo;
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            /// <param name="replyTo">TBD</param>
            /// <param name="entities">TBD</param>
            /// <param name="stopMessage">TBD</param>
            public HandOffStopper(ShardId shard, IActorRef replyTo, IEnumerable<IActorRef> entities, object stopMessage)
            {
                _shard = shard;
                _replyTo = replyTo;
                _remaining = new HashSet<IActorRef>(entities, ActorRefComparer.Instance);

                Receive<Terminated>(HandleTerminated);

                foreach (var aref in _remaining)
                {
                    Context.Watch(aref);
                    aref.Tell(stopMessage);
                }
            }

            private void HandleTerminated(Terminated t)
            {
                _remaining.Remove(t.ActorRef);
                if (_remaining.Count == 0)
                {
                    _replyTo.Tell(new PersistentShardCoordinator.ShardStopped(_shard));
                    Context.Stop(Self);
                }
            }
        }

        private class MemberAgeComparer : IComparer<Member>
        {
            public static readonly IComparer<Member> Instance = new MemberAgeComparer();

            private MemberAgeComparer() { }

            public int Compare(Member x, Member y)
            {
                if (x.IsOlderThan(y)) return -1;
                return y.IsOlderThan(x) ? 1 : 0;
            }
        }

        /// <summary>
        /// Factory method for the <see cref="Actor.Props"/> of the <see cref="ShardRegion"/> actor.
        /// </summary>
        /// <param name="typeName">TBD</param>
        /// <param name="entityProps">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="coordinatorPath">TBD</param>
        /// <param name="extractEntityId">TBD</param>
        /// <param name="extractShardId">TBD</param>
        /// <param name="handOffStopMessage">TBD</param>
        /// <param name="replicator"></param>
        /// <param name="majorityMinCap"></param>
        /// <returns>TBD</returns>
        internal static Props Props(string typeName, Func<string, Props> entityProps, ClusterShardingSettings settings, string coordinatorPath, ExtractEntityId extractEntityId, ExtractShardId extractShardId, object handOffStopMessage, IActorRef replicator, int majorityMinCap)
        {
            return Actor.Props.Create(() => new ShardRegion(typeName, entityProps, settings, coordinatorPath, extractEntityId, extractShardId, handOffStopMessage, replicator, majorityMinCap)).WithDeploy(Deploy.Local);
        }

        /// <summary>
        /// Factory method for the <see cref="Actor.Props"/> of the <see cref="ShardRegion"/> actor when used in proxy only mode.
        /// </summary>
        /// <param name="typeName">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="coordinatorPath">TBD</param>
        /// <param name="extractEntityId">TBD</param>
        /// <param name="extractShardId">TBD</param>
        /// <param name="replicator"></param>
        /// <param name="majorityMinCap"></param>
        /// <returns>TBD</returns>
        internal static Props ProxyProps(string typeName, ClusterShardingSettings settings, string coordinatorPath, ExtractEntityId extractEntityId, ExtractShardId extractShardId, IActorRef replicator, int majorityMinCap)
        {
            return Actor.Props.Create(() => new ShardRegion(typeName, null, settings, coordinatorPath, extractEntityId, extractShardId, PoisonPill.Instance, replicator, majorityMinCap)).WithDeploy(Deploy.Local);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public readonly string TypeName;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly Func<string, Props> EntityProps;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly ClusterShardingSettings Settings;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly string CoordinatorPath;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly ExtractEntityId ExtractEntityId;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly ExtractShardId ExtractShardId;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly object HandOffStopMessage;

        private readonly IActorRef _replicator;
        private readonly int _majorityMinCap;

        /// <summary>
        /// TBD
        /// </summary>
        public readonly Cluster Cluster = Cluster.Get(Context.System);

        // sort by age, oldest first
        private static readonly IComparer<Member> AgeOrdering = MemberAgeComparer.Instance;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableSet<Member> MembersByAge = ImmutableSortedSet<Member>.Empty.WithComparer(AgeOrdering);

        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableDictionary<IActorRef, IImmutableSet<ShardId>> Regions = ImmutableDictionary<IActorRef, IImmutableSet<ShardId>>.Empty;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableDictionary<ShardId, IActorRef> RegionByShard = ImmutableDictionary<ShardId, IActorRef>.Empty;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableDictionary<ShardId, IImmutableList<KeyValuePair<Msg, IActorRef>>> ShardBuffers = ImmutableDictionary<ShardId, IImmutableList<KeyValuePair<Msg, IActorRef>>>.Empty;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableDictionary<ShardId, IActorRef> Shards = ImmutableDictionary<ShardId, IActorRef>.Empty;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableDictionary<IActorRef, ShardId> ShardsByRef = ImmutableDictionary<IActorRef, ShardId>.Empty;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableSet<ShardId> StartingShards = ImmutableHashSet<ShardId>.Empty;
        /// <summary>
        /// TBD
        /// </summary>
        protected IImmutableSet<IActorRef> HandingOff = ImmutableHashSet<IActorRef>.Empty;

        private readonly ICancelable _retryTask;
        private IActorRef _coordinator;
        private int _retryCount;
        private bool _loggedFullBufferWarning;
        private const int RetryCountThreshold = 5;

        private readonly CoordinatedShutdown _coordShutdown = CoordinatedShutdown.Get(Context.System);
        private readonly TaskCompletionSource<Done> _gracefulShutdownProgress = new TaskCompletionSource<Done>();

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="typeName">TBD</param>
        /// <param name="entityProps">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="coordinatorPath">TBD</param>
        /// <param name="extractEntityId">TBD</param>
        /// <param name="extractShardId">TBD</param>
        /// <param name="handOffStopMessage">TBD</param>
        /// <param name="replicator"></param>
        /// <param name="majorityMinCap"></param>
        public ShardRegion(string typeName, Func<string, Props> entityProps, ClusterShardingSettings settings, string coordinatorPath, ExtractEntityId extractEntityId, ExtractShardId extractShardId, object handOffStopMessage, IActorRef replicator, int majorityMinCap)
        {
            TypeName = typeName;
            EntityProps = entityProps;
            Settings = settings;
            CoordinatorPath = coordinatorPath;
            ExtractEntityId = extractEntityId;
            ExtractShardId = extractShardId;
            HandOffStopMessage = handOffStopMessage;
            _replicator = replicator;
            _majorityMinCap = majorityMinCap;

            _retryTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(Settings.TunningParameters.RetryInterval, Settings.TunningParameters.RetryInterval, Self, Retry.Instance, Self);
            SetupCoordinatedShutdown();
        }

        private void SetupCoordinatedShutdown()
        {
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterShardingShutdownRegion, "region-shutdown",
                InvokeRegionShutdownFunc, this, Self, Cluster);
        }

        private static readonly Func<ShardRegion, IActorRef, Cluster, Task<Done>> InvokeRegionShutdownFunc = InvokeRegionShutdown;
        private static Task<Done> InvokeRegionShutdown(ShardRegion owner, IActorRef self, Cluster cluster)
        {
            if (cluster.IsTerminated || cluster.SelfMember.Status == MemberStatus.Down)
            {
                return Task.FromResult(Done.Instance);
            }
            else
            {
                self.Tell(GracefulShutdown.Instance);
                return owner._gracefulShutdownProgress.Task;
            }
        }

        private ILoggingAdapter _log;
        /// <summary>
        /// TBD
        /// </summary>
        public ILoggingAdapter Log { get { return _log ?? (_log = Context.GetLogger()); } }
        /// <summary>
        /// TBD
        /// </summary>
        public bool GracefulShutdownInProgress { get; private set; }
        /// <summary>
        /// TBD
        /// </summary>
        public int TotalBufferSize { get { return ShardBuffers.Aggregate(0, (acc, entity) => acc + entity.Value.Count); } }

        /// <summary>
        /// TBD
        /// </summary>
        protected ActorSelection CoordinatorSelection
        {
            get
            {
                var firstMember = MembersByAge.FirstOrDefault();
                return firstMember == null ? null : Context.ActorSelection(firstMember.Address + CoordinatorPath);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected object RegistrationMessage
        {
            get
            {
                if (EntityProps != null)
                    return new PersistentShardCoordinator.Register(Self);
                return new PersistentShardCoordinator.RegisterProxy(Self);
            }
        }

        /// <inheritdoc cref="ActorBase.PreStart"/>
        /// <summary>
        /// Subscribe to MemberEvent, re-subscribe when restart
        /// </summary>
        protected override void PreStart()
        {
            Cluster.Subscribe(Self, typeof(ClusterEvent.IMemberEvent));
            var passivateIdleEntityAfter = Settings.PassivateIdleEntityAfter;
            if (passivateIdleEntityAfter > TimeSpan.Zero)
            {
                var log = Log;
                if (log.IsInfoEnabled) log.IdleEntitiesWillBePassivatedAfter(passivateIdleEntityAfter);
            }
        }

        /// <inheritdoc cref="ActorBase.PostStop"/>
        protected override void PostStop()
        {
            base.PostStop();
            Cluster.Unsubscribe(Self);
            _gracefulShutdownProgress.TrySetResult(Done.Instance);
            _retryTask.Cancel();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="member">TBD</param>
        /// <returns>TBD</returns>
        protected bool MatchingRole(Member member)
        {
            return string.IsNullOrEmpty(Settings.Role) || member.HasRole(Settings.Role);
        }

        private void ChangeMembers(IImmutableSet<Member> newMembers)
        {
            var before = MembersByAge.FirstOrDefault();
            var after = newMembers.FirstOrDefault();
            MembersByAge = newMembers;
            if (!Equals(before, after))
            {
                if (Log.IsDebugEnabled) { Log.CoordinatorMoved(before, after); }

                _coordinator = null;
                Register();
            }
        }

        /// <inheritdoc cref="ActorBase.Receive"/>
        protected override bool Receive(object message)
        {
            switch (message)
            {
                case Terminated t:
                    HandleTerminated(t);
                    return true;
                case ShardInitialized si:
                    InitializeShard(si.ShardId, Sender);
                    return true;
                case ClusterEvent.IClusterDomainEvent cde:
                    HandleClusterEvent(cde);
                    return true;
                case ClusterEvent.CurrentClusterState ccs:
                    HandleClusterState(ccs);
                    return true;
                case PersistentShardCoordinator.ICoordinatorMessage cm:
                    HandleCoordinatorMessage(cm);
                    return true;
                case IShardRegionCommand src:
                    HandleShardRegionCommand(src);
                    return true;
                case IShardRegionQuery srq:
                    HandleShardRegionQuery(srq);
                    return true;
                case RestartShard _:
                    DeliverMessage(message, Sender);
                    return true;
                case StartEntity _:
                    DeliverStartEntity(message, Sender);
                    return true;
                case var _ when ExtractEntityId(message) != null:
                    DeliverMessage(message, Sender);
                    return true;
                default:
                    if (Log.IsWarningEnabled) Log.MessageDoesNotHaveAnExtractorDefinedInShard(TypeName, message);
                    return false;
            }
        }

        private void InitializeShard(ShardId id, IActorRef shardRef)
        {
            if (Log.IsDebugEnabled) Log.ShardWasInitialized(id);
            StartingShards = StartingShards.Remove(id);
            DeliverBufferedMessage(id, shardRef);
        }

        private void Register()
        {
            var coordinator = CoordinatorSelection;
            coordinator?.Tell(RegistrationMessage);
            if (ShardBuffers.Count != 0 && _retryCount >= RetryCountThreshold)
            {
                if (Log.IsWarningEnabled)
                {
                    if (coordinator != null)
                    {
                        Log.TryingToRregisterToCoordinatorAtButNoAcknowledgement(Cluster, coordinator, MembersByAge, TotalBufferSize);
                    }
                    else
                    {
                        Log.NoCoordinatorFoundToRegister(TotalBufferSize);
                    }
                }
            }
        }

        private void DeliverStartEntity(object message, IActorRef sender)
        {
            try
            {
                DeliverMessage(message, sender);
            }
            catch (Exception ex)
            {
                //case ex: MatchError ⇒
                Log.WhenUsingRememberEntitiesTheShardIdExtractorMustHandleShardRegionStartEntity(ex);
            }
        }

        private void DeliverMessage(object message, IActorRef sender)
        {
            if (message is RestartShard restart)
            {
                var shardId = restart.ShardId;
                if (RegionByShard.TryGetValue(shardId, out var regionRef))
                {
                    if (Self.Equals(regionRef))
                        GetShard(shardId);
                }
                else
                {
                    if (!ShardBuffers.TryGetValue(shardId, out var buffer))
                    {
                        buffer = ImmutableList<KeyValuePair<object, IActorRef>>.Empty;
                        if (Log.IsDebugEnabled) Log.RequestShard(shardId);
                        _coordinator?.Tell(new PersistentShardCoordinator.GetShardHome(shardId));
                    }

                    if (Log.IsDebugEnabled) Log.BufferMessageForShard(shardId, buffer.Count);
                    ShardBuffers = ShardBuffers.SetItem(shardId, buffer.Add(new KeyValuePair<object, IActorRef>(message, sender)));
                }
            }
            else
            {
                var shardId = ExtractShardId(message);
                if (RegionByShard.TryGetValue(shardId, out var region))
                {
                    if (region.Equals(Self))
                    {
                        var sref = GetShard(shardId);
                        if (Equals(sref, ActorRefs.Nobody))
                            BufferMessage(shardId, message, sender);
                        else
                        {
                            if (ShardBuffers.TryGetValue(shardId, out var buffer))
                            {
                                // Since now messages to a shard is buffered then those messages must be in right order
                                BufferMessage(shardId, message, sender);
                                DeliverBufferedMessage(shardId, sref);
                            }
                            else
                                sref.Tell(message, sender);
                        }
                    }
                    else
                    {
                        if (Log.IsDebugEnabled) Log.ForwardingRequestForShard(shardId, region);
                        region.Tell(message, sender);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(shardId))
                    {
                        Log.ShardMustNotBeEmptyDroppingMessage(message);
                        Context.System.DeadLetters.Tell(message);
                    }
                    else
                    {
                        if (!ShardBuffers.ContainsKey(shardId))
                        {
                            if (Log.IsDebugEnabled) Log.RequestShard(shardId);
                            _coordinator?.Tell(new PersistentShardCoordinator.GetShardHome(shardId));
                        }

                        BufferMessage(shardId, message, sender);
                    }
                }
            }
        }

        private void BufferMessage(ShardId shardId, Msg message, IActorRef sender)
        {
            var totalBufferSize = TotalBufferSize;
            if (totalBufferSize >= Settings.TunningParameters.BufferSize)
            {
                if (_loggedFullBufferWarning)
                {
                    if (Log.IsDebugEnabled) Log.BufferIsFullDroppingMessageForShard(shardId, true);
                }
                else
                {
                    if (Log.IsWarningEnabled) Log.BufferIsFullDroppingMessageForShard(shardId, false);
                    _loggedFullBufferWarning = true;
                }

                Context.System.DeadLetters.Tell(message);
            }
            else
            {
                if (!ShardBuffers.TryGetValue(shardId, out var buffer))
                    buffer = ImmutableList<KeyValuePair<Msg, IActorRef>>.Empty;
                ShardBuffers = ShardBuffers.SetItem(shardId, buffer.Add(new KeyValuePair<object, IActorRef>(message, sender)));

                // log some insight to how buffers are filled up every 10% of the buffer capacity
                var total = totalBufferSize + 1;
                var bufferSize = Settings.TunningParameters.BufferSize;
                if (total % (bufferSize / 10) == 0)
                {
                    if ((total > bufferSize / 2))
                    {
                        if (Log.IsWarningEnabled) Log.ShardRegionForTyp1eNameTheCoordinatorMightNotBeAvailable(TypeName, total, bufferSize);
                    }
                    else
                    {
                        if (Log.IsInfoEnabled) Log.ShardRegionForTypeName(TypeName, total, bufferSize);
                    }
                }
            }
        }

        private void HandleShardRegionCommand(IShardRegionCommand command)
        {
            switch (command)
            {
                case Retry _:
                    if (ShardBuffers.Count != 0) _retryCount++;

                    if (_coordinator == null) Register();
                    else
                    {
                        SendGracefulShutdownToCoordinator();
                        RequestShardBufferHomes();
                        TryCompleteGracefulShutdown();
                    }
                    break;
                case GracefulShutdown _:
                    if (Log.IsDebugEnabled) Log.StartingGracefulShutdownOfRegionAndAllItsShards();
                    GracefulShutdownInProgress = true;
                    SendGracefulShutdownToCoordinator();
                    TryCompleteGracefulShutdown();
                    break;
                default:
                    Unhandled(command);
                    break;
            }
        }

        private void HandleShardRegionQuery(IShardRegionQuery query)
        {
            switch (query)
            {
                case GetCurrentRegions _:
                    if (_coordinator != null) _coordinator.Forward(query);
                    else Sender.Tell(new CurrentRegions(ImmutableHashSet<Address>.Empty));
                    break;
                case GetShardRegionState _:
                    ReplyToRegionStateQuery(Sender);
                    break;
                case GetShardRegionStats _:
                    ReplyToRegionStatsQuery(Sender);
                    break;
                case GetClusterShardingStats _:
                    if (_coordinator != null)
                        _coordinator.Forward(query);
                    else
                        Sender.Tell(new ClusterShardingStats(ImmutableDictionary<Address, ShardRegionStats>.Empty));
                    break;
                default:
                    Unhandled(query);
                    break;
            }
        }

        private void ReplyToRegionStateQuery(IActorRef sender)
        {
            AskAllShardsAsync<Shard.CurrentShardState>(Shard.GetCurrentShardState.Instance)
                .ContinueWith(AskCurrentShardStateContinuationFunc, TaskContinuationOptions.ExecuteSynchronously).PipeTo(sender);
        }

        private static readonly Func<Task<Tuple<ShardId, Shard.CurrentShardState>[]>, CurrentShardRegionState> AskCurrentShardStateContinuationFunc = AskCurrentShardStateContinuation;
        private static CurrentShardRegionState AskCurrentShardStateContinuation(Task<Tuple<ShardId, Shard.CurrentShardState>[]> shardStates)
        {
            if (shardStates.IsCanceled)
                return new CurrentShardRegionState(ImmutableHashSet<ShardState>.Empty);

            if (shardStates.IsFaulted)
                throw shardStates.Exception; //TODO check if this is the right way

            return new CurrentShardRegionState(shardStates.Result.Select(x => new ShardState(x.Item1, x.Item2.EntityIds.ToImmutableHashSet(StringComparer.Ordinal))).ToImmutableHashSet());
        }

        private void ReplyToRegionStatsQuery(IActorRef sender)
        {
            AskAllShardsAsync<Shard.ShardStats>(Shard.GetShardStats.Instance)
                .ContinueWith(AskShardStatsContinuationFunc, TaskContinuationOptions.ExecuteSynchronously).PipeTo(sender);
        }

        private static readonly Func<Task<Tuple<ShardId, Shard.ShardStats>[]>, ShardRegionStats> AskShardStatsContinuationFunc = AskShardStatsContinuation;
        private static ShardRegionStats AskShardStatsContinuation(Task<Tuple<ShardId, Shard.ShardStats>[]> shardStats)
        {
            if (shardStats.IsCanceled)
                return new ShardRegionStats(ImmutableDictionary<string, int>.Empty);

            if (shardStats.IsFaulted)
                throw shardStats.Exception; //TODO check if this is the right way

            return new ShardRegionStats(shardStats.Result.ToImmutableDictionary(x => x.Item1, x => x.Item2.EntityCount));
        }

        private Task<Tuple<ShardId, T>[]> AskAllShardsAsync<T>(object message)
        {
            var timeout = TimeSpan.FromSeconds(3);
            var tasks = Shards.Select(entity => entity.Value.Ask<T>(message, timeout).Then(Helper<T>.AfterAskShardFunc, entity.Key, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion));
            return Task.WhenAll(tasks);
        }

        sealed class Helper<T>
        {
            public static readonly Func<T, ShardId, Tuple<ShardId, T>> AfterAskShardFunc = AfterAskShard;
            private static Tuple<ShardId, T> AfterAskShard(T result, ShardId shardId) => Tuple.Create(shardId, result);
        }

        private List<ActorSelection> GracefulShutdownCoordinatorSelections
        {
            get
            {
                return
                    MembersByAge.Take(2)
                        .Select(m => Context.ActorSelection(new RootActorPath(m.Address) + CoordinatorPath))
                        .ToList();
            }
        }

        private void TryCompleteGracefulShutdown()
        {
            if (GracefulShutdownInProgress && Shards.Count == 0 && ShardBuffers.Count == 0)
                Context.Stop(Self);     // all shards have been rebalanced, complete graceful shutdown
        }

        private void SendGracefulShutdownToCoordinator()
        {
            if (GracefulShutdownInProgress)
                GracefulShutdownCoordinatorSelections
                    .ForEach(c => c.Tell(new PersistentShardCoordinator.GracefulShutdownRequest(Self)));
        }

        private void HandleCoordinatorMessage(PersistentShardCoordinator.ICoordinatorMessage message)
        {
            switch (message)
            {
                case PersistentShardCoordinator.HostShard hs:
                    {
                        var shard = hs.Shard;
                        if (Log.IsDebugEnabled) Log.HostShard(shard);
                        RegionByShard = RegionByShard.SetItem(shard, Self);
                        UpdateRegionShards(Self, shard);

                        // Start the shard, if already started this does nothing
                        GetShard(shard);

                        Sender.Tell(new PersistentShardCoordinator.ShardStarted(shard));
                    }
                    break;
                case PersistentShardCoordinator.ShardHome home:
                    if (Log.IsDebugEnabled) Log.ShardLocatedAt(home);

                    if (RegionByShard.TryGetValue(home.Shard, out var region))
                    {
                        if (region.Equals(Self) && !home.Ref.Equals(Self))
                        {
                            // should not happen, inconsistency between ShardRegion and PersistentShardCoordinator
                            ThrowHelper.ThrowIllegalStateException_UnexpectedChangeOfShard(home);
                        }
                    }

                    RegionByShard = RegionByShard.SetItem(home.Shard, home.Ref);
                    UpdateRegionShards(home.Ref, home.Shard);

                    if (!home.Ref.Equals(Self))
                        Context.Watch(home.Ref);

                    if (home.Ref.Equals(Self))
                    {
                        var shardRef = GetShard(home.Shard);
                        if (!Equals(shardRef, ActorRefs.Nobody))
                            DeliverBufferedMessage(home.Shard, shardRef);
                    }
                    else
                        DeliverBufferedMessage(home.Shard, home.Ref);
                    break;
                case PersistentShardCoordinator.RegisterAck ra:
                    _coordinator = ra.Coordinator;
                    Context.Watch(_coordinator);
                    RequestShardBufferHomes();
                    break;
                case PersistentShardCoordinator.BeginHandOff bho:
                    {
                        var shard = bho.Shard;
                        if (Log.IsDebugEnabled) Log.BeginHandOffShard(shard);
                        if (RegionByShard.TryGetValue(shard, out var regionRef))
                        {
                            if (!Regions.TryGetValue(regionRef, out var updatedShards))
                                updatedShards = ImmutableHashSet<ShardId>.Empty;

                            updatedShards = updatedShards.Remove(shard);

                            Regions = updatedShards.Count == 0
                                ? Regions.Remove(regionRef)
                                : Regions.SetItem(regionRef, updatedShards);

                            RegionByShard = RegionByShard.Remove(shard);
                        }

                        Sender.Tell(new PersistentShardCoordinator.BeginHandOffAck(shard));
                    }
                    break;
                case PersistentShardCoordinator.HandOff ho:
                    {
                        var shard = ho.Shard;
                        if (Log.IsDebugEnabled) Log.HandOffShard(shard);

                        // must drop requests that came in between the BeginHandOff and now,
                        // because they might be forwarded from other regions and there
                        // is a risk or message re-ordering otherwise
                        if (ShardBuffers.ContainsKey(shard))
                        {
                            ShardBuffers = ShardBuffers.Remove(shard);
                            _loggedFullBufferWarning = false;
                        }

                        if (Shards.TryGetValue(shard, out var actorRef))
                        {
                            HandingOff = HandingOff.Add(actorRef);
                            actorRef.Forward(message);
                        }
                        else
                            Sender.Tell(new PersistentShardCoordinator.ShardStopped(shard));
                    }
                    break;
                default:
                    Unhandled(message);
                    break;
            }
        }

        private void UpdateRegionShards(IActorRef regionRef, string shard)
        {
            if (!Regions.TryGetValue(regionRef, out var shards))
                shards = ImmutableSortedSet<ShardId>.Empty;
            Regions = Regions.SetItem(regionRef, shards.Add(shard));
        }

        private void RequestShardBufferHomes()
        {
            var isDebugEnabled = Log.IsDebugEnabled;
            foreach (var buffer in ShardBuffers)
            {
                if (_retryCount >= RetryCountThreshold)
                {
                    Log.RetryRequestForShardHomesFromCoordinator2(buffer.Key, _coordinator, buffer.Value.Count);
                }
                else
                {
                    if (isDebugEnabled) Log.RetryRequestForShardHomesFromCoordinator(buffer.Key, _coordinator, buffer.Value.Count);
                }

                _coordinator.Tell(new PersistentShardCoordinator.GetShardHome(buffer.Key));
            }
        }

        private void DeliverBufferedMessage(ShardId shardId, IActorRef receiver)
        {
            if (ShardBuffers.TryGetValue(shardId, out var buffer))
            {
                if (Log.IsDebugEnabled) Log.DeliverBufferedMessagesForShard(buffer.Count, shardId);

                foreach (var m in buffer)
                    receiver.Tell(m.Key, m.Value);

                ShardBuffers = ShardBuffers.Remove(shardId);
            }

            _loggedFullBufferWarning = false;
            _retryCount = 0;
        }

        private IActorRef GetShard(ShardId id)
        {
            if (StartingShards.Contains(id))
                return ActorRefs.Nobody;

            //TODO: change on ConcurrentDictionary.GetOrAdd?
            if (!Shards.TryGetValue(id, out var region))
            {
                if (EntityProps == null) ThrowHelper.ThrowIllegalStateException_ShardMustNotBeAllocatedToAProxyOnlyShardRegion();

                if (ShardsByRef.Values.All(shardId => shardId != id))
                {
                    if (Log.IsDebugEnabled) Log.StartingShardInRegion(id);

                    var name = Uri.EscapeDataString(id);
                    var shardRef = Context.Watch(Context.ActorOf(Sharding.Shards.Props(
                        TypeName,
                        id,
                        EntityProps,
                        Settings,
                        ExtractEntityId,
                        ExtractShardId,
                        HandOffStopMessage,
                        _replicator,
                        _majorityMinCap).WithDispatcher(Context.Props.Dispatcher), name));

                    ShardsByRef = ShardsByRef.SetItem(shardRef, id);
                    Shards = Shards.SetItem(id, shardRef);
                    StartingShards = StartingShards.Add(id);
                    return shardRef;
                }
            }

            return region ?? ActorRefs.Nobody;
        }

        private void HandleClusterState(ClusterEvent.CurrentClusterState state)
        {
            var members = ImmutableSortedSet<Member>.Empty.WithComparer(AgeOrdering).Union(state.Members.Where(m => m.Status == MemberStatus.Up && MatchingRole(m)));
            ChangeMembers(members);
        }

        private void HandleClusterEvent(ClusterEvent.IClusterDomainEvent e)
        {
            switch (e)
            {

                case ClusterEvent.MemberUp mu:
                    {
                        var m = mu.Member;
                        if (MatchingRole(m))
                            ChangeMembers(MembersByAge.Remove(m).Add(m)); // replace
                    }
                    break;
                case ClusterEvent.MemberRemoved mr:
                    {
                        var m = mr.Member;
                        if (m.UniqueAddress == Cluster.SelfUniqueAddress)
                            Context.Stop(Self);
                        else if (MatchingRole(m))
                            ChangeMembers(MembersByAge.Remove(m));
                    }
                    break;
                case ClusterEvent.IMemberEvent _:
                    // these are expected, no need to warn about them
                    break;
                default:
                    Unhandled(e);
                    break;
            }
        }

        private void HandleTerminated(Terminated terminated)
        {
            if (_coordinator != null && _coordinator.Equals(terminated.ActorRef))
                _coordinator = null;
            else if (Regions.TryGetValue(terminated.ActorRef, out var shards))
            {
                RegionByShard = RegionByShard.RemoveRange(shards);
                Regions = Regions.Remove(terminated.ActorRef);

                if (Log.IsDebugEnabled) { Log.RegionWithShardsTerminated(terminated, shards); }
            }
            else if (ShardsByRef.TryGetValue(terminated.ActorRef, out var shard))
            {
                ShardsByRef = ShardsByRef.Remove(terminated.ActorRef);
                Shards = Shards.Remove(shard);
                StartingShards = StartingShards.Remove(shard);
                if (HandingOff.Contains(terminated.ActorRef))
                {
                    HandingOff = HandingOff.Remove(terminated.ActorRef);
                    if (Log.IsDebugEnabled) Log.ShardHandoffComplete(shard);
                }
                else
                {
                    // if persist fails it will stop
                    if (Log.IsDebugEnabled) Log.ShardTerminatedWhileNotBeingHandedOff(shard);
                    if (Settings.RememberEntities)
                        Context.System.Scheduler.ScheduleTellOnce(Settings.TunningParameters.ShardFailureBackoff, Self, new RestartShard(shard), Self);
                }

                TryCompleteGracefulShutdown();
            }
        }
    }
}
