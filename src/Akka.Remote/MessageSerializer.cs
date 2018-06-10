//-----------------------------------------------------------------------
// <copyright file="MessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Actor;
using Akka.Serialization;
using Akka.Util;
using CuteAnt.Reflection;
using Microsoft.Extensions.Logging;
using SerializedMessage = Akka.Remote.Serialization.Protocol.Payload;

namespace Akka.Remote
{
    /// <summary>Class MessageSerializer.</summary>
    internal static class MessageSerializer
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(MessageSerializer));

        /// <summary>Deserializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        public static object Deserialize(ActorSystem system, SerializedMessage messageProtocol)
        {
            try
            {
                var manifest = messageProtocol.MessageManifest;
                switch (messageProtocol.ManifestMode)
                {
                    case Serialization.Protocol.MessageManifestMode.IncludeManifest:
                        return system.Serialization.Deserialize(messageProtocol.Message, messageProtocol.SerializerId, manifest, messageProtocol.TypeHashCode);

                    case Serialization.Protocol.MessageManifestMode.WithStringManifest:
                        return system.Serialization.Deserialize(messageProtocol.Message, messageProtocol.SerializerId, Encoding.UTF8.GetString(manifest));

                    case Serialization.Protocol.MessageManifestMode.None:
                    default:
                        return system.Serialization.Deserialize(messageProtocol.Message, messageProtocol.SerializerId);
                }
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Unimplemented deserialization of message with serializerId [{messageProtocol.SerializerId}]");
                throw;
            }
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        public static SerializedMessage Serialize(ActorSystem system, Address address, object message)
        {
            try
            {
                var objectType = message.GetType();
                var serializer = system.Serialization.FindSerializerForType(objectType);

                var serializedMsg = new SerializedMessage
                {
                    Message = serializer.ToBinaryWithAddress(address, message),
                    SerializerId = serializer.Identifier
                };

                if (serializer is SerializerWithStringManifest manifestSerializer)
                {
                    serializedMsg.ManifestMode = Serialization.Protocol.MessageManifestMode.WithStringManifest;
                    serializedMsg.MessageManifest = manifestSerializer.ManifestBytes(message);
                }
                else if (serializer.IncludeManifest)
                {
                    serializedMsg.ManifestMode = Serialization.Protocol.MessageManifestMode.IncludeManifest;
                    var typeKey = TypeSerializer.GetTypeKeyFromType(objectType);
                    serializedMsg.TypeHashCode = typeKey.HashCode;
                    serializedMsg.MessageManifest = typeKey.TypeName;
                }

                return serializedMsg;
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Cannot serialize object of type{message?.GetType().TypeQualifiedName()}");
                throw;
            }
        }
    }
}