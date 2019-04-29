﻿//-----------------------------------------------------------------------
// <copyright file="ShardingMessages.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using System.Collections.Immutable;
using MessagePack;

namespace Akka.Cluster.Sharding
{
    /// <summary>
    /// TBD
    /// </summary>
    public interface IShardRegionCommand { }
    /// <summary>
    /// TBD
    /// </summary>
    public interface IShardRegionQuery { }

    /// <summary>
    /// If the state of the entries are persistent you may stop entries that are not used to
    /// reduce memory consumption. This is done by the application specific implementation of
    /// the entity actors for example by defining receive timeout (<see cref="IActorContext.SetReceiveTimeout"/>).
    /// If a message is already enqueued to the entity when it stops itself the enqueued message
    /// in the mailbox will be dropped. To support graceful passivation without loosing such
    /// messages the entity actor can send this <see cref="Passivate"/> message to its parent <see cref="ShardRegion"/>.
    /// The specified wrapped <see cref="StopMessage"/> will be sent back to the entity, which is
    /// then supposed to stop itself. Incoming messages will be buffered by the `ShardRegion`
    /// between reception of <see cref="Passivate"/> and termination of the entity. Such buffered messages
    /// are thereafter delivered to a new incarnation of the entity.
    /// 
    /// <see cref="PoisonPill"/> is a perfectly fine <see cref="StopMessage"/>.
    /// </summary>
    [MessagePackObject]
    public sealed class Passivate : IShardRegionCommand
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="stopMessage">TBD</param>
        [SerializationConstructor]
        public Passivate(object stopMessage)
        {
            StopMessage = stopMessage;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public object StopMessage { get; private set; }
    }

    /// <summary>
    /// Send this message to the <see cref="ShardRegion"/> actor to handoff all shards that are hosted by
    /// the <see cref="ShardRegion"/> and then the <see cref="ShardRegion"/> actor will be stopped. You can <see cref="ICanWatch.Watch"/>
    /// it to know when it is completed.
    /// </summary>
    public sealed class GracefulShutdown : IShardRegionCommand, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GracefulShutdown Instance = new GracefulShutdown();

        private GracefulShutdown()
        {
        }
    }

    /// <summary>
    /// We must be sure that a shard is initialized before to start send messages to it.
    /// Shard could be terminated during initialization.
    /// </summary>
    [MessagePackObject]
    public sealed class ShardInitialized
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly string ShardId;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="shardId">TBD</param>
        [SerializationConstructor]
        public ShardInitialized(string shardId)
        {
            ShardId = shardId;
        }
    }

    /// <summary>
    /// Send this message to the <see cref="ShardRegion"/> actor to request for <see cref="CurrentRegions"/>,
    /// which contains the addresses of all registered regions.
    /// Intended for testing purpose to see when cluster sharding is "ready".
    /// </summary>
    public sealed class GetCurrentRegions : IShardRegionQuery, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GetCurrentRegions Instance = new GetCurrentRegions();

        private GetCurrentRegions()
        {
        }
    }

    /// <summary>
    /// Reply to <see cref="GetCurrentRegions"/>.
    /// </summary>
    [MessagePackObject]
    public sealed class CurrentRegions
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly IImmutableSet<Address> Regions;
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="regions">TBD</param>
        [SerializationConstructor]
        public CurrentRegions(IImmutableSet<Address> regions)
        {
            Regions = regions;
        }
    }

    /// <summary>
    /// Send this message to the <see cref="ShardRegion"/> actor to request for <see cref="ClusterShardingStats"/>,
    /// which contains statistics about the currently running sharded entities in the
    /// entire cluster. If the `timeout` is reached without answers from all shard regions
    /// the reply will contain an empty map of regions.
    /// 
    /// Intended for testing purpose to see when cluster sharding is "ready" or to monitor
    /// the state of the shard regions.
    /// </summary>
    [MessagePackObject]
    public sealed class GetClusterShardingStats : IShardRegionQuery
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly TimeSpan Timeout;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="timeout">TBD</param>
        [SerializationConstructor]
        public GetClusterShardingStats(TimeSpan timeout)
        {
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Reply to <see cref="GetClusterShardingStats"/>, contains statistics about all the sharding regions in the cluster.
    /// </summary>
    [MessagePackObject]
    public sealed class ClusterShardingStats
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly IImmutableDictionary<Address, ShardRegionStats> Regions;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="regions">TBD</param>
        [SerializationConstructor]
        public ClusterShardingStats(IImmutableDictionary<Address, ShardRegionStats> regions)
        {
            Regions = regions;
        }
    }

    /// <summary>
    /// Send this message to the <see cref="ShardRegion"/> actor to request for <see cref="ShardRegionStats"/>,
    /// which contains statistics about the currently running sharded entities in the
    /// entire region.
    /// Intended for testing purpose to see when cluster sharding is "ready" or to monitor
    /// the state of the shard regions.
    /// 
    /// For the statistics for the entire cluster, see <see cref="GetClusterShardingStats"/>.
    /// </summary>
    public sealed class GetShardRegionStats : IShardRegionQuery, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GetShardRegionStats Instance = new GetShardRegionStats();

        private GetShardRegionStats()
        {
        }
    }

    /// <summary>
    /// Send this message to a <see cref="ShardRegion"/> actor instance to request a
    /// <see cref="CurrentShardRegionState"/> which describes the current state of the region.
    /// The state contains information about what shards are running in this region
    /// and what entities are running on each of those shards.
    /// </summary>
    public sealed class GetShardRegionState : IShardRegionQuery, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GetShardRegionState Instance = new GetShardRegionState();

        private GetShardRegionState()
        {
        }
    }

    /// <summary>
    /// Reply to <see cref="GetShardRegionState"/> If gathering the shard information times out the set of shards will be empty.
    /// </summary>
    [MessagePackObject]
    public sealed class CurrentShardRegionState
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly IImmutableSet<ShardState> Shards;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="shards">TBD</param>
        [SerializationConstructor]
        public CurrentShardRegionState(IImmutableSet<ShardState> shards)
        {
            Shards = shards;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class ShardRegionStats
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly IImmutableDictionary<string, int> Stats;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="stats">TBD</param>
        [SerializationConstructor]
        public ShardRegionStats(IImmutableDictionary<string, int> stats)
        {
            Stats = stats;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class ShardState
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly string ShardId;
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly IImmutableSet<string> EntityIds;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="shardId">TBD</param>
        /// <param name="entityIds">TBD</param>
        [SerializationConstructor]
        public ShardState(string shardId, IImmutableSet<string> entityIds)
        {
            ShardId = shardId;
            EntityIds = entityIds;
        }
    }
}