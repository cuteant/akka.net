//-----------------------------------------------------------------------
// <copyright file="WrappedPayloadSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Text;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt.Reflection;
using SerializedMessage = Akka.Remote.Serialization.Protocol.Payload;

namespace Akka.Remote.Serialization
{
    internal static class WrappedPayloadSupport
    {
        public static SerializedMessage PayloadToProto(ActorSystem system, object payload)
        {
            if (null == payload) { return null; }

            var payloadType = payload.GetType();
            var serializer = system.Serialization.FindSerializerForType(payloadType);

            var payloadProto = new SerializedMessage
            {
                Message = serializer.ToBinary(payload),
                SerializerId = serializer.Identifier
            };

            // get manifest
            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                payloadProto.ManifestMode = Protocol.MessageManifestMode.WithStringManifest;
                payloadProto.MessageManifest = manifestSerializer.ManifestBytes(payload);
            }
            else if (serializer.IncludeManifest)
            {
                payloadProto.ManifestMode = Protocol.MessageManifestMode.IncludeManifest;
                var typeKey = TypeSerializer.GetTypeKeyFromType(payloadType);
                payloadProto.TypeHashCode = typeKey.HashCode;
                payloadProto.MessageManifest = typeKey.TypeName;
            }

            return payloadProto;
        }

        public static object PayloadFrom(ActorSystem system, SerializedMessage payload)
        {
            if (null == payload) { return null; }

            switch (payload.ManifestMode)
            {
                case Protocol.MessageManifestMode.IncludeManifest:
                    return system.Serialization.Deserialize(payload.Message, payload.SerializerId, payload.MessageManifest, payload.TypeHashCode);

                case Protocol.MessageManifestMode.WithStringManifest:
                    return system.Serialization.Deserialize(payload.Message, payload.SerializerId, Encoding.UTF8.GetString(payload.MessageManifest));

                case Protocol.MessageManifestMode.None:
                default:
                    var msg = payload.Message;
                    if (null == msg || msg.Length == 0) { return null; }
                    return system.Serialization.Deserialize(msg, payload.SerializerId);
            }
        }
    }
}
