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

    /// <summary>TBD</summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class ActionHandler<TMessage> : IHandle<TMessage>
    {
        public readonly Action<TMessage> _handler;

        /// <summary>TBD</summary>
        /// <param name="handler"></param>
        public ActionHandler(Action<TMessage> handler)
        {
            if (null == handler) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.handler); }
            _handler = handler;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        public void Handle(TMessage message) => _handler(message);
    }
}
