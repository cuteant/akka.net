//-----------------------------------------------------------------------
// <copyright file="UnboundedMessageQueue.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using TQueue = System.Collections.Concurrent.ConcurrentQueue<Akka.Actor.Envelope>;

namespace Akka.Dispatch.MessageQueues
{
    /// <summary> An unbounded mailbox message queue. </summary>
    public class UnboundedMessageQueue : IMessageQueue, IUnboundedMessageQueueSemantics
    {
        private readonly TQueue _queue = new TQueue();

        /// <inheritdoc cref="IMessageQueue"/>
        public bool HasMessages => !_queue.IsEmpty;

        /// <inheritdoc cref="IMessageQueue"/>
        public int Count => _queue.Count;

        /// <inheritdoc cref="IMessageQueue"/>
        public void Enqueue(IActorRef receiver, in Envelope envelope) => _queue.Enqueue(envelope);

        /// <inheritdoc cref="IMessageQueue"/>
        public bool TryDequeue(out Envelope envelope) => _queue.TryDequeue(out envelope);

        /// <inheritdoc cref="IMessageQueue"/>
        public void CleanUp(IActorRef owner, IMessageQueue deadletters)
        {
            while (TryDequeue(out Envelope msg))
            {
                deadletters.Enqueue(owner, msg);
            }
        }
    }
}
