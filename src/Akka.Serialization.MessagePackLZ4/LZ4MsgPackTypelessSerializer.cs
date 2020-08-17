using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using MessagePack;

namespace Akka.Serialization
{
    public sealed class LZ4MsgPackTypelessSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly IFormatterResolver _resolver;
        private readonly int _initialBufferSize;

        public LZ4MsgPackTypelessSerializer(ExtendedActorSystem system)
            : this(system, MsgPackSerializerSettings.Default, HyperionSerializerSettings.Default) { }

        public LZ4MsgPackTypelessSerializer(ExtendedActorSystem system, Config config)
            : this(system, MsgPackSerializerSettings.Create(config), HyperionSerializerSettings.Create(config)) { }

        public LZ4MsgPackTypelessSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings, HyperionSerializerSettings hyperionSettings)
            : base(system)
        {
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;

            var serializer = HyperionSerializerHelper.CreateSerializer(system, hyperionSettings);
            _resolver = new AkkaTypelessResolver(system, serializer);
        }

        /// <inheritdoc />
        public sealed override int Identifier => 103;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source is null) { return null; }

            using (var serializedObject = MessagePackSerializer.SerializeUnsafe(source, _resolver))
            {
                return MessagePackSerializer.Deserialize<object>(serializedObject.Span, _resolver);
            }
        }

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object obj)
        {
            //if (obj is null) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理
            return LZ4MessagePackSerializer.Serialize(obj, _resolver);
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, Type type) => LZ4MessagePackSerializer.Deserialize<object>(bytes, _resolver);
    }
}
