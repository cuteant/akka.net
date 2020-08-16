//-----------------------------------------------------------------------
// <copyright file="ClusterSingletonManager.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Coordination;
using Akka.Event;
using Akka.Remote;
using Akka.Util.Internal;
using MessagePack;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.Tools.Singleton
{
    /// <summary>
    /// Control messages used for the cluster singleton
    /// </summary>
    public interface IClusterSingletonMessage // : ISingletonMessage 由 ClusterSingletonMessageSerializer 处理
    {
    }

    /// <summary>
    /// Sent from new oldest to previous oldest to initiate the
    /// hand-over process. <see cref="HandOverInProgress"/>  and <see cref="HandOverDone"/>
    /// are expected replies.
    /// </summary>
    internal sealed class HandOverToMe : IClusterSingletonMessage, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly HandOverToMe Instance = new HandOverToMe();
        private HandOverToMe() { }
    }

    /// <summary>
    /// Confirmation by the previous oldest that the hand
    /// over process, shut down of the singleton actor, has
    /// started.
    /// </summary>
    internal sealed class HandOverInProgress : IClusterSingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly HandOverInProgress Instance = new HandOverInProgress();
        private HandOverInProgress() { }
    }

    /// <summary>
    /// Confirmation by the previous oldest that the singleton
    /// actor has been terminated and the hand-over process is
    /// completed.
    /// </summary>
    internal sealed class HandOverDone : IClusterSingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly HandOverDone Instance = new HandOverDone();
        private HandOverDone() { }
    }

    /// <summary>
    /// Sent from from previous oldest to new oldest to
    /// initiate the normal hand-over process.
    /// Especially useful when new node joins and becomes
    /// oldest immediately, without knowing who was previous
    /// oldest.
    /// </summary>
    internal sealed class TakeOverFromMe : IClusterSingletonMessage, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly TakeOverFromMe Instance = new TakeOverFromMe();
        private TakeOverFromMe() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class Cleanup : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Cleanup Instance = new Cleanup();
        private Cleanup() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class StartOldestChangedBuffer : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly StartOldestChangedBuffer Instance = new StartOldestChangedBuffer();
        private StartOldestChangedBuffer() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class HandOverRetry
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public int Count { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="count">TBD</param>
        [SerializationConstructor]
        public HandOverRetry(int count)
        {
            Count = count;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class TakeOverRetry
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public int Count { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="count">TBD</param>
        [SerializationConstructor]
        public TakeOverRetry(int count)
        {
            Count = count;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    internal sealed class LeaseRetry : INoSerializationVerificationNeeded
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly LeaseRetry Instance = new LeaseRetry();
        private LeaseRetry() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public interface IClusterSingletonData { }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class Uninitialized : IClusterSingletonData, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Uninitialized Instance = new Uninitialized();
        private Uninitialized() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class YoungerData : IClusterSingletonData
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public List<UniqueAddress> Oldest { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="oldest">TBD</param>
        [SerializationConstructor]
        public YoungerData(List<UniqueAddress> oldest)
        {
            Oldest = oldest;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class BecomingOldestData : IClusterSingletonData
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public List<UniqueAddress> PreviousOldest { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="previousOldest">TBD</param>
        [SerializationConstructor]
        public BecomingOldestData(List<UniqueAddress> previousOldest)
        {
            PreviousOldest = previousOldest;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class OldestData : IClusterSingletonData
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public IActorRef Singleton { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="singleton">TBD</param>
        [SerializationConstructor]
        public OldestData(IActorRef singleton)
        {
            Singleton = singleton;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class WasOldestData : IClusterSingletonData
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public IActorRef Singleton { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public UniqueAddress NewOldest { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="singleton">TBD</param>
        /// <param name="newOldest">TBD</param>
        [SerializationConstructor]
        public WasOldestData(IActorRef singleton, UniqueAddress newOldest)
        {
            Singleton = singleton;
            NewOldest = newOldest;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class HandingOverData : IClusterSingletonData
    {

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public IActorRef Singleton { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public IActorRef HandOverTo { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="singleton">TBD</param>
        /// <param name="handOverTo">TBD</param>
        [SerializationConstructor]
        public HandingOverData(IActorRef singleton, IActorRef handOverTo)
        {
            Singleton = singleton;
            HandOverTo = handOverTo;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class StoppingData : IClusterSingletonData
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public IActorRef Singleton { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="singleton">TBD</param>
        [SerializationConstructor]
        public StoppingData(IActorRef singleton)
        {
            Singleton = singleton;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class EndData : IClusterSingletonData, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly EndData Instance = new EndData();
        private EndData() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class AcquiringLeaseData : IClusterSingletonData
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public bool LeaseRequestInProgress { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public IActorRef Singleton { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="leaseRequestInProgress">TBD</param>
        /// <param name="singleton">TBD</param>
        [SerializationConstructor]
        public AcquiringLeaseData(bool leaseRequestInProgress, IActorRef singleton)
        {
            LeaseRequestInProgress = leaseRequestInProgress;
            Singleton = singleton;
        }
    }


    [Serializable]
    internal sealed class AcquireLeaseResult : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public bool HoldingLease { get; }

        public AcquireLeaseResult(bool holdingLease)
        {
            HoldingLease = holdingLease;
        }
    }

    [Serializable]
    internal sealed class ReleaseLeaseResult : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public bool Released { get; }

        public ReleaseLeaseResult(bool released)
        {
            Released = released;
        }
    }

    [Serializable]
    internal sealed class AcquireLeaseFailure : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public Exception Failure { get; }

        public AcquireLeaseFailure(Exception failure)
        {
            Failure = failure;
        }
    }

    [Serializable]
    internal sealed class ReleaseLeaseFailure : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public Exception Failure { get; }

        public ReleaseLeaseFailure(Exception failure)
        {
            Failure = failure;
        }
    }

    [Serializable]
    internal sealed class LeaseLost : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public Exception Reason { get; }

        public LeaseLost(Exception reason)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class DelayedMemberRemoved
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public Member Member { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="member">TBD</param>
        [SerializationConstructor]
        public DelayedMemberRemoved(Member member)
        {
            Member = member;
        }
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// Used for graceful termination as part of <see cref="CoordinatedShutdown"/>.
    /// </summary>
    internal sealed class SelfExiting : ISingletonMessage
    {
        private SelfExiting() { }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static readonly SelfExiting Instance = new SelfExiting();
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public enum ClusterSingletonState
    {
        /// <summary>
        /// TBD
        /// </summary>
        Start,
        /// <summary>
        /// TBD
        /// </summary>
        AcquiringLease,
        /// <summary>
        /// TBD
        /// </summary>
        Oldest,
        /// <summary>
        /// TBD
        /// </summary>
        Younger,
        /// <summary>
        /// TBD
        /// </summary>
        BecomingOldest,
        /// <summary>
        /// TBD
        /// </summary>
        WasOldest,
        /// <summary>
        /// TBD
        /// </summary>
        HandingOver,
        /// <summary>
        /// TBD
        /// </summary>
        TakeOver,
        /// <summary>
        /// TBD
        /// </summary>
        Stopping,
        /// <summary>
        /// TBD
        /// </summary>
        End
    }

    /// <summary>
    /// Thrown when a consistent state can't be determined within the defined retry limits.
    /// Eventually it will reach a stable state and can continue, and that is simplified
    /// by starting over with a clean state. Parent supervisor should typically restart the actor, i.e. default decision.
    /// </summary>
    public sealed class ClusterSingletonManagerIsStuckException : AkkaException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterSingletonManagerIsStuckException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ClusterSingletonManagerIsStuckException(string message) : base(message) { }

#if SERIALIZATION
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterSingletonManagerIsStuckException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        public ClusterSingletonManagerIsStuckException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    /// <para>
    /// Manages singleton actor instance among all cluster nodes or a group of nodes tagged with a specific role.
    /// At most one singleton instance is running at any point in time.
    /// </para>
    /// <para>
    /// The ClusterSingletonManager is supposed to be started on all nodes, or all nodes with specified role,
    /// in the cluster with <see cref="ActorSystem.ActorOf"/>. The actual singleton is started on the oldest node
    /// by creating a child actor from the supplied `singletonProps`.
    /// </para>
    /// <para>
    /// The singleton actor is always running on the oldest member with specified role. The oldest member is determined
    /// by <see cref="Member.IsOlderThan"/>. This can change when removing members. A graceful hand over can normally
    /// be performed when current oldest node is leaving the cluster. Be aware that there is a short time period when
    /// there is no active singleton during the hand-over process.
    /// </para>
    /// <para>
    /// The cluster failure detector will notice when oldest node becomes unreachable due to things like CLR crash,
    /// hard shut down, or network failure. When the crashed node has been removed (via down) from the cluster then
    /// a new oldest node will take over and a new singleton actor is created.For these failure scenarios there
    /// will not be a graceful hand-over, but more than one active singletons is prevented by all reasonable means.
    /// Some corner cases are eventually resolved by configurable timeouts.
    /// </para>
    /// <para>
    /// You access the singleton actor with <see cref="ClusterSingletonProxy"/>. Alternatively the singleton actor may
    /// broadcast its existence when it is started.
    /// </para>
    /// <para>
    /// Use one of the factory methods <see cref="ClusterSingletonManager.Props(Actor.Props, ClusterSingletonManagerSettings)">ClusterSingletonManager.Props</see> to create the <see cref="Actor.Props"/> for the actor.
    /// </para>
    /// </summary>
    public sealed class ClusterSingletonManager : FSM<ClusterSingletonState, IClusterSingletonData>
    {
        /// <summary>
        /// Returns default HOCON configuration for the cluster singleton.
        /// </summary>
        /// <returns>TBD</returns>
        public static Config DefaultConfig()
        {
            return ConfigurationFactory.FromResource<ClusterSingletonManager>("Akka.Cluster.Tools.Singleton.reference.conf");
        }

        /// <summary>
        /// Creates props for the current cluster singleton manager using <see cref="PoisonPill"/>
        /// as the default termination message.
        /// </summary>
        /// <param name="singletonProps"><see cref="Actor.Props"/> of the singleton actor instance.</param>
        /// <param name="settings">Cluster singleton manager settings.</param>
        /// <returns>TBD</returns>
        public static Props Props(Props singletonProps, ClusterSingletonManagerSettings settings)
        {
            return Props(singletonProps, PoisonPill.Instance, settings);
        }

        /// <summary>
        /// Creates props for the current cluster singleton manager.
        /// </summary>
        /// <param name="singletonProps"><see cref="Actor.Props"/> of the singleton actor instance.</param>
        /// <param name="terminationMessage">
        /// When handing over to a new oldest node this <paramref name="terminationMessage"/> is sent to the singleton actor
        /// to tell it to finish its work, close resources, and stop. The hand-over to the new oldest node
        /// is completed when the singleton actor is terminated. Note that <see cref="PoisonPill"/> is a
        /// perfectly fine <paramref name="terminationMessage"/> if you only need to stop the actor.
        /// </param>
        /// <param name="settings">Cluster singleton manager settings.</param>
        /// <returns>TBD</returns>
        public static Props Props(Props singletonProps, object terminationMessage, ClusterSingletonManagerSettings settings)
        {
            return Actor.Props.Create(() => new ClusterSingletonManager(singletonProps, terminationMessage, settings)).WithDeploy(Deploy.Local);
        }

        private readonly Props _singletonProps;
        private readonly object _terminationMessage;
        private readonly ClusterSingletonManagerSettings _settings;

        private const string HandOverRetryTimer = "hand-over-retry";
        private const string TakeOverRetryTimer = "take-over-retry";
        private const string CleanupTimer = "cleanup";
        private const string LeaseRetryTimer = "lease-retry";

        // Previous GetNext request delivered event and new GetNext is to be sent
        private bool _oldestChangedReceived = true;
        private bool _selfExited;

        // started when when self member is Up
        private IActorRef _oldestChangedBuffer;
        // keep track of previously removed members
        private ImmutableDictionary<UniqueAddress, Deadline> _removed = ImmutableDictionary<UniqueAddress, Deadline>.Empty;
        private readonly TimeSpan _removalMargin;
        private readonly int _maxHandOverRetries;
        private readonly int _maxTakeOverRetries;
        private readonly Cluster _cluster = Cluster.Get(Context.System);
        private readonly UniqueAddress _selfUniqueAddress;
        private ILoggingAdapter _log;

        private readonly CoordinatedShutdown _coordShutdown = CoordinatedShutdown.Get(Context.System);
        private readonly TaskCompletionSource<Done> _memberExitingProgress = new TaskCompletionSource<Done>();

        private readonly string singletonLeaseName;
        private readonly Lease lease;
        private readonly TimeSpan leaseRetryInterval = TimeSpan.FromSeconds(5); // won't be used

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="singletonProps">TBD</param>
        /// <param name="terminationMessage">TBD</param>
        /// <param name="settings">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <exception cref="ConfigurationException">TBD</exception>
        /// <returns>TBD</returns>
        public ClusterSingletonManager(Props singletonProps, object terminationMessage, ClusterSingletonManagerSettings settings)
        {
            var role = settings.Role;
            if (!string.IsNullOrEmpty(role) && !_cluster.SelfRoles.Contains(role))
            {
                ThrowHelper.ThrowArgumentException_ThisCusterMemberDoesNotHaveTheRole(_cluster, role);
            }

            _singletonProps = singletonProps;
            _terminationMessage = terminationMessage;
            _settings = settings;
            singletonLeaseName = $"{Context.System.Name}-singleton-{Self.Path}";

            if (settings.LeaseSettings != null)
            {
                lease = LeaseProvider.Get(Context.System)
                    .GetLease(singletonLeaseName, settings.LeaseSettings.LeaseImplementation, _cluster.SelfAddress.HostPort());
                leaseRetryInterval = settings.LeaseSettings.LeaseRetryInterval;
            }

            _removalMargin = (settings.RemovalMargin <= TimeSpan.Zero) ? _cluster.DowningProvider.DownRemovalMargin : settings.RemovalMargin;

            var n = (int)(_removalMargin.TotalMilliseconds / _settings.HandOverRetryInterval.TotalMilliseconds);

            var minRetries = Context.System.Settings.Config.GetInt("akka.cluster.singleton.min-number-of-hand-over-retries", 0);
            if (minRetries < 1) ThrowHelper.ThrowConfigurationException_MinNumberOfHandOverRetriesMustBe_1();

            _maxHandOverRetries = Math.Max(minRetries, n + 3);
            _maxTakeOverRetries = Math.Max(1, _maxHandOverRetries - 3);

            _selfUniqueAddress = _cluster.SelfUniqueAddress;
            SetupCoordinatedShutdown();

            InitializeFSM();
        }

        private void SetupCoordinatedShutdown()
        {
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterExiting, "wait-singleton-exiting", InvokeWaitSingletonExitingFunc, this);
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterExiting, "singleton-exiting-2", InvokeSingletonExitingFunc, this, Self);
        }

        private static readonly Func<ClusterSingletonManager, Task<Done>> InvokeWaitSingletonExitingFunc = InvokeWaitSingletonExiting;
        private static Task<Done> InvokeWaitSingletonExiting(ClusterSingletonManager owner)
        {
            var cluster = owner._cluster;
            if (cluster.IsTerminated || cluster.SelfMember.Status == MemberStatus.Down)
                return Task.FromResult(Done.Instance);
            else
                return owner._memberExitingProgress.Task;
        }

        private static readonly Func<ClusterSingletonManager, IActorRef, Task<Done>> InvokeSingletonExitingFunc = InvokeSingletonExiting;
        private static Task<Done> InvokeSingletonExiting(ClusterSingletonManager owner, IActorRef self)
        {
            var cluster = owner._cluster;
            if (cluster.IsTerminated || cluster.SelfMember.Status == MemberStatus.Down)
            {
                return Task.FromResult(Done.Instance);
            }
            else
            {
                var timeout = owner._coordShutdown.Timeout(CoordinatedShutdown.PhaseClusterExiting);
                return self.Ask(SelfExiting.Instance, timeout).ContinueWith(tr => Done.Instance);
            }
        }

        private ILoggingAdapter Log { get { return _log ?? (_log = Context.GetLogger()); } }

        /// <inheritdoc cref="ActorBase.PreStart"/>
        protected override void PreStart()
        {
            if (_cluster.IsTerminated)
            {
                ThrowHelper.ActorInitializationException_Cluster_node_must_not_be_terminated();
            }

            // subscribe to cluster changes, re-subscribe when restart
            _cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, typeof(ClusterEvent.MemberRemoved), typeof(ClusterEvent.MemberDowned));

            SetTimer(CleanupTimer, Cleanup.Instance, TimeSpan.FromMinutes(1.0), repeat: true);

            // defer subscription to avoid some jitter when
            // starting/joining several nodes at the same time
            var self = Self;
            _cluster.RegisterOnMemberUp(() => self.Tell(StartOldestChangedBuffer.Instance));
        }

        /// <inheritdoc cref="ActorBase.PostStop"/>
        protected override void PostStop()
        {
            CancelTimer(CleanupTimer);
            _cluster.Unsubscribe(Self);
            _memberExitingProgress.TrySetResult(Done.Instance);
            base.PostStop();
        }

        // HACK: this is to patch issue #4474 (https://github.com/akkadotnet/akka.net/issues/4474), but it doesn't guarantee that it fixes the underlying bug.
        // There is no spec for this fix, no reproduction spec was possible.
        private void AddRemoved(UniqueAddress node)
        {
            if (_removed.TryGetValue(node, out _))
            {
                _removed = _removed.SetItem(node, Deadline.Now + TimeSpan.FromMinutes(15.0));
            }
            else
            {
                _removed = _removed.Add(node, Deadline.Now + TimeSpan.FromMinutes(15.0));
            }
        }

        private void CleanupOverdueNotMemberAnyMore()
        {
            _removed = _removed.Where(kv => kv.Value.IsOverdue).ToImmutableDictionary(UniqueAddressComparer.Instance);
        }

        private ActorSelection Peer(Address at)
        {
            return Context.ActorSelection(Self.Path.ToStringWithAddress(at));
        }

        private void GetNextOldestChanged()
        {
            if (!_oldestChangedReceived) { return; }
            _oldestChangedReceived = false;
            _oldestChangedBuffer.Tell(OldestChangedBuffer.GetNext.Instance);
        }

        private State<ClusterSingletonState, IClusterSingletonData> TryAcquireLease()
        {
            var self = Self;
            lease.Acquire(reason =>
            {
                self.Tell(new LeaseLost(reason));
            }).ContinueWith(r =>
            {
                if (r.IsFaulted || r.IsCanceled)
                    return (object)new AcquireLeaseFailure(r.Exception);
                return new AcquireLeaseResult(r.Result);
            }).PipeTo(Self);

            return GoTo(ClusterSingletonState.AcquiringLease).Using(new AcquiringLeaseData(true, null));
        }

        // Try and go to oldest, taking the lease if needed
        private State<ClusterSingletonState, IClusterSingletonData> TryGotoOldest()
        {
            // check if lease
            if (lease == null)
                return GoToOldest();
            else
            {
                if (Log.IsInfoEnabled) { Log.Trying_to_acquire_lease_before_starting_singleton(); }
                return TryAcquireLease();
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> GoToOldest()
        {
            var singleton = Context.Watch(Context.ActorOf(_singletonProps, _settings.SingletonName));
            if (Log.IsInfoEnabled) Log.SingletonManagerStartedSingletonActor(singleton);
            return
                GoTo(ClusterSingletonState.Oldest).Using(new OldestData(singleton));
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleOldestChanged(IActorRef singleton, UniqueAddress oldest)
        {
            _oldestChangedReceived = true;
            if (Log.IsInfoEnabled) Log.Info("{0} observed OldestChanged: [{1} -> {2}]", StateName, _cluster.SelfAddress, oldest?.Address);
            switch (oldest)
            {
                case UniqueAddress a when a.Equals(_cluster.SelfUniqueAddress):
                    // already oldest
                    return Stay();
                case UniqueAddress a when !_selfExited && _removed.ContainsKey(a):
                    // The member removal was not completed and the old removed node is considered
                    // oldest again. Safest is to terminate the singleton instance and goto Younger.
                    // This node will become oldest again when the other is removed again.
                    return GoToHandingOver(singleton, null);
                case UniqueAddress a:
                    // send TakeOver request in case the new oldest doesn't know previous oldest
                    Peer(a.Address).Tell(TakeOverFromMe.Instance);
                    SetTimer(TakeOverRetryTimer, new TakeOverRetry(1), _settings.HandOverRetryInterval, repeat: false);
                    return GoTo(ClusterSingletonState.WasOldest)
                        .Using(new WasOldestData(singleton, a));
                case null:
                    // new oldest will initiate the hand-over
                    SetTimer(TakeOverRetryTimer, new TakeOverRetry(1), _settings.HandOverRetryInterval, repeat: false);
                    return GoTo(ClusterSingletonState.WasOldest)
                        .Using(new WasOldestData(singleton, newOldest: null));
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleHandOverDone(IActorRef handOverTo)
        {
            var newOldest = handOverTo?.Path.Address;
            if (Log.IsInfoEnabled) Log.SingletonTerminatedHandoverDone(_cluster, newOldest);
            handOverTo?.Tell(HandOverDone.Instance);
            _memberExitingProgress.TrySetResult(Done.Instance);
            if (_removed.ContainsKey(_cluster.SelfUniqueAddress))
            {
                if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                return Stop();
            }
            else if (handOverTo == null)
            {
                return GoTo(ClusterSingletonState.Younger).Using(new YoungerData(null));
            }
            else
            {
                return GoTo(ClusterSingletonState.End).Using(EndData.Instance);
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> GoToHandingOver(IActorRef singleton, IActorRef handOverTo)
        {
            if (singleton == null)
            {
                return HandleHandOverDone(handOverTo);
            }

            handOverTo?.Tell(HandOverInProgress.Instance);
            if (Log.IsInfoEnabled) { Log.SingletonManagerStoppingSingletonActor(singleton); }
            singleton.Tell(_terminationMessage);
            return GoTo(ClusterSingletonState.HandingOver).Using(new HandingOverData(singleton, handOverTo));
        }

        private State<ClusterSingletonState, IClusterSingletonData> GoToStopping(IActorRef singleton)
        {
            if (Log.IsInfoEnabled) { Log.SingletonManagerStoppingSingletonActor(singleton); }
            singleton.Tell(_terminationMessage);
            return GoTo(ClusterSingletonState.Stopping).Using(new StoppingData(singleton));
        }

        private void InitializeFSM()
        {
            When(ClusterSingletonState.Start, HandleWhenStart);

            When(ClusterSingletonState.Younger, HandleWhenYounger);

            When(ClusterSingletonState.BecomingOldest, HandleWhenBecomingOldest);

            When(ClusterSingletonState.AcquiringLease, HandleWhenAcquiringLease);

            When(ClusterSingletonState.Oldest, HandleWhenOldest);

            When(ClusterSingletonState.WasOldest, HandleWhenWasOldest);

            When(ClusterSingletonState.HandingOver, HandleWhenHandingOver);

            When(ClusterSingletonState.Stopping, HandleWhenStopping);

            When(ClusterSingletonState.End, HandleWhenEnd);

            WhenUnhandled(HandleWhenUnhandled);

            OnTransition(new TransitionHandler(HandleTransition));

            StartWith(ClusterSingletonState.Start, Uninitialized.Instance);
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenStart(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case StartOldestChangedBuffer _:
                    _oldestChangedBuffer = Context.ActorOf(Actor.Props.Create<OldestChangedBuffer>(_settings.Role).WithDispatcher(Context.Props.Dispatcher));
                    GetNextOldestChanged();
                    return Stay();

                case OldestChangedBuffer.InitialOldestState initialOldestState:
                    _oldestChangedReceived = true;
                    if (initialOldestState.Oldest.Head() == _selfUniqueAddress && initialOldestState.SafeToBeOldest)
                    {
                        // oldest immediately
                        return TryGotoOldest();
                    }
                    return initialOldestState.Oldest.Head() == _selfUniqueAddress
                        ? GoTo(ClusterSingletonState.BecomingOldest).Using(new BecomingOldestData(initialOldestState.Oldest.FindAll(u => !u.Equals(_selfUniqueAddress))))
                        : GoTo(ClusterSingletonState.Younger).Using(new YoungerData(initialOldestState.Oldest.FindAll(u => !u.Equals(_selfUniqueAddress))));

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenYounger(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case OldestChangedBuffer.OldestChanged oldestChanged when e.StateData is YoungerData youngerData:
                    _oldestChangedReceived = true;
                    if (oldestChanged.Oldest.Equals(_selfUniqueAddress))
                    {
                        if (Log.IsInfoEnabled) Log.YoungerObservedOldestChanged(youngerData);

                        if (youngerData.Oldest.All(m => _removed.ContainsKey(m)))
                        {
                            return TryGotoOldest();
                        }
                        else
                        {
                            Peer(youngerData.Oldest.Head().Address).Tell(HandOverToMe.Instance);
                            return GoTo(ClusterSingletonState.BecomingOldest).Using(new BecomingOldestData(youngerData.Oldest));
                        }
                    }
                    else
                    {
                        if (Log.IsInfoEnabled) Log.YoungerObservedOldestChanged(youngerData, oldestChanged);
                        GetNextOldestChanged();
                        if (oldestChanged.Oldest != null && !youngerData.Oldest.Contains(oldestChanged.Oldest))
                        {
                            youngerData.Oldest.Insert(0, oldestChanged.Oldest);
                        }
                        return Stay().Using(new YoungerData(youngerData.Oldest));
                    }

                case MemberDowned memberDowned when memberDowned.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress):
                    if (Log.IsInfoEnabled) { Log.SelfDownedStoppingClusterSingletonManager(); }
                    return Stop();

                case MemberRemoved memberRemoved:
                    if (memberRemoved.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress))
                    {
                        if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                        return Stop();
                    }
                    ScheduleDelayedMemberRemoved(memberRemoved.Member);
                    return Stay();

                case DelayedMemberRemoved removed when e.StateData is YoungerData data:
                    if (!_selfExited && Log.IsInfoEnabled) Log.MemberRemoved(removed);
                    AddRemoved(removed.Member.UniqueAddress);
                    // transition when OldestChanged
                    return Stay().Using(new YoungerData(data.Oldest.FindAll(u => !u.Equals(removed.Member.UniqueAddress))));

                case HandOverToMe _:
                    var selfStatus = _cluster.SelfMember.Status;
                    if (selfStatus == MemberStatus.Leaving || selfStatus == MemberStatus.Exiting)
                    {
                        if (Log.IsInfoEnabled) { Log.IgnoringHandOverToMeinYoungerfrom(Sender, selfStatus); }
                    }
                    else
                    {
                        // this node was probably quickly restarted with same hostname:port,
                        // confirm that the old singleton instance has been stopped
                        Sender.Tell(HandOverDone.Instance);
                    }
                    return Stay();

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenBecomingOldest(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case HandOverInProgress _:
                    // confirmation that the hand-over process has started
                    if (Log.IsInfoEnabled) Log.HandoverInProgressAt(Sender);
                    CancelTimer(HandOverRetryTimer);
                    return Stay();

                case HandOverDone _ when e.StateData is BecomingOldestData b:
                    {
                        var oldest = b.PreviousOldest.Head();
                        if (oldest != null)
                        {
                            if (Sender.Path.Address.Equals(oldest.Address))
                            {
                                return TryGotoOldest();
                            }

                            if (Log.IsInfoEnabled) { Log.IgnoringHandOverDoneInBecomingOldest(Sender, oldest); }
                            return Stay();
                        }

                        if (Log.IsInfoEnabled) { Log.IgnoringHandOverToMeinYoungerfrom(Sender); }
                        return Stay();
                    }

                case MemberDowned memberDowned when memberDowned.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress):
                    if (Log.IsInfoEnabled) { Log.SelfDownedStoppingClusterSingletonManager(); }
                    return Stop();

                case MemberRemoved memberRemoved:
                    if (memberRemoved.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress))
                    {
                        if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                        return Stop();
                    }
                    else
                    {
                        ScheduleDelayedMemberRemoved(memberRemoved.Member);
                        return Stay();
                    }

                case DelayedMemberRemoved delayed when e.StateData is BecomingOldestData becoming:
                    if (!_selfExited && Log.IsInfoEnabled)
                    {
                        Log.PreviousOldestRemoved(delayed, becoming);
                    }
                    AddRemoved(delayed.Member.UniqueAddress);
                    if (_cluster.IsTerminated)
                    {
                        // Don't act on DelayedMemberRemoved (starting singleton) if this node is shutting its self down,
                        // just wait for self MemberRemoved
                        return Stay();
                    }
                    else if (becoming.PreviousOldest.Contains(delayed.Member.UniqueAddress) && becoming.PreviousOldest.All(a => _removed.ContainsKey(a)))
                    {
                        return TryGotoOldest();
                    }
                    else
                    {
                        return Stay().Using(new BecomingOldestData(becoming.PreviousOldest.FindAll(u => !u.Equals(delayed.Member.UniqueAddress))));
                    }

                case TakeOverFromMe _ when e.StateData is BecomingOldestData becomingOldestData:
                    var senderAddress = Sender.Path.Address;
                    // it would have been better to include the UniqueAddress in the TakeOverFromMe message,
                    // but can't change due to backwards compatibility
                    var senderUniqueAddress = _cluster.State.Members
                        .Where(m => m.Address.Equals(senderAddress))
                        .Select(m => m.UniqueAddress)
                        .FirstOrDefault();

                    switch (senderUniqueAddress)
                    {
                        case null:
                            // from unknown node, ignore
                            if (Log.IsInfoEnabled) Log.IgnoringTakeOverRequestFromUnknownNode(senderAddress);
                            return Stay();
                        case UniqueAddress _:
                            switch (becomingOldestData.PreviousOldest.Head())
                            {
                                case UniqueAddress oldest:
                                    if (oldest.Equals(senderUniqueAddress))
                                    {
                                        Sender.Tell(HandOverToMe.Instance);
                                    }
                                    else
                                    {
                                        if (Log.IsInfoEnabled) { Log.IgnoringTakeOverRequestInBecomingOldest(Sender, oldest); }
                                    }
                                    return Stay();
                                case null:
                                    Sender.Tell(HandOverToMe.Instance);
                                    becomingOldestData.PreviousOldest.Insert(0, senderUniqueAddress);
                                    return Stay().Using(new BecomingOldestData(becomingOldestData.PreviousOldest));
                            }
                    }

                case HandOverRetry handOverRetry when e.StateData is BecomingOldestData becomingOldest:
                    if (handOverRetry.Count <= _maxHandOverRetries)
                    {
                        var oldest = becomingOldest.PreviousOldest.Head();
                        if (Log.IsInfoEnabled) Log.RetrySendingHandOverToMeTo(handOverRetry.Count, oldest);
                        if (oldest != null) Peer(oldest.Address).Tell(HandOverToMe.Instance);
                        SetTimer(HandOverRetryTimer, new HandOverRetry(handOverRetry.Count + 1), _settings.HandOverRetryInterval);
                        return Stay();
                    }
                    else if (becomingOldest.PreviousOldest != null && becomingOldest.PreviousOldest.All(m => _removed.ContainsKey(m)))
                    {
                        // can't send HandOverToMe, previousOldest unknown for new node (or restart)
                        // previous oldest might be down or removed, so no TakeOverFromMe message is received
                        if (Log.IsInfoEnabled) Log.TimeoutInBecomingOldest();
                        return TryGotoOldest();
                    }
                    else if (_cluster.IsTerminated)
                    {
                        return Stop();
                    }
                    else
                    {
                        ThrowHelper.ThrowClusterSingletonManagerIsStuckException_BecomingSingletonOldest(becomingOldest);
                        return null;
                    }

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenAcquiringLease(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case AcquireLeaseResult alr:
                    if (Log.IsInfoEnabled) { Log.Acquire_lease_result(alr); }
                    if (alr.HoldingLease)
                    {
                        return GoToOldest();
                    }
                    else
                    {
                        SetTimer(LeaseRetryTimer, LeaseRetry.Instance, leaseRetryInterval, repeat: false);
                        return Stay().Using(new AcquiringLeaseData(false, null));
                    }

                case Terminated t when e.StateData is AcquiringLeaseData ald && t.ActorRef.Equals(ald.Singleton):
                    if (Log.IsInfoEnabled)
                    {
                        Log.Singleton_actor_terminated_Trying_to_acquire_lease_again_before_re_creating();
                    }
                    // tryAcquireLease sets the state to None for singleton actor
                    return TryAcquireLease();

                case AcquireLeaseFailure alf:
                    Log.Failed_to_get_lease_will_be_retried(alf);
                    SetTimer(LeaseRetryTimer, LeaseRetry.Instance, leaseRetryInterval, repeat: false);
                    return Stay().Using(new AcquiringLeaseData(false, null));

                case LeaseRetry _:
                    // If lease was lost (so previous state was oldest) then we don't try and get the lease
                    // until the old singleton instance has been terminated so we know there isn't an
                    // instance in this case
                    return TryAcquireLease();

                case OldestChangedBuffer.OldestChanged oldestChanged when e.StateData is AcquiringLeaseData ald2:
                    return HandleOldestChanged(ald2.Singleton, oldestChanged.Oldest);

                case HandOverToMe _ when e.StateData is AcquiringLeaseData ald3:
                    return GoToHandingOver(ald3.Singleton, Sender);

                case TakeOverFromMe _:
                    // already oldest, so confirm and continue like that
                    Sender.Tell(HandOverToMe.Instance);
                    return Stay();

                case SelfExiting _:
                    SelfMemberExited();
                    // complete memberExitingProgress when handOverDone
                    Sender.Tell(Done.Instance); // reply to ask
                    return Stay();

                case MemberDowned md when md.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress):
                    if (Log.IsInfoEnabled) { Log.SelfDownedStoppingClusterSingletonManager(); }
                    return Stop();

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenOldest(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case OldestChangedBuffer.OldestChanged oldestChanged when e.StateData is OldestData oldestData:
                    return HandleOldestChanged(oldestData.Singleton, oldestChanged.Oldest);

                case HandOverToMe _ when e.StateData is OldestData oldest:
                    return GoToHandingOver(oldest.Singleton, Sender);

                case TakeOverFromMe _:
                    // already oldest, so confirm and continue like that
                    Sender.Tell(HandOverToMe.Instance);
                    return Stay();

                case Terminated terminated when e.StateData is OldestData o && terminated.ActorRef.Equals(o.Singleton):
                    if (Log.IsInfoEnabled) { Log.SingletonActorWasTerminated(o.Singleton); }
                    return Stay().Using(new OldestData(null));

                case SelfExiting _:
                    SelfMemberExited();
                    // complete _memberExitingProgress when HandOverDone
                    Sender.Tell(Done.Instance); // reply to ask
                    return Stay();

                case MemberDowned memberDowned when e.StateData is OldestData od && memberDowned.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress):
                    if (od.Singleton == null)
                    {
                        if (Log.IsInfoEnabled) { Log.SelfDownedStoppingClusterSingletonManager(); }
                        return Stop();
                    }
                    else
                    {
                        if (Log.IsInfoEnabled) { Log.SelfDownedStopping(); }
                        return GoToStopping(od.Singleton);
                    }

                case LeaseLost ll when e.StateData is OldestData od2:
                    Log.Lease_has_been_lost_Terminating_singleton_and_trying_to_re_acquire_lease(ll);
                    if (od2.Singleton != null)
                    {
                        od2.Singleton.Tell(_terminationMessage);
                        return GoTo(ClusterSingletonState.AcquiringLease).Using(new AcquiringLeaseData(false, od2.Singleton));
                    }
                    else
                    {
                        return TryAcquireLease();
                    }

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenWasOldest(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case TakeOverRetry takeOverRetry when e.StateData is WasOldestData wasOldestData:
                    if ((_cluster.IsTerminated || _selfExited)
                        && (wasOldestData.NewOldest == null || takeOverRetry.Count > _maxTakeOverRetries))
                    {
                        return wasOldestData.Singleton != null ? GoToStopping(wasOldestData.Singleton) : Stop();
                    }
                    else if (takeOverRetry.Count <= _maxTakeOverRetries)
                    {
                        if (_maxTakeOverRetries - takeOverRetry.Count <= 3)
                        {
                            if (Log.IsInfoEnabled) { Log.RetrySendingTakeOverFromMeTo(takeOverRetry.Count, wasOldestData); }
                        }
                        else
                            Log.Debug("Retry [{0}], sending TakeOverFromMe to [{1}]", takeOverRetry.Count, wasOldestData.NewOldest?.Address);

                        if (wasOldestData.NewOldest != null)
                            Peer(wasOldestData.NewOldest.Address).Tell(TakeOverFromMe.Instance);

                        SetTimer(TakeOverRetryTimer, new TakeOverRetry(takeOverRetry.Count + 1), _settings.HandOverRetryInterval, false);
                        return Stay();
                    }
                    else
                    {
                        ThrowHelper.ThrowClusterSingletonManagerIsStuckException_ExpectedHandOverTo(wasOldestData);
                        return null;
                    }

                case HandOverToMe _ when e.StateData is WasOldestData w:
                    return GoToHandingOver(w.Singleton, Sender);

                case MemberRemoved removed:
                    if (removed.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress) && !_selfExited)
                    {
                        if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                        return Stop();
                    }
                    else if (e.StateData is WasOldestData data
                            && data.NewOldest != null
                            && !_selfExited
                            && removed.Member.UniqueAddress.Equals(data.NewOldest))
                    {
                        AddRemoved(removed.Member.UniqueAddress);
                        return GoToHandingOver(data.Singleton, null);
                    }
                    return null;

                case Terminated t when e.StateData is WasOldestData oldestData && t.ActorRef.Equals(oldestData.Singleton):
                    if (Log.IsInfoEnabled) { Log.SingletonActorWasTerminated(oldestData.Singleton); }
                    return Stay().Using(new WasOldestData(null, oldestData.NewOldest));

                case SelfExiting _:
                    SelfMemberExited();
                    // complete _memberExitingProgress when HandOverDone
                    Sender.Tell(Done.Instance); // reply to ask
                    return Stay();

                case MemberDowned memberDowned when e.StateData is WasOldestData od && memberDowned.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress):
                    if (od.Singleton == null)
                    {
                        if (Log.IsInfoEnabled) { Log.SelfDownedStoppingClusterSingletonManager(); }
                        return Stop();
                    }
                    else
                    {
                        if (Log.IsInfoEnabled) { Log.SelfDownedStopping(); }
                        return GoToStopping(od.Singleton);
                    }

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenHandingOver(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case Terminated terminated when e.StateData is HandingOverData handingOverData && terminated.ActorRef.Equals(handingOverData.Singleton):
                    return HandleHandOverDone(handingOverData.HandOverTo);

                case HandOverToMe _ when e.StateData is HandingOverData d && d.HandOverTo.Equals(Sender):
                    // retry
                    Sender.Tell(HandOverInProgress.Instance);
                    return Stay();

                case SelfExiting _:
                    SelfMemberExited();
                    // complete _memberExitingProgress when HandOverDone
                    Sender.Tell(Done.Instance);
                    return Stay();

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenStopping(Event<IClusterSingletonData> e)
        {
            if (e.FsmEvent is Terminated terminated
                && e.StateData is StoppingData stoppingData
                && terminated.ActorRef.Equals(stoppingData.Singleton))
            {
                if (Log.IsInfoEnabled) { Log.SingletonActorWasTerminated(stoppingData.Singleton); }
                return Stop();
            }

            return null;
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenEnd(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case MemberRemoved removed when removed.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress):
                    if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                    return Stop();

                case OldestChangedBuffer.OldestChanged _:
                case HandOverToMe _:
                    // not interested anymore - waiting for removal
                    return Stay();

                default:
                    return null;
            }
        }

        private State<ClusterSingletonState, IClusterSingletonData> HandleWhenUnhandled(Event<IClusterSingletonData> e)
        {
            switch (e.FsmEvent)
            {
                case SelfExiting _:
                    SelfMemberExited();
                    // complete _memberExitingProgress when HandOverDone
                    _memberExitingProgress.TrySetResult(Done.Instance);
                    Sender.Tell(Done.Instance); // reply to ask
                    return Stay();

                case MemberRemoved removed:
                    if (removed.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress) && !_selfExited)
                    {
                        if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                        return Stop();
                    }
                    else
                    {
                        if (!_selfExited && Log.IsInfoEnabled) { Log.MemberRemoved(removed); }

                        AddRemoved(removed.Member.UniqueAddress);
                        return Stay();
                    }

                case DelayedMemberRemoved delayedMemberRemoved:
                    if (!_selfExited && Log.IsInfoEnabled) { Log.MemberRemoved(delayedMemberRemoved); }

                    AddRemoved(delayedMemberRemoved.Member.UniqueAddress);
                    return Stay();

                case TakeOverFromMe _:
                    if (Log.IsDebugEnabled) { Log.IgnoringTakeOverRequest(StateName, Sender); }
                    return Stay();

                case Cleanup _:
                    CleanupOverdueNotMemberAnyMore();
                    return Stay();

                case MemberDowned memberDowned:
                    if (memberDowned.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress))
                    {
                        if (Log.IsInfoEnabled) { Log.SelfDownedWaitingForRemoval(); }
                    }
                    return Stay();

                case ReleaseLeaseFailure rlf:
                    Log.Failed_to_release_lease_Singleton(rlf);
                    return Stay();

                case ReleaseLeaseResult rlr:
                    if (rlr.Released)
                    {
                        if (Log.IsInfoEnabled) { Log.Lease_released(); }
                    }
                    else
                    {
                        // TODO we could retry
                        Log.Failed_to_release_lease_Singleton(null);
                    }
                    return Stay();

                default:
                    return null;
            }
        }

        private void HandleTransition(ClusterSingletonState from, ClusterSingletonState to)
        {
            if (Log.IsInfoEnabled) Log.ClusterSingletonManagerStateChange(from, to, StateData);

            if (to == ClusterSingletonState.BecomingOldest) SetTimer(HandOverRetryTimer, new HandOverRetry(1), _settings.HandOverRetryInterval);
            if (from == ClusterSingletonState.BecomingOldest) CancelTimer(HandOverRetryTimer);
            if (from == ClusterSingletonState.WasOldest) CancelTimer(TakeOverRetryTimer);

            if (from == ClusterSingletonState.AcquiringLease && to != ClusterSingletonState.Oldest)
            {
                if (StateData is AcquiringLeaseData ald && ald.LeaseRequestInProgress)
                {
                    if (Log.IsInfoEnabled) { Log.Releasing_lease_as_leaving_AcquiringLease_going_to(to); }
                    if (lease != null)
                    {
                        lease.Release().ContinueWith(r =>
                        {
                            if (r.IsCanceled || r.IsFaulted)
                                return (object)new ReleaseLeaseFailure(r.Exception);
                            return new ReleaseLeaseResult(r.Result);
                        }).PipeTo(Self);
                    }
                }
            }

            if (from == ClusterSingletonState.Oldest && lease != null)
            {
                if (Log.IsInfoEnabled) { Log.Releasing_lease_as_leaving_Oldest(); }
                lease.Release().ContinueWith(r => new ReleaseLeaseResult(r.Result)).PipeTo(Self);
            }

            if (to == ClusterSingletonState.Younger || to == ClusterSingletonState.Oldest) GetNextOldestChanged();
            if (to == ClusterSingletonState.Younger || to == ClusterSingletonState.End)
            {
                if (_removed.ContainsKey(_cluster.SelfUniqueAddress))
                {
                    if (Log.IsInfoEnabled) Log.SelfRemovedStoppingClusterSingletonManager();
                    Context.Stop(Self);
                }
            }
        }

        private void SelfMemberExited()
        {
            _selfExited = true;
            if (Log.IsInfoEnabled) Log.ExitedCluster(_cluster);
        }

        private void ScheduleDelayedMemberRemoved(Member member)
        {
            if (_removalMargin > TimeSpan.Zero)
            {
                if (_log.IsDebugEnabled) Log.ScheduleDelayedMemberRemovedFor(member);
                Context.System.Scheduler.ScheduleTellOnce(_removalMargin, Self, new DelayedMemberRemoved(member), Self);
            }
            else Self.Tell(new DelayedMemberRemoved(member));
        }
    }
}
