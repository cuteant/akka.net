//-----------------------------------------------------------------------
// <copyright file="MiscMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Remote.Routing;
using Akka.Routing;
using Akka.Serialization;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt;
using Google.Protobuf;

namespace Akka.Remote.Serialization
{
    public sealed class MiscMessageSerializer : SerializerWithStringManifest
    {
        private const string IdentifyManifest = "ID";
        private const string ActorIdentityManifest = "AID";
        private const string ActorRefManifest = "AR";
        private const string PoisonPillManifest = "PP";
        private const string KillManifest = "K";
        private const string RemoteWatcherHearthbeatManifest = "RWHB";
        private const string RemoteWatcherHearthbeatRspManifest = "RWHR";
        private const string LocalScopeManifest = "LS";
        private const string RemoteScopeManifest = "RS";
        private const string ConfigManifest = "CF";
        private const string FromConfigManifest = "FC";
        private const string DefaultResizerManifest = "DR";
        private const string RoundRobinPoolManifest = "RORRP";
        private const string BroadcastPoolManifest = "ROBP";
        private const string RandomPoolManifest = "RORP";
        private const string ScatterGatherPoolManifest = "ROSGP";
        private const string TailChoppingPoolManifest = "ROTCP";
        private const string ConsistentHashingPoolManifest = "ROCHP";
        private const string RemoteRouterConfigManifest = "RORRC";

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        private readonly WrappedPayloadSupport _payloadSupport;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public MiscMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _payloadSupport = new WrappedPayloadSupport(system);
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case Identify identify:
                    return IdentifyToProto(identify);
                case ActorIdentity actorIdentity:
                    return ActorIdentityToProto(actorIdentity);
                case IActorRef actorRef:
                    return ActorRefToProto(actorRef);
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    return HeartbeatRspToProto(heartbeatRsp);
                case RemoteScope remoteScope:
                    return RemoteScopeToProto(remoteScope);
                case Config config:
                    return ConfigToProto(config);
                case FromConfig fromConfig:
                    return FromConfigToProto(fromConfig);
                case DefaultResizer defaultResizer:
                    return DefaultResizerToProto(defaultResizer);
                case RoundRobinPool roundRobinPool:
                    return RoundRobinPoolToProto(roundRobinPool);
                case BroadcastPool broadcastPool:
                    return BroadcastPoolToProto(broadcastPool);
                case RandomPool randomPool:
                    return RandomPoolToProto(randomPool);
                case ScatterGatherFirstCompletedPool scatterPool:
                    return ScatterGatherFirstCompletedPoolToProto(scatterPool);
                case TailChoppingPool tailChoppingPool:
                    return TailChoppingPoolToProto(tailChoppingPool);
                case ConsistentHashingPool hashingPool:
                    return ConsistentHashingPoolToProto(hashingPool);
                case RemoteRouterConfig remoteRouterConfig:
                    return RemoteRouterConfigToProto(remoteRouterConfig);

                case PoisonPill poisonPill:
                case Kill kill:
                case RemoteWatcher.Heartbeat heartbeat:
                case LocalScope localScope:
                    return EmptyBytes;

