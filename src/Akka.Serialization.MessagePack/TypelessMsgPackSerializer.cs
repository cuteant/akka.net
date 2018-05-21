using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt.Extensions.Serialization;

namespace Akka.Serialization
{
    public sealed class TypelessMsgPackSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private static readonly TypelessMessagePackMessageFormatter s_formatter = TypelessMessagePackMessageFormatter.DefaultInstance;
        private readonly int _initialBufferSize;

        static TypelessMsgPackSerializer() => MsgPackSerializerHelper.Register();

        public TypelessMsgPackSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default) { }

        public TypelessMsgPackSerializer(ExtendedActorSystem system, Config config) : this(system, MsgPackSerializerSettings.Create(config)) { }

        public TypelessMsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
        {
            MsgPackSerializerHelper.LocalSystem.Value = system;
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;
        }

        public override byte[] ToBinary(object obj) => s_formatter.SerializeObject(obj, _initialBufferSize);

        public override object FromBinary(byte[] bytes, Type type) => s_formatter.Deserialize(type, bytes);

        public override int Identifier => 152;

        public override bool IncludeManifest => false;
    }
}
