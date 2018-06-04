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
using CuteAnt.Text;

namespace Akka.Remote.Serialization
{
    public sealed class MiscMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string IdentifyManifest = "ID";
        private static readonly byte[] IdentifyManifestBytes = StringHelper.UTF8NoBOM.GetBytes(IdentifyManifest);
        private const string ActorIdentityManifest = "AID";
        private static readonly byte[] ActorIdentityManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ActorIdentityManifest);
        private const string ActorRefManifest = "AR";
        private static readonly byte[] ActorRefManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ActorRefManifest);
        private const string PoisonPillManifest = "PP";
        private static readonly byte[] PoisonPillManifestBytes = StringHelper.UTF8NoBOM.GetBytes(PoisonPillManifest);
        private const string KillManifest = "K";
        private static readonly byte[] KillManifestBytes = StringHelper.UTF8NoBOM.GetBytes(KillManifest);
        private const string RemoteWatcherHearthbeatManifest = "RWHB";
        private static readonly byte[] RemoteWatcherHearthbeatManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteWatcherHearthbeatManifest);
        private const string RemoteWatcherHearthbeatRspManifest = "RWHR";
        private static readonly byte[] RemoteWatcherHearthbeatRspManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteWatcherHearthbeatRspManifest);
        private const string LocalScopeManifest = "LS";
        private static readonly byte[] LocalScopeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(LocalScopeManifest);
        private const string RemoteScopeManifest = "RS";
        private static readonly byte[] RemoteScopeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteScopeManifest);
        private const string ConfigManifest = "CF";
        private static readonly byte[] ConfigManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ConfigManifest);
        private const string FromConfigManifest = "FC";
        private static readonly byte[] FromConfigManifestBytes = StringHelper.UTF8NoBOM.GetBytes(FromConfigManifest);
        private const string DefaultResizerManifest = "DR";
        private static readonly byte[] DefaultResizerManifestBytes = StringHelper.UTF8NoBOM.GetBytes(DefaultResizerManifest);
        private const string RoundRobinPoolManifest = "RORRP";
        private static readonly byte[] RoundRobinPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RoundRobinPoolManifest);
        private const string BroadcastPoolManifest = "ROBP";
        private static readonly byte[] BroadcastPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(BroadcastPoolManifest);
        private const string RandomPoolManifest = "RORP";
        private static readonly byte[] RandomPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RandomPoolManifest);
        private const string ScatterGatherPoolManifest = "ROSGP";
        private static readonly byte[] ScatterGatherPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ScatterGatherPoolManifest);
        private const string TailChoppingPoolManifest = "ROTCP";
        private static readonly byte[] TailChoppingPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(TailChoppingPoolManifest);
        private const string ConsistentHashingPoolManifest = "ROCHP";
        private static readonly byte[] ConsistentHashingPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ConsistentHashingPoolManifest);
        private const string RemoteRouterConfigManifest = "RORRC";
        private static readonly byte[] RemoteRouterConfigManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteRouterConfigManifest);

        #endregion

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public MiscMessageSerializer(ExtendedActorSystem system) : base(system) { }

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
        public override byte[] ManifestBytes(object obj)
        {
            switch (obj)
            {
                case Identify identify:
                    return IdentifyManifestBytes;
                case ActorIdentity actorIdentity:
                    return ActorIdentityManifestBytes;
                case IActorRef actorRef:
                    return ActorRefManifestBytes;
                case PoisonPill poisonPill:
                    return PoisonPillManifestBytes;
                case Kill kill:
                    return KillManifestBytes;
                case RemoteWatcher.Heartbeat heartbeat:
                    return RemoteWatcherHearthbeatManifestBytes;
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    return RemoteWatcherHearthbeatRspManifestBytes;
                case RemoteScope remoteScope:
                    return RemoteScopeManifestBytes;
                case LocalScope localScope:
                    return LocalScopeManifestBytes;
                case Config config:
                    return ConfigManifestBytes;
                case FromConfig fromConfig:
                    return FromConfigManifestBytes;
                case DefaultResizer defaultResizer:
                    return DefaultResizerManifestBytes;
                case RoundRobinPool roundRobinPool:
                    return RoundRobinPoolManifestBytes;
                case BroadcastPool broadcastPool:
                    return BroadcastPoolManifestBytes;
                case RandomPool randomPool:
                    return RandomPoolManifestBytes;
                case ScatterGatherFirstCompletedPool scatterPool:
                    return ScatterGatherPoolManifestBytes;
                case TailChoppingPool tailChoppingPool:
                    return TailChoppingPoolManifestBytes;
                case ConsistentHashingPool hashingPool:
                    return ConsistentHashingPoolManifestBytes;
                case RemoteRouterConfig remoteRouterConfig:
                    return RemoteRouterConfigManifestBytes;

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
                protoIdentify.MessageId = WrappedPayloadSupport.PayloadToProto(system, identify.MessageId);
            }

            return protoIdentify.ToArray();
        }

        private Identify IdentifyFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.Identify.Parser.ParseFrom(bytes);
            if (protoMessage.MessageId == null)
            {
                return new Identify(null);
            }

            return new Identify(WrappedPayloadSupport.PayloadFrom(system, protoMessage.MessageId));
        }

        //
        // ActorIdentity
        //
        private byte[] ActorIdentityToProto(ActorIdentity actorIdentity)
        {
            var protoIdentify = new Proto.Msg.ActorIdentity
            {
                CorrelationId = WrappedPayloadSupport.PayloadToProto(system, actorIdentity.MessageId),
                Path = Akka.Serialization.Serialization.SerializedActorPath(actorIdentity.Subject)
            };
            return protoIdentify.ToArray();
        }

        private ActorIdentity ActorIdentityFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.ActorIdentity.Parser.ParseFrom(bytes);
            return new ActorIdentity(WrappedPayloadSupport.PayloadFrom(system, protoMessage.CorrelationId), ResolveActorRef(protoMessage.Path));
        }

        private const string c_nobody = "nobody";
        //
        // IActorRef
        //
        private static byte[] ActorRefToProto(IActorRef actorRef)
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

            return protoActor.ToArray();
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
        private static byte[] HeartbeatRspToProto(RemoteWatcher.HeartbeatRsp heartbeatRsp)
        {
            var message = new Proto.Msg.RemoteWatcherHeartbeatResponse
            {
                Uid = (ulong)heartbeatRsp.AddressUid // TODO: change to uint32
            };
            return message.ToArray();
        }

        private static RemoteWatcher.HeartbeatRsp HearthbeatRspFromProto(byte[] bytes)
        {
            var message = Proto.Msg.RemoteWatcherHeartbeatResponse.Parser.ParseFrom(bytes);
            return new RemoteWatcher.HeartbeatRsp((int)message.Uid);
        }

        //
        // RemoteScope
        //
        private static byte[] RemoteScopeToProto(RemoteScope remoteScope)
        {
            var message = new Proto.Msg.RemoteScope
            {
                Node = AddressMessageBuilder(remoteScope.Address)
            };
            return message.ToArray();
        }

        private static RemoteScope RemoteScopeFromProto(byte[] bytes)
        {
            var message = Proto.Msg.RemoteScope.Parser.ParseFrom(bytes);
            return new RemoteScope(AddressFrom(message.Node));
        }

        //
        // Config
        //
        private static byte[] ConfigToProto(Config config)
        {
            if (config.IsEmpty) { return EmptyBytes; }

            return Encoding.UTF8.GetBytes(config.Root.ToString());
        }

        private static Config ConfigFromProto(byte[] bytes)
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
                message.Resizer = WrappedPayloadSupport.PayloadToProto(system, fromConfig.Resizer);
            }

            if (!string.IsNullOrEmpty(fromConfig.RouterDispatcher))
            {
                message.RouterDispatcher = fromConfig.RouterDispatcher;
            }

            return message.ToArray();
        }

        private FromConfig FromConfigFromProto(byte[] bytes)
        {
            if (bytes.Length == 0) { return FromConfig.Instance; }

            var fromConfig = Proto.Msg.FromConfig.Parser.ParseFrom(bytes);

            Resizer resizer = fromConfig.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, fromConfig.Resizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(fromConfig.RouterDispatcher)
                ? fromConfig.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new FromConfig(resizer, Pool.DefaultSupervisorStrategy, routerDispatcher);
        }

        //
        // DefaultResizer
        //
        private static byte[] DefaultResizerToProto(DefaultResizer defaultResizer)
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
            return message.ToArray();
        }

        private static DefaultResizer DefaultResizerFromProto(byte[] bytes)
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
                message.Resizer = WrappedPayloadSupport.PayloadToProto(system, pool.Resizer);
            }

            message.UsePoolDispatcher = pool.UsePoolDispatcher;
            return message;
        }

        //
        // RoundRobinPool
        //
        private byte[] RoundRobinPoolToProto(RoundRobinPool roundRobinPool)
        {
            return GenericRoutingPoolBuilder(roundRobinPool).ToArray();
        }

        private RoundRobinPool RoundRobinPoolFromProto(byte[] bytes)
        {
            var broadcastPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = broadcastPool.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, broadcastPool.Resizer)
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
            return GenericRoutingPoolBuilder(broadcastPool).ToArray();
        }

        private BroadcastPool BroadcastPoolFromProto(byte[] bytes)
        {
            var broadcastPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = broadcastPool.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, broadcastPool.Resizer)
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
            return GenericRoutingPoolBuilder(randomPool).ToArray();
        }

        private RandomPool RandomPoolFromProto(byte[] bytes)
        {
            var randomPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = randomPool.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, randomPool.Resizer)
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
            return message.ToArray();
        }

        private ScatterGatherFirstCompletedPool ScatterGatherFirstCompletedPoolFromProto(byte[] bytes)
        {
            var scatterGatherFirstCompletedPool = Proto.Msg.ScatterGatherPool.Parser.ParseFrom(bytes);

            Resizer resizer = scatterGatherFirstCompletedPool.Generic.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, scatterGatherFirstCompletedPool.Generic.Resizer)
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
            return message.ToArray();
        }

        private TailChoppingPool TailChoppingPoolFromProto(byte[] bytes)
        {
            var tailChoppingPool = Proto.Msg.TailChoppingPool.Parser.ParseFrom(bytes);

            Resizer resizer = tailChoppingPool.Generic.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, tailChoppingPool.Generic.Resizer)
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
            return GenericRoutingPoolBuilder(hashingPool).ToArray();
        }

        private object ConsistentHashingPoolFromProto(byte[] bytes)
        {
            var consistentHashingPool = Proto.Msg.GenericRoutingPool.Parser.ParseFrom(bytes);

            Resizer resizer = consistentHashingPool.Resizer != null
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, consistentHashingPool.Resizer)
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
                Local = WrappedPayloadSupport.PayloadToProto(system, remoteRouterConfig.Local)
            };
            protoRemoteRouterConfig.Nodes.AddRange(remoteRouterConfig.Nodes.Select(AddressMessageBuilder));
            return protoRemoteRouterConfig.ToArray();
        }

        private RemoteRouterConfig RemoteRouterConfigFromProto(byte[] bytes)
        {
            var protoMessage = Proto.Msg.RemoteRouterConfig.Parser.ParseFrom(bytes);
            return new RemoteRouterConfig(WrappedPayloadSupport.PayloadFrom(system, protoMessage.Local).AsInstanceOf<Pool>(), protoMessage.Nodes.Select(AddressFrom));
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
