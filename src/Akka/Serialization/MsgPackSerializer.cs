using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt;
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
        public sealed override object DeepCopy(object source)
        {
            if (source == null) { return null; }

            var type = source.GetType();
            var serializedObject = MessagePackSerializer.SerializeUnsafe(source, _resolver);
            return MessagePackSerializer.NonGeneric.Deserialize(type, serializedObject, _resolver);
        }

        public override byte[] ToBinary(object obj)
        {
            if (null == obj) { return EmptyArray<byte>.Instance; }
            return MessagePackSerializer.Serialize(obj, _resolver);
        }

        public override object FromBinary(byte[] bytes, Type type) => MessagePackSerializer.NonGeneric.Deserialize(type, bytes, _resolver);

        public override int Identifier => 101;
    }
}
