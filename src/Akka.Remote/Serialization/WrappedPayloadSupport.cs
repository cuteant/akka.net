//-----------------------------------------------------------------------
// <copyright file="WrappedPayloadSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Serialization;
using Akka.Util;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Remote.Serialization
{
    internal class WrappedPayloadSupport
    {
        private readonly ExtendedActorSystem _system;

        public WrappedPayloadSupport(ExtendedActorSystem system)
        {
            _system = system;
        }

        public Proto.Msg.Payload PayloadToProto(object payload)
        {
            if (payload == null) // TODO: handle null messages
            {
                return new Proto.Msg.Payload();
            }

            var payloadProto = new Proto.Msg.Payload();
            var serializer = _system.Serialization.FindSerializerFor(payload);

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

        public object PayloadFrom(Proto.Msg.Payload payload)
        {
            //var manifest = !payload.MessageManifest.IsEmpty
            //    ? payload.MessageManifest.ToStringUtf8()
            //    : string.Empty;

            //return _system.Serialization.Deserialize(
            //    payload.Message.ToByteArray(),
            //    payload.SerializerId,
            //    manifest);

            if (payload.IsSerializerWithStringManifest)
            {
                return _system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Message),
                    payload.SerializerId,
                    payload.HasManifest ? payload.MessageManifest.ToStringUtf8() : null);
            }
            else if (payload.HasManifest)
            {
                return _system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(payload.Message),
                    payload.SerializerId,
                    ProtobufUtil.GetBuffer(payload.MessageManifest), payload.TypeHashCode);
            }
            else
            {
                var msg = payload.Message;
                if (null == msg || msg.IsEmpty) { return null; }
                return _system.Serialization.Deserialize(ProtobufUtil.GetBuffer(msg), payload.SerializerId);
            }
        }
    }
}
