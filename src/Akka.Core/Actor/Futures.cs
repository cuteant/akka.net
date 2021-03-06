﻿//-----------------------------------------------------------------------
// <copyright file="Futures.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor.Internal;
using Akka.Dispatch.SysMsg;
using Akka.Util;
using Akka.Util.Internal;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Akka.Actor
{
    /// <summary>
    ///     Extension method class designed to create Ask support for
    ///     non-ActorRef objects such as <see cref="ActorSelection" />.
    /// </summary>
    public static class Futures
    {
        //when asking from outside of an actor, we need to pass a system, so the FutureActor can register itself there and be resolvable for local and remote calls
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="self">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        public static Task<object> Ask(this ICanTell self, object message, TimeSpan? timeout = null)
        {
            return self.Ask<object>(message, timeout, CancellationToken.None);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="self">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="cancellationToken">TBD</param>
        /// <returns>TBD</returns>
        public static Task<object> Ask(this ICanTell self, object message, CancellationToken cancellationToken)
        {
            return self.Ask<object>(message, null, cancellationToken);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="self">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="cancellationToken">TBD</param>
        /// <returns>TBD</returns>
        public static Task<object> Ask(this ICanTell self, object message, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            return self.Ask<object>(message, timeout, cancellationToken);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="self">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        public static Task<T> Ask<T>(this ICanTell self, object message, TimeSpan? timeout = null)
        {
            return self.Ask<T>(message, timeout, CancellationToken.None);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="self">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="cancellationToken">TBD</param>
        /// <returns>TBD</returns>
        public static Task<T> Ask<T>(this ICanTell self, object message, CancellationToken cancellationToken)
        {
            return self.Ask<T>(message, null, cancellationToken);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="self">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="cancellationToken">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the system can't resolve the target provider.
        /// </exception>
        /// <returns>TBD</returns>
        public static Task<T> Ask<T>(this ICanTell self, object message, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            return Ask<T>(self, _ => message, timeout, cancellationToken);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="self">TBD</param>
        /// <param name="messageFactory">Factory method that creates a message that can encapsulate the 'Sender' IActorRef</param>
        /// <param name="timeout">TBD</param>
        /// <param name="cancellationToken">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the system can't resolve the target provider.
        /// </exception>
        /// <returns>TBD</returns>
        public static async Task<T> Ask<T>(this ICanTell self, Func<IActorRef, object> messageFactory, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            await SynchronizationContextManager.RemoveContext;

            IActorRefProvider provider = ResolveProvider(self);
            if (provider is null) AkkaThrowHelper.ThrowArgumentException(AkkaExceptionResource.Argument_Futures_Ask, AkkaExceptionArgument.self);

            return (T)await Ask(self, messageFactory, provider, timeout, cancellationToken);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="self">TBD</param>
        /// <returns>TBD</returns>
        internal static IActorRefProvider ResolveProvider(ICanTell self)
        {
            if (ActorCell.Current is object)
                return InternalCurrentActorCellKeeper.Current.SystemImpl.Provider;

            switch (self)
            {
                case IInternalActorRef actorRef:
                    return actorRef.Provider;

                case ActorSelection actorSel:
                    return ResolveProvider(actorSel.Anchor);

                default:
                    return null;
            }
        }

        private static async Task<object> Ask(ICanTell self, Func<IActorRef, object> messageFactory, IActorRefProvider provider,
            TimeSpan? timeout, CancellationToken cancellationToken)
        {
            TaskCompletionSource<object> result = TaskEx.NonBlockingTaskCompletionSource<object>();

            CancellationTokenSource timeoutCancellation = null;
            timeout = timeout ?? provider.Settings.AskTimeout;
            var ctrList = new List<CancellationTokenRegistration>(2);

            if (timeout != Timeout.InfiniteTimeSpan && timeout.Value > default(TimeSpan))
            {
                timeoutCancellation = new CancellationTokenSource();

                ctrList.Add(timeoutCancellation.Token.Register(() =>
                {
                    result.TrySetException(new AskTimeoutException($"Timeout after {timeout} seconds"));
                }));

                timeoutCancellation.CancelAfter(timeout.Value);
            }

            if (cancellationToken.CanBeCanceled)
            {
                ctrList.Add(cancellationToken.Register(() => result.TrySetCanceled()));
            }

            //create a new tempcontainer path
            ActorPath path = provider.TempPath();

            var future = new FutureActorRef(result, () => { }, path);
            //The future actor needs to be registered in the temp container
            provider.RegisterTempActor(future, path);
            var message = messageFactory(future);
            self.Tell(message, future);

            try
            {
                return await result.Task;
            }
            finally
            {
                //callback to unregister from tempcontainer

                provider.UnregisterTempActor(path);

                for (var i = 0; i < ctrList.Count; i++)
                {
                    ctrList[i].Dispose();
                }

                if (timeoutCancellation is object)
                {
                    timeoutCancellation.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Akka private optimized representation of the temporary actor spawned to
    /// receive the reply to an "ask" operation.
    /// 
    /// INTERNAL API
    /// </summary>
    internal sealed class PromiseActorRef : MinimalActorRef
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<PromiseActorRef>();

        /// <summary>
        /// Can't access constructor directly - use <see cref="Apply"/> instead.
        /// </summary>
        private PromiseActorRef(IActorRefProvider provider, TaskCompletionSource<object> promise, string mcn)
        {
            _provider = provider;
            _promise = promise;
            _mcn = mcn;
        }

        private readonly IActorRefProvider _provider;
        private readonly TaskCompletionSource<object> _promise;

        /// <summary>
        /// The result of the promise being completed.
        /// </summary>
        public Task<object> Result => _promise.Task;

        /// <summary>
        /// This is necessary for weaving the PromiseActorRef into the asked message, i.e. the replyTo pattern.
        /// </summary>
        private readonly string _mcn;

        #region Internal states


        /**
           * As an optimization for the common (local) case we only register this PromiseActorRef
           * with the provider when the `path` member is actually queried, which happens during
           * serialization (but also during a simple call to `ToString`, `Equals` or `GetHashCode`!).
           *
           * Defined states:
           * null                  => started, path not yet created
           * Registering           => currently creating temp path and registering it
           * path: ActorPath       => path is available and was registered
           * StoppedWithPath(path) => stopped, path available
           * Stopped               => stopped, path not yet created
           */
        private AtomicReference<object> _stateDoNotCallMeDirectly = new AtomicReference<object>(null);

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class Registering : ISingletonMessage
        {
            private Registering() { }
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Registering Instance = new Registering();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class Stopped : ISingletonMessage
        {
            private Stopped() { }
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Stopped Instance = new Stopped();
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        internal sealed class StoppedWithPath : IEquatable<StoppedWithPath>
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="path">TBD</param>
            [SerializationConstructor]
            public StoppedWithPath(ActorPath path)
            {
                Path = path;
            }

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public ActorPath Path { get; private set; }

            #region Equality

            /// <inheritdoc/>
            public bool Equals(StoppedWithPath other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Path, other.Path);
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is StoppedWithPath stoppedWithPath && Equals(stoppedWithPath);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return (Path is object ? Path.GetHashCode() : 0);
            }

            #endregion
        }

        #endregion

        #region Static methods

        private static readonly Status.Failure ActorStopResult = new Status.Failure(new ActorKilledException("Stopped"));

        // use a static delegate to avoid allocations
        private static readonly Action<object> CancelAction = o => ((TaskCompletionSource<object>)o).TrySetCanceled();

        /// <summary>
        /// Creates a new <see cref="PromiseActorRef"/>
        /// </summary>
        /// <param name="provider">The current actor ref provider.</param>
        /// <param name="timeout">The timeout on the promise.</param>
        /// <param name="targetName">The target of the object / actor</param>
        /// <param name="messageClassName">The name of the message class.</param>
        /// <param name="sender">The actor sending the message via promise.</param>
        /// <returns>A new <see cref="PromiseActorRef"/></returns>
        public static PromiseActorRef Apply(IActorRefProvider provider, TimeSpan timeout, object targetName,
            string messageClassName, IActorRef sender = null)
        {
            sender = sender ?? ActorRefs.NoSender;
            var result = new TaskCompletionSource<object>();
            var a = new PromiseActorRef(provider, result, messageClassName);
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.Token.Register(CancelAction, result);
            cancellationSource.CancelAfter(timeout);

            //var scheduler = provider.Guardian.Underlying.System.Scheduler.Advanced;
            //var c = new Cancelable(scheduler, timeout);
            //scheduler.ScheduleOnce(timeout, () => result.TrySetResult(new Status.Failure(new AskTimeoutException(
            //    string.Format("Ask timed out on [{0}] after [{1} ms]. Sender[{2}] sent message of type {3}.", targetName, timeout.TotalMilliseconds, sender, messageClassName)))),
            //    c);

            result.Task.LinkOutcome(InvokeStopAction, a, TaskContinuationOptions.ExecuteSynchronously);

            return a;
        }

        private static readonly Action<Task<object>, PromiseActorRef> InvokeStopAction = (t, s) => InvokeStop(s);
        private static void InvokeStop(/*Task<object> task, */PromiseActorRef a) => a.Stop();

        #endregion

        //TODO: ActorCell.emptyActorRefSet ?
        // Aaronontheweb: using the ImmutableHashSet.Empty for now
        private readonly AtomicReference<ImmutableHashSet<IActorRef>> _watchedByDoNotCallMeDirectly = new AtomicReference<ImmutableHashSet<IActorRef>>(ImmutableHashSet<IActorRef>.Empty);

        private ImmutableHashSet<IActorRef> WatchedBy
        {
            get { return _watchedByDoNotCallMeDirectly.Value; }
        }

        private bool UpdateWatchedBy(ImmutableHashSet<IActorRef> oldWatchedBy, ImmutableHashSet<IActorRef> newWatchedBy)
        {
            return _watchedByDoNotCallMeDirectly.CompareAndSet(oldWatchedBy, newWatchedBy);
        }

        public override IActorRefProvider Provider
        {
            get { return _provider; }
        }

        /// <summary>
        /// Returns false if the <see cref="_promise"/> is already completed.
        /// </summary>
        private bool AddWatcher(IActorRef watcher)
        {
            if (WatchedBy.Contains(watcher))
            {
                return false;
            }
            return UpdateWatchedBy(WatchedBy, WatchedBy.Add(watcher)) || AddWatcher(watcher);
        }

        private void RemoveWatcher(IActorRef watcher)
        {
            if (!WatchedBy.Contains(watcher))
            {
                return;
            }
            if (!UpdateWatchedBy(WatchedBy, WatchedBy.Remove(watcher))) RemoveWatcher(watcher);
        }

        private ImmutableHashSet<IActorRef> ClearWatchers()
        {
            //TODO: ActorCell.emptyActorRefSet ?
            if (WatchedBy is null || WatchedBy.IsEmpty) return ImmutableHashSet<IActorRef>.Empty;
            if (!UpdateWatchedBy(WatchedBy, null)) return ClearWatchers();
            else return WatchedBy;
        }

        private object State
        {
            get { return _stateDoNotCallMeDirectly.Value; }
            set { _stateDoNotCallMeDirectly.Value = value; }
        }

        private bool UpdateState(object oldState, object newState)
        {
            return _stateDoNotCallMeDirectly.CompareAndSet(oldState, newState);
        }

        /// <inheritdoc cref="InternalActorRefBase.Parent"/>
        public override IInternalActorRef Parent
        {
            get { return Provider.TempContainer; }
        }


        /// <inheritdoc cref="InternalActorRefBase"/>
        public override ActorPath Path
        {
            get { return GetPath(); }
        }

        /// <summary>
        ///  Contract of this method:
        ///  Must always return the same ActorPath, which must have
        ///  been registered if we haven't been stopped yet.
        /// </summary>
        private ActorPath GetPath()
        {
            while (true)
            {
                switch (State)
                {
                    case null:
                        if (UpdateState(null, Registering.Instance))
                        {
                            ActorPath p = null;
                            try
                            {
                                p = Provider.TempPath();
                                Provider.RegisterTempActor(this, p);
                                return p;
                            }
                            finally
                            {
                                State = p;
                            }
                        }
                        continue;

                    case ActorPath actorPath:
                        return actorPath;
                    case StoppedWithPath stoppedWithPath:
                        return stoppedWithPath.Path;
                    case Stopped _:
                        //even if we are already stopped we still need to produce a proper path
                        UpdateState(Stopped.Instance, new StoppedWithPath(Provider.TempPath()));
                        continue;
                    case Registering _:
                        continue;
                    default:
                        break;
                }
            }
        }

        /// <inheritdoc cref="ActorRefBase.TellInternal">InternalActorRefBase.TellInternal</inheritdoc>
        protected override void TellInternal(object message, IActorRef sender)
        {
            switch (State)
            {
                case Stopped _:
                case StoppedWithPath _:
                    Provider.DeadLetters.Tell(message);
                    break;

                default:
                    if (message is null) AkkaThrowHelper.ThrowInvalidMessageException(AkkaExceptionResource.InvalidMessage_MsgIsNull);
                    // @Aaronontheweb note: not using any of the Status stuff here. Seems like it's extraneous in CLR
                    //var wrappedMessage = message;
                    //if (!(message is Status.Success || message is Status.Failure))
                    //{
                    //    wrappedMessage = new Status.Success(message);
                    //}
                    if (!(_promise.TrySetResult(message))) { Provider.DeadLetters.Tell(message); }
                    break;
            }
        }

        /// <inheritdoc cref="InternalActorRefBase.SendSystemMessage(ISystemMessage)"/>
        public override void SendSystemMessage(ISystemMessage message)
        {
            switch (message)
            {
                case Terminate _:
                    Stop();
                    break;
                case DeathWatchNotification dw:
                    Tell(new Terminated(dw.Actor, dw.ExistenceConfirmed, dw.AddressTerminated), this);
                    break;
                case Watch watch:
                    if (Equals(watch.Watchee, this))
                    {
                        if (!AddWatcher(watch.Watcher))
                        {
                            // ➡➡➡ NEVER SEND THE SAME SYSTEM MESSAGE OBJECT TO TWO ACTORS ⬅⬅⬅
                            watch.Watcher.SendSystemMessage(new DeathWatchNotification(watch.Watchee, existenceConfirmed: true,
                                addressTerminated: false));
                        }
                        else
                        {
                            //TODO: find a way to get access to logger?
                            s_logger.LogWarning("BUG: illegal Watch({0},{1}) for {2}", watch.Watchee, watch.Watcher, this);
                        }
                    }
                    break;
                case Unwatch unwatch:
                    if (Equals(unwatch.Watchee, this) && !Equals(unwatch.Watcher, this))
                    {
                        RemoveWatcher(unwatch.Watcher);
                    }
                    else
                    {
                        s_logger.LogWarning("BUG: illegal Unwatch({0},{1}) for {2}", unwatch.Watchee, unwatch.Watcher, this);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override void Stop()
        {
            while (true)
            {
                var state = State;
                switch (state)
                {
                    // if path was never queried nobody can possibly be watching us, so we don't have to publish termination either
                    case null:
                        if (UpdateState(null, Stopped.Instance))
                        {
                            StopEnsureCompleted();
                        }
                        else
                        {
                            continue;
                        }
                        break;

                    case ActorPath p:
                        if (UpdateState(p, new StoppedWithPath(p)))
                        {
                            try
                            {
                                StopEnsureCompleted();
                            }
                            finally
                            {
                                Provider.UnregisterTempActor(p);
                            }
                        }
                        else
                        {
                            continue;
                        }
                        break;

                    //spin until registration is completed before stopping
                    case Registering _:
                        continue;

                    //already stopped
                    case Stopped _:
                    case StoppedWithPath _:
                    default:
                        break;
                }

                break;
            }
        }

        private void StopEnsureCompleted()
        {
            _promise.TrySetResult(ActorStopResult);
            var watchers = ClearWatchers();
            if (watchers.Count > 0)
            {
                foreach (var watcher in watchers)
                {
                    watcher.AsInstanceOf<IInternalActorRef>()
                        .SendSystemMessage(new DeathWatchNotification(watcher, existenceConfirmed: true,
                            addressTerminated: false));
                }
            }
        }
    }
}
