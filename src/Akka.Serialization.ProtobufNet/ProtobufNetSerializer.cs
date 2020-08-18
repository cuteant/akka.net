using System;
using System.Buffers;
using System.IO;
using Akka.Actor;
using CuteAnt.Buffers;
using ProtoBuf.Meta;

namespace Akka.Serialization
{
    public sealed class ProtobufNetSerializer : Serializer
    {
#if DESKTOPCLR
        private const int c_initialBufferSize = 1024 * 80;
#else
        private const int c_initialBufferSize = 1024 * 64;
#endif
        private static readonly ArrayPool<byte> s_bufferPool;
        private static readonly RuntimeTypeModel s_model;

        static ProtobufNetSerializer()
        {
            s_bufferPool = BufferManager.Shared;
            s_model = RuntimeTypeModel.Default;
            // 考虑到类的继承问题，禁用 [DataContract] / [XmlType]
            s_model.AutoAddProtoContractTypesOnly = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ProtobufNetSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public ProtobufNetSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override int Identifier => 107;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source) => s_model.DeepClone(source);

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object obj)
        {
            //if (obj is null) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理

            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(c_initialBufferSize, s_bufferPool);

                s_model.Serialize(outputStream, obj, null);
                return outputStream.ToByteArray();
            }
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, Type type)
        {
            //if (0u >= (uint)bytes.Length) { return null; }
            using (var readStream = new MemoryStream(bytes))
            {
                // protobuf-net，读取空数据流并不会返回空对象，而是根据类型实例化一个
                return s_model.Deserialize(readStream, null, type, null);
            }
        }
    }
}
