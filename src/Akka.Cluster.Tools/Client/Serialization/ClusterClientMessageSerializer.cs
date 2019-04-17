﻿//-----------------------------------------------------------------------
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
        private const string GetContactsManifest = "B";
        private const string HeartbeatManifest = "C";
        private const string HeartbeatRspManifest = "D";

        private static readonly Dictionary<Type, string> ManifestMap;

        static ClusterClientMessageSerializer()
        {
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

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case ClusterReceptionist.Contacts contacts:
                    manifest = ContactsManifest;
                    return ContactsToProto(contacts);
                case ClusterReceptionist.GetContacts _:
                    manifest = GetContactsManifest;
                    return EmptyBytes;
                case ClusterReceptionist.Heartbeat _:
                    manifest = HeartbeatManifest;
                    return EmptyBytes;
                case ClusterReceptionist.HeartbeatRsp _:
                    manifest = HeartbeatRspManifest;
                    return EmptyBytes;
                default:
                    throw ThrowHelper.GetArgumentException_Manifest_ClusterClientMessage(obj);
            }
        }

        /// <inheritdoc />
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
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterClientMessage(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            throw ThrowHelper.GetArgumentException_Manifest_ClusterClientMessage(type);
        }

        /// <inheritdoc />
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
                    throw ThrowHelper.GetArgumentException_Manifest_ClusterClientMessage(o);
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
