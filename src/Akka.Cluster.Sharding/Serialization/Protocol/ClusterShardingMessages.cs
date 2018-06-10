using MessagePack;

namespace Akka.Cluster.Sharding.Serialization.Protocol
{
    [MessagePackObject]
    public readonly struct ShardEntry
    {
        [Key(0)]
        public readonly string ShardId;
        [Key(1)]
        public readonly string RegionRef;

        [SerializationConstructor]
        public ShardEntry(string shardId, string regionRef)
        {
            ShardId = shardId;
            RegionRef = regionRef;
        }
    }

    [MessagePackObject]
    public readonly struct CoordinatorState
    {
        [Key(0)]
        public readonly ShardEntry[] Shards;
        [Key(1)]
        public readonly string[] Regions;
        [Key(2)]
        public readonly string[] RegionProxies;
        [Key(3)]
        public readonly string[] UnallocatedShards;

        [SerializationConstructor]
        public CoordinatorState(ShardEntry[] shards, string[] regions, string[] regionProxies, string[] unallocatedShards)
        {
            Shards = shards;
            Regions = regions;
            RegionProxies = regionProxies;
            UnallocatedShards = unallocatedShards;
        }
    }

    [MessagePackObject]
    public readonly struct ActorRefMessage
    {
        [Key(0)]
        public readonly string Ref;

        [SerializationConstructor]
        public ActorRefMessage(string r) => Ref = r;
    }

    [MessagePackObject]
    public readonly struct ShardIdMessage
    {
        [Key(0)]
        public readonly string Shard;

        [SerializationConstructor]
        public ShardIdMessage(string shard) => Shard = shard;
    }

    [MessagePackObject]
    public readonly struct ShardHomeAllocated
    {
        [Key(0)]
        public readonly string Shard;
        [Key(1)]
        public readonly string Region;

        [SerializationConstructor]
        public ShardHomeAllocated(string shard, string region)
        {
            Shard = shard;
            Region = region;
        }
    }

    [MessagePackObject]
    public readonly struct ShardHome
    {
        [Key(0)]
        public readonly string Shard;
        [Key(1)]
        public readonly string Region;

        [SerializationConstructor]
        public ShardHome(string shard, string region)
        {
            Shard = shard;
            Region = region;
        }
    }

    [MessagePackObject]
    public readonly struct EntityState
    {
        [Key(0)]
        public readonly string[] Entities;

        [SerializationConstructor]
        public EntityState(string[] entities) => Entities = entities;
    }

    [MessagePackObject]
    public readonly struct EntityStarted
    {
        [Key(0)]
        public readonly string EntityId;

        [SerializationConstructor]
        public EntityStarted(string entityId) => EntityId = entityId;
    }

    [MessagePackObject]
    public readonly struct EntityStopped
    {
        [Key(0)]
        public readonly string EntityId;

        [SerializationConstructor]
        public EntityStopped(string entityId) => EntityId = entityId;
    }

    [MessagePackObject]
    public readonly struct ShardStats
    {
        [Key(0)]
        public readonly string Shard;
        [Key(1)]
        public readonly int EntityCount;

        [SerializationConstructor]
        public ShardStats(string shard, int entityCount)
        {
            Shard = shard;
            EntityCount = entityCount;
        }
    }

    [MessagePackObject]
    public readonly struct StartEntity
    {
        [Key(0)]
        public readonly string EntityId;

        [SerializationConstructor]
        public StartEntity(string entityId) => EntityId = entityId;
    }

    [MessagePackObject]
    public readonly struct StartEntityAck
    {
        [Key(0)]
        public readonly string EntityId;
        [Key(1)]
        public readonly string ShardId;

        [SerializationConstructor]
        public StartEntityAck(string entityId, string shardId)
        {
            EntityId = entityId;
            ShardId = shardId;
        }
    }
}
