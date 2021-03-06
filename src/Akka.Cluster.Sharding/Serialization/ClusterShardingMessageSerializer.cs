﻿//-----------------------------------------------------------------------
// <copyright file="ClusterShardingMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt;
using MessagePack;
using ActorRefMessage = Akka.Serialization.Protocol.ReadOnlyActorRefData;

namespace Akka.Cluster.Sharding.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterShardingMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string CoordinatorStateManifest = "AA";
        private const string ShardRegionRegisteredManifest = "AB";
        private const string ShardRegionProxyRegisteredManifest = "AC";
        private const string ShardRegionTerminatedManifest = "AD";
        private const string ShardRegionProxyTerminatedManifest = "AE";
        private const string ShardHomeAllocatedManifest = "AF";
        private const string ShardHomeDeallocatedManifest = "AG";

        private const string RegisterManifest = "BA";
        private const string RegisterProxyManifest = "BB";
        private const string RegisterAckManifest = "BC";
        private const string GetShardHomeManifest = "BD";
        private const string ShardHomeManifest = "BE";
        private const string HostShardManifest = "BF";
        private const string ShardStartedManifest = "BG";
        private const string BeginHandOffManifest = "BH";
        private const string BeginHandOffAckManifest = "BI";
        private const string HandOffManifest = "BJ";
        private const string ShardStoppedManifest = "BK";
        private const string GracefulShutdownReqManifest = "BL";

        private const string EntityStateManifest = "CA";
        private const string EntityStartedManifest = "CB";
        private const string EntityStoppedManifest = "CD";

        private const string StartEntityManifest = "EA";
        private const string StartEntityAckManifest = "EB";

        private const string GetShardStatsManifest = "DA";
        private const string ShardStatsManifest = "DB";
        private const string GetShardRegionStatsManifest = "DC";
        private const string ShardRegionStatsManifest = "DD";

        private const string GetClusterShardingStatsManifest = "GS";
        private const string ClusterShardingStatsManifest = "CS";

        private static readonly Dictionary<Type, string> ManifestMap;

        private static readonly IFormatterResolver s_defaultResolver;

        static ClusterShardingMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(Shard.ShardState), EntityStateManifest },
                { typeof(Shard.EntityStarted), EntityStartedManifest },
                { typeof(Shard.EntityStopped), EntityStoppedManifest },
                { typeof(PersistentShardCoordinator.State), CoordinatorStateManifest },
                { typeof(PersistentShardCoordinator.ShardRegionRegistered), ShardRegionRegisteredManifest },
                { typeof(PersistentShardCoordinator.ShardRegionProxyRegistered), ShardRegionProxyRegisteredManifest },
                { typeof(PersistentShardCoordinator.ShardRegionTerminated), ShardRegionTerminatedManifest },
                { typeof(PersistentShardCoordinator.ShardRegionProxyTerminated), ShardRegionProxyTerminatedManifest },
                { typeof(PersistentShardCoordinator.ShardHomeAllocated), ShardHomeAllocatedManifest },
                { typeof(PersistentShardCoordinator.ShardHomeDeallocated), ShardHomeDeallocatedManifest },
                { typeof(PersistentShardCoordinator.Register), RegisterManifest },
                { typeof(PersistentShardCoordinator.RegisterProxy), RegisterProxyManifest },
                { typeof(PersistentShardCoordinator.RegisterAck), RegisterAckManifest },
                { typeof(PersistentShardCoordinator.GetShardHome), GetShardHomeManifest },
                { typeof(PersistentShardCoordinator.ShardHome), ShardHomeManifest },
                { typeof(PersistentShardCoordinator.HostShard), HostShardManifest },
                { typeof(PersistentShardCoordinator.ShardStarted), ShardStartedManifest },
                { typeof(PersistentShardCoordinator.BeginHandOff), BeginHandOffManifest },
                { typeof(PersistentShardCoordinator.BeginHandOffAck), BeginHandOffAckManifest },
                { typeof(PersistentShardCoordinator.HandOff), HandOffManifest },
                { typeof(PersistentShardCoordinator.ShardStopped), ShardStoppedManifest },
                { typeof(PersistentShardCoordinator.GracefulShutdownRequest), GracefulShutdownReqManifest },
                { typeof(ShardRegion.StartEntity), StartEntityManifest },
                { typeof(ShardRegion.StartEntityAck), StartEntityAckManifest },
                { typeof(Shard.GetShardStats), GetShardStatsManifest },
                { typeof(Shard.ShardStats), ShardStatsManifest },
                { typeof(GetShardRegionStats), GetShardRegionStatsManifest },
                { typeof(ShardRegionStats), ShardRegionStatsManifest },
                { typeof(GetClusterShardingStats), GetClusterShardingStatsManifest },
                { typeof(ClusterShardingStats), ClusterShardingStatsManifest },
            };
            s_defaultResolver = MessagePackStandardResolver.Default;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterShardingMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public ClusterShardingMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case PersistentShardCoordinator.State o:
                    manifest = CoordinatorStateManifest;
                    return MessagePackSerializer.Serialize(CoordinatorStateToProto(o), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionRegistered o:
                    manifest = ShardRegionRegisteredManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.Region), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionProxyRegistered o:
                    manifest = ShardRegionProxyRegisteredManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.RegionProxy), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionTerminated o:
                    manifest = ShardRegionTerminatedManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.Region), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionProxyTerminated o:
                    manifest = ShardRegionProxyTerminatedManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.RegionProxy), s_defaultResolver);
                case PersistentShardCoordinator.ShardHomeAllocated o:
                    manifest = ShardHomeAllocatedManifest;
                    return MessagePackSerializer.Serialize(ShardHomeAllocatedToProto(o), s_defaultResolver);
                case PersistentShardCoordinator.ShardHomeDeallocated o:
                    manifest = ShardHomeDeallocatedManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.Register o:
                    manifest = RegisterManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.ShardRegion), s_defaultResolver);
                case PersistentShardCoordinator.RegisterProxy o:
                    manifest = RegisterProxyManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.ShardRegionProxy), s_defaultResolver);
                case PersistentShardCoordinator.RegisterAck o:
                    manifest = RegisterAckManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.Coordinator), s_defaultResolver);
                case PersistentShardCoordinator.GetShardHome o:
                    manifest = GetShardHomeManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.ShardHome o:
                    manifest = ShardHomeManifest;
                    return MessagePackSerializer.Serialize(ShardHomeToProto(o), s_defaultResolver);
                case PersistentShardCoordinator.HostShard o:
                    manifest = HostShardManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.ShardStarted o:
                    manifest = ShardStartedManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.BeginHandOff o:
                    manifest = BeginHandOffManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.BeginHandOffAck o:
                    manifest = BeginHandOffAckManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.HandOff o:
                    manifest = HandOffManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.ShardStopped o:
                    manifest = ShardStoppedManifest;
                    return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.GracefulShutdownRequest o:
                    manifest = GracefulShutdownReqManifest;
                    return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.ShardRegion), s_defaultResolver);
                case Shard.ShardState o:
                    manifest = EntityStateManifest;
                    return MessagePackSerializer.Serialize(EntityStateToProto(o), s_defaultResolver);
                case Shard.EntityStarted o:
                    manifest = EntityStartedManifest;
                    return MessagePackSerializer.Serialize(EntityStartedToProto(o), s_defaultResolver);
                case Shard.EntityStopped o:
                    manifest = EntityStoppedManifest;
                    return MessagePackSerializer.Serialize(EntityStoppedToProto(o), s_defaultResolver);
                case ShardRegion.StartEntity o:
                    manifest = StartEntityManifest;
                    return MessagePackSerializer.Serialize(StartEntityToProto(o), s_defaultResolver);
                case ShardRegion.StartEntityAck o:
                    manifest = StartEntityAckManifest;
                    return MessagePackSerializer.Serialize(StartEntityAckToProto(o), s_defaultResolver);
                case Shard.GetShardStats _:
                    manifest = GetShardStatsManifest;
                    return EmptyArray<byte>.Instance;
                case Shard.ShardStats o:
                    manifest = ShardStatsManifest;
                    return MessagePackSerializer.Serialize(ShardStatsToProto(o), s_defaultResolver);
                case GetShardRegionStats _:
                    manifest = GetShardRegionStatsManifest;
                    return EmptyArray<byte>.Instance;
                case ShardRegionStats o:
                    manifest = ShardRegionStatsManifest;
                    return MessagePackSerializer.Serialize(ShardRegionStatsToProto(o), s_defaultResolver);
                case GetClusterShardingStats o:
                    manifest = GetClusterShardingStatsManifest;
                    return MessagePackSerializer.Serialize(GetClusterShardingStatsToProto(o), s_defaultResolver);
                case ClusterShardingStats o:
                    manifest = ClusterShardingStatsManifest;
                    return MessagePackSerializer.Serialize(ClusterShardingStatsToProto(o), s_defaultResolver);
                default:
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterShardingMessage(obj);
            }
        }

        /// <summary>
        /// Deserializes a byte array into an object using an optional <paramref name="manifest" /> (type hint).
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="manifest">The type hint used to deserialize the object contained in the array.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="bytes"/>cannot be deserialized using the specified <paramref name="manifest"/>.
        /// </exception>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case EntityStateManifest:
                    return EntityStateFromBinary(bytes);
                case EntityStartedManifest:
                    return EntityStartedFromBinary(bytes);
                case EntityStoppedManifest:
                    return EntityStoppedFromBinary(bytes);

                case CoordinatorStateManifest:
                    return CoordinatorStateFromBinary(bytes);
                case ShardRegionRegisteredManifest:
                    return new PersistentShardCoordinator.ShardRegionRegistered(ActorRefMessageFromBinary(bytes));
                case ShardRegionProxyRegisteredManifest:
                    return new PersistentShardCoordinator.ShardRegionProxyRegistered(ActorRefMessageFromBinary(bytes));
                case ShardRegionTerminatedManifest:
                    return new PersistentShardCoordinator.ShardRegionTerminated(ActorRefMessageFromBinary(bytes));
                case ShardRegionProxyTerminatedManifest:
                    return new PersistentShardCoordinator.ShardRegionProxyTerminated(ActorRefMessageFromBinary(bytes));
                case ShardHomeAllocatedManifest:
                    return ShardHomeAllocatedFromBinary(bytes);
                case ShardHomeDeallocatedManifest:
                    return new PersistentShardCoordinator.ShardHomeDeallocated(ShardIdMessageFromBinary(bytes));

                case RegisterManifest:
                    return new PersistentShardCoordinator.Register(ActorRefMessageFromBinary(bytes));
                case RegisterProxyManifest:
                    return new PersistentShardCoordinator.RegisterProxy(ActorRefMessageFromBinary(bytes));
                case RegisterAckManifest:
                    return new PersistentShardCoordinator.RegisterAck(ActorRefMessageFromBinary(bytes));
                case GetShardHomeManifest:
                    return new PersistentShardCoordinator.GetShardHome(ShardIdMessageFromBinary(bytes));
                case ShardHomeManifest:
                    return ShardHomeFromBinary(bytes);
                case HostShardManifest:
                    return new PersistentShardCoordinator.HostShard(ShardIdMessageFromBinary(bytes));
                case ShardStartedManifest:
                    return new PersistentShardCoordinator.ShardStarted(ShardIdMessageFromBinary(bytes));
                case BeginHandOffManifest:
                    return new PersistentShardCoordinator.BeginHandOff(ShardIdMessageFromBinary(bytes));
                case BeginHandOffAckManifest:
                    return new PersistentShardCoordinator.BeginHandOffAck(ShardIdMessageFromBinary(bytes));
                case HandOffManifest:
                    return new PersistentShardCoordinator.HandOff(ShardIdMessageFromBinary(bytes));
                case ShardStoppedManifest:
                    return new PersistentShardCoordinator.ShardStopped(ShardIdMessageFromBinary(bytes));
                case GracefulShutdownReqManifest:
                    return new PersistentShardCoordinator.GracefulShutdownRequest(ActorRefMessageFromBinary(bytes));

                case GetShardStatsManifest:
                    return Shard.GetShardStats.Instance;
                case ShardStatsManifest:
                    return ShardStatsFromBinary(bytes);

                case GetShardRegionStatsManifest:
                    return GetShardRegionStats.Instance;
                case ShardRegionStatsManifest:
                    return ShardRegionStatsFromBinary(bytes);
                case GetClusterShardingStatsManifest:
                    return GetClusterShardingStatsFromBinary(bytes);
                case ClusterShardingStatsManifest:
                    return ClusterShardingStatsFromBinary(bytes);

                case StartEntityManifest:
                    return StartEntityFromBinary(bytes);
                case StartEntityAckManifest:
                    return StartEntityAckFromBinary(bytes);

                default:
                    throw ThrowHelper.GetSerializationException_Serializer_ClusterShardingMessage(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (type is null) { return null; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            throw ThrowHelper.GetArgumentException_Serializer_ClusterShardingMessage(type);
        }

        /// <inheritdoc />
        public override string Manifest(object o)
        {
            switch (o)
            {
                case Shard.ShardState _: return EntityStateManifest;
                case Shard.EntityStarted _: return EntityStartedManifest;
                case Shard.EntityStopped _: return EntityStoppedManifest;
                case PersistentShardCoordinator.State _: return CoordinatorStateManifest;
                case PersistentShardCoordinator.ShardRegionRegistered _: return ShardRegionRegisteredManifest;
                case PersistentShardCoordinator.ShardRegionProxyRegistered _: return ShardRegionProxyRegisteredManifest;
                case PersistentShardCoordinator.ShardRegionTerminated _: return ShardRegionTerminatedManifest;
                case PersistentShardCoordinator.ShardRegionProxyTerminated _: return ShardRegionProxyTerminatedManifest;
                case PersistentShardCoordinator.ShardHomeAllocated _: return ShardHomeAllocatedManifest;
                case PersistentShardCoordinator.ShardHomeDeallocated _: return ShardHomeDeallocatedManifest;
                case PersistentShardCoordinator.Register _: return RegisterManifest;
                case PersistentShardCoordinator.RegisterProxy _: return RegisterProxyManifest;
                case PersistentShardCoordinator.RegisterAck _: return RegisterAckManifest;
                case PersistentShardCoordinator.GetShardHome _: return GetShardHomeManifest;
                case PersistentShardCoordinator.ShardHome _: return ShardHomeManifest;
                case PersistentShardCoordinator.HostShard _: return HostShardManifest;
                case PersistentShardCoordinator.ShardStarted _: return ShardStartedManifest;
                case PersistentShardCoordinator.BeginHandOff _: return BeginHandOffManifest;
                case PersistentShardCoordinator.BeginHandOffAck _: return BeginHandOffAckManifest;
                case PersistentShardCoordinator.HandOff _: return HandOffManifest;
                case PersistentShardCoordinator.ShardStopped _: return ShardStoppedManifest;
                case PersistentShardCoordinator.GracefulShutdownRequest _: return GracefulShutdownReqManifest;
                case ShardRegion.StartEntity _: return StartEntityManifest;
                case ShardRegion.StartEntityAck _: return StartEntityAckManifest;
                case Shard.GetShardStats _: return GetShardStatsManifest;
                case Shard.ShardStats _: return ShardStatsManifest;
                case GetShardRegionStats _: return GetShardRegionStatsManifest;
                case ShardRegionStats _: return ShardRegionStatsManifest;
                case GetClusterShardingStats _: return GetClusterShardingStatsManifest;
                case ClusterShardingStats _: return ClusterShardingStatsManifest;
                default:
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterShardingMessage(o);
            }
        }

        //
        // ShardStats
        //
        private static Protocol.ShardStats ShardStatsToProto(Shard.ShardStats shardStats)
        {
            return new Protocol.ShardStats(shardStats.ShardId, shardStats.EntityCount);
        }

        private static Shard.ShardStats ShardStatsFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.ShardStats>(bytes, s_defaultResolver);
            return new Shard.ShardStats(message.Shard, message.EntityCount);
        }

        //
        // ShardRegion.StartEntity
        //
        private static Protocol.StartEntity StartEntityToProto(ShardRegion.StartEntity startEntity)
        {
            return new Protocol.StartEntity(startEntity.EntityId);
        }

        private static ShardRegion.StartEntity StartEntityFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.StartEntity>(bytes, s_defaultResolver);
            return new ShardRegion.StartEntity(message.EntityId);
        }

        //
        // ShardRegion.StartEntityAck
        //
        private static Protocol.StartEntityAck StartEntityAckToProto(ShardRegion.StartEntityAck startEntityAck)
        {
            return new Protocol.StartEntityAck(startEntityAck.EntityId, startEntityAck.ShardId);
        }

        private static ShardRegion.StartEntityAck StartEntityAckFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.StartEntityAck>(bytes, s_defaultResolver);
            return new ShardRegion.StartEntityAck(message.EntityId, message.ShardId);
        }

        //
        // EntityStarted
        //
        private static Protocol.EntityStarted EntityStartedToProto(Shard.EntityStarted entityStarted)
        {
            return new Protocol.EntityStarted(entityStarted.EntityId);
        }

        private static Shard.EntityStarted EntityStartedFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.EntityStarted>(bytes, s_defaultResolver);
            return new Shard.EntityStarted(message.EntityId);
        }

        //
        // EntityStopped
        //
        private static Protocol.EntityStopped EntityStoppedToProto(Shard.EntityStopped entityStopped)
        {
            return new Protocol.EntityStopped(entityStopped.EntityId);
        }

        private static Shard.EntityStopped EntityStoppedFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.EntityStopped>(bytes, s_defaultResolver);
            return new Shard.EntityStopped(message.EntityId);
        }

        //
        // PersistentShardCoordinator.State
        //
        private static Protocol.CoordinatorState CoordinatorStateToProto(PersistentShardCoordinator.State state)
        {
            return new Protocol.CoordinatorState(
                state.Shards.Select(_ => new Protocol.ShardEntry(_.Key, Akka.Serialization.Serialization.SerializedActorPath(_.Value))).ToArray(),
                state.Regions.Keys.Select(Akka.Serialization.Serialization.SerializedActorPath).ToArray(),
                state.RegionProxies.Select(Akka.Serialization.Serialization.SerializedActorPath).ToArray(),
                state.UnallocatedShards.ToArray());
        }

        private PersistentShardCoordinator.State CoordinatorStateFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var state = MessagePackSerializer.Deserialize<Protocol.CoordinatorState>(bytes, s_defaultResolver);
            var shards = ImmutableDictionary.CreateRange(StringComparer.Ordinal, state.Shards.Select(entry => new KeyValuePair<string, IActorRef>(entry.ShardId, ResolveActorRef(entry.RegionRef))));
            var regionsZero = ImmutableDictionary.CreateRange(ActorRefComparer.Instance, state.Regions.Select(region => new KeyValuePair<IActorRef, IImmutableList<string>>(ResolveActorRef(region), ImmutableList<string>.Empty)));
            var regions = shards.Aggregate(regionsZero, (acc, entry) => acc.SetItem(entry.Value, acc[entry.Value].Add(entry.Key)));
            var proxies = state.RegionProxies.Select(ResolveActorRef).ToImmutableHashSet(ActorRefComparer.Instance);
            var unallocatedShards = state.UnallocatedShards.ToImmutableHashSet(StringComparer.Ordinal);

            return new PersistentShardCoordinator.State(
                shards: shards,
                regions: regions,
                regionProxies: proxies,
                unallocatedShards: unallocatedShards);
        }

        //
        // PersistentShardCoordinator.ShardHomeAllocated
        //
        private static Protocol.ShardHomeAllocated ShardHomeAllocatedToProto(PersistentShardCoordinator.ShardHomeAllocated shardHomeAllocated)
        {
            return new Protocol.ShardHomeAllocated(shardHomeAllocated.Shard, Akka.Serialization.Serialization.SerializedActorPath(shardHomeAllocated.Region));
        }

        private PersistentShardCoordinator.ShardHomeAllocated ShardHomeAllocatedFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var msg = MessagePackSerializer.Deserialize<Protocol.ShardHomeAllocated>(bytes, s_defaultResolver);
            return new PersistentShardCoordinator.ShardHomeAllocated(msg.Shard, ResolveActorRef(msg.Region));
        }

        //
        // PersistentShardCoordinator.ShardHome
        //
        private static Protocol.ShardHome ShardHomeToProto(PersistentShardCoordinator.ShardHome shardHome)
        {
            return new Protocol.ShardHome(shardHome.Shard, Akka.Serialization.Serialization.SerializedActorPath(shardHome.Ref));
        }

        private PersistentShardCoordinator.ShardHome ShardHomeFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var msg = MessagePackSerializer.Deserialize<Protocol.ShardHome>(bytes, s_defaultResolver);
            return new PersistentShardCoordinator.ShardHome(msg.Shard, ResolveActorRef(msg.Region));
        }

        //
        // ActorRefMessage
        //
        private static ActorRefMessage ActorRefMessageToProto(IActorRef actorRef)
        {
            return new ActorRefMessage(Akka.Serialization.Serialization.SerializedActorPath(actorRef));
        }

        private IActorRef ActorRefMessageFromBinary(in ReadOnlySpan<byte> binary)
        {
            return ResolveActorRef(MessagePackSerializer.Deserialize<ActorRefMessage>(binary, s_defaultResolver).Path);
        }

        //
        // Shard.ShardState
        //

        private static Protocol.EntityState EntityStateToProto(Shard.ShardState entityState)
        {
            return new Protocol.EntityState(entityState.Entries.ToArray());
        }

        private static Shard.ShardState EntityStateFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var msg = MessagePackSerializer.Deserialize<Protocol.EntityState>(bytes, s_defaultResolver);
            return new Shard.ShardState(msg.Entities.ToImmutableHashSet(StringComparer.Ordinal));
        }

        //
        // ShardIdMessage
        //
        private static Protocol.ShardIdMessage ShardIdMessageToProto(string shard)
        {
            return new Protocol.ShardIdMessage(shard);
        }

        private static string ShardIdMessageFromBinary(in ReadOnlySpan<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<Protocol.ShardIdMessage>(bytes, s_defaultResolver).Shard;
        }

        // ShardRegionStats
        private static Protocol.ShardRegionStats ShardRegionStatsToProto(ShardRegionStats s)
        {
            return new Protocol.ShardRegionStats(s.Stats);
        }

        private static ShardRegionStats ShardRegionStatsFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var p = MessagePackSerializer.Deserialize<Protocol.ShardRegionStats>(bytes, s_defaultResolver);
            return new ShardRegionStats(p.Stats);
        }

        // GetClusterShardingStats
        private static Protocol.GetClusterShardingStats GetClusterShardingStatsToProto(GetClusterShardingStats stats)
        {
            return new Protocol.GetClusterShardingStats(stats.Timeout);
        }

        private static GetClusterShardingStats GetClusterShardingStatsFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var p = MessagePackSerializer.Deserialize<Protocol.GetClusterShardingStats>(bytes, s_defaultResolver);
            return new GetClusterShardingStats(p.Timeout);
        }

        // ClusterShardingStats
        private static Protocol.ClusterShardingStats ClusterShardingStatsToProto(ClusterShardingStats stats)
        {
            var regions = new List<Protocol.ShardRegionWithAddress>(stats.Regions.Count);
            foreach (var s in stats.Regions)
            {
                regions.Add(new Protocol.ShardRegionWithAddress(AddressToProto(s.Key), ShardRegionStatsToProto(s.Value)));
            }

            return new Protocol.ClusterShardingStats(regions);
        }

        private static ClusterShardingStats ClusterShardingStatsFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var p = MessagePackSerializer.Deserialize<Protocol.ClusterShardingStats>(bytes, s_defaultResolver);
            var dict = new Dictionary<Address, ShardRegionStats>();
            foreach (var s in p.Regions)
            {
                dict[AddressFrom(s.NodeAddress)] = new ShardRegionStats(s.Stats.Stats);
            }
            return new ClusterShardingStats(dict.ToImmutableDictionary());
        }

        private static Akka.Serialization.Protocol.AddressData AddressToProto(Address address)
        {
            return new Akka.Serialization.Protocol.AddressData(address.System, address.Host, (uint)(address.Port ?? 0), address.Protocol);
        }

        private static Address AddressFrom(Akka.Serialization.Protocol.AddressData addressProto)
        {
            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Hostname,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        private IActorRef ResolveActorRef(string path)
        {
            return _system.Provider.ResolveActorRef(path);
        }
    }
}
