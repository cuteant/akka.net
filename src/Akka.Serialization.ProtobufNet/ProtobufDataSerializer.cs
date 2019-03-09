using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Util;
using CuteAnt.Buffers;
using ProtoBuf.Data;

namespace Akka.Serialization
{
    public sealed class ProtobufDataSerializer : SerializerWithIntegerManifest
    {
        #region manifests

        private const int DataSetManifest = 50;
        private const int DataTableManifest = 51;

        private static readonly Dictionary<Type, int> ManifestMap;

        static ProtobufDataSerializer()
        {
            ManifestMap = new Dictionary<Type, int>
            {
                { typeof(DataSet), DataSetManifest },
                { typeof(DataTable), DataTableManifest },
            };

            DefaultWriterOptions = new ProtoDataWriterOptions();
            SharedBufferPool = BufferManager.Shared;
        }

        #endregion

        internal const int InitialBufferSize = 80 * 1024;
        internal static readonly ProtoDataWriterOptions DefaultWriterOptions;
        internal static readonly ArrayPool<byte> SharedBufferPool;

        /// <summary>Initializes a new instance of the <see cref="ProtobufDataSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public ProtobufDataSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override int Identifier => 108;

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object o, out int manifest)
        {
            switch (o)
            {
                case DataSet ds:
                    manifest = DataSetManifest;
                    using (var pooledStream = BufferManagerOutputStreamManager.Create())
                    {
                        var outputStream = pooledStream.Object;
                        outputStream.Reinitialize(InitialBufferSize, SharedBufferPool);

                        DataSerializer.Serialize(outputStream, ds, DefaultWriterOptions);
                        return outputStream.ToByteArray();
                    }
                case DataTable dt:
                    manifest = DataTableManifest;
                    using (var pooledStream = BufferManagerOutputStreamManager.Create())
                    {
                        var outputStream = pooledStream.Object;
                        outputStream.Reinitialize(InitialBufferSize, SharedBufferPool);

                        DataSerializer.Serialize(outputStream, dt, DefaultWriterOptions);
                        return outputStream.ToByteArray();
                    }
                default:
                    manifest = 0; ThrowArgumentException_Serializer_D(o); return null;
            }
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, int manifest)
        {
            switch (manifest)
            {
                case DataSetManifest:
                    using (var inputStream = new MemoryStream(bytes))
                    {
                        return DataSerializer.DeserializeDataSet(inputStream);
                    }
                case DataTableManifest:
                    using (var inputStream = new MemoryStream(bytes))
                    {
                        return DataSerializer.DeserializeDataTable(inputStream);
                    }
                default:
                    return ThrowArgumentException_Serializer(manifest);
            }
        }

        /// <inheritdoc />
        protected sealed override int GetManifest(Type type)
        {
            if (null == type) { return 0; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            ThrowArgumentException_Serializer_D(type); return 0;
        }

        /// <inheritdoc />
        public sealed override int Manifest(object o)
        {
            switch (o)
            {
                case DataSet _:
                    return DataSetManifest;
                case DataTable _:
                    return DataTableManifest;
                default:
                    ThrowArgumentException_Serializer_D(o); return 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object ThrowArgumentException_Serializer(int manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [${nameof(ProtobufDataSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentException_Serializer_D(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var type = obj as Type;
                var typeQualifiedName = type != null ? type.TypeQualifiedName() : obj?.GetType().TypeQualifiedName();
                return new ArgumentException($"Cannot deserialize object of type [{typeQualifiedName}]");
            }
        }
    }
}
