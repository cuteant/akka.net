//-----------------------------------------------------------------------
// <copyright file="PersistenceSnapshotSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
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

            var payload = GetPersistentPayload(snapShot);
            return new Snapshot(system.Serialization.Deserialize(payload));
        }

        public override byte[] ToBinary(object obj)
        {
            if (obj is Snapshot snapShot)
            {
                return MessagePackSerializer.Serialize(GetPersistentPayload(snapShot), s_defaultResolver);
            }

            return ThrowHelper.ThrowArgumentException_SnapshotSerializer(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Payload GetPersistentPayload(Snapshot snapShot)
        {
            var snapshotData = snapShot.Data;
            if (null == snapshotData) { return Payload.Null; }

            var serializer = system.Serialization.FindSerializerForType(snapshotData.GetType());
            return serializer.ToPayload(snapshotData);
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            var payload = MessagePackSerializer.Deserialize<Payload>(bytes, s_defaultResolver);
            return new Snapshot(system.Serialization.Deserialize(payload));
            //if (type == typeof(Snapshot)) return GetSnapshot(bytes);
            //return ThrowHelper.ThrowArgumentException_SnapshotSerializer(type);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private Snapshot GetSnapshot(byte[] bytes)
        //{
        //    var payload = MessagePackSerializer.Deserialize<Payload>(bytes, s_defaultResolver);
        //    return new Snapshot(system.Serialization.Deserialize(payload));
        //}
    }
}
