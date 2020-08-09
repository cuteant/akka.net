#region copyright
//-----------------------------------------------------------------------
// <copyright file="StreamRefSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Serialization;
using Akka.Streams.Implementation.StreamRef;
using MessagePack;
using CumulativeDemand = Akka.Streams.Implementation.StreamRef.CumulativeDemand;
using OnSubscribeHandshake = Akka.Streams.Implementation.StreamRef.OnSubscribeHandshake;
using RemoteStreamCompleted = Akka.Streams.Implementation.StreamRef.RemoteStreamCompleted;
using RemoteStreamFailure = Akka.Streams.Implementation.StreamRef.RemoteStreamFailure;
using SequencedOnNext = Akka.Streams.Implementation.StreamRef.SequencedOnNext;

namespace Akka.Streams.Serialization
{
    public sealed class StreamRefSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string SequencedOnNextManifest = "A";
        private const string CumulativeDemandManifest = "B";
        private const string RemoteSinkFailureManifest = "C";
        private const string RemoteSinkCompletedManifest = "D";
        private const string SourceRefManifest = "E";
        private const string SinkRefManifest = "F";
        private const string OnSubscribeHandshakeManifest = "G";

        private static readonly Dictionary<Type, string> ManifestMap;

        static StreamRefSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(SequencedOnNext), SequencedOnNextManifest },
                { typeof(CumulativeDemand), CumulativeDemandManifest },
                { typeof(OnSubscribeHandshake), RemoteSinkFailureManifest },
                { typeof(RemoteStreamFailure), RemoteSinkCompletedManifest },
                { typeof(RemoteStreamCompleted), SourceRefManifest },
                { typeof(SourceRefImpl), SinkRefManifest },
                { typeof(SinkRefImpl), OnSubscribeHandshakeManifest },
            };
        }

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        private readonly Akka.Serialization.Serialization _serialization;

        public StreamRefSerializer(ExtendedActorSystem system) : base(system)
        {
            _serialization = system.Serialization;
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object o, out string manifest)
        {
            switch (o)
            {
                case SequencedOnNext onNext:
                    manifest = SequencedOnNextManifest;
                    return MessagePackSerializer.Serialize(SerializeSequencedOnNext(onNext), s_defaultResolver);

                case CumulativeDemand demand:
                    manifest = CumulativeDemandManifest;
                    return MessagePackSerializer.Serialize(SerializeCumulativeDemand(demand), s_defaultResolver);

                case OnSubscribeHandshake handshake:
                    manifest = RemoteSinkFailureManifest;
                    return MessagePackSerializer.Serialize(SerializeOnSubscribeHandshake(handshake), s_defaultResolver);

                case RemoteStreamFailure failure:
                    manifest = RemoteSinkCompletedManifest;
                    return MessagePackSerializer.Serialize(SerializeRemoteStreamFailure(failure), s_defaultResolver);

                case RemoteStreamCompleted completed:
                    manifest = SourceRefManifest;
                    return MessagePackSerializer.Serialize(SerializeRemoteStreamCompleted(completed), s_defaultResolver);

                case SourceRefImpl sourceRef:
                    manifest = SinkRefManifest;
                    return MessagePackSerializer.Serialize(SerializeSourceRef(sourceRef), s_defaultResolver);

                case SinkRefImpl sinkRef:
                    manifest = OnSubscribeHandshakeManifest;
                    return MessagePackSerializer.Serialize(SerializeSinkRef(sinkRef), s_defaultResolver);
                default: throw ThrowHelper.GetArgumentException_Serializer_StreamRefMessages(o);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case SequencedOnNextManifest: return DeserializeSequenceOnNext(bytes);
                case CumulativeDemandManifest: return DeserializeCumulativeDemand(bytes);
                case OnSubscribeHandshakeManifest: return DeserializeOnSubscribeHandshake(bytes);
                case RemoteSinkFailureManifest: return DeserializeRemoteSinkFailure(bytes);
                case RemoteSinkCompletedManifest: return DeserializeRemoteSinkCompleted(bytes);
                case SourceRefManifest: return DeserializeSourceRef(bytes);
                case SinkRefManifest: return DeserializeSinkRef(bytes);
                default: throw ThrowHelper.GetArgumentException_Serializer_StreamRefMessages(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (type is null) { return null; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            throw ThrowHelper.GetArgumentException_Serializer_StreamRefMessages(type);
        }

        /// <inheritdoc />
        public override string Manifest(object o)
        {
            switch (o)
            {
                case SequencedOnNext _: return SequencedOnNextManifest;
                case CumulativeDemand _: return CumulativeDemandManifest;
                case OnSubscribeHandshake _: return OnSubscribeHandshakeManifest;
                case RemoteStreamFailure _: return RemoteSinkFailureManifest;
                case RemoteStreamCompleted _: return RemoteSinkCompletedManifest;
                case SourceRefImpl _: return SourceRefManifest;
                case SinkRefImpl _: return SinkRefManifest;
                default: throw ThrowHelper.GetArgumentException_Serializer_StreamRefMessages(o);
            }
        }

        private SinkRefImpl DeserializeSinkRef(byte[] bytes)
        {
            var sinkRef = MessagePackSerializer.Deserialize<Protocol.SinkRef>(bytes, s_defaultResolver);
            return SinkRefImpl.Create(sinkRef.EventType, _system.Provider.ResolveActorRef(sinkRef.TargetRef.Path));
        }

        private SourceRefImpl DeserializeSourceRef(byte[] bytes)
        {
            var sourceRef = MessagePackSerializer.Deserialize<Protocol.SourceRef>(bytes, s_defaultResolver);
            return SourceRefImpl.Create(sourceRef.EventType, _system.Provider.ResolveActorRef(sourceRef.OriginRef.Path));
        }

        private static RemoteStreamCompleted DeserializeRemoteSinkCompleted(byte[] bytes)
        {
            var completed = MessagePackSerializer.Deserialize<Protocol.RemoteStreamCompleted>(bytes, s_defaultResolver);
            return new RemoteStreamCompleted(completed.SeqNr);
        }

        private static RemoteStreamFailure DeserializeRemoteSinkFailure(byte[] bytes)
        {
            var failure = MessagePackSerializer.Deserialize<Protocol.RemoteStreamFailure>(bytes, s_defaultResolver);
            return new RemoteStreamFailure(failure.Cause);
        }

        private OnSubscribeHandshake DeserializeOnSubscribeHandshake(byte[] bytes)
        {
            var handshake = MessagePackSerializer.Deserialize<Protocol.OnSubscribeHandshake>(bytes, s_defaultResolver);
            var targetRef = _system.Provider.ResolveActorRef(handshake.TargetRef.Path);
            return new OnSubscribeHandshake(targetRef);
        }

        private static CumulativeDemand DeserializeCumulativeDemand(byte[] bytes)
        {
            var demand = MessagePackSerializer.Deserialize<Protocol.CumulativeDemand>(bytes, s_defaultResolver);
            return new CumulativeDemand(demand.SeqNr);
        }

        private SequencedOnNext DeserializeSequenceOnNext(byte[] bytes)
        {
            var onNext = MessagePackSerializer.Deserialize<Protocol.SequencedOnNext>(bytes, s_defaultResolver);
            var payload = _serialization.Deserialize(onNext.Payload);
            return new SequencedOnNext(onNext.SeqNr, payload);
        }

        private static Protocol.SinkRef SerializeSinkRef(SinkRefImpl sinkRef) => new Protocol.SinkRef
        (
            new Protocol.ActorRef(Akka.Serialization.Serialization.SerializedActorPath(sinkRef.InitialPartnerRef)),
            sinkRef.EventType
        );

        private static Protocol.SourceRef SerializeSourceRef(SourceRefImpl sourceRef) => new Protocol.SourceRef
        (
            new Protocol.ActorRef(Akka.Serialization.Serialization.SerializedActorPath(sourceRef.InitialPartnerRef)),
            sourceRef.EventType
        );

        private static Protocol.RemoteStreamCompleted SerializeRemoteStreamCompleted(RemoteStreamCompleted completed) =>
            new Protocol.RemoteStreamCompleted(completed.SeqNr);

        private static Protocol.RemoteStreamFailure SerializeRemoteStreamFailure(RemoteStreamFailure failure) =>
            new Protocol.RemoteStreamFailure(failure.Message);

        private static Protocol.OnSubscribeHandshake SerializeOnSubscribeHandshake(OnSubscribeHandshake handshake) =>
            new Protocol.OnSubscribeHandshake(new Protocol.ActorRef(Akka.Serialization.Serialization.SerializedActorPath(handshake.TargetRef)));

        private static Protocol.CumulativeDemand SerializeCumulativeDemand(CumulativeDemand demand) =>
            new Protocol.CumulativeDemand(demand.SeqNr);

        private Protocol.SequencedOnNext SerializeSequencedOnNext(SequencedOnNext onNext)
        {
            var payload = _serialization.SerializeMessage(onNext.Payload);

            return new Protocol.SequencedOnNext(onNext.SeqNr, payload);
        }
    }
}