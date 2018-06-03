using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using CuteAnt.Extensions.Serialization;
using Hyperion;
using MessagePack;
using MessagePack.Resolvers;

namespace Akka.Serialization
{
    public sealed class MsgPackTypelessSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly TypelessMessagePackMessageFormatter _formatter;
        private readonly int _initialBufferSize;

        static MsgPackTypelessSerializer() => MsgPackSerializerHelper.Register();

        public MsgPackTypelessSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default) { }

        public MsgPackTypelessSerializer(ExtendedActorSystem system, Config config) : this(system, MsgPackSerializerSettings.Create(config)) { }

        public MsgPackTypelessSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
        {
            _settings = settings;
            _initialBufferSize = settings.InitialBufferSize;

            var akkaSurrogate =
                Surrogate
                .Create<ISurrogated, ISurrogate>(
                from => from.ToSurrogate(system),
                to => to.FromSurrogate(system));

            var serializer = new Hyperion.Serializer(
                new SerializerOptions(
                    versionTolerance: true,
                    preserveObjectReferences: true,
                    surrogates: new[] { akkaSurrogate }
                ));

            var resolver = new TypelessDefaultResolver();
            resolver.Context.Add(HyperionConstants.HyperionSerializer, serializer);
            resolver.Context.Add(MsgPackSerializerHelper.ActorSystem, system);
            _formatter = new TypelessMessagePackMessageFormatter(resolver);
        }

        public override byte[] ToBinary(object obj)
        {
            try
            {
                return _formatter.SerializeObject(obj, _initialBufferSize);
            }
            catch(Exception exc)
            {
                var err = exc.ToString();
                throw exc;
            }
        }

        public override object FromBinary(byte[] bytes, Type type) => _formatter.Deserialize(type, bytes);

        //public override int Identifier => 152;

        public override bool IncludeManifest => false;
    }
}
