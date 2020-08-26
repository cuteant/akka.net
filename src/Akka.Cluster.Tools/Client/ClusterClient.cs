//-----------------------------------------------------------------------
// <copyright file="ClusterClient.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Remote;
using Akka.Util.Internal;

namespace Akka.Cluster.Tools.Client
{
    /// <summary>
    /// This actor is intended to be used on an external node that is not member
    /// of the cluster. It acts like a gateway for sending messages to actors
    /// somewhere in the cluster. From the initial contact points it will establish
    /// a connection to a <see cref="ClusterReceptionist"/> somewhere in the cluster. It will
    /// monitor the connection to the receptionist and establish a new connection if
    /// the link goes down. When looking for a new receptionist it uses fresh contact
    /// points retrieved from previous establishment, or periodically refreshed
    /// contacts, i.e. not necessarily the initial contact points.
    /// </summary>
    public sealed class ClusterClient : ActorBase
    {
        #region Messages

        /// <summary>
        /// The message will be delivered to one recipient with a matching path, if any such
        /// exists. If several entries match the path the message will be delivered
        /// to one random destination. The sender of the message can specify that local
        /// affinity is preferred, i.e. the message is sent to an actor in the same local actor
        /// system as the used receptionist actor, if any such exists, otherwise random to any other
        /// matching entry.
        /// </summary>
        [Serializable]
        public sealed class Send
        {
            /// <summary>
            /// TBD
            /// </summary>
            public string Path { get; }
            /// <summary>
            /// TBD
            /// </summary>
            public object Message { get; }
            /// <summary>
            /// TBD
            /// </summary>
            public bool LocalAffinity { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="path">TBD</param>
            /// <param name="message">TBD</param>
            /// <param name="localAffinity">TBD</param>
            public Send(string path, object message, bool localAffinity = false)
            {
                Path = path;
                Message = message;
                LocalAffinity = localAffinity;
            }
        }

        /// <summary>
        /// The message will be delivered to all recipients with a matching path.
        /// </summary>
        [Serializable]
        public sealed class SendToAll
        {
            /// <summary>
            /// TBD
            /// </summary>
            public string Path { get; }
            /// <summary>
            /// TBD
            /// </summary>
            public object Message { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="path">TBD</param>
            /// <param name="message">TBD</param>
            public SendToAll(string path, object message)
            {
                Path = path;
                Message = message;
            }
        }

        /// <summary>
        /// The message will be delivered to all recipients Actors that have been registered as subscribers to
        /// to the named topic.
        /// </summary>
        [Serializable]
        public sealed class Publish
        {
            /// <summary>
            /// TBD
            /// </summary>
            public string Topic { get; }
            /// <summary>
            /// TBD
            /// </summary>
            public object Message { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="topic">TBD</param>
            /// <param name="message">TBD</param>
            public Publish(string topic, object message)
            {
                Topic = topic;
                Message = message;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class RefreshContactsTick : ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly RefreshContactsTick Instance = new RefreshContactsTick();
            private RefreshContactsTick() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class HeartbeatTick : ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly HeartbeatTick Instance = new HeartbeatTick();
            private HeartbeatTick() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class ReconnectTimeout : ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly ReconnectTimeout Instance = new ReconnectTimeout();
            private ReconnectTimeout() { }
        }

        #endregion

        /// <summary>
        /// Factory method for <see cref="ClusterClient"/> <see cref="Actor.Props"/>.
        /// </summary>
        /// <param name="settings">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="settings"/> is undefined.
        /// </exception>
        /// <returns>TBD</returns>
        public static Props Props(ClusterClientSettings settings)
        {
            if (settings is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings);

            return Actor.Props.Create(() => new ClusterClient(settings)).WithDeploy(Deploy.Local);
        }

