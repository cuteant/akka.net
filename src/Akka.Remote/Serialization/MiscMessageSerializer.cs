//-----------------------------------------------------------------------
// <copyright file="MiscMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
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
                    return ThrowHelper.ThrowArgumentException_Serializer_D<byte[]>(obj);
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
            var protoActor = new Protocol.ActorRefData(actorRef is Nobody ? c_nobody : Akka.Serialization.Serialization.SerializedActorPath(actorRef));

            return MessagePackSerializer.Serialize(protoActor, _defaultResolver);
        }

        private IActorRef ActorRefFromProto(byte[] bytes)
        {
            var protoMessage = MessagePackSerializer.Deserialize<Protocol.ActorRefData>(bytes, _defaultResolver);
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

        private IActorRef ResolveActorRef(string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return system.Provider.ResolveActorRef(path);
        }
    }
}
