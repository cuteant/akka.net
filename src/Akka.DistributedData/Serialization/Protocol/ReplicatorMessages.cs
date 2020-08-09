using System.Collections.Generic;
using MessagePack;
using OtherMessage = Akka.Serialization.Protocol.Payload;

namespace Akka.DistributedData.Serialization.Protocol
{
    [MessagePackObject]
    public readonly struct Get
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly int Consistency;
        [Key(2)]
        public readonly uint Timeout;
        [Key(3)]
        public readonly OtherMessage Request;

        [SerializationConstructor]
        public Get(OtherMessage key, int consistency, uint timeout, OtherMessage request)
        {
            Key = key;
            Consistency = consistency;
            Timeout = timeout;
            Request = request;
        }
    }

    [MessagePackObject]
    public readonly struct GetSuccess
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly OtherMessage Data;
        [Key(2)]
        public readonly OtherMessage Request;

        [SerializationConstructor]
        public GetSuccess(OtherMessage key, OtherMessage data, OtherMessage request)
        {
            Key = key;
            Data = data;
            Request = request;
        }
    }

    [MessagePackObject]
    public readonly struct NotFound
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly OtherMessage Request;

        [SerializationConstructor]
        public NotFound(OtherMessage key, OtherMessage request)
        {
            Key = key;
            Request = request;
        }
    }

    [MessagePackObject]
    public readonly struct GetFailure
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly OtherMessage Request;

        [SerializationConstructor]
        public GetFailure(OtherMessage key, OtherMessage request)
        {
            Key = key;
            Request = request;
        }
    }

    [MessagePackObject]
    public readonly struct Subscribe
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly string Ref;

        [SerializationConstructor]
        public Subscribe(OtherMessage key, string r)
        {
            Key = key;
            Ref = r;
        }
    }

    [MessagePackObject]
    public readonly struct Unsubscribe
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly string Ref;

        [SerializationConstructor]
        public Unsubscribe(OtherMessage key, string r)
        {
            Key = key;
            Ref = r;
        }
    }

    [MessagePackObject]
    public readonly struct Changed
    {
        [Key(0)]
        public readonly OtherMessage Key;
        [Key(1)]
        public readonly OtherMessage Data;

        [SerializationConstructor]
        public Changed(OtherMessage key, OtherMessage data)
        {
            Key = key;
            Data = data;
        }
    }

    [MessagePackObject]
    public readonly struct Write
    {
        [Key(0)]
        public readonly string Key;
        [Key(1)]
        public readonly DataEnvelope Envelope;
        [Key(2)]
        public readonly UniqueAddress FromNode;

        public Write(string key, DataEnvelope envelope)
        {
            Key = key;
            Envelope = envelope;
            FromNode = null;
        }

        [SerializationConstructor]
        public Write(string key, DataEnvelope envelope, UniqueAddress fromNode)
        {
            Key = key;
            Envelope = envelope;
            FromNode = fromNode;
        }
    }

    // message WriteAck, via Empty
    public sealed class Empty : ISingletonMessage
    {
        public static readonly Empty Instance = new Empty();

        private Empty() { }
    }

    [MessagePackObject]
    public readonly struct Read
    {
        [Key(0)]
        public readonly string Key;
        [Key(1)]
        public readonly UniqueAddress FromNode;

        public Read(string key)
        {
            Key = key;
            FromNode = null;
        }

        [SerializationConstructor]
        public Read(string key, UniqueAddress fromNode)
        {
            Key = key;
            FromNode = fromNode;
        }
    }

    [MessagePackObject]
    public readonly struct ReadResult
    {
        [Key(0)]
        public readonly DataEnvelope Envelope;

        [SerializationConstructor]
        public ReadResult(DataEnvelope envelope)
        {
            Envelope = envelope;
        }
    }

    [MessagePackObject]
    public sealed class DataEnvelope
    {
        [MessagePackObject]
        public sealed class PruningEntry
        {
            [Key(0)]
            public UniqueAddress RemovedAddress { get; set; }
            [Key(1)]
            public UniqueAddress OwnerAddress { get; set; }
            [Key(2)]
            public bool Performed { get; set; }
            [Key(3)]
            public List<Address> Seen { get; set; }
            [Key(4)]
            public long ObsoleteTime { get; set; }
        }

        [Key(0)]
        public OtherMessage Data { get; set; }
        [Key(1)]
        public List<PruningEntry> Pruning { get; set; }
        [Key(2)]
        public VersionVector DeltaVersions { get; set; }
    }

    [MessagePackObject]
    public readonly struct Status
    {
        [MessagePackObject]
        public readonly struct Entry
        {
            [Key(0)]
            public readonly string Key;
            [Key(1)]
            public readonly byte[] Digest;

            [SerializationConstructor]
            public Entry(string key, byte[] digest)
            {
                Key = key;
                Digest = digest;
            }
        }

        [Key(0)]
        public readonly uint Chunk;
        [Key(1)]
        public readonly uint TotChunks;
        [Key(2)]
        public readonly List<Entry> Entries;
        //[Key(3)]
        //public readonly long ToSystemUid;
        //[Key(4)]
        //public readonly long FromSystemUid;

        [SerializationConstructor]
        public Status(uint chunk, uint totChunks, List<Entry> entries/*, long toSystemUid, long fromSystemUid*/)
        {
            Chunk = chunk;
            TotChunks = totChunks;
            Entries = entries;
            //ToSystemUid = toSystemUid;
            //FromSystemUid = fromSystemUid;
        }
    }

    [MessagePackObject]
    public readonly struct Gossip
    {
        [MessagePackObject]
        public readonly struct Entry
        {
            [Key(0)]
            public readonly string Key;
            [Key(1)]
            public readonly DataEnvelope Envelope;

            [SerializationConstructor]
            public Entry(string key, DataEnvelope envelope)
            {
                Key = key;
                Envelope = envelope;
            }
        }

        [Key(0)]
        public readonly bool SendBack;
        [Key(1)]
        public readonly List<Entry> Entries;
        //[Key(2)]
        //public readonly long ToSystemUid;
        //[Key(3)]
        //public readonly long FromSystemUid;

        [SerializationConstructor]
        public Gossip(bool sendBack, List<Entry> entries/*, long toSystemUid, long fromSystemUid*/)
        {
            SendBack = sendBack;
            Entries = entries;
            //ToSystemUid = toSystemUid;
            //FromSystemUid = fromSystemUid;
        }
    }

    [MessagePackObject]
    public readonly struct DeltaPropagation
    {
        [MessagePackObject]
        public readonly struct Entry
        {
            [Key(0)]
            public readonly string Key;
            [Key(1)]
            public readonly DataEnvelope Envelope;
            [Key(2)]
            public readonly long FromSeqNr;
            [Key(3)]
            public readonly long ToSeqNr;

            [SerializationConstructor]
            public Entry(string key, DataEnvelope envelope, long fromSeqNr, long toSeqNr)
            {
                Key = key;
                Envelope = envelope;
                FromSeqNr = fromSeqNr;
                ToSeqNr = toSeqNr;
            }
        }

        [Key(0)]
        public readonly UniqueAddress FromNode;
        [Key(1)]
        public readonly List<Entry> Entries;
        [Key(2)]
        public readonly bool Reply;

        [SerializationConstructor]
        public DeltaPropagation(UniqueAddress fromNode, List<Entry> entries, bool reply)
        {
            FromNode = fromNode;
            Entries = entries;
            Reply = reply;
        }
    }

    [MessagePackObject]
    public sealed class UniqueAddress
    {
        [Key(0)]
        public readonly Address Address;
        [Key(1)]
        public readonly long Uid;

        [SerializationConstructor]
        public UniqueAddress(Address address, long uid)
        {
            Address = address;
            Uid = uid;
        }
    }

    [MessagePackObject]
    public readonly struct Address
    {
        [Key(0)]
        public readonly string HostName;
        [Key(1)]
        public readonly int Port;

        [SerializationConstructor]
        public Address(string hostName, int port)
        {
            HostName = hostName;
            Port = port;
        }
    }

    [MessagePackObject]
    public readonly struct VersionVector
    {
        [MessagePackObject]
        public readonly struct Entry
        {
            [Key(0)]
            public readonly UniqueAddress Node;
            [Key(1)]
            public readonly long Version;

            [SerializationConstructor]
            public Entry(UniqueAddress node, long version)
            {
                Node = node;
                Version = version;
            }
        }

        [Key(0)]
        public readonly List<Entry> Entries;

        [SerializationConstructor]
        public VersionVector(List<Entry> entries)
        {
            Entries = entries;
        }
    }

    //[MessagePackObject]
    //public readonly struct OtherMessage
    //{
    //    [Key(0)]
    //    public readonly byte[] EnclosedMessage;
    //    [Key(1)]
    //    public readonly int SerializerId;
    //    [Key(2)]
    //    public readonly byte[] MessageManifest;

    //    [SerializationConstructor]
    //    public OtherMessage(byte[] enclosedMessage, int serializerId, byte[] messageManifest)
    //    {
    //        EnclosedMessage = enclosedMessage;
    //        SerializerId = serializerId;
    //        MessageManifest = messageManifest;
    //    }
    //}

    [MessagePackObject]
    public readonly struct StringGSet
    {
        [Key(0)]
        public readonly List<string> Elements;

        [SerializationConstructor]
        public StringGSet(List<string> elements)
        {
            Elements = elements;
        }
    }

    [MessagePackObject]
    public readonly struct DurableDataEnvelope
    {
        [Key(0)]
        public readonly OtherMessage Data;
        [Key(1)]
        public readonly List<DataEnvelope.PruningEntry> Pruning;

        [SerializationConstructor]
        public DurableDataEnvelope(OtherMessage data, List<DataEnvelope.PruningEntry> pruning)
        {
            Data = data;
            Pruning = pruning;
        }
    }
}
