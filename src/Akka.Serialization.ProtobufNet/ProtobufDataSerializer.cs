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
    public sealed class ProtobufDataSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string DataSetManifest = "DS";
        private const string DataTableManifest = "DT";

        private static readonly Dictionary<Type, string> ManifestMap;

        static ProtobufDataSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(DataSet), DataSetManifest },
                { typeof(DataTable), DataTableManifest },
            };

            DefaultWriterOptions = new ProtoDataWriterOptions();
            SharedBufferPool = BufferManager.Shared;
        }

        #endregion

#if DESKTOPCLR
        internal const int InitialBufferSize = 1024 * 80;
#else
        internal const int InitialBufferSize = 1024 * 64;
#endif
        internal static readonly ProtoDataWriterOptions DefaultWriterOptions;
        internal static readonly ArrayPool<byte> SharedBufferPool;

        /// <summary>Initializes a new instance of the <see cref="ProtobufDataSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public ProtobufDataSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override int Identifier => 108;

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object o, out string manifest)
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
                    throw GetArgumentException_Serializer_D(o);
            }
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, string manifest)
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
                    throw GetArgumentException_Serializer(manifest);
            }
        }

        /// <inheritdoc />
        protected sealed override string GetManifest(Type type)
        {
            if (type is null) { return null; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            throw GetArgumentException_Serializer_D(type);
        }

        /// <inheritdoc />
        public sealed override string Manifest(object o)
        {
            switch (o)
            {
                case DataSet _:
                    return DataSetManifest;
                case DataTable _:
                    return DataTableManifest;
                default:
                    throw GetArgumentException_Serializer_D(o);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetArgumentException_Serializer(string manifest)
        {
            return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [${nameof(ProtobufDataSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetArgumentException_Serializer_D(object obj)
        {
            var type = obj as Type;
            var typeQualifiedName = type is object ? type.TypeQualifiedName() : obj?.GetType().TypeQualifiedName();
            return new ArgumentException($"Cannot deserialize object of type [{typeQualifiedName}]");
        }
    }
}
