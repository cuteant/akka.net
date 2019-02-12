using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Actor.Internal;
using Akka.Dispatch;

namespace Akka.Actor
{
    /// <summary>TBD</summary>
    public abstract class ReceiveActor2 : ActorBase, IInitializableActor
    {
        private const string BehaviorKey = "RECEIVE";

        private readonly Stack<PatternMatchBuilder> _matchHandlerBuilders = new Stack<PatternMatchBuilder>();
        private Action<object> _partialReceive = PatternMatch<object>.EmptyAction;
        private bool _hasBeenInitialized;

        protected readonly PatternMatchBuilder DefaultPatterns;

        /// <summary>TBD</summary>
        protected ReceiveActor2()
        {
            DefaultPatterns = new PatternMatchBuilder();
            PrepareConfigureMessageHandlers(DefaultPatterns);
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

        /// <inheritdoc />
        protected sealed override bool Receive(object message)
        {
            _partialReceive(message);
            return true;
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.</summary>
        /// <param name="configure">Configures the new handler by calling the different Receive overloads.</param>
        protected PatternMatchBuilder Become(Action configure)
        {
            var patterns = ConfigurePatterns(configure);
            Become(patterns);
            return patterns;
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.</summary>
        protected void Become(PatternMatchBuilder patterns)
        {
            Receive receiveFunc = GetBehavior(patterns);
            base.Become(receiveFunc);
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.
        /// The current handler is stored on a stack, and you can revert to it by calling <see cref="ActorBase.UnbecomeStacked"/>.</summary>
        /// <remarks>Please note, that in order to not leak memory, make sure every call to <see cref="BecomeStacked(Action)"/>
        /// is matched with a call to <see cref="ActorBase.UnbecomeStacked"/>.</remarks>
        /// <param name="configure">Configures the new handler by calling the different Receive overloads.</param>
        protected PatternMatchBuilder BecomeStacked(Action configure)
        {
            var patterns = ConfigurePatterns(configure);
            BecomeStacked(patterns);
            return patterns;
        }

        /// <summary>Changes the actor's behavior and replaces the current receive handler with the specified handler.
        /// The current handler is stored on a stack, and you can revert to it by calling <see cref="ActorBase.UnbecomeStacked"/>.</summary>
        /// <remarks>Please note, that in order to not leak memory, make sure every call to <see cref="BecomeStacked(Action)"/>
        /// is matched with a call to <see cref="ActorBase.UnbecomeStacked"/>.</remarks>
        protected void BecomeStacked(PatternMatchBuilder patterns)
        {
            Receive receiveFunc = GetBehavior(patterns);
            base.BecomeStacked(receiveFunc);
        }

        private Receive GetBehavior(PatternMatchBuilder patterns)
        {
            if (null == patterns) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.patterns); }

            if (patterns.Properties.TryGetValue(BehaviorKey, out var behavior))
            {
                return (Receive)behavior;
            }

            var newHandler = BuildNewReceiveHandler(patterns);
            bool LocalReceive(object message)
            {
                newHandler(message);
                return true;
            }
            Receive receiveFunc = LocalReceive;

            patterns.Properties[BehaviorKey] = receiveFunc;
            return receiveFunc;
        }

        private Action<object> BuildNewReceiveHandler(PatternMatchBuilder matchBuilder)
        {
            matchBuilder.TryMatchAny(Unhandled);
            return matchBuilder.Build();
        }

        /// <summary>TBD</summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        protected PatternMatchBuilder ConfigurePatterns(Action configure)
        {
            PrepareConfigureMessageHandlers();
            configure();
            return _matchHandlerBuilders.Pop();
        }

        /// <summary>Creates and pushes a new MatchBuilder</summary>
        private void PrepareConfigureMessageHandlers(PatternMatchBuilder patterns = null)
        {
            _matchHandlerBuilders.Push(patterns ?? new PatternMatchBuilder());
        }

        private Action<T> WrapAsyncHandler<T>(Func<T, Task> asyncHandler)
        {
            void WrapRunTask(T m)
            {
                ActorTaskScheduler.RunTask(asyncHandler, m);
            }
            return new Action<T>(WrapRunTask);
        }

        /// <summary>Registers a handler for incoming messages of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.</summary>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        /// <exception cref="InvalidOperationException">This exception is thrown if this method is called outside of the actor's constructor or from <see cref="Become(Action)"/>.</exception>
        protected void Receive<T>(Action<T> handler, Predicate<T> shouldHandle = null)
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

        /// <summary>Registers a handler for incoming messages of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.</summary>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        /// <exception cref="InvalidOperationException">This exception is thrown if this method is called outside of the actor's constructor or from <see cref="Become(Action)"/>.</exception>
        protected void Receive<T>(Predicate<T> shouldHandle, Action<T> handler)
        {
            Receive<T>(handler, shouldHandle);
        }

        /// <summary>Registers a handler for incoming messages of any type.</summary>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// <param name="handler">The message handler that is invoked for all</param>
        /// <exception cref="InvalidOperationException">This exception is thrown if this method is called outside of the actor's constructor or from <see cref="Become(Action)"/>.</exception>
        protected void ReceiveAny(Action<object> handler)
        {
            EnsureMayConfigureMessageHandlers();
            _matchHandlerBuilders.Peek().MatchAny(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMayConfigureMessageHandlers()
        {
            if (_matchHandlerBuilders.Count <= 0) { AkkaThrowHelper.ThrowInvalidOperationException(AkkaExceptionResource.InvalidOperation_ReceiveActor_Ensure); }
        }

        /// <summary>
        /// Registers an asynchronous handler for incoming messages of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        protected void ReceiveAsync<T>(Func<T, Task> handler, Predicate<T> shouldHandle = null)
        {
            Receive(WrapAsyncHandler(handler), shouldHandle);
        }

        /// <summary>
        /// Registers an asynchronous handler for incoming messages of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        protected void ReceiveAsync<T>(Predicate<T> shouldHandle, Func<T, Task> handler)
        {
            Receive(WrapAsyncHandler(handler), shouldHandle);
        }

        /// <summary>
        /// Registers an asynchronous handler for incoming messages of any type.
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already. 
        /// In that case, this handler will not be invoked.</remarks>
        /// </summary>
        /// <param name="handler">The message handler that is invoked for all</param>
        protected void ReceiveAnyAsync(Func<object, Task> handler)
        {
            ReceiveAny(WrapAsyncHandler(handler));
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
