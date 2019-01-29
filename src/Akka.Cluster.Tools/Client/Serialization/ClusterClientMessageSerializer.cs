//-----------------------------------------------------------------------
// <copyright file="ClusterClientMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt;
using CuteAnt.Text;
using MessagePack;

namespace Akka.Cluster.Tools.Client.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterClientMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string ContactsManifest = "A";
        private static readonly byte[] ContactsManifestBytes;
        private const string GetContactsManifest = "B";
        private static readonly byte[] GetContactsManifestBytes;
        private const string HeartbeatManifest = "C";
        private static readonly byte[] HeartbeatManifestBytes;
        private const string HeartbeatRspManifest = "D";
        private static readonly byte[] HeartbeatRspManifestBytes;

        private static readonly Dictionary<Type, string> ManifestMap;

        static ClusterClientMessageSerializer()
        {
            ContactsManifestBytes = StringHelper.UTF8NoBOM.GetBytes(ContactsManifest);
            GetContactsManifestBytes = StringHelper.UTF8NoBOM.GetBytes(GetContactsManifest);
            HeartbeatManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HeartbeatManifest);
            HeartbeatRspManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HeartbeatRspManifest);

            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(ClusterReceptionist.Contacts), ContactsManifest},
                { typeof(ClusterReceptionist.GetContacts), GetContactsManifest},
                { typeof(ClusterReceptionist.Heartbeat), HeartbeatManifest},
                { typeof(ClusterReceptionist.HeartbeatRsp), HeartbeatRspManifest},
            };
        }

        #endregion

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterClientMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public ClusterClientMessageSerializer(ExtendedActorSystem system) : base(system) { }

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
                case ClusterReceptionist.GetContacts _:
                case ClusterReceptionist.Heartbeat _:
                case ClusterReceptionist.HeartbeatRsp _:
                    return EmptyBytes;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterClientMessage(obj);
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
            switch (manifest)
            {
                case ContactsManifest:
                    return ContactsFromBinary(bytes);
                case GetContactsManifest:
                    return ClusterReceptionist.GetContacts.Instance;
                case HeartbeatManifest:
                    return ClusterReceptionist.Heartbeat.Instance;
                case HeartbeatRspManifest:
                    return ClusterReceptionist.HeartbeatRsp.Instance;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterClientMessage(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return string.Empty; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            return ThrowHelper.ThrowArgumentException_Manifest_ClusterClientMessage<string>(type);
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
                case ClusterReceptionist.Contacts _:
                    return ContactsManifest;
                case ClusterReceptionist.GetContacts _:
                    return GetContactsManifest;
                case ClusterReceptionist.Heartbeat _:
                    return HeartbeatManifest;
                case ClusterReceptionist.HeartbeatRsp _:
                    return HeartbeatRspManifest;
                default:
                    return ThrowHelper.ThrowArgumentException_Manifest_ClusterClientMessage<string>(o);
            }
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out byte[] manifest)
        {
            switch (obj)
            {
                case ClusterReceptionist.Contacts contacts:
                    manifest = ContactsManifestBytes;
                    return ContactsToProto(contacts);
                case ClusterReceptionist.GetContacts _:
                    manifest = GetContactsManifestBytes;
                    return EmptyBytes;
                case ClusterReceptionist.Heartbeat _:
                    manifest = HeartbeatManifestBytes;
                    return EmptyBytes;
                case ClusterReceptionist.HeartbeatRsp _:
                    manifest = HeartbeatRspManifestBytes;
                    return EmptyBytes;
                default:
                    manifest = null; return ThrowHelper.ThrowArgumentException_Manifest_ClusterClientMessage<byte[]>(obj);
            }
        }

        private byte[] ContactsToProto(ClusterReceptionist.Contacts message)
        {
            var protoMessage = new Protocol.Contacts(message.ContactPoints.ToArray());
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private ClusterReceptionist.Contacts ContactsFromBinary(byte[] binary)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Contacts>(binary, s_defaultResolver);
            return new ClusterReceptionist.Contacts(proto.ContactPoints.ToImmutableList());
        }
    }
}
