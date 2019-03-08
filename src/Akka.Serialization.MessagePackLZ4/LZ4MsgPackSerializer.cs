using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt;
using MessagePack;

namespace Akka.Serialization
{
    public sealed class LZ4MsgPackSerializer : SerializerWithTypeManifest
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly IFormatterResolver _resolver;
        private readonly int _initialBufferSize;

        public LZ4MsgPackSerializer(ExtendedActorSystem system)
            : this(system, MsgPackSerializerSettings.Default, HyperionSerializerSettings.Default) { }

        public LZ4MsgPackSerializer(ExtendedActorSystem system, Config config)
            : this(system, MsgPackSerializerSettings.Create(config), HyperionSerializerSettings.Create(config)) { }

        public LZ4MsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings, HyperionSerializerSettings hyperionSettings)
            : base(system)
        {
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;

            var serializer = HyperionSerializerHelper.CreateSerializer(system, hyperionSettings);
            _resolver = new AkkaDefaultResolver(system, serializer);
        }

        /// <inheritdoc />
        public sealed override int Identifier => 102;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source == null) { return null; }

            var type = source.GetType();
            var serializedObject = MessagePackSerializer.SerializeUnsafe(source, _resolver);
            return MessagePackSerializer.NonGeneric.Deserialize(type, serializedObject, _resolver);
        }

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object obj)
        {
            //if (null == obj) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理
            return LZ4MessagePackSerializer.Serialize(obj, _resolver);
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, Type type) => LZ4MessagePackSerializer.NonGeneric.Deserialize(type, new ArraySegment<byte>(bytes, 0, bytes.Length), _resolver);
    }
}
