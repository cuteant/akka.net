using Akka.Serialization;
using Google.Protobuf;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Formatters
{
    public sealed class ByteStringFormatter : IMessagePackFormatter<ByteString>
    {
        public static readonly IMessagePackFormatter<ByteString> Instance = new ByteStringFormatter();

        public ByteString Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var bts = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            return ProtobufUtil.FromBytes(bts);
        }

        public int Serialize(ref byte[] bytes, int offset, ByteString value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var bts = ProtobufUtil.GetBuffer(value);
            return MessagePackBinary.WriteBytes(ref bytes, offset, bts);
        }
    }
}