        private ILoggingAdapter _log = Context.GetLogger();
        private readonly ClusterClientSettings _settings;
        private readonly DeadlineFailureDetector _failureDetector;
        private ImmutableHashSet<ActorPath> _contactPaths;
        private readonly ActorSelection[] _initialContactsSelections;
        private ActorSelection[] _contacts;
        private ImmutableHashSet<ActorPath> _contactPathsPublished;
        private ImmutableList<IActorRef> _subscribers;
        private readonly ICancelable _heartbeatTask;
        private ICancelable _refreshContactsCancelable;
        private readonly Queue<(object, IActorRef)> _buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterClient" /> class.
        /// </summary>
        /// <param name="settings">The settings used to configure the client.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the settings contains no initial contacts.
        /// </exception>
        public ClusterClient(ClusterClientSettings settings)
        {
            if (settings.InitialContacts.IsEmptyR())
            {
                ThrowHelper.ThrowArgumentException_InitialContactsForClusterClientCannotBeEmpty();
            }

            _settings = settings;
            _failureDetector = new DeadlineFailureDetector(_settings.AcceptableHeartbeatPause, _settings.HeartbeatInterval);

            _contactPaths = settings.InitialContacts.ToImmutableHashSet(ActorPathComparer.Instance);
            _initialContactsSelections = _contactPaths.Select(Context.ActorSelection).ToArray();
            _contacts = _initialContactsSelections;

            SendGetContacts();

            _contactPathsPublished = _contactPaths;
            _subscribers = ImmutableList<IActorRef>.Empty;

            _heartbeatTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                settings.HeartbeatInterval,
                settings.HeartbeatInterval,
                Self,
                HeartbeatTick.Instance,
                Self);

            _refreshContactsCancelable = null;
            ScheduleRefreshContactsTick(settings.EstablishingGetContactsInterval);
            Self.Tell(RefreshContactsTick.Instance);

            _buffer = new Queue<(object, IActorRef)>();
        }

        private void ScheduleRefreshContactsTick(TimeSpan interval)
        {
            if (_refreshContactsCancelable is object)
            {
                _refreshContactsCancelable.Cancel();
                _refreshContactsCancelable = null;
            }

            _refreshContactsCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                interval,
                interval,
                Self,
                RefreshContactsTick.Instance,
                Self);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            base.PostStop();
            _heartbeatTask.Cancel();

            if (_refreshContactsCancelable is object)
            {
                _refreshContactsCancelable.Cancel();
                _refreshContactsCancelable = null;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            return Establishing(message);
        }

        private bool Establishing(object message)
        {
            ICancelable connectTimerCancelable = null;
            if (_settings.ReconnectTimeout.HasValue)
            {
                connectTimerCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(
                    _settings.ReconnectTimeout.Value,
                    Self,
                    ReconnectTimeout.Instance,
                    Self);
            }

            switch (message)
            {
                case ClusterReceptionist.Contacts contacts:
                    if (contacts.ContactPoints.Count > 0)
                    {
                        _contactPaths = contacts.ContactPoints.Select(ActorPath.Parse).ToImmutableHashSet(ActorPathComparer.Instance);
                        _contacts = _contactPaths.Select(Context.ActorSelection).ToArray();
                        _contacts.ForEach(c => c.Tell(new Identify(null)));
                    }

                    PublishContactPoints();
                    return true;
                case ActorIdentity actorIdentify:
                    var receptionist = actorIdentify.Subject;

                    if (receptionist is object)
                    {
                        if (_log.IsInfoEnabled) { _log.ConnectedTo(receptionist); }
                        ScheduleRefreshContactsTick(_settings.RefreshContactsInterval);
                        SendBuffered(receptionist);
                        Context.Become(Active(receptionist));
                        connectTimerCancelable?.Cancel();
                        _failureDetector.HeartBeat();
                        Self.Tell(HeartbeatTick.Instance); // will register us as active client of the selected receptionist
                    }
                    else
                    {
                        // ok, use another instead
                    }
                    return true;
                case HeartbeatTick _:
                    _failureDetector.HeartBeat();
                    return true;
                case RefreshContactsTick _:
                    SendGetContacts();
                    return true;
                case Send send:
                    Buffer(new PublishSubscribe.Send(send.Path, send.Message, send.LocalAffinity));
                    return true;
                case SendToAll sendToAll:
                    Buffer(new PublishSubscribe.SendToAll(sendToAll.Path, sendToAll.Message));
                    return true;
                case Publish publish:
                    Buffer(new PublishSubscribe.Publish(publish.Topic, publish.Message));
                    return true;
                case ReconnectTimeout _:
                    if (_log.IsWarningEnabled) { _log.ReceptionistReconnectNotSuccessful(_settings); }
                    Context.Stop(Self);
                    return true;
                case ClusterReceptionist.ReceptionistShutdown _:
                    // ok, haven't chosen a receptionist yet
                    return true;
                default:
                    return ContactPointMessages(message);
            }
        }

