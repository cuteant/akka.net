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
using CuteAnt.Text;
using ActorRefMessage = Akka.Remote.Serialization.Proto.Msg.ActorRefData;

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

        private readonly Dictionary<string, Func<byte[], object>> _fromBinaryMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterShardingMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public ClusterShardingMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _fromBinaryMap = new Dictionary<string, Func<byte[], object>>(StringComparer.Ordinal)
            {
                {EntityStateManifest, EntityStateFromBinary},
                {EntityStartedManifest, EntityStartedFromBinary},
                {EntityStoppedManifest, EntityStoppedFromBinary},

                {CoordinatorStateManifest, CoordinatorStateFromBinary},
                {ShardRegionRegisteredManifest, bytes => new PersistentShardCoordinator.ShardRegionRegistered(ActorRefMessageFromBinary(bytes)) },
                {ShardRegionProxyRegisteredManifest, bytes => new PersistentShardCoordinator.ShardRegionProxyRegistered(ActorRefMessageFromBinary(bytes)) },
                {ShardRegionTerminatedManifest, bytes => new PersistentShardCoordinator.ShardRegionTerminated(ActorRefMessageFromBinary(bytes)) },
                {ShardRegionProxyTerminatedManifest, bytes => new PersistentShardCoordinator.ShardRegionProxyTerminated(ActorRefMessageFromBinary(bytes)) },
                {ShardHomeAllocatedManifest, ShardHomeAllocatedFromBinary},
                {ShardHomeDeallocatedManifest, bytes => new PersistentShardCoordinator.ShardHomeDeallocated(ShardIdMessageFromBinary(bytes)) },

                {RegisterManifest, bytes => new PersistentShardCoordinator.Register(ActorRefMessageFromBinary(bytes)) },
                {RegisterProxyManifest, bytes => new PersistentShardCoordinator.RegisterProxy(ActorRefMessageFromBinary(bytes)) },
                {RegisterAckManifest, bytes => new PersistentShardCoordinator.RegisterAck(ActorRefMessageFromBinary(bytes)) },
                {GetShardHomeManifest, bytes => new PersistentShardCoordinator.GetShardHome(ShardIdMessageFromBinary(bytes)) },
                {ShardHomeManifest, ShardHomeFromBinary},
                {HostShardManifest, bytes => new PersistentShardCoordinator.HostShard(ShardIdMessageFromBinary(bytes)) },
                {ShardStartedManifest, bytes => new PersistentShardCoordinator.ShardStarted(ShardIdMessageFromBinary(bytes)) },
                {BeginHandOffManifest, bytes => new PersistentShardCoordinator.BeginHandOff(ShardIdMessageFromBinary(bytes)) },
                {BeginHandOffAckManifest, bytes => new PersistentShardCoordinator.BeginHandOffAck(ShardIdMessageFromBinary(bytes)) },
                {HandOffManifest, bytes => new PersistentShardCoordinator.HandOff(ShardIdMessageFromBinary(bytes)) },
                {ShardStoppedManifest, bytes => new PersistentShardCoordinator.ShardStopped(ShardIdMessageFromBinary(bytes)) },
                {GracefulShutdownReqManifest, bytes => new PersistentShardCoordinator.GracefulShutdownRequest(ActorRefMessageFromBinary(bytes)) },

                {GetShardStatsManifest, bytes => Shard.GetShardStats.Instance },
                {ShardStatsManifest, ShardStatsFromBinary},

                {StartEntityManifest, StartEntityFromBinary },
                {StartEntityAckManifest, StartEntityAckFromBinary}
            };
        }

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
                case PersistentShardCoordinator.State o: return CoordinatorStateToProto(o).ToArray();
                case PersistentShardCoordinator.ShardRegionRegistered o: return ActorRefMessageToProto(o.Region).ToArray();
                case PersistentShardCoordinator.ShardRegionProxyRegistered o: return ActorRefMessageToProto(o.RegionProxy).ToArray();
                case PersistentShardCoordinator.ShardRegionTerminated o: return ActorRefMessageToProto(o.Region).ToArray();
                case PersistentShardCoordinator.ShardRegionProxyTerminated o: return ActorRefMessageToProto(o.RegionProxy).ToArray();
                case PersistentShardCoordinator.ShardHomeAllocated o: return ShardHomeAllocatedToProto(o).ToArray();
                case PersistentShardCoordinator.ShardHomeDeallocated o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.Register o: return ActorRefMessageToProto(o.ShardRegion).ToArray();
                case PersistentShardCoordinator.RegisterProxy o: return ActorRefMessageToProto(o.ShardRegionProxy).ToArray();
                case PersistentShardCoordinator.RegisterAck o: return ActorRefMessageToProto(o.Coordinator).ToArray();
                case PersistentShardCoordinator.GetShardHome o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.ShardHome o: return ShardHomeToProto(o).ToArray();
                case PersistentShardCoordinator.HostShard o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.ShardStarted o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.BeginHandOff o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.BeginHandOffAck o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.HandOff o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.ShardStopped o: return ShardIdMessageToProto(o.Shard).ToArray();
                case PersistentShardCoordinator.GracefulShutdownRequest o: return ActorRefMessageToProto(o.ShardRegion).ToArray();
                case Shard.ShardState o: return EntityStateToProto(o).ToArray();
                case Shard.EntityStarted o: return EntityStartedToProto(o).ToArray();
                case Shard.EntityStopped o: return EntityStoppedToProto(o).ToArray();
                case ShardRegion.StartEntity o: return StartEntityToProto(o).ToArray();
                case ShardRegion.StartEntityAck o: return StartEntityAckToProto(o).ToArray();
                case Shard.GetShardStats o: return new byte[0];
                case Shard.ShardStats o: return ShardStatsToProto(o).ToArray();
            }
            throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{this.GetType()}]");
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
            if (_fromBinaryMap.TryGetValue(manifest, out var factory))
                return factory(bytes);

            throw new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [{this.GetType()}]");
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
            throw new ArgumentException($"Can't serialize object of type [{o.GetType()}] in [{this.GetType()}]");
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
            throw new ArgumentException($"Can't serialize object of type [{o.GetType()}] in [{this.GetType()}]");
        }

        //
        // ShardStats
        //
        private static Proto.Msg.ShardStats ShardStatsToProto(Shard.ShardStats shardStats)
        {
            var message = new Proto.Msg.ShardStats
            {
                Shard = shardStats.ShardId,
                EntityCount = shardStats.EntityCount
            };
            return message;
        }

        private static Shard.ShardStats ShardStatsFromBinary(byte[] bytes)
        {
            var message = Proto.Msg.ShardStats.Parser.ParseFrom(bytes);
            return new Shard.ShardStats(message.Shard, message.EntityCount);
        }

        //
        // ShardRegion.StartEntity
        //
        private static Proto.Msg.StartEntity StartEntityToProto(ShardRegion.StartEntity startEntity)
        {
            var message = new Proto.Msg.StartEntity
            {
                EntityId = startEntity.EntityId
            };
            return message;
        }

        private static ShardRegion.StartEntity StartEntityFromBinary(byte[] bytes)
        {
            var message = Proto.Msg.StartEntity.Parser.ParseFrom(bytes);
            return new ShardRegion.StartEntity(message.EntityId);
        }

        //
        // ShardRegion.StartEntityAck
        //
        private static Proto.Msg.StartEntityAck StartEntityAckToProto(ShardRegion.StartEntityAck startEntityAck)
        {
            var message = new Proto.Msg.StartEntityAck
            {
                EntityId = startEntityAck.EntityId,
                ShardId = startEntityAck.ShardId
            };
            return message;
        }

        private static ShardRegion.StartEntityAck StartEntityAckFromBinary(byte[] bytes)
        {
            var message = Proto.Msg.StartEntityAck.Parser.ParseFrom(bytes);
            return new ShardRegion.StartEntityAck(message.EntityId, message.ShardId);
        }

        //
        // EntityStarted
        //
        private static Proto.Msg.EntityStarted EntityStartedToProto(Shard.EntityStarted entityStarted)
        {
            var message = new Proto.Msg.EntityStarted
            {
                EntityId = entityStarted.EntityId
            };
            return message;
        }

        private static Shard.EntityStarted EntityStartedFromBinary(byte[] bytes)
        {
            var message = Proto.Msg.EntityStarted.Parser.ParseFrom(bytes);
            return new Shard.EntityStarted(message.EntityId);
        }

        //
        // EntityStopped
        //
        private static Proto.Msg.EntityStopped EntityStoppedToProto(Shard.EntityStopped entityStopped)
        {
            var message = new Proto.Msg.EntityStopped
            {
                EntityId = entityStopped.EntityId
            };
            return message;
        }

        private static Shard.EntityStopped EntityStoppedFromBinary(byte[] bytes)
        {
            var message = Proto.Msg.EntityStopped.Parser.ParseFrom(bytes);
            return new Shard.EntityStopped(message.EntityId);
        }

        //
        // PersistentShardCoordinator.State
        //
        private static Proto.Msg.CoordinatorState CoordinatorStateToProto(PersistentShardCoordinator.State state)
        {
            var message = new Proto.Msg.CoordinatorState();
            message.Shards.AddRange(state.Shards.Select(entry =>
            {
                var coordinatorState = new Proto.Msg.CoordinatorState.Types.ShardEntry
                {
                    ShardId = entry.Key,
                    RegionRef = Akka.Serialization.Serialization.SerializedActorPath(entry.Value)
                };
                return coordinatorState;
            }));

            message.Regions.AddRange(state.Regions.Keys.Select(Akka.Serialization.Serialization.SerializedActorPath));
            message.RegionProxies.AddRange(state.RegionProxies.Select(Akka.Serialization.Serialization.SerializedActorPath));
            message.UnallocatedShards.AddRange(state.UnallocatedShards);

            return message;
        }

        private PersistentShardCoordinator.State CoordinatorStateFromBinary(byte[] bytes)
        {
            var state = Proto.Msg.CoordinatorState.Parser.ParseFrom(bytes);
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
        private static Proto.Msg.ShardHomeAllocated ShardHomeAllocatedToProto(PersistentShardCoordinator.ShardHomeAllocated shardHomeAllocated)
        {
            var message = new Proto.Msg.ShardHomeAllocated
            {
                Shard = shardHomeAllocated.Shard,
                Region = Akka.Serialization.Serialization.SerializedActorPath(shardHomeAllocated.Region)
            };
            return message;
        }

        private PersistentShardCoordinator.ShardHomeAllocated ShardHomeAllocatedFromBinary(byte[] bytes)
        {
            var msg = Proto.Msg.ShardHomeAllocated.Parser.ParseFrom(bytes);
            return new PersistentShardCoordinator.ShardHomeAllocated(msg.Shard, ResolveActorRef(msg.Region));
        }

        //
        // PersistentShardCoordinator.ShardHome
        //
        private static Proto.Msg.ShardHome ShardHomeToProto(PersistentShardCoordinator.ShardHome shardHome)
        {
            var message = new Proto.Msg.ShardHome
            {
                Shard = shardHome.Shard,
                Region = Akka.Serialization.Serialization.SerializedActorPath(shardHome.Ref)
            };
            return message;
        }

        private PersistentShardCoordinator.ShardHome ShardHomeFromBinary(byte[] bytes)
        {
            var msg = Proto.Msg.ShardHome.Parser.ParseFrom(bytes);
            return new PersistentShardCoordinator.ShardHome(msg.Shard, ResolveActorRef(msg.Region));
        }

        //
        // ActorRefMessage
        //
        private static ActorRefMessage ActorRefMessageToProto(IActorRef actorRef)
        {
            var message = new ActorRefMessage
            {
                Path = Akka.Serialization.Serialization.SerializedActorPath(actorRef)
            };
            return message;
        }

        private IActorRef ActorRefMessageFromBinary(byte[] binary)
        {
            return ResolveActorRef(ActorRefMessage.Parser.ParseFrom(binary).Path);
        }

        //
        // Shard.ShardState
        //

        private static Proto.Msg.EntityState EntityStateToProto(Shard.ShardState entityState)
        {
            var message = new Proto.Msg.EntityState();
            message.Entities.AddRange(entityState.Entries);
            return message;
        }

        private static Shard.ShardState EntityStateFromBinary(byte[] bytes)
        {
            var msg = Proto.Msg.EntityState.Parser.ParseFrom(bytes);
            return new Shard.ShardState(msg.Entities.ToImmutableHashSet());
        }

        //
        // ShardIdMessage
        //
        private static Proto.Msg.ShardIdMessage ShardIdMessageToProto(string shard)
        {
            var message = new Proto.Msg.ShardIdMessage
            {
                Shard = shard
            };
            return message;
        }

        private static string ShardIdMessageFromBinary(byte[] bytes)
        {
            return Proto.Msg.ShardIdMessage.Parser.ParseFrom(bytes).Shard;
        }

        private IActorRef ResolveActorRef(string path)
        {
            return system.Provider.ResolveActorRef(path);
        }
    }
}
