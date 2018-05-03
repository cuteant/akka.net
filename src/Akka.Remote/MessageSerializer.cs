//-----------------------------------------------------------------------
// <copyright file="MessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Serialization;
using CuteAnt.Reflection;
using Google.Protobuf;
using SerializedMessage = Akka.Remote.Serialization.Proto.Msg.Payload;

namespace Akka.Remote
{
    /// <summary>Class MessageSerializer.</summary>
    internal static class MessageSerializer
    {
        /// <summary>Deserializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        public static object Deserialize(ActorSystem system, SerializedMessage messageProtocol)
        {
            if (messageProtocol.IsSerializerWithStringManifest)
            {
                return system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(messageProtocol.Message),
                    messageProtocol.SerializerId,
                    messageProtocol.HasManifest ? messageProtocol.MessageManifest.ToStringUtf8() : null);
            }
            else if (messageProtocol.HasManifest)
            {
                return system.Serialization.Deserialize(
                    ProtobufUtil.GetBuffer(messageProtocol.Message),
                    messageProtocol.SerializerId,
                    ProtobufUtil.GetBuffer(messageProtocol.MessageManifest), messageProtocol.TypeHashCode);
            }
            else
            {
                return system.Serialization.Deserialize(ProtobufUtil.GetBuffer(messageProtocol.Message), messageProtocol.SerializerId);
            }
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        public static SerializedMessage Serialize(ActorSystem system, Address address, object message)
        {
            var objectType = message.GetType();
            Serializer serializer = system.Serialization.FindSerializerForType(objectType);

            var serializedMsg = new SerializedMessage
            {
                // ## 苦竹 修改 ##
                //Message = ByteString.CopyFrom(serializer.ToBinaryWithAddress(address, message)),
                Message = serializer.ToByteStringWithAddress(address, message),
                SerializerId = serializer.Identifier
            };

            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                serializedMsg.IsSerializerWithStringManifest = true;
                var manifest = manifestSerializer.Manifest(message);
                if (!string.IsNullOrEmpty(manifest))
                {
                    serializedMsg.HasManifest = true;
                    serializedMsg.MessageManifest = ByteString.CopyFromUtf8(manifest);
                }
                else
                {
                    serializedMsg.MessageManifest = ByteString.Empty;
                }
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    serializedMsg.HasManifest = true;
                    var typeKey = TypeSerializer.GetTypeKeyFromType(objectType);
                    serializedMsg.TypeHashCode = typeKey.HashCode;
                    serializedMsg.MessageManifest = ProtobufUtil.FromBytes(typeKey.TypeName);
                }
            }

            return serializedMsg;
        }
    }
}