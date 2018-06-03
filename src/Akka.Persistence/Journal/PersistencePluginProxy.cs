//-----------------------------------------------------------------------
// <copyright file="PersistencePluginProxy.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;

namespace Akka.Persistence.Journal
{
    /// <summary>
    /// TBD
    /// </summary>
    public class PersistencePluginProxy : ActorBase, IWithUnboundedStash
    {
        /// <summary>
        /// TBD
        /// </summary>
        public sealed class TargetLocation
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="address">TBD</param>
            public TargetLocation(Address address)
            {
                Address = address;
            }

            /// <summary>
            /// TBD
            /// </summary>
            public Address Address { get; private set; }
        }

        private sealed class InitTimeout : ISingletonMessage
        {
            public static readonly InitTimeout Instance = new InitTimeout();
            private InitTimeout() { }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        /// <param name="address">TBD</param>
        public static void SetTargetLocation(ActorSystem system, Address address)
        {
            var persistence = Persistence.Instance.Apply(system);
            persistence.JournalFor(null).Tell(new TargetLocation(address));
            if (string.IsNullOrEmpty(system.Settings.Config.GetString("akka.persistence.snapshot-store.plugin")))
                persistence.SnapshotStoreFor(null).Tell(new TargetLocation(address));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public static void Start(ActorSystem system)
        {
            var persistence = Persistence.Instance.Apply(system);
            persistence.JournalFor(null);
            if (string.IsNullOrEmpty(system.Settings.Config.GetString("akka.persistence.snapshot-store.plugin")))
                persistence.SnapshotStoreFor(null);
        }

        private interface IPluginType
        {
            string Qualifier { get; }
        }

        private class Journal : IPluginType
        {
            public string Qualifier => "journal";
        }

        private class SnapshotStore : IPluginType
        {
            public string Qualifier => "snapshot-store";
        }

        private readonly Config _config;
        private readonly IPluginType _pluginType;
        private readonly TimeSpan _initTimeout;
        private readonly string _targetPluginId;
        private readonly bool _startTarget;
        private readonly Address _selfAddress;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistencePluginProxy"/> class.
        /// </summary>
        /// <param name="config">The configuration used to configure the proxy.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when configuration is undefined for the plugin
        /// or an unknown plugin type is defined.
        /// </exception>
        public PersistencePluginProxy(Config config)
        {
            _config = config;
            var pluginId = Self.Path.Name;
            if (string.Equals(pluginId, "akka.persistence.journal.proxy", StringComparison.Ordinal))
                _pluginType = new Journal();
            else if (string.Equals(pluginId, "akka.persistence.snapshot-store.proxy", StringComparison.Ordinal))
                _pluginType = new SnapshotStore();
            else
                throw new ArgumentException($"Unknown plugin type: {pluginId}.");
            _initTimeout = config.GetTimeSpan("init-timeout");
            var key = "target-" + _pluginType.Qualifier + "-plugin";
            _targetPluginId = config.GetString(key);
            if (string.IsNullOrEmpty(_targetPluginId))
                throw new ArgumentException($"{pluginId}.{key} must be defined.");
            _startTarget = config.GetBoolean("start-target-" + _pluginType.Qualifier);

            _selfAddress = ((ExtendedActorSystem)Context.System).Provider.DefaultAddress;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PreStart()
        {
            if (_startTarget)
            {
                IActorRef target = null;
                switch (_pluginType)
                {
                    case Journal _:
                        if (_log.IsInfoEnabled) { _log.Info("Starting target journal [{0}]", _targetPluginId); }
                        target = Persistence.Instance.Apply(Context.System).JournalFor(_targetPluginId);
                        break;

                    case SnapshotStore _:
                        if (_log.IsInfoEnabled) { _log.Info("Starting target snapshot-store [{0}]", _targetPluginId); }
                        target = Persistence.Instance.Apply(Context.System).SnapshotStoreFor(_targetPluginId);
                        break;

                    default:
                        break;
                }
                Context.Become(Active(target, true));
            }
            else
            {
                var targetAddressKey = "target-" + _pluginType.Qualifier + "-address";
                var targetAddress = _config.GetString(targetAddressKey);
                if (!string.IsNullOrEmpty(targetAddress))
                {
                    try
                    {
                        if (_log.IsInfoEnabled) { _log.Info("Setting target {0} address to {1}", _pluginType.Qualifier, targetAddress); }
                        SetTargetLocation(Context.System, Address.Parse(targetAddress));
                    }
                    catch (UriFormatException)
                    {
                        if (_log.IsWarningEnabled)
                        {
                            _log.Warning("Invalid URL provided for target {0} address: {1}", _pluginType.Qualifier, targetAddress);
                        }
                    }
                }
                Context.System.Scheduler.ScheduleTellOnce(_initTimeout, Self, InitTimeout.Instance, Self);
            }
            base.PreStart();
        }

        private TimeoutException TimeoutException()
        {
            return
                new TimeoutException(
                    $"Target {_pluginType.Qualifier} not initialized. Use `PersistencePluginProxy.SetTargetLocation` or set `target-{_pluginType.Qualifier}-address`.");
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            return Init(message);
        }

        private bool Init(object message)
        {
            switch (message)
            {
                case TargetLocation tl:
                    Context.SetReceiveTimeout(TimeSpan.FromSeconds(1)); // for retries
                    Context.Become(Identifying(((TargetLocation)message).Address));
                    break;
                case InitTimeout _:
                    if (_log.IsInfoEnabled)
                    {
                        _log.Info("Initialization timed-out (after {0}s), use `PersistencePluginProxy.SetTargetLocation` or set `target-{1}-address`",
                            _initTimeout.TotalSeconds, _pluginType.Qualifier);
                    }
                    Context.Become(InitTimedOut());
                    Stash.UnstashAll(); // will trigger appropriate failures
                    break;
                case Terminated _:
                    break;
                default:
                    Stash.Stash();
                    break;
            }
            return true;
        }

        private void BecomeIdentifying(Address address)
        {
            SendIdentify(address);
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(1)); // for retries
            Context.Become(Identifying(address));
        }

        private void SendIdentify(Address address)
        {
            var sel = Context.ActorSelection($"{new RootActorPath(address)}/system/{_targetPluginId}");
            if (_log.IsInfoEnabled) { _log.Info("Trying to identify target + {0} at {1}", _pluginType.Qualifier, sel); }
            sel.Tell(new Identify(_targetPluginId));
        }

        private Receive Identifying(Address address)
        {
            return message =>
            {
                switch (message)
                {
                    case ActorIdentity ai:
                        if (_targetPluginId.Equals(ai.MessageId))
                        {
                            var target = ai.Subject;
                            if (_log.IsInfoEnabled) { _log.Info("Found target {0} at [{1}]", _pluginType.Qualifier, address); }
                            Context.SetReceiveTimeout(null);
                            Context.Watch(target);
                            Stash.UnstashAll();
                            Context.Become(Active(target, address.Equals(_selfAddress)));
                        }
                        else
                        {
                            // will retry after ReceiveTimeout
                        }
                        return true;
                    case Terminated _:
                        return true;
                    case ReceiveTimeout _:
                        SendIdentify(address);
                        return true;
                    default:
                        return Init(message); ;
                }
            };
        }

        private Receive Active(IActorRef targetJournal, bool targetAtThisNode)
        {
            return message =>
            {
                switch (message)
                {
                    case TargetLocation tl:
                        var address = tl.Address;
                        if (targetAtThisNode && !address.Equals(_selfAddress)) { BecomeIdentifying(address); }
                        break;
                    case Terminated t:
                        if (t.ActorRef.Equals(targetJournal))
                        {
                            Context.Unwatch(targetJournal);
                            Context.Become(InitTimedOut());
                        }
                        break;
                    case InitTimeout _:
                        break;
                    default:
                        targetJournal.Forward(message);
                        break;
                }
                return true;
            };
        }

        private Receive InitTimedOut()
        {
            return message =>
            {
                switch (message)
                {
                    case IJournalRequest _:
                        // exhaustive match
                        switch (message)
                        {
                            case WriteMessages w:
                                w.PersistentActor.Tell(new WriteMessagesFailed(TimeoutException()));
                                foreach (var m in w.Messages)
                                {
                                    if (m is AtomicWrite)
                                    {
                                        foreach (var p in (IEnumerable<IPersistentRepresentation>)m.Payload)
                                        {
                                            w.PersistentActor.Tell(new WriteMessageFailure(p, TimeoutException(),
                                                w.ActorInstanceId));
                                        }
                                    }
                                    else if (m is NonPersistentMessage)
                                    {
                                        w.PersistentActor.Tell(new LoopMessageSuccess(m.Payload, w.ActorInstanceId));
                                    }
                                }
                                break;
                            case ReplayMessages r:
                                r.PersistentActor.Tell(new ReplayMessagesFailure(TimeoutException()));
                                break;
                            case DeleteMessagesTo d:
                                d.PersistentActor.Tell(new DeleteMessagesFailure(TimeoutException(), d.ToSequenceNr));
                                break;
                            default:
                                break;
                        }
                        break;
                    case ISnapshotRequest _:
                        // exhaustive match
                        switch (message)
                        {
                            case LoadSnapshot l:
                                Sender.Tell(new LoadSnapshotFailed(TimeoutException()));
                                break;
                            case SaveSnapshot s:
                                Sender.Tell(new SaveSnapshotFailure(s.Metadata, TimeoutException()));
                                break;
                            case DeleteSnapshot d:
                                Sender.Tell(new DeleteSnapshotFailure(d.Metadata, TimeoutException()));
                                break;
                            case DeleteSnapshots ds:
                                Sender.Tell(new DeleteSnapshotsFailure(ds.Criteria, TimeoutException()));
                                break;
                            default:
                                break;
                        }
                        break;
                    case TargetLocation tl:
                        BecomeIdentifying(tl.Address);
                        break;
                    case Terminated _:
                        break;
                    default:
                        var exception = TimeoutException();
                        if (_log.IsErrorEnabled) { _log.Error(exception, "Failed PersistencePluginProxyRequest: {0}", exception.Message); }
                        break;
                }
                return true;
            };
        }
    }

    /// <summary>
    /// <see cref="PersistencePluginProxyExtension"/> is an <see cref="IExtension"/> that enables initialization
    /// of the <see cref="PersistencePluginProxy"/> via configuration, without requiring any code changes or the
    /// creation of any actors.
    /// </summary>
    public class PersistencePluginProxyExtension : ExtensionIdProvider<PersistencePluginProxyExtension>, IExtension
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public PersistencePluginProxyExtension(ActorSystem system)
        {
            PersistencePluginProxy.Start(system);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public override PersistencePluginProxyExtension CreateExtension(ExtendedActorSystem system)
        {
            return new PersistencePluginProxyExtension(system);
        }
    }
}
