﻿//-----------------------------------------------------------------------
// <copyright file="BlockingMessageQueue.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using Akka.Actor;

namespace Akka.Dispatch.MessageQueues
{
    /// <summary> 
    /// Base class for blocking message queues. Allows non thread safe data structures to be used as message queues. 
    /// </summary>
    public abstract class BlockingMessageQueue : IMessageQueue, IBlockingMessageQueueSemantics
    {
        private readonly object _lock = new object();

        /// <summary>
        /// TBD
        /// </summary>
        protected abstract int LockedCount { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan BlockTimeOut { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// TBD
        /// </summary>
        public bool HasMessages
        {
            get { return (uint)Count > 0u; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return LockedCount;
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="receiver">TBD</param>
        /// <param name="envelope">TBD</param>
        public void Enqueue(IActorRef receiver, in Envelope envelope)
        {
            lock (_lock)
            {
                LockedEnqueue(envelope);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="envelope">TBD</param>
        /// <returns>TBD</returns>
        public bool TryDequeue(out Envelope envelope)
        {
            lock (_lock)
            {
                return LockedTryDequeue(out envelope);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="owner">TBD</param>
        /// <param name="deadletters">TBD</param>
        /// <returns>TBD</returns>
        public void CleanUp(IActorRef owner, IMessageQueue deadletters)
        {
            while (TryDequeue(out var msg)) // lock gets acquired inside the TryDequeue method
            {
                deadletters.Enqueue(owner, msg);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="envelope">TBD</param>
        protected abstract void LockedEnqueue(in Envelope envelope);

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="envelope">TBD</param>
        /// <returns>TBD</returns>
        protected abstract bool LockedTryDequeue(out Envelope envelope);
    }
}

