using System.Runtime.Serialization;
using CuteAnt;
using MessagePack;

namespace Akka.Serialization.Protocol
{
    /// <summary>Defines a payload.</summary>
    [MessagePackObject]
    public readonly struct Payload
    {
        public static readonly Payload Null = new Payload(EmptyArray<byte>.Instance, 0);

        [Key(0)]
        public readonly byte[] Message;

        [Key(1)]
        public readonly int SerializerId;

        [Key(2)]
        public readonly byte[] MessageManifest;

        [Key(3)]
        public readonly int TypeHashCode;

        public Payload(byte[] message, int serializerId)
        {
            Message = message;
            SerializerId = serializerId;
            MessageManifest = null;
            TypeHashCode = 0;
        }

        public Payload(byte[] message, int serializerId, byte[] messageManifest)
        {
            Message = message;
            SerializerId = serializerId;
            MessageManifest = messageManifest;
            TypeHashCode = 0;
        }

        [SerializationConstructor]
        public Payload(byte[] message, int serializerId, byte[] messageManifest, int typeHashCode)
        {
            Message = message;
            SerializerId = serializerId;
            MessageManifest = messageManifest;
            TypeHashCode = typeHashCode;
        }

        [IgnoreMember, IgnoreDataMember]
        public bool IsEmtpy => null == Message || Message.Length == 0;

        [IgnoreMember, IgnoreDataMember]
        public bool NoeEmtpy => Message != null && Message.Length > 0;
    }
}
