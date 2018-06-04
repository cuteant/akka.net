//-----------------------------------------------------------------------
// <copyright file="WrappedPayloadSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Serialization;
using CuteAnt.Reflection;
using Google.Protobuf;
using SerializedMessage = Akka.Remote.Serialization.Proto.Msg.Payload;

namespace Akka.Remote.Serialization
{
    internal static class WrappedPayloadSupport
    {
        public static SerializedMessage PayloadToProto(ActorSystem system, object payload)
        {
            if (payload == null) // TODO: handle null messages
            {
                return new SerializedMessage();
            }

            var payloadProto = new SerializedMessage();
            var serializer = system.Serialization.FindSerializerFor(payload);

            payloadProto.Message = serializer.ToByteString(payload);
            payloadProto.SerializerId = serializer.Identifier;

            // get manifest
            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                payloadProto.IsSerializerWithStringManifest = true;
                var manifest = manifestSerializer.ManifestBytes(payload);
                if (manifest != null)
                {
                    payloadProto.HasManifest = true;
                    payloadProto.MessageManifest = ProtobufUtil.FromBytes(manifest);
                }
                else
                {
                    payloadProto.MessageManifest = ByteString.Empty;
                }
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    payloadProto.HasManifest = true;
                    var typeKey = TypeSerializer.GetTypeKeyFromType(payload.GetType());
                    payloadProto.TypeHashCode = typeKey.HashCode;
                    payloadProto.MessageManifest = ProtobufUtil.FromBytes(typeKey.TypeName);
                }
            }

            return payloadProto;
        }

        public static object PayloadFrom(ActorSystem system, SerializedMessage payload)
        {
            if (payload.IsSerializerWithStringManifest)
            {
                return system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Message),
                    payload.SerializerId,
                    payload.HasManifest ? payload.MessageManifest.ToStringUtf8() : null);
            }
            else if (payload.HasManifest)
            {
                return system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Message),
                    payload.SerializerId,
                    ProtobufUtil.GetBuffer(payload.MessageManifest), payload.TypeHashCode);
            }
            else
            {
                var msg = payload.Message;
                if (null == msg || msg.IsEmpty) { return null; }
                return system.Serialization.Deserialize(ProtobufUtil.GetBuffer(msg), payload.SerializerId);
            }
        }
    }
}
