﻿//-----------------------------------------------------------------------
// <copyright file="PersistenceMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
    public sealed class PersistenceMessageSerializer : SerializerWithTypeManifest
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;
        private static readonly CachedReadConcurrentDictionary<Type, bool> s_persistentFSMSnapshotMap =
            new CachedReadConcurrentDictionary<Type, bool>(DictionaryCacheConstants.SIZE_MEDIUM);

        public PersistenceMessageSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case IPersistentRepresentation repr:
                    return MessagePackSerializer.Serialize(GetPersistentMessage(repr), s_defaultResolver);
                case AtomicWrite aw:
                    return MessagePackSerializer.Serialize(GetAtomicWrite(aw), s_defaultResolver);
                case AtLeastOnceDeliverySnapshot snap:
                    return MessagePackSerializer.Serialize(GetAtLeastOnceDeliverySnapshot(snap), s_defaultResolver);
                case PersistentFSM.StateChangeEvent stateEvent:
                    return MessagePackSerializer.Serialize(GetStateChangeEvent(stateEvent), s_defaultResolver);
                default:
                    if (s_persistentFSMSnapshotMap.GetOrAdd(obj.GetType(), s_isPersistentFSMSnapshotFunc))
                    {
                        return MessagePackSerializer.Serialize(GetPersistentFSMSnapshot(obj), s_defaultResolver);
                    }
                    ThrowHelper.ThrowArgumentException_MessageSerializer(obj); return null;
            }
        }

        private PersistentMessage GetPersistentMessage(IPersistentRepresentation persistent)
        {
            var message = new PersistentMessage();

            if (persistent.PersistenceId != null) message.PersistenceId = persistent.PersistenceId;
            if (persistent.Manifest != null) message.Manifest = persistent.Manifest;
            if (persistent.WriterGuid != null) message.WriterGuid = persistent.WriterGuid;
            if (persistent.Sender != null) message.Sender = Akka.Serialization.Serialization.SerializedActorPath(persistent.Sender);

            message.Payload = system.Serialize(persistent.Payload);
            message.SequenceNr = persistent.SequenceNr;
            message.Deleted = persistent.IsDeleted;

            return message;
        }

        private Protocol.AtomicWrite GetAtomicWrite(AtomicWrite write)
        {
            var payload = (IImmutableList<IPersistentRepresentation>)write.Payload;
            var count = payload.Count;
            var persistentMsgs = new PersistentMessage[count];
            for (var idx = 0; idx < count; idx++)
            {
                persistentMsgs[idx] = GetPersistentMessage(payload[idx]);
            }
            return new Protocol.AtomicWrite(persistentMsgs);
        }

        private Protocol.AtLeastOnceDeliverySnapshot GetAtLeastOnceDeliverySnapshot(AtLeastOnceDeliverySnapshot snapshot)
        {
            var unconfirmedDeliveries = snapshot.UnconfirmedDeliveries?
                .Select(_ => new Protocol.UnconfirmedDelivery(_.DeliveryId, _.Destination.ToString(), system.Serialize(_.Message))).ToArray();
            return new Protocol.AtLeastOnceDeliverySnapshot(
                snapshot.CurrentDeliveryId,
                unconfirmedDeliveries
            );
        }

        private static PersistentStateChangeEvent GetStateChangeEvent(PersistentFSM.StateChangeEvent changeEvent)
        {
            var timeout = changeEvent.Timeout;
            return new PersistentStateChangeEvent(
                changeEvent.StateIdentifier,
                timeout.HasValue ? (long)timeout.Value.TotalMilliseconds : 0L
            );
        }

        private PersistentFSMSnapshot GetPersistentFSMSnapshot(object obj)
        {
            var type = obj.GetType();
            var fsmSnapshot = obj as PersistentFSM.IPersistentFSMSnapshot;

            var timeout = fsmSnapshot.Timeout;
            return new PersistentFSMSnapshot(
                fsmSnapshot.StateIdentifier,
                system.Serialize(fsmSnapshot.Data),
                timeout.HasValue ? (long)timeout.Value.TotalMilliseconds : 0L
            );
        }

        static class _
        {
            internal const int Persistent = 0;
            internal const int IPersistentRepresentation = 1;
            internal const int AtomicWrite = 2;
            internal const int AtLeastOnceDeliverySnapshot = 3;
            internal const int StateChangeEvent = 4;
        }
        private static readonly Dictionary<Type, int> s_fromBinaryMap = new Dictionary<Type, int>()
        {
            { typeof(Persistent), _.Persistent },
            { typeof(IPersistentRepresentation), _.IPersistentRepresentation },
            { typeof(AtomicWrite), _.AtomicWrite },
            { typeof(AtLeastOnceDeliverySnapshot), _.AtLeastOnceDeliverySnapshot },
            { typeof(PersistentFSM.StateChangeEvent), _.StateChangeEvent },
        };
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (s_fromBinaryMap.TryGetValue(type, out var flag))
            {
                switch (flag)
                {
                    case _.Persistent:
                    case _.IPersistentRepresentation:
                        return GetPersistentRepresentation(system, MessagePackSerializer.Deserialize<PersistentMessage>(bytes, s_defaultResolver));
                    case _.AtomicWrite:
                        return GetAtomicWrite(system, bytes);
                    case _.AtLeastOnceDeliverySnapshot:
                        return GetAtLeastOnceDeliverySnapshot(system, bytes);
                    case _.StateChangeEvent:
                        return GetStateChangeEvent(bytes);
                }
            }
            if (s_persistentFSMSnapshotMap.GetOrAdd(type, s_isPersistentFSMSnapshotFunc))
            {
                return GetPersistentFSMSnapshot(type, bytes);
            }

            ThrowHelper.ThrowSerializationException(type); return null;
        }

        private static readonly Func<Type, bool> s_isPersistentFSMSnapshotFunc = IsPersistentFSMSnapshot;
        private static bool IsPersistentFSMSnapshot(Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(PersistentFSM.PersistentFSMSnapshot<>);
        }

        private static IPersistentRepresentation GetPersistentRepresentation(ExtendedActorSystem system, PersistentMessage message)
        {
            var sender = ActorRefs.NoSender;
            if (message.Sender != null)
            {
                sender = system.Provider.ResolveActorRef(message.Sender);
            }

            return new Persistent(
                system.Deserialize(message.Payload),
                message.SequenceNr,
                message.PersistenceId,
                message.Manifest,
                message.Deleted,
                sender,
                message.WriterGuid);
        }

        private static AtomicWrite GetAtomicWrite(ExtendedActorSystem system, byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.AtomicWrite>(bytes, s_defaultResolver);
            var payloads = new List<IPersistentRepresentation>();
            foreach (var payload in message.Payload)
            {
                payloads.Add(GetPersistentRepresentation(system, payload));
            }
            return new AtomicWrite(payloads.ToImmutableList());
        }

        private static AtLeastOnceDeliverySnapshot GetAtLeastOnceDeliverySnapshot(ExtendedActorSystem system, byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.AtLeastOnceDeliverySnapshot>(bytes, s_defaultResolver);

            var unconfirmedDeliveries = new List<UnconfirmedDelivery>();
            if (message.UnconfirmedDeliveries != null)
            {
                foreach (var unconfirmed in message.UnconfirmedDeliveries)
                {
                    ActorPath.TryParse(unconfirmed.Destination, out var actorPath);
                    unconfirmedDeliveries.Add(new UnconfirmedDelivery(unconfirmed.DeliveryId, actorPath, system.Deserialize(unconfirmed.Payload)));
                }
            }
            return new AtLeastOnceDeliverySnapshot(message.CurrentDeliveryId, unconfirmedDeliveries.ToArray());
        }

        private static PersistentFSM.StateChangeEvent GetStateChangeEvent(byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<PersistentStateChangeEvent>(bytes, s_defaultResolver);
            TimeSpan? timeout = null;
            if (message.TimeoutMillis > 0)
            {
                timeout = TimeSpan.FromMilliseconds(message.TimeoutMillis);
            }
            return new PersistentFSM.StateChangeEvent(message.StateIdentifier, timeout);
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

            object[] arguments = { message.StateIdentifier, system.Deserialize(message.Data), timeout };

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
