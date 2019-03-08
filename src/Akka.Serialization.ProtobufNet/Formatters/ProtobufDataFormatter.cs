using System.Buffers;
using System.Data;
using System.IO;
using CuteAnt.Buffers;
using MessagePack;
using MessagePack.Formatters;
using ProtoBuf.Data;

namespace Akka.Serialization.Formatters
{
    public sealed class DataSetFormatter : IMessagePackFormatter<DataSet>
    {
        public static readonly IMessagePackFormatter<DataSet> Instance = new DataSetFormatter();

        private const int InitialBufferSize = ProtobufDataSerializer.InitialBufferSize;
        private static readonly ProtoDataWriterOptions DefaultWriterOptions = ProtobufDataSerializer.DefaultWriterOptions;
        private static readonly ArrayPool<byte> SharedBufferPool = ProtobufDataSerializer.SharedBufferPool;

        public DataSet Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var bts = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            using (var inputStream = new MemoryStream(bts))
            {
                return DataSerializer.DeserializeDataSet(inputStream);
            }
        }

        public int Serialize(ref byte[] bytes, int offset, DataSet value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            byte[] buffer = null; int bufferSize;
            try
            {
                using (var pooledStream = BufferManagerOutputStreamManager.Create())
                {
                    var outputStream = pooledStream.Object;
                    outputStream.Reinitialize(InitialBufferSize, SharedBufferPool);

                    DataSerializer.Serialize(outputStream, value, DefaultWriterOptions);
                    buffer = outputStream.ToArray(out bufferSize);
                }
                return MessagePackBinary.WriteBytes(ref bytes, offset, buffer, 0, bufferSize);
            }
            finally { if (buffer != null) { SharedBufferPool.Return(buffer); } }
        }
    }

    public sealed class DataTableFormatter : IMessagePackFormatter<DataTable>
    {
        public static readonly IMessagePackFormatter<DataTable> Instance = new DataTableFormatter();

        private const int InitialBufferSize = ProtobufDataSerializer.InitialBufferSize;
        private static readonly ProtoDataWriterOptions DefaultWriterOptions = ProtobufDataSerializer.DefaultWriterOptions;
        private static readonly ArrayPool<byte> SharedBufferPool = ProtobufDataSerializer.SharedBufferPool;

        public DataTable Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var bts = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            using (var inputStream = new MemoryStream(bts))
            {
                return DataSerializer.DeserializeDataTable(inputStream);
            }
        }

        public int Serialize(ref byte[] bytes, int offset, DataTable value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            byte[] buffer = null; int bufferSize;
            try
            {
                using (var pooledStream = BufferManagerOutputStreamManager.Create())
                {
                    var outputStream = pooledStream.Object;
                    outputStream.Reinitialize(InitialBufferSize, SharedBufferPool);

                    DataSerializer.Serialize(outputStream, value, DefaultWriterOptions);
                    buffer = outputStream.ToArray(out bufferSize);
                }
                return MessagePackBinary.WriteBytes(ref bytes, offset, buffer, 0, bufferSize);
            }
            finally { if (buffer != null) { SharedBufferPool.Return(buffer); } }
        }
    }
}
