//-----------------------------------------------------------------------
// <copyright file="Replicator.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData.Internal;
using Akka.Serialization;
using Akka.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Akka.DistributedData.Durable;
using Akka.Event;
using CuteAnt;
using Status = Akka.DistributedData.Internal.Status;

namespace Akka.DistributedData
{
    /// <summary>
    /// <para>
    /// A replicated in-memory data store supporting low latency and high availability
    /// requirements.
    /// 
    /// The <see cref="Replicator"/> actor takes care of direct replication and gossip based
    /// dissemination of Conflict Free Replicated Data Types (CRDTs) to replicas in the
    /// the cluster.
    /// The data types must be convergent CRDTs and implement <see cref="IReplicatedData{T}"/>, i.e.
    /// they provide a monotonic merge function and the state changes always converge.
    /// 
    /// You can use your own custom <see cref="IReplicatedData{T}"/> or <see cref="IDeltaReplicatedData{T,TDelta}"/> types,
    /// and several types are provided by this package, such as:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <term>Counters</term>
    ///         <description><see cref="GCounter"/>, <see cref="PNCounter"/></description> 
    ///     </item>
    ///     <item>
    ///         <term>Registers</term>
    ///         <description><see cref="LWWRegister{T}"/>, <see cref="Flag"/></description>
    ///     </item>
    ///     <item>
    ///         <term>Sets</term>
    ///         <description><see cref="GSet{T}"/>, <see cref="ORSet{T}"/></description>
    ///     </item>
    ///     <item>
    ///         <term>Maps</term> 
    ///         <description><see cref="ORDictionary{TKey,TValue}"/>, <see cref="ORMultiValueDictionary{TKey,TValue}"/>, <see cref="LWWDictionary{TKey,TValue}"/>, <see cref="PNCounterDictionary{TKey}"/></description>
    ///     </item>
    /// </list>
    /// <para>
    /// For good introduction to the CRDT subject watch the
    /// <a href="http://www.ustream.tv/recorded/61448875">The Final Causal Frontier</a>
    /// and <a href="http://vimeo.com/43903960">Eventually Consistent Data Structures</a>
    /// talk by Sean Cribbs and and the
    /// <a href="http://research.microsoft.com/apps/video/dl.aspx?id=153540">talk by Mark Shapiro</a>
    /// and read the excellent paper <a href="http://hal.upmc.fr/docs/00/55/55/88/PDF/techreport.pdf">
    /// A comprehensive study of Convergent and Commutative Replicated Data Types</a>
    /// by Mark Shapiro et. al.
    /// </para>
    /// <para>
    /// The <see cref="Replicator"/> actor must be started on each node in the cluster, or group of
    /// nodes tagged with a specific role. It communicates with other <see cref="Replicator"/> instances
    /// with the same path (without address) that are running on other nodes . For convenience it
    /// can be used with the <see cref="DistributedData"/> extension but it can also be started as an ordinary
    /// actor using the <see cref="Props"/>. If it is started as an ordinary actor it is important
    /// that it is given the same name, started on same path, on all nodes.
    /// </para>
    /// <para>
    /// <a href="paper http://arxiv.org/abs/1603.01529">Delta State Replicated Data Types</a>
    /// is supported. delta-CRDT is a way to reduce the need for sending the full state
    /// for updates. For example adding element 'c' and 'd' to set {'a', 'b'} would
    /// result in sending the delta {'c', 'd'} and merge that with the state on the
    /// receiving side, resulting in set {'a', 'b', 'c', 'd'}.
    /// </para>
    /// <para>
    /// The protocol for replicating the deltas supports causal consistency if the data type
    /// is marked with <see cref="IRequireCausualDeliveryOfDeltas"/>. Otherwise it is only eventually
    /// consistent. Without causal consistency it means that if elements 'c' and 'd' are
    /// added in two separate <see cref="Update"/> operations these deltas may occasionally be propagated
    /// to nodes in different order than the causal order of the updates. For this example it
    /// can result in that set {'a', 'b', 'd'} can be seen before element 'c' is seen. Eventually
    /// it will be {'a', 'b', 'c', 'd'}.
    /// </para>
    /// <para>
    /// == Update ==
    /// 
    /// To modify and replicate a <see cref="IReplicatedData{T}"/> value you send a <see cref="Update"/> message
    /// to the local <see cref="Replicator"/>.
    /// The current data value for the `key` of the <see cref="Update"/> is passed as parameter to the `modify`
    /// function of the <see cref="Update"/>. The function is supposed to return the new value of the data, which
    /// will then be replicated according to the given consistency level.
    /// 
    /// The `modify` function is called by the `Replicator` actor and must therefore be a pure
    /// function that only uses the data parameter and stable fields from enclosing scope. It must
    /// for example not access `sender()` reference of an enclosing actor.
    /// 
    /// <see cref="Update"/> is intended to only be sent from an actor running in same local `ActorSystem` as
    /// the <see cref="Replicator"/>, because the `modify` function is typically not serializable.
    /// 
    /// You supply a write consistency level which has the following meaning:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="WriteLocal"/></term>
    ///         <description>
    ///             The value will immediately only be written to the local replica, and later disseminated with gossip.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="WriteTo"/></term>
    ///         <description>
    ///             The value will immediately be written to at least <see cref="WriteTo.Count"/> replicas, including the local replica.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="WriteMajority"/></term>
    ///         <description>
    ///             The value will immediately be written to a majority of replicas, i.e. at least `N/2 + 1` replicas, 
    ///             where N is the number of nodes in the cluster (or cluster role group).
    ///         </description>     
    ///     </item>
    ///     <item>
    ///         <term><see cref="WriteAll"/></term> 
    ///         <description>
    ///             The value will immediately be written to all nodes in the cluster (or all nodes in the cluster role group).
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// As reply of the <see cref="Update"/> a <see cref="UpdateSuccess"/> is sent to the sender of the
    /// <see cref="Update"/> if the value was successfully replicated according to the supplied consistency
    /// level within the supplied timeout. Otherwise a <see cref="IUpdateFailure"/> subclass is
    /// sent back. Note that a <see cref="UpdateTimeout"/> reply does not mean that the update completely failed
    /// or was rolled back. It may still have been replicated to some nodes, and will eventually
    /// be replicated to all nodes with the gossip protocol.
    /// 
    /// You will always see your own writes. For example if you send two <see cref="Update"/> messages
    /// changing the value of the same `key`, the `modify` function of the second message will
    /// see the change that was performed by the first <see cref="Update"/> message.
    /// 
    /// In the <see cref="Update"/> message you can pass an optional request context, which the <see cref="Replicator"/>
    /// does not care about, but is included in the reply messages. This is a convenient
    /// way to pass contextual information (e.g. original sender) without having to use <see cref="Ask"/>
    /// or local correlation data structures.
    /// </para>
    /// <para>
    /// == Get ==
    /// 
    /// To retrieve the current value of a data you send <see cref="Get"/> message to the
    /// <see cref="Replicator"/>. You supply a consistency level which has the following meaning:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="ReadLocal"/></term> 
    ///         <description>The value will only be read from the local replica.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="ReadFrom"/></term> 
    ///         <description>The value will be read and merged from <see cref="ReadFrom.N"/> replicas, including the local replica.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="ReadMajority"/></term>
    ///         <description>
    ///             The value will be read and merged from a majority of replicas, i.e. at least `N/2 + 1` replicas, where N is the number of nodes in the cluster (or cluster role group).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="ReadAll"/></term>
    ///         <description>The value will be read and merged from all nodes in the cluster (or all nodes in the cluster role group).</description>
    ///     </item>
    /// </list>
    /// 
    /// As reply of the <see cref="Get"/> a <see cref="GetSuccess"/> is sent to the sender of the
    /// <see cref="Get"/> if the value was successfully retrieved according to the supplied consistency
    /// level within the supplied timeout. Otherwise a <see cref="GetFailure"/> is sent.
    /// If the key does not exist the reply will be <see cref="NotFound"/>.
    /// 
    /// You will always read your own writes. For example if you send a <see cref="Update"/> message
    /// followed by a <see cref="Get"/> of the same `key` the <see cref="Get"/> will retrieve the change that was
    /// performed by the preceding <see cref="Update"/> message. However, the order of the reply messages are
    /// not defined, i.e. in the previous example you may receive the <see cref="GetSuccess"/> before
    /// the <see cref="UpdateSuccess"/>.
    /// 
    /// In the <see cref="Get"/> message you can pass an optional request context in the same way as for the
    /// <see cref="Update"/> message, described above. For example the original sender can be passed and replied
    /// to after receiving and transforming <see cref="GetSuccess"/>.
    /// </para>
    /// <para>
    /// == Subscribe ==
    /// 
    /// You may also register interest in change notifications by sending <see cref="Subscribe"/>
    /// message to the <see cref="Replicator"/>. It will send <see cref="Changed"/> messages to the registered
    /// subscriber when the data for the subscribed key is updated. Subscribers will be notified
    /// periodically with the configured `notify-subscribers-interval`, and it is also possible to
    /// send an explicit <see cref="FlushChanges"/> message to the <see cref="Replicator"/> to notify the subscribers
    /// immediately.
    /// 
    /// The subscriber is automatically removed if the subscriber is terminated. A subscriber can
    /// also be deregistered with the <see cref="Unsubscribe"/> message.
    /// </para>
    /// <para>
    /// == Delete ==
    /// 
    /// A data entry can be deleted by sending a <see cref="Delete"/> message to the local
    /// local <see cref="Replicator"/>. As reply of the <see cref="Delete"/> a <see cref="DeleteSuccess"/> is sent to
    /// the sender of the <see cref="Delete"/> if the value was successfully deleted according to the supplied
    /// consistency level within the supplied timeout. Otherwise a <see cref="ReplicationDeleteFailure"/>
    /// is sent. Note that <see cref="ReplicationDeleteFailure"/> does not mean that the delete completely failed or
    /// was rolled back. It may still have been replicated to some nodes, and may eventually be replicated
    /// to all nodes.
    /// 
    /// A deleted key cannot be reused again, but it is still recommended to delete unused
    /// data entries because that reduces the replication overhead when new nodes join the cluster.
    /// Subsequent <see cref="Delete"/>, <see cref="Update"/> and <see cref="Get"/> requests will be replied with <see cref="DataDeleted"/>.
    /// Subscribers will receive <see cref="Deleted"/>.
    /// 
    /// In the <see cref="Delete"/> message you can pass an optional request context in the same way as for the
    /// <see cref="Update"/> message, described above. For example the original sender can be passed and replied
    /// to after receiving and transforming <see cref="DeleteSuccess"/>.
    /// </para>
    /// <para>
    /// == CRDT Garbage ==
    /// 
    /// One thing that can be problematic with CRDTs is that some data types accumulate history (garbage).
    /// For example a <see cref="GCounter"/> keeps track of one counter per node. If a <see cref="GCounter"/> has been updated
    /// from one node it will associate the identifier of that node forever. That can become a problem
    /// for long running systems with many cluster nodes being added and removed. To solve this problem
    /// the <see cref="Replicator"/> performs pruning of data associated with nodes that have been removed from the
    /// cluster. Data types that need pruning have to implement <see cref="IRemovedNodePruning{T}"/>. The pruning consists
    /// of several steps:
    /// <list type="">
    ///     <item>When a node is removed from the cluster it is first important that all updates that were
    /// done by that node are disseminated to all other nodes. The pruning will not start before the
    /// <see cref="ReplicatorSettings.MaxPruningDissemination"/> duration has elapsed. The time measurement is stopped when any
    /// replica is unreachable, but it's still recommended to configure this with certain margin.
    /// It should be in the magnitude of minutes.</item>
    /// <item>The nodes are ordered by their address and the node ordered first is called leader.
    /// The leader initiates the pruning by adding a <see cref="PruningInitialized"/> marker in the data envelope.
    /// This is gossiped to all other nodes and they mark it as seen when they receive it.</item>
    /// <item>When the leader sees that all other nodes have seen the <see cref="PruningInitialized"/> marker
    /// the leader performs the pruning and changes the marker to <see cref="PruningPerformed"/> so that nobody
    /// else will redo the pruning. The data envelope with this pruning state is a CRDT itself.
    /// The pruning is typically performed by "moving" the part of the data associated with
    /// the removed node to the leader node. For example, a <see cref="GCounter"/> is a `Map` with the node as key
    /// and the counts done by that node as value. When pruning the value of the removed node is
    /// moved to the entry owned by the leader node. See <see cref="IRemovedNodePruning{T}.Prune"/>.</item>
    /// <item>Thereafter the data is always cleared from parts associated with the removed node so that
    /// it does not come back when merging. See <see cref="IRemovedNodePruning{T}.PruningCleanup"/></item>
    /// <item>After another `maxPruningDissemination` duration after pruning the last entry from the
    /// removed node the <see cref="PruningPerformed"/> markers in the data envelope are collapsed into a
    /// single tombstone entry, for efficiency. Clients may continue to use old data and therefore
    /// all data are always cleared from parts associated with tombstoned nodes. </item>
    /// </list>
    /// </para>
    /// </summary>
    internal sealed class Replicator : ReceiveActor2
    {
        public static Props Props(ReplicatorSettings settings) =>
            Actor.Props.Create(() => new Replicator(settings)).WithDeploy(Deploy.Local).WithDispatcher(settings.Dispatcher);

