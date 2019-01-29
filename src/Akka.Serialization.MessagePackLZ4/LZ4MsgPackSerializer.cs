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
    public sealed class LZ4MsgPackSerializer : SerializerWithTypeManifest
    {
        private readonly MsgPackSerializerSettings _settings;
        private readonly IFormatterResolver _resolver;
        private readonly int _initialBufferSize;

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

            _resolver = new DefaultResolver();
            _resolver.Context.Add(HyperionConstants.HyperionSerializer, serializer);
            _resolver.Context2.Add(HyperionConstants.HyperionSerializerIdentifier, serializer);
            _resolver.Context2.Add(MsgPackSerializerHelper.ActorSystemIdentifier, system);
        }

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source == null) { return null; }

            var type = source.GetType();
            var serializedObject = MessagePackSerializer.SerializeUnsafe(source, _resolver);
            return MessagePackSerializer.NonGeneric.Deserialize(type, serializedObject, _resolver);
        }

        public override byte[] ToBinary(object obj)
        {
            if (null == obj) { return EmptyArray<byte>.Instance; }
            return LZ4MessagePackSerializer.Serialize(obj, _resolver);
        }

        public override object FromBinary(byte[] bytes, Type type) => LZ4MessagePackSerializer.NonGeneric.Deserialize(type, new ArraySegment<byte>(bytes, 0, bytes.Length), _resolver);

        public override int Identifier => 102;
    }
}
