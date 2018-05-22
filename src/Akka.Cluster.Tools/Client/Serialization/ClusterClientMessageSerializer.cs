﻿//-----------------------------------------------------------------------
// <copyright file="ClusterClientMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt;
using CuteAnt.Text;

namespace Akka.Cluster.Tools.Client.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterClientMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string ContactsManifest = "A";
        private static readonly byte[] ContactsManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ContactsManifest);
        private const string GetContactsManifest = "B";
        private static readonly byte[] GetContactsManifestBytes = StringHelper.UTF8NoBOM.GetBytes(GetContactsManifest);
        private const string HeartbeatManifest = "C";
        private static readonly byte[] HeartbeatManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HeartbeatManifest);
        private const string HeartbeatRspManifest = "D";
        private static readonly byte[] HeartbeatRspManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HeartbeatRspManifest);

        #endregion

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;
        private readonly IDictionary<string, Func<byte[], IClusterClientMessage>> _fromBinaryMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterClientMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public ClusterClientMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _fromBinaryMap = new Dictionary<string, Func<byte[], IClusterClientMessage>>(StringComparer.Ordinal)
            {
                {ContactsManifest, ContactsFromBinary},
                {GetContactsManifest, _ => ClusterReceptionist.GetContacts.Instance},
                {HeartbeatManifest, _ => ClusterReceptionist.Heartbeat.Instance},
                {HeartbeatRspManifest, _ => ClusterReceptionist.HeartbeatRsp.Instance}
            };
        }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="obj"/> is of an unknown type.
        /// Acceptable values include:
        /// <see cref="ClusterReceptionist.Contacts"/> | <see cref="ClusterReceptionist.GetContacts"/> | <see cref="ClusterReceptionist.Heartbeat"/> | <see cref="ClusterReceptionist.HeartbeatRsp"/>
        /// </exception>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case ClusterReceptionist.Contacts contacts:
                    return ContactsToProto(contacts);
                case ClusterReceptionist.GetContacts getContacts:
                case ClusterReceptionist.Heartbeat heartbeat:
                case ClusterReceptionist.HeartbeatRsp heartbeatRsp:
                    return EmptyBytes;
                default:
                    throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{nameof(ClusterClientMessageSerializer)}]");
            }
        }

        /// <summary>
        /// Deserializes a byte array into an object using an optional <paramref name="manifest" /> (type hint).
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="manifest">The type hint used to deserialize the object contained in the array.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="bytes"/>cannot be deserialized using the specified <paramref name="manifest"/>.
        /// </exception>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, string manifest)
        {
            if (_fromBinaryMap.TryGetValue(manifest, out var deserializer))
            {
                return deserializer(bytes);
            }

            throw new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in serializer {nameof(ClusterClientMessageSerializer)}");
        }

        /// <summary>
        /// Returns the manifest (type hint) that will be provided in the <see cref="FromBinary(System.Byte[],System.String)" /> method.
        /// <note>
        /// This method returns <see cref="String.Empty" /> if a manifest is not needed.
        /// </note>
        /// </summary>
        /// <param name="o">The object for which the manifest is needed.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="o"/> does not have an associated manifest.
        /// </exception>
        /// <returns>The manifest needed for the deserialization of the specified <paramref name="o" />.</returns>
        public override string Manifest(object o)
        {
            switch (o)
            {
                case ClusterReceptionist.Contacts contacts:
                    return ContactsManifest;
                case ClusterReceptionist.GetContacts getContacts:
                    return GetContactsManifest;
                case ClusterReceptionist.Heartbeat heartbeat:
                    return HeartbeatManifest;
                case ClusterReceptionist.HeartbeatRsp heartbeatRsp:
                    return HeartbeatRspManifest;
                default:
                    throw new ArgumentException($"Can't serialize object of type [{o.GetType()}] in [{nameof(ClusterClientMessageSerializer)}]");
            }
        }

        /// <inheritdoc />
        public override byte[] ManifestBytes(object o)
        {
            switch (o)
            {
                case ClusterReceptionist.Contacts contacts:
                    return ContactsManifestBytes;
                case ClusterReceptionist.GetContacts getContacts:
                    return GetContactsManifestBytes;
                case ClusterReceptionist.Heartbeat heartbeat:
                    return HeartbeatManifestBytes;
                case ClusterReceptionist.HeartbeatRsp heartbeatRsp:
                    return HeartbeatRspManifestBytes;
                default:
                    throw new ArgumentException($"Can't serialize object of type [{o.GetType()}] in [{nameof(ClusterClientMessageSerializer)}]");
            }
        }

        private byte[] ContactsToProto(ClusterReceptionist.Contacts message)
        {
            var protoMessage = new Proto.Msg.Contacts();
            foreach (var contactPoint in message.ContactPoints)
            {
                protoMessage.ContactPoints.Add(contactPoint);
            }
            return protoMessage.ToArray();
        }

        private ClusterReceptionist.Contacts ContactsFromBinary(byte[] binary)
        {
            var proto = Proto.Msg.Contacts.Parser.ParseFrom(binary);
            return new ClusterReceptionist.Contacts(proto.ContactPoints.ToImmutableList());
        }
    }
}