        private static readonly byte[] DeletedDigest = EmptyArray<byte>.Instance;
        private static readonly byte[] LazyDigest = new byte[] { 0 };
        private static readonly byte[] NotFoundDigest = new byte[] { 255 };

        private static readonly DataEnvelope DeletedEnvelope = new DataEnvelope(DeletedData.Instance);

        private readonly ReplicatorSettings _settings;

        private readonly Cluster.Cluster _cluster;
        private readonly Address _selfAddress;
        private readonly UniqueAddress _selfUniqueAddress;

        private readonly ICancelable _gossipTask;
        private readonly ICancelable _notifyTask;
        private readonly ICancelable _pruningTask;
        private readonly ICancelable _clockTask;

        private readonly Serializer _serializer;
        private readonly long _maxPruningDisseminationNanos;

        /// <summary>
        /// Cluster nodes, doesn't contain selfAddress.
        /// </summary>
        private ImmutableHashSet<Address> _nodes = ImmutableHashSet<Address>.Empty;

        /// <summary>
        /// Cluster weaklyUp nodes, doesn't contain selfAddress
        /// </summary>
        private ImmutableHashSet<Address> _weaklyUpNodes = ImmutableHashSet<Address>.Empty;

        private ImmutableDictionary<UniqueAddress, long> _removedNodes = ImmutableDictionary<UniqueAddress, long>.Empty;
        private ImmutableDictionary<UniqueAddress, long> _pruningPerformed = ImmutableDictionary<UniqueAddress, long>.Empty;
        private ImmutableHashSet<UniqueAddress> _tombstonedNodes = ImmutableHashSet<UniqueAddress>.Empty;

