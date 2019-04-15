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

        public DataSet Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var bts = reader.ReadBytes();
            using (var inputStream = new MemoryStream(bts))
            {
                return DataSerializer.DeserializeDataSet(inputStream);
            }
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, DataSet value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

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
                writer.WriteBytes(buffer, 0, bufferSize, ref idx);
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

        public DataTable Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var bts = reader.ReadBytes();
            using (var inputStream = new MemoryStream(bts))
            {
                return DataSerializer.DeserializeDataTable(inputStream);
            }
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, DataTable value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

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
                writer.WriteBytes(buffer, 0, bufferSize, ref idx);
            }
            finally { if (buffer != null) { SharedBufferPool.Return(buffer); } }
        }
    }
}
