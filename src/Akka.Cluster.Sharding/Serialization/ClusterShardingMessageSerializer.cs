//-----------------------------------------------------------------------
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
using CuteAnt.Text;
using MessagePack;
using ActorRefMessage = Akka.Remote.Serialization.Protocol.ActorRefData;

namespace Akka.Cluster.Sharding.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterShardingMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string CoordinatorStateManifest = "AA";
        private static readonly byte[] CoordinatorStateManifestBytes = StringHelper.UTF8NoBOM.GetBytes(CoordinatorStateManifest);
        private const string ShardRegionRegisteredManifest = "AB";
        private static readonly byte[] ShardRegionRegisteredManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardRegionRegisteredManifest);
        private const string ShardRegionProxyRegisteredManifest = "AC";
        private static readonly byte[] ShardRegionProxyRegisteredManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardRegionProxyRegisteredManifest);
        private const string ShardRegionTerminatedManifest = "AD";
        private static readonly byte[] ShardRegionTerminatedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardRegionTerminatedManifest);
        private const string ShardRegionProxyTerminatedManifest = "AE";
        private static readonly byte[] ShardRegionProxyTerminatedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardRegionProxyTerminatedManifest);
        private const string ShardHomeAllocatedManifest = "AF";
        private static readonly byte[] ShardHomeAllocatedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardHomeAllocatedManifest);
        private const string ShardHomeDeallocatedManifest = "AG";
        private static readonly byte[] ShardHomeDeallocatedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardHomeDeallocatedManifest);

        private const string RegisterManifest = "BA";
        private static readonly byte[] RegisterManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RegisterManifest);
        private const string RegisterProxyManifest = "BB";
        private static readonly byte[] RegisterProxyManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RegisterProxyManifest);
        private const string RegisterAckManifest = "BC";
        private static readonly byte[] RegisterAckManifestBytes = StringHelper.UTF8NoBOM.GetBytes(RegisterAckManifest);
        private const string GetShardHomeManifest = "BD";
        private static readonly byte[] GetShardHomeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(GetShardHomeManifest);
        private const string ShardHomeManifest = "BE";
        private static readonly byte[] ShardHomeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardHomeManifest);
        private const string HostShardManifest = "BF";
        private static readonly byte[] HostShardManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HostShardManifest);
        private const string ShardStartedManifest = "BG";
        private static readonly byte[] ShardStartedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardStartedManifest);
        private const string BeginHandOffManifest = "BH";
        private static readonly byte[] BeginHandOffManifestBytes = StringHelper.UTF8NoBOM.GetBytes(BeginHandOffManifest);
        private const string BeginHandOffAckManifest = "BI";
        private static readonly byte[] BeginHandOffAckManifestBytes = StringHelper.UTF8NoBOM.GetBytes(BeginHandOffAckManifest);
        private const string HandOffManifest = "BJ";
        private static readonly byte[] HandOffManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HandOffManifest);
        private const string ShardStoppedManifest = "BK";
        private static readonly byte[] ShardStoppedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardStoppedManifest);
        private const string GracefulShutdownReqManifest = "BL";
        private static readonly byte[] GracefulShutdownReqManifestBytes = StringHelper.UTF8NoBOM.GetBytes(GracefulShutdownReqManifest);

        private const string EntityStateManifest = "CA";
        private static readonly byte[] EntityStateManifestBytes = StringHelper.UTF8NoBOM.GetBytes(EntityStateManifest);
        private const string EntityStartedManifest = "CB";
        private static readonly byte[] EntityStartedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(EntityStartedManifest);
        private const string EntityStoppedManifest = "CD";
        private static readonly byte[] EntityStoppedManifestBytes = StringHelper.UTF8NoBOM.GetBytes(EntityStoppedManifest);

        private const string StartEntityManifest = "EA";
        private static readonly byte[] StartEntityManifestBytes = StringHelper.UTF8NoBOM.GetBytes(StartEntityManifest);
        private const string StartEntityAckManifest = "EB";
        private static readonly byte[] StartEntityAckManifestBytes = StringHelper.UTF8NoBOM.GetBytes(StartEntityAckManifest);

        private const string GetShardStatsManifest = "DA";
        private static readonly byte[] GetShardStatsManifestBytes = StringHelper.UTF8NoBOM.GetBytes(GetShardStatsManifest);
        private const string ShardStatsManifest = "DB";
        private static readonly byte[] ShardStatsManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ShardStatsManifest);

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterShardingMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public ClusterShardingMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="obj"/> is of an unknown type.
        /// </exception>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case PersistentShardCoordinator.State o: return MessagePackSerializer.Serialize(CoordinatorStateToProto(o), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionRegistered o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.Region), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionProxyRegistered o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.RegionProxy), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionTerminated o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.Region), s_defaultResolver);
                case PersistentShardCoordinator.ShardRegionProxyTerminated o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.RegionProxy), s_defaultResolver);
                case PersistentShardCoordinator.ShardHomeAllocated o: return MessagePackSerializer.Serialize(ShardHomeAllocatedToProto(o), s_defaultResolver);
                case PersistentShardCoordinator.ShardHomeDeallocated o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.Register o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.ShardRegion), s_defaultResolver);
                case PersistentShardCoordinator.RegisterProxy o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.ShardRegionProxy), s_defaultResolver);
                case PersistentShardCoordinator.RegisterAck o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.Coordinator), s_defaultResolver);
                case PersistentShardCoordinator.GetShardHome o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.ShardHome o: return MessagePackSerializer.Serialize(ShardHomeToProto(o), s_defaultResolver);
                case PersistentShardCoordinator.HostShard o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.ShardStarted o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.BeginHandOff o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.BeginHandOffAck o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.HandOff o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.ShardStopped o: return MessagePackSerializer.Serialize(ShardIdMessageToProto(o.Shard), s_defaultResolver);
                case PersistentShardCoordinator.GracefulShutdownRequest o: return MessagePackSerializer.Serialize(ActorRefMessageToProto(o.ShardRegion), s_defaultResolver);
                case Shard.ShardState o: return MessagePackSerializer.Serialize(EntityStateToProto(o), s_defaultResolver);
                case Shard.EntityStarted o: return MessagePackSerializer.Serialize(EntityStartedToProto(o), s_defaultResolver);
                case Shard.EntityStopped o: return MessagePackSerializer.Serialize(EntityStoppedToProto(o), s_defaultResolver);
                case ShardRegion.StartEntity o: return MessagePackSerializer.Serialize(StartEntityToProto(o), s_defaultResolver);
                case ShardRegion.StartEntityAck o: return MessagePackSerializer.Serialize(StartEntityAckToProto(o), s_defaultResolver);
                case Shard.GetShardStats _: return EmptyArray<byte>.Instance;
                case Shard.ShardStats o: return MessagePackSerializer.Serialize(ShardStatsToProto(o), s_defaultResolver);
            }
            return ThrowHelper.ThrowArgumentException_Serializer_ClusterShardingMessage<byte[]>(obj);
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

                case StartEntityManifest:
                    return StartEntityFromBinary(bytes);
                case StartEntityAckManifest:
                    return StartEntityAckFromBinary(bytes);

                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterShardingMessage(manifest);
            }
        }

        /// <summary>
        /// Returns the manifest (type hint) that will be provided in the <see cref="FromBinary(System.Byte[],System.String)" /> method.
        /// <note>
        /// This method returns <see cref="String.Empty" /> if a manifest is not needed.
        /// </note>
        /// </summary>
        /// <param name="o">The object for which the manifest is needed.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="o"/> does not have an associated manifest.
        /// </exception>
        /// <returns>The manifest needed for the deserialization of the specified <paramref name="o" />.</returns>
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
            }
            return ThrowHelper.ThrowArgumentException_Serializer_ClusterShardingMessage<string>(o);
        }

        /// <inheritdoc />
        public override byte[] ManifestBytes(object o)
        {
            switch (o)
            {
                case Shard.ShardState _: return EntityStateManifestBytes;
                case Shard.EntityStarted _: return EntityStartedManifestBytes;
                case Shard.EntityStopped _: return EntityStoppedManifestBytes;
                case PersistentShardCoordinator.State _: return CoordinatorStateManifestBytes;
                case PersistentShardCoordinator.ShardRegionRegistered _: return ShardRegionRegisteredManifestBytes;
                case PersistentShardCoordinator.ShardRegionProxyRegistered _: return ShardRegionProxyRegisteredManifestBytes;
                case PersistentShardCoordinator.ShardRegionTerminated _: return ShardRegionTerminatedManifestBytes;
                case PersistentShardCoordinator.ShardRegionProxyTerminated _: return ShardRegionProxyTerminatedManifestBytes;
                case PersistentShardCoordinator.ShardHomeAllocated _: return ShardHomeAllocatedManifestBytes;
                case PersistentShardCoordinator.ShardHomeDeallocated _: return ShardHomeDeallocatedManifestBytes;
                case PersistentShardCoordinator.Register _: return RegisterManifestBytes;
                case PersistentShardCoordinator.RegisterProxy _: return RegisterProxyManifestBytes;
                case PersistentShardCoordinator.RegisterAck _: return RegisterAckManifestBytes;
                case PersistentShardCoordinator.GetShardHome _: return GetShardHomeManifestBytes;
                case PersistentShardCoordinator.ShardHome _: return ShardHomeManifestBytes;
                case PersistentShardCoordinator.HostShard _: return HostShardManifestBytes;
                case PersistentShardCoordinator.ShardStarted _: return ShardStartedManifestBytes;
                case PersistentShardCoordinator.BeginHandOff _: return BeginHandOffManifestBytes;
                case PersistentShardCoordinator.BeginHandOffAck _: return BeginHandOffAckManifestBytes;
                case PersistentShardCoordinator.HandOff _: return HandOffManifestBytes;
                case PersistentShardCoordinator.ShardStopped _: return ShardStoppedManifestBytes;
                case PersistentShardCoordinator.GracefulShutdownRequest _: return GracefulShutdownReqManifestBytes;
                case ShardRegion.StartEntity _: return StartEntityManifestBytes;
                case ShardRegion.StartEntityAck _: return StartEntityAckManifestBytes;
                case Shard.GetShardStats _: return GetShardStatsManifestBytes;
                case Shard.ShardStats _: return ShardStatsManifestBytes;
            }
            return ThrowHelper.ThrowArgumentException_Serializer_ClusterShardingMessage<byte[]>(o);
        }

        //
        // ShardStats
        //
        private static Protocol.ShardStats ShardStatsToProto(Shard.ShardStats shardStats)
        {
            return new Protocol.ShardStats(shardStats.ShardId, shardStats.EntityCount);
        }

        private static Shard.ShardStats ShardStatsFromBinary(byte[] bytes)
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

        private static ShardRegion.StartEntity StartEntityFromBinary(byte[] bytes)
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

        private static ShardRegion.StartEntityAck StartEntityAckFromBinary(byte[] bytes)
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

        private static Shard.EntityStarted EntityStartedFromBinary(byte[] bytes)
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

        private static Shard.EntityStopped EntityStoppedFromBinary(byte[] bytes)
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

        private PersistentShardCoordinator.State CoordinatorStateFromBinary(byte[] bytes)
        {
            var state = MessagePackSerializer.Deserialize<Protocol.CoordinatorState>(bytes, s_defaultResolver);
            var shards = ImmutableDictionary.CreateRange(state.Shards.Select(entry => new KeyValuePair<string, IActorRef>(entry.ShardId, ResolveActorRef(entry.RegionRef))));
            var regionsZero = ImmutableDictionary.CreateRange(state.Regions.Select(region => new KeyValuePair<IActorRef, IImmutableList<string>>(ResolveActorRef(region), ImmutableList<string>.Empty)));
            var regions = shards.Aggregate(regionsZero, (acc, entry) => acc.SetItem(entry.Value, acc[entry.Value].Add(entry.Key)));
            var proxies = state.RegionProxies.Select(ResolveActorRef).ToImmutableHashSet();
            var unallocatedShards = state.UnallocatedShards.ToImmutableHashSet();

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

        private PersistentShardCoordinator.ShardHomeAllocated ShardHomeAllocatedFromBinary(byte[] bytes)
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

        private PersistentShardCoordinator.ShardHome ShardHomeFromBinary(byte[] bytes)
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

        private IActorRef ActorRefMessageFromBinary(byte[] binary)
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

        private static Shard.ShardState EntityStateFromBinary(byte[] bytes)
        {
            var msg = MessagePackSerializer.Deserialize<Protocol.EntityState>(bytes, s_defaultResolver);
            return new Shard.ShardState(msg.Entities.ToImmutableHashSet());
        }

        //
        // ShardIdMessage
        //
        private static Protocol.ShardIdMessage ShardIdMessageToProto(string shard)
        {
            return new Protocol.ShardIdMessage(shard);
        }

        private static string ShardIdMessageFromBinary(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<Protocol.ShardIdMessage>(bytes, s_defaultResolver).Shard;
        }

        private IActorRef ResolveActorRef(string path)
        {
            return system.Provider.ResolveActorRef(path);
        }
    }
}
