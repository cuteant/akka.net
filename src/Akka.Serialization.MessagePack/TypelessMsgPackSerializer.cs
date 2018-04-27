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

        static TypelessMsgPackSerializer()
        {
            MsgPackSerializerHelper.Register();
        }

        public TypelessMsgPackSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default)
        {
        }

        public TypelessMsgPackSerializer(ExtendedActorSystem system, Config config)
            : this(system, MsgPackSerializerSettings.Create(config))
        {
        }

        public TypelessMsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
        {
            MsgPackSerializerHelper.LocalSystem.Value = system;
            _settings = settings;
        }

        public override byte[] ToBinary(object obj)
        {
            return s_formatter.SerializeObject(obj);
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            return s_formatter.Deserialize(type, bytes);
        }

        public override int Identifier => 150;

        public override bool IncludeManifest => true;
    }
}
