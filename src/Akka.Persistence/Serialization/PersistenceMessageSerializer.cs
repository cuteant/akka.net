//-----------------------------------------------------------------------
// <copyright file="PersistenceMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Persistence.Fsm;
using Akka.Persistence.Serialization.Proto.Msg;
using Akka.Serialization;
using CuteAnt;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Persistence.Serialization
{
    public sealed class PersistenceMessageSerializer : Serializer
    {
        public PersistenceMessageSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override bool IncludeManifest { get; } = true;

        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case IPersistentRepresentation repr:
                    return GetPersistentMessage(repr).ToArray();
                case AtomicWrite aw:
                    return GetAtomicWrite(aw).ToArray();
                case AtLeastOnceDeliverySnapshot snap:
                    return GetAtLeastOnceDeliverySnapshot(snap).ToArray();
                case PersistentFSM.StateChangeEvent stateEvent:
                    return GetStateChangeEvent(stateEvent).ToArray();
                default:
                    if (obj.GetType().GetTypeInfo().IsGenericType
                        && obj.GetType().GetGenericTypeDefinition() == typeof(PersistentFSM.PersistentFSMSnapshot<>))
                    {
                        return GetPersistentFSMSnapshot(obj).ToArray();
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
            var serializer = system.Serialization.FindSerializerFor(obj);
            var payload = new PersistentPayload();

            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                payload.IsSerializerWithStringManifest = true;
                var manifest = manifestSerializer.ManifestBytes(obj);
                if (manifest != null)
                {
                    payload.HasManifest = true;
                    payload.PayloadManifest = ProtobufUtil.FromBytes(manifest);
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
                    var typeKey = TypeSerializer.GetTypeKeyFromType(obj.GetType());
                    payload.TypeHashCode = typeKey.HashCode;
                    payload.PayloadManifest = ProtobufUtil.FromBytes(typeKey.TypeName);
                }
            }

            payload.Payload = serializer.ToByteString(obj);
            payload.SerializerId = serializer.Identifier;

            return payload;
        }

        private Proto.Msg.AtomicWrite GetAtomicWrite(AtomicWrite write)
        {
            var message = new Proto.Msg.AtomicWrite();
            foreach (var pr in (IImmutableList<IPersistentRepresentation>)write.Payload)
            {
                message.Payload.Add(GetPersistentMessage(pr));
            }
            return message;
        }

        private Proto.Msg.AtLeastOnceDeliverySnapshot GetAtLeastOnceDeliverySnapshot(AtLeastOnceDeliverySnapshot snapshot)
        {
            var message = new Proto.Msg.AtLeastOnceDeliverySnapshot
            {
                CurrentDeliveryId = snapshot.CurrentDeliveryId
            };

            foreach (var unconfirmed in snapshot.UnconfirmedDeliveries)
            {
                message.UnconfirmedDeliveries.Add(new Proto.Msg.UnconfirmedDelivery
                {
                    DeliveryId = unconfirmed.DeliveryId,
                    Destination = unconfirmed.Destination.ToString(),
                    Payload = GetPersistentPayload(unconfirmed.Message)
                });
            }
            return message;
        }

        private static PersistentStateChangeEvent GetStateChangeEvent(PersistentFSM.StateChangeEvent changeEvent)
        {
            var message = new PersistentStateChangeEvent
            {
                StateIdentifier = changeEvent.StateIdentifier
            };
            if (changeEvent.Timeout.HasValue)
            {
                message.TimeoutMillis = (long)changeEvent.Timeout.Value.TotalMilliseconds;
            }
            return message;
        }

        private PersistentFSMSnapshot GetPersistentFSMSnapshot(object obj)
        {
            var type = obj.GetType();

            var message = new PersistentFSMSnapshot
            {
                StateIdentifier = (string)type.GetProperty("StateIdentifier")?.GetValue(obj),
                Data = GetPersistentPayload(type.GetProperty("Data")?.GetValue(obj))
            };
            TimeSpan? timeout = (TimeSpan?)type.GetProperty("Timeout")?.GetValue(obj);
            if (timeout.HasValue)
            {
                message.TimeoutMillis = (long)timeout.Value.TotalMilliseconds;
            }
            return message;
        }

        private static readonly Dictionary<Type, Func<PersistenceMessageSerializer, byte[], object>> s_fromBinaryMap = new Dictionary<Type, Func<PersistenceMessageSerializer, byte[], object>>()
        {
            { typeof(Persistent), (s, b)=> GetPersistentRepresentation(s, PersistentMessage.Parser.ParseFrom(b)) },
            { typeof(IPersistentRepresentation), (s, b)=> GetPersistentRepresentation(s, PersistentMessage.Parser.ParseFrom(b)) },
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
            if (payload.IsSerializerWithStringManifest)
            {
                return system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Payload),
                    payload.SerializerId,
                    payload.HasManifest ? payload.PayloadManifest.ToStringUtf8() : null);
            }
            else if (payload.HasManifest)
            {
                return system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Payload),
                    payload.SerializerId,
                    ProtobufUtil.GetBuffer(payload.PayloadManifest), payload.TypeHashCode);
            }
            else
            {
                return system.Serialization.Deserialize(ProtobufUtil.GetBuffer(payload.Payload), payload.SerializerId);
            }
        }

        private static AtomicWrite GetAtomicWrite(PersistenceMessageSerializer serializer, byte[] bytes)
        {
            var message = Proto.Msg.AtomicWrite.Parser.ParseFrom(bytes);
            var payloads = new List<IPersistentRepresentation>();
            foreach (var payload in message.Payload)
            {
                payloads.Add(GetPersistentRepresentation(serializer, payload));
            }
            return new AtomicWrite(payloads.ToImmutableList());
        }

        private static AtLeastOnceDeliverySnapshot GetAtLeastOnceDeliverySnapshot(PersistenceMessageSerializer serializer, byte[] bytes)
        {
            var message = Proto.Msg.AtLeastOnceDeliverySnapshot.Parser.ParseFrom(bytes);

            var unconfirmedDeliveries = new List<UnconfirmedDelivery>();
            foreach (var unconfirmed in message.UnconfirmedDeliveries)
            {
                ActorPath.TryParse(unconfirmed.Destination, out var actorPath);
                unconfirmedDeliveries.Add(new UnconfirmedDelivery(unconfirmed.DeliveryId, actorPath, serializer.GetPayload(unconfirmed.Payload)));
            }

            return new AtLeastOnceDeliverySnapshot(message.CurrentDeliveryId, unconfirmedDeliveries.ToArray());
        }

        private static PersistentFSM.StateChangeEvent GetStateChangeEvent(byte[] bytes)
        {
            var message = PersistentStateChangeEvent.Parser.ParseFrom(bytes);
            TimeSpan? timeout = null;
            if (message.TimeoutMillis > 0)
            {
                timeout = TimeSpan.FromMilliseconds(message.TimeoutMillis);
            }
            return new PersistentFSM.StateChangeEvent(message.StateIdentifier, timeout);
        }

        private object GetPersistentFSMSnapshot(Type type, byte[] bytes)
        {
            var message = PersistentFSMSnapshot.Parser.ParseFrom(bytes);

            TimeSpan? timeout = null;
            if (message.TimeoutMillis > 0)
            {
                timeout = TimeSpan.FromMilliseconds(message.TimeoutMillis);
            }

            // use reflection to create the generic type of PersistentFSM.PersistentFSMSnapshot
            Type[] types = { TypeConstants.StringType, type.GenericTypeArguments[0], typeof(TimeSpan?) };
            object[] arguments = { message.StateIdentifier, GetPayload(message.Data), timeout };

            return type.GetConstructor(types).Invoke(arguments);
        }
    }
}
