﻿//-----------------------------------------------------------------------
// <copyright file="DequeWrapperMessageQueue.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Akka.Actor;

namespace Akka.Dispatch.MessageQueues
{
    /// <summary>
    /// Message queue for supporting <see cref="IDequeBasedMessageQueueSemantics"/> within <see cref="Mailbox"/> instances.
    /// 
    /// Uses a <see cref="Stack{Envelope}"/> internally - each individual <see cref="EnqueueFirst"/>
    /// </summary>
    public class DequeWrapperMessageQueue : IMessageQueue, IDequeBasedMessageQueueSemantics
    {
        // doesn't need to be threadsafe - only called from within actor
#if NETCOREAPP || NETSTANDARD_2_0_GREATER
        private readonly Stack<Envelope> _prependBuffer = new Stack<Envelope>();
#else
        private readonly CuteAnt.Collections.StackX<Envelope> _prependBuffer = new CuteAnt.Collections.StackX<Envelope>();
#endif

        /// <summary>
        /// The underlying <see cref="IMessageQueue"/>.
        /// </summary>
        protected readonly IMessageQueue MessageQueue;

        /// <summary>
        /// Takes another <see cref="IMessageQueue"/> as an argument - wraps <paramref name="messageQueue"/>
        /// in order to provide it with prepend (<see cref="EnqueueFirst"/>) semantics.
        /// </summary>
        /// <param name="messageQueue">The underlying message queue wrapped by this one.</param>
        public DequeWrapperMessageQueue(IMessageQueue messageQueue)
        {
            MessageQueue = messageQueue;
        }

        /// <summary>
        /// Returns true if there are any messages inside the queue.
        /// </summary>
        public bool HasMessages
        {
            get { return (uint)Count > 0u; }
        }

        /// <summary>
        /// Returns the number of messages in both the internal message queue
        /// and the prepend buffer.
        /// </summary>
        public int Count
        {
            get { return MessageQueue.Count + _prependBuffer.Count; }
        }

        /// <inheritdoc cref="IMessageQueue"/>
        public void Enqueue(IActorRef receiver, in Envelope envelope)
        {
            MessageQueue.Enqueue(receiver, envelope);
        }

        /// <summary>
        /// Attempt to dequeue a message from the front of the prepend buffer.
        /// 
        /// If the prepend buffer is empty, dequeue a message from the normal
        /// <see cref="IMessageQueue"/> wrapped but this wrapper.
        /// </summary>
        /// <param name="envelope">The message to return, if any</param>
        /// <returns><c>true</c> if a message was available, <c>false</c> otherwise.</returns>
        public bool TryDequeue(out Envelope envelope)
        {
            if (_prependBuffer.TryPop(out envelope)) { return true; }

            return MessageQueue.TryDequeue(out envelope);
        }

        /// <inheritdoc cref="IMessageQueue"/>
        public void CleanUp(IActorRef owner, IMessageQueue deadletters)
        {
            while (TryDequeue(out Envelope msg))
            {
                deadletters.Enqueue(owner, msg);
            }
        }

        /// <summary>
        /// Add a message to the front of the queue via the prepend buffer.
        /// </summary>
        /// <param name="envelope">The message we wish to append to the front of the queue.</param>
        public void EnqueueFirst(in Envelope envelope)
        {
            _prependBuffer.Push(envelope);
        }
    }
}

