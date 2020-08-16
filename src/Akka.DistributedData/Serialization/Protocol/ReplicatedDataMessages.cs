using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MessagePack;
using OtherMessage = Akka.Serialization.Protocol.Payload;

namespace Akka.DistributedData.Serialization.Protocol
{
    public enum ValType
    {
        Int = 0,
        Long = 1,
        String = 2,
        ActorRef = 3,
        Other = 4,
    }

    [MessagePackObject]
    public readonly struct TypeDescriptor
    {
        [Key(0)]
        public readonly ValType Type;
        [Key(1)]
        public readonly string TypeName; // used when ValType.Other is selected

        public TypeDescriptor(ValType type)
        {
            Type = type;
            TypeName = null;
        }

        [SerializationConstructor]
        public TypeDescriptor(ValType type, string typeName)
        {
            Type = type;
            TypeName = typeName;
        }
    }

    [MessagePackObject]
    public sealed class GSet
    {
        [Key(0)]
        public List<string> StringElements { get; set; }
        [Key(1)]
        public TypeDescriptor TypeInfo { get; set; }
        [Key(2)]
        public List<int> IntElements { get; set; }
        [Key(3)]
        public List<long> LongElements { get; set; }
        [Key(4)]
        public List<OtherMessage> OtherElements { get; set; }
        [Key(5)]
        public List<string> ActorRefElements { get; set; }
    }

    [MessagePackObject]
    public sealed class ORSet
    {
        [Key(0)]
        public VersionVector Vvector { get; set; }
        [Key(1)]
        public TypeDescriptor TypeInfo { get; set; }
        [Key(2)]
        public List<VersionVector> Dots { get; set; }
        [Key(3)]
        public List<string> StringElements { get; set; }
        [Key(4)]
        public List<int> IntElements { get; set; }
        [Key(5)]
        public List<long> LongElements { get; set; }
        [Key(6)]
        public List<OtherMessage> OtherElements { get; set; }
        [Key(7)]
        public List<string> ActorRefElements { get; set; }
    }

    [MessagePackObject]
    public readonly struct ORSetDeltaGroup
    {
        [MessagePackObject]
        public readonly struct Entry
        {
            [Key(0)]
            public readonly ORSetDeltaOp Operation;
            [Key(1)]
            public readonly ORSet Underlying;

            [SerializationConstructor]
            public Entry(ORSetDeltaOp operation, ORSet underlying)
            {
                Operation = operation;
                Underlying = underlying;
            }
        }

        [Key(0)]
        public readonly List<Entry> Entries;
        [Key(1)]
        public readonly TypeDescriptor TypeInfo;

        [SerializationConstructor]
        public ORSetDeltaGroup(List<Entry> entries, TypeDescriptor typeInfo)
        {
            Entries = entries;
            TypeInfo = typeInfo;
        }
    }

    public enum ORSetDeltaOp
    {
        Add = 0,
        Remove = 1,
        Full = 2,
    }

