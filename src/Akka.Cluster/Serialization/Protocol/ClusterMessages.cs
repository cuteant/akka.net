using System;
using System.Collections.Generic;
using Akka.Remote.Serialization.Protocol;
using MessagePack;

namespace Akka.Cluster.Serialization.Protocol
{
    /****************************************
     * Internal Cluster Action Messages
     ****************************************/

    // Join
    [MessagePackObject]
    public readonly struct Join
    {
        [Key(0)]
        public readonly UniqueAddress Node;
        [Key(1)]
        public readonly string[] Roles;

        [SerializationConstructor]
        public Join(UniqueAddress node, string[] roles)
        {
            Node = node;
            Roles = roles;
        }
    }

    // Welcome, reply to Join
    [MessagePackObject]
    public readonly struct Welcome
    {
        [Key(0)]
        public readonly UniqueAddress From;
        [Key(1)]
        public readonly Gossip Gossip;

        [SerializationConstructor]
        public Welcome(UniqueAddress from, Gossip gossip)
        {
            From = from;
            Gossip = gossip;
        }
    }


    /****************************************
     * Cluster Gossip Messages
     ****************************************/

    // Gossip Envelope
    [MessagePackObject]
    public readonly struct GossipEnvelope
    {
        [Key(0)]
        public readonly UniqueAddress From;
        [Key(1)]
        public readonly UniqueAddress To;
        [Key(2)]
        public readonly byte[] SerializedGossip;

        [SerializationConstructor]
        public GossipEnvelope(UniqueAddress from, UniqueAddress to, byte[] serializedGossip)
        {
            From = from;
            To = to;
            SerializedGossip = serializedGossip;
        }
    }

    // Gossip Status
    [MessagePackObject]
    public readonly struct GossipStatus
    {
        [Key(0)]
        public readonly UniqueAddress From;
        [Key(1)]
        public readonly string[] AllHashes;
        [Key(2)]
        public readonly VectorClock Version;

        [SerializationConstructor]
        public GossipStatus(UniqueAddress from, string[] allHashes, VectorClock version)
        {
            From = from;
            AllHashes = allHashes;
            Version = version;
        }
    }

    // Gossip
    [MessagePackObject]
    public sealed class Gossip
    {
        [Key(0)]
        public UniqueAddress[] AllAddresses { get; set; }
        [Key(1)]
        public string[] AllRoles { get; set; }
        [Key(2)]
        public string[] AllHashes { get; set; }
        [Key(3)]
        public Member[] Members { get; set; }
        [Key(4)]
        public GossipOverview Overview { get; set; }
        [Key(5)]
        public VectorClock Version { get; set; }
    }

    // Gossip Overview
    [MessagePackObject]
    public readonly struct GossipOverview
    {
        [Key(0)]
        public readonly int[] Seen;
        [Key(1)]
        public readonly List<ObserverReachability> ObserverReachability;

        [SerializationConstructor]
        public GossipOverview(int[] seen, List<ObserverReachability> observerReachability)
        {
            Seen = seen;
            ObserverReachability = observerReachability;
        }
    }

    // Reachability
    [MessagePackObject]
    public readonly struct ObserverReachability
    {
        [Key(0)]
        public readonly int AddressIndex;
        [Key(1)]
        public readonly long Version;
        [Key(2)]
        public readonly SubjectReachability[] SubjectReachability;

        [SerializationConstructor]
        public ObserverReachability(int addressIndex, long version, SubjectReachability[] subjectReachability)
        {
            AddressIndex = addressIndex;
            Version = version;
            SubjectReachability = subjectReachability;
        }
    }

    public enum ReachabilityStatus
    {
        Reachable = 0,
        Unreachable = 1,
        Terminated = 2,
    }

    [MessagePackObject]
    public readonly struct SubjectReachability
    {
        [Key(0)]
        public readonly int AddressIndex;
        [Key(1)]
        public readonly ReachabilityStatus Status;
        [Key(2)]
        public readonly long Version;

        [SerializationConstructor]
        public SubjectReachability(int addressIndex, ReachabilityStatus status, long version)
        {
            AddressIndex = addressIndex;
            Status = status;
            Version = version;
        }
    }

    public enum MemberStatus
    {
        Joining = 0,
        Up = 1,
        Leaving = 2,
        Exiting = 3,
        Down = 4,
        Removed = 5,
        WeaklyUp = 6,
    }

    // Member
    [MessagePackObject]
    public readonly struct Member
    {
        [Key(0)]
        public readonly int AddressIndex;
        [Key(1)]
        public readonly int UpNumber;
        [Key(2)]
        public readonly MemberStatus Status;
        [Key(3)]
        public readonly int[] RolesIndexes;

        [SerializationConstructor]
        public Member(int addressIndex, int upNumber, MemberStatus status, int[] rolesIndexes)
        {
            AddressIndex = addressIndex;
            UpNumber = upNumber;
            Status = status;
            RolesIndexes = rolesIndexes;
        }
    }

    [MessagePackObject]
    public readonly struct Version
    {
        [Key(0)]
        public readonly int HashIndex;
        [Key(1)]
        public readonly long Timestamp;

        [SerializationConstructor]
        public Version(int hashIndex, long timestamp)
        {
            HashIndex = hashIndex;
            Timestamp = timestamp;
        }
    }

    // Vector Clock
    [MessagePackObject]
    public readonly struct VectorClock
    {
        [Key(0)]
        public readonly long Timestamp;
        [Key(1)]
        public readonly Version[] Versions;

        [SerializationConstructor]
        public VectorClock(long timestamp, Version[] versions)
        {
            Timestamp = timestamp;
            Versions = versions;
        }
    }


    /****************************************
     * Common Datatypes and Messages
     ****************************************/

    // Defines a remote address with uid.
    [MessagePackObject]
    public readonly struct UniqueAddress
    {
        [Key(0)]
        public readonly AddressData Address;
        [Key(1)]
        public readonly uint Uid;

        [SerializationConstructor]
        public UniqueAddress(AddressData address, uint uid)
        {
            Address = address;
            Uid = uid;
        }
    }


    /****************************************
     * Cluster routing
     ****************************************/

    [MessagePackObject]
    public readonly struct ClusterRouterPool
    {
        [Key(0)]
        public readonly Payload Pool;
        [Key(1)]
        public readonly ClusterRouterPoolSettings Settings;

        [SerializationConstructor]
        public ClusterRouterPool(Payload pool, ClusterRouterPoolSettings settings)
        {
            Pool = pool;
            Settings = settings;
        }
    }

    [MessagePackObject]
    public readonly struct ClusterRouterPoolSettings
    {
        [Key(0)]
        public readonly uint TotalInstances;
        [Key(1)]
        public readonly uint MaxInstancesPerNode;
        [Key(2)]
        public readonly bool AllowLocalRoutees;
        [Key(3)]
        public readonly string UseRole;

        [SerializationConstructor]
        public ClusterRouterPoolSettings(uint totalInstances, uint maxInstancesPerNode, bool allowLocalRoutees, string useRole)
        {
            TotalInstances = totalInstances;
            MaxInstancesPerNode = maxInstancesPerNode;
            AllowLocalRoutees = allowLocalRoutees;
            UseRole = useRole;
        }
    }
}
