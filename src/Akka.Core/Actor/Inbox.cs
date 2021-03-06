﻿//-----------------------------------------------------------------------
// <copyright file="Inbox.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor.Internal;
using Akka.Configuration;
using CuteAnt.Collections;
using MessagePack;

namespace Akka.Actor
{
    /// <summary>
    /// TBD
    /// </summary>
    //[Union(0, typeof(Get))]
    //[Union(1, typeof(Select))]
    internal interface IQuery
    {
        /// <summary>
        /// TBD
        /// </summary>
        TimeSpan Deadline { get; }
        /// <summary>
        /// TBD
        /// </summary>
        IActorRef Client { get; }
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="client">TBD</param>
        /// <returns>TBD</returns>
        IQuery WithClient(IActorRef client);
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal readonly struct Get : IQuery
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="deadline">TBD</param>
        /// <param name="client">TBD</param>
        [SerializationConstructor]
        public Get(TimeSpan deadline, IActorRef client = null)
            : this()
        {
            Deadline = deadline;
            Client = client;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly TimeSpan Deadline;
        [IgnoreMember, IgnoreDataMember]
        TimeSpan IQuery.Deadline => this.Deadline;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly IActorRef Client;
        [IgnoreMember, IgnoreDataMember]
        IActorRef IQuery.Client => this.Client;
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="client">TBD</param>
        /// <returns>TBD</returns>
        public IQuery WithClient(IActorRef client)
        {
            return new Get(Deadline, client);
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal readonly struct Select : IQuery
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="deadline">TBD</param>
        /// <param name="predicate">TBD</param>
        /// <param name="client">TBD</param>
        [SerializationConstructor]
        public Select(TimeSpan deadline, Predicate<object> predicate, IActorRef client = null)
            : this()
        {
            Deadline = deadline;
            Predicate = predicate;
            Client = client;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly TimeSpan Deadline;
        [IgnoreMember, IgnoreDataMember]
        TimeSpan IQuery.Deadline => this.Deadline;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly Predicate<object> Predicate;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public readonly IActorRef Client;
        [IgnoreMember, IgnoreDataMember]
        IActorRef IQuery.Client => this.Client;
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="client">TBD</param>
        /// <returns>TBD</returns>
        public IQuery WithClient(IActorRef client)
        {
            return new Select(Deadline, Predicate, client);
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal readonly struct StartWatch
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="target">TBD</param>
        /// <param name="message">TBD</param>
        [SerializationConstructor]
        public StartWatch(IActorRef target, object message)
            : this()
        {
            Target = target;
            Message = message;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly IActorRef Target;

        /// <summary>
        /// The custom termination message or null
        /// </summary>
        [Key(1)]
        public readonly object Message;
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    internal readonly struct StopWatch
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="target">TBD</param>
        [SerializationConstructor]
        public StopWatch(IActorRef target)
            : this()
        {
            Target = target;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly IActorRef Target;
    }

    internal readonly struct Kick { }

    /// <summary>
    /// TBD
    /// </summary>
    /// <typeparam name="T">TBD</typeparam>
    [Serializable]
    internal class InboxQueue<T> : ICollection<T>
    {
        // LinkedList wrapper instead of Queue? While it's used for queueing, however I expect a lot of churn around 
        // adding-removing elements. Additionally we have to get a functionality of dequeueing element meeting
        // a specific predicate (even if it's in middle of queue), and current queue implementation won't provide that in easy way.


        private readonly Deque<T> _inner = new Deque<T>();

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="item">TBD</param>
        public void Add(T item)
        {
            _inner.AddToBack(item);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Clear()
        {
            _inner.Clear();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="item">TBD</param>
        /// <returns>TBD</returns>
        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="array">TBD</param>
        /// <param name="arrayIndex">TBD</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="item">TBD</param>
        /// <returns>TBD</returns>
        public bool Remove(T item)
        {
            return _inner.Remove(item);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="predicate">TBD</param>
        /// <returns>TBD</returns>
        public int RemoveAll(Predicate<T> predicate)
        {
            return _inner.RemoveAll(predicate);
            //var i = 0;
            //var node = _inner.First;
            //while (!(node is null || predicate(node.Value)))
            //{
            //    var n = node;
            //    node = node.Next;
            //    _inner.Remove(n);
            //    i++;
            //}
            //return i;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="item">TBD</param>
        public void Enqueue(T item)
        {
            _inner.AddToBack(item);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public T Dequeue()
        {
            return _inner.RemoveFromFront();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="predicate">TBD</param>
        /// <returns>TBD</returns>
        public T DequeueFirstOrDefault(Predicate<T> predicate)
        {
            _inner.TryRemoveFromFrontUntil(predicate, out var item);
            return item;

            //var node = _inner.First;
            //while (!(node is null || predicate(node.Value)))
            //{
            //    node = node.Next;
            //}

            //if (node is object)
            //{
            //    var item = node.Value;
            //    _inner.Remove(node);
            //    return item;
            //}

            //return default;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public int Count { get { return _inner.Count; } }
        /// <summary>
        /// TBD
        /// </summary>
        public bool IsReadOnly { get { return false; } }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal class DeadlineComparer : IComparer<IQuery>
    {
        /// <summary>
        /// The singleton instance of this comparer
        /// </summary>
        public static readonly DeadlineComparer Instance = new DeadlineComparer();

        private DeadlineComparer()
        {
        }

        /// <inheritdoc/>
        public int Compare(IQuery x, IQuery y)
        {
            return x.Deadline.CompareTo(y.Deadline);
        }
    }

    /// <summary>
    /// <see cref="IInboxable"/> is an actor-like object to be listened by external objects.
    /// It can watch other actors lifecycle and contains inner actor, which could be passed
    /// as reference to other actors.
    /// </summary>
    public interface IInboxable : ICanWatch
    {
        /// <summary>
        /// Get a reference to internal actor. It may be for example registered in event stream.
        /// </summary>
        IActorRef Receiver { get; }

        /// <summary>
        /// Receive a next message from current <see cref="IInboxable"/> with default timeout. This call will return immediately,
        /// if the internal actor previously received a message, or will block until it'll receive a message.
        /// </summary>
        /// <returns>TBD</returns>
        object Receive();

        /// <summary>
        /// Receive a next message from current <see cref="IInboxable"/>. This call will return immediately,
        /// if the internal actor previously received a message, or will block for time specified by 
        /// <paramref name="timeout"/> until it'll receive a message.
        /// </summary>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        object Receive(TimeSpan timeout);

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        Task<object> ReceiveAsync();

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        Task<object> ReceiveAsync(TimeSpan timeout);

        /// <summary>
        /// Receive a next message satisfying specified <paramref name="predicate"/> under default timeout.
        /// </summary>
        /// <param name="predicate">TBD</param>
        /// <returns>TBD</returns>
        object ReceiveWhere(Predicate<object> predicate);

        /// <summary>
        /// Receive a next message satisfying specified <paramref name="predicate"/> under provided <paramref name="timeout"/>.
        /// </summary>
        /// <param name="predicate">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        object ReceiveWhere(Predicate<object> predicate, TimeSpan timeout);

        /// <summary>
        /// Makes an internal actor act as a proxy of a given <paramref name="message"/>, 
        /// which is sent to a given target actor. It means, that all <paramref name="target"/>'s
        /// replies will be sent to current inbox instead.
        /// </summary>
        /// <param name="target">TBD</param>
        /// <param name="message">TBD</param>
        void Send(IActorRef target, object message);
    }

    /// <summary>
    /// TBD
    /// </summary>
    public class Inbox : IInboxable, IDisposable
    {
        private static int _inboxNr = 0;
        private readonly ActorSystem _system;
        private readonly TimeSpan _defaultTimeout;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public static Inbox Create(ActorSystem system)
        {
            var config = system.Settings.Config.GetConfig("akka.actor.inbox");
            if (config.IsNullOrEmpty())
            {
                throw ConfigurationException.NullOrEmptyConfig<Inbox>("akka.actor.inbox");
            }

            var inboxSize = config.GetInt("inbox-size", 0);
            var timeout = config.GetTimeSpan("default-timeout", null);

            var receiver = ((ActorSystemImpl)system).SystemActorOf(Props.Create(() => new InboxActor(inboxSize)), "inbox-" + Interlocked.Increment(ref _inboxNr));

            return new Inbox(timeout, receiver, system);
        }

        private Inbox(TimeSpan defaultTimeout, IActorRef receiver, ActorSystem system)
        {
            _defaultTimeout = defaultTimeout;
            _system = system;
            Receiver = receiver;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IActorRef Receiver { get; private set; }

        /// <summary>
        /// Make the inbox’s actor watch the <paramref name="subject"/> actor such that 
        /// reception of the <see cref="Terminated"/> message can then be awaited.
        /// </summary>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef Watch(IActorRef subject)
        {
            Receiver.Tell(new StartWatch(subject, null));
            return subject;
        }

        public IActorRef WatchWith(IActorRef subject, object message)
        {
            Receiver.Tell(new StartWatch(subject, message));
            return subject;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef Unwatch(IActorRef subject)
        {
            Receiver.Tell(new StopWatch(subject));
            return subject;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actorRef">TBD</param>
        /// <param name="message">TBD</param>
        public void Send(IActorRef actorRef, object message)
        {
            actorRef.Tell(message, Receiver);
        }

        /// <summary>
        /// Receive a single message from <see cref="Receiver"/> actor with default timeout. 
        /// NOTE: Timeout resolution depends on system's scheduler.
        /// </summary>
        /// <remarks>
        /// Don't use this method within actors, since it block current thread until a message is received.
        /// </remarks>
        /// <returns>TBD</returns>
        public object Receive()
        {
            return Receive(_defaultTimeout);
        }

        /// <summary>
        /// Receive a single message from <see cref="Receiver"/> actor. 
        /// Provided <paramref name="timeout"/> is used for cleanup purposes.
        /// NOTE: <paramref name="timeout"/> resolution depends on system's scheduler.
        /// </summary>
        /// <remarks>
        /// Don't use this method within actors, since it block current thread until a message is received.
        /// </remarks>
        /// <param name="timeout">TBD</param>
        /// <exception cref="TimeoutException">
        /// This exception is thrown if the inbox received a <see cref="Status.Failure"/> response message or
        /// it didn't receive a response message by the given <paramref name="timeout"/> .
        /// </exception>
        /// <returns>TBD</returns>
        public object Receive(TimeSpan timeout)
        {
            var task = ReceiveAsync(timeout);
            return AwaitResult(task, timeout);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="predicate">TBD</param>
        /// <returns>TBD</returns>
        public object ReceiveWhere(Predicate<object> predicate)
        {
            return ReceiveWhere(predicate, _defaultTimeout);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="predicate">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <exception cref="TimeoutException">
        /// This exception is thrown if the inbox received a <see cref="Status.Failure"/> response message or
        /// it didn't receive a response message by the given <paramref name="timeout"/> .
        /// </exception>
        /// <returns>TBD</returns>
        public object ReceiveWhere(Predicate<object> predicate, TimeSpan timeout)
        {
            var task = Receiver.Ask(new Select(_system.Scheduler.MonotonicClock + timeout, predicate), Timeout.InfiniteTimeSpan);
            return AwaitResult(task, timeout);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public Task<object> ReceiveAsync()
        {
            return ReceiveAsync(_defaultTimeout);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        public Task<object> ReceiveAsync(TimeSpan timeout)
        {
            return Receiver.Ask(new Get(_system.Scheduler.MonotonicClock + timeout), Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <param name="disposing">if set to <c>true</c> the method has been called directly or indirectly by a 
        /// user's code. Managed and unmanaged resources will be disposed.<br />
        /// if set to <c>false</c> the method has been called by the runtime from inside the finalizer and only 
        /// unmanaged resources can be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _system.Stop(Receiver);
        }

        private object AwaitResult(Task<object> task, TimeSpan timeout)
        {
            if (!task.Wait(timeout))
            {
                AkkaThrowHelper.ThrowTimeoutException_InboxDidntReceiveResponseMsgInSpecifiedTimeout(Receiver, timeout);
            }

            if (task.Result is Status.Failure received && received.Cause is TimeoutException)
            {
                AkkaThrowHelper.ThrowTimeoutException_InboxReceivedAStatusFailureResponseMsg(Receiver, received);
            }

            return task.Result;
        }
    }
}
