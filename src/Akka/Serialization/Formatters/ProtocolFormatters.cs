namespace Akka.Serialization.Formatters
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using MessagePack;
    using MessagePack.Formatters;

    #region == ProtocolPayloadFormatter ==

    internal sealed class ProtocolPayloadFormatter : IMessagePackFormatter<Protocol.Payload>
    {
        public static readonly ProtocolPayloadFormatter Instance = new ProtocolPayloadFormatter();

        ProtocolPayloadFormatter() { }

        public Protocol.Payload Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var count = reader.ReadArrayHeaderRaw();
            if (count != 4u) { ThrowInvalidOperationException(); }

            var msgData = reader.ReadBytes();
            var serializerId = reader.ReadInt32();
            var manifest = reader.ReadStringWithCache();
            var msgType = reader.ReadNamedType(true);
            return new Protocol.Payload(msgData, serializerId, manifest, msgType);
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, Protocol.Payload value, IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(4, ref idx);
            writer.WriteBytes(value.Message, ref idx);
            writer.WriteInt32(value.SerializerId, ref idx);
            writer.WriteStringWithCache(value.Manifest, ref idx);
            writer.WriteNamedType(value.MessageType, ref idx);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationException()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Invalid Payload formatter.");
            }
        }
    }

    #endregion

    #region == ProtocolReadOnlyActorRefDataFormatter ==

    internal sealed class ProtocolReadOnlyActorRefDataFormatter : IMessagePackFormatter<Protocol.ReadOnlyActorRefData>
    {
        public static readonly ProtocolReadOnlyActorRefDataFormatter Instance = new ProtocolReadOnlyActorRefDataFormatter();

        static ProtocolReadOnlyActorRefDataFormatter() { }

        public Protocol.ReadOnlyActorRefData Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var count = reader.ReadArrayHeaderRaw();
            if (count != 1u) { ThrowInvalidOperationException(); }

            return new Protocol.ReadOnlyActorRefData(reader.ReadStringWithCache());
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, Protocol.ReadOnlyActorRefData value, IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(1, ref idx);
            writer.WriteStringWithCache(value.Path, ref idx);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationException()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Invalid ReadOnlyActorRefData formatter.");
            }
        }
    }

    #endregion

    #region == ProtocolActorRefDataFormatter ==

    internal sealed class ProtocolActorRefDataFormatter : IMessagePackFormatter<Protocol.ActorRefData>
    {
        public static readonly ProtocolActorRefDataFormatter Instance = new ProtocolActorRefDataFormatter();

        static ProtocolActorRefDataFormatter() { }

        public Protocol.ActorRefData Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return null; }

            var count = reader.ReadArrayHeaderRaw();
            if (count != 1u) { ThrowInvalidOperationException(); }

            return new Protocol.ActorRefData(reader.ReadStringWithCache());
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, Protocol.ActorRefData value, IFormatterResolver formatterResolver)
        {
            if (value is null) { writer.WriteNil(ref idx); return; }

            writer.WriteFixedArrayHeaderUnsafe(1, ref idx);
            writer.WriteStringWithCache(value.Path, ref idx);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationException()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Invalid ActorRefData formatter.");
            }
        }
    }

    #endregion

    #region == ProtocolAddressDataFormatter ==

    internal sealed class ProtocolAddressDataFormatter : IMessagePackFormatter<Protocol.AddressData>
    {
        public static readonly ProtocolAddressDataFormatter Instance = new ProtocolAddressDataFormatter();

        static ProtocolAddressDataFormatter() { }

        public Protocol.AddressData Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var count = reader.ReadArrayHeaderRaw();
            if (count != 4u) { ThrowInvalidOperationException(); }

            var system = reader.ReadStringWithCache();
            var hostname = reader.ReadStringWithCache();
            var port = reader.ReadUInt32();
            var protocol = reader.ReadStringWithCache();
            return new Protocol.AddressData(system, hostname, port, protocol);
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, Protocol.AddressData value, IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(4, ref idx);
            writer.WriteStringWithCache(value.System, ref idx);
            writer.WriteStringWithCache(value.Hostname, ref idx);
            writer.WriteUInt32(value.Port, ref idx);
            writer.WriteStringWithCache(value.Protocol, ref idx);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationException()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Invalid AddressData formatter.");
            }
        }
    }

    #endregion

    #region == ProtocolActorIdentityFormatter ==

    internal sealed class ProtocolActorIdentityFormatter : IMessagePackFormatter<Protocol.ActorIdentity>
    {
        public static readonly ProtocolActorIdentityFormatter Instance = new ProtocolActorIdentityFormatter();

        static ProtocolActorIdentityFormatter() { }

        public Protocol.ActorIdentity Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var count = reader.ReadArrayHeaderRaw();
            if (count != 2u) { ThrowInvalidOperationException(); }

            var correlationId = ProtocolPayloadFormatter.Instance.Deserialize(ref reader, null);
            var path = reader.ReadStringWithCache();
            return new Protocol.ActorIdentity(correlationId, path);
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, Protocol.ActorIdentity value, IFormatterResolver formatterResolver)
        {
            writer.WriteFixedArrayHeaderUnsafe(2, ref idx);

            ProtocolPayloadFormatter.Instance.Serialize(ref writer, ref idx, value.CorrelationId, null);
            writer.WriteStringWithCache(value.Path, ref idx);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationException()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Invalid ActorIdentity formatter.");
            }
        }
    }

    #endregion
}
