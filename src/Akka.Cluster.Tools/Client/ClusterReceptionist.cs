﻿//-----------------------------------------------------------------------
// <copyright file="ClusterReceptionist.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using Akka.Pattern;
using Akka.Remote;
using Akka.Routing;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Cluster.Tools.Client
{
    /// <summary>
    /// Marker trait for remote messages with special serializer.
    /// </summary>
    public interface IClusterClientMessage { }

    /// <summary>
    /// Declares a super type for all events emitted by the <see cref="ClusterClientReceptionist"/>
    /// in relation to cluster clients being interacted with.
    /// </summary>
    public interface IClusterClientInteraction
    {
        /// <summary>
        /// TBD
        /// </summary>
        IActorRef ClusterClient { get; }
    }

    /// <summary>
    /// Emitted to the Akka event stream when a cluster client has interacted with
    /// a receptionist.
    /// </summary>
    public sealed class ClusterClientUp : IClusterClientInteraction
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="clusterClient">TBD</param>
        public ClusterClientUp(IActorRef clusterClient)
        {
            ClusterClient = clusterClient;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IActorRef ClusterClient { get; }
    }

    /// <summary>
    /// Emitted to the Akka event stream when a cluster client was previously connected
    /// but then not seen for some time.
    /// </summary>
    public sealed class ClusterClientUnreachable : IClusterClientInteraction
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="clusterClient">TBD</param>
        public ClusterClientUnreachable(IActorRef clusterClient)
        {
            ClusterClient = clusterClient;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IActorRef ClusterClient { get; }
    }

    /// <summary>
    /// Subscribe to a cluster receptionist's client interactions where
    /// it is guaranteed that a sender receives the initial state
    /// of contact points prior to any events in relation to them
    /// changing.
    /// The sender will automatically become unsubscribed when it
    /// terminates.
    /// </summary>
    public sealed class SubscribeClusterClients
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly SubscribeClusterClients Instance = new SubscribeClusterClients();
        private SubscribeClusterClients() { }
    }

    /// <summary>
    /// Explicitly unsubscribe from client interaction events.
    /// </summary>
    public sealed class UnsubscribeClusterClients
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly UnsubscribeClusterClients Instance = new UnsubscribeClusterClients();
        private UnsubscribeClusterClients() { }
    }

    /// <summary>
    /// Get the cluster clients known to this receptionist. A <see cref="ClusterClients"/> message
    /// will be replied.
    /// </summary>
    public sealed class GetClusterClients
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GetClusterClients Instance = new GetClusterClients();
        private GetClusterClients() { }
    }

    /// <summary>
    /// The reply to <see cref="GetClusterClients"/>
    /// </summary>
    public sealed class ClusterClients
    {
        /// <summary>
        /// The reply to <see cref="GetClusterClients"/>
        /// </summary>
        /// <param name="clusterClientsList">The presently known list of cluster clients.</param>
        public ClusterClients(IImmutableSet<IActorRef> clusterClientsList)
        {
            ClusterClientsList = clusterClientsList;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IImmutableSet<IActorRef> ClusterClientsList { get; }
    }

    /// <summary>
    /// <para>
    /// <see cref="ClusterClient"/> connects to this actor to retrieve. The <see cref="ClusterReceptionist"/> is
    /// supposed to be started on all nodes, or all nodes with specified role, in the cluster.
    /// The receptionist can be started with the <see cref="ClusterClientReceptionist.Get"/> or as an
    /// ordinary actor (use the factory method <see cref="ClusterReceptionist.Props"/>).
    /// </para>
    /// <para>
    /// The receptionist forwards messages from the client to the associated <see cref="DistributedPubSubMediator"/>,
    /// i.e. the client can send messages to any actor in the cluster that is registered in the
    /// <see cref="DistributedPubSubMediator"/>. Messages from the client are wrapped in <see cref="Send"/>, <see cref="SendToAll"/>
    /// or <see cref="Publish"/> with the semantics described in distributed publish/subscribe.
    /// </para>
    /// <para>
    /// Response messages from the destination actor are tunneled via the receptionist
    /// to avoid inbound connections from other cluster nodes to the client, i.e.
    /// the <see cref="ActorBase.Sender"/>, as seen by the destination actor, is not the client itself.
    /// The <see cref="ActorBase.Sender"/> of the response messages, as seen by the client, is preserved
    /// as the original sender, so the client can choose to send subsequent messages
    /// directly to the actor in the cluster.
    /// </para>
    /// </summary>
    public sealed class ClusterReceptionist : ActorBase
    {
        #region Messages

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        internal sealed class GetContacts : IClusterClientMessage, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly GetContacts Instance = new GetContacts();
            private GetContacts() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        internal sealed class Contacts : IClusterClientMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public ImmutableList<string> ContactPoints { get; }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="contactPoints">TBD</param>
            public Contacts(ImmutableList<string> contactPoints)
            {
                ContactPoints = contactPoints;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;

                if (obj is Contacts other) { return ContactPoints.SequenceEqual(other.ContactPoints); }

                return false;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 19;
                    foreach (var foo in ContactPoints)
                    {
                        hash = hash * 31 + foo.GetHashCode();
                    }
                    return hash;
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        internal sealed class Heartbeat : IClusterClientMessage, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Heartbeat Instance = new Heartbeat();
            private Heartbeat() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        internal sealed class HeartbeatRsp : IClusterClientMessage, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly HeartbeatRsp Instance = new HeartbeatRsp();
            private HeartbeatRsp() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        internal sealed class Ping : IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Ping Instance = new Ping();
            private Ping() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class CheckDeadlines
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly CheckDeadlines Instance = new CheckDeadlines();
            private CheckDeadlines() { }
        }
        #endregion

        /// <summary>
        /// Factory method for <see cref="ClusterReceptionist"/> <see cref="Actor.Props"/>.
        /// </summary>
        /// <param name="pubSubMediator">TBD</param>
        /// <param name="settings">TBD</param>
        /// <returns>TBD</returns>
        public static Props Props(IActorRef pubSubMediator, ClusterReceptionistSettings settings)
        {
            return Actor.Props.Create(() => new ClusterReceptionist(
                pubSubMediator,
                settings)).WithDeploy(Deploy.Local);
        }


        #region RingOrdering
        /// <summary>
        /// TBD
        /// </summary>
        internal class RingOrdering : IComparer<Address>
        {
            /// <summary>
            /// The singleton instance of this comparer
            /// </summary>
            public static readonly RingOrdering Instance = new RingOrdering();
            private RingOrdering() { }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="node">TBD</param>
            /// <exception cref="IllegalStateException">
            /// This exception is thrown when the specified <paramref name="node"/> has a host/port that is undefined.
            /// </exception>
            /// <returns>TBD</returns>
            public static int HashFor(Address node)
            {
                // cluster node identifier is the host and port of the address; protocol and system is assumed to be the same
                if (!string.IsNullOrEmpty(node.Host) && node.Port.HasValue)
                    return MurmurHash.StringHash(node.Host + ":" + node.Port.Value);
                else
                    throw new IllegalStateException("Unexpected address without host/port: " + node);
            }

            /// <inheritdoc/>
            public int Compare(Address x, Address y)
            {
                var ha = HashFor(x);
                var hb = HashFor(y);

                if (ha == hb) return 0;
                return ha < hb || Member.AddressOrdering.Compare(x, y) < 0 ? -1 : 1;
            }
        }
        #endregion

        private ILoggingAdapter _log;
        private readonly IActorRef _pubSubMediator;
        private readonly ClusterReceptionistSettings _settings;
        private readonly Cluster _cluster;
        private ImmutableSortedSet<Address> _nodes;
        private readonly int _virtualNodesFactor;
        private ConsistentHash<Address> _consistentHash;

        private ImmutableDictionary<IActorRef, DeadlineFailureDetector> _clientInteractions;
        private ImmutableHashSet<IActorRef> _clientsPublished;
        private ImmutableList<IActorRef> _subscribers;
        private ICancelable _checkDeadlinesTask;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="pubSubMediator">TBD</param>
        /// <param name="settings">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        public ClusterReceptionist(IActorRef pubSubMediator, ClusterReceptionistSettings settings)
        {
            _log = Context.GetLogger();
            _pubSubMediator = pubSubMediator;
            _settings = settings;
            _cluster = Cluster.Get(Context.System);

            if (!(_settings.Role == null || _cluster.SelfRoles.Contains(_settings.Role)))
            {
                throw new ArgumentException($"This cluster member [{_cluster.SelfAddress}] does not have a role [{_settings.Role}]");
            }

            _nodes = ImmutableSortedSet<Address>.Empty.WithComparer(RingOrdering.Instance);
            _virtualNodesFactor = 10;
            _consistentHash = ConsistentHash.Create(_nodes, _virtualNodesFactor);

            _clientInteractions = ImmutableDictionary<IActorRef, DeadlineFailureDetector>.Empty;
            _clientsPublished = ImmutableHashSet<IActorRef>.Empty;
            _subscribers = ImmutableList<IActorRef>.Empty;
            _checkDeadlinesTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                _settings.FailureDetectionInterval,
                _settings.FailureDetectionInterval,
                Self,
                CheckDeadlines.Instance,
                Self);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="IllegalStateException">TBD</exception>
        protected override void PreStart()
        {
            base.PreStart();
            if (_cluster.IsTerminated)
            {
                throw new IllegalStateException("Cluster node must not be terminated");
            }
            _cluster.Subscribe(Self, typeof(ClusterEvent.IMemberEvent));
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            base.PostStop();
            _cluster.Unsubscribe(Self);
            _checkDeadlinesTask.Cancel();
        }

        private bool IsMatchingRole(Member member)
        {
            return string.IsNullOrEmpty(_settings.Role) || member.HasRole(_settings.Role);
        }

        private IActorRef ResponseTunnel(IActorRef client)
        {
            var encName = Uri.EscapeDataString(client.Path.ToSerializationFormat());
            var child = Context.Child(encName);

            return !child.Equals(ActorRefs.Nobody)
                ? child
                : Context.ActorOf(Actor.Props.Create(() => new ClientResponseTunnel(client, _settings.ResponseTunnelReceiveTimeout)), encName);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            switch (message)
            {
                case PublishSubscribe.Send _:
                case PublishSubscribe.SendToAll _:
                case PublishSubscribe.Publish _:
                    var tunnel = ResponseTunnel(Sender);
                    tunnel.Tell(Ping.Instance); // keep alive
                    _pubSubMediator.Tell(message, tunnel);
                    return true;

                case Heartbeat _:
                    if (_cluster.Settings.VerboseHeartbeatLogging)
                    {
                        _log.Debug("Heartbeat from client [{0}]", Sender.Path);
                    }
                    Sender.Tell(HeartbeatRsp.Instance);
                    UpdateClientInteractions(Sender);
                    return true;

                case GetContacts _:
                    // Consistent hashing is used to ensure that the reply to GetContacts
                    // is the same from all nodes (most of the time) and it also
                    // load balances the client connections among the nodes in the cluster.
                    if (_settings.NumberOfContacts >= _nodes.Count)
                    {
                        var contacts = new Contacts(_nodes.Select(a => Self.Path.ToStringWithAddress(a)).ToImmutableList());
                        if (_log.IsDebugEnabled)
                            _log.Debug("Client [{0}] gets contactPoints [{1}] (all nodes)", Sender.Path, string.Join(", ", contacts));

                        Sender.Tell(contacts);
                    }
                    else
                    {
                        // using ToStringWithAddress in case the client is local, normally it is not, and
                        // ToStringWithAddress will use the remote address of the client
                        var addr = _consistentHash.NodeFor(Sender.Path.ToStringWithAddress(_cluster.SelfAddress));

                        var first = _nodes.From(addr).Tail().Take(_settings.NumberOfContacts).ToArray();
                        var slice = first.Length == _settings.NumberOfContacts
                            ? first
                            : first.Union(_nodes.Take(_settings.NumberOfContacts - first.Length)).ToArray();
                        var contacts = new Contacts(slice.Select(a => Self.Path.ToStringWithAddress(a)).ToImmutableList());

                        if (_log.IsDebugEnabled)
                            _log.Debug("Client [{0}] gets ContactPoints [{1}]", Sender.Path, string.Join(", ", contacts.ContactPoints));

                        Sender.Tell(contacts);
                        UpdateClientInteractions(Sender);
                    }
                    return true;

                case ClusterEvent.CurrentClusterState state:
                    _nodes = ImmutableSortedSet<Address>.Empty.WithComparer(RingOrdering.Instance)
                        .Union(state.Members
                            .Where(m => m.Status != MemberStatus.Joining && IsMatchingRole(m))
                            .Select(m => m.Address));

                    _consistentHash = ConsistentHash.Create(_nodes, _virtualNodesFactor);
                    return true;

                case ClusterEvent.MemberUp up:
                    if (IsMatchingRole(up.Member))
                    {
                        _nodes = _nodes.Add(up.Member.Address);
                        _consistentHash = ConsistentHash.Create(_nodes, _virtualNodesFactor);
                    }
                    return true;

                case ClusterEvent.MemberRemoved removed:
                    if (removed.Member.Address.Equals(_cluster.SelfAddress))
                    {
                        Context.Stop(Self);
                    }
                    else if (IsMatchingRole(removed.Member))
                    {
                        _nodes = _nodes.Remove(removed.Member.Address);
                        _consistentHash = ConsistentHash.Create(_nodes, _virtualNodesFactor);
                    }
                    return true;

                case ClusterEvent.IMemberEvent _:
                    // not of interest
                    return true;

                case SubscribeClusterClients _:
                    var subscriber = Sender;
                    subscriber.Tell(new ClusterClients(_clientInteractions.Keys.ToImmutableHashSet()));
                    _subscribers = _subscribers.Add(subscriber);
                    Context.Watch(subscriber);
                    return true;

                case UnsubscribeClusterClients _:
                    var subscriber0 = Sender;
                    _subscribers = _subscribers.Where(c => !c.Equals(subscriber0)).ToImmutableList();
                    return true;

                case Terminated terminated:
                    Self.Tell(UnsubscribeClusterClients.Instance, terminated.ActorRef);
                    return true;

                case GetClusterClients _:
                    Sender.Tell(new ClusterClients(_clientInteractions.Keys.ToImmutableHashSet()));
                    return true;

                case CheckDeadlines _:
                    _clientInteractions = _clientInteractions.Where(c => c.Value.IsAvailable).ToImmutableDictionary();
                    PublishClientsUnreachable();
                    return true;

                default:
                    return false;
            }
        }

        private void UpdateClientInteractions(IActorRef client)
        {
            if (_clientInteractions.TryGetValue(client, out var failureDetector))
                failureDetector.HeartBeat();
            else
            {
                failureDetector = new DeadlineFailureDetector(_settings.AcceptableHeartbeatPause, _settings.HeartbeatInterval);
                failureDetector.HeartBeat();
                _clientInteractions = _clientInteractions.Add(client, failureDetector);
                if (_log.IsDebugEnabled) _log.Debug($"Received new contact from [{client.Path}]");
                var clusterClientUp = new ClusterClientUp(client);
                _subscribers.ForEach(s => s.Tell(clusterClientUp));
                _clientsPublished = _clientInteractions.Keys.ToImmutableHashSet();
            }
        }

        private void PublishClientsUnreachable()
        {
            var publishableClients = _clientInteractions.Keys.ToImmutableHashSet();
            foreach (var c in _clientsPublished)
            {
                if (!publishableClients.Contains(c))
                {
                    if (_log.IsDebugEnabled) _log.Debug($"Lost contact with [{c.Path}]");
                    var clusterClientUnreachable = new ClusterClientUnreachable(c);
                    _subscribers.ForEach(s => s.Tell(clusterClientUnreachable));
                }
            }
            _clientsPublished = publishableClients;
        }
    }

    /// <summary>
    /// Replies are tunneled via this actor, child of the receptionist, to avoid inbound connections from other cluster nodes to the client.
    /// </summary>
    internal class ClientResponseTunnel : ActorBase
    {
        private readonly IActorRef _client;
        private readonly ILoggingAdapter _log;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="client">TBD</param>
        /// <param name="timeout">TBD</param>
        public ClientResponseTunnel(IActorRef client, TimeSpan timeout)
        {
            _client = client;
            _log = Context.GetLogger();
            Context.SetReceiveTimeout(timeout);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            switch (message)
            {
                case ClusterReceptionist.Ping _:
                    // keep alive from client
                    break;

                case ReceiveTimeout _:
                    if (_log.IsDebugEnabled) _log.Debug("ClientResponseTunnel for client [{0}] stopped due to inactivity", _client.Path);
                    Context.Stop(Self);
                    break;

                default:
                    _client.Tell(message, ActorRefs.NoSender);
                    if (IsAsk()) { Context.Stop(Self); }
                    break;
            }
            return true;
        }

        private bool IsAsk()
        {
            var pathElements = _client.Path.Elements;
            return pathElements.Count == 2 && string.Equals(pathElements[0], "temp", StringComparison.Ordinal) && pathElements.Last().StartsWith("$", StringComparison.Ordinal);
        }
    }
}
