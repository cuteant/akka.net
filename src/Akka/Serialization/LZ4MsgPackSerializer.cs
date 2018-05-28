using System;
using Akka.Actor;
using Akka.Configuration;
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
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;
        }

        public override byte[] ToBinary(object obj)
        {
            MsgPackSerializerHelper.LocalSystem.Value = system;
            var bts = s_formatter.SerializeObject(obj, _initialBufferSize);
            MsgPackSerializerHelper.LocalSystem.Value = null;
            return bts;
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            MsgPackSerializerHelper.LocalSystem.Value = system;
            var obj = s_formatter.Deserialize(type, bytes);
            MsgPackSerializerHelper.LocalSystem.Value = null;
            return obj;
        }

        public override int Identifier => -2;

        public override bool IncludeManifest => true;
    }
}
