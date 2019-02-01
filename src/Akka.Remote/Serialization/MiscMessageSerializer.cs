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
using Akka.Serialization.Resolvers;
using CuteAnt;
using CuteAnt.Text;
using MessagePack;

namespace Akka.Remote.Serialization
{
    public sealed class MiscMessageSerializer : SerializerWithIntegerManifest
    {
        #region manifests

        private const int IdentifyManifest = 200;
        private const int ActorIdentityManifest = 201;
        private const int ActorRefManifest = 202;
        private const int PoisonPillManifest = 203;
        private const int KillManifest = 204;
        private const int RemoteWatcherHearthbeatManifest = 205;
        private const int RemoteWatcherHearthbeatRspManifest = 206;
        private const int LocalScopeManifest = 207;
        private const int RemoteScopeManifest = 208;
        private const int ConfigManifest = 209;
        private const int FromConfigManifest = 210;
        private const int DefaultResizerManifest = 211;
        private const int RoundRobinPoolManifest = 212;
        private const int BroadcastPoolManifest = 213;
        private const int RandomPoolManifest = 214;
        private const int ScatterGatherPoolManifest = 215;
        private const int TailChoppingPoolManifest = 216;
        private const int ConsistentHashingPoolManifest = 217;
        private const int RemoteRouterConfigManifest = 218;

        private static readonly Dictionary<Type, int> ManifestMap;

        static MiscMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, int>
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

        /// <summary>Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public MiscMessageSerializer(ExtendedActorSystem system)
            : this(system, HyperionSerializerSettings.Default) { }

        /// <summary>Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        /// <param name="config"></param>
        public MiscMessageSerializer(ExtendedActorSystem system, Config config)
            : this(system, HyperionSerializerSettings.Create(config)) { }

        /// <summary>Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.</summary>
        public MiscMessageSerializer(ExtendedActorSystem system, HyperionSerializerSettings settings)
            : base(system)
        {
            var serializer = HyperionSerializerHelper.CreateSerializer(system, settings);
            _defaultResolver = new AkkaDefaultResolver(system, serializer);
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out int manifest)
        {
            switch (obj)
            {
                case Identify identify:
                    manifest = IdentifyManifest;
                    return MessagePackSerializer.Serialize(identify, _defaultResolver);
                case ActorIdentity actorIdentity:
                    manifest = ActorIdentityManifest;
                    return MessagePackSerializer.Serialize(actorIdentity, _defaultResolver);
                case IActorRef actorRef:
                    manifest = ActorRefManifest;
                    return ActorRefToProto(actorRef);
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    manifest = RemoteWatcherHearthbeatRspManifest;
                    return MessagePackSerializer.Serialize(heartbeatRsp, _defaultResolver);
                case RemoteScope remoteScope:
                    manifest = RemoteScopeManifest;
                    return MessagePackSerializer.Serialize(remoteScope, _defaultResolver);
                case Config config:
                    manifest = ConfigManifest;
                    return ConfigToProto(config);
                case FromConfig fromConfig:
                    manifest = FromConfigManifest;
                    return MessagePackSerializer.Serialize(fromConfig, _defaultResolver);
                case DefaultResizer defaultResizer:
                    manifest = DefaultResizerManifest;
                    return MessagePackSerializer.Serialize(defaultResizer, _defaultResolver);
                case RoundRobinPool roundRobinPool:
                    manifest = RoundRobinPoolManifest;
                    return MessagePackSerializer.Serialize(roundRobinPool, _defaultResolver);
                case BroadcastPool broadcastPool:
                    manifest = BroadcastPoolManifest;
                    return MessagePackSerializer.Serialize(broadcastPool, _defaultResolver);
                case RandomPool randomPool:
                    manifest = RandomPoolManifest;
                    return MessagePackSerializer.Serialize(randomPool, _defaultResolver);
                case ScatterGatherFirstCompletedPool scatterPool:
                    manifest = ScatterGatherPoolManifest;
                    return MessagePackSerializer.Serialize(scatterPool, _defaultResolver);
                case TailChoppingPool tailChoppingPool:
                    manifest = TailChoppingPoolManifest;
                    return MessagePackSerializer.Serialize(tailChoppingPool, _defaultResolver);
                case ConsistentHashingPool hashingPool:
                    manifest = ConsistentHashingPoolManifest;
                    return MessagePackSerializer.Serialize(hashingPool, _defaultResolver);
                case RemoteRouterConfig remoteRouterConfig:
                    manifest = RemoteRouterConfigManifest;
                    return MessagePackSerializer.Serialize(remoteRouterConfig, _defaultResolver);

                case PoisonPill _:
                    manifest = PoisonPillManifest;
                    return EmptyBytes;
                case Kill _:
                    manifest = KillManifest;
                    return EmptyBytes;
                case RemoteWatcher.Heartbeat _:
                    manifest = RemoteWatcherHearthbeatManifest;
                    return EmptyBytes;
                case LocalScope _:
                    manifest = LocalScopeManifest;
                    return EmptyBytes;

                default:
                    manifest = 0; return ThrowHelper.ThrowArgumentException_Serializer_D<byte[]>(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, int manifest)
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

        /// <inheritdoc />
        protected override int GetManifest(Type type)
        {
            if (null == type) { return 0; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            return ThrowHelper.ThrowArgumentException_Serializer_D<int>(type);
        }

        /// <inheritdoc />
        public override int Manifest(object obj)
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
                    return ThrowHelper.ThrowArgumentException_Serializer_D<int>(obj);
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
