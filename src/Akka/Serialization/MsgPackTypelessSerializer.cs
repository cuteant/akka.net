using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Resolvers;
using CuteAnt.Extensions.Serialization;

namespace Akka.Serialization
{
    public sealed class MsgPackTypelessSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private static readonly TypelessMessagePackMessageFormatter s_formatter = TypelessMessagePackMessageFormatter.DefaultInstance;
        private readonly int _initialBufferSize;

        static MsgPackTypelessSerializer() => MsgPackSerializerHelper.Register();

        public MsgPackTypelessSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default) { }

        public MsgPackTypelessSerializer(ExtendedActorSystem system, Config config) : this(system, MsgPackSerializerSettings.Create(config)) { }

        public MsgPackTypelessSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
        {
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;
        }

        public override byte[] ToBinary(object obj) => s_formatter.SerializeObject(obj, _initialBufferSize);

        public override object FromBinary(byte[] bytes, Type type) => s_formatter.Deserialize(type, bytes);

        //public override int Identifier => 152;

        public override bool IncludeManifest => false;
    }
}
