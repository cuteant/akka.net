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
using MessagePack;

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

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

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

                case PoisonPill _:
                case Kill _:
                case RemoteWatcher.Heartbeat _:
                case LocalScope _:
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
                case Identify _:
                    return IdentifyManifest;
                case ActorIdentity _:
                    return ActorIdentityManifest;
                case IActorRef _:
                    return ActorRefManifest;
                case PoisonPill _:
                    return PoisonPillManifest;
                case Kill _:
                    return KillManifest;
                case RemoteWatcher.Heartbeat _:
                    return RemoteWatcherHearthbeatManifest;
                case RemoteWatcher.HeartbeatRsp _:
                    return RemoteWatcherHearthbeatRspManifest;
                case RemoteScope _:
                    return RemoteScopeManifest;
                case LocalScope _:
                    return LocalScopeManifest;
                case Config _:
                    return ConfigManifest;
                case FromConfig _:
                    return FromConfigManifest;
                case DefaultResizer _:
                    return DefaultResizerManifest;
                case RoundRobinPool _:
                    return RoundRobinPoolManifest;
                case BroadcastPool _:
                    return BroadcastPoolManifest;
                case RandomPool _:
                    return RandomPoolManifest;
                case ScatterGatherFirstCompletedPool _:
                    return ScatterGatherPoolManifest;
                case TailChoppingPool _:
                    return TailChoppingPoolManifest;
                case ConsistentHashingPool _:
                    return ConsistentHashingPoolManifest;
                case RemoteRouterConfig _:
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
                case Identify _:
                    return IdentifyManifestBytes;
                case ActorIdentity _:
                    return ActorIdentityManifestBytes;
                case IActorRef _:
                    return ActorRefManifestBytes;
                case PoisonPill _:
                    return PoisonPillManifestBytes;
                case Kill _:
                    return KillManifestBytes;
                case RemoteWatcher.Heartbeat _:
                    return RemoteWatcherHearthbeatManifestBytes;
                case RemoteWatcher.HeartbeatRsp _:
                    return RemoteWatcherHearthbeatRspManifestBytes;
                case RemoteScope _:
                    return RemoteScopeManifestBytes;
                case LocalScope _:
                    return LocalScopeManifestBytes;
                case Config _:
                    return ConfigManifestBytes;
                case FromConfig _:
                    return FromConfigManifestBytes;
                case DefaultResizer _:
                    return DefaultResizerManifestBytes;
                case RoundRobinPool _:
                    return RoundRobinPoolManifestBytes;
                case BroadcastPool _:
                    return BroadcastPoolManifestBytes;
                case RandomPool _:
                    return RandomPoolManifestBytes;
                case ScatterGatherFirstCompletedPool _:
                    return ScatterGatherPoolManifestBytes;
                case TailChoppingPool _:
                    return TailChoppingPoolManifestBytes;
                case ConsistentHashingPool _:
                    return ConsistentHashingPoolManifestBytes;
                case RemoteRouterConfig _:
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
            return MessagePackSerializer.Serialize(new Protocol.Identify(WrappedPayloadSupport.PayloadToProto(system, identify.MessageId)), s_defaultResolver);
        }

        private Identify IdentifyFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.Identify>(bytes, s_defaultResolver);
            return new Identify(WrappedPayloadSupport.PayloadFrom(system, protoMessage.MessageId));
        }

        //
        // ActorIdentity
        //
        private byte[] ActorIdentityToProto(ActorIdentity actorIdentity)
        {
            var protoIdentify = new Protocol.ActorIdentity(
                WrappedPayloadSupport.PayloadToProto(system, actorIdentity.MessageId),
                Akka.Serialization.Serialization.SerializedActorPath(actorIdentity.Subject)
            );
            return MessagePackSerializer.Serialize(protoIdentify, s_defaultResolver);
        }

        private ActorIdentity ActorIdentityFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.ActorIdentity>(bytes, s_defaultResolver);
            return new ActorIdentity(WrappedPayloadSupport.PayloadFrom(system, protoMessage.CorrelationId), ResolveActorRef(protoMessage.Path));
        }

        private const string c_nobody = "nobody";
        //
        // IActorRef
        //
        private static byte[] ActorRefToProto(IActorRef actorRef)
        {
            var protoActor = new Protocol.ActorRefData(actorRef is Nobody ? c_nobody : Akka.Serialization.Serialization.SerializedActorPath(actorRef));

            return MessagePackSerializer.Serialize(protoActor, s_defaultResolver);
        }

        private IActorRef ActorRefFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.ActorRefData>(bytes, s_defaultResolver);
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
            return MessagePackSerializer.Serialize(new Protocol.RemoteWatcherHeartbeatResponse((ulong)heartbeatRsp.AddressUid), s_defaultResolver);
        }

        private static RemoteWatcher.HeartbeatRsp HearthbeatRspFromProto(byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.RemoteWatcherHeartbeatResponse>(bytes, s_defaultResolver);
            return new RemoteWatcher.HeartbeatRsp((int)message.Uid);
        }

        //
        // RemoteScope
        //
        private static byte[] RemoteScopeToProto(RemoteScope remoteScope)
        {
            return MessagePackSerializer.Serialize(new Protocol.RemoteScope(AddressMessageBuilder(remoteScope.Address)), s_defaultResolver);
        }

        private static RemoteScope RemoteScopeFromProto(byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.RemoteScope>(bytes, s_defaultResolver);
            return new RemoteScope(AddressFrom(message.Node));
        }

        //
        // Config
        //
        private static byte[] ConfigToProto(Config config)
        {
            if (config.IsEmpty) { return EmptyBytes; }

            return StringHelper.UTF8NoBOM.GetBytes(config.Root.ToString());
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
            Protocol.FromConfig message = null;
            if (fromConfig != FromConfig.Instance)
            {
                message = new Protocol.FromConfig(WrappedPayloadSupport.PayloadToProto(system, fromConfig.Resizer), fromConfig.RouterDispatcher);
            }

            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private FromConfig FromConfigFromProto(byte[] bytes)
        {
            var fromConfig = MessagePackSerializer.Deserialize<Protocol.FromConfig>(bytes, s_defaultResolver);

            if (null == fromConfig) { return FromConfig.Instance; }

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
            var message = new Protocol.DefaultResizer(
                (uint)defaultResizer.LowerBound,
                (uint)defaultResizer.UpperBound,
                (uint)defaultResizer.PressureThreshold,
                defaultResizer.RampupRate,
                defaultResizer.BackoffThreshold,
                defaultResizer.BackoffRate,
                (uint)defaultResizer.MessagesPerResize
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static DefaultResizer DefaultResizerFromProto(byte[] bytes)
        {
            var resizer = MessagePackSerializer.Deserialize<Protocol.DefaultResizer>(bytes, s_defaultResolver);
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
        private Protocol.GenericRoutingPool GenericRoutingPoolBuilder(Pool pool)
        {
            return new Protocol.GenericRoutingPool(
                (uint)pool.NrOfInstances,
                pool.RouterDispatcher,
                pool.UsePoolDispatcher,
                WrappedPayloadSupport.PayloadToProto(system, pool.Resizer)
            );
        }

        //
        // RoundRobinPool
        //
        private byte[] RoundRobinPoolToProto(RoundRobinPool roundRobinPool)
        {
            return MessagePackSerializer.Serialize(GenericRoutingPoolBuilder(roundRobinPool), s_defaultResolver);
        }

        private RoundRobinPool RoundRobinPoolFromProto(byte[] bytes)
        {
            var broadcastPool = MessagePackSerializer.Deserialize<Protocol.GenericRoutingPool>(bytes, s_defaultResolver);

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
            return MessagePackSerializer.Serialize(GenericRoutingPoolBuilder(broadcastPool), s_defaultResolver);
        }

        private BroadcastPool BroadcastPoolFromProto(byte[] bytes)
        {
            var broadcastPool = MessagePackSerializer.Deserialize<Protocol.GenericRoutingPool>(bytes, s_defaultResolver);

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
            return MessagePackSerializer.Serialize(GenericRoutingPoolBuilder(randomPool), s_defaultResolver);
        }

        private RandomPool RandomPoolFromProto(byte[] bytes)
        {
            var randomPool = MessagePackSerializer.Deserialize<Protocol.GenericRoutingPool>(bytes, s_defaultResolver);

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
            var message = new Protocol.ScatterGatherPool(
                GenericRoutingPoolBuilder(scatterGatherFirstCompletedPool),
                Protocol.Duration.FromTimeSpan(scatterGatherFirstCompletedPool.Within)
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private ScatterGatherFirstCompletedPool ScatterGatherFirstCompletedPoolFromProto(byte[] bytes)
        {
            var scatterGatherFirstCompletedPool = MessagePackSerializer.Deserialize<Protocol.ScatterGatherPool>(bytes, s_defaultResolver);

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
            var message = new Protocol.TailChoppingPool(
                GenericRoutingPoolBuilder(tailChoppingPool),
                Protocol.Duration.FromTimeSpan(tailChoppingPool.Within),
                Protocol.Duration.FromTimeSpan(tailChoppingPool.Interval)
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private TailChoppingPool TailChoppingPoolFromProto(byte[] bytes)
        {
            var tailChoppingPool = MessagePackSerializer.Deserialize<Protocol.TailChoppingPool>(bytes, s_defaultResolver);

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
            return MessagePackSerializer.Serialize(GenericRoutingPoolBuilder(hashingPool), s_defaultResolver);
        }

        private object ConsistentHashingPoolFromProto(byte[] bytes)
        {
            var consistentHashingPool = MessagePackSerializer.Deserialize<Protocol.GenericRoutingPool>(bytes, s_defaultResolver);

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
            var protoRemoteRouterConfig = new Protocol.RemoteRouterConfig(
                WrappedPayloadSupport.PayloadToProto(system, remoteRouterConfig.Local),
                remoteRouterConfig.Nodes.Select(AddressMessageBuilder).ToArray()
            );
            return MessagePackSerializer.Serialize(protoRemoteRouterConfig, s_defaultResolver);
        }

        private RemoteRouterConfig RemoteRouterConfigFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.RemoteRouterConfig>(bytes, s_defaultResolver);
            return new RemoteRouterConfig(WrappedPayloadSupport.PayloadFrom(system, protoMessage.Local).AsInstanceOf<Pool>(), protoMessage.Nodes.Select(AddressFrom));
        }

        //
        // Address
        //
        private static Protocol.AddressData AddressMessageBuilder(Address address)
        {
            var message = new Protocol.AddressData(
                address.System,
                address.Host,
                (uint)(address.Port ?? 0),
                address.Protocol);
            return message;
        }

        private static Address AddressFrom(Protocol.AddressData addressProto)
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
