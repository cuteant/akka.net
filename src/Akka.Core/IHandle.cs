using System;

namespace Akka
{
    /// <summary>Interface IHandle</summary>
    /// <typeparam name="TMessage">The type of the t message.</typeparam>
    public interface IHandle<in TMessage>
    {
        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        void Handle(TMessage message);
    }

    /// <summary>Interface IHandle</summary>
    /// <typeparam name="TMessage1">The type of the t message.</typeparam>
    /// <typeparam name="TMessage2">The type of the t message.</typeparam>
    public interface IHandle<in TMessage1, in TMessage2>
    {
        /// <summary>Handles the specified message.</summary>
        /// <param name="message1">The message.</param>
        /// <param name="message2">The message.</param>
        void Handle(TMessage1 message1, TMessage2 message2);
    }

    /// <summary>TBD</summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class ActionHandler<TMessage> : IHandle<TMessage>
    {
        public readonly Action<TMessage> _handler;

        /// <summary>TBD</summary>
        /// <param name="handler"></param>
        public ActionHandler(Action<TMessage> handler)
        {
            if (handler is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.handler); }
            _handler = handler;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        public void Handle(TMessage message) => _handler(message);
    }

    /// <summary>TBD</summary>
    /// <typeparam name="TMessage1">The type of the t message.</typeparam>
    /// <typeparam name="TMessage2">The type of the t message.</typeparam>
    public sealed class ActionHandler<TMessage1, TMessage2> : IHandle<TMessage1, TMessage2>
    {
        public readonly Action<TMessage1, TMessage2> _handler;

        /// <summary>TBD</summary>
        /// <param name="handler"></param>
        public ActionHandler(Action<TMessage1, TMessage2> handler)
        {
            if (handler is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.handler); }
            _handler = handler;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message1">The message.</param>
        /// <param name="message2">The message.</param>
        public void Handle(TMessage1 message1, TMessage2 message2) => _handler(message1, message2);
    }
}