                default:
                    throw new ArgumentException($"Cannot serialize object of type [{obj.GetType().TypeQualifiedName()}]");
            }
        }

        /// <inheritdoc />
        public override string Manifest(object obj)
        {
            switch (obj)
            {
                case Identify identify:
                    return IdentifyManifest;
                case ActorIdentity actorIdentity:
                    return ActorIdentityManifest;
                case IActorRef actorRef:
                    return ActorRefManifest;
                case PoisonPill poisonPill:
                    return PoisonPillManifest;
                case Kill kill:
                    return KillManifest;
                case RemoteWatcher.Heartbeat heartbeat:
                    return RemoteWatcherHearthbeatManifest;
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    return RemoteWatcherHearthbeatRspManifest;
                case RemoteScope remoteScope:
                    return RemoteScopeManifest;
                case LocalScope localScope:
                    return LocalScopeManifest;
                case Config config:
                    return ConfigManifest;
                case FromConfig fromConfig:
                    return FromConfigManifest;
                case DefaultResizer defaultResizer:
                    return DefaultResizerManifest;
                case RoundRobinPool roundRobinPool:
                    return RoundRobinPoolManifest;
                case BroadcastPool broadcastPool:
                    return BroadcastPoolManifest;
                case RandomPool randomPool:
                    return RandomPoolManifest;
                case ScatterGatherFirstCompletedPool scatterPool:
                    return ScatterGatherPoolManifest;
                case TailChoppingPool tailChoppingPool:
                    return TailChoppingPoolManifest;
                case ConsistentHashingPool hashingPool:
                    return ConsistentHashingPoolManifest;
                case RemoteRouterConfig remoteRouterConfig:
                    return RemoteRouterConfigManifest;

                default:
                    throw new ArgumentException($"Cannot deserialize object of type [{obj.GetType().TypeQualifiedName()}]");
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case IdentifyManifest:
                    return IdentifyFromProto(bytes);
                case ActorIdentityManifest:
                    return ActorIdentityFromProto(bytes);
                case ActorRefManifest:
                    return ActorRefFromProto(bytes);
                case PoisonPillManifest:
                    return PoisonPill.Instance;
                case KillManifest:
                    return Kill.Instance;
                case RemoteWatcherHearthbeatManifest:
                    return RemoteWatcher.Heartbeat.Instance;
                case RemoteWatcherHearthbeatRspManifest:
                    return HearthbeatRspFromProto(bytes);
                case LocalScopeManifest:
                    return LocalScope.Instance;
                case RemoteScopeManifest:
                    return RemoteScopeFromProto(bytes);
                case ConfigManifest:
                    return ConfigFromProto(bytes);
                case FromConfigManifest:
                    return FromConfigFromProto(bytes);
                case DefaultResizerManifest:
                    return DefaultResizerFromProto(bytes);
                case RoundRobinPoolManifest:
                    return RoundRobinPoolFromProto(bytes);
                case BroadcastPoolManifest:
                    return BroadcastPoolFromProto(bytes);
                case RandomPoolManifest:
                    return RandomPoolFromProto(bytes);
                case ScatterGatherPoolManifest:
                    return ScatterGatherFirstCompletedPoolFromProto(bytes);
                case TailChoppingPoolManifest:
                    return TailChoppingPoolFromProto(bytes);
                case ConsistentHashingPoolManifest:
                    return ConsistentHashingPoolFromProto(bytes);
                case RemoteRouterConfigManifest:
                    return RemoteRouterConfigFromProto(bytes);

                default:
                    throw new SerializationException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(MiscMessageSerializer)}]");
            }
        }

        //
        // Identify
        //
        private byte[] IdentifyToProto(Identify identify)
        {
            var protoIdentify = new Proto.Msg.Identify();
            if (identify.MessageId != null)
            {
                protoIdentify.MessageId = _payloadSupport.PayloadToProto(identify.MessageId);
            }

            return protoIdentify.ToByteArray();
        }

        private Identify IdentifyFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.Identify.Parser.ParseFrom(bytes);
            if (protoMessage.MessageId == null)
            {
                return new Identify(null);
            }

            return new Identify(_payloadSupport.PayloadFrom(protoMessage.MessageId));
        }

        //
        // ActorIdentity
        //
        private byte[] ActorIdentityToProto(ActorIdentity actorIdentity)
        {
            var protoIdentify = new Proto.Msg.ActorIdentity
            {
                CorrelationId = _payloadSupport.PayloadToProto(actorIdentity.MessageId),
                Path = Akka.Serialization.Serialization.SerializedActorPath(actorIdentity.Subject)
            };
            return protoIdentify.ToByteArray();
        }

        private ActorIdentity ActorIdentityFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.ActorIdentity.Parser.ParseFrom(bytes);
            return new ActorIdentity(_payloadSupport.PayloadFrom(protoMessage.CorrelationId), ResolveActorRef(protoMessage.Path));
        }

        private const string c_nobody = "nobody";
        //
        // IActorRef
        //
        private byte[] ActorRefToProto(IActorRef actorRef)
        {
            var protoActor = new Proto.Msg.ActorRefData();
            if (actorRef is Nobody) // TODO: this is a hack. Should work without it
            {
                protoActor.Path = c_nobody;
            }
            else
            {
                protoActor.Path = Akka.Serialization.Serialization.SerializedActorPath(actorRef);
            }

            return protoActor.ToByteArray();
        }

        private IActorRef ActorRefFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.ActorRefData.Parser.ParseFrom(bytes);
            if (string.Equals(protoMessage.Path, c_nobody, StringComparison.Ordinal))
            {
                return Nobody.Instance;
            }

            return system.AsInstanceOf<ExtendedActorSystem>().Provider.ResolveActorRef(protoMessage.Path);
        }

        //
        // RemoteWatcher.HeartbeatRsp
        //
        private byte[] HeartbeatRspToProto(RemoteWatcher.HeartbeatRsp heartbeatRsp)
        {
            var message = new Proto.Msg.RemoteWatcherHeartbeatResponse
            {
                Uid = (ulong)heartbeatRsp.AddressUid // TODO: change to uint32
            };
            return message.ToByteArray();
        }

        private RemoteWatcher.HeartbeatRsp HearthbeatRspFromProto(byte[] bytes)
        {
            var message = Proto.Msg.RemoteWatcherHeartbeatResponse.Parser.ParseFrom(bytes);
            return new RemoteWatcher.HeartbeatRsp((int)message.Uid);
        }

        //
        // RemoteScope
        //
        private byte[] RemoteScopeToProto(RemoteScope remoteScope)
        {
            var message = new Proto.Msg.RemoteScope
            {
                Node = AddressMessageBuilder(remoteScope.Address)
            };
            return message.ToByteArray();
        }

        private RemoteScope RemoteScopeFromProto(byte[] bytes)
        {
            var message = Proto.Msg.RemoteScope.Parser.ParseFrom(bytes);
            return new RemoteScope(AddressFrom(message.Node));
        }

        //
        // Config
        //
        private byte[] ConfigToProto(Config config)
        {
            if (config.IsEmpty) { return EmptyBytes; }

            return Encoding.UTF8.GetBytes(config.Root.ToString());
        }

        private Config ConfigFromProto(byte[] bytes)
        {
            if (bytes.Length == 0) { return Config.Empty; }

            return ConfigurationFactory.ParseString(Encoding.UTF8.GetString(bytes));
        }

        //
        // FromConfig
        //
        private byte[] FromConfigToProto(FromConfig fromConfig)
        {
            if (fromConfig == FromConfig.Instance) { return EmptyBytes; }

            var message = new Proto.Msg.FromConfig();

            if (fromConfig.Resizer != null)
            {
                message.Resizer = _payloadSupport.PayloadToProto(fromConfig.Resizer);
            }

            if (!string.IsNullOrEmpty(fromConfig.RouterDispatcher))
            {
                message.RouterDispatcher = fromConfig.RouterDispatcher;
            }

            return message.ToByteArray();
        }

        private FromConfig FromConfigFromProto(byte[] bytes)
        {
            if (bytes.Length == 0) { return FromConfig.Instance; }

            var fromConfig = Proto.Msg.FromConfig.Parser.ParseFrom(bytes);

            Resizer resizer = fromConfig.Resizer != null
                ? (Resizer)_payloadSupport.PayloadFrom(fromConfig.Resizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(fromConfig.RouterDispatcher)
                ? fromConfig.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new FromConfig(resizer, Pool.DefaultSupervisorStrategy, routerDispatcher);
        }

        //
        // DefaultResizer
        //
        private byte[] DefaultResizerToProto(DefaultResizer defaultResizer)
        {
            var message = new Proto.Msg.DefaultResizer
            {
                LowerBound = (uint)defaultResizer.LowerBound,
                UpperBound = (uint)defaultResizer.UpperBound,
                PressureThreshold = (uint)defaultResizer.PressureThreshold,
                RampupRate = defaultResizer.RampupRate,
                BackoffThreshold = defaultResizer.BackoffThreshold,
                BackoffRate = defaultResizer.BackoffRate,
                MessagesPerResize = (uint)defaultResizer.MessagesPerResize
            };
            return message.ToByteArray();
        }

        private DefaultResizer DefaultResizerFromProto(byte[] bytes)
        {
            var resizer = Proto.Msg.DefaultResizer.Parser.ParseFrom(bytes);
            return new DefaultResizer(
                (int)resizer.LowerBound,
                (int)resizer.UpperBound,
                (int)resizer.PressureThreshold,
                resizer.RampupRate,
                resizer.BackoffThreshold,
                resizer.BackoffRate,
                (int)resizer.MessagesPerResize);
        }

        //
        // Generic Routing Pool
        //
        private Proto.Msg.GenericRoutingPool GenericRoutingPoolBuilder(Pool pool)
        {
            var message = new Proto.Msg.GenericRoutingPool
            {
                NrOfInstances = (uint)pool.NrOfInstances
            };
            if (!string.IsNullOrEmpty(pool.RouterDispatcher))
            {
                message.RouterDispatcher = pool.RouterDispatcher;
            }
            if (pool.Resizer != null)
            {
                message.Resizer = _payloadSupport.PayloadToProto(pool.Resizer);
            }

            message.UsePoolDispatcher = pool.UsePoolDispatcher;
            return message;
        }

        //
        // RoundRobinPool
        //
        private byte[] RoundRobinPoolToProto(RoundRobinPool roundRobinPool)
        {
            return GenericRoutingPoolBuilder(roundRobinPool).ToByteArray();
        }

        private RoundRobinPool RoundRobinPoolFromProto(byte[] bytes)
        {
            var broadcastPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = broadcastPool.Resizer != null
                ? (Resizer)_payloadSupport.PayloadFrom(broadcastPool.Resizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(broadcastPool.RouterDispatcher)
                ? broadcastPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new RoundRobinPool(
                (int)broadcastPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                broadcastPool.UsePoolDispatcher);
        }

        //
        // BroadcastPool
        //
        private byte[] BroadcastPoolToProto(BroadcastPool broadcastPool)
        {
            return GenericRoutingPoolBuilder(broadcastPool).ToByteArray();
        }

        private BroadcastPool BroadcastPoolFromProto(byte[] bytes)
        {
            var broadcastPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = broadcastPool.Resizer != null 
                ? (Resizer)_payloadSupport.PayloadFrom(broadcastPool.Resizer)
                : null;
            var routerDispatcher = !string.IsNullOrEmpty(broadcastPool.RouterDispatcher)
                ? broadcastPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new BroadcastPool(
                (int)broadcastPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                broadcastPool.UsePoolDispatcher);
        }

        //
        // RandomPool
        //
        private byte[] RandomPoolToProto(RandomPool randomPool)
        {
            return GenericRoutingPoolBuilder(randomPool).ToByteArray();
        }

        private RandomPool RandomPoolFromProto(byte[] bytes)
        {
            var randomPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = randomPool.Resizer != null
                ? (Resizer)_payloadSupport.PayloadFrom(randomPool.Resizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(randomPool.RouterDispatcher)
                ? randomPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new RandomPool(
                (int)randomPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                randomPool.UsePoolDispatcher);
        }

        //
        // ScatterGatherFirstCompletedPool
        //
        private byte[] ScatterGatherFirstCompletedPoolToProto(ScatterGatherFirstCompletedPool scatterGatherFirstCompletedPool)
        {
            var message = new Proto.Msg.ScatterGatherPool
            {
                Generic = GenericRoutingPoolBuilder(scatterGatherFirstCompletedPool),
                Within = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(scatterGatherFirstCompletedPool.Within)
            };
            return message.ToByteArray();
        }

        private ScatterGatherFirstCompletedPool ScatterGatherFirstCompletedPoolFromProto(byte[] bytes)
        {
            var scatterGatherFirstCompletedPool = Proto.Msg.ScatterGatherPool.Parser.ParseFrom(bytes);

            Resizer resizer = scatterGatherFirstCompletedPool.Generic.Resizer != null
                ? (Resizer)_payloadSupport.PayloadFrom(scatterGatherFirstCompletedPool.Generic.Resizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(scatterGatherFirstCompletedPool.Generic.RouterDispatcher)
                ? scatterGatherFirstCompletedPool.Generic.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new ScatterGatherFirstCompletedPool(
                (int)scatterGatherFirstCompletedPool.Generic.NrOfInstances,
                resizer,
                scatterGatherFirstCompletedPool.Within.ToTimeSpan(),
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                scatterGatherFirstCompletedPool.Generic.UsePoolDispatcher);
        }

        //
        // TailChoppingPool
        //
        private byte[] TailChoppingPoolToProto(TailChoppingPool tailChoppingPool)
        {
            var message = new Proto.Msg.TailChoppingPool
            {
                Generic = GenericRoutingPoolBuilder(tailChoppingPool),
                Within = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(tailChoppingPool.Within),
                Interval = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(tailChoppingPool.Interval)
            };
            return message.ToByteArray();
        }

        private TailChoppingPool TailChoppingPoolFromProto(byte[] bytes)
        {
            var tailChoppingPool = Proto.Msg.TailChoppingPool.Parser.ParseFrom(bytes);

            Resizer resizer = tailChoppingPool.Generic.Resizer != null
                ? (Resizer)_payloadSupport.PayloadFrom(tailChoppingPool.Generic.Resizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(tailChoppingPool.Generic.RouterDispatcher)
                ? tailChoppingPool.Generic.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new TailChoppingPool(
                (int)tailChoppingPool.Generic.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                tailChoppingPool.Within.ToTimeSpan(),
                tailChoppingPool.Interval.ToTimeSpan(),
                tailChoppingPool.Generic.UsePoolDispatcher);
        }

        //
        // ConsistentHashingPool
        //
        private byte[] ConsistentHashingPoolToProto(ConsistentHashingPool hashingPool)
        {
            return GenericRoutingPoolBuilder(hashingPool).ToByteArray();
        }

        private object ConsistentHashingPoolFromProto(byte[] bytes)
        {
            var consistentHashingPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = consistentHashingPool.Resizer != null
                ? (Resizer)_payloadSupport.PayloadFrom(consistentHashingPool.Resizer)
                : null;
            var routerDispatcher = !string.IsNullOrEmpty(consistentHashingPool.RouterDispatcher)
                ? consistentHashingPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new ConsistentHashingPool(
                (int)consistentHashingPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                consistentHashingPool.UsePoolDispatcher);
        }

        //
        // RemoteRouterConfig
        //
        private byte[] RemoteRouterConfigToProto(RemoteRouterConfig remoteRouterConfig)
        {
            var protoRemoteRouterConfig = new Proto.Msg.RemoteRouterConfig
            {
                Local = _payloadSupport.PayloadToProto(remoteRouterConfig.Local)
            };
            protoRemoteRouterConfig.Nodes.AddRange(remoteRouterConfig.Nodes.Select(AddressMessageBuilder));
            return protoRemoteRouterConfig.ToByteArray();
        }

        private RemoteRouterConfig RemoteRouterConfigFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.RemoteRouterConfig.Parser.ParseFrom(bytes);
            return new RemoteRouterConfig(_payloadSupport.PayloadFrom(protoMessage.Local).AsInstanceOf<Pool>(), protoMessage.Nodes.Select(AddressFrom));
        }

        //
        // Address
        //
        private static Proto.Msg.AddressData AddressMessageBuilder(Address address)
        {
            var message = new Proto.Msg.AddressData
            {
                System = address.System,
                Hostname = address.Host,
                Port = (uint)(address.Port ?? 0),
                Protocol = address.Protocol
            };
            return message;
        }

        private static Address AddressFrom(Proto.Msg.AddressData addressProto)
        {
            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Hostname,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        private IActorRef ResolveActorRef(string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return system.Provider.ResolveActorRef(path);
        }
    }
}
