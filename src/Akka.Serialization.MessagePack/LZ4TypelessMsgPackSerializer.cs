using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt.Extensions.Serialization;

namespace Akka.Serialization
{
    public sealed class LZ4TypelessMsgPackSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private static readonly LZ4TypelessMessagePackMessageFormatter s_formatter = LZ4TypelessMessagePackMessageFormatter.DefaultInstance;

        static LZ4TypelessMsgPackSerializer()
        {
            MsgPackSerializerHelper.Register();
        }

        public LZ4TypelessMsgPackSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default)
        {
        }

        public LZ4TypelessMsgPackSerializer(ExtendedActorSystem system, Config config)
            : this(system, MsgPackSerializerSettings.Create(config))
        {
        }

        public LZ4TypelessMsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
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
