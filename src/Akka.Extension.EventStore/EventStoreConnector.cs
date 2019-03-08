using System;
using Akka.Actor;
using EventStore.ClientAPI;

namespace Akka.Extension.EventStore
{
    /// <summary>TBD</summary>
    public sealed class EventStoreConnector : IExtension
    {
        private readonly ExtendedActorSystem _system;

        /// <summary>TBD</summary>
        public IEventStoreConnection2 Connection { get; }

        private void OnConnected(object sender, ClientConnectionEventArgs args)
        {
        }

        private void OnDisconnected(object sender, ClientConnectionEventArgs args)
        {
        }

        private void OnReconnecting(object sender, ClientReconnectingEventArgs args)
        {
        }

        private void OnClosed(object sender, ClientClosedEventArgs args)
        {
        }

        private void OnErrorOccurred(object sender, ClientErrorEventArgs args)
        {
        }

        private void OnAuthenticationFailed(object sender, ClientAuthenticationFailedEventArgs args)
        {
        }

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <param name="connection">TBD</param>
        public EventStoreConnector(ExtendedActorSystem system, IEventStoreConnection2 connection)
        {
            _system = system;
            _system.RegisterOnTermination(s => ((IEventStoreConnection2)s).Close(), connection);

            connection.Connected += OnConnected;
            connection.Disconnected += OnDisconnected;
            connection.Reconnecting += OnReconnecting;
            connection.Closed += OnClosed;
            connection.ErrorOccurred += OnErrorOccurred;
            connection.AuthenticationFailed += OnAuthenticationFailed;

            Connection = connection;
            connection.ConnectAsync();
        }

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public static EventStoreConnector Get(ActorSystem system)
        {
            return system.WithExtension<EventStoreConnector, EventStoreConnectorProvider>();
        }
    }

    /// <summary>TBD</summary>
    public sealed class EventStoreConnectorProvider : ExtensionIdProvider<EventStoreConnector>
    {
        public override EventStoreConnector CreateExtension(ExtendedActorSystem system)
        {
            var config = system.Settings.Config;

            var connName = config.GetString("akka.eventstore.connection-name");
            if (string.IsNullOrWhiteSpace(connName)) { connName = system.Name; }
            if (string.IsNullOrWhiteSpace(connName)) { connName = Guid.NewGuid().ToString(); }

            var builder = ConnectionSettings.Create(); // 配置默认 EventAdapter，可通过 ConnectionString 自定义配置 EventAdapter
            builder.SetEventAdapter(new AkkaEventAdapter(system.Serialization));
            var connStr = config.GetString("akka.eventstore.connection-string");
            if (string.IsNullOrWhiteSpace(connStr)) { throw new ArgumentNullException("connection-string"); }

            return new EventStoreConnector(system,
                EventStoreConnection.Create(connStr, builder, $"akka-{connName}"));
        }
    }
}
