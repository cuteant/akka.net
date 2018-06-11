//-----------------------------------------------------------------------
// <copyright file="PersistenceSnapshotSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Actor;
using Akka.Persistence.Serialization.Protocol;
using Akka.Serialization;
using CuteAnt.Reflection;
using MessagePack;

namespace Akka.Persistence.Serialization
{
    public class PersistenceSnapshotSerializer : Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        public PersistenceSnapshotSerializer(ExtendedActorSystem system) : base(system)
        {
            IncludeManifest = true;
        }

        public override bool IncludeManifest { get; }

        public override byte[] ToBinary(object obj)
        {
            if (obj is Snapshot snapShot) return MessagePackSerializer.Serialize(GetPersistentPayload(snapShot), s_defaultResolver);

            throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{GetType()}]");
        }

        private PersistentPayload GetPersistentPayload(Snapshot snapshot)
        {
            var snapshotData = snapshot.Data;
            if (null == snapshotData) { return null; }

            var snapshotDataType = snapshotData.GetType();
            var serializer = system.Serialization.FindSerializerForType(snapshotDataType);
            var payload = new PersistentPayload();

            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                payload.ManifestMode = MessageManifestMode.WithStringManifest;
                payload.PayloadManifest = manifestSerializer.ManifestBytes(snapshotData);
            }
            else if (serializer.IncludeManifest)
            {
                payload.ManifestMode = MessageManifestMode.IncludeManifest;
                var typeKey = TypeSerializer.GetTypeKeyFromType(snapshotDataType);
                payload.TypeHashCode = typeKey.HashCode;
                payload.PayloadManifest = typeKey.TypeName;
            }

            payload.Payload = serializer.ToBinary(snapshotData);
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
            var payload = MessagePackSerializer.Deserialize<PersistentPayload>(bytes, s_defaultResolver);

            object data;
            switch (payload.ManifestMode)
            {
                case MessageManifestMode.IncludeManifest:
                    data = system.Serialization.Deserialize(payload.Payload, payload.SerializerId, new TypeKey(payload.TypeHashCode, payload.PayloadManifest));
                    break;
                case MessageManifestMode.WithStringManifest:
                    data = system.Serialization.Deserialize(payload.Payload, payload.SerializerId, Encoding.UTF8.GetString(payload.PayloadManifest));
                    break;
                case MessageManifestMode.None:
                default:
                    data = system.Serialization.Deserialize(payload.Payload, payload.SerializerId);
                    break;
            }
            return new Snapshot(data);
        }
    }
}
