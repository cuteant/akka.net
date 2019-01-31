//-----------------------------------------------------------------------
// <copyright file="MiscMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Routing;
using Akka.Routing;
using Akka.Serialization;
using CuteAnt;
using CuteAnt.Text;
using MessagePack;
using MessagePack.Resolvers;

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

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        private readonly IFormatterResolver _defaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public MiscMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _defaultResolver = new DefaultResolver();
            _defaultResolver.Context2.Add(MsgPackSerializerHelper.ActorSystemIdentifier, system);
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case Identify identify:
                    return MessagePackSerializer.Serialize(identify, _defaultResolver);
                case ActorIdentity actorIdentity:
                    return MessagePackSerializer.Serialize(actorIdentity, _defaultResolver);
                case IActorRef actorRef:
                    return ActorRefToProto(actorRef);
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    return MessagePackSerializer.Serialize(heartbeatRsp, _defaultResolver);
                case RemoteScope remoteScope:
                    return MessagePackSerializer.Serialize(remoteScope, _defaultResolver);
                case Config config:
                    return ConfigToProto(config);
                case FromConfig fromConfig:
                    return MessagePackSerializer.Serialize(fromConfig, _defaultResolver);
                case DefaultResizer defaultResizer:
                    return MessagePackSerializer.Serialize(defaultResizer, _defaultResolver);
                case RoundRobinPool roundRobinPool:
                    return MessagePackSerializer.Serialize(roundRobinPool, _defaultResolver);
                case BroadcastPool broadcastPool:
                    return MessagePackSerializer.Serialize(broadcastPool, _defaultResolver);
                case RandomPool randomPool:
                    return MessagePackSerializer.Serialize(randomPool, _defaultResolver);
                case ScatterGatherFirstCompletedPool scatterPool:
                    return MessagePackSerializer.Serialize(scatterPool, _defaultResolver);
                case TailChoppingPool tailChoppingPool:
                    return MessagePackSerializer.Serialize(tailChoppingPool, _defaultResolver);
                case ConsistentHashingPool hashingPool:
                    return MessagePackSerializer.Serialize(hashingPool, _defaultResolver);
                case RemoteRouterConfig remoteRouterConfig:
                    return MessagePackSerializer.Serialize(remoteRouterConfig, _defaultResolver);

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
                    return MessagePackSerializer.Serialize(identify, _defaultResolver);
                case ActorIdentity actorIdentity:
                    manifest = ActorIdentityManifestBytes;
                    return MessagePackSerializer.Serialize(actorIdentity, _defaultResolver);
                case IActorRef actorRef:
                    manifest = ActorRefManifestBytes;
                    return ActorRefToProto(actorRef);
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    manifest = RemoteWatcherHearthbeatRspManifestBytes;
                    return MessagePackSerializer.Serialize(heartbeatRsp, _defaultResolver);
                case RemoteScope remoteScope:
                    manifest = RemoteScopeManifestBytes;
                    return MessagePackSerializer.Serialize(remoteScope, _defaultResolver);
                case Config config:
                    manifest = ConfigManifestBytes;
                    return ConfigToProto(config);
                case FromConfig fromConfig:
                    manifest = FromConfigManifestBytes;
                    return MessagePackSerializer.Serialize(fromConfig, _defaultResolver);
                case DefaultResizer defaultResizer:
                    manifest = DefaultResizerManifestBytes;
                    return MessagePackSerializer.Serialize(defaultResizer, _defaultResolver);
                case RoundRobinPool roundRobinPool:
                    manifest = RoundRobinPoolManifestBytes;
                    return MessagePackSerializer.Serialize(roundRobinPool, _defaultResolver);
                case BroadcastPool broadcastPool:
                    manifest = BroadcastPoolManifestBytes;
                    return MessagePackSerializer.Serialize(broadcastPool, _defaultResolver);
                case RandomPool randomPool:
                    manifest = RandomPoolManifestBytes;
                    return MessagePackSerializer.Serialize(randomPool, _defaultResolver);
                case ScatterGatherFirstCompletedPool scatterPool:
                    manifest = ScatterGatherPoolManifestBytes;
                    return MessagePackSerializer.Serialize(scatterPool, _defaultResolver);
                case TailChoppingPool tailChoppingPool:
                    manifest = TailChoppingPoolManifestBytes;
                    return MessagePackSerializer.Serialize(tailChoppingPool, _defaultResolver);
                case ConsistentHashingPool hashingPool:
                    manifest = ConsistentHashingPoolManifestBytes;
                    return MessagePackSerializer.Serialize(hashingPool, _defaultResolver);
                case RemoteRouterConfig remoteRouterConfig:
                    manifest = RemoteRouterConfigManifestBytes;
                    return MessagePackSerializer.Serialize(remoteRouterConfig, _defaultResolver);

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
                    return MessagePackSerializer.Deserialize<Identify>(bytes, _defaultResolver);
                case ActorIdentityManifest:
                    return MessagePackSerializer.Deserialize<ActorIdentity>(bytes, _defaultResolver);
                case ActorRefManifest:
                    return ActorRefFromProto(bytes);
                case PoisonPillManifest:
                    return PoisonPill.Instance;
                case KillManifest:
                    return Kill.Instance;
                case RemoteWatcherHearthbeatManifest:
                    return RemoteWatcher.Heartbeat.Instance;
                case RemoteWatcherHearthbeatRspManifest:
                    return MessagePackSerializer.Deserialize<RemoteWatcher.HeartbeatRsp>(bytes, _defaultResolver);
                case LocalScopeManifest:
                    return LocalScope.Instance;
                case RemoteScopeManifest:
                    return MessagePackSerializer.Deserialize<RemoteScope>(bytes, _defaultResolver);
                case ConfigManifest:
                    return ConfigFromProto(bytes);
                case FromConfigManifest:
                    return MessagePackSerializer.Deserialize<FromConfig>(bytes, _defaultResolver);
                case DefaultResizerManifest:
                    return MessagePackSerializer.Deserialize<DefaultResizer>(bytes, _defaultResolver);
                case RoundRobinPoolManifest:
                    return MessagePackSerializer.Deserialize<RoundRobinPool>(bytes, _defaultResolver);
                case BroadcastPoolManifest:
                    return MessagePackSerializer.Deserialize<BroadcastPool>(bytes, _defaultResolver);
                case RandomPoolManifest:
                    return MessagePackSerializer.Deserialize<RandomPool>(bytes, _defaultResolver);
                case ScatterGatherPoolManifest:
                    return MessagePackSerializer.Deserialize<ScatterGatherFirstCompletedPool>(bytes, _defaultResolver);
                case TailChoppingPoolManifest:
                    return MessagePackSerializer.Deserialize<TailChoppingPool>(bytes, _defaultResolver);
                case ConsistentHashingPoolManifest:
                    return MessagePackSerializer.Deserialize<ConsistentHashingPool>(bytes, _defaultResolver);
                case RemoteRouterConfigManifest:
                    return MessagePackSerializer.Deserialize<RemoteRouterConfig>(bytes, _defaultResolver);

                default:
                    return ThrowHelper.ThrowSerializationException_Serializer_MiscFrom(manifest);
            }
        }

        private const string c_nobody = "nobody";
        //
        // IActorRef
        //
        private byte[] ActorRefToProto(IActorRef actorRef)
        {
            var protoActor = new Protocol.ReadOnlyActorRefData(actorRef is Nobody ? c_nobody : Akka.Serialization.Serialization.SerializedActorPath(actorRef));

            return MessagePackSerializer.Serialize(protoActor, _defaultResolver);
        }

        private IActorRef ActorRefFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.ReadOnlyActorRefData>(bytes, _defaultResolver);
            if (string.Equals(protoMessage.Path, c_nobody, StringComparison.Ordinal))
            {
                return Nobody.Instance;
            }

            return system.Provider.ResolveActorRef(protoMessage.Path);
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
    }
}
