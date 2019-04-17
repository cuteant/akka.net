using Google.Protobuf;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
{
    public sealed class ByteStringFormatter : IMessagePackFormatter<ByteString>
    {
        public static readonly IMessagePackFormatter<ByteString> Instance = new ByteStringFormatter();

        public ByteString Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var bts = reader.ReadBytes();
            if (null == bts) { return null; }

            return ProtobufUtil.FromBytes(bts);
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, ByteString value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var bts = ProtobufUtil.GetBuffer(value);
            writer.WriteBytes(bts, ref idx);
        }
    }
}
