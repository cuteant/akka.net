using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;

namespace Akka.Persistence
{
    /// <summary>Persistent actor - can be used to implement command or eventsourcing.</summary>
    public abstract class ReceivePersistentActor2 : Eventsourced, IInitializableActor
    {
        private const string BehaviorKey = "RECEIVE";

        private readonly Stack<PatternMatchBuilder> _matchCommandBuilders = new Stack<PatternMatchBuilder>();
        private readonly Stack<PatternMatchBuilder> _matchRecoverBuilders = new Stack<PatternMatchBuilder>();
        private Action<object> _partialReceiveCommand = PatternMatch<object>.EmptyAction;
        private Action<object> _partialReceiveRecover = PatternMatch<object>.EmptyAction;
        private bool _hasBeenInitialized;

        protected readonly PatternMatchBuilder DefaultCommandPatterns;
        protected readonly PatternMatchBuilder DefaultRecoverPatterns;

        /// <summary>TBD</summary>
        protected ReceivePersistentActor2()
        {
            DefaultCommandPatterns = new PatternMatchBuilder();
            _matchCommandBuilders.Push(DefaultCommandPatterns);
            DefaultRecoverPatterns = new PatternMatchBuilder();
            _matchRecoverBuilders.Push(DefaultRecoverPatterns);
        }

        void IInitializableActor.Init()
        {
            //This might be called directly after the constructor, or when the same actor instance has been returned
            //during recreate. Make sure what happens here is idempotent
            if (!_hasBeenInitialized)	//Do not perform this when "recreating" the same instance
            {
                _partialReceiveCommand = BuildNewReceiveHandler(_matchCommandBuilders.Pop());
                _partialReceiveRecover = BuildNewReceiveHandler(_matchRecoverBuilders.Pop());
                _hasBeenInitialized = true;
            }
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
            if (patterns is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.patterns); }

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
            _matchCommandBuilders.Push(new PatternMatchBuilder());
            configure();
            return _matchCommandBuilders.Pop();
        }

        /// <inheritdoc/>
        protected override bool Receive(object message)
        {
            return ReceiveCommand(message);
        }

        /// <inheritdoc/>
        protected sealed override bool ReceiveCommand(object message)
        {
            _partialReceiveCommand(message);
            return true;
        }

        /// <inheritdoc/>
        protected sealed override bool ReceiveRecover(object message)
        {
            _partialReceiveRecover(message);
            return true;
        }

        private Action<T> WrapAsyncHandler<T>(Func<T, Task> asyncHandler)
        {
            void wrapAsyncHandler(T m)
            {
                RunTask(asyncHandler, m);
            }
            return wrapAsyncHandler;
        }

        #region Recover helper methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMayConfigureRecoverHandlers()
        {
            if (_matchRecoverBuilders.Count <= 0) ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_Recover_methods);
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="handler">TBD</param>
        /// <param name="shouldHandle">TBD</param>
        protected void Recover<T>(Action<T> handler, Predicate<T> shouldHandle = null)
        {
            EnsureMayConfigureRecoverHandlers();
            if (shouldHandle is null)
            {
                _matchRecoverBuilders.Peek().Match<T>(handler);
            }
            else
            {
                _matchRecoverBuilders.Peek().Match<T>(handler, shouldHandle);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="shouldHandle">TBD</param>
        /// <param name="handler">TBD</param>
        protected void Recover<T>(Predicate<T> shouldHandle, Action<T> handler)
        {
            Recover<T>(handler, shouldHandle);
        }

        /// <summary>TBD</summary>
        /// <param name="handler">TBD</param>
        protected void RecoverAny(Action<object> handler)
        {
            EnsureMayConfigureRecoverHandlers();
            _matchRecoverBuilders.Peek().MatchAny(handler);
        }

        #endregion

        #region Command helper methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMayConfigureCommandHandlers()
        {
            if (_matchCommandBuilders.Count <= 0) ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_Command_methods);
        }

        /// <summary>
        /// Registers an asynchronous handler for incoming command of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes, including the <see cref="Eventsourced.Persist{TEvent}(TEvent, Action{TEvent})" /> and
        /// <see cref="Eventsourced.PersistAll{TEvent}(IEnumerable{TEvent}, Action{TEvent})" /> calls.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already.
        /// In that case, this handler will not be invoked.</remarks>
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        protected void CommandAsync<T>(Func<T, Task> handler, Predicate<T> shouldHandle = null)
        {
            Command(WrapAsyncHandler(handler), shouldHandle);
        }

        /// <summary>
        /// Registers an asynchronous handler for incoming command of the specified type <typeparamref name="T"/>.
        /// If <paramref name="shouldHandle"/>!=<c>null</c> then it must return true before a message is passed to <paramref name="handler"/>.
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes, including the <see cref="Eventsourced.Persist{TEvent}(TEvent, Action{TEvent})" /> and
        /// <see cref="Eventsourced.PersistAll{TEvent}(IEnumerable{TEvent}, Action{TEvent})" /> calls.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already.
        /// In that case, this handler will not be invoked.</remarks>
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="shouldHandle">When not <c>null</c> it is used to determine if the message matches.</param>
        /// <param name="handler">The message handler that is invoked for incoming messages of the specified type <typeparamref name="T"/></param>
        protected void CommandAsync<T>(Predicate<T> shouldHandle, Func<T, Task> handler)
        {
            Command(shouldHandle, WrapAsyncHandler(handler));
        }

        /// <summary>
        /// Registers an asynchronous handler for incoming command of any type.
        /// <remarks>The actor will be suspended until the task returned by <paramref name="handler"/> completes, including the <see cref="Eventsourced.Persist{TEvent}(TEvent, Action{TEvent})" /> and
        /// <see cref="Eventsourced.PersistAll{TEvent}(IEnumerable{TEvent}, Action{TEvent})" /> calls.</remarks>
        /// <remarks>This method may only be called when constructing the actor or from <see cref="Become(System.Action)"/> or <see cref="BecomeStacked(Action)"/>.</remarks>
        /// <remarks>Note that handlers registered prior to this may have handled the message already.
        /// In that case, this handler will not be invoked.</remarks>
        /// </summary>
        /// <param name="handler">The message handler that is invoked for incoming messages of any type</param>
        protected void CommandAnyAsync(Func<object, Task> handler)
        {
            CommandAny(WrapAsyncHandler(handler));
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="handler">TBD</param>
        /// <param name="shouldHandle">TBD</param>
        protected void Command<T>(Action<T> handler, Predicate<T> shouldHandle = null)
        {
            EnsureMayConfigureCommandHandlers();
            if (shouldHandle is null)
            {
                _matchCommandBuilders.Peek().Match<T>(handler);
            }
            else
            {
                _matchCommandBuilders.Peek().Match<T>(handler, shouldHandle);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="shouldHandle">TBD</param>
        /// <param name="handler">TBD</param>
        protected void Command<T>(Predicate<T> shouldHandle, Action<T> handler)
        {
            Command<T>(handler, shouldHandle);
        }

        /// <summary>TBD</summary>
        /// <param name="handler">TBD</param>
        protected void CommandAny(Action<object> handler)
        {
            EnsureMayConfigureCommandHandlers();
            _matchCommandBuilders.Peek().MatchAny(handler);
        }

        #endregion
    }
}
