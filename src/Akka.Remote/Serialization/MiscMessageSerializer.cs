//-----------------------------------------------------------------------
// <copyright file="MiscMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Remote.Routing;
using Akka.Routing;
using Akka.Serialization;
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
        private static readonly byte[] IdentifyManifestBytes;
        private const string ActorIdentityManifest = "AID";
        private static readonly byte[] ActorIdentityManifestBytes;
        private const string ActorRefManifest = "AR";
        private static readonly byte[] ActorRefManifestBytes;
        private const string PoisonPillManifest = "PP";
        private static readonly byte[] PoisonPillManifestBytes;
        private const string KillManifest = "K";
        private static readonly byte[] KillManifestBytes;
        private const string RemoteWatcherHearthbeatManifest = "RWHB";
        private static readonly byte[] RemoteWatcherHearthbeatManifestBytes;
        private const string RemoteWatcherHearthbeatRspManifest = "RWHR";
        private static readonly byte[] RemoteWatcherHearthbeatRspManifestBytes;
        private const string LocalScopeManifest = "LS";
        private static readonly byte[] LocalScopeManifestBytes;
        private const string RemoteScopeManifest = "RS";
        private static readonly byte[] RemoteScopeManifestBytes;
        private const string ConfigManifest = "CF";
        private static readonly byte[] ConfigManifestBytes;
        private const string FromConfigManifest = "FC";
        private static readonly byte[] FromConfigManifestBytes;
        private const string DefaultResizerManifest = "DR";
        private static readonly byte[] DefaultResizerManifestBytes;
        private const string RoundRobinPoolManifest = "RORRP";
        private static readonly byte[] RoundRobinPoolManifestBytes;
        private const string BroadcastPoolManifest = "ROBP";
        private static readonly byte[] BroadcastPoolManifestBytes;
        private const string RandomPoolManifest = "RORP";
        private static readonly byte[] RandomPoolManifestBytes;
        private const string ScatterGatherPoolManifest = "ROSGP";
        private static readonly byte[] ScatterGatherPoolManifestBytes;
        private const string TailChoppingPoolManifest = "ROTCP";
        private static readonly byte[] TailChoppingPoolManifestBytes;
        private const string ConsistentHashingPoolManifest = "ROCHP";
        private static readonly byte[] ConsistentHashingPoolManifestBytes;
        private const string RemoteRouterConfigManifest = "RORRC";
        private static readonly byte[] RemoteRouterConfigManifestBytes;
        private static readonly Dictionary<Type, string> ManifestMap;

        static MiscMessageSerializer()
        {
            IdentifyManifestBytes = StringHelper.UTF8NoBOM.GetBytes(IdentifyManifest);
            ActorIdentityManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ActorIdentityManifest);
            ActorRefManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ActorRefManifest);
            PoisonPillManifestBytes = StringHelper.UTF8NoBOM.GetBytes(PoisonPillManifest);
            KillManifestBytes = StringHelper.UTF8NoBOM.GetBytes(KillManifest);
            RemoteWatcherHearthbeatManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteWatcherHearthbeatManifest);
            RemoteWatcherHearthbeatRspManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteWatcherHearthbeatRspManifest);
            LocalScopeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(LocalScopeManifest);
            RemoteScopeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteScopeManifest);
            ConfigManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ConfigManifest);
            FromConfigManifestBytes = StringHelper.UTF8NoBOM.GetBytes(FromConfigManifest);
            DefaultResizerManifestBytes = StringHelper.UTF8NoBOM.GetBytes(DefaultResizerManifest);
            RoundRobinPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RoundRobinPoolManifest);
            BroadcastPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(BroadcastPoolManifest);
            RandomPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RandomPoolManifest);
            ScatterGatherPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ScatterGatherPoolManifest);
            TailChoppingPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(TailChoppingPoolManifest);
            ConsistentHashingPoolManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ConsistentHashingPoolManifest);
            RemoteRouterConfigManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RemoteRouterConfigManifest);
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(Identify), IdentifyManifest},
                { typeof(ActorIdentity), ActorIdentityManifest},
                { typeof(IActorRef), ActorRefManifest},
                { typeof(PoisonPill), PoisonPillManifest},
                { typeof(Kill), KillManifest},
                { typeof(RemoteWatcher.Heartbeat), RemoteWatcherHearthbeatManifest},
                { typeof(RemoteWatcher.HeartbeatRsp), RemoteWatcherHearthbeatRspManifest},
                { typeof(RemoteScope), RemoteScopeManifest},
                { typeof(LocalScope), LocalScopeManifest},
                { typeof(Config), ConfigManifest},
                { typeof(FromConfig), FromConfigManifest},
                { typeof(DefaultResizer), DefaultResizerManifest},
                { typeof(RoundRobinPool), RoundRobinPoolManifest},
                { typeof(BroadcastPool), BroadcastPoolManifest},
                { typeof(RandomPool), RandomPoolManifest},
                { typeof(ScatterGatherFirstCompletedPool), ScatterGatherPoolManifest},
                { typeof(TailChoppingPool), TailChoppingPoolManifest},
                { typeof(ConsistentHashingPool), ConsistentHashingPoolManifest},
                { typeof(RemoteRouterConfig), RemoteRouterConfigManifest},
            };
        }

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
                    return ThrowHelper.ThrowArgumentException_Serializer_S(obj);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return string.Empty; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            return ThrowHelper.ThrowArgumentException_Serializer_D<string>(type);
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
                    return ThrowHelper.ThrowArgumentException_Serializer_D<string>(obj);
            }
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out byte[] manifest)
        {
            switch (obj)
            {
                case Identify identify:
                    manifest = IdentifyManifestBytes;
                    return IdentifyToProto(identify);
                case ActorIdentity actorIdentity:
                    manifest = ActorIdentityManifestBytes;
                    return ActorIdentityToProto(actorIdentity);
                case IActorRef actorRef:
                    manifest = ActorRefManifestBytes;
                    return ActorRefToProto(actorRef);
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    manifest = RemoteWatcherHearthbeatRspManifestBytes;
                    return HeartbeatRspToProto(heartbeatRsp);
                case RemoteScope remoteScope:
                    manifest = RemoteScopeManifestBytes;
                    return RemoteScopeToProto(remoteScope);
                case Config config:
                    manifest = ConfigManifestBytes;
                    return ConfigToProto(config);
                case FromConfig fromConfig:
                    manifest = FromConfigManifestBytes;
                    return FromConfigToProto(fromConfig);
                case DefaultResizer defaultResizer:
                    manifest = DefaultResizerManifestBytes;
                    return DefaultResizerToProto(defaultResizer);
                case RoundRobinPool roundRobinPool:
                    manifest = RoundRobinPoolManifestBytes;
                    return RoundRobinPoolToProto(roundRobinPool);
                case BroadcastPool broadcastPool:
                    manifest = BroadcastPoolManifestBytes;
                    return BroadcastPoolToProto(broadcastPool);
                case RandomPool randomPool:
                    manifest = RandomPoolManifestBytes;
                    return RandomPoolToProto(randomPool);
                case ScatterGatherFirstCompletedPool scatterPool:
                    manifest = ScatterGatherPoolManifestBytes;
                    return ScatterGatherFirstCompletedPoolToProto(scatterPool);
                case TailChoppingPool tailChoppingPool:
                    manifest = TailChoppingPoolManifestBytes;
                    return TailChoppingPoolToProto(tailChoppingPool);
                case ConsistentHashingPool hashingPool:
                    manifest = ConsistentHashingPoolManifestBytes;
                    return ConsistentHashingPoolToProto(hashingPool);
                case RemoteRouterConfig remoteRouterConfig:
                    manifest = RemoteRouterConfigManifestBytes;
                    return RemoteRouterConfigToProto(remoteRouterConfig);

                case PoisonPill _:
                    manifest = PoisonPillManifestBytes;
                    return EmptyBytes;
                case Kill _:
                    manifest = KillManifestBytes;
                    return EmptyBytes;
                case RemoteWatcher.Heartbeat _:
                    manifest = RemoteWatcherHearthbeatManifestBytes;
                    return EmptyBytes;
                case LocalScope _:
                    manifest = LocalScopeManifestBytes;
                    return EmptyBytes;

                default:
                    manifest = null; return ThrowHelper.ThrowArgumentException_Serializer_D<byte[]>(obj);
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
                    return ThrowHelper.ThrowSerializationException_Serializer_MiscFrom(manifest);
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
            var protoActor = new Protocol.ReadOnlyActorRefData(actorRef is Nobody ? c_nobody : Akka.Serialization.Serialization.SerializedActorPath(actorRef));

            return MessagePackSerializer.Serialize(protoActor, s_defaultResolver);
        }

        private IActorRef ActorRefFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.ReadOnlyActorRefData>(bytes, s_defaultResolver);
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
            Protocol.FromConfig message;
            if (fromConfig == FromConfig.Instance || null == fromConfig)
            {
                message = null;
            }
            else
            {
                message = new Protocol.FromConfig(WrappedPayloadSupport.PayloadToProto(system, fromConfig.Resizer), fromConfig.RouterDispatcher);
            }

            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private FromConfig FromConfigFromProto(byte[] bytes)
        {
            var fromConfig = MessagePackSerializer.Deserialize<Protocol.FromConfig>(bytes, s_defaultResolver);

            if (null == fromConfig) { return FromConfig.Instance; }

            var rawResizer = fromConfig.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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

            var rawResizer = broadcastPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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

            var rawResizer = broadcastPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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

            var rawResizer = randomPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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

            var rawResizer = scatterGatherFirstCompletedPool.Generic.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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

            var rawResizer = tailChoppingPool.Generic.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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

            var rawResizer = consistentHashingPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(system, rawResizer)
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
            return new RemoteRouterConfig(WrappedPayloadSupport.PayloadFrom(system, protoMessage.Local).AsInstanceOf<Pool>(), protoMessage.Nodes.Select(_ => AddressFrom(_)));
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

        private static Address AddressFrom(in Protocol.AddressData addressProto)
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
