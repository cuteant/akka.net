using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using MessagePack;

namespace Akka.Serialization
{
    public sealed class MsgPackSerializer : SerializerWithTypeManifest
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly IFormatterResolver _resolver;
        private readonly int _initialBufferSize;

        public MsgPackSerializer(ExtendedActorSystem system)
            : this(system, MsgPackSerializerSettings.Default, HyperionSerializerSettings.Default) { }

        public MsgPackSerializer(ExtendedActorSystem system, Config config)
            : this(system, MsgPackSerializerSettings.Create(config), HyperionSerializerSettings.Create(config)) { }

        public MsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings, HyperionSerializerSettings hyperionSettings)
            : base(system)
        {
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;

            var serializer = HyperionSerializerHelper.CreateSerializer(system, hyperionSettings);
            _resolver = new AkkaDefaultResolver(system, serializer);
        }

        /// <inheritdoc />
        public override int Identifier => 101;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source == null) { return null; }

            var type = source.GetType();
            using (var serializedObject = MessagePackSerializer.SerializeUnsafe(source, _resolver))
            {
                return MessagePackSerializer.NonGeneric.Deserialize(type, serializedObject.Span, _resolver);
            }
        }

        public sealed override byte[] ToBinary(object obj)
        {
            //if (null == obj) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理
            return MessagePackSerializer.Serialize(obj, _resolver);
        }

        public sealed override object FromBinary(byte[] bytes, Type type)
            => MessagePackSerializer.NonGeneric.Deserialize(type, bytes, _resolver);
    }
}
