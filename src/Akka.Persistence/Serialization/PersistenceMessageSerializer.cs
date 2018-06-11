//-----------------------------------------------------------------------
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
using System.Runtime.Serialization;
using System.Text;
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
    public sealed class PersistenceMessageSerializer : Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        public PersistenceMessageSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override bool IncludeManifest { get; } = true;

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
                    if (obj.GetType().GetTypeInfo().IsGenericType
                        && obj.GetType().GetGenericTypeDefinition() == typeof(PersistentFSM.PersistentFSMSnapshot<>))
                    {
                        return MessagePackSerializer.Serialize(GetPersistentFSMSnapshot(obj), s_defaultResolver);
                    }
                    throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{GetType()}]");
            }
        }

        private PersistentMessage GetPersistentMessage(IPersistentRepresentation persistent)
        {
            var message = new PersistentMessage();

            if (persistent.PersistenceId != null) message.PersistenceId = persistent.PersistenceId;
            if (persistent.Manifest != null) message.Manifest = persistent.Manifest;
            if (persistent.WriterGuid != null) message.WriterGuid = persistent.WriterGuid;
            if (persistent.Sender != null) message.Sender = Akka.Serialization.Serialization.SerializedActorPath(persistent.Sender);

            message.Payload = GetPersistentPayload(persistent.Payload);
            message.SequenceNr = persistent.SequenceNr;
            message.Deleted = persistent.IsDeleted;

            return message;
        }

        private PersistentPayload GetPersistentPayload(object obj)
        {
            if (null == obj) { return null; }

            var objType = obj.GetType();
            var serializer = system.Serialization.FindSerializerForType(objType);
            var payload = new PersistentPayload();

            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                payload.ManifestMode = MessageManifestMode.WithStringManifest;
                payload.PayloadManifest = manifestSerializer.ManifestBytes(obj);
            }
            else if (serializer.IncludeManifest)
            {
                payload.ManifestMode = MessageManifestMode.IncludeManifest;
                var typeKey = TypeSerializer.GetTypeKeyFromType(objType);
                payload.TypeHashCode = typeKey.HashCode;
                payload.PayloadManifest = typeKey.TypeName;
            }

            payload.Payload = serializer.ToBinary(obj);
            payload.SerializerId = serializer.Identifier;

            return payload;
        }

        private Protocol.AtomicWrite GetAtomicWrite(AtomicWrite write)
        {
            return new Protocol.AtomicWrite(((IImmutableList<IPersistentRepresentation>)write.Payload).Select(_ => GetPersistentMessage(_)).ToArray());
        }

        private Protocol.AtLeastOnceDeliverySnapshot GetAtLeastOnceDeliverySnapshot(AtLeastOnceDeliverySnapshot snapshot)
        {
            var unconfirmedDeliveries = snapshot.UnconfirmedDeliveries?
                .Select(_ => new Protocol.UnconfirmedDelivery(_.DeliveryId, _.Destination.ToString(), GetPersistentPayload(_.Message))).ToArray();
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
                GetPersistentPayload(fsmSnapshot.Data),
                timeout.HasValue ? (long)timeout.Value.TotalMilliseconds : 0L
            );
        }

        private static readonly Dictionary<Type, Func<PersistenceMessageSerializer, byte[], object>> s_fromBinaryMap = new Dictionary<Type, Func<PersistenceMessageSerializer, byte[], object>>()
        {
            { typeof(Persistent), (s, b)=> GetPersistentRepresentation(s, MessagePackSerializer.Deserialize<PersistentMessage>(b, s_defaultResolver)) },
            { typeof(IPersistentRepresentation), (s, b)=> GetPersistentRepresentation(s, MessagePackSerializer.Deserialize<PersistentMessage>(b, s_defaultResolver)) },
            { typeof(AtomicWrite), (s, b)=> GetAtomicWrite(s, b) },
            { typeof(AtLeastOnceDeliverySnapshot), (s, b)=> GetAtLeastOnceDeliverySnapshot(s, b) },
            { typeof(PersistentFSM.StateChangeEvent), (s, b)=> GetStateChangeEvent(b) },
        };
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (s_fromBinaryMap.TryGetValue(type, out var factory))
            {
                return factory(this, bytes);
            }
            if (type.GetTypeInfo().IsGenericType
                && type.GetGenericTypeDefinition() == typeof(PersistentFSM.PersistentFSMSnapshot<>))
            {
                return GetPersistentFSMSnapshot(type, bytes);
            }

            throw new SerializationException($"Unimplemented deserialization of message with type [{type}] in [{GetType()}]");
        }

        private static IPersistentRepresentation GetPersistentRepresentation(PersistenceMessageSerializer serializer, PersistentMessage message)
        {
            var sender = ActorRefs.NoSender;
            if (message.Sender != null)
            {
                sender = serializer.system.Provider.ResolveActorRef(message.Sender);
            }

            return new Persistent(
                serializer.GetPayload(message.Payload),
                message.SequenceNr,
                message.PersistenceId,
                message.Manifest,
                message.Deleted,
                sender,
                message.WriterGuid);
        }

        private object GetPayload(PersistentPayload payload)
        {
            switch (payload.ManifestMode)
            {
                case MessageManifestMode.IncludeManifest:
                    return system.Serialization.Deserialize(payload.Payload, payload.SerializerId, new TypeKey(payload.TypeHashCode, payload.PayloadManifest));
                case MessageManifestMode.WithStringManifest:
                    return system.Serialization.Deserialize(payload.Payload, payload.SerializerId, Encoding.UTF8.GetString(payload.PayloadManifest));
                case MessageManifestMode.None:
                default:
                    return system.Serialization.Deserialize(payload.Payload, payload.SerializerId);
            }
        }

        private static AtomicWrite GetAtomicWrite(PersistenceMessageSerializer serializer, byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.AtomicWrite>(bytes, s_defaultResolver);
            var payloads = new List<IPersistentRepresentation>();
            foreach (var payload in message.Payload)
            {
                payloads.Add(GetPersistentRepresentation(serializer, payload));
            }
            return new AtomicWrite(payloads.ToImmutableList());
        }

        private static AtLeastOnceDeliverySnapshot GetAtLeastOnceDeliverySnapshot(PersistenceMessageSerializer serializer, byte[] bytes)
        {
            var message = MessagePackSerializer.Deserialize<Protocol.AtLeastOnceDeliverySnapshot>(bytes, s_defaultResolver);

            var unconfirmedDeliveries = new List<UnconfirmedDelivery>();
            if (message.UnconfirmedDeliveries != null)
            {
                foreach (var unconfirmed in message.UnconfirmedDeliveries)
                {
                    ActorPath.TryParse(unconfirmed.Destination, out var actorPath);
                    unconfirmedDeliveries.Add(new UnconfirmedDelivery(unconfirmed.DeliveryId, actorPath, serializer.GetPayload(unconfirmed.Payload)));
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

            CtorInvoker<object> MakeDelegateForCtor(Type instanceType)
            {
                // use reflection to create the generic type of PersistentFSM.PersistentFSMSnapshot
                Type[] types = { TypeConstants.StringType, type.GenericTypeArguments[0], typeof(TimeSpan?) };
                return instanceType.MakeDelegateForCtor(types);
            }

            object[] arguments = { message.StateIdentifier, GetPayload(message.Data), timeout };

            var ctorInvoker = s_ctorInvokerCache.GetOrAdd(type, MakeDelegateForCtor);

            return ctorInvoker(arguments);
        }
    }
}
