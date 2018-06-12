﻿using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using CuteAnt;
using Hyperion;
using MessagePack;
using MessagePack.Resolvers;

namespace Akka.Serialization
{
    public sealed class MsgPackTypelessSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly IFormatterResolver _resolver;
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

            _resolver = new TypelessDefaultResolver();
            _resolver.Context.Add(HyperionConstants.HyperionSerializer, serializer);
            _resolver.Context.Add(MsgPackSerializerHelper.ActorSystem, system);
        }

        public override byte[] ToBinary(object obj)
        {
            if (null == obj) { return EmptyArray<byte>.Instance; }
            return MessagePackSerializer.Serialize(obj, _resolver);
        }

        public override object FromBinary(byte[] bytes, Type type) => MessagePackSerializer.Deserialize<object>(bytes, _resolver);

        //public override int Identifier => 152;

        public override bool IncludeManifest => false;
    }
}