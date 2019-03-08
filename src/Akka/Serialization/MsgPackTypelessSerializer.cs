using System;
using System.Threading;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt;
using MessagePack;

namespace Akka.Serialization
{
    public sealed class MsgPackTypelessSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly IFormatterResolver _resolver;
        private readonly int _initialBufferSize;

        public MsgPackTypelessSerializer(ExtendedActorSystem system)
            : this(system, MsgPackSerializerSettings.Default, HyperionSerializerSettings.Default) { }

        public MsgPackTypelessSerializer(ExtendedActorSystem system, Config config)
            : this(system, MsgPackSerializerSettings.Create(config), HyperionSerializerSettings.Create(config)) { }

        public MsgPackTypelessSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings, HyperionSerializerSettings hyperionSettings)
            : base(system)
        {
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;

            var serializer = HyperionSerializerHelper.CreateSerializer(system, hyperionSettings);
            _resolver = new AkkaTypelessResolver(system, serializer);

            Interlocked.CompareExchange(ref MsgPackSerializerHelper.DefaultResolver, _resolver, null);
        }

        //public sealed override int Identifier => 1;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source == null) { return null; }

            var serializedObject = MessagePackSerializer.SerializeUnsafe(source, _resolver);
            return MessagePackSerializer.Deserialize<object>(serializedObject, _resolver);
        }

        public sealed override byte[] ToBinary(object obj)
        {
            //if (null == obj) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理
            return MessagePackSerializer.Serialize(obj, _resolver);
        }

        public sealed override object FromBinary(byte[] bytes, Type type) => MessagePackSerializer.Deserialize<object>(bytes, _resolver);
    }
}
