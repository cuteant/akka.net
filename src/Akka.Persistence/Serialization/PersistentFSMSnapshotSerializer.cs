//-----------------------------------------------------------------------
// <copyright file="PersistenceMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Persistence.Fsm;
using Akka.Persistence.Serialization.Protocol;
using Akka.Serialization;
using CuteAnt;
using CuteAnt.Collections;
using CuteAnt.Reflection;
using MessagePack;

namespace Akka.Persistence.Serialization
{
    public sealed class PersistentFSMSnapshotSerializer : SerializerWithTypeManifest
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;
        private static readonly CachedReadConcurrentDictionary<Type, bool> s_persistentFSMSnapshotMap =
            new CachedReadConcurrentDictionary<Type, bool>(DictionaryCacheConstants.SIZE_MEDIUM);

        public PersistentFSMSnapshotSerializer(ExtendedActorSystem system) : base(system) { }

        public override byte[] ToBinary(object obj)
        {
            if (s_persistentFSMSnapshotMap.GetOrAdd(obj.GetType(), s_isPersistentFSMSnapshotFunc))
            {
                return MessagePackSerializer.Serialize(GetPersistentFSMSnapshot(obj), s_defaultResolver);
            }
            ThrowHelper.ThrowArgumentException_MessageSerializerFSM(obj); return null;
        }

        private PersistentFSMSnapshot GetPersistentFSMSnapshot(object obj)
        {
            var fsmSnapshot = obj as PersistentFSM.IPersistentFSMSnapshot;

            var timeout = fsmSnapshot.Timeout;
            return new PersistentFSMSnapshot(
                fsmSnapshot.StateIdentifier,
                _system.Serialization.SerializeMessageWithTransport(fsmSnapshot.Data),
                timeout.HasValue ? (long)timeout.Value.TotalMilliseconds : 0L
            );
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (s_persistentFSMSnapshotMap.GetOrAdd(type, s_isPersistentFSMSnapshotFunc))
            {
                return GetPersistentFSMSnapshot(type, bytes);
            }

            ThrowHelper.ThrowSerializationException_FSM(type); return null;
        }

        private static readonly Func<Type, bool> s_isPersistentFSMSnapshotFunc = IsPersistentFSMSnapshot;
        private static bool IsPersistentFSMSnapshot(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PersistentFSM.PersistentFSMSnapshot<>);
        }

        private static readonly CachedReadConcurrentDictionary<Type, CtorInvoker<object>> s_ctorInvokerCache =
            new CachedReadConcurrentDictionary<Type, CtorInvoker<object>>(DictionaryCacheConstants.SIZE_SMALL);

        private object GetPersistentFSMSnapshot(Type type, byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<PersistentFSMSnapshot>(bytes, s_defaultResolver);

            TimeSpan? timeout = null;
            if (message.TimeoutMillis > 0)
            {
                timeout = TimeSpan.FromMilliseconds(message.TimeoutMillis);
            }

            object[] arguments = { message.StateIdentifier, _system.Deserialize(message.Data), timeout };

            var ctorInvoker = s_ctorInvokerCache.GetOrAdd(type, s_makeDelegateForCtorFunc);

            return ctorInvoker(arguments);
        }

        private static readonly Func<Type, CtorInvoker<object>> s_makeDelegateForCtorFunc = MakeDelegateForCtor;
        private static CtorInvoker<object> MakeDelegateForCtor(Type instanceType)
        {
            // use reflection to create the generic type of PersistentFSM.PersistentFSMSnapshot
            Type[] types = { TypeConstants.StringType, instanceType.GenericTypeArguments[0], typeof(TimeSpan?) };
            return instanceType.MakeDelegateForCtor(types);
        }
    }
}
