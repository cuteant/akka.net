using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt.Extensions.Serialization;

namespace Akka.Serialization
{
    public sealed class LZ4MsgPackSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private static readonly LZ4MessagePackMessageFormatter s_formatter = LZ4MessagePackMessageFormatter.DefaultInstance;
        private readonly int _initialBufferSize;

        static LZ4MsgPackSerializer() => MsgPackSerializerHelper.Register();

        public LZ4MsgPackSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default) { }

        public LZ4MsgPackSerializer(ExtendedActorSystem system, Config config) : this(system, MsgPackSerializerSettings.Create(config)) { }

        public LZ4MsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
        {
            MsgPackSerializerHelper.LocalSystem.Value = system;
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;
        }

        public override byte[] ToBinary(object obj) => s_formatter.SerializeObject(obj, _initialBufferSize);

        public override object FromBinary(byte[] bytes, Type type) => s_formatter.Deserialize(type, bytes);

        public override int Identifier => 151;

        public override bool IncludeManifest => true;
    }
}
