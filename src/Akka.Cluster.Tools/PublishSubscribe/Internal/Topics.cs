//-----------------------------------------------------------------------
// <copyright file="Topics.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Remote;
using Akka.Routing;

namespace Akka.Cluster.Tools.PublishSubscribe.Internal
{
    /// <summary>
    /// TBD
    /// </summary>
    internal abstract class TopicLike : ActorBase
    {
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly TimeSpan PruneInterval;
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly ICancelable PruneCancelable;
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly ISet<IActorRef> Subscribers;
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly TimeSpan EmptyTimeToLive;

        /// <summary>
        /// TBD
        /// </summary>
        protected Deadline PruneDeadline = null;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="emptyTimeToLive">TBD</param>
        protected TopicLike(TimeSpan emptyTimeToLive)
        {
            Subscribers = new HashSet<IActorRef>();
            EmptyTimeToLive = emptyTimeToLive;
            PruneInterval = new TimeSpan(emptyTimeToLive.Ticks / 2);
            PruneCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(PruneInterval, PruneInterval, Self, Prune.Instance, Self);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            base.PostStop();
            PruneCancelable.Cancel();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected bool DefaultReceive(object message)
        {
            switch (message)
            {
                case Subscribe subscribe:
                    Context.Watch(subscribe.Ref);
                    Subscribers.Add(subscribe.Ref);
                    PruneDeadline = null;
                    Context.Parent.Tell(new Subscribed(new SubscribeAck(subscribe), Sender));
                    break;

                case Unsubscribe unsubscribe:
                    Context.Unwatch(unsubscribe.Ref);
                    Remove(unsubscribe.Ref);
                    Context.Parent.Tell(new Unsubscribed(new UnsubscribeAck(unsubscribe), Sender));
                    break;

                case Terminated terminated:
                    Remove(terminated.ActorRef);
                    break;

                case Prune _:
                    if (PruneDeadline != null && PruneDeadline.IsOverdue)
                    {
                        PruneDeadline = null;
                        Context.Parent.Tell(NoMoreSubscribers.Instance);
                    }
                    break;

                case TerminateRequest _:
                    if (Subscribers.Count == 0 && !Context.GetChildren().Any())
                    {
                        Context.Stop(Self);
                    }
                    else
                    {
                        Context.Parent.Tell(NewSubscriberArrived.Instance);
                    }
                    break;

                default:
                    foreach (var subscriber in Subscribers)
                    {
                        subscriber.Forward(message);
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected abstract bool Business(object message);

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            return Business(message) || DefaultReceive(message);
        }

        private void Remove(IActorRef actorRef)
        {
            Subscribers.Remove(actorRef);

            if (Subscribers.Count == 0 && !Context.GetChildren().Any())
            {
                PruneDeadline = Deadline.Now + EmptyTimeToLive;
            }
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal class Topic : TopicLike
    {
        private readonly RoutingLogic _routingLogic;
        private readonly PerGroupingBuffer _buffer;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="emptyTimeToLive">TBD</param>
        /// <param name="routingLogic">TBD</param>
        public Topic(TimeSpan emptyTimeToLive, RoutingLogic routingLogic) : base(emptyTimeToLive)
        {
            _routingLogic = routingLogic;
            _buffer = new PerGroupingBuffer();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Business(object message)
        {
            switch (message)
            {
                case Subscribe subscribe when subscribe.Group != null:
                    var encodedGroup = Utils.EncodeName(subscribe.Group);
                    _buffer.BufferOr(Utils.MakeKey(Self.Path / encodedGroup), subscribe, Sender, () =>
                    {
                        var child = Context.Child(encodedGroup);
                        if (!child.IsNobody())
                        {
                            child.Forward(message);
                        }
                        else
                        {
                            NewGroupActor(encodedGroup).Forward(message);
                        }
                    });
                    PruneDeadline = null;
                    return true;

                case Unsubscribe unsubscribe when unsubscribe.Group != null:
                    var encodedGroup0 = Utils.EncodeName(unsubscribe.Group);
                    _buffer.BufferOr(Utils.MakeKey(Self.Path / encodedGroup0), unsubscribe, Sender, () =>
                    {
                        var child = Context.Child(encodedGroup0);
                        if (!child.IsNobody())
                        {
                            child.Forward(message);
                        }
                        else
                        {
                                // no such group here
                            }
                    });
                    return true;

                case Subscribed _:
                    Context.Parent.Forward(message);
                    return true;

                case Unsubscribed _:
                    Context.Parent.Forward(message);
                    return true;

                case NoMoreSubscribers _:
                    var key = Utils.MakeKey(Sender);
                    _buffer.InitializeGrouping(key);
                    Sender.Tell(TerminateRequest.Instance);
                    return true;

                case NewSubscriberArrived _:
                    var key0 = Utils.MakeKey(Sender);
                    _buffer.ForwardMessages(key0, Sender);
                    return true;

                case Terminated terminated:
                    var key1 = Utils.MakeKey(terminated.ActorRef);
                    _buffer.RecreateAndForwardMessagesIfNeeded(key1, () => NewGroupActor(terminated.ActorRef.Path.Name));
                    return true;

                default:
                    return false;
            }
        }

        private IActorRef NewGroupActor(string encodedGroup)
        {
            var g = Context.ActorOf(Props.Create(() => new Group(EmptyTimeToLive, _routingLogic)), encodedGroup);
            Context.Watch(g);
            Context.Parent.Tell(new RegisterTopic(g));
            return g;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal class Group : TopicLike
    {
        private readonly RoutingLogic _routingLogic;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="emptyTimeToLive">TBD</param>
        /// <param name="routingLogic">TBD</param>
        public Group(TimeSpan emptyTimeToLive, RoutingLogic routingLogic) : base(emptyTimeToLive)
        {
            _routingLogic = routingLogic;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Business(object message)
        {
            if (message is SendToOneSubscriber)
            {
                if (Subscribers.Count != 0)
                {
                    var send = (SendToOneSubscriber)message;
                    var routees = Subscribers.Select(sub => (Routee)new ActorRefRoutee(sub)).ToArray();
                    new Router(_routingLogic, routees).Route(Utils.WrapIfNeeded(send.Message), Sender);
                }
            }
            else return false;
            return true;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// <para>
        /// Mediator uses <see cref="Router"/> to send messages to multiple destinations, Router in general
        /// unwraps messages from <see cref="RouterEnvelope"/> and sends the contents to <see cref="Routee"/>s.
        /// </para>
        /// <para>
        /// Using mediator services should not have an undesired effect of unwrapping messages
        /// out of <see cref="RouterEnvelope"/>. For this reason user messages are wrapped in
        /// <see cref="MediatorRouterEnvelope"/> which will be unwrapped by the <see cref="Router"/> leaving original
        /// user message.
        /// </para>
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        public static object WrapIfNeeded(object message)
        {
            return message is RouterEnvelope ? new MediatorRouterEnvelope(message) : message;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actorRef">TBD</param>
        /// <returns>TBD</returns>
        public static string MakeKey(IActorRef actorRef)
        {
            return MakeKey(actorRef.Path);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="name">TBD</param>
        /// <returns>TBD</returns>
        public static string EncodeName(string name)
        {
            return name == null ? null : Uri.EscapeDataString(name);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <returns>TBD</returns>
        public static string MakeKey(ActorPath path)
        {
            return path.ToStringWithoutAddress();
        }
    }
}
