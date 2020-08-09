//-----------------------------------------------------------------------
// <copyright file="ActorCell.DeathWatch.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Util;

namespace Akka.Actor
{
    partial class ActorCell
    {
        private IActorState _state = new DefaultActorState();

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef Watch(IActorRef subject)
        {
            var a = (IInternalActorRef)subject;

            if (!a.Equals(Self) && !WatchingContains(a))
            {
                MaintainAddressTerminatedSubscription(_watchAction, a);
            }
            return a;
        }

        private readonly Action<IInternalActorRef> _watchAction;
        private void InvokeWatch(IInternalActorRef a)
        {
            a.SendSystemMessage(new Watch(a, _self)); // ➡➡➡ NEVER SEND THE SAME SYSTEM MESSAGE OBJECT TO TWO ACTORS
            _state = _state.AddWatching(a, Option<object>.None);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subject">TBD</param>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef WatchWith(IActorRef subject, object message)
        {
            if (message == null)
            {
                AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.message, AkkaExceptionResource.ArgumentNull_WatchWithMsg);
            }

            var a = (IInternalActorRef)subject;

            if (!a.Equals(Self) && !WatchingContains(a))
            {
                MaintainAddressTerminatedSubscription(a, message);
            }
            return a;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef Unwatch(IActorRef subject)
        {
            var a = (IInternalActorRef)subject;
            if (!a.Equals(Self) && WatchingContains(a))
            {
                a.SendSystemMessage(new Unwatch(a, _self));
                MaintainAddressTerminatedSubscription(_unwatchAction, subject); // a
            }
            (_state, _) = _state.RemoveTerminated(a);
            return a;
        }

        private readonly Action<IActorRef> _unwatchAction;
        private void InvokeUnwatch(IActorRef a)
        {
            _state = _state.RemoveWatching(a);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="t">TBD</param>
        protected void ReceivedTerminated(Terminated t)
        {
            if (!_state.ContainsTerminated(t.ActorRef)) { return; }

            Option<object> customTerminatedMessage;
            (_state, customTerminatedMessage) = _state.RemoveTerminated(t.ActorRef); // here we know that it is the SAME ref which was put in
            ReceiveMessage(customTerminatedMessage.GetOrElse(t));
        }

        /// <summary>
        /// When this actor is watching the subject of <see cref="Terminated"/> message
        /// it will be propagated to user's receive.
        /// </summary>
        /// <param name="actor">TBD</param>
        /// <param name="existenceConfirmed">TBD</param>
        /// <param name="addressTerminated">TBD</param>
        protected void WatchedActorTerminated(IActorRef actor, bool existenceConfirmed, bool addressTerminated)
        {
            if (TryGetWatching(actor, out var message)) // message is custom termination message that was requested
            {
                MaintainAddressTerminatedSubscription(_unwatchAction, actor);
                if (!IsTerminating)
                {
                    // Unwatch could be called somewhere there inbetween here and the actual delivery of the custom message
                    Self.Tell(new Terminated(actor, existenceConfirmed, addressTerminated), actor);
                    TerminatedQueuedFor(actor, message);
                }
            }
            if (ChildrenContainer.Contains(actor))
            {
                HandleChildTerminated(actor);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subject">Tracked subject</param>
        /// <param name="customMessage">Terminated custom message</param>
        public void TerminatedQueuedFor(IActorRef subject, Option<object> customMessage) => _state = _state.AddTerminated(subject, customMessage);

        private bool WatchingContains(IActorRef subject) => _state.ContainsWatching(subject);

        private bool TryGetWatching(IActorRef subject, out Option<object> message) => _state.TryGetWatching(subject, out message);

        /// <summary>
        /// TBD
        /// </summary>
        protected void TellWatchersWeDied()
        {
            var watchedBy = _state
                .GetWatchedBy()
                .ToList();

            if (0u >= (uint)watchedBy.Count) return;
            try
            {
                // Don't need to send to parent parent since it receives a DWN by default

                /*
                * It is important to notify the remote watchers first, otherwise RemoteDaemon might shut down, causing
                * the remoting to shut down as well. At this point Terminated messages to remote watchers are no longer
                * deliverable.
                *
                * The problematic case is:
                *  1. Terminated is sent to RemoteDaemon
                *   1a. RemoteDaemon is fast enough to notify the terminator actor in RemoteActorRefProvider
                *   1b. The terminator is fast enough to enqueue the shutdown command in the remoting
                *  2. Only at this point is the Terminated (to be sent remotely) enqueued in the mailbox of remoting
                *
                * If the remote watchers are notified first, then the mailbox of the Remoting will guarantee the correct order.
                */
                foreach (var w in watchedBy) SendTerminated(false, (IInternalActorRef)w);
                foreach (var w in watchedBy) SendTerminated(true, (IInternalActorRef)w);
            }
            finally
            {
                MaintainAddressTerminatedSubscription(() =>
                {
                    _state = _state.ClearWatchedBy();
                });
            }
        }

        private void SendTerminated(bool ifLocal, IInternalActorRef watcher)
        {
            if (watcher.IsLocal == ifLocal && !watcher.Equals(Parent))
            {
                watcher.SendSystemMessage(new DeathWatchNotification(Self, true, false));
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actor">TBD</param>
        protected void UnwatchWatchedActors(ActorBase actor)
        {
            var watching = _state
                .GetWatching()
                .ToList();

            if (0u >= (uint)watching.Count) return;

            MaintainAddressTerminatedSubscription(_unwatchWatchedActorsAciton, arg1: watching, change: null);
        }

        private readonly Action<List<IActorRef>> _unwatchWatchedActorsAciton;
        private void InvokeUnwatchWatchedActors(List<IActorRef> watching)
        {
            try
            {
                // ➡➡➡ NEVER SEND THE SAME SYSTEM MESSAGE OBJECT TO TWO ACTORS
                foreach (var watchee in watching.OfType<IInternalActorRef>())
                {
                    watchee.SendSystemMessage(new Unwatch(watchee, _self));
                }
            }
            finally
            {
                _state = _state.ClearWatching();
                _state = _state.ClearTerminated();
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="watchee">TBD</param>
        /// <param name="watcher">TBD</param>
        protected void AddWatcher(IActorRef watchee, IActorRef watcher)
        {
            var watcheeSelf = watchee.Equals(Self);
            var watcherSelf = watcher.Equals(Self);

            if (watcheeSelf && !watcherSelf)
            {
                if (!_state.ContainsWatchedBy(watcher)) MaintainAddressTerminatedSubscription(_addWatcherAction, watcher);
            }
            else if (!watcheeSelf && watcherSelf)
            {
                Watch(watchee);
            }
            else
            {
                Publish(new Warning(Self.Path.ToString(), Actor.GetType(), string.Format("BUG: illegal Watch({0},{1} for {2}", watchee, watcher, Self)));
            }
        }

        private readonly Action<IActorRef> _addWatcherAction;
        private void InvokeAddWatcher(IActorRef watcher)
        {
            _state = _state.AddWatchedBy(watcher);

            if (System.Settings.DebugLifecycle) Publish(new Debug(Self.Path.ToString(), Actor.GetType(), string.Format("now watched by {0}", watcher)));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="watchee">TBD</param>
        /// <param name="watcher">TBD</param>
        protected void RemWatcher(IActorRef watchee, IActorRef watcher)
        {
            var watcheeSelf = watchee.Equals(Self);
            var watcherSelf = watcher.Equals(Self);

            if (watcheeSelf && !watcherSelf)
            {
                if (_state.ContainsWatchedBy(watcher)) MaintainAddressTerminatedSubscription(_remWatcherAction, watcher);
            }
            else if (!watcheeSelf && watcherSelf)
            {
                Unwatch(watchee);
            }
            else
            {
                Publish(new Warning(Self.Path.ToString(), Actor.GetType(), string.Format("BUG: illegal Unwatch({0},{1} for {2}", watchee, watcher, Self)));
            }
        }

        private readonly Action<IActorRef> _remWatcherAction;
        private void InvokeRemWatcher(IActorRef watcher)
        {
            _state = _state.RemoveWatchedBy(watcher);

            if (System.Settings.DebugLifecycle) Publish(new Debug(Self.Path.ToString(), Actor.GetType(), string.Format("no longer watched by {0}", watcher)));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        protected void AddressTerminated(Address address)
        {
            // cleanup watchedBy since we know they are dead
            MaintainAddressTerminatedSubscription(_addressTerminated, arg1: address, change: null);

            // send DeathWatchNotification to self for all matching subjects
            // that are not child with existenceConfirmed = false because we could have been watching a
            // non-local ActorRef that had never resolved before the other node went down
            // When a parent is watching a child and it terminates due to AddressTerminated
            // it is removed by sending DeathWatchNotification with existenceConfirmed = true to support
            // immediate creation of child with same name.
            foreach (var a in _state.GetWatching().Where(a => a.Path.Address == address))
            {
                ((IInternalActorRef)Self).SendSystemMessage(new DeathWatchNotification(a, true /*TODO: childrenRefs.getByRef(a).isDefined*/, true));
            }
        }

        private readonly Action<Address> _addressTerminated;
        private void InvokeAddressTerminated(Address address)
        {
            foreach (var a in _state.GetWatchedBy().Where(a => a.Path.Address == address).ToList())
            {
                //_watchedBy.Remove(a);
                _state = _state.RemoveWatchedBy(a);
            }
        }

        #region ** MaintainAddressTerminatedSubscription **

        /// <summary>
        /// Starts subscription to AddressTerminated if not already subscribing and the
        /// block adds a non-local ref to watching or watchedBy.
        /// Ends subscription to AddressTerminated if subscribing and the
        /// block removes the last non-local ref from watching and watchedBy.
        /// </summary>
        /// <param name="block">TBD</param>
        /// <param name="change">TBD</param>
        private void MaintainAddressTerminatedSubscription(Action block, IActorRef change = null)
        {
            if (IsNonLocal(change))
            {
                var had = HasNonLocalAddress();
                block();
                var has = HasNonLocalAddress();

                if (had && !has)
                    UnsubscribeAddressTerminated();
                else if (!had && has)
                    SubscribeAddressTerminated();
            }
            else
            {
                block();
            }
        }

        private void MaintainAddressTerminatedSubscription<TActorRef>(Action<TActorRef> block, TActorRef change)
            where TActorRef : IActorRef
        {
            if (IsNonLocal(change))
            {
                var had = HasNonLocalAddress();
                block(change);
                var has = HasNonLocalAddress();

                if (had && !has)
                    UnsubscribeAddressTerminated();
                else if (!had && has)
                    SubscribeAddressTerminated();
            }
            else
            {
                block(change);
            }
        }

        private void MaintainAddressTerminatedSubscription<T1>(Action<T1> block, T1 arg1, IActorRef change)
        {
            if (IsNonLocal(change))
            {
                var had = HasNonLocalAddress();
                block(arg1);
                var has = HasNonLocalAddress();

                if (had && !has)
                    UnsubscribeAddressTerminated();
                else if (!had && has)
                    SubscribeAddressTerminated();
            }
            else
            {
                block(arg1);
            }
        }

        private void MaintainAddressTerminatedSubscription(IInternalActorRef change, object message)
        {
            void LocalBlock()
            {
                change.SendSystemMessage(new Watch(change, _self)); // ➡➡➡ NEVER SEND THE SAME SYSTEM MESSAGE OBJECT TO TWO ACTORS
                _state = _state.AddWatching(change, message);
            }

            if (IsNonLocal(change))
            {
                var had = HasNonLocalAddress();
                LocalBlock();
                var has = HasNonLocalAddress();

                if (had && !has)
                    UnsubscribeAddressTerminated();
                else if (!had && has)
                    SubscribeAddressTerminated();
            }
            else
            {
                LocalBlock();
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonLocal(IActorRef @ref)
        {
            if (@ref == null) { return true; }

            return @ref is IInternalActorRef a && !a.IsLocal;
        }

        private bool HasNonLocalAddress()
        {
            var watching = _state.GetWatching();
            var watchedBy = _state.GetWatchedBy();
            return watching.Any(IsNonLocal) || watchedBy.Any(IsNonLocal);
        }

        private void UnsubscribeAddressTerminated() => AddressTerminatedTopic.Get(System).Unsubscribe(Self);

        private void SubscribeAddressTerminated() => AddressTerminatedTopic.Get(System).Subscribe(Self);
    }
}