        private Receive Active(IActorRef receptionist)
        {
            bool LocalHandleMessage(object message)
            {
                switch (message)
                {
                    case Send send:
                        receptionist.Forward(new PublishSubscribe.Send(send.Path, send.Message, send.LocalAffinity));
                        return true;
                    case SendToAll sendToAll:
                        receptionist.Forward(new PublishSubscribe.SendToAll(sendToAll.Path, sendToAll.Message));
                        return true;
                    case Publish publish:
                        receptionist.Forward(new PublishSubscribe.Publish(publish.Topic, publish.Message));
                        return true;
                    case HeartbeatTick _:
                        if (!_failureDetector.IsAvailable)
                        {
                            if (_log.IsInfoEnabled) { _log.LostContactWithReestablishingConnection(receptionist); }
                            Reestablish();
                        }
                        else
                        {
                            receptionist.Tell(ClusterReceptionist.Heartbeat.Instance);
                        }
                        return true;
                    case ClusterReceptionist.HeartbeatRsp _:
                        _failureDetector.HeartBeat();
                        return true;
                    case RefreshContactsTick _:
                        receptionist.Tell(ClusterReceptionist.GetContacts.Instance);
                        return true;
                    case ClusterReceptionist.Contacts contacts:
                        // refresh of contacts
                        if (contacts.ContactPoints.Count > 0)
                        {
                            _contactPaths = contacts.ContactPoints.Select(ActorPath.Parse).ToImmutableHashSet(ActorPathComparer.Instance);
                            _contacts = _contactPaths.Select(Context.ActorSelection).ToArray();
                        }
                        PublishContactPoints();
                        return true;
                    case ActorIdentity _:
                        // ok, from previous establish, already handled
                        return true;
                    case ClusterReceptionist.ReceptionistShutdown _:
                        if (receptionist.Equals(Sender))
                        {
                            if (_log.IsInfoEnabled) { _log.Receptionist_is_shutting_down_reestablishing_connection(receptionist); }
                            Reestablish();
                        }
                        return true;

                    default:
                        return ContactPointMessages(message);
                }
            }
            return m => LocalHandleMessage(m);
        }

        private void Reestablish()
        {
            SendGetContacts();
            ScheduleRefreshContactsTick(_settings.EstablishingGetContactsInterval);
            Context.Become(Establishing);
            _failureDetector.HeartBeat();
        }

        private bool ContactPointMessages(object message)
        {
            switch (message)
            {
                case SubscribeContactPoints _:
                    var subscriber = Sender;
                    subscriber.Tell(new ContactPoints(_contactPaths));
                    _subscribers = _subscribers.Add(subscriber);
                    Context.Watch(subscriber);
                    return true;
                case UnsubscribeContactPoints _:
                    var subscriber1 = Sender;
                    _subscribers = _subscribers.Where(c => !c.Equals(subscriber1)).ToImmutableList();
                    return true;
                case Terminated terminated:
                    Self.Tell(UnsubscribeContactPoints.Instance, terminated.ActorRef);
                    return true;
                case GetContactPoints _:
                    Sender.Tell(new ContactPoints(_contactPaths));
                    return true;
                default:
                    return false;
            }
        }

        private void SendGetContacts()
        {
            ActorSelection[] sendTo;
            if (0u >= (uint)_contacts.Length)
                sendTo = _initialContactsSelections;
            else if (1u >= (uint)_contacts.Length)
                sendTo = _initialContactsSelections.Union(_contacts).ToArray();
            else
                sendTo = _contacts;

#if DEBUG
            if (_log.IsDebugEnabled) { _log.SendingGetContactsTo(sendTo); }
#endif

            sendTo.ForEach(c => c.Tell(ClusterReceptionist.GetContacts.Instance));
        }

