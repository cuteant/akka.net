using System.Collections.Generic;
using Akka.Serialization.Protocol;
using MessagePack;

namespace Akka.Cluster.Tools.PublishSubscribe.Serialization.Protocol
{
    [MessagePackObject]
    public readonly struct Version
    {
        [Key(0)]
        public readonly AddressData Address;
        [Key(1)]
        public readonly long Timestamp;

        [SerializationConstructor]
        public Version(in AddressData address, long timestamp)
        {
            Address = address;
            Timestamp = timestamp;
        }
    }

    [MessagePackObject]
    public readonly struct Status
    {
        [Key(0)]
        public readonly Version[] Versions;
        [Key(1)]
        public readonly bool ReplyToStatus;

        [SerializationConstructor]
        public Status(Version[] versions, bool replyToStatus)
        {
            Versions = versions;
            ReplyToStatus = replyToStatus;
        }
    }

    [MessagePackObject]
    public readonly struct ValueHolder
    {
        [Key(0)]
        public readonly long Version;
        [Key(1)]
        public readonly string Ref;

        [SerializationConstructor]
        public ValueHolder(long version, string r)
        {
            Version = version;
            Ref = r;
        }
    }

    [MessagePackObject]
    public readonly struct Bucket
    {
        [Key(0)]
        public readonly AddressData Owner;
        [Key(1)]
        public readonly long Version;
        [Key(2)]
        public readonly Dictionary<string, ValueHolder> Content;

        [SerializationConstructor]
        public Bucket(in AddressData owner, long version, Dictionary<string, ValueHolder> content)
        {
            Owner = owner;
            Version = version;
            Content = content;
        }
    }

    [MessagePackObject]
    public readonly struct Delta
    {
        [Key(0)]
        public readonly List<Bucket> Buckets;

        [SerializationConstructor]
        public Delta(List<Bucket> buckets) => Buckets = buckets;
    }

    // Send normally local, but it is also used by the ClusterClient.
    [MessagePackObject]
    public readonly struct Send
    {
        [Key(0)]
        public readonly string Path;
        [Key(1)]
        public readonly bool LocalAffinity;
        [Key(2)]
        public readonly Payload Payload;

        [SerializationConstructor]
        public Send(string path, bool localAffinity, in Payload payload)
        {
            Path = path;
            LocalAffinity = localAffinity;
            Payload = payload;
        }
    }

    // SendToAll normally local, but it is also used by the ClusterClient.
    [MessagePackObject]
    public readonly struct SendToAll
    {
        [Key(0)]
        public readonly string Path;
        [Key(1)]
        public readonly bool AllButSelf;
        [Key(2)]
        public readonly Payload Payload;

        [SerializationConstructor]
        public SendToAll(string path, bool allButSelf, in Payload payload)
        {
            Path = path;
            AllButSelf = allButSelf;
            Payload = payload;
        }
    }

    // Publish normally local, but it is also used by the ClusterClient.
    [MessagePackObject]
    public readonly struct Publish
    {
        [Key(0)]
        public readonly string Topic;
        [Key(1)]
        public readonly Payload Payload;

        [SerializationConstructor]
        public Publish(string topic, in Payload payload)
        {
            Topic = topic;
            Payload = payload;
        }
    }

    // Send a public readonly struct to only one subscriber of a group.
    [MessagePackObject]
    public readonly struct SendToOneSubscriber
    {
        [Key(0)]
        public readonly Payload Payload;

        [SerializationConstructor]
        public SendToOneSubscriber(in Payload payload) => Payload = payload;
    }
}
