using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Tools.MatchHandler;

namespace Akka.Actor
{
    /// <summary>TBD</summary>
    public abstract class AsyncReceiveActor2 : ActorBase, IInitializableActor
    {
        private static readonly Task s_completedTask = Akka.Util.Internal.TaskEx.CompletedTask;
        private static readonly Func<object, Task> s_emptyReceive;

        static AsyncReceiveActor2()
        {
            s_completedTask = Akka.Util.Internal.TaskEx.CompletedTask;
            s_emptyReceive = _ => s_completedTask;
        }

        private readonly Stack<SimpleMatchBuilder<object, Task>> _matchHandlerBuilders = new Stack<SimpleMatchBuilder<object, Task>>();
        private Func<object, Task> _partialReceive = s_emptyReceive;
        private bool _hasBeenInitialized;

        protected readonly SimpleMatchBuilder<object, Task> DefaultPatterns;

        /// <summary>TBD</summary>
        protected AsyncReceiveActor2()
        {
            DefaultPatterns = ConfigureDefaultPatterns();
            if (DefaultPatterns != null)
            {
                _hasBeenInitialized = true;
            }
            else
            {
                DefaultPatterns = new SimpleMatchBuilder<object, Task>();
                PrepareConfigureMessageHandlers(DefaultPatterns);
            }
        }

        void IInitializableActor.Init()
        {
            //This might be called directly after the constructor, or when the same actor instance has been returned
            //during recreate. Make sure what happens here is idempotent
            if (!_hasBeenInitialized)	//Do not perform this when "recreating" the same instance
            {
                _partialReceive = BuildNewReceiveHandler(_matchHandlerBuilders.Pop());
                _hasBeenInitialized = true;
            }
        }

        protected virtual SimpleMatchBuilder<object, Task> ConfigureDefaultPatterns() => null;

        /// <inheritdoc />
        protected sealed override bool Receive(object message)
        {
            ActorTaskScheduler.RunTask(_partialReceive, message);
            return true;
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.</summary>
        /// <param name="configure">Configures the new handler by calling the different Receive overloads.</param>
        protected SimpleMatchBuilder<object, Task> Become(Action configure)
        {
            var patterns = ConfigurePatterns(configure);
            Become(patterns);
            return patterns;
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.</summary>
        protected void Become(SimpleMatchBuilder<object, Task> patterns)
        {
            var newHandler = BuildNewReceiveHandler(patterns);
            bool LocalReceive(object message)
            {
                ActorTaskScheduler.RunTask(newHandler, message);
                return true;
            }
            base.Become(LocalReceive);
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.
        /// The current handler is stored on a stack, and you can revert to it by calling <see cref="ActorBase.UnbecomeStacked"/>.</summary>
        /// <remarks>Please note, that in order to not leak memory, make sure every call to <see cref="BecomeStacked(Action)"/>
        /// is matched with a call to <see cref="ActorBase.UnbecomeStacked"/>.</remarks>
        /// <param name="configure">Configures the new handler by calling the different Receive overloads.</param>
        protected SimpleMatchBuilder<object, Task> BecomeStacked(Action configure)
        {
            var patterns = ConfigurePatterns(configure);
            BecomeStacked(patterns);
            return patterns;
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.
        /// The current handler is stored on a stack, and you can revert to it by calling <see cref="ActorBase.UnbecomeStacked"/>.</summary>
        protected void BecomeStacked(SimpleMatchBuilder<object, Task> patterns)
        {
            var newHandler = BuildNewReceiveHandler(patterns);
            bool LocalReceive(object message)
            {
                ActorTaskScheduler.RunTask(newHandler, message);
                return true;
            }
            base.BecomeStacked(LocalReceive);
        }

        private Func<object, Task> BuildNewReceiveHandler(SimpleMatchBuilder<object, Task> matchBuilder)
        {
            matchBuilder.TryMatchAny(UnhandledAsync);
            return matchBuilder.Build();
        }

        private Task UnhandledAsync(object message)
        {
            Unhandled(message);
            return s_completedTask;
        }

        private SimpleMatchBuilder<object, Task> ConfigurePatterns(Action configure)
        {
            PrepareConfigureMessageHandlers();
            configure();
            return _matchHandlerBuilders.Pop();
        }

        /// <summary>Creates and pushes a new MatchBuilder</summary>
        private void PrepareConfigureMessageHandlers(SimpleMatchBuilder<object, Task> patterns = null)
        {
            _matchHandlerBuilders.Push(patterns ?? new SimpleMatchBuilder<object, Task>());
        }

        /// <summary>Registers an asynchronous handler for incoming messages of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.</summary>
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        protected void ReceiveAsync<T>(Func<T, Task> handler, Predicate<T> shouldHandle = null)
        {
            EnsureMayConfigureMessageHandlers();
            if (shouldHandle == null)
            {
                _matchHandlerBuilders.Peek().Match<T>(handler);
            }
            else
            {
                _matchHandlerBuilders.Peek().Match<T>(handler, shouldHandle);
            }
        }

        /// <summary>Registers an asynchronous handler for incoming messages of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.</summary>
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        protected void ReceiveAsync<T>(Predicate<T> shouldHandle, Func<T, Task> handler)
        {
            ReceiveAsync(handler, shouldHandle);
        }

        /// <summary>Registers an asynchronous handler for incoming messages of any type.</summary>
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// <param name="handler">The message handler that is invoked for all</param>
        protected void ReceiveAnyAsync(Func<object, Task> handler)
        {
            EnsureMayConfigureMessageHandlers();
            _matchHandlerBuilders.Peek().MatchAny(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMayConfigureMessageHandlers()
        {
            if (_matchHandlerBuilders.Count <= 0) AkkaThrowHelper.ThrowInvalidOperationException(AkkaExceptionResource.InvalidOperation_ReceiveActor_Ensure);
        }

        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        protected void RunTask(Action action)
        {
            ActorTaskScheduler.RunTask(action);
        }

        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        /// <param name="state"></param>
        protected void RunTask(Action<object> action, object state)
        {
            ActorTaskScheduler.RunTask(action, state);
        }

        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        protected void RunTask(Func<Task> action)
        {
            ActorTaskScheduler.RunTask(action);
        }

        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        /// <param name="state"></param>
        protected void RunTask(Func<object, Task> action, object state)
        {
            ActorTaskScheduler.RunTask(action, state);
        }
    }
}
