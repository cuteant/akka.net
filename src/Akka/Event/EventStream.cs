//-----------------------------------------------------------------------
// <copyright file="EventStream.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Util;
using Akka.Util.Internal;


namespace Akka.Event
{
    /// <summary>
    /// The EventStream is a pub-sub stream of events that can be both system and user generated. 
    /// 
    /// The subscribers are IActorRef instances and events can be any object. Subscriptions are hierarchical meaning that if you listen to
    /// an event for a particular type you will receive events for that type and any sub types.
    /// 
    /// If the debug flag is activated any operations on the event stream will be published as debug level events.
    /// </summary>
    public class EventStream : LoggingBus
    {
        private readonly bool _debug;

        private readonly AtomicReference<Either<IImmutableSet<IActorRef>, IActorRef>> _initiallySubscribedOrUnsubscriber =
            new AtomicReference<Either<IImmutableSet<IActorRef>, IActorRef>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStream"/> class.
        /// </summary>
        /// <param name="debug">if set to <c>true</c> [debug].</param>
        public EventStream(bool debug)
        {
            _debug = debug;
        }

        /// <summary>
        /// Subscribes the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        /// <param name="channel">The channel.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if the given <paramref name="subscriber"/> is undefined.
        /// </exception>
        /// <returns><c>true</c> if subscription was successful, <c>false</c> otherwise.</returns>
        public override bool Subscribe(IActorRef subscriber, Type channel)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber), "The subscriber cannot be a null actor.");

            RegisterWithUnsubscriber(subscriber);
            var res = base.Subscribe(subscriber, channel);
            if (_debug)
            {
                Publish(new Debug(SimpleName(this), GetType(), "subscribing " + subscriber + " to channel " + channel));
            }
            return res;
        }

        /// <summary>
        /// Unsubscribes the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        /// <param name="channel">The channel.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if the given <paramref name="subscriber"/> is undefined.
        /// </exception>
        /// <returns><c>true</c> if unsubscription was successful, <c>false</c> otherwise.</returns>
        public override bool Unsubscribe(IActorRef subscriber, Type channel)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber), "The subscriber cannot be a null actor.");

            if (_debug)
            {
                Publish(new Debug(SimpleName(this), GetType(), "unsubscribing " + subscriber + " from channel " + channel));
            }
            UnregisterIfNoMoreSubscribedChannels(subscriber);
            return base.Unsubscribe(subscriber, channel);
        }

        /// <summary>
        /// Unsubscribes the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if the given <paramref name="subscriber"/> is undefined.
        /// </exception>
        /// <returns><c>true</c> if unsubscription was successful, <c>false</c> otherwise.</returns>
        public override bool Unsubscribe(IActorRef subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber), "The subscriber cannot be a null actor.");

            if (_debug)
            {
                Publish(new Debug(SimpleName(this), GetType(), "unsubscribing " + subscriber + " from all channels"));
            }
            UnregisterIfNoMoreSubscribedChannels(subscriber);
            return base.Unsubscribe(subscriber);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public void StartUnsubscriber(ActorSystemImpl system)
        {
            EventStreamUnsubscribersProvider.Instance.Start(system, this, _debug);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="unsubscriber">TBD</param>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public bool InitUnsubscriber(IActorRef unsubscriber, ActorSystem system)
        {
            if (system == null) { return false; }

            void Handle0(Left<IImmutableSet<IActorRef>> v)
            {
                if (_initiallySubscribedOrUnsubscriber.CompareAndSet(v, Either.Right(unsubscriber)))
                {
                    if (_debug)
                    {
                        Publish(new Debug(SimpleName(this), GetType(),
                            string.Format("initialized unsubscriber to: {0}  registering {1} initial subscribers with it", unsubscriber, v.Value.Count)));

                    }
                    v.Value.ForEach(RegisterWithUnsubscriber);


                }
                else
                {
                    InitUnsubscriber(unsubscriber, system);
                }
            }
            void Handle1(Right<IActorRef> presentUnsubscriber)
            {
                if (_debug)
                {
                    Publish(new Debug(SimpleName(this), GetType(),
                        string.Format("not using unsubscriber {0}, because already initialized with {1}", unsubscriber, presentUnsubscriber)));

                }
            }

            return _initiallySubscribedOrUnsubscriber.Match()
                .With<Left<IImmutableSet<IActorRef>>>(Handle0)
                .With<Right<IActorRef>>(Handle1)
                .WasHandled;
        }

        private void RegisterWithUnsubscriber(IActorRef subscriber)
        {
            void Handle0(Left<IImmutableSet<IActorRef>> v)
            {
                if (_initiallySubscribedOrUnsubscriber.CompareAndSet(v, Either.Left(v.Value.Add(subscriber))))
                {
                    RegisterWithUnsubscriber(subscriber);
                }
            }
            void Handle1(Right<IActorRef> unsubscriber)
            {
                unsubscriber.Value.Tell(new EventStreamUnsubscriber.Register(subscriber));
            }
            _initiallySubscribedOrUnsubscriber.Match()
                .With<Left<IImmutableSet<IActorRef>>>(Handle0)
                .With<Right<IActorRef>>(Handle1);
        }

        private void UnregisterIfNoMoreSubscribedChannels(IActorRef subscriber)
        {
            void Handle0(Left<IImmutableSet<IActorRef>> v)
            {
                if (_initiallySubscribedOrUnsubscriber.CompareAndSet(v, Either.Left(v.Value.Remove(subscriber))))
                {
                    UnregisterIfNoMoreSubscribedChannels(subscriber);
                }
            }
            void Handle1(Right<IActorRef> unsubscriber)
            {
                unsubscriber.Value.Tell(new EventStreamUnsubscriber.UnregisterIfNoMoreSubscribedChannels(subscriber));
            }
            _initiallySubscribedOrUnsubscriber.Match()
                .With<Left<IImmutableSet<IActorRef>>>(Handle0)
                .With<Right<IActorRef>>(Handle1);
        }
    }
}
