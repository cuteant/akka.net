//-----------------------------------------------------------------------
// <copyright file="OldestChangedBuffer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Cluster.Tools.Singleton
{
    /// <summary>
    /// Notifications of member events that track oldest member is tunneled
    /// via this actor (child of ClusterSingletonManager) to be able to deliver
    /// one change at a time. Avoiding simultaneous changes simplifies
    /// the process in ClusterSingletonManager. ClusterSingletonManager requests
    /// next event with <see cref="GetNext"/> when it is ready for it. Only one outstanding
    /// <see cref="GetNext"/> request is allowed. Incoming events are buffered and delivered
    /// upon <see cref="GetNext"/> request.
    /// </summary>
    internal sealed class OldestChangedBuffer : UntypedActor
    {
        #region Internal messages

        /// <summary>
        /// TBD
        /// </summary>
        public sealed class GetNext : ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly GetNext Instance = new GetNext();
            private GetNext() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class InitialOldestState
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public UniqueAddress Oldest { get; }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public bool SafeToBeOldest { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="oldest">TBD</param>
            /// <param name="safeToBeOldest">TBD</param>
            [SerializationConstructor]
            public InitialOldestState(UniqueAddress oldest, bool safeToBeOldest)
            {
                Oldest = oldest;
                SafeToBeOldest = safeToBeOldest;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class OldestChanged
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public UniqueAddress Oldest { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="oldest">TBD</param>
            [SerializationConstructor]
            public OldestChanged(UniqueAddress oldest)
            {
                Oldest = oldest;
            }
        }

        #endregion

        private readonly CoordinatedShutdown _coordShutdown = CoordinatedShutdown.Get(Context.System);

        /// <summary>
        /// Creates a new instance of the <see cref="OldestChangedBuffer"/>.
        /// </summary>
        /// <param name="role">The role for which we're watching for membership changes.</param>
        public OldestChangedBuffer(string role)
        {
            _role = role;
            _onDeliverNextAction = OnDeliverNext;

            SetupCoordinatedShutdown();
        }

        /// <summary>
        /// It's a delicate difference between <see cref="CoordinatedShutdown.PhaseClusterExiting"/> and <see cref="ClusterEvent.MemberExited"/>.
        ///
        /// MemberExited event is published immediately (leader may have performed that transition on other node),
        /// and that will trigger run of <see cref="CoordinatedShutdown"/>, while PhaseClusterExiting will happen later.
        /// Using PhaseClusterExiting in the singleton because the graceful shutdown of sharding region
        /// should preferably complete before stopping the singleton sharding coordinator on same node.
        /// </summary>
        private void SetupCoordinatedShutdown()
        {
            _coordShutdown.AddTask(CoordinatedShutdown.PhaseClusterExiting, "singleton-exiting-1", InvokeSingletonExiting1Func, this, Self);
        }

        private static readonly Func<OldestChangedBuffer, IActorRef, Task<Done>> InvokeSingletonExiting1Func = InvokeSingletonExiting1;
        private static Task<Done> InvokeSingletonExiting1(OldestChangedBuffer owner, IActorRef self)
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

        private readonly string _role;
        private ImmutableSortedSet<Member> _membersByAge = ImmutableSortedSet<Member>.Empty.WithComparer(MemberAgeOrdering.Descending);
        private ImmutableQueue<object> _changes = ImmutableQueue<object>.Empty;

        private readonly Cluster _cluster = Cluster.Get(Context.System);

        private void TrackChanges(Action block)
        {
            var before = _membersByAge.FirstOrDefault();
            block();
            var after = _membersByAge.FirstOrDefault();

            // todo: fix neq comparison
            if (!Equals(before, after))
                _changes = _changes.Enqueue(new OldestChanged(after?.UniqueAddress));
        }

        private bool MatchingRole(Member member)
        {
            return string.IsNullOrEmpty(_role) || member.HasRole(_role);
        }

        private void HandleInitial(ClusterEvent.CurrentClusterState state)
        {
            _membersByAge = state.Members
                .Where(m => (m.Status == MemberStatus.Up) && MatchingRole(m))
                .ToImmutableSortedSet(MemberAgeOrdering.Descending);
            // If there is some removal in progress of an older node it's not safe to immediately become oldest,
            // removal of younger nodes doesn't matter. Note that it can also be started via restart after
            // ClusterSingletonManagerIsStuck.

            int selfUpNumber = state.Members.Where(m => m.UniqueAddress == _cluster.SelfUniqueAddress).Select(m => (int?)m.UpNumber).FirstOrDefault() ?? int.MaxValue;

            var safeToBeOldest = !state.Members.Any(m => (m.UpNumber < selfUpNumber && MatchingRole(m)) && (m.Status == MemberStatus.Down || m.Status == MemberStatus.Exiting || m.Status == MemberStatus.Leaving));
            var initial = new InitialOldestState(_membersByAge.FirstOrDefault()?.UniqueAddress, safeToBeOldest);
            _changes = _changes.Enqueue(initial);
        }

        private void Add(Member member)
        {
            if (MatchingRole(member))
                TrackChanges(() =>
                {
                    // replace, it's possible that the upNumber is changed
                    _membersByAge = _membersByAge.Remove(member);
                    _membersByAge = _membersByAge.Add(member);
                });
        }

        private void Remove(Member member)
        {
            if (MatchingRole(member))
                TrackChanges(() => _membersByAge = _membersByAge.Remove(member));
        }

        private void SendFirstChange()
        {
            // don't send cluster change events if this node is shutting its self down, just wait for SelfExiting
            if (!_cluster.IsTerminated)
            {
                _changes = _changes.Dequeue(out var change);
                Context.Parent.Tell(change);
            }
        }

        /// <inheritdoc cref="ActorBase.PreStart"/>
        protected override void PreStart()
        {
            _cluster.Subscribe(Self, typeof(ClusterEvent.IMemberEvent));
        }

        /// <inheritdoc cref="ActorBase.PostStop"/>
        protected override void PostStop()
        {
            _cluster.Unsubscribe(Self);
        }

        /// <inheritdoc cref="UntypedActor.OnReceive"/>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case ClusterEvent.CurrentClusterState state:
                    HandleInitial(state);
                    break;

                case ClusterEvent.MemberUp up:
                    Add(up.Member);
                    break;

                case ClusterEvent.MemberRemoved removed:
                    Remove(removed.Member);
                    break;

                case ClusterEvent.MemberExited exited when (!exited.Member.UniqueAddress.Equals(_cluster.SelfUniqueAddress)):
                    Remove(exited.Member);
                    break;

                case SelfExiting _:
                    Remove(_cluster.ReadView.Self);
                    Sender.Tell(Done.Instance);
                    break;

                case GetNext _:
                    if (_changes.IsEmpty)
                    {
                        Context.BecomeStacked(_onDeliverNextAction);
                    }
                    else
                    {
                        SendFirstChange();
                    }
                    break;

                default:
                    Unhandled(message);
                    break;
            }
        }

        private readonly UntypedReceive _onDeliverNextAction;
        /// <summary>
        /// The buffer was empty when GetNext was received, deliver next event immediately.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        private void OnDeliverNext(object message)
        {
            switch (message)
            {
                case ClusterEvent.CurrentClusterState state:
                    HandleInitial(state);
                    SendFirstChange();
                    Context.UnbecomeStacked();
                    break;

                case ClusterEvent.MemberUp memberUp:
                    Add(memberUp.Member);
                    DeliverChanges();
                    break;

                case ClusterEvent.MemberRemoved removed:
                    Remove(removed.Member);
                    DeliverChanges();
                    break;

                case ClusterEvent.MemberExited exited when exited.Member.UniqueAddress != _cluster.SelfUniqueAddress:
                    Remove(exited.Member);
                    DeliverChanges();
                    break;

                case SelfExiting _:
                    Remove(_cluster.ReadView.Self);
                    DeliverChanges();
                    Sender.Tell(Done.Instance); // reply to ask
                    break;

                default:
                    Unhandled(message);
                    break;
            }
        }

        private void DeliverChanges()
        {
            if (!_changes.IsEmpty)
            {
                SendFirstChange();
                Context.UnbecomeStacked();
            }
        }

        /// <inheritdoc cref="ActorBase.Unhandled"/>
        protected override void Unhandled(object message)
        {
            if (message is ClusterEvent.IMemberEvent)
            {
                // ok, silence
            }
            else
            {
                base.Unhandled(message);
            }
        }
    }
}
