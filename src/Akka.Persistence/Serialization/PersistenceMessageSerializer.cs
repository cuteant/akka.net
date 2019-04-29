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
using Akka.Actor;
using Akka.Persistence.Fsm;
using Akka.Persistence.Serialization.Protocol;
using Akka.Serialization;
using MessagePack;

namespace Akka.Persistence.Serialization
{
    public sealed class PersistenceMessageSerializer : SerializerWithStringManifest
    {
        private static readonly IFormatterResolver s_defaultResolver;

        #region manifests

        private const string PersistentManifest = "P";
        private const string AtomicWriteManifest = "AW";
        private const string ALODSnapshotManifest = "AS";
        private const string StateChangeEventManifest = "SCE";

        private static readonly Dictionary<Type, string> ManifestMap;

        static PersistenceMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(IPersistentRepresentation), PersistentManifest },
                { typeof(Persistent), PersistentManifest },
                { typeof(AtomicWrite), AtomicWriteManifest },
                { typeof(AtLeastOnceDeliverySnapshot), ALODSnapshotManifest },
                { typeof(PersistentFSM.StateChangeEvent), StateChangeEventManifest },
            };
            s_defaultResolver = MessagePackSerializer.DefaultResolver;
        }

        #endregion

        public PersistenceMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case IPersistentRepresentation repr:
                    manifest = PersistentManifest;
                    return MessagePackSerializer.Serialize(GetPersistentMessage(repr), s_defaultResolver);
                case AtomicWrite aw:
                    manifest = AtomicWriteManifest;
                    return MessagePackSerializer.Serialize(GetAtomicWrite(aw), s_defaultResolver);
                case AtLeastOnceDeliverySnapshot snap:
                    manifest = ALODSnapshotManifest;
                    return MessagePackSerializer.Serialize(GetAtLeastOnceDeliverySnapshot(snap), s_defaultResolver);
                case PersistentFSM.StateChangeEvent stateEvent:
                    manifest = StateChangeEventManifest;
                    return MessagePackSerializer.Serialize(GetStateChangeEvent(stateEvent), s_defaultResolver);
                default:
                    throw ThrowHelper.GetArgumentException_MessageSerializer(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case PersistentManifest:
                    return GetPersistentRepresentation(system, MessagePackSerializer.Deserialize<PersistentMessage>(bytes, s_defaultResolver));
                case AtomicWriteManifest:
                    return GetAtomicWrite(system, bytes);
                case ALODSnapshotManifest:
                    return GetAtLeastOnceDeliverySnapshot(system, bytes);
                case StateChangeEventManifest:
                    return GetStateChangeEvent(bytes);
                default:
                    throw ThrowHelper.GetArgumentException_Serializer(manifest);
            }
        }

        /// <inheritdoc />
        protected sealed override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            throw ThrowHelper.GetArgumentException_Serializer_D(type);
        }

        /// <inheritdoc />
        public sealed override string Manifest(object o)
        {
            switch (o)
            {
                case IPersistentRepresentation repr:
                    return PersistentManifest;
                case AtomicWrite aw:
                    return AtomicWriteManifest;
                case AtLeastOnceDeliverySnapshot snap:
                    return ALODSnapshotManifest;
                case PersistentFSM.StateChangeEvent stateEvent:
                    return StateChangeEventManifest;
                default:
                    throw ThrowHelper.GetArgumentException_Serializer_D(o);
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
    }
}