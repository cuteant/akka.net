﻿using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using CuteAnt.Extensions.Serialization;
using Hyperion;
using MessagePack;
using MessagePack.Resolvers;

namespace Akka.Serialization
{
    public sealed class LZ4MsgPackSerializer : Serializer
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly LZ4MessagePackMessageFormatter _formatter;
        private readonly int _initialBufferSize;

        static LZ4MsgPackSerializer() => MsgPackSerializerHelper.Register();

        public LZ4MsgPackSerializer(ExtendedActorSystem system) : this(system, MsgPackSerializerSettings.Default) { }

        public LZ4MsgPackSerializer(ExtendedActorSystem system, Config config) : this(system, MsgPackSerializerSettings.Create(config)) { }

        public LZ4MsgPackSerializer(ExtendedActorSystem system, MsgPackSerializerSettings settings) : base(system)
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

            var resolver = new DefaultResolver();
            resolver.Context.Add(HyperionConstants.HyperionSerializer, serializer);
            resolver.Context.Add(MsgPackSerializerHelper.ActorSystem, system);
            _formatter = new LZ4MessagePackMessageFormatter(resolver);
        }

        public override byte[] ToBinary(object obj) => _formatter.SerializeObject(obj, _initialBufferSize);

        public override object FromBinary(byte[] bytes, Type type) => _formatter.Deserialize(type, bytes);

        public override int Identifier => -2;

        public override bool IncludeManifest => true;
    }
}