        private Address _leader = null;
        private bool IsLeader => _leader != null && _leader == _selfAddress;

        /// <summary>
        /// For pruning timeouts are based on clock that is only increased when all nodes are reachable.
        /// </summary>
        private long _previousClockTime;
        private long _allReachableClockTime = 0;
        private ImmutableHashSet<Address> _unreachable = ImmutableHashSet<Address>.Empty;

        /// <summary>
        /// The actual data.
        /// </summary>
        private ImmutableDictionary<string, Tuple<DataEnvelope, byte[]>> _dataEntries = ImmutableDictionary<string, Tuple<DataEnvelope, byte[]>>.Empty;

        /// <summary>
        /// Keys that have changed, Changed event published to subscribers on FlushChanges
        /// </summary>
        private ImmutableHashSet<string> _changed = ImmutableHashSet<string>.Empty;

        /// <summary>
        /// For splitting up gossip in chunks.
        /// </summary>
        private long _statusCount = 0;
        private int _statusTotChunks = 0;

        private readonly Dictionary<string, HashSet<IActorRef>> _subscribers = new Dictionary<string, HashSet<IActorRef>>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<IActorRef>> _newSubscribers = new Dictionary<string, HashSet<IActorRef>>(StringComparer.Ordinal);
        private ImmutableDictionary<string, IKey> _subscriptionKeys = ImmutableDictionary<string, IKey>.Empty;

        private readonly ILoggingAdapter _log;

        private readonly bool _hasDurableKeys;
        private readonly IImmutableSet<string> _durableKeys;
        private readonly IImmutableSet<string> _durableWildcards;
        private readonly IActorRef _durableStore;

        private readonly DeltaPropagationSelector _deltaPropagationSelector;
        private readonly ICancelable _deltaPropagationTask;
        private readonly int _maxDeltaSize;

        public Replicator(ReplicatorSettings settings)
        {
            _settings = settings;
            _cluster = Cluster.Cluster.Get(Context.System);
            _selfAddress = _cluster.SelfAddress;
            _selfUniqueAddress = _cluster.SelfUniqueAddress;
            _log = Context.GetLogger();
            _maxDeltaSize = settings.MaxDeltaSize;

            _localOnlyDeciderFunc = LocalOnlyDecider;

            if (_cluster.IsTerminated) ThrowHelper.ThrowArgumentException_ClusterNodeMustNotBeTerminated();
            if (!string.IsNullOrEmpty(_settings.Role) && !_cluster.SelfRoles.Contains(_settings.Role))
            {
                ThrowHelper.ThrowArgumentException_TheClusterNodeDoesNotHaveTheRole(_selfAddress, _settings);
            }

            _gossipTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(_settings.GossipInterval, _settings.GossipInterval, Self, GossipTick.Instance, Self);
            _notifyTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(_settings.NotifySubscribersInterval, _settings.NotifySubscribersInterval, Self, FlushChanges.Instance, Self);
            _pruningTask = _settings.PruningInterval != TimeSpan.Zero
                ? Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(_settings.PruningInterval, _settings.PruningInterval, Self, RemovedNodePruningTick.Instance, Self)
                : null;
            _clockTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(_settings.GossipInterval, _settings.GossipInterval, Self, ClockTick.Instance, Self);

            _serializer = Context.System.Serialization.FindSerializerForType(typeof(DataEnvelope));
            _maxPruningDisseminationNanos = _settings.MaxPruningDissemination.Ticks * 100;

            _previousClockTime = DateTime.UtcNow.Ticks * 100;

            _hasDurableKeys = settings.DurableKeys.Count > 0;
            var durableKeysBuilder = ImmutableHashSet<string>.Empty.ToBuilder();
            durableKeysBuilder.KeyComparer = StringComparer.Ordinal;
            var durableWildcardsBuilder = ImmutableHashSet<string>.Empty.ToBuilder();
            durableWildcardsBuilder.KeyComparer = StringComparer.Ordinal;
            foreach (var key in settings.DurableKeys)
            {
                if (key.EndsWith("*"))
                    durableWildcardsBuilder.Add(key.Substring(0, key.Length - 1));
                else
                    durableKeysBuilder.Add(key);
            }

            _durableKeys = durableKeysBuilder.ToImmutable();
            _durableWildcards = durableWildcardsBuilder.ToImmutable();

            _durableStore = _hasDurableKeys
                ? Context.Watch(Context.ActorOf(_settings.DurableStoreProps, "durableStore"))
                : Context.System.DeadLetters;

            _deltaPropagationSelector = new ReplicatorDeltaPropagationSelector(this);

            // Derive the deltaPropagationInterval from the gossipInterval.
            // Normally the delta is propagated to all nodes within the gossip tick, so that
            // full state gossip is not needed.
            var deltaPropagationInterval = new TimeSpan(Math.Max(
                (_settings.GossipInterval.Ticks / _deltaPropagationSelector.GossipInternalDivisor),
                TimeSpan.TicksPerMillisecond * 200));
            _deltaPropagationTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(deltaPropagationInterval, deltaPropagationInterval, Self, DeltaPropagationTick.Instance, Self);

            if (_hasDurableKeys) Load();
            else NormalReceive();
        }

