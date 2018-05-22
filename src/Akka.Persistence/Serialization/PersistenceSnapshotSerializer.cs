//-----------------------------------------------------------------------
// <copyright file="PersistenceSnapshotSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Persistence.Serialization.Proto.Msg;
using Akka.Serialization;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Persistence.Serialization
{
    public class PersistenceSnapshotSerializer : Serializer
    {
        public PersistenceSnapshotSerializer(ExtendedActorSystem system) : base(system)
        {
            IncludeManifest = true;
        }

        public override bool IncludeManifest { get; }

        public override byte[] ToBinary(object obj)
        {
            if (obj is Snapshot snapShot) return GetPersistentPayload(snapShot).ToArray();

            throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{GetType()}]");
        }

        private PersistentPayload GetPersistentPayload(Snapshot snapshot)
        {
            var serializer = system.Serialization.FindSerializerFor(snapshot.Data);
            var payload = new PersistentPayload();

            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                payload.IsSerializerWithStringManifest = true;
                string manifest = manifestSerializer.Manifest(snapshot.Data);
                if (!string.IsNullOrEmpty(manifest))
                {
                    payload.HasManifest = true;
                    payload.PayloadManifest = ByteString.CopyFromUtf8(manifest);
                }
                else
                {
                    payload.PayloadManifest = ByteString.Empty;
                }
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    payload.HasManifest = true;
                    var typeKey = TypeSerializer.GetTypeKeyFromType(snapshot.Data.GetType());
                    payload.TypeHashCode = typeKey.HashCode;
                    payload.PayloadManifest = ProtobufUtil.FromBytes(typeKey.TypeName);
                }
            }

            payload.Payload = serializer.ToByteString(snapshot.Data);
            payload.SerializerId = serializer.Identifier;

            return payload;
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == typeof(Snapshot)) return GetSnapshot(bytes);

            throw new ArgumentException($"Unimplemented deserialization of message with type [{type}] in [{GetType()}]");
        }

        private Snapshot GetSnapshot(byte[] bytes)
        {
            var payload = PersistentPayload.Parser.ParseFrom(bytes);

            if (payload.IsSerializerWithStringManifest)
            {
                return new Snapshot(system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Payload),
                    payload.SerializerId,
                    payload.HasManifest ? payload.PayloadManifest.ToStringUtf8() : null));
            }
            else if (payload.HasManifest)
            {
                return new Snapshot(system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Payload),
                    payload.SerializerId,
                    ProtobufUtil.GetBuffer(payload.PayloadManifest), payload.TypeHashCode));
            }
            else
            {
                return new Snapshot(system.Serialization.Deserialize(ProtobufUtil.GetBuffer(payload.Payload), payload.SerializerId));
            }
        }
    }
}