    [MessagePackObject]
    public readonly struct Flag
    {
        [Key(0)]
        public readonly bool Enabled;

        [SerializationConstructor]
        public Flag(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [MessagePackObject]
    public readonly struct LWWRegister
    {
        [Key(0)]
        public readonly long Timestamp;
        [Key(1)]
        public readonly UniqueAddress Node;
        [Key(2)]
        public readonly OtherMessage State;
        [Key(3)]
        public readonly TypeDescriptor TypeInfo;

        [SerializationConstructor]
        public LWWRegister(long timestamp, UniqueAddress node, OtherMessage state, TypeDescriptor typeInfo)
        {
            Timestamp = timestamp;
            Node = node;
            State = state;
            TypeInfo = typeInfo;
        }
    }

    [MessagePackObject]
    public readonly struct GCounter
    {
        [MessagePackObject]
        public readonly struct Entry
        {
            [Key(0)]
            public readonly UniqueAddress Node;
            [Key(1)]
            public readonly byte[] Value;

            [SerializationConstructor]
            public Entry(UniqueAddress node, byte[] value)
            {
                Node = node;
                Value = value;
            }
        }

        [Key(0)]
        public readonly List<Entry> Entries;

        [SerializationConstructor]
        public GCounter(List<Entry> entries)
        {
            Entries = entries;
        }
    }

    [MessagePackObject]
    public readonly struct PNCounter
    {
        [Key(0)]
        public readonly GCounter Increments;
        [Key(1)]
        public readonly GCounter Decrements;

        [SerializationConstructor]
        public PNCounter(GCounter increments, GCounter decrements)
        {
            Increments = increments;
            Decrements = decrements;
        }
    }

    [MessagePackObject]
    public sealed class ORMap
    {
        [MessagePackObject]
        public sealed class Entry
        {
            [Key(0)]
            public string StringKey { get; set; }
            [Key(1)]
            public OtherMessage Value { get; set; }
            [Key(2)]
            public int IntKey { get; set; }
            [Key(3)]
            public long LongKey { get; set; }
            [Key(4)]
            public OtherMessage OtherKey { get; set; }
        }

        [Key(0)]
        public ORSet Keys { get; set; }
        [Key(1)]
        public List<Entry> Entries { get; set; }
        [Key(2)]
        public TypeDescriptor ValueTypeInfo { get; set; }
    }

    [MessagePackObject]
    public sealed class ORMapDeltaGroup
    {
        [MessagePackObject]
        public sealed class MapEntry
        {
            [Key(0)]
            public string StringKey { get; set; }
            [Key(1)]
            public OtherMessage Value { get; set; }
            [Key(2)]
            public int IntKey { get; set; }
            [Key(3)]
            public long LongKey { get; set; }
            [Key(4)]
            public OtherMessage OtherKey { get; set; }
        }

        [MessagePackObject]
        public sealed class Entry
        {
            [Key(0)]
            public ORMapDeltaOp Operation { get; set; }
            [Key(1)]
            public ORSet Underlying { get; set; }
            [Key(2)]
            public int ZeroTag { get; set; }
            [Key(3)]
            public List<MapEntry> EntryData
            {
                [MethodImpl(InlineOptions.AggressiveOptimization)]
                get => _entryData ?? EnsureEntryDataCreated();
                set => _entryData = value;
            }

            private List<MapEntry> _entryData;
            [MethodImpl(MethodImplOptions.NoInlining)]
            private List<MapEntry> EnsureEntryDataCreated()
            {
                lock (this)
                {
                    if (_entryData is null)
                    {
                        _entryData = new List<MapEntry>();
                    }
                }
                return _entryData;
            }
        }

        [Key(0)]
        public List<Entry> Entries { get; set; }
        [Key(1)]
        public TypeDescriptor KeyTypeInfo { get; set; }
        [Key(2)]
        public TypeDescriptor ValueTypeInfo { get; set; }
    }

    [MessagePackObject]
    public readonly struct ORMultiMapDelta
    {
        [Key(0)]
        public readonly ORMapDeltaGroup Delta;
        [Key(1)]
        public readonly bool WithValueDeltas;

        [SerializationConstructor]
        public ORMultiMapDelta(ORMapDeltaGroup delta, bool withValueDeltas)
        {
            Delta = delta;
            WithValueDeltas = withValueDeltas;
        }
    }

    public enum ORMapDeltaOp
    {
        ORMapPut = 0,
        ORMapRemove = 1,
        ORMapRemoveKey = 2,
        ORMapUpdate = 3,
    }

    [MessagePackObject]
    public sealed class LWWMap
    {
        [MessagePackObject]
        public sealed class Entry
        {
            [Key(0)]
            public string StringKey { get; set; }
            [Key(1)]
            public LWWRegister Value { get; set; }
            [Key(2)]
            public int IntKey { get; set; }
            [Key(3)]
            public long LongKey { get; set; }
            [Key(4)]
            public OtherMessage OtherKey { get; set; }
        }

        [Key(0)]
        public ORSet Keys { get; set; }
        [Key(1)]
        public List<Entry> Entries { get; set; }
        [Key(2)]
        public TypeDescriptor ValueTypeInfo { get; set; }
    }

    [MessagePackObject]
    public sealed class PNCounterMap
    {
        [MessagePackObject]
        public sealed class Entry
        {
            [Key(0)]
            public string StringKey { get; set; }
            [Key(1)]
            public PNCounter Value { get; set; }
            [Key(2)]
            public int IntKey { get; set; }
            [Key(3)]
            public long LongKey { get; set; }
            [Key(4)]
            public OtherMessage OtherKey { get; set; }
        }

        [Key(0)]
        public ORSet Keys { get; set; }
        [Key(1)]
        public List<Entry> Entries { get; set; }
    }

    [MessagePackObject]
    public sealed class ORMultiMap
    {
        [MessagePackObject]
        public sealed class Entry
        {
            [Key(0)]
            public string StringKey { get; set; }
            [Key(1)]
            public ORSet Value { get; set; }
            [Key(2)]
            public int IntKey { get; set; }
            [Key(3)]
            public long LongKey { get; set; }
            [Key(4)]
            public OtherMessage OtherKey { get; set; }
        }

        [Key(0)]
        public ORSet Keys { get; set; }
        [Key(1)]
        public List<Entry> Entries { get; set; }
        [Key(2)]
        public bool WithValueDeltas { get; set; }
        [Key(3)]
        public TypeDescriptor ValueTypeInfo { get; set; }
    }

    public enum KeyType
    {
        ORSetKey = 0,
        GSetKey = 1,
        GCounterKey = 2,
        PNCounterKey = 3,
        FlagKey = 4,
        LWWRegisterKey = 5,
        ORMapKey = 6,
        LWWMapKey = 7,
        PNCounterMapKey = 8,
        ORMultiMapKey = 9,
    }

    [MessagePackObject]
    public readonly struct Key
    {
        [Key(0)]
        public readonly string KeyId;
        [Key(1)]
        public readonly KeyType KeyType;
        [Key(2)]
        public readonly TypeDescriptor KeyTypeInfo;
        [Key(3)]
        public readonly TypeDescriptor ValueTypeInfo;

        public Key(string keyId, KeyType keyType)
        {
            KeyId = keyId;
            KeyType = keyType;
            KeyTypeInfo = default;
            ValueTypeInfo = default;
        }

        public Key(string keyId, KeyType keyType, TypeDescriptor keyTypeInfo)
        {
            KeyId = keyId;
            KeyType = keyType;
            KeyTypeInfo = keyTypeInfo;
            ValueTypeInfo = default;
        }

        [SerializationConstructor]
        public Key(string keyId, KeyType keyType, TypeDescriptor keyTypeInfo, TypeDescriptor valueTypeInfo)
        {
            KeyId = keyId;
            KeyType = keyType;
            KeyTypeInfo = keyTypeInfo;
            ValueTypeInfo = valueTypeInfo;
        }
    }
}