        protected override void PreStart()
        {
            if (_hasDurableKeys) _durableStore.Tell(LoadAll.Instance);

            var leaderChangedClass = _settings.Role != null
                ? typeof(ClusterEvent.RoleLeaderChanged)
                : typeof(ClusterEvent.LeaderChanged);
            _cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsEvents,
                typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.IReachabilityEvent), leaderChangedClass);
        }

        protected override void PostStop()
        {
            _cluster.Unsubscribe(Self);
            _gossipTask.Cancel();
            _deltaPropagationTask.Cancel();
            _notifyTask.Cancel();
            _pruningTask?.Cancel();
            _clockTask.Cancel();
        }

        protected override SupervisorStrategy SupervisorStrategy() => new OneForOneStrategy(_localOnlyDeciderFunc);
        private readonly Func<Exception, Directive> _localOnlyDeciderFunc;
        private Directive LocalOnlyDecider(Exception e)
        {
            var fromDurableStore = Equals(Sender, _durableStore) && !Equals(Sender, Context.System.DeadLetters);
            if ((e is LoadFailedException || e is ActorInitializationException) && fromDurableStore)
            {
                _log.StoppingDistributedDataReplicatorDueToLoadOrStartupFailureInDurableStore(e);
                Context.Stop(Self);
                return Directive.Stop;
            }
            else return Actor.SupervisorStrategy.DefaultDecider.Decide(e);
        }

        private DateTime _startTime;
        private int _count;
        private void Load()
        {
            _startTime = DateTime.UtcNow;
            _count = 0;

            NormalReceive();

            Receive<LoadData>(HandleLoadData);
            Receive<LoadAllCompleted>(HandleLoadAllCompleted);
            Receive<GetReplicaCount>(HandleGetReplicaCount0);

            // ignore scheduled ticks when loading durable data
            Receive<RemovedNodePruningTick>(Ignore);
            Receive<FlushChanges>(Ignore);
            Receive<GossipTick>(Ignore);

            // ignore gossip and replication when loading durable data
            Receive<Read>(IgnoreDebug);
            Receive<Write>(IgnoreDebug);
            Receive<Status>(IgnoreDebug);
            Receive<Gossip>(IgnoreDebug);
        }

        private void HandleLoadData(LoadData load)
        {
            _count += load.Data.Count;
            foreach (var entry in load.Data)
            {
                var envelope = entry.Value.Data;
                var newEnvelope = Write(entry.Key, envelope);
                if (!ReferenceEquals(newEnvelope.Data, envelope.Data))
                {
                    _durableStore.Tell(new Store(entry.Key, new DurableDataEnvelope(newEnvelope), null));
                }
            }
        }

        private void HandleLoadAllCompleted(LoadAllCompleted _)
        {
            if (_log.IsDebugEnabled) _log.LoadingEntriesFromDurableStoreTookMs(_count, _startTime);
            Become(NormalReceive);
            Self.Tell(FlushChanges.Instance);
        }

        private void HandleGetReplicaCount0(GetReplicaCount _)
        {
            // 0 until durable data has been loaded, used by test
            Sender.Tell(new ReplicaCount(0));
        }

        private void NormalReceive()
        {
            Receive<Get>(ReceiveGet);
            Receive<Update>(ReceiveUpdate);
            Receive<Read>(ReceiveRead);
            Receive<Write>(ReceiveWrite);
            Receive<ReadRepair>(ReceiveReadRepair);
            Receive<DeltaPropagation>(ReceiveDeltaPropagation);
            Receive<FlushChanges>(ReceiveFlushChanges);
            Receive<DeltaPropagationTick>(ReceiveDeltaPropagationTick);
            Receive<GossipTick>(ReceiveGossipTick);
            Receive<ClockTick>(ReceiveClockTick);
            Receive<Internal.Status>(ReceiveStatus);
            Receive<Gossip>(ReceiveGossip);
            Receive<Subscribe>(ReceiveSubscribe);
            Receive<Unsubscribe>(ReceiveUnsubscribe);
            Receive<Terminated>(ReceiveTerminated);
            Receive<ClusterEvent.MemberWeaklyUp>(ReceiveMemberWeaklyUp);
            Receive<ClusterEvent.MemberUp>(ReceiveMemberUp);
            Receive<ClusterEvent.MemberRemoved>(ReceiveMemberRemoved);
            Receive<ClusterEvent.IMemberEvent>(PatternMatch<ClusterEvent.IMemberEvent>.EmptyAction);
            Receive<ClusterEvent.UnreachableMember>(ReceiveUnreachable);
            Receive<ClusterEvent.ReachableMember>(ReceiveReachable);
            Receive<ClusterEvent.LeaderChanged>(HandleLeaderChanged);
            Receive<ClusterEvent.RoleLeaderChanged>(HandleRoleLeaderChanged);
            Receive<GetKeyIds>(ReceiveGetKeyIds);
            Receive<Delete>(ReceiveDelete);
            Receive<RemovedNodePruningTick>(ReceiveRemovedNodePruningTick);
            Receive<GetReplicaCount>(ReceiveGetReplicaCount);
        }

        private void IgnoreDebug<T>(T msg)
        {
            if (_log.IsDebugEnabled) _log.Debug("ignoring message [{0}] when loading durable data", typeof(T));
        }

        private void Ignore<T>(T msg) { }

        private void ReceiveGet(Get g)
        {
            IKey key = g.Key;
            IReadConsistency consistency = g.Consistency;
            object req = g.Request;
            var localValue = GetData(key.Id);

            if (_log.IsDebugEnabled) _log.ReceivedGetForKey(key, localValue, consistency);

            if (IsLocalGet(consistency))
            {
                if (localValue == null) Sender.Tell(new NotFound(key, req));
                else if (localValue.Data is DeletedData) Sender.Tell(new DataDeleted(key, req));
                else Sender.Tell(new GetSuccess(key, req, localValue.Data));
            }
            else
                Context.ActorOf(ReadAggregator.Props(key, consistency, req, _nodes, _unreachable, localValue, Sender)
                    .WithDispatcher(Context.Props.Dispatcher));
        }

        private bool IsLocalGet(IReadConsistency consistency)
        {
            if (consistency is ReadLocal) return true;
            if (consistency is ReadAll || consistency is ReadMajority) return _nodes.Count == 0;
            return false;
        }

        private void ReceiveRead(Read read)
        {
            Sender.Tell(new ReadResult(GetData(read.Key)));
        }

        private bool MatchingRole(Member m) => string.IsNullOrEmpty(_settings.Role) || m.HasRole(_settings.Role);

        private void ReceiveUpdate(Update msg)
        {
            IKey key = msg.Key;
            object request = msg.Request;

            var localValue = GetData(key.Id);

            try
            {
                Func<IReplicatedData, IReplicatedData> modify = msg.Modify;
                IWriteConsistency consistency = msg.Consistency;

                DataEnvelope envelope;
                IReplicatedData delta;
                if (localValue == null)
                {
                    var d = modify(null);
                    if (d is IDeltaReplicatedData withDelta)
                    {
                        envelope = new DataEnvelope(withDelta.ResetDelta());
                        delta = withDelta.Delta ?? DeltaPropagation.NoDeltaPlaceholder;
                    }
                    else
                    {
                        envelope = new DataEnvelope(d);
                        delta = null;
                    }
                }
                else if (localValue.Data is DeletedData)
                {
                    if (_log.IsDebugEnabled) _log.ReceivedUpdateForDeletedKey(key);
                    Sender.Tell(new DataDeleted(key, request));
                    return;
                }
                else
                {
                    var d = modify(localValue.Data);
                    if (d is IDeltaReplicatedData withDelta)
                    {
                        envelope = localValue.Merge(withDelta.ResetDelta());
                        delta = withDelta.Delta ?? DeltaPropagation.NoDeltaPlaceholder;
                    }
                    else
                    {
                        envelope = localValue.Merge(d);
                        delta = null;
                    }
                }

                // case Success((envelope, delta)) ⇒

                if (_log.IsDebugEnabled) _log.ReceivedUpdateForKey(key);

                // handle the delta
                if (delta != null)
                {
                    _deltaPropagationSelector.Update(key.Id, delta);
                }

                // note that it's important to do deltaPropagationSelector.update before setData,
                // so that the latest delta version is used
                DataEnvelope newEnvelope = SetData(key.Id, envelope);

                var durable = IsDurable(key.Id);
                if (IsLocalUpdate(consistency))
                {
                    if (durable)
                    {
                        var reply = new StoreReply(
                            successMessage: new UpdateSuccess(key, request),
                            failureMessage: new StoreFailure(key, request),
                            replyTo: Sender);
                        _durableStore.Tell(new Store(key.Id, new DurableDataEnvelope(newEnvelope), reply));
                    }
                    else Sender.Tell(new UpdateSuccess(key, request));
                }
                else
                {
                    DataEnvelope writeEnvelope;
                    Delta writeDelta;
                    if (delta == null || delta == DeltaPropagation.NoDeltaPlaceholder)
                    {
                        writeEnvelope = newEnvelope;
                        writeDelta = null;
                    }
                    else if (delta is IRequireCausualDeliveryOfDeltas)
                    {
                        var version = _deltaPropagationSelector.CurrentVersion(key.Id);
                        writeEnvelope = newEnvelope;
                        writeDelta = new Delta(newEnvelope.WithData(delta), version, version);
                    }
                    else
                    {
                        writeEnvelope = newEnvelope.WithData(delta);
                        writeDelta = null;
                    }

                    var writeAggregator = Context.ActorOf(WriteAggregator
                        .Props(key, writeEnvelope, writeDelta, consistency, request, _nodes, _unreachable, Sender, durable)
                        .WithDispatcher(Context.Props.Dispatcher));

                    if (durable)
                    {
                        var reply = new StoreReply(
                            successMessage: new UpdateSuccess(key, request),
                            failureMessage: new StoreFailure(key, request),
                            replyTo: writeAggregator);
                        _durableStore.Tell(new Store(key.Id, new DurableDataEnvelope(envelope), reply));
                    }
                }
            }
            catch (Exception ex)
            {
                if (_log.IsDebugEnabled) _log.ReceivedUpdateForKey(key, ex);
                Sender.Tell(new ModifyFailure(key, "Update failed: " + ex.Message, ex, request));
            }
        }
        private bool IsDurable(string key) =>
            _durableKeys.Contains(key) || (_durableWildcards.Count > 0 && _durableWildcards.Any(_ => key.StartsWith(_, StringComparison.Ordinal)));

        private bool IsLocalUpdate(IWriteConsistency consistency)
        {
            switch (consistency)
            {
                case WriteLocal _:
                    return true;
                case WriteAll _:
                case WriteMajority _:
                    return _nodes.Count == 0;
                default:
                    return false;
            }
        }

        private void ReceiveWrite(Write write)
        {
            WriteAndStore(write.Key, write.Envelope, reply: true);
        }

        private void WriteAndStore(string key, DataEnvelope writeEnvelope, bool reply)
        {
            var newEnvelope = Write(key, writeEnvelope);
            if (newEnvelope != null)
            {
                if (IsDurable(key))
                {
                    var storeReply = reply
                        ? new StoreReply(WriteAck.Instance, WriteNack.Instance, Sender)
                        : null;

                    _durableStore.Tell(new Store(key, new DurableDataEnvelope(newEnvelope), storeReply));
                }
                else if (reply) Sender.Tell(WriteAck.Instance);
            }
            else if (reply) Sender.Tell(WriteNack.Instance);
        }

        private DataEnvelope Write(string key, DataEnvelope writeEnvelope)
        {
            var envelope = GetData(key);
            if (envelope != null)
            {
                if (ReferenceEquals(envelope, writeEnvelope)) return writeEnvelope;
                if (envelope.Data is DeletedData) return DeletedEnvelope; // already deleted

                try
                {
                    // DataEnvelope will mergeDelta when needed
                    var merged = envelope.Merge(writeEnvelope).AddSeen(_selfAddress);
                    return SetData(key, merged);
                }
                catch (ArgumentException e)
                {
                    if (_log.IsWarningEnabled) _log.CouldnotMergeDueTo(key, e);
                    return null;
                }
            }
            else
            {
                // no existing data for the key
                if (writeEnvelope.Data is IReplicatedDelta withDelta)
                    writeEnvelope = writeEnvelope.WithData(withDelta.Zero.MergeDelta(withDelta));

                return SetData(key, writeEnvelope.AddSeen(_selfAddress));
            }
        }

        private void ReceiveReadRepair(ReadRepair rr)
        {
            WriteAndStore(rr.Key, rr.Envelope, reply: false);
            Sender.Tell(ReadRepairAck.Instance);
        }

        private void ReceiveGetKeyIds(GetKeyIds ids)
        {
            var keys = _dataEntries
                .Where(kvp => !(kvp.Value.Item1.Data is DeletedData))
                .Select(x => x.Key)
                .ToImmutableHashSet(StringComparer.Ordinal);
            Sender.Tell(new GetKeysIdsResult(keys));
        }

        private void ReceiveDelete(Delete d)
        {
            IKey key = d.Key;
            IWriteConsistency consistency = d.Consistency;
            object request = d.Request;

            var envelope = GetData(key.Id);
            if (envelope?.Data is DeletedData)
            {
                // already deleted
                Sender.Tell(new DataDeleted(key, request));
            }
            else
            {
                SetData(key.Id, DeletedEnvelope);
                var durable = IsDurable(key.Id);
                if (IsLocalUpdate(consistency))
                {
                    if (durable)
                    {
                        var reply = new StoreReply(
                            successMessage: new DeleteSuccess(key, request),
                            failureMessage: new StoreFailure(key, request),
                            replyTo: Sender);
                        _durableStore.Tell(new Store(key.Id, new DurableDataEnvelope(DeletedEnvelope), reply));
                    }
                    else Sender.Tell(new DeleteSuccess(key, request));
                }
                else
                {
                    var writeAggregator = Context.ActorOf(WriteAggregator
                        .Props(key, DeletedEnvelope, null, consistency, request, _nodes, _unreachable, Sender, durable)
                        .WithDispatcher(Context.Props.Dispatcher));

                    if (durable)
                    {
                        var reply = new StoreReply(
                            successMessage: new DeleteSuccess(key, request),
                            failureMessage: new StoreFailure(key, request),
                            replyTo: writeAggregator);
                        _durableStore.Tell(new Store(key.Id, new DurableDataEnvelope(DeletedEnvelope), reply));
                    }
                }
            }
        }

        private DataEnvelope SetData(string key, DataEnvelope envelope)
        {
            var deltaVersions = envelope.DeltaVersions;
            var currentVersion = _deltaPropagationSelector.CurrentVersion(key);
            var newEnvelope = currentVersion == 0 || currentVersion == deltaVersions.VersionAt(_selfUniqueAddress)
                ? envelope
                : envelope.WithDeltaVersions(deltaVersions.Merge(VersionVector.Create(_selfUniqueAddress, currentVersion)));

            byte[] digest;
            if (_subscribers.ContainsKey(key) && !_changed.Contains(key))
            {
                var oldDigest = GetDigest(key);
                var dig = Digest(newEnvelope);
                if (!Equals(dig, oldDigest))
                    _changed = _changed.Add(key);

                digest = dig;
            }
            else if (newEnvelope.Data is DeletedData) digest = DeletedDigest;
            else digest = LazyDigest;

            _dataEntries = _dataEntries.SetItem(key, Tuple.Create(newEnvelope, digest));
            if (newEnvelope.Data is DeletedData)
            {
                _deltaPropagationSelector.Delete(key);
            }

            return newEnvelope;
        }

        private byte[] GetDigest(string key)
        {
            var contained = _dataEntries.TryGetValue(key, out Tuple<DataEnvelope, byte[]> value);
            if (contained)
            {
                if (Equals(value.Item2, LazyDigest))
                {
                    var digest = Digest(value.Item1);
                    _dataEntries = _dataEntries.SetItem(key, Tuple.Create(value.Item1, digest));
                    return digest;
                }
                else return value.Item2;
            }
            else return NotFoundDigest;
        }

        private byte[] Digest(DataEnvelope envelope)
        {
            if (Equals(envelope.Data, DeletedData.Instance)) return DeletedDigest;

            var bytes = _serializer.ToBinary(envelope.WithoutDeltaVersions());
            var serialized = SHA1.Create().ComputeHash(bytes);
            return serialized;
        }

        private DataEnvelope GetData(string key)
        {
            return !_dataEntries.TryGetValue(key, out var value) ? null : value.Item1;
        }

        private long GetDeltaSequenceNr(string key, UniqueAddress from)
        {
            return _dataEntries.TryGetValue(key, out var tuple) ? tuple.Item1.DeltaVersions.VersionAt(@from) : 0L;
        }

        private bool IsNodeRemoved(UniqueAddress node, IEnumerable<string> keys)
        {
            if (_removedNodes.ContainsKey(node)) return true;

            return keys.Any(key =>
            {
                return _dataEntries.TryGetValue(key, out var tuple)
                    ? tuple.Item1.Pruning.ContainsKey(node)
                    : false;
            });
        }

        private void Notify(string keyId, HashSet<IActorRef> subs)
        {
            var key = _subscriptionKeys[keyId];
            var envelope = GetData(keyId);
            if (envelope != null)
            {
                var msg = envelope.Data is DeletedData
                    ? (object)new DataDeleted(key, null)
                    : new Changed(key, envelope.Data);

                foreach (var sub in subs) sub.Tell(msg);
            }
        }

        private void ReceiveFlushChanges(FlushChanges fc)
        {
            if (_subscribers.Count != 0)
            {
                foreach (var key in _changed)
                {
                    if (_subscribers.TryGetValue(key, out var subs))
                        Notify(key, subs);
                }
            }

            // Changed event is sent to new subscribers even though the key has not changed,
            // i.e. send current value
            if (_newSubscribers.Count != 0)
            {
                foreach (var kvp in _newSubscribers)
                {
                    Notify(kvp.Key, kvp.Value);
                    if (!_subscribers.TryGetValue(kvp.Key, out var set))
                    {
                        set = new HashSet<IActorRef>(ActorRefComparer.Instance);
                        _subscribers.Add(kvp.Key, set);
                    }

                    foreach (var sub in kvp.Value) set.Add(sub);
                }

                _newSubscribers.Clear();
            }

            _changed = ImmutableHashSet<string>.Empty;
        }

        private void ReceiveDeltaPropagationTick(DeltaPropagationTick dpt)
        {
            foreach (var entry in _deltaPropagationSelector.CollectPropagations())
            {
                var node = entry.Key;
                var deltaPropagation = entry.Value;

                // TODO split it to several DeltaPropagation if too many entries
                if (!deltaPropagation.Deltas.IsEmpty) Replica(node).Tell(deltaPropagation);
            }

            if (_deltaPropagationSelector.PropagationCount % _deltaPropagationSelector.GossipInternalDivisor == 0)
                _deltaPropagationSelector.CleanupDeltaEntries();
        }

        private void ReceiveDeltaPropagation(DeltaPropagation msg)
        {
            var from = msg.FromNode;
            try
            {
                var reply = msg.ShouldReply;
                var deltas = msg.Deltas;

                var isDebug = _log.IsDebugEnabled;
                if (isDebug) { _log.ReceivedDeltaPropagationFromContaining(from, deltas); }

                if (IsNodeRemoved(from, deltas.Keys))
                {
                    // Late message from a removed node.
                    // Drop it to avoid merging deltas that have been pruned on one side.
                    if (isDebug) _log.SkippingDeltaPropagationBecauseThatNodeHasBeenRemoved(from);
                }
                else
                {
                    foreach (var entry in deltas)
                    {
                        var key = entry.Key;
                        var delta = entry.Value;
                        var envelope = delta.DataEnvelope;
                        if (envelope.Data is IRequireCausualDeliveryOfDeltas)
                        {
                            var currentSeqNr = GetDeltaSequenceNr(key, from);
                            if (currentSeqNr >= delta.ToSeqNr)
                            {
                                if (isDebug) _log.SkippingDeltaPropagationBecauseToSeqNrAlreadyHandled(from, key, delta, currentSeqNr);
                                if (reply) { Sender.Tell(WriteAck.Instance); }
                            }
                            else if (delta.FromSeqNr > currentSeqNr + 1)
                            {
                                if (isDebug) _log.SkippingDeltaPropagationBecauseMissingDeltas(from, key, currentSeqNr, delta);
                                if (reply) { Sender.Tell(DeltaNack.Instance); }
                            }
                            else
                            {
                                if (isDebug) _log.ApplyingDeltaPropagationFromWithSequenceNumbers(from, key, delta, currentSeqNr);
                                var newEnvelope = envelope.WithDeltaVersions(VersionVector.Create(from, delta.ToSeqNr));
                                WriteAndStore(key, newEnvelope, reply);
                            }
                        }
                        else
                        {
                            // causal delivery of deltas not needed, just apply it
                            WriteAndStore(key, envelope, reply);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // catching in case we need to support rolling upgrades that are
                // mixing nodes with incompatible delta-CRDT types
                if (_log.IsWarningEnabled) _log.CouldnotProcessDeltaPropagation(from, e);
            }
        }

        private void ReceiveGossipTick(GossipTick gossipTick)
        {
            var node = SelectRandomNode(_nodes.Union(_weaklyUpNodes));
            if (node != null)
            {
                GossipTo(node);
            }
        }

        private void GossipTo(Address address)
        {
            var to = Replica(address);
            if (_dataEntries.Count <= _settings.MaxDeltaElements)
            {
                var status = new Internal.Status(_dataEntries.Select(x => new KeyValuePair<string, byte[]>(x.Key, GetDigest(x.Key))).ToImmutableDictionary(StringComparer.Ordinal), chunk: 0, totalChunks: 1);
                to.Tell(status);
            }
            else
            {
                var totChunks = _dataEntries.Count / _settings.MaxDeltaElements;
                for (var i = 1; i <= Math.Min(totChunks, 10); i++)
                {
                    if (totChunks == _statusTotChunks)
                    {
                        _statusCount++;
                    }
                    else
                    {
                        _statusCount = ThreadLocalRandom.Current.Next(0, totChunks);
                        _statusTotChunks = totChunks;
                    }
                    var chunk = (int)(_statusCount % totChunks);
                    var entries = _dataEntries.Where(x => Math.Abs(MurmurHash.StringHash(x.Key)) % totChunks == chunk)
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, GetDigest(x.Key)))
                        .ToImmutableDictionary(StringComparer.Ordinal);
                    var status = new Internal.Status(entries, chunk, totChunks);
                    to.Tell(status);
                }
            }
        }

        private Address SelectRandomNode(ImmutableHashSet<Address> addresses)
        {
            if (addresses.Count > 0)
            {
                var random = ThreadLocalRandom.Current.Next(addresses.Count - 1);
                return addresses.Skip(random).First();
            }

            return null;
        }

        private ActorSelection Replica(Address address) => Context.ActorSelection(Self.Path.ToStringWithAddress(address));

        private bool IsOtherDifferent(string key, byte[] otherDigest)
        {
            var d = GetDigest(key);
            var isFound = !Equals(d, NotFoundDigest);
            var isEqualToOther = Equals(d, otherDigest);
            return isFound && !isEqualToOther;
        }

        private void ReceiveStatus(Internal.Status s)
        {
            var otherDigests = s.Digests;
            var chunk = s.Chunk;
            var totChunks = s.TotalChunks;

            if (_log.IsDebugEnabled)
            {
                _log.ReceivedGossipStatusFrom(Sender, chunk, totChunks, otherDigests);
            }

            // if no data was send we do nothing
            if (otherDigests.Count == 0)
                return;

            var otherDifferentKeys = otherDigests
                .Where(x => IsOtherDifferent(x.Key, x.Value))
                .Select(x => x.Key)
                .ToImmutableHashSet(StringComparer.Ordinal);

            var otherKeys = otherDigests.Keys.ToImmutableHashSet(StringComparer.Ordinal);
            var myKeys = (totChunks == 1
                    ? _dataEntries.Keys
                    : _dataEntries.Keys.Where(x => Math.Abs(MurmurHash.StringHash(x)) % totChunks == chunk))
                .ToImmutableHashSet(StringComparer.Ordinal);

            var otherMissingKeys = myKeys.Except(otherKeys);

            var keys = otherDifferentKeys
                .Union(otherMissingKeys)
                .Take(_settings.MaxDeltaElements)
                .ToArray();

            if (keys.Length != 0)
            {
                if (_log.IsDebugEnabled) { _log.SendingGossipTo(Sender, keys); }

                var g = new Gossip(keys.Select(k => new KeyValuePair<string, DataEnvelope>(k, GetData(k))).ToImmutableDictionary(StringComparer.Ordinal), !otherDifferentKeys.IsEmpty);
                Sender.Tell(g);
            }

            var myMissingKeys = otherKeys.Except(myKeys);
            if (!myMissingKeys.IsEmpty)
            {
                var log = Context.System.Log;
                if (log.IsDebugEnabled) { log.SendingGossipStatusTo(Sender, myMissingKeys); }

                var status = new Internal.Status(myMissingKeys.Select(x => new KeyValuePair<string, byte[]>(x, NotFoundDigest)).ToImmutableDictionary(StringComparer.Ordinal), chunk, totChunks);
                Sender.Tell(status);
            }
        }

        private void ReceiveGossip(Gossip g)
        {
            var updatedData = g.UpdatedData;
            var sendBack = g.SendBack;
            if (_log.IsDebugEnabled) { _log.ReceivedGossipFrom(Sender, updatedData); }

            var replyData = ImmutableDictionary<string, DataEnvelope>.Empty.ToBuilder();
            replyData.KeyComparer = StringComparer.Ordinal;
            foreach (var d in updatedData)
            {
                var key = d.Key;
                var envelope = d.Value;
                var hadData = _dataEntries.ContainsKey(key);
                WriteAndStore(key, envelope, reply: false);
                if (sendBack)
                {
                    var data = GetData(key);
                    if (data != null && (hadData || data.Pruning.Count != 0))
                        replyData[key] = data;
                }
            }

            if (sendBack && replyData.Count != 0) Sender.Tell(new Gossip(replyData.ToImmutable(), sendBack: false));
        }

        private void ReceiveSubscribe(Subscribe s)
        {
            var key = s.Key;
            var subscriber = s.Subscriber;
            if (!_newSubscribers.TryGetValue(key.Id, out var set))
            {
                _newSubscribers[key.Id] = set = new HashSet<IActorRef>(ActorRefComparer.Instance);
            }
            set.Add(subscriber);

            if (!_subscriptionKeys.ContainsKey(key.Id))
                _subscriptionKeys = _subscriptionKeys.SetItem(key.Id, key);

            Context.Watch(subscriber);
        }

        private void ReceiveUnsubscribe(Unsubscribe u)
        {
            var key = u.Key;
            var subscriber = u.Subscriber;
            if (_subscribers.TryGetValue(key.Id, out var set) && set.Remove(subscriber) && set.Count == 0)
                _subscribers.Remove(key.Id);

            if (_newSubscribers.TryGetValue(key.Id, out set) && set.Remove(subscriber) && set.Count == 0)
                _newSubscribers.Remove(key.Id);

            if (!HasSubscriber(subscriber))
                Context.Unwatch(subscriber);

            if (!_subscribers.ContainsKey(key.Id) || !_newSubscribers.ContainsKey(key.Id))
                _subscriptionKeys = _subscriptionKeys.Remove(key.Id);
        }

        private bool HasSubscriber(IActorRef subscriber)
        {
            return _subscribers.Any(kvp => kvp.Value.Contains(subscriber)) ||
                   _newSubscribers.Any(kvp => kvp.Value.Contains(subscriber));
        }

        private void ReceiveTerminated(Terminated t)
        {
            var terminated = t.ActorRef;
            if (Equals(terminated, _durableStore))
            {
                _log.StoppingDistributedDataReplicatorBecauseDurableStoreTerminated();
                Context.Stop(Self);
            }
            else
            {
                var keys1 = _subscribers.Where(x => x.Value.Contains(terminated))
                    .Select(x => x.Key)
                    .ToImmutableHashSet(StringComparer.Ordinal);

                foreach (var k in keys1)
                {
                    if (_subscribers.TryGetValue(k, out var set) && set.Remove(terminated) && set.Count == 0)
                        _subscribers.Remove(k);
                }

                var keys2 = _newSubscribers.Where(x => x.Value.Contains(terminated))
                    .Select(x => x.Key)
                    .ToImmutableHashSet(StringComparer.Ordinal);

                foreach (var k in keys2)
                {
                    if (_newSubscribers.TryGetValue(k, out var set) && set.Remove(terminated) && set.Count == 0)
                        _newSubscribers.Remove(k);
                }

                var allKeys = keys1.Union(keys2);
                foreach (var k in allKeys)
                {
                    if (!_subscribers.ContainsKey(k) && !_newSubscribers.ContainsKey(k))
                        _subscriptionKeys = _subscriptionKeys.Remove(k);
                }
            }
        }

        private void ReceiveMemberWeaklyUp(ClusterEvent.MemberWeaklyUp weaklyUp)
        {
            var m = weaklyUp.Member;
            if (MatchingRole(m) && m.Address != _selfAddress)
            {
                _weaklyUpNodes = _weaklyUpNodes.Add(m.Address);
            }
        }

        private void ReceiveMemberUp(ClusterEvent.MemberUp up)
        {
            var m = up.Member;
            if (MatchingRole(m) && m.Address != _selfAddress)
            {
                _nodes = _nodes.Add(m.Address);
                _weaklyUpNodes = _weaklyUpNodes.Remove(m.Address);
            }
        }

        private void ReceiveMemberRemoved(ClusterEvent.MemberRemoved removed)
        {
            var m = removed.Member;
            if (m.Address == _selfAddress) { Context.Stop(Self); }
            else if (MatchingRole(m))
            {
                _nodes = _nodes.Remove(m.Address);
                _weaklyUpNodes = _weaklyUpNodes.Remove(m.Address);
                if (_log.IsDebugEnabled) _log.AddingRemovedNodeFromMemberRemoved(m);
                _removedNodes = _removedNodes.SetItem(m.UniqueAddress, _allReachableClockTime);
                _unreachable = _unreachable.Remove(m.Address);
                _deltaPropagationSelector.CleanupRemovedNode(m.Address);
            }
        }

        private void ReceiveUnreachable(ClusterEvent.UnreachableMember um)
        {
            var m = um.Member;
            if (MatchingRole(m)) _unreachable = _unreachable.Add(m.Address);
        }

        private void ReceiveReachable(ClusterEvent.ReachableMember rm)
        {
            var m = rm.Member;
            if (MatchingRole(m)) _unreachable = _unreachable.Remove(m.Address);
        }

        private void HandleLeaderChanged(ClusterEvent.LeaderChanged changed)
        {
            ReceiveLeaderChanged(changed.Leader, null);
        }

        private void HandleRoleLeaderChanged(ClusterEvent.RoleLeaderChanged changed)
        {
            ReceiveLeaderChanged(changed.Leader, changed.Role);
        }

        private void ReceiveLeaderChanged(Address leader, string role)
        {
            if (role == _settings.Role) _leader = leader;
        }

        private void ReceiveClockTick(ClockTick clockTick)
        {
            var now = DateTime.UtcNow.Ticks * TimeSpan.TicksPerMillisecond / 100; // we need ticks per nanosec.
            if (_unreachable.Count == 0)
                _allReachableClockTime += (now - _previousClockTime);
            _previousClockTime = now;
        }

        private void ReceiveRemovedNodePruningTick(RemovedNodePruningTick r)
        {
            // See 'CRDT Garbage' section in Replicator Scaladoc for description of the process
            if (_unreachable.IsEmpty)
            {
                if (IsLeader)
                {
                    CollectRemovedNodes();
                    InitRemovedNodePruning();
                }

                PerformRemovedNodePruning();
                DeleteObsoletePruningPerformed();
            }
        }

        private void CollectRemovedNodes()
        {
            var knownNodes = _nodes.Union(_weaklyUpNodes).Union(_removedNodes.Keys.Select(x => x.Address));
            var newRemovedNodes = new HashSet<UniqueAddress>(UniqueAddressComparer.Instance);
            foreach (var pair in _dataEntries)
            {
                if (pair.Value.Item1.Data is IRemovedNodePruning removedNodePruning)
                {
                    newRemovedNodes.UnionWith(removedNodePruning.ModifiedByNodes.Where(n => !(n == _selfUniqueAddress || knownNodes.Contains(n.Address))));
                }
            }

            var debugEnabled = _log.IsDebugEnabled;
            var removedNodesBuilder = _removedNodes.ToBuilder();
            removedNodesBuilder.KeyComparer = UniqueAddressComparer.Instance;
            foreach (var node in newRemovedNodes)
            {
                if (debugEnabled) _log.AddingRemovedNodeFromData(node);
                removedNodesBuilder[node] = _allReachableClockTime;
            }
            _removedNodes = removedNodesBuilder.ToImmutable();
        }

        private void InitRemovedNodePruning()
        {
            // initiate pruning for removed nodes
            var removedSet = _removedNodes
                .Where(x => (_allReachableClockTime - x.Value) > _maxPruningDisseminationNanos)
                .Select(x => x.Key)
                .ToImmutableHashSet(UniqueAddressComparer.Instance);

            if (!removedSet.IsEmpty)
            {
                var debugEnabled = _log.IsDebugEnabled;
                foreach (var entry in _dataEntries)
                {
                    var key = entry.Key;
                    var envelope = entry.Value.Item1;

                    foreach (var removed in removedSet)
                    {
                        if (envelope.NeedPruningFrom(removed))
                        {
                            if (envelope.Data is IRemovedNodePruning)
                            {
                                if (envelope.Pruning.TryGetValue(removed, out var state))
                                {
                                    if (state is PruningInitialized initialized && initialized.Owner != _selfUniqueAddress)
                                    {
                                        var newEnvelope = envelope.InitRemovedNodePruning(removed, _selfUniqueAddress);
                                        if (debugEnabled) _log.InitiatingPruningOfWithData(removed, key);
                                        SetData(key, newEnvelope);
                                    }
                                }
                                else
                                {
                                    var newEnvelope = envelope.InitRemovedNodePruning(removed, _selfUniqueAddress);
                                    if (debugEnabled) _log.InitiatingPruningOfWithData(removed, key);
                                    SetData(key, newEnvelope);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PerformRemovedNodePruning()
        {
            // perform pruning when all seen Init
            var allNodes = _nodes.Union(_weaklyUpNodes);
            var prunningPerformed = new PruningPerformed(DateTime.UtcNow + _settings.PruningMarkerTimeToLive);
            var durablePrunningPerformed = new PruningPerformed(DateTime.UtcNow + _settings.DurablePruningMarkerTimeToLive);

            var debugEnabled = _log.IsDebugEnabled;
            foreach (var entry in _dataEntries)
            {
                var key = entry.Key;
                var envelope = entry.Value.Item1;
                if (envelope.Data is IRemovedNodePruning data)
                {
                    foreach (var entry2 in envelope.Pruning)
                    {
                        if (entry2.Value is PruningInitialized init && init.Owner == _selfUniqueAddress && (allNodes.IsEmpty || allNodes.IsSubsetOf(init.Seen)))
                        {
                            var removed = entry2.Key;
                            var isDurable = IsDurable(key);
                            var newEnvelope = envelope.Prune(removed, isDurable ? durablePrunningPerformed : prunningPerformed);
                            if (debugEnabled) _log.PerformPruningOfFromTo(key, removed, _selfUniqueAddress);
                            SetData(key, newEnvelope);

                            if (!ReferenceEquals(newEnvelope.Data, data) && isDurable)
                            {
                                _durableStore.Tell(new Store(key, new DurableDataEnvelope(newEnvelope), null));
                            }
                        }
                    }
                }
            }
        }

        private void DeleteObsoletePruningPerformed()
        {
            var currentTime = DateTime.UtcNow;
            foreach (var entry in _dataEntries)
            {
                var key = entry.Key;
                var envelope = entry.Value.Item1;
                if (envelope.Data is IRemovedNodePruning)
                {
                    var toRemove = envelope.Pruning
                        .Where(pair => (pair.Value as PruningPerformed)?.IsObsolete(currentTime) ?? false)
                        .Select(pair => pair.Key)
                        .ToArray();

                    if (0u < (uint)toRemove.Length)
                    {
                        var removedNodesBuilder = _removedNodes.ToBuilder();
                        removedNodesBuilder.KeyComparer = UniqueAddressComparer.Instance;
                        var newPruningBuilder = envelope.Pruning.ToBuilder();
                        newPruningBuilder.KeyComparer = UniqueAddressComparer.Instance;

                        removedNodesBuilder.RemoveRange(toRemove);
                        newPruningBuilder.RemoveRange(toRemove);

                        _removedNodes = removedNodesBuilder.ToImmutable();
                        var newEnvelope = envelope.WithPruning(newPruningBuilder.ToImmutable());

                        SetData(key, newEnvelope);
                    }
                }
            }
        }

        private void ReceiveGetReplicaCount(GetReplicaCount c) => Sender.Tell(new ReplicaCount(_nodes.Count + 1));

        #region delta propagation selector

        private sealed class ReplicatorDeltaPropagationSelector : DeltaPropagationSelector
        {
            private readonly Replicator _replicator;

            public ReplicatorDeltaPropagationSelector(Replicator replicator)
            {
                _replicator = replicator;
            }

            public override int GossipInternalDivisor { get; } = 5;

            protected override int MaxDeltaSize => _replicator._maxDeltaSize;

            // TODO optimize, by maintaining a sorted instance variable instead
            protected override ImmutableArray<Address> AllNodes =>
                _replicator._nodes.Union(_replicator._weaklyUpNodes).Except(_replicator._unreachable).OrderBy(x => x).ToImmutableArray();

            protected override DeltaPropagation CreateDeltaPropagation(ImmutableDictionary<string, Tuple<IReplicatedData, long, long>> deltas)
            {
                // Important to include the pruning state in the deltas. For example if the delta is based
                // on an entry that has been pruned but that has not yet been performed on the target node.
                var newDeltas = deltas
                    .Where(x => !Equals(x.Value.Item1, DeltaPropagation.NoDeltaPlaceholder))
                    .Select(x =>
                    {
                        var key = x.Key;
                        var t = x.Value;
                        var envelope = _replicator.GetData(key);
                        return envelope != null
                            ? new KeyValuePair<string, Delta>(key,
                                new Delta(envelope.WithData(t.Item1), t.Item2, t.Item3))
                            : new KeyValuePair<string, Delta>(key,
                                new Delta(new DataEnvelope(t.Item1), t.Item2, t.Item3));
                    })
                    .ToImmutableDictionary(StringComparer.Ordinal);

                return new DeltaPropagation(_replicator._selfUniqueAddress, shouldReply: false, deltas: newDeltas);
            }
        }

        #endregion
    }
}
