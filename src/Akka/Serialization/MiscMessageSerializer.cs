//-----------------------------------------------------------------------
// <copyright file="MiscMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using Akka.Serialization.Resolvers;
using CuteAnt;
using MessagePack;

namespace Akka.Serialization
{
    public sealed class MiscMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string IdentifyManifest = "ID";
        private const string ActorIdentityManifest = "AID";
        private const string ActorRefManifest = "AR";
        private const string PoisonPillManifest = "PP";
        private const string KillManifest = "K";
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


        private static readonly Dictionary<Type, string> ManifestMap;

        static MiscMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(Identify), IdentifyManifest},
                { typeof(ActorIdentity), ActorIdentityManifest},
                { typeof(IActorRef), ActorRefManifest},
                { typeof(PoisonPill), PoisonPillManifest},
                { typeof(Kill), KillManifest},
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
        public override byte[] ToBinary(object obj, out string manifest)
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
                    return MessagePackSerializer.Serialize(actorRef, _defaultResolver);
                case RemoteScope remoteScope:
                    manifest = RemoteScopeManifest;
                    return MessagePackSerializer.Serialize(remoteScope, _defaultResolver);
                case Config config:
                    manifest = ConfigManifest;
                    return MessagePackSerializer.Serialize(config, _defaultResolver);
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

                case PoisonPill _:
                    manifest = PoisonPillManifest;
                    return EmptyBytes;
                case Kill _:
                    manifest = KillManifest;
                    return EmptyBytes;
                case LocalScope _:
                    manifest = LocalScopeManifest;
                    return EmptyBytes;

                default:
                    throw AkkaThrowHelper.GetArgumentException_Serializer_D(obj);
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
                    return MessagePackSerializer.Deserialize<IActorRef>(bytes, _defaultResolver);
                case PoisonPillManifest:
                    return PoisonPill.Instance;
                case KillManifest:
                    return Kill.Instance;
                case LocalScopeManifest:
                    return LocalScope.Instance;
                case RemoteScopeManifest:
                    return MessagePackSerializer.Deserialize<RemoteScope>(bytes, _defaultResolver);
                case ConfigManifest:
                    return MessagePackSerializer.Deserialize<Config>(bytes, _defaultResolver);
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

                default:
                    throw GetSerializationException_Serializer_MiscFrom(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            throw AkkaThrowHelper.GetArgumentException_Serializer_D(type);
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

                default:
                    throw AkkaThrowHelper.GetArgumentException_Serializer_D(obj);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetSerializationException_Serializer_MiscFrom(string manifest)
        {
            return new SerializationException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(MiscMessageSerializer)}]");
        }
    }
}
