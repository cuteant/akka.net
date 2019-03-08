//-----------------------------------------------------------------------
// <copyright file="PersistenceSnapshotSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using MessagePack;

namespace Akka.Persistence.Serialization
{
    public class PersistenceSnapshotSerializer : Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        public PersistenceSnapshotSerializer(ExtendedActorSystem system) : base(system) { }

        public override object DeepCopy(object source)
        {
            if (null == source) { return null; }
            var snapShot = source as Snapshot;
            if (null == snapShot) { ThrowHelper.ThrowArgumentException_SnapshotSerializer(source); }

            var payload = system.Serialize(snapShot.Data);
            return new Snapshot(system.Deserialize(payload));
        }

        public override byte[] ToBinary(object obj)
        {
            var snapShot = obj as Snapshot;
            if (null == snapShot) { ThrowHelper.ThrowArgumentException_SnapshotSerializer(obj); }
            return MessagePackSerializer.Serialize(system.Serialize(snapShot.Data), s_defaultResolver);
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            var payload = MessagePackSerializer.Deserialize<Payload>(bytes, s_defaultResolver);
            return new Snapshot(system.Deserialize(payload));
            //if (type == typeof(Snapshot)) return GetSnapshot(bytes);
            //return ThrowHelper.ThrowArgumentException_SnapshotSerializer(type);
        }
    }
}
