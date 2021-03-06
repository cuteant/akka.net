﻿//-----------------------------------------------------------------------
// <copyright file="ClusterDaemon.cs" company="Akka.NET Project">
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
using Akka.Dispatch;
using Akka.Event;
using Akka.Remote;
using Akka.Util;
using Akka.Util.Internal;
using Akka.Util.Internal.Collections;
using MessagePack;

namespace Akka.Cluster
{
    /// <summary>
    /// Base interface for all cluster messages. All ClusterMessage's are serializable.
    /// </summary>
    public interface IClusterMessage
    {
    }

    /// <summary>
    /// Cluster commands sent by the USER via <see cref="Cluster"/> extension.
    /// </summary>
    internal class ClusterUserAction
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Union(0, typeof(JoinTo))]
        [Union(1, typeof(Leave))]
        [Union(2, typeof(Down))]
        [MessagePackObject]
        internal abstract class BaseClusterUserAction
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            protected BaseClusterUserAction(Address address)
            {
                Address = address;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public Address Address { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="other">TBD</param>
            /// <returns>TBD</returns>
            protected bool Equals(BaseClusterUserAction other)
            {
                return Equals(Address, other.Address);
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((BaseClusterUserAction)obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return (Address is object ? Address.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Command to initiate join another node (represented by `address`).
        /// Join will be sent to the other node.
        /// </summary>
        [MessagePackObject]
        internal sealed class JoinTo : BaseClusterUserAction
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            public JoinTo(Address address)
                : base(address)
            {
            }
        }

        /// <summary>
        /// Command to leave the cluster.
        /// </summary>
        [MessagePackObject]
        internal sealed class Leave : BaseClusterUserAction, IClusterMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            public Leave(Address address)
                : base(address)
            { }
        }

        /// <summary>
        /// Command to mark node as temporary down.
        /// </summary>
        [MessagePackObject]
        internal sealed class Down : BaseClusterUserAction, IClusterMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            public Down(Address address)
                : base(address)
            {
            }
        }
    }

    /// <summary>
    /// Command to join the cluster. Sent when a node wants to join another node (the receiver).
    /// </summary>
    internal sealed class InternalClusterAction
    {
        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        internal sealed class Join : IClusterMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="node">the node that wants to join the cluster</param>
            /// <param name="roles">TBD</param>
            [SerializationConstructor]
            public Join(UniqueAddress node, ImmutableHashSet<string> roles)
            {
                Node = node;
                Roles = roles;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public UniqueAddress Node { get; }
            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public ImmutableHashSet<string> Roles { get; }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Join join && Equals(join);
            }

            private bool Equals(Join other)
            {
                return Node.Equals(other.Node) && !Roles.Except(other.Roles).Any();
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (Node.GetHashCode() * 397) ^ Roles.GetHashCode();
                }
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                var roles = string.Join(",", Roles ?? ImmutableHashSet<string>.Empty);
                return $"{GetType()}: {Node} wants to join on Roles [{roles}]";
            }
        }

        /// <summary>
        /// Reply to Join
        /// </summary>
        [MessagePackObject]
        internal sealed class Welcome : IClusterMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="from">the sender node in the cluster, i.e. the node that received the Join command</param>
            /// <param name="gossip">TBD</param>
            public Welcome(UniqueAddress from, Gossip gossip)
            {
                From = from;
                Gossip = gossip;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public UniqueAddress From { get; }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public Gossip Gossip { get; }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Welcome welcome && Equals(welcome);
            }

            private bool Equals(Welcome other)
            {
                return From.Equals(other.From) && string.Equals(Gossip.ToString(), other.Gossip.ToString(), StringComparison.Ordinal);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (From.GetHashCode() * 397) ^ Gossip.GetHashCode();
                }
            }

        }

        /// <summary>
        /// Command to initiate the process to join the specified
        /// seed nodes.
        /// </summary>
        [MessagePackObject]
        internal sealed class JoinSeedNodes : IDeadLetterSuppression
        {
            /// <summary>
            /// Creates a new instance of the command.
            /// </summary>
            /// <param name="seedNodes">The list of seeds we wish to join.</param>
            public JoinSeedNodes(ImmutableList<Address> seedNodes)
            {
                SeedNodes = seedNodes;
            }

            /// <summary>
            /// The list of seeds we wish to join.
            /// </summary>
            [Key(0)]
            public readonly ImmutableList<Address> SeedNodes;
        }

        /// <summary>
        /// Start message of the process to join one of the seed nodes.
        /// The node sends <see cref="InitJoin"/> to all seed nodes, which replies
        /// with <see cref="InitJoinAck"/>. The first reply is used others are discarded.
        /// The node sends <see cref="Join"/> command to the seed node that replied first.
        /// If a node is uninitialized it will reply to `InitJoin` with
        /// <see cref="InitJoinNack"/>.
        /// </summary>
        internal sealed class JoinSeenNode : ISingletonMessage
        {
            private JoinSeenNode() { }
            public static readonly JoinSeenNode Instance = new JoinSeenNode();
        }

        /// <inheritdoc cref="JoinSeenNode"/>
        internal sealed class InitJoin : IClusterMessage, IDeadLetterSuppression, ISingletonMessage
        {
            private InitJoin() { }

            public static readonly InitJoin Instance = new InitJoin();

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return obj is InitJoin;
            }
        }

        /// <inheritdoc cref="JoinSeenNode"/>
        [MessagePackObject]
        internal sealed class InitJoinAck : IClusterMessage, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            /// <returns>TBD</returns>
            [SerializationConstructor]
            public InitJoinAck(Address address)
            {
                Address = address;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly Address Address;

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is InitJoinAck initJoinAck && Equals(initJoinAck);
            }

            private bool Equals(InitJoinAck other)
            {
                return Equals(Address, other.Address);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return (Address is object ? Address.GetHashCode() : 0);
            }
        }

        /// <inheritdoc cref="JoinSeenNode"/>
        [MessagePackObject]
        internal sealed class InitJoinNack : IClusterMessage, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">The address we attempted to join</param>
            [SerializationConstructor]
            public InitJoinNack(Address address)
            {
                Address = address;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly Address Address;

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is InitJoinNack initJoinNack && Equals(initJoinNack);
            }

            private bool Equals(InitJoinNack other)
            {
                return Equals(Address, other.Address);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return (Address is object ? Address.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Signals that a member is confirmed to be exiting the cluster
        /// </summary>
        [MessagePackObject]
        internal sealed class ExitingConfirmed : IClusterMessage, IDeadLetterSuppression
        {
            [SerializationConstructor]
            public ExitingConfirmed(UniqueAddress address)
            {
                Address = address;
            }

            /// <summary>
            /// The member's address
            /// </summary>
            [Key(0)]
            public readonly UniqueAddress Address;

            private bool Equals(ExitingConfirmed other)
            {
                return Address.Equals(other.Address);
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is ExitingConfirmed exitingConfirmed && Equals(exitingConfirmed);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return Address.GetHashCode();
            }
        }

        /// <summary>
        /// Used to signal that a self-exiting event has completed.
        /// </summary>
        internal sealed class ExitingCompleted : ISingletonMessage
        {
            private ExitingCompleted() { }

            /// <summary>
            /// Singleton instance
            /// </summary>
            public static readonly ExitingCompleted Instance = new ExitingCompleted();
        }

        /// <summary>
        /// Marker interface for periodic tick messages
        /// </summary>
        internal interface ITick { }

        /// <summary>
        /// Used to trigger the publication of gossip
        /// </summary>
        internal sealed class GossipTick : ITick, ISingletonMessage
        {
            private GossipTick() { }

            /// <summary>
            /// TBD
            /// </summary>
            public static readonly GossipTick Instance = new GossipTick();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class GossipSpeedupTick : ITick, ISingletonMessage
        {
            private GossipSpeedupTick() { }

            /// <summary>
            /// TBD
            /// </summary>
            public static readonly GossipSpeedupTick Instance = new GossipSpeedupTick();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class ReapUnreachableTick : ITick, ISingletonMessage
        {
            private ReapUnreachableTick() { }

            /// <summary>
            /// TBD
            /// </summary>
            public static readonly ReapUnreachableTick Instance = new ReapUnreachableTick();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class MetricsTick : ITick, ISingletonMessage
        {
            private MetricsTick() { }

            /// <summary>
            /// TBD
            /// </summary>
            public static readonly MetricsTick Instance = new MetricsTick();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class LeaderActionsTick : ITick, ISingletonMessage
        {
            private LeaderActionsTick() { }

            /// <summary>
            /// TBD
            /// </summary>
            public static readonly LeaderActionsTick Instance = new LeaderActionsTick();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class PublishStatsTick : ITick, ISingletonMessage
        {
            private PublishStatsTick() { }

            /// <summary>
            /// TBD
            /// </summary>
            public static readonly PublishStatsTick Instance = new PublishStatsTick();
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        internal sealed class SendGossipTo
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            [SerializationConstructor]
            public SendGossipTo(Address address)
            {
                Address = address;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly Address Address;

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (!(obj is SendGossipTo other)) return false;
                return Address.Equals(other.Address);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return Address.GetHashCode();
            }
        }

        /// <summary>
        /// Gets a reference to the cluster core daemon.
        /// </summary>
        internal sealed class GetClusterCoreRef : ISingletonMessage
        {
            private GetClusterCoreRef() { }

            /// <summary>
            /// The singleton instance
            /// </summary>
            public static readonly GetClusterCoreRef Instance = new GetClusterCoreRef();
        }

        /// <summary>
        /// Command to <see cref="Akka.Cluster.ClusterDaemon"/> to create a
        /// <see cref="OnMemberStatusChangedListener"/> that will be invoked
        /// when the current member is marked as up.
        /// </summary>
        [MessagePackObject]
        public sealed class AddOnMemberUpListener : INoSerializationVerificationNeeded
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="callback">TBD</param>
            [SerializationConstructor]
            public AddOnMemberUpListener(Action callback)
            {
                Callback = callback;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public Action Callback { get; }
        }

        /// <summary>
        /// Command to the <see cref="ClusterDaemon"/> to create a <see cref="OnMemberStatusChangedListener"/>
        /// that will be invoked when the current member is removed.
        /// </summary>
        [MessagePackObject]
        public sealed class AddOnMemberRemovedListener : INoSerializationVerificationNeeded
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="callback">TBD</param>
            [SerializationConstructor]
            public AddOnMemberRemovedListener(Action callback)
            {
                Callback = callback;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public Action Callback { get; }
        }

        /// <summary>
        /// All messages related to creating or removing <see cref="Cluster"/> event subscriptions
        /// </summary>
        //[Union(0, typeof(Subscribe))]
        //[Union(1, typeof(Unsubscribe))]
        //[Union(2, typeof(SendCurrentClusterState))]
        public interface ISubscriptionMessage { }

        /// <summary>
        /// Subscribe an actor to new <see cref="Cluster"/> events.
        /// </summary>
        [MessagePackObject]
        public sealed class Subscribe : ISubscriptionMessage
        {
            /// <summary>
            /// Creates a new subscription
            /// </summary>
            /// <param name="subscriber">The actor being subscribed to events.</param>
            /// <param name="initialStateMode">The initial state of the subscription.</param>
            /// <param name="to">The range of event types to which we'll be subscribing.</param>
            public Subscribe(IActorRef subscriber, ClusterEvent.SubscriptionInitialStateMode initialStateMode,
                ImmutableHashSet<Type> to)
            {
                Subscriber = subscriber;
                InitialStateMode = initialStateMode;
                To = to;
            }

            /// <summary>
            /// The actor that is subscribed to cluster events.
            /// </summary>
            [Key(0)]
            public readonly IActorRef Subscriber;

            /// <summary>
            /// The delivery mechanism for the initial cluster state.
            /// </summary>
            [Key(1)]
            public readonly ClusterEvent.SubscriptionInitialStateMode InitialStateMode;

            /// <summary>
            /// The range of cluster events to which <see cref="Subscriber"/> is subscribed.
            /// </summary>
            [Key(2)]
            public readonly ImmutableHashSet<Type> To;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class Unsubscribe : ISubscriptionMessage, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="subscriber">TBD</param>
            /// <param name="to">TBD</param>
            [SerializationConstructor]
            public Unsubscribe(IActorRef subscriber, Type to)
            {
                To = to;
                Subscriber = subscriber;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly IActorRef Subscriber;

            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public readonly Type To;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class SendCurrentClusterState : ISubscriptionMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly IActorRef Receiver;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="receiver"><see cref="Akka.Cluster.ClusterEvent.CurrentClusterState"/> will be sent to the `receiver`</param>
            [SerializationConstructor]
            public SendCurrentClusterState(IActorRef receiver)
            {
                Receiver = receiver;
            }
        }

        /// <summary>
        /// INTERNAL API.
        ///
        /// Marker interface for publication events from Akka.Cluster.
        /// </summary>
        /// <remarks>
        /// <see cref="INoSerializationVerificationNeeded"/> is not explicitly used on the JVM,
        /// but without it we run into serialization issues via https://github.com/akkadotnet/akka.net/issues/3724
        /// </remarks>
        private interface IPublishMessage : INoSerializationVerificationNeeded { }

        /// <summary>
        /// INTERNAL API.
        /// 
        /// Used to publish Gossip and Membership changes inside Akka.Cluster.
        /// </summary>
        [MessagePackObject]
        internal sealed class PublishChanges : IPublishMessage
        {
            /// <summary>
            /// Creates a new <see cref="PublishChanges"/> message with updated gossip.
            /// </summary>
            /// <param name="newGossip">The gossip to publish internally.</param>
            [SerializationConstructor]
            public PublishChanges(Gossip newGossip)
            {
                NewGossip = newGossip;
            }

            /// <summary>
            /// The gossip being published.
            /// </summary>
            [Key(0)]
            public readonly Gossip NewGossip;
        }

        /// <summary>
        /// INTERNAL API.
        ///
        /// Used to publish events out to the cluster.
        /// </summary>
        [MessagePackObject]
        internal sealed class PublishEvent : IPublishMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="event">TBD</param>
            [SerializationConstructor]
            public PublishEvent(ClusterEvent.IClusterDomainEvent @event)
            {
                Event = @event;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly ClusterEvent.IClusterDomainEvent Event;
        }
    }

    /// <summary>
    /// Supervisor managing the different Cluster daemons.
    /// </summary>
    internal sealed class ClusterDaemon : ReceiveActorSlim, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
    {
        private IActorRef _coreSupervisor;
        private readonly ClusterSettings _settings;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly CoordinatedShutdown _coordShutdown = CoordinatedShutdown.Get(Context.System);
        private readonly TaskCompletionSource<Done> _clusterPromise = new TaskCompletionSource<Done>();

        /// <summary>
        /// Creates a new instance of the ClusterDaemon
        /// </summary>
        /// <param name="settings">The settings that will be used for the <see cref="Cluster"/>.</param>
        public ClusterDaemon(ClusterSettings settings)
        {
            // Important - don't use Cluster(context.system) in constructor because that would
            // cause deadlock. The Cluster extension is currently being created and is waiting
            // for response from GetClusterCoreRef in its constructor.
            // Child actors are therefore created when GetClusterCoreRef is received
            _coreSupervisor = null;
            _settings = settings;

            AddCoordinatedLeave();

            Receive<InternalClusterAction.GetClusterCoreRef>(e => HandleGetClusterCoreRef(e));

            Receive<InternalClusterAction.AddOnMemberUpListener>(e => HandleAddOnMemberUpListener(e));

            Receive<InternalClusterAction.AddOnMemberRemovedListener>(e => HandleAddOnMemberRemovedListener(e));

            Receive<CoordinatedShutdownLeave.LeaveReq>(e => HandleLeaveReq(e));
        }

        private void HandleGetClusterCoreRef(InternalClusterAction.GetClusterCoreRef msg)
        {
            if (_coreSupervisor is null) { CreateChildren(); }
            _coreSupervisor.Forward(msg);
        }

        private void HandleAddOnMemberUpListener(InternalClusterAction.AddOnMemberUpListener msg)
        {
            Context.ActorOf(
                Props.Create(() => new OnMemberStatusChangedListener(msg.Callback, MemberStatus.Up))
                    .WithDeploy(Deploy.Local));
        }

        private void HandleAddOnMemberRemovedListener(InternalClusterAction.AddOnMemberRemovedListener msg)
        {
            Context.ActorOf(
                Props.Create(() => new OnMemberStatusChangedListener(msg.Callback, MemberStatus.Removed))
                    .WithDeploy(Deploy.Local));
        }

        private void HandleLeaveReq(CoordinatedShutdownLeave.LeaveReq leave)
        {
            var actor = Context.ActorOf(Props.Create(() => new CoordinatedShutdownLeave()));

            // forward the Ask request so the shutdown task gets completed
            actor.Forward(leave);
        }

        private void AddCoordinatedLeave()
        {
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterLeave, "leave",
                InvokeClusterLeaveFunc, Context.System, Self, _coordShutdown);

            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterShutdown, "wait-shutdown",
                InvokeWaitShutdownFunc, _clusterPromise);
        }

        private static readonly Func<ActorSystem, IActorRef, CoordinatedShutdown, Task<Done>> InvokeClusterLeaveFunc = (s, ar, c) => InvokeClusterLeave(s, ar, c);
        private static Task<Done> InvokeClusterLeave(ActorSystem sys, IActorRef self, CoordinatedShutdown coordShutdown)
        {
            if (Cluster.Get(sys).IsTerminated || Cluster.Get(sys).SelfMember.Status == MemberStatus.Down)
            {
                return Task.FromResult(Done.Instance);
            }
            else
            {
                var timeout = coordShutdown.Timeout(CoordinatedShutdown.PhaseClusterLeave);
                return self.Ask<Done>(CoordinatedShutdownLeave.LeaveReq.Instance, timeout);
            }
        }

        private static readonly Func<TaskCompletionSource<Done>, Task<Done>> InvokeWaitShutdownFunc = tcs => InvokeWaitShutdown(tcs);
        private static Task<Done> InvokeWaitShutdown(TaskCompletionSource<Done> clusterPromise)
        {
            return clusterPromise.Task;
        }

        private void CreateChildren()
        {
            _coreSupervisor = Context.ActorOf(Props.Create<ClusterCoreSupervisor>(), "core");

            Context.ActorOf(Props.Create<ClusterHeartbeatReceiver>(), "heartbeatReceiver");
        }

        protected override void PostStop()
        {
            _clusterPromise.TrySetResult(Done.Instance);
            if (_settings.RunCoordinatedShutdownWhenDown)
            {
                // if it was stopped due to leaving CoordinatedShutdown was started earlier
                _coordShutdown.Run(CoordinatedShutdown.ClusterDowningReason.Instance);
            }
        }
    }

    /// <summary>
    /// ClusterCoreDaemon and ClusterDomainEventPublisher can't be restarted because the state
    /// would be obsolete. Shutdown the member if any those actors crashed.
    /// </summary>
    internal class ClusterCoreSupervisor : ReceiveActorSlim, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
    {
        private IActorRef _publisher;
        private IActorRef _coreDaemon;

        private readonly ILoggingAdapter _log = Context.GetLogger();

        /// <summary>
        /// Creates a new instance of the ClusterCoreSupervisor
        /// </summary>
        public ClusterCoreSupervisor()
        {
            _localOnlyDeciderFunc = e => LocalOnlyDecider(e);

            // Important - don't use Cluster(Context.System) in constructor because that would
            // cause deadlock. The Cluster extension is currently being created and is waiting
            // for response from GetClusterCoreRef in its constructor.
            // Child actors are therefore created when GetClusterCoreRef is received

            Receive<InternalClusterAction.GetClusterCoreRef>(e => HandleGetClusterCoreRef(e));
        }

        private void HandleGetClusterCoreRef(InternalClusterAction.GetClusterCoreRef cr)
        {
            if (_coreDaemon is null) { CreateChildren(); }
            Sender.Tell(_coreDaemon);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(_localOnlyDeciderFunc);
        }

        private readonly Func<Exception, Directive> _localOnlyDeciderFunc;
        private Directive LocalOnlyDecider(Exception e)
        {
            //TODO: JVM version matches NonFatal. Can / should we do something similar? 
            _log.Error(e, "Cluster node [{0}] crashed, [{1}] - shutting down...",
                Cluster.Get(Context.System).SelfAddress, e);
            Self.Tell(PoisonPill.Instance);
            return Directive.Stop;
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            Cluster.Get(Context.System).Shutdown();
        }

        private void CreateChildren()
        {
            _publisher =
                Context.ActorOf(Props.Create<ClusterDomainEventPublisher>().WithDispatcher(Context.Props.Dispatcher), "publisher");
            _coreDaemon = Context.ActorOf(Props.Create(() => new ClusterCoreDaemon(_publisher)).WithDispatcher(Context.Props.Dispatcher), "daemon");
            Context.Watch(_coreDaemon);
        }
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// Actor used to power the guts of the Akka.Cluster membership and gossip protocols.
    /// </summary>
    internal class ClusterCoreDaemon : UntypedActor, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
    {
        private readonly Cluster _cluster;
        /// <summary>
        /// The current self-unique address.
        /// </summary>
        protected readonly UniqueAddress SelfUniqueAddress;
        private const int NumberOfGossipsBeforeShutdownWhenLeaderExits = 5;
        private const int MaxGossipsBeforeShuttingDownMyself = 5;
        private const int MaxTicksBeforeShuttingDownMyself = 4;

        private readonly VectorClock.Node _vclockNode;

        internal static string VclockName(UniqueAddress node)
        {
            return $"{node.Address}-{node.Uid}";
        }

        private bool _isCurrentlyLeader;

        // note that self is not initially member,
        // and the SendGossip is not versioned for this 'Node' yet
        private Gossip _latestGossip = Gossip.Empty;

        private readonly bool _statsEnabled;
        private GossipStats _gossipStats = new GossipStats();
        private ImmutableList<Address> _seedNodes;
        private IActorRef _seedNodeProcess;
        private int _seedNodeProcessCounter = 0; //for unique names
        private Deadline _joinSeedNodesDeadline;

        private readonly IActorRef _publisher;
        private int _leaderActionCounter = 0;
        private int _selfDownCounter = 0;

        private bool _exitingTasksInProgress = false;
        private readonly TaskCompletionSource<Done> _selfExiting = new TaskCompletionSource<Done>();
        private readonly CoordinatedShutdown _coordShutdown = CoordinatedShutdown.Get(Context.System);
        private HashSet<UniqueAddress> _exitingConfirmed = new HashSet<UniqueAddress>(UniqueAddressComparer.Instance);


        /// <summary>
        /// Creates a new cluster core daemon instance.
        /// </summary>
        /// <param name="publisher">A reference to the <see cref="ClusterDomainEventPublisher"/>.</param>
        public ClusterCoreDaemon(IActorRef publisher)
        {
            _cluster = Cluster.Get(Context.System);
            _publisher = publisher;
            SelfUniqueAddress = _cluster.SelfUniqueAddress;
            _vclockNode = VectorClock.Node.Create(VclockName(SelfUniqueAddress));
            var settings = _cluster.Settings;
            var scheduler = _cluster.Scheduler;
            _seedNodes = _cluster.Settings.SeedNodes;

            _statsEnabled = settings.PublishStatsInterval.HasValue
                            && settings.PublishStatsInterval >= TimeSpan.Zero
                            && settings.PublishStatsInterval != TimeSpan.MaxValue;

            // start periodic gossip to random nodes in cluster
            _gossipTaskCancellable =
                scheduler.ScheduleTellRepeatedlyCancelable(
                    settings.PeriodicTasksInitialDelay.Max(settings.GossipInterval),
                    settings.GossipInterval,
                    Self,
                    InternalClusterAction.GossipTick.Instance,
                    Self);

            // start periodic cluster failure detector reaping (moving nodes condemned by the failure detector to unreachable list)
            _failureDetectorReaperTaskCancellable =
                scheduler.ScheduleTellRepeatedlyCancelable(
                    settings.PeriodicTasksInitialDelay.Max(settings.UnreachableNodesReaperInterval),
                    settings.UnreachableNodesReaperInterval,
                    Self,
                    InternalClusterAction.ReapUnreachableTick.Instance,
                    Self);

            // start periodic leader action management (only applies for the current leader)
            _leaderActionsTaskCancellable =
                scheduler.ScheduleTellRepeatedlyCancelable(
                    settings.PeriodicTasksInitialDelay.Max(settings.LeaderActionsInterval),
                    settings.LeaderActionsInterval,
                    Self,
                    InternalClusterAction.LeaderActionsTick.Instance,
                    Self);

            // start periodic publish of current stats
            if (settings.PublishStatsInterval is object && settings.PublishStatsInterval > TimeSpan.Zero && settings.PublishStatsInterval != TimeSpan.MaxValue)
            {
                _publishStatsTaskTaskCancellable =
                    scheduler.ScheduleTellRepeatedlyCancelable(
                        settings.PeriodicTasksInitialDelay.Max(settings.PublishStatsInterval.Value),
                        settings.PublishStatsInterval.Value,
                        Self,
                        InternalClusterAction.PublishStatsTick.Instance,
                        Self);
            }

            // register shutdown tasks
            AddCoordinatedLeave();
        }

        private void AddCoordinatedLeave()
        {
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterExiting, "wait-exiting", InvokeWaitExitingFunc, this);
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterExitingDone, "exiting-completed",
                InvokeExitingCompletedFunc, Context.System, Self, _coordShutdown);
        }

        private static readonly Func<ClusterCoreDaemon, Task<Done>> InvokeWaitExitingFunc = o => InvokeWaitExiting(o);
        private static Task<Done> InvokeWaitExiting(ClusterCoreDaemon owner)
        {
            if (owner._latestGossip.Members.IsEmpty)
                return Task.FromResult(Done.Instance); // not joined yet
            else
                return owner._selfExiting.Task;
        }

        private static readonly Func<ActorSystem, IActorRef, CoordinatedShutdown, Task<Done>> InvokeExitingCompletedFunc = (s, ar, c) => InvokeExitingCompleted(s, ar, c);
        private static Task<Done> InvokeExitingCompleted(ActorSystem sys, IActorRef self, CoordinatedShutdown coordShutdown)
        {
            if (Cluster.Get(sys).IsTerminated || Cluster.Get(sys).SelfMember.Status == MemberStatus.Down)
                return TaskEx.Completed;
            else
            {
                var timeout = coordShutdown.Timeout(CoordinatedShutdown.PhaseClusterExitingDone);
                return self.Ask(InternalClusterAction.ExitingCompleted.Instance, timeout).ContinueWith(tr => Done.Instance);
            }
        }

        private ActorSelection ClusterCore(Address address)
        {
            return Context.ActorSelection(new RootActorPath(address) / "system" / "cluster" / "core" / "daemon");
        }

        private readonly ICancelable _gossipTaskCancellable;
        private readonly ICancelable _failureDetectorReaperTaskCancellable;
        private readonly ICancelable _leaderActionsTaskCancellable;
        private readonly ICancelable _publishStatsTaskTaskCancellable;

        /// <inheritdoc cref="ActorBase.PreStart"/>
        protected override void PreStart()
        {
            Context.System.EventStream.Subscribe(Self, typeof(QuarantinedEvent));

            if (_cluster.DowningProvider.DowningActorProps is object)
            {
                var props = _cluster.DowningProvider.DowningActorProps;
                var propsWithDispatcher = props.Dispatcher == Deploy.NoDispatcherGiven
                    ? props.WithDispatcher(Context.Props.Dispatcher)
                    : props;

                Context.ActorOf(propsWithDispatcher, "downingProvider");
            }

            if (_seedNodes.IsEmpty)
            {
                if (_cluster.IsInfoEnabled) _cluster.NoSeedNodesConfiguredManualClusterJoinRequired();
            }
            else
            {
                Self.Tell(new InternalClusterAction.JoinSeedNodes(_seedNodes));
            }
        }

        /// <inheritdoc cref="ActorBase.PostStop"/>
        protected override void PostStop()
        {
            Context.System.EventStream.Unsubscribe(Self);
            _gossipTaskCancellable.Cancel();
            _failureDetectorReaperTaskCancellable.Cancel();
            _leaderActionsTaskCancellable.Cancel();
            if (_publishStatsTaskTaskCancellable is object) _publishStatsTaskTaskCancellable.Cancel();
            _selfExiting.TrySetResult(Done.Instance);
        }

        private void ExitingCompleted()
        {
            if (_cluster.IsInfoEnabled) _cluster.ExitingCompleted();
            // ExitingCompleted sent via CoordinatedShutdown to continue the leaving process.
            _exitingTasksInProgress = false;

            // mark as seen
            _latestGossip = _latestGossip.Seen(SelfUniqueAddress);
            AssertLatestGossip();
            Publish(_latestGossip);

            // Let others know (best effort) before shutdown. Otherwise they will not see
            // convergence of the Exiting state until they have detected this node as
            // unreachable and the required downing has finished. They will still need to detect
            // unreachable, but Exiting unreachable will be removed without downing, i.e.
            // normally the leaving of a leader will be graceful without the need
            // for downing. However, if those final gossip messages never arrive it is
            // alright to require the downing, because that is probably caused by a
            // network failure anyway.
            SendGossipRandom(NumberOfGossipsBeforeShutdownWhenLeaderExits);

            // send ExitingConfirmed to two potential leaders
            var membersWithoutSelf = _latestGossip.Members.Where(m => !m.UniqueAddress.Equals(SelfUniqueAddress))
                .ToImmutableSortedSet();
            var leader = _latestGossip.LeaderOf(membersWithoutSelf, SelfUniqueAddress);
            if (leader is object)
            {
                ClusterCore(leader.Address).Tell(new InternalClusterAction.ExitingConfirmed(SelfUniqueAddress));
                var leader2 =
                    _latestGossip.LeaderOf(
                        membersWithoutSelf.Where(x => !x.UniqueAddress.Equals(leader)).ToImmutableSortedSet(),
                        SelfUniqueAddress);
                if (leader2 is object)
                {
                    ClusterCore(leader2.Address).Tell(new InternalClusterAction.ExitingConfirmed(SelfUniqueAddress));
                }
            }

            Shutdown();
        }

        private void ReceiveExitingConfirmed(UniqueAddress node)
        {
            if (_cluster.IsInfoEnabled) _cluster.ExitingConfirmed(node);
            _exitingConfirmed.Add(node);
        }

        private void CleanupExitingConfirmed()
        {
            // in case the actual removal was performed by another leader node
            if (_exitingConfirmed.Count > 0)
            {
                _exitingConfirmed = new HashSet<UniqueAddress>(_exitingConfirmed.Where(n => _latestGossip.Members.Any(m => m.UniqueAddress.Equals(n))), UniqueAddressComparer.Instance);
            }
        }

        private bool ReceiveExitingCompleted(object message)
        {
            if (message is InternalClusterAction.ExitingCompleted)
            {
                ExitingCompleted();
                // complete the Ask
                Sender.Tell(Done.Instance);
                return true;
            }
            return false;
        }

        private void Uninitialized(object message)
        {
            switch (message)
            {
                case InternalClusterAction.InitJoin _:
                    if (_cluster.IsInfoEnabled) _cluster.ReceivedInitJoinMessageFromButThisNodeIsNotInitializedYet(Sender);
                    Sender.Tell(new InternalClusterAction.InitJoinNack(_cluster.SelfAddress));
                    break;

                case ClusterUserAction.JoinTo jt:
                    Join(jt.Address);
                    break;

                case InternalClusterAction.JoinSeedNodes js:
                    ResetJoinSeedNodesDeadline();
                    JoinSeedNodes(js.SeedNodes);
                    break;

                case InternalClusterAction.ISubscriptionMessage isub:
                    _publisher.Forward(isub);
                    break;

                case InternalClusterAction.ITick _:
                    if (_joinSeedNodesDeadline is object && _joinSeedNodesDeadline.IsOverdue) JoinSeedNodesWasUnsuccessful();
                    break;

                default:
                    if (!ReceiveExitingCompleted(message)) { Unhandled(message); }
                    break;
            }
        }

        private void TryingToJoin(object message, Address joinWith, Deadline deadline)
        {
            switch (message)
            {
                case InternalClusterAction.Welcome w:
                    Welcome(joinWith, w.From, w.Gossip);
                    break;

                case InternalClusterAction.InitJoin _:
                    if (_cluster.IsInfoEnabled) _cluster.ReceivedInitJoinMessageFromButThisNodeIsNotInitializedYet(Sender);
                    Sender.Tell(new InternalClusterAction.InitJoinNack(_cluster.SelfAddress));
                    break;

                case ClusterUserAction.JoinTo jt:
                    BecomeUninitialized();
                    Join(jt.Address);
                    break;

                case InternalClusterAction.JoinSeedNodes js:
                    ResetJoinSeedNodesDeadline();
                    BecomeUninitialized();
                    JoinSeedNodes(js.SeedNodes);
                    break;

                case InternalClusterAction.ISubscriptionMessage isub:
                    _publisher.Forward(isub);
                    break;

                case InternalClusterAction.ITick _:
                    if (_joinSeedNodesDeadline is object && _joinSeedNodesDeadline.IsOverdue)
                    {
                        JoinSeedNodesWasUnsuccessful();
                    }
                    else if (deadline is object && deadline.IsOverdue)
                    {
                        // join attempt failed, retry
                        BecomeUninitialized();
                        if (!_seedNodes.IsEmpty) JoinSeedNodes(_seedNodes);
                        else Join(joinWith);
                    }
                    break;

                default:
                    if (!ReceiveExitingCompleted(message)) { Unhandled(message); }
                    break;
            }
        }

        private void ResetJoinSeedNodesDeadline()
        {
            _joinSeedNodesDeadline = _cluster.Settings.ShutdownAfterUnsuccessfulJoinSeedNodes is object
                ? Deadline.Now + _cluster.Settings.ShutdownAfterUnsuccessfulJoinSeedNodes
                : null;
        }

        private void JoinSeedNodesWasUnsuccessful()
        {
            _log.JoiningOfSeedNodesWasUnsuccessful(_cluster, _seedNodes);

            _joinSeedNodesDeadline = null;
            _coordShutdown.Run(CoordinatedShutdown.ClusterJoinUnsuccessfulReason.Instance);
        }

        private void BecomeUninitialized()
        {
            // make sure that join process is stopped
            StopSeedNodeProcess();
            Context.Become(Uninitialized);
        }

        private void BecomeInitialized()
        {
            // start heartbeatSender here, and not in constructor to make sure that
            // heartbeating doesn't start before Welcome is received
            Context.ActorOf(Props.Create<ClusterHeartbeatSender>().WithDispatcher(_cluster.Settings.UseDispatcher),
                "heartbeatSender");
            // make sure that join process is stopped
            StopSeedNodeProcess();
            Context.Become(Initialized);
        }

        private void Initialized(object message)
        {
            switch (message)
            {
                case GossipEnvelope ge:
                    var receivedType = ReceiveGossip(ge);
                    if (_cluster.Settings.VerboseGossipReceivedLogging)
                    {
                        _log.ClusterNodeReceivedGossip(_cluster, ge, receivedType);
                    }
                    break;

                case GossipStatus gs:
                    ReceiveGossipStatus(gs);
                    break;

                case InternalClusterAction.GossipTick _:
                    GossipTick();
                    break;

                case InternalClusterAction.GossipSpeedupTick _:
                    GossipSpeedupTick();
                    break;

                case InternalClusterAction.ReapUnreachableTick _:
                    ReapUnreachableMembers();
                    break;

                case InternalClusterAction.LeaderActionsTick _:
                    LeaderActions();
                    break;

                case InternalClusterAction.PublishStatsTick _:
                    PublishInternalStats();
                    break;

                case InternalClusterAction.InitJoin _:
                    if (_cluster.IsInfoEnabled) _cluster.ReceivedInitJoinMessageFrom(Sender, SelfUniqueAddress);
                    InitJoin();
                    break;

                case InternalClusterAction.Join join:
                    Joining(join.Node, join.Roles);
                    break;

                case ClusterUserAction.Down down:
                    Downing(down.Address);
                    break;

                case ClusterUserAction.Leave leave:
                    Leaving(leave.Address);
                    break;

                case InternalClusterAction.SendGossipTo sendGossipTo:
                    SendGossipTo(sendGossipTo.Address);
                    break;

                case InternalClusterAction.ISubscriptionMessage _:
                    _publisher.Forward(message);
                    break;

                case QuarantinedEvent q:
                    Quarantined(new UniqueAddress(q.Address, q.Uid));
                    break;

                case ClusterUserAction.JoinTo jt:
                    if (_cluster.IsInfoEnabled) _cluster.TryingToJoinWhenAlreadyPartOfAClusterIgnoring(jt);
                    break;

                case InternalClusterAction.JoinSeedNodes joinSeedNodes:
                    if (_cluster.IsInfoEnabled) _cluster.TryingToJoinSeedNodesWhenAlreadyPartOfAClusterIgnoring(joinSeedNodes);
                    break;

                case InternalClusterAction.ExitingConfirmed exitingConfirmed:
                    ReceiveExitingConfirmed(exitingConfirmed.Address);
                    break;

                default:
                    if (ReceiveExitingCompleted(message)) { }
                    else
                    {
                        Unhandled(message);
                    }
                    break;
            }
        }

        /// <inheritdoc cref="ActorBase.PreStart"/>
        protected override void OnReceive(object message)
        {
            Uninitialized(message);
        }

        /// <inheritdoc cref="ActorBase.Unhandled"/>
        protected override void Unhandled(object message)
        {
            switch (message)
            {
                case InternalClusterAction.ITick _:
                case GossipEnvelope _:
                case GossipStatus _:
                case InternalClusterAction.ExitingConfirmed _:
                    // do nothing
                    break;

                default:
                    base.Unhandled(message);
                    break;
            }
        }

        /// <summary>
        /// Begins the joining process.
        /// </summary>
        public void InitJoin()
        {
            var selfStatus = _latestGossip.GetMember(SelfUniqueAddress).Status;
            if (Gossip.RemoveUnreachableWithMemberStatus.Contains(selfStatus))
            {
                if (_cluster.IsInfoEnabled) _cluster.SendingInitJoinNackMessageFromNode(SelfUniqueAddress, Sender);
                // prevents a Down and Exiting node from being used for joining
                Sender.Tell(new InternalClusterAction.InitJoinNack(_cluster.SelfAddress));
            }
            else
            {
                // TODO: add config checking
                if (_cluster.IsInfoEnabled) _cluster.SendingInitJoinNackMessageFromNode(SelfUniqueAddress, Sender);
                Sender.Tell(new InternalClusterAction.InitJoinAck(_cluster.SelfAddress));
            }
        }

        /// <summary>
        /// Attempts to join this node or one or more seed nodes.
        /// </summary>
        /// <param name="newSeedNodes">The list of seed node we're attempting to join.</param>
        public void JoinSeedNodes(ImmutableList<Address> newSeedNodes)
        {
            if (!newSeedNodes.IsEmpty)
            {
                StopSeedNodeProcess();
                _seedNodes = newSeedNodes; // keep them for retry
                if (newSeedNodes.SequenceEqual(ImmutableList.Create(_cluster.SelfAddress))) // self-join for a singleton cluster
                {
                    Self.Tell(new ClusterUserAction.JoinTo(_cluster.SelfAddress));
                    _seedNodeProcess = null;
                }
                else
                {
                    // use unique name of this actor, stopSeedNodeProcess doesn't wait for termination
                    _seedNodeProcessCounter += 1;
                    if (newSeedNodes.Head().Equals(_cluster.SelfAddress))
                    {
                        _seedNodeProcess = Context.ActorOf(Props.Create(() => new FirstSeedNodeProcess(newSeedNodes)).WithDispatcher(_cluster.Settings.UseDispatcher), "firstSeedNodeProcess-" + _seedNodeProcessCounter);
                    }
                    else
                    {
                        _seedNodeProcess = Context.ActorOf(Props.Create(() => new JoinSeedNodeProcess(newSeedNodes)).WithDispatcher(_cluster.Settings.UseDispatcher), "joinSeedNodeProcess-" + _seedNodeProcessCounter);
                    }
                }
            }
        }

        /// <summary>
        /// Try to join this cluster node with the node specified by `address`.
        /// It's only allowed to join from an empty state, i.e. when not already a member.
        /// A `Join(selfUniqueAddress)` command is sent to the node to join,
        /// which will reply with a `Welcome` message.
        /// </summary>
        /// <param name="address">The address of the node we're going to join.</param>
        /// <exception cref="InvalidOperationException">Join can only be done from an empty state</exception>
        public void Join(Address address)
        {
            var selfAddress = _cluster.SelfAddress;
            if (address.Protocol != selfAddress.Protocol)
            {
                if (_log.IsWarningEnabled) _log.TryingToJoinMemberWithWrongProtocol(_cluster, address);
            }
            else if (address.System != selfAddress.System)
            {
                if (_log.IsWarningEnabled) _log.TryingToJoinMemberWithWrongActorSystemName(_cluster, address);
            }
            else
            {
                //TODO: Akka exception?
                if (!_latestGossip.Members.IsEmpty) ThrowHelper.ThrowInvalidOperationException_JoinCanOnlyBeDoneFromAnEmptyState();

                // to support manual join when joining to seed nodes is stuck (no seed nodes available)
                StopSeedNodeProcess();

                if (address.Equals(selfAddress))
                {
                    BecomeInitialized();
                    Joining(SelfUniqueAddress, _cluster.SelfRoles);
                }
                else
                {
                    var joinDeadline = _cluster.Settings.RetryUnsuccessfulJoinAfter is null
                        ? null
                        : Deadline.Now + _cluster.Settings.RetryUnsuccessfulJoinAfter;

                    Context.Become(m => TryingToJoin(m, address, joinDeadline));
                    ClusterCore(address).Tell(new InternalClusterAction.Join(_cluster.SelfUniqueAddress, _cluster.SelfRoles));
                }
            }
        }

        /// <summary>
        /// Stops the seed node process after the cluster has started.
        /// </summary>
        public void StopSeedNodeProcess()
        {
            if (_seedNodeProcess is object)
            {
                // manual join, abort current seedNodeProcess
                Context.Stop(_seedNodeProcess);
                _seedNodeProcess = null;
            }
            else
            {
                // no seedNodeProcess in progress
            }
        }


        /// <summary>
        /// State transition to JOINING - new node joining.
        /// Received `Join` message and replies with `Welcome` message, containing
        /// current gossip state, including the new joining member.
        /// </summary>
        /// <param name="node">TBD</param>
        /// <param name="roles">TBD</param>
        public void Joining(UniqueAddress node, ImmutableHashSet<string> roles)
        {
            var selfStatus = _latestGossip.GetMember(SelfUniqueAddress).Status;
            if (!string.Equals(node.Address.Protocol, _cluster.SelfAddress.Protocol, StringComparison.Ordinal))
            {
                if (_log.IsWarningEnabled) _log.MemberWithWrongProtocolTriedToJoin(_cluster, node);
            }
            else if (!string.Equals(node.Address.System, _cluster.SelfAddress.System, StringComparison.Ordinal))
            {
                if (_log.IsWarningEnabled) _log.MemberWithWrongActorSystemNameTriedToJoin(_cluster, node);
            }
            else if (Gossip.RemoveUnreachableWithMemberStatus.Contains(selfStatus))
            {
                if (_cluster.IsInfoEnabled) _cluster.TryingToJoinToMemberIgnoring(node, selfStatus);
            }
            else
            {
                var localMembers = _latestGossip.Members;

                // check by address without uid to make sure that node with same host:port is not allowed
                // to join until previous node with that host:port has been removed from the cluster
                var localMember = localMembers.FirstOrDefault(m => m.Address.Equals(node.Address));
                if (localMember is object && localMember.UniqueAddress.Equals(node))
                {
                    // node retried join attempt, probably due to lost Welcome message
                    if (_cluster.IsInfoEnabled) _cluster.ExistingMemberIsJoiningAgain(node);
                    if (!node.Equals(SelfUniqueAddress))
                    {
                        Sender.Tell(new InternalClusterAction.Welcome(SelfUniqueAddress, _latestGossip));
                    }
                }
                else if (localMember is object)
                {
                    // node restarted, same host:port as existing member, but with different uid
                    // safe to down and later remove existing member
                    // new node will retry join
                    if (_cluster.IsInfoEnabled) _cluster.NewIncarnationOfExistingMemberIsTryingToJoin(node);

                    if (localMember.Status != MemberStatus.Down)
                    {
                        // we can confirm it as terminated/unreachable immediately
                        var newReachability = _latestGossip.Overview.Reachability.Terminated(
                            _cluster.SelfUniqueAddress, localMember.UniqueAddress);
                        var newOverview = _latestGossip.Overview.Copy(reachability: newReachability);
                        var newGossip = _latestGossip.Copy(overview: newOverview);
                        UpdateLatestGossip(newGossip);
                        Downing(localMember.Address);
                    }
                }
                else
                {
                    // remove the node from the failure detector
                    _cluster.FailureDetector.Remove(node.Address);

                    // add joining node as Joining
                    // add self in case someone else joins before self has joined (Set discards duplicates)
                    var newMembers = localMembers
                            .Add(Member.Create(node, roles))
                            .Add(Member.Create(_cluster.SelfUniqueAddress, _cluster.SelfRoles));
                    var newGossip = _latestGossip.Copy(members: newMembers);

                    UpdateLatestGossip(newGossip);

                    if (node.Equals(SelfUniqueAddress))
                    {
                        if (_cluster.IsInfoEnabled) _cluster.NodeIsJOININGItself(node, roles);

                        if (localMembers.IsEmpty)
                        {
                            // important for deterministic oldest when bootstrapping
                            LeaderActions();
                        }
                    }
                    else
                    {
                        if (_cluster.IsInfoEnabled) _cluster.NodeIsJOININGRoles(node, roles);
                        Sender.Tell(new InternalClusterAction.Welcome(SelfUniqueAddress, _latestGossip));
                    }

                    Publish(_latestGossip);
                }
            }
        }

        /// <summary>
        /// Reply from Join request
        /// </summary>
        /// <param name="joinWith">TBD</param>
        /// <param name="from">TBD</param>
        /// <param name="gossip">TBD</param>
        /// <exception cref="InvalidOperationException">Welcome can only be done from an empty state</exception>
        public void Welcome(Address joinWith, UniqueAddress from, Gossip gossip)
        {
            if (!_latestGossip.Members.IsEmpty) ThrowHelper.ThrowInvalidOperationException_WelcomeCanOnlyBeDoneFromAnEmptyState();
            if (!joinWith.Equals(from.Address))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringWelcomeFromWhenTryingToJoinWith(from, joinWith);
            }
            else
            {
                if (_cluster.IsInfoEnabled) _cluster.WelcomeFrom(from);
                _latestGossip = gossip.Seen(SelfUniqueAddress);
                AssertLatestGossip();
                Publish(_latestGossip);
                if (!from.Equals(SelfUniqueAddress))
                    GossipTo(from, Sender);
                BecomeInitialized();
            }
        }

        /// <summary>
        /// State transition to LEAVING.
        /// The node will eventually be removed by the leader, after hand-off in EXITING, and only after
        /// removal a new node with same address can join the cluster through the normal joining procedure.
        /// </summary>
        /// <param name="address">The address of the node who is leaving the cluster.</param>
        public void Leaving(Address address)
        {
            // only try to update if the node is available (in the member ring)
            if (_latestGossip.Members.Any(m => m.Address.Equals(address) && (m.Status == MemberStatus.Joining || m.Status == MemberStatus.WeaklyUp || m.Status == MemberStatus.Up)))
            {
                // mark node as LEAVING
                var newMembers = _latestGossip.Members.Select(m =>
                {
                    if (m.Address == address) return m.Copy(status: MemberStatus.Leaving);
                    return m;
                }).ToImmutableSortedSet(); // mark node as LEAVING
                var newGossip = _latestGossip.Copy(members: newMembers);

                UpdateLatestGossip(newGossip);

                if (_cluster.IsInfoEnabled) _cluster.MarkedAddress(address);
                Publish(_latestGossip);
                // immediate gossip to speed up the leaving process
                SendGossip();
            }
        }

        /// <summary>
        /// This method is called when a member sees itself as Exiting or Down.
        /// </summary>
        public void Shutdown()
        {
            _cluster.Shutdown();
        }

        /// <summary>
        /// State transition to DOWN.
        /// Its status is set to DOWN.The node is also removed from the `seen` table.
        /// The node will eventually be removed by the leader, and only after removal a new node with same address can
        /// join the cluster through the normal joining procedure.
        /// </summary>
        /// <param name="address">The address of the member that will be downed.</param>
        public void Downing(Address address)
        {
            var localGossip = _latestGossip;
            var localMembers = localGossip.Members;
            var localOverview = localGossip.Overview;
            var localSeen = localOverview.Seen;
            var localReachability = localOverview.Reachability;

            // check if the node to DOWN is in the 'members' set
            var member = localMembers.FirstOrDefault(m => m.Address == address);
            if (member is object && member.Status != MemberStatus.Down)
            {
                if (_cluster.IsInfoEnabled)
                {
                    if (localReachability.IsReachable(member.UniqueAddress))
                    {
                        _cluster.MarkingNode(member);
                    }
                    else
                    {
                        _cluster.MarkingUnreachableNode(member);
                    }
                }

                // replace member (changed status)
                var newMembers = localMembers.Remove(member).Add(member.Copy(MemberStatus.Down));
                // remove nodes marked as DOWN from the 'seen' table
                var newSeen = localSeen.Remove(member.UniqueAddress);

                //update gossip overview
                var newOverview = localOverview.Copy(seen: newSeen);
                var newGossip = localGossip.Copy(members: newMembers, overview: newOverview); //update gossip
                UpdateLatestGossip(newGossip);

                Publish(_latestGossip);
            }
            else if (member is object)
            {
                // already down
            }
            else
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringDownOfUnknownNode(address);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        public void Quarantined(UniqueAddress node)
        {
            var localGossip = _latestGossip;
            if (localGossip.HasMember(node))
            {
                var newReachability = localGossip.Overview.Reachability.Terminated(SelfUniqueAddress, node);
                var newOverview = localGossip.Overview.Copy(reachability: newReachability);
                var newGossip = localGossip.Copy(overview: newOverview);
                UpdateLatestGossip(newGossip);
                if (_log.IsWarningEnabled) _log.MarkingNodeAsTERMINATED(Self, node, _cluster);
                Publish(_latestGossip);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="status">TBD</param>
        public void ReceiveGossipStatus(GossipStatus status)
        {
            var from = status.From;
            if (!_latestGossip.Overview.Reachability.IsReachable(SelfUniqueAddress, from))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringReceivedGossipStatusFromUnreachable(from);
            }
            else if (_latestGossip.Members.All(m => !m.UniqueAddress.Equals(from)))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringReceivedGossipStatusFromUnknown(from);
            }
            else
            {
                var comparison = status.Version.CompareTo(_latestGossip.Version);
                switch (comparison)
                {
                    case VectorClock.Ordering.Same:
                        //same version
                        break;
                    case VectorClock.Ordering.After:
                        GossipStatusTo(from, Sender); //remote is newer
                        break;
                    default:
                        GossipTo(from, Sender); //conflicting or local is newer
                        break;
                }
            }
        }

        /// <summary>
        /// The types of gossip actions that receive gossip has performed.
        /// </summary>
        public enum ReceiveGossipType
        {
            /// <summary>
            /// Gossip is ignored because node was not part of cluster, unreachable, etc..
            /// </summary>
            Ignored,
            /// <summary>
            /// Gossip received is older than what we currently have
            /// </summary>
            Older,
            /// <summary>
            /// Gossip received is newer than what we currently have
            /// </summary>
            Newer,
            /// <summary>
            /// Gossip received is same as what we currently have
            /// </summary>
            Same,
            /// <summary>
            /// Gossip received is concurrent with what we haved, and then merged.
            /// </summary>
            Merge
        }

        /// <summary>
        /// The types of gossip actions that receive gossip has performed.
        /// </summary>
        /// <param name="envelope">The gossip payload.</param>
        /// <returns>A command indicating how the gossip should be handled.</returns>
        public ReceiveGossipType ReceiveGossip(GossipEnvelope envelope)
        {
            var from = envelope.From;
            var remoteGossip = envelope.Gossip;
            var localGossip = _latestGossip;

#if DEBUG
            var debugEnabled = _log.IsDebugEnabled;
#endif
            if (remoteGossip.Equals(Gossip.Empty))
            {
#if DEBUG
                if (debugEnabled) _log.ClusterNodeIgnoringReceivedGossip(_cluster, from);
#endif
                return ReceiveGossipType.Ignored;
            }
            if (!envelope.To.Equals(SelfUniqueAddress))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringReceivedGossipIntendedForSomeone(from, envelope);
                return ReceiveGossipType.Ignored;
            }
            if (!localGossip.Overview.Reachability.IsReachable(SelfUniqueAddress, from))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringReceivedGossipGromUnreachable(from);
                return ReceiveGossipType.Ignored;
            }
            if (localGossip.Members.All(m => !m.UniqueAddress.Equals(from)))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringReceivedGossipFromUnknown(from);
                return ReceiveGossipType.Ignored;
            }
            if (remoteGossip.Members.All(m => !m.UniqueAddress.Equals(SelfUniqueAddress)))
            {
                if (_cluster.IsInfoEnabled) _cluster.IgnoringReceivedGossipThatDoesNotContainMyself(from);
                return ReceiveGossipType.Ignored;
            }

            var comparison = remoteGossip.Version.CompareTo(localGossip.Version);

            Gossip winningGossip;
            bool talkback;
            ReceiveGossipType gossipType;

            switch (comparison)
            {
                case VectorClock.Ordering.Same:
                    //same version
                    talkback = !_exitingTasksInProgress && !remoteGossip.SeenByNode(SelfUniqueAddress);
                    winningGossip = remoteGossip.MergeSeen(localGossip);
                    gossipType = ReceiveGossipType.Same;
                    break;
                case VectorClock.Ordering.Before:
                    //local is newer
                    winningGossip = localGossip;
                    talkback = true;
                    gossipType = ReceiveGossipType.Older;
                    break;
                case VectorClock.Ordering.After:
                    //remote is newer
                    winningGossip = remoteGossip;
                    talkback = !_exitingTasksInProgress && !remoteGossip.SeenByNode(SelfUniqueAddress);
                    gossipType = ReceiveGossipType.Newer;
                    break;
                default:
                    // conflicting versions, merge
                    // We can see that a removal was done when it is not in one of the gossips has status
                    // Down or Exiting in the other gossip.
                    // Perform the same pruning (clear of VectorClock) as the leader did when removing a member.
                    // Removal of member itself is handled in merge (pickHighestPriority)
                    var prunedLocalGossip = localGossip.Members.Aggregate(localGossip, (g, m) =>
                    {
                        if (Gossip.RemoveUnreachableWithMemberStatus.Contains(m.Status) && !remoteGossip.Members.Contains(m))
                        {
#if DEBUG
                            if (debugEnabled) _log.ClusterNodePrunedConflictingLocalGossip(_cluster, m);
#endif
                            return g.Prune(VectorClock.Node.Create(VclockName(m.UniqueAddress)));
                        }
                        return g;
                    });

                    var prunedRemoteGossip = remoteGossip.Members.Aggregate(remoteGossip, (g, m) =>
                    {
                        if (Gossip.RemoveUnreachableWithMemberStatus.Contains(m.Status) && !localGossip.Members.Contains(m))
                        {
#if DEBUG
                            if (debugEnabled) _log.ClusterNodePrunedConflictingRemoteGossip(_cluster, m);
#endif
                            return g.Prune(VectorClock.Node.Create(VclockName(m.UniqueAddress)));
                        }
                        return g;
                    });

                    //conflicting versions, merge
                    winningGossip = prunedRemoteGossip.Merge(prunedLocalGossip);
                    talkback = true;
                    gossipType = ReceiveGossipType.Merge;
                    break;
            }

            // Don't mark gossip state as seen while exiting is in progress, e.g.
            // shutting down singleton actors. This delays removal of the member until
            // the exiting tasks have been completed.
            if (_exitingTasksInProgress)
            {
                _latestGossip = winningGossip;
            }
            else
            {
                _latestGossip = winningGossip.Seen(SelfUniqueAddress);
            }

            AssertLatestGossip();

            // for all new joining nodes we remove them from the failure detector
            foreach (var node in _latestGossip.Members)
            {
                if (node.Status == MemberStatus.Joining && !localGossip.Members.Contains(node))
                {
                    _cluster.FailureDetector.Remove(node.Address);
                }
            }

#if DEBUG
            if (debugEnabled)
            {
                _log.ClusterNodeReceivingGossipFrom(_cluster, from);

                if (comparison == VectorClock.Ordering.Concurrent)
                {
                    _log.CouldNotEstablishACausalRelationship(remoteGossip, localGossip, winningGossip);
                }
            }
#endif

            if (_statsEnabled)
            {
                switch (gossipType)
                {
                    case ReceiveGossipType.Merge:
                        _gossipStats = _gossipStats.IncrementMergeCount();
                        break;
                    case ReceiveGossipType.Same:
                        _gossipStats = _gossipStats.IncrementSameCount();
                        break;
                    case ReceiveGossipType.Newer:
                        _gossipStats = _gossipStats.IncrementNewerCount();
                        break;
                    case ReceiveGossipType.Older:
                        _gossipStats = _gossipStats.IncrementOlderCount();
                        break;
                }
            }

            Publish(_latestGossip);

            var selfStatus = _latestGossip.GetMember(SelfUniqueAddress).Status;
            if (selfStatus == MemberStatus.Exiting && !_exitingTasksInProgress)
            {
                // ExitingCompleted will be received via CoordinatedShutdown to continue
                // the leaving process. Meanwhile the gossip state is not marked as seen.
                _exitingTasksInProgress = true;
                if (_coordShutdown.ShutdownReason is null)
                {
                    if (_cluster.IsInfoEnabled) _cluster.ExitingStartingCoordinatedShutdown();
                }
                _selfExiting.TrySetResult(Done.Instance);
                _coordShutdown.Run(CoordinatedShutdown.ClusterLeavingReason.Instance);
            }

            if (talkback)
            {
                // send back gossip to sender() when sender() had different view, i.e. merge, or sender() had
                // older or sender() had newer
                GossipTo(from, Sender);
            }

            return gossipType;
        }

        /// <summary>
        /// Sends gossip and schedules two future intervals for more gossip
        /// </summary>
        public void GossipTick()
        {
            SendGossip();
            if (IsGossipSpeedupNeeded())
            {
                _cluster.Scheduler.ScheduleTellOnce(new TimeSpan(_cluster.Settings.GossipInterval.Ticks / 3), Self,
                    InternalClusterAction.GossipSpeedupTick.Instance, ActorRefs.NoSender);
                _cluster.Scheduler.ScheduleTellOnce(new TimeSpan(_cluster.Settings.GossipInterval.Ticks * 2 / 3), Self,
                    InternalClusterAction.GossipSpeedupTick.Instance, ActorRefs.NoSender);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void GossipSpeedupTick()
        {
            if (IsGossipSpeedupNeeded()) SendGossip();
        }

        /// <summary>
        /// TBD
        /// </summary>
        public bool IsGossipSpeedupNeeded()
        {
            return _latestGossip.Overview.Seen.Count < _latestGossip.Members.Count / 2;
        }

        private void SendGossipRandom(int n)
        {
            if (!IsSingletonCluster && n > 0)
            {
                var localGossip = _latestGossip;
                var possibleTargets =
                    localGossip.Members.Where(m => ValidNodeForGossip(m.UniqueAddress))
                        .Select(m => m.UniqueAddress)
                        .ToList();
                var randomTargets = possibleTargets.Count <= n ? possibleTargets : possibleTargets.Shuffle().Slice(0, n);
                randomTargets.ForEach(GossipTo);
            }
        }

        /// <summary>
        /// Initiates a new round of gossip.
        /// </summary>
        public void SendGossip()
        {
            if (!IsSingletonCluster)
            {
                var localGossip = _latestGossip;

                ImmutableList<UniqueAddress> preferredGossipTarget;

                if (ThreadLocalRandom.Current.NextDouble() < AdjustedGossipDifferentViewProbability)
                {
                    // If it's time to try to gossip to some nodes with a different view
                    // gossip to a random alive member with preference to a member with older gossip version
                    preferredGossipTarget = ImmutableList.CreateRange(localGossip.Members.Where(m => !localGossip.SeenByNode(m.UniqueAddress) &&
                                                   ValidNodeForGossip(m.UniqueAddress)).Select(m => m.UniqueAddress));
                }
                else
                {
                    preferredGossipTarget = ImmutableList<UniqueAddress>.Empty;
                }

                if (!preferredGossipTarget.IsEmpty)
                {
                    var peer = SelectRandomNode(preferredGossipTarget);
                    // send full gossip because it has different view
                    GossipTo(peer);
                }
                else
                {
                    // Fall back to localGossip; important to preserve the original order
                    var peer =
                        SelectRandomNode(
                            ImmutableList.CreateRange(
                                localGossip.Members.Where(m => ValidNodeForGossip(m.UniqueAddress))
                                    .Select(m => m.UniqueAddress)));

                    if (peer is object)
                    {
                        if (localGossip.SeenByNode(peer)) GossipStatusTo(peer);
                        else GossipTo(peer);
                    }
                }
            }
        }

        /// <summary>
        /// For large clusters we should avoid shooting down individual
        /// nodes. Therefore the probability is reduced for large clusters
        /// </summary>
        public double AdjustedGossipDifferentViewProbability
        {
            get
            {
                var size = _latestGossip.Members.Count;
                var low = _cluster.Settings.ReduceGossipDifferentViewProbability;
                var high = low * 3;
                // start reduction when cluster is larger than configured ReduceGossipDifferentViewProbability
                if (size <= low)
                    return _cluster.Settings.GossipDifferentViewProbability;

                // don't go lower than 1/10 of the configured GossipDifferentViewProbability
                var minP = _cluster.Settings.GossipDifferentViewProbability / 10;
                if (size >= high) return minP;
                // linear reduction of the probability with increasing number of nodes
                // from ReduceGossipDifferentViewProbability at ReduceGossipDifferentViewProbability nodes
                // to ReduceGossipDifferentViewProbability / 10 at ReduceGossipDifferentViewProbability * 3 nodes
                // i.e. default from 0.8 at 400 nodes, to 0.08 at 1600 nodes
                var k = (minP - _cluster.Settings.GossipDifferentViewProbability) / (high - low);
                return _cluster.Settings.GossipDifferentViewProbability + (size - low) * k;
            }
        }

        /// <summary>
        /// Runs periodic leader actions, such as member status transitions, assigning partitions etc.
        /// </summary>
        public void LeaderActions()
        {
            if (_latestGossip.IsLeader(SelfUniqueAddress, SelfUniqueAddress))
            {
                // only run the leader actions if we are the LEADER
                if (!_isCurrentlyLeader)
                {
                    if (_cluster.IsInfoEnabled) { _cluster.Is_the_new_leader_among_reachable_nodes(); }
                    _isCurrentlyLeader = true;
                }
                const int firstNotice = 20;
                const int periodicNotice = 60;
                if (_latestGossip.Convergence(SelfUniqueAddress, _exitingConfirmed))
                {
                    if (_leaderActionCounter >= firstNotice)
                    {
                        if (_cluster.IsInfoEnabled) _cluster.LeaderCanPerformItsDutiesAgain();
                    }

                    _leaderActionCounter = 0;
                    LeaderActionsOnConvergence();
                }
                else
                {
                    _leaderActionCounter += 1;

                    if (_cluster.Settings.AllowWeaklyUpMembers && _leaderActionCounter >= 3)
                    {
                        MoveJoiningToWeaklyUp();
                    }

                    if (_leaderActionCounter == firstNotice || _leaderActionCounter % periodicNotice == 0)
                    {
                        if (_cluster.IsInfoEnabled) _cluster.LeaderCanCurrentlyNotPerformItsDuties(_latestGossip);
                    }
                }
            }
            else if (_isCurrentlyLeader)
            {
                if (_cluster.IsInfoEnabled) { _cluster.Is_no_longer_leader(); }
                _isCurrentlyLeader = false;
            }

            CleanupExitingConfirmed();
            ShutdownSelfWhenDown();
        }

        private void MoveJoiningToWeaklyUp()
        {
            var localGossip = _latestGossip;
            var localMembers = localGossip.Members;
            var enoughMembers = IsMinNrOfMembersFulfilled();

            bool IsJoiningToWeaklyUp(Member m) => m.Status == MemberStatus.Joining
                                                  && enoughMembers
                                                  && _latestGossip.ReachabilityExcludingDownedObservers.Value.IsReachable(m.UniqueAddress);

            var changedMembers = localMembers
                .Where(IsJoiningToWeaklyUp)
                .Select(m => m.Copy(MemberStatus.WeaklyUp))
                .ToImmutableSortedSet();

            if (!changedMembers.IsEmpty)
            {
                // replace changed members
                var newMembers = Member.PickNextTransition(localMembers, changedMembers);
                var newGossip = localGossip.Copy(members: newMembers);
                UpdateLatestGossip(newGossip);

                // log status change
                if (_cluster.IsInfoEnabled)
                {
                    foreach (var m in changedMembers)
                    {
                        _cluster.LeaderIsMovingNode(m);
                    }
                }

                Publish(newGossip);
                if (_cluster.Settings.PublishStatsInterval == TimeSpan.Zero) PublishInternalStats();
            }
        }

        private void ShutdownSelfWhenDown()
        {
            if (_latestGossip.GetMember(SelfUniqueAddress).Status == MemberStatus.Down)
            {
                // When all reachable have seen the state this member will shutdown itself when it has
                // status Down. The down commands should spread before we shutdown.
                var unreachable = _latestGossip.Overview.Reachability.AllUnreachableOrTerminated;
                var downed = _latestGossip.Members.Where(m => m.Status == MemberStatus.Down)
                    .Select(m => m.UniqueAddress).ToList();
                if (_selfDownCounter >= MaxTicksBeforeShuttingDownMyself || downed.All(node => unreachable.Contains(node) || _latestGossip.SeenByNode(node)))
                {
                    // the reason for not shutting down immediately is to give the gossip a chance to spread
                    // the downing information to other downed nodes, so that they can shutdown themselves
                    if (_cluster.IsInfoEnabled) _cluster.ShuttingDownMyself();
                    // not crucial to send gossip, but may speedup removal since fallback to failure detection is not needed
                    // if other downed know that this node has seen the version
                    SendGossipRandom(MaxGossipsBeforeShuttingDownMyself);
                    Shutdown();
                }
                else
                {
                    _selfDownCounter++;
                }
            }
        }

        /// <summary>
        /// If akka.cluster.min-rn-of-members or akka.cluster.roles.[rolename].min-nr-of-members is set,
        /// this function will check to see if that threshold is met.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the setting isn't enabled or is satisfied.
        /// <c>false</c> is the setting is enabled and unsatisfied.
        /// </returns>
        public bool IsMinNrOfMembersFulfilled()
        {
            var clusterSettings = _cluster.Settings;
            return _latestGossip.Members.Count >= clusterSettings.MinNrOfMembers
                && clusterSettings
                    .MinNrOfMembersOfRole
                    .All(x => _latestGossip.Members.Count(c => c.HasRole(x.Key)) >= x.Value);
        }

        /// <summary>
        /// Leader actions are as follows:
        /// 1. Move JOINING     => UP                   -- When a node joins the cluster
        /// 2. Move LEAVING     => EXITING              -- When all partition handoff has completed
        /// 3. Non-exiting remain                       -- When all partition handoff has completed
        /// 4. Move unreachable EXITING => REMOVED      -- When all nodes have seen the EXITING node as unreachable (convergence) -
        ///                                                 remove the node from the node ring and seen table
        /// 5. Move unreachable DOWN/EXITING => REMOVED -- When all nodes have seen that the node is DOWN/EXITING (convergence) -
        ///                                                 remove the node from the node ring and seen table
        /// 7. Updating the vclock version for the changes
        /// 8. Updating the `seen` table
        /// 9. Update the state with the new gossip
        /// </summary>
        public void LeaderActionsOnConvergence()
        {
            var localGossip = _latestGossip;
            var localMembers = localGossip.Members;
            var localOverview = localGossip.Overview;
            var localSeen = localOverview.Seen;

            bool enoughMembers = IsMinNrOfMembersFulfilled();
            bool IsJoiningUp(Member m) => (m.Status == MemberStatus.Joining || m.Status == MemberStatus.WeaklyUp) && enoughMembers;

            var removedUnreachable =
                localOverview.Reachability.AllUnreachableOrTerminated.Select(localGossip.GetMember)
                    .Where(m => Gossip.RemoveUnreachableWithMemberStatus.Contains(m.Status))
                    .ToImmutableHashSet(MemberComparer.Instance);

            var removedExitingConfirmed =
                _exitingConfirmed.Where(x => localGossip.GetMember(x).Status == MemberStatus.Exiting)
                .ToImmutableHashSet(UniqueAddressComparer.Instance);

            var upNumber = 0;
            var changedMembers = localMembers.Select(m =>
            {
                if (IsJoiningUp(m))
                {
                    // Move JOINING => UP (once all nodes have seen that this node is JOINING, i.e. we have a convergence)
                    // and minimum number of nodes have joined the cluster
                    if (upNumber == 0)
                    {
                        // It is alright to use same upNumber as already used by a removed member, since the upNumber
                        // is only used for comparing age of current cluster members (Member.isOlderThan)
                        var youngest = localGossip.YoungestMember;
                        upNumber = 1 + (youngest.UpNumber == int.MaxValue ? 0 : youngest.UpNumber);
                    }
                    else
                    {
                        upNumber += 1;
                    }
                    return m.CopyUp(upNumber);
                }

                if (m.Status == MemberStatus.Leaving)
                {
                    // Move LEAVING => EXITING (once we have a convergence on LEAVING
                    // *and* if we have a successful partition handoff)
                    return m.Copy(MemberStatus.Exiting);
                }

                return null;
            }).Where(m => m is object).ToImmutableSortedSet();

            if (!removedUnreachable.IsEmpty || !removedExitingConfirmed.IsEmpty || !changedMembers.IsEmpty)
            {
                // handle changes

                // replace changed members
                var newMembers = Member.PickNextTransition(changedMembers, localMembers)
                    .Except(removedUnreachable)
                    .Where(x => !removedExitingConfirmed.Contains(x.UniqueAddress))
                    .ToImmutableSortedSet();

                // removing REMOVED nodes from the `seen` table
                var removed = removedUnreachable.Select(u => u.UniqueAddress)
                    .ToImmutableHashSet(UniqueAddressComparer.Instance)
                    .Union(removedExitingConfirmed);
                var newSeen = localSeen.Except(removed);
                // removing REMOVED nodes from the `reachability` table
                var newReachability = localOverview.Reachability.Remove(removed);
                var newOverview = localOverview.Copy(seen: newSeen, reachability: newReachability);

                // Clear the VectorClock when member is removed. The change made by the leader is stamped
                // and will propagate as is if there are no other changes on other nodes.
                // If other concurrent changes on other nodes (e.g. join) the pruning is also
                // taken care of when receiving gossips.
                var newVersion = removed.Aggregate(localGossip.Version, (v, node) =>
                {
                    return v.Prune(VectorClock.Node.Create(VclockName(node)));
                });
                var newGossip = localGossip.Copy(members: newMembers, overview: newOverview, version: newVersion);

                var infoEnabled = _cluster.IsInfoEnabled;
                if (!_exitingTasksInProgress && newGossip.GetMember(SelfUniqueAddress).Status == MemberStatus.Exiting)
                {
                    // Leader is moving itself from Leaving to Exiting.
                    // ExitingCompleted will be received via CoordinatedShutdown to continue
                    // the leaving process. Meanwhile the gossip state is not marked as seen.

                    _exitingTasksInProgress = true;
                    if (_coordShutdown.ShutdownReason is null)
                    {
                        if (infoEnabled) _cluster.ExitingLeaderStartingCoordinatedShutdown();
                    }
                    _selfExiting.TrySetResult(Done.Instance);
                    _coordShutdown.Run(CoordinatedShutdown.ClusterLeavingReason.Instance);
                }

                UpdateLatestGossip(newGossip);
                _exitingConfirmed = new HashSet<UniqueAddress>(_exitingConfirmed.Except(removedExitingConfirmed), UniqueAddressComparer.Instance);

                if (infoEnabled)
                {
                    // log status changes
                    foreach (var m in changedMembers)
                    {
                        _cluster.LeaderIsMovingNode(m);
                    }

                    //log the removal of unreachable nodes
                    foreach (var m in removedUnreachable)
                    {
                        _cluster.LeaderIsRemovingNode(m);
                    }

                    foreach (var m in removedExitingConfirmed)
                    {
                        _cluster.LeaderIsRemovingConfirmedExitingNode(m);
                    }
                }

                Publish(_latestGossip);
                GossipExitingMembersToOldest(changedMembers.Where(i => i.Status == MemberStatus.Exiting));
            }
        }

        /// <summary>
        /// Gossip the Exiting change to the two oldest nodes for quick dissemination to potential Singleton nodes
        /// </summary>
        /// <param name="exitingMembers"></param>
        private void GossipExitingMembersToOldest(IEnumerable<Member> exitingMembers)
        {
            var targets = GossipTargetsForExitingMembers(_latestGossip, exitingMembers);
            if (targets is object && targets.Any())
            {
#if DEBUG
                if (_log.IsDebugEnabled)
                {
                    _log.Cluster_Node_Gossip_exiting_members_to_the_two_oldest(SelfUniqueAddress, exitingMembers, targets);
                }
#endif

                foreach (var m in targets)
                {
                    GossipTo(m.UniqueAddress);
                }
            }
        }

        /// <summary>
        /// Reaps the unreachable members according to the failure detector's verdict.
        /// </summary>
        public void ReapUnreachableMembers()
        {
            if (!IsSingletonCluster)
            {
                // only scrutinize if we are a non-singleton cluster

                var localGossip = _latestGossip;
                var localOverview = localGossip.Overview;
                var localMembers = localGossip.Members;

                var newlyDetectedUnreachableMembers =
                    localMembers.Where(member => !(
                        member.UniqueAddress.Equals(SelfUniqueAddress) ||
                        localOverview.Reachability.Status(SelfUniqueAddress, member.UniqueAddress) == Reachability.ReachabilityStatus.Unreachable ||
                        localOverview.Reachability.Status(SelfUniqueAddress, member.UniqueAddress) == Reachability.ReachabilityStatus.Terminated ||
                        _cluster.FailureDetector.IsAvailable(member.Address))).ToImmutableSortedSet();

                var newlyDetectedReachableMembers = localOverview.Reachability.AllUnreachableFrom(SelfUniqueAddress)
                        .Where(node => !node.Equals(SelfUniqueAddress) && _cluster.FailureDetector.IsAvailable(node.Address))
                        .Select(localGossip.GetMember).ToImmutableHashSet(MemberComparer.Instance);

                if (!newlyDetectedUnreachableMembers.IsEmpty || !newlyDetectedReachableMembers.IsEmpty)
                {
                    var newReachability1 = newlyDetectedUnreachableMembers.Aggregate(
                        localOverview.Reachability,
                        (reachability, m) => reachability.Unreachable(SelfUniqueAddress, m.UniqueAddress));

                    var newReachability2 = newlyDetectedReachableMembers.Aggregate(
                        newReachability1,
                        (reachability, m) => reachability.Reachable(SelfUniqueAddress, m.UniqueAddress));

                    if (!newReachability2.Equals(localOverview.Reachability))
                    {
                        var newOverview = localOverview.Copy(reachability: newReachability2);
                        var newGossip = localGossip.Copy(overview: newOverview);

                        UpdateLatestGossip(newGossip);

                        var partitioned = newlyDetectedUnreachableMembers.Partition(m => m.Status == MemberStatus.Exiting);
                        var exiting = partitioned.Item1;
                        var nonExiting = partitioned.Item2;

                        if (!nonExiting.IsEmpty && _log.IsWarningEnabled)
                        {
                            _log.MarkingNodesAsUNREACHABLE(_cluster, nonExiting);
                        }

                        if (_cluster.IsInfoEnabled)
                        {
                            if (!exiting.IsEmpty) { _cluster.MarkingExitingNodesAsUNREACHABLE(exiting); }

                            if (!newlyDetectedReachableMembers.IsEmpty) { _cluster.MarkingNodesAsREACHABLE(newlyDetectedReachableMembers); }
                        }

                        Publish(_latestGossip);
                    }
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="nodes">TBD</param>
        /// <returns>TBD</returns>
        public UniqueAddress SelectRandomNode(ImmutableList<UniqueAddress> nodes)
        {
            if (nodes.IsEmpty) return null;
            return nodes[ThreadLocalRandom.Current.Next(nodes.Count)];
        }

        /// <summary>
        /// Returns <c>true</c> if this is a one node cluster. <c>false</c> otherwise.
        /// </summary>
        public bool IsSingletonCluster
        {
            get { return _latestGossip.IsSingletonCluster; }
        }

        /// <summary>
        /// needed for tests
        /// </summary>
        /// <param name="address">TBD</param>
        public void SendGossipTo(Address address)
        {
            foreach (var m in _latestGossip.Members)
            {
                if (m.Address.Equals(address))
                    GossipTo(m.UniqueAddress);
            }
        }

        /// <summary>
        /// Gossips latest gossip to a node.
        /// </summary>
        /// <param name="node">The address of the node we want to send gossip to.</param>
        public void GossipTo(UniqueAddress node)
        {
            if (ValidNodeForGossip(node))
                ClusterCore(node.Address).Tell(new GossipEnvelope(SelfUniqueAddress, node, _latestGossip));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <param name="destination">TBD</param>
        public void GossipTo(UniqueAddress node, IActorRef destination)
        {
            if (ValidNodeForGossip(node))
                destination.Tell(new GossipEnvelope(SelfUniqueAddress, node, _latestGossip));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        public void GossipStatusTo(UniqueAddress node)
        {
            if (ValidNodeForGossip(node))
                ClusterCore(node.Address).Tell(new GossipStatus(SelfUniqueAddress, _latestGossip.Version));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <param name="destination">TBD</param>
        public void GossipStatusTo(UniqueAddress node, IActorRef destination)
        {
            if (ValidNodeForGossip(node))
                destination.Tell(new GossipStatus(SelfUniqueAddress, _latestGossip.Version));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public bool ValidNodeForGossip(UniqueAddress node)
        {
            return !node.Equals(SelfUniqueAddress) && _latestGossip.HasMember(node) &&
                    _latestGossip.ReachabilityExcludingDownedObservers.Value.IsReachable(node);
        }

        /// <summary>
        /// The Exiting change is gossiped to the two oldest nodes for quick dissemination to potential Singleton nodes
        /// </summary>
        /// <param name="latestGossip"></param>
        /// <param name="exitingMembers"></param>
        /// <returns></returns>
        public static IEnumerable<Member> GossipTargetsForExitingMembers(Gossip latestGossip, IEnumerable<Member> exitingMembers)
        {
            if (exitingMembers.Any())
            {
                var roles = exitingMembers.SelectMany(m => m.Roles);
                var membersSortedByAge = latestGossip.Members.OrderBy(m => m, Member.AgeOrdering);
                var targets = new HashSet<Member>();

                var t = membersSortedByAge.Take(2).ToArray(); // 2 oldest of all nodes
                targets.UnionWith(t);

                foreach (var role in roles)
                {
                    t = membersSortedByAge.Where(i => i.HasRole(role)).Take(2).ToArray(); // 2 oldest with the role
                    if (t.Length > 0)
                    {
                        targets.UnionWith(t);
                    }
                }

                return targets;
            }
            return null;
        }

        /// <summary>
        /// Updates the local gossip with the latest received from over the network.
        /// </summary>
        /// <param name="newGossip">The new gossip to merge with our own.</param>
        public void UpdateLatestGossip(Gossip newGossip)
        {
            // Updating the vclock version for the changes
            var versionedGossip = newGossip.Increment(_vclockNode);

            // Don't mark gossip state as seen while exiting is in progress, e.g.
            // shutting down singleton actors. This delays removal of the member until
            // the exiting tasks have been completed.
            if (_exitingTasksInProgress)
                _latestGossip = versionedGossip.ClearSeen();
            else
            {
                // Nobody else has seen this gossip but us
                var seenVersionedGossip = versionedGossip.OnlySeen(SelfUniqueAddress);

                // Update the state with the new gossip
                _latestGossip = seenVersionedGossip;
            }
            AssertLatestGossip();
        }

        /// <summary>
        /// Asserts that the gossip is valid and only contains information for current members of the cluster.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the VectorClock is corrupt and has not been pruned properly.</exception>
        public void AssertLatestGossip()
        {
            if (Cluster.IsAssertInvariantsEnabled && _latestGossip.Version.Versions.Count > _latestGossip.Members.Count)
            {
                ThrowHelper.ThrowInvalidOperationException_TooManyVectorClockEntriesInGossipState(_latestGossip);
            }
        }

        /// <summary>
        /// Publishes gossip to other nodes in the cluster.
        /// </summary>
        /// <param name="newGossip">The new gossip to share.</param>
        public void Publish(Gossip newGossip)
        {
            _publisher.Tell(new InternalClusterAction.PublishChanges(newGossip));
            if (_cluster.Settings.PublishStatsInterval == TimeSpan.Zero)
            {
                PublishInternalStats();
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void PublishInternalStats()
        {
            var vclockStats = new VectorClockStats(_latestGossip.Version.Versions.Count,
                _latestGossip.Members.Count(m => _latestGossip.SeenByNode(m.UniqueAddress)));

            _publisher.Tell(new ClusterEvent.CurrentInternalStats(_gossipStats, vclockStats));
        }

        private readonly ILoggingAdapter _log = Context.GetLogger();
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// Sends <see cref="InternalClusterAction.InitJoin"/> to all seed nodes (except itself) and expect
    /// <see cref="InternalClusterAction.InitJoinAck"/> reply back. The seed node that replied first
    /// will be used and joined to. <see cref="InternalClusterAction.InitJoinAck"/> replies received after
    /// the first one are ignored.
    ///
    /// Retries if no <see cref="InternalClusterAction.InitJoinAck"/> replies are received within the 
    /// <see cref="ClusterSettings.SeedNodeTimeout"/>. When at least one reply has been received it stops itself after
    /// an idle <see cref="ClusterSettings.SeedNodeTimeout"/>.
    ///
    /// The seed nodes can be started in any order, but they will not be "active" until they have been
    /// able to join another seed node (seed1.)
    ///
    /// They will retry the join procedure.
    ///
    /// Possible scenarios:
    ///  1. seed2 started, but doesn't get any ack from seed1 or seed3
    ///  2. seed3 started, doesn't get any ack from seed1 or seed3 (seed2 doesn't reply)
    ///  3. seed1 is started and joins itself
    ///  4. seed2 retries the join procedure and gets an ack from seed1, and then joins to seed1
    ///  5. seed3 retries the join procedure and gets acks from seed2 first, and then joins to seed2
    /// </summary>
    internal sealed class JoinSeedNodeProcess : UntypedActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly ImmutableList<Address> _seeds;
        private readonly Address _selfAddress;
        private int _attempts = 0;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="seeds">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when either the list of specified <paramref name="seeds"/> is empty
        /// or the first listed seed is a reference to the <see cref="IActorContext.System">IUntypedActorContext.System</see>'s address.
        /// </exception>
        public JoinSeedNodeProcess(ImmutableList<Address> seeds)
        {
            _selfAddress = Cluster.Get(Context.System).SelfAddress;
            _seeds = seeds;
            if (seeds.IsEmpty || seeds.Head() == _selfAddress)
                ThrowHelper.ThrowArgumentException_JoinSeedNodeShouldNotBeDone();
            Context.SetReceiveTimeout(Cluster.Get(Context.System).Settings.SeedNodeTimeout);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PreStart()
        {
            Self.Tell(InternalClusterAction.JoinSeenNode.Instance);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case InternalClusterAction.JoinSeenNode _:
                    var ctx = Context;
                    var ctxParentPath = ctx.Parent.Path;
                    // send InitJoin to all seed nodes (except myself)
                    foreach (var path in _seeds.Where(x => x != _selfAddress)
                                .Select(y => ctx.ActorSelection(ctxParentPath.ToStringWithAddress(y))))
                    {
                        path.Tell(InternalClusterAction.InitJoin.Instance);
                    }
                    _attempts++;
                    break;

                case InternalClusterAction.InitJoinAck initJoinAck:
                    var context = Context;
                    // first InitJoinAck reply
                    context.Parent.Tell(new ClusterUserAction.JoinTo(initJoinAck.Address));
                    context.Become(Done);
                    break;

                case InternalClusterAction.InitJoinNack _:
                    // that seed was uninitialized
                    break;

                case ReceiveTimeout _:
                    if (_attempts >= 2 && _log.IsWarningEnabled)
                    {
                        _log.CouldnotJoinSeedNodesAfterAttempts(_attempts, _seeds, _selfAddress);
                    }
                    // no InitJoinAck received - try again
                    Self.Tell(InternalClusterAction.JoinSeenNode.Instance);
                    break;

                default:
                    Unhandled(message);
                    break;
            }
        }

        private void Done(object message)
        {
            switch (message)
            {
                case InternalClusterAction.InitJoinAck _:
                    // already received one, skip the rest
                    break;

                case ReceiveTimeout _:
                    Context.Stop(Self);
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// Used only for the first seed node.
    /// Sends <see cref="InternalClusterAction.InitJoin"/> to all seed nodes except itself.
    /// If other seed nodes are not part of the cluster yet they will reply with
    /// <see cref="InternalClusterAction.InitJoinNack"/> or not respond at all and then the
    /// first seed node will join itself to initialize the new cluster. When the first seed
    /// node is restarted, and some other seed node is part of the cluster it will reply with
    /// <see cref="InternalClusterAction.InitJoinAck"/> and then the first seed node will
    /// join that other seed node to join the existing cluster.
    /// </summary>
    internal sealed class FirstSeedNodeProcess : UntypedActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private ImmutableList<Address> _remainingSeeds;
        private readonly Address _selfAddress;
        private readonly Cluster _cluster;
        private readonly Deadline _timeout;
        private readonly ICancelable _retryTaskToken;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="seeds">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when either the number of specified <paramref name="seeds"/> is less than or equal to 1
        /// or the first listed seed is a reference to the <see cref="IActorContext.System">IUntypedActorContext.System</see>'s address.
        /// </exception>
        public FirstSeedNodeProcess(ImmutableList<Address> seeds)
        {
            _cluster = Cluster.Get(Context.System);
            _selfAddress = _cluster.SelfAddress;

            if (seeds.Count <= 1 || seeds.Head() != _selfAddress)
                ThrowHelper.ThrowArgumentException_JoinSeedNodeShouldNotBeDone();

            _remainingSeeds = seeds.Remove(_selfAddress);
            _timeout = Deadline.Now + _cluster.Settings.SeedNodeTimeout;
            _retryTaskToken = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Self, InternalClusterAction.JoinSeenNode.Instance, Self);
            Self.Tell(InternalClusterAction.JoinSeenNode.Instance);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            _retryTaskToken.Cancel();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case InternalClusterAction.JoinSeenNode _:
                    if (_timeout.HasTimeLeft)
                    {
                        var ctx = Context;
                        var ctxParentPath = ctx.Parent.Path;
                        // send InitJoin to remaining seed nodes (except myself)
                        foreach (var seed in _remainingSeeds.Select(
                                    x => ctx.ActorSelection(ctxParentPath.ToStringWithAddress(x))))
                            seed.Tell(InternalClusterAction.InitJoin.Instance);
                    }
                    else
                    {
                        var ctx = Context;
                        // no InitJoinAck received, initialize new cluster by joining myself
                        ctx.Parent.Tell(new ClusterUserAction.JoinTo(_selfAddress));
                        ctx.Stop(Self);
                    }
                    break;

                case InternalClusterAction.InitJoinAck initJoinAck:
                    var context = Context;
                    // first InitJoinAck reply, join existing cluster
                    context.Parent.Tell(new ClusterUserAction.JoinTo(initJoinAck.Address));
                    context.Stop(Self);
                    break;

                case InternalClusterAction.InitJoinNack initJoinNack:
                    _remainingSeeds = _remainingSeeds.Remove(initJoinNack.Address);
                    if (_remainingSeeds.IsEmpty)
                    {
                        var ctx = Context;
                        // initialize new cluster by joining myself when nacks from all other seed nodes
                        ctx.Parent.Tell(new ClusterUserAction.JoinTo(_selfAddress));
                        ctx.Stop(Self);
                    }
                    break;

                default:
                    Unhandled(message);
                    break;
            }
        }
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    [MessagePackObject]
    internal sealed class GossipStats
    {
        [Key(0)]
        public readonly long ReceivedGossipCount;
        [Key(1)]
        public readonly long MergeCount;
        [Key(2)]
        public readonly long SameCount;
        [Key(3)]
        public readonly long NewerCount;
        [Key(4)]
        public readonly long OlderCount;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="receivedGossipCount">TBD</param>
        /// <param name="mergeCount">TBD</param>
        /// <param name="sameCount">TBD</param>
        /// <param name="newerCount">TBD</param>
        /// <param name="olderCount">TBD</param>
        [SerializationConstructor]
        public GossipStats(long receivedGossipCount = 0L,
            long mergeCount = 0L,
            long sameCount = 0L,
            long newerCount = 0L, long olderCount = 0L)
        {
            ReceivedGossipCount = receivedGossipCount;
            MergeCount = mergeCount;
            SameCount = sameCount;
            NewerCount = newerCount;
            OlderCount = olderCount;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public GossipStats IncrementMergeCount()
        {
            return Copy(mergeCount: MergeCount + 1, receivedGossipCount: ReceivedGossipCount + 1);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public GossipStats IncrementSameCount()
        {
            return Copy(sameCount: SameCount + 1, receivedGossipCount: ReceivedGossipCount + 1);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public GossipStats IncrementNewerCount()
        {
            return Copy(newerCount: NewerCount + 1, receivedGossipCount: ReceivedGossipCount + 1);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public GossipStats IncrementOlderCount()
        {
            return Copy(olderCount: OlderCount + 1, receivedGossipCount: ReceivedGossipCount + 1);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="receivedGossipCount">TBD</param>
        /// <param name="mergeCount">TBD</param>
        /// <param name="sameCount">TBD</param>
        /// <param name="newerCount">TBD</param>
        /// <param name="olderCount">TBD</param>
        /// <returns>TBD</returns>
        public GossipStats Copy(long? receivedGossipCount = null,
            long? mergeCount = null,
            long? sameCount = null,
            long? newerCount = null, long? olderCount = null)
        {
            return new GossipStats(receivedGossipCount ?? ReceivedGossipCount,
                mergeCount ?? MergeCount,
                sameCount ?? SameCount,
                newerCount ?? NewerCount,
                olderCount ?? OlderCount);
        }

        #region Operator overloads

        /// <summary>
        /// Combines two statistics together to create new statistics.
        /// </summary>
        /// <param name="a">The first set of statistics to combine.</param>
        /// <param name="b">The second statistics to combine.</param>
        /// <returns>A new <see cref="GossipStats"/> that is a combination of the two specified statistics.</returns>
        public static GossipStats operator +(GossipStats a, GossipStats b)
        {
            return new GossipStats(a.ReceivedGossipCount + b.ReceivedGossipCount,
                a.MergeCount + b.MergeCount,
                a.SameCount + b.SameCount,
                a.NewerCount + b.NewerCount,
                a.OlderCount + b.OlderCount);
        }

        /// <summary>
        /// Decrements the first set of statistics, <paramref name="a"/>, using the second set of statistics, <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The set of statistics to decrement.</param>
        /// <param name="b">The set of statistics used to decrement.</param>
        /// <returns>A new <see cref="GossipStats"/> that is calculated by decrementing <paramref name="a"/> by <paramref name="b"/>.</returns>
        public static GossipStats operator -(GossipStats a, GossipStats b)
        {
            return new GossipStats(a.ReceivedGossipCount - b.ReceivedGossipCount,
                a.MergeCount - b.MergeCount,
                a.SameCount - b.SameCount,
                a.NewerCount - b.NewerCount,
                a.OlderCount - b.OlderCount);
        }

        #endregion
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// The supplied callback will be run once when the current cluster member has the same status.
    /// </summary>
    internal sealed class OnMemberStatusChangedListener : ReceiveActorSlim
    {
        private readonly Action _callback;
        private readonly MemberStatus _status;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly Cluster _cluster;

        private Type To
        {
            get
            {
                switch (_status)
                {
                    case MemberStatus.Up:
                        return typeof(ClusterEvent.MemberUp);
                    case MemberStatus.Removed:
                        return typeof(ClusterEvent.MemberRemoved);
                    default:
                        ThrowHelper.ThrowArgumentException_ExpectedUpOrRemovedInOnMemberStatusChangedListener(_status); return null;
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="callback">TBD</param>
        /// <param name="targetStatus">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="targetStatus"/> is invalid.
        /// Acceptable values are: <see cref="MemberStatus.Up"/> | <see cref="MemberStatus.Down"/>.
        /// </exception>
        public OnMemberStatusChangedListener(Action callback, MemberStatus targetStatus)
        {
            _callback = callback;
            _status = targetStatus;
            _cluster = Cluster.Get(Context.System);

            Receive<ClusterEvent.CurrentClusterState>(e => HandleCurrentClusterState(e));

            Receive<ClusterEvent.MemberUp>(e => HandleMemberUp(e));

            Receive<ClusterEvent.MemberRemoved>(e => HandleMemberRemoved(e));
        }

        private void HandleCurrentClusterState(ClusterEvent.CurrentClusterState state)
        {
            if (state.Members.Any(IsTriggered)) { Done(); }
        }

        private void HandleMemberUp(ClusterEvent.MemberUp up)
        {
            if (IsTriggered(up.Member)) { Done(); }
        }

        private void HandleMemberRemoved(ClusterEvent.MemberRemoved removed)
        {
            if (IsTriggered(removed.Member)) { Done(); }
        }

        /// <inheritdoc cref="ActorBase.PreStart"/>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the current status neither <see cref="MemberStatus.Up"/> or <see cref="MemberStatus.Down"/>.
        /// </exception>
        protected override void PreStart()
        {
            _cluster.Subscribe(Self, To);
        }

        /// <inheritdoc cref="ActorBase.PostStop"/>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the current status neither <see cref="MemberStatus.Up"/> or <see cref="MemberStatus.Down"/>.
        /// </exception>
        protected override void PostStop()
        {
            // execute MemberRemoved hooks if we are shutting down
            if (_status == MemberStatus.Removed)
                Done();
            _cluster.Unsubscribe(Self);
        }

        private void Done()
        {
            try
            {
                _callback.Invoke();
            }
            catch (Exception ex)
            {
                _log.CallbackFailedWith(ex, To);
            }
            finally
            {
                Context.Stop(Self);
            }
        }

        private bool IsTriggered(Member m)
        {
            return m.UniqueAddress == _cluster.SelfUniqueAddress && m.Status == _status;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal sealed class VectorClockStats
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="versionSize">TBD</param>
        /// <param name="seenLatest">TBD</param>
        [SerializationConstructor]
        public VectorClockStats(int versionSize = 0, int seenLatest = 0)
        {
            VersionSize = versionSize;
            SeenLatest = seenLatest;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public int VersionSize { get; }
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public int SeenLatest { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is VectorClockStats other)
            {
                return VersionSize == other.VersionSize && SeenLatest == other.SeenLatest;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (VersionSize * 397) ^ SeenLatest;
            }
        }
    }
}
