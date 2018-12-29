//-----------------------------------------------------------------------
// <copyright file="ActorRefSourceActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Event;
using Akka.Streams.Actors;

namespace Akka.Streams.Implementation
{
    /// <summary>
    /// TBD
    /// </summary>
    /// <typeparam name="T">TBD</typeparam>
    internal class ActorRefSourceActor<T> : Actors.ActorPublisher<T>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="bufferSize">TBD</param>
        /// <param name="overflowStrategy">TBD</param>
        /// <param name="settings">TBD</param>
        /// <exception cref="NotSupportedException">
        /// This exception is thrown when the specified <paramref name="overflowStrategy"/> is <see cref="Akka.Streams.OverflowStrategy.Backpressure"/>.
        /// </exception>
        /// <returns>TBD</returns>
        public static Props Props(int bufferSize, OverflowStrategy overflowStrategy, ActorMaterializerSettings settings)
        {
            if (overflowStrategy == OverflowStrategy.Backpressure) ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Backpressure_strategy);

            var maxFixedBufferSize = settings.MaxFixedBufferSize;
            return Actor.Props.Create(() => new ActorRefSourceActor<T>(bufferSize, overflowStrategy, maxFixedBufferSize));
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected readonly IBuffer<T> Buffer;

        /// <summary>
        /// TBD
        /// </summary>
        public readonly int BufferSize;
        /// <summary>
        /// TBD
        /// </summary>
        public readonly OverflowStrategy OverflowStrategy;
        private ILoggingAdapter _log;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="bufferSize">TBD</param>
        /// <param name="overflowStrategy">TBD</param>
        /// <param name="maxFixedBufferSize">TBD</param>
        public ActorRefSourceActor(int bufferSize, OverflowStrategy overflowStrategy, int maxFixedBufferSize)
        {
            BufferSize = bufferSize;
            OverflowStrategy = overflowStrategy;
            Buffer = bufferSize != 0 ? Implementation.Buffer.Create<T>(bufferSize, maxFixedBufferSize) : null;
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected ILoggingAdapter Log => _log ?? (_log = Context.GetLogger());

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
            => DefaultReceive(message) || RequestElement(message) || (message is T tmsg && ReceiveElement(tmsg));

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected bool DefaultReceive(object message)
        {
            switch (message)
            {
                case Actors.Cancel _:
                    Context.Stop(Self);
                    return true;
                case Status.Success _:
                    if (BufferSize == 0 || Buffer.IsEmpty)
                        Context.Stop(Self);  // will complete the stream successfully
                    else
                        Context.Become(DrainBufferThenComplete);
                    return true;
                case Status.Failure failure:
                    OnErrorThenStop(failure.Cause);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected virtual bool RequestElement(object message)
        {
            if (message is Request)
            {
                // totalDemand is tracked by base
                if (BufferSize != 0)
                    while (TotalDemand > 0L && !Buffer.IsEmpty)
                        OnNext(Buffer.Dequeue());

                return true;
            }

            return false;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected virtual bool ReceiveElement(T message)
        {
            if (IsActive)
            {
                if (TotalDemand > 0L)
                {
                    OnNext(message);
                }
                else if (BufferSize == 0)
                {
                    if (Log.IsDebugEnabled) Log.DroppingElementBecauseThereIsNoDownstreamDemand(message);
                }
                else if (!Buffer.IsFull)
                {
                    Buffer.Enqueue(message);
                }
                else
                {
                    switch (OverflowStrategy)
                    {
                        case OverflowStrategy.DropHead:
                            if (Log.IsDebugEnabled) Log.DroppingTheHeadElementBecauseBufferIsFull();
                            Buffer.DropHead();
                            Buffer.Enqueue(message);
                            break;
                        case OverflowStrategy.DropTail:
                            if (Log.IsDebugEnabled) Log.DroppingTheTailElementBecauseBufferIsFull();
                            Buffer.DropTail();
                            Buffer.Enqueue(message);
                            break;
                        case OverflowStrategy.DropBuffer:
                            if (Log.IsDebugEnabled) Log.DroppingAllTheBufferedElementsBecauseBufferIsFull();
                            Buffer.Clear();
                            Buffer.Enqueue(message);
                            break;
                        case OverflowStrategy.DropNew:
                            // do not enqueue new element if the buffer is full
                            if (Log.IsDebugEnabled) Log.DroppingTheNewElementBecauseBufferIsFull();
                            break;
                        case OverflowStrategy.Fail:
                            Log.FailingBecauseBufferIsFull();
                            OnErrorThenStop(new BufferOverflowException($"Buffer overflow, max capacity was ({BufferSize})"));
                            break;
                        case OverflowStrategy.Backpressure:
                            // there is a precondition check in Source.actorRefSource factory method
                            if (Log.IsDebugEnabled) Log.BackpressuringBecauseBufferIsFull();
                            break;
                    }
                }

                return true;
            }

            return false;
        }

        private bool DrainBufferThenComplete(object message)
        {
            switch (message)
            {
                case Cancel _:
                    Context.Stop(Self);
                    return true;
                case Status.Failure failure when (IsActive):
                    // errors must be signaled as soon as possible,
                    // even if previously valid completion was requested via Status.Success
                    OnErrorThenStop(failure.Cause);
                    return true;
                case Request _:
                    // totalDemand is tracked by base
                    while (TotalDemand > 0L && !Buffer.IsEmpty)
                    {
                        OnNext(Buffer.Dequeue());
                    }
                    if (Buffer.IsEmpty)
                    {
                        Context.Stop(Self); // will complete the stream successfully
                    }
                    return true;
                default:
                    if (IsActive)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.DroppingElementBecauseStatusSuccessReceivedAlready(message, Buffer.Used);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
            }
        }
    }
}