        private void Buffer(object message)
        {
            if (_settings.BufferSize == 0)
            {
                if (_log.IsWarningEnabled) { _log.ReceptionistNotAvailableAndBufferingIsDisabled(message); }
            }
            else if (_buffer.Count == _settings.BufferSize)
            {
                var m = _buffer.Dequeue();
                if (_log.IsWarningEnabled) { _log.ReceptionistNotAvailableBufferIsFull(m.Item1); }
                _buffer.Enqueue((message, Sender));
            }
            else
            {
#if DEBUG
                if (_log.IsDebugEnabled) // don't invoke reflection call on message type if we don't have to
                {
                    _log.ReceptionistNotAvailableBufferingMessageType(message);
                }
#endif
                _buffer.Enqueue((message, Sender));
            }
        }

        private void SendBuffered(IActorRef receptionist)
        {
#if DEBUG
            if (_log.IsDebugEnabled) { _log.SendingBufferedMessagesToReceptionist(); }
#endif
            while (_buffer.Count != 0)
            {
                var t = _buffer.Dequeue();
                receptionist.Tell(t.Item1, t.Item2);
            }
        }

        private void PublishContactPoints()
        {
            foreach (var cp in _contactPaths)
            {
                if (!_contactPathsPublished.Contains(cp))
                {
                    var contactPointAdded = new ContactPointAdded(cp);
                    _subscribers.ForEach(s => s.Tell(contactPointAdded));
                }
            }

            foreach (var cp in _contactPathsPublished)
            {
                if (!_contactPaths.Contains(cp))
                {
                    var contactPointRemoved = new ContactPointRemoved(cp);
                    _subscribers.ForEach(s => s.Tell(contactPointRemoved));
                }
            }

            _contactPathsPublished = _contactPaths;
        }
    }

    /// <summary>
    /// Declares a super type for all events emitted by the `ClusterClient`
    /// in relation to contact points being added or removed.
    /// </summary>
    public interface IContactPointChange
    {
        /// <summary>
        /// TBD
        /// </summary>
        ActorPath ContactPoint { get; }
    }

    /// <summary>
    /// Emitted to a subscriber when contact points have been
    /// received by the <see cref="ClusterClient"/> and a new one has been added.
    /// </summary>
    public sealed class ContactPointAdded : IContactPointChange
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="contactPoint">TBD</param>
        public ContactPointAdded(ActorPath contactPoint)
        {
            ContactPoint = contactPoint;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ActorPath ContactPoint { get; }
    }

    /// <summary>
    /// Emitted to a subscriber when contact points have been
    /// received by the <see cref="ClusterClient"/> and a new one has been added.
    /// </summary>
    public sealed class ContactPointRemoved : IContactPointChange
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="contactPoint">TBD</param>
        public ContactPointRemoved(ActorPath contactPoint)
        {
            ContactPoint = contactPoint;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ActorPath ContactPoint { get; }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public interface ISubscribeContactPoints
    {
    }

    /// <summary>
    /// Subscribe to a cluster client's contact point changes where
    /// it is guaranteed that a sender receives the initial state
    /// of contact points prior to any events in relation to them
    /// changing.
    /// The sender will automatically become unsubscribed when it
    /// terminates.
    /// </summary>
    public sealed class SubscribeContactPoints : ISubscribeContactPoints, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly SubscribeContactPoints Instance = new SubscribeContactPoints();
        private SubscribeContactPoints() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public interface IUnsubscribeContactPoints
    {
    }

    /// <summary>
    /// Explicitly unsubscribe from contact point change events.
    /// </summary>
    public sealed class UnsubscribeContactPoints : IUnsubscribeContactPoints, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly UnsubscribeContactPoints Instance = new UnsubscribeContactPoints();
        private UnsubscribeContactPoints() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public interface IGetContactPoints
    {
    }

    /// <summary>
    /// Get the contact points known to this client. A <see cref="ContactPoints"/> message
    /// will be replied.
    /// </summary>
    public sealed class GetContactPoints : IGetContactPoints, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GetContactPoints Instance = new GetContactPoints();
        private GetContactPoints() { }
    }

    /// <summary>
    /// The reply to <see cref="GetContactPoints"/>.
    /// </summary>
    public sealed class ContactPoints
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="contactPoints">TBD</param>
        public ContactPoints(IImmutableSet<ActorPath> contactPoints)
        {
            ContactPointsList = contactPoints;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IImmutableSet<ActorPath> ContactPointsList { get; }
    }
}
