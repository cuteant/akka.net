using System;
using Akka.Annotations;
using CuteAnt;
using MessagePack;

namespace Akka.Serialization
{
    using Akka.Serialization.Protocol;

    public static class PayloadExtensions
    {
        public static bool IsEmtpy(in this Payload payload)
        {
            var message = payload.Message;
            return message is null || 0u >= (uint)message.Length;
        }

        public static bool NonEmtpy(in this Payload payload)
        {
            var message = payload.Message;
            return message is object && 0u < (uint)message.Length;
        }

        public static bool IsEmtpy(in this ExternalPayload payload)
        {
            var message = payload.Message;
            return message is null || 0u >= (uint)message.Length;
        }

        public static bool NonEmtpy(in this ExternalPayload payload)
        {
            var message = payload.Message;
            return message is object && 0u < (uint)message.Length;
        }
    }
}

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
        public readonly string Manifest;

        [Key(3)]
        public readonly Type MessageType;

        public Payload(byte[] message, int serializerId)
        {
            Message = message;
            SerializerId = serializerId;
            Manifest = null;
            MessageType = null;
        }

        [SerializationConstructor]
        public Payload(byte[] message, int serializerId, string manifest, Type messageType)
        {
            Message = message;
            SerializerId = serializerId;
            Manifest = manifest;
            MessageType = messageType;
        }
    }

    /// <summary>Defines a payload.</summary>
    [InternalApi]
    public readonly struct ExternalPayload
    {
        public static readonly ExternalPayload Null = new ExternalPayload(EmptyArray<byte>.Instance, 0, null, null);

        public readonly byte[] Message;

        public readonly int Identifier;

        public readonly string TypeName;

        public readonly string Manifest;

        public readonly bool IsJson;

        public readonly Type MessageType;

        public ExternalPayload(byte[] message, int identifier, string typeName, string manifest)
        {
            Message = message;
            Identifier = identifier;
            TypeName = typeName;
            Manifest = manifest;
            IsJson = false;
            MessageType = null;
        }

        public ExternalPayload(byte[] message, int identifier, bool isJson, Type type)
        {
            Message = message;
            Identifier = identifier;
            TypeName = null;
            Manifest = null;
            IsJson = isJson;
            MessageType = type;
        }

        public ExternalPayload(byte[] message, int identifier, string manifest, bool isJson, Type type)
        {
            Message = message;
            Identifier = identifier;
            TypeName = null;
            Manifest = manifest;
            IsJson = isJson;
            MessageType = type;
        }
    }
}
