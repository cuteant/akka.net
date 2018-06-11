//-----------------------------------------------------------------------
// <copyright file="MessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Text;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt.Reflection;
using SerializedMessage = Akka.Remote.Serialization.Protocol.Payload;
#if DEBUG
using System;
using Akka.Util;
using Microsoft.Extensions.Logging;
#endif

namespace Akka.Remote
{
    /// <summary>Class MessageSerializer.</summary>
    internal static class MessageSerializer
    {
#if DEBUG
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(MessageSerializer));
#endif

        /// <summary>Deserializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        public static object Deserialize(ActorSystem system, SerializedMessage messageProtocol)
        {
#if DEBUG
            try
            {
#endif
                switch (messageProtocol.ManifestMode)
                {
                    case Serialization.Protocol.MessageManifestMode.IncludeManifest:
                        return system.Serialization.Deserialize(messageProtocol.Message, messageProtocol.SerializerId, new TypeKey(messageProtocol.TypeHashCode, messageProtocol.MessageManifest));

                    case Serialization.Protocol.MessageManifestMode.WithStringManifest:
                        return system.Serialization.Deserialize(messageProtocol.Message, messageProtocol.SerializerId, Encoding.UTF8.GetString(messageProtocol.MessageManifest));

                    case Serialization.Protocol.MessageManifestMode.None:
                    default:
                        return system.Serialization.Deserialize(messageProtocol.Message, messageProtocol.SerializerId);
                }
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Unimplemented deserialization of message with serializerId [{messageProtocol.SerializerId}]");
                throw;
            }
#endif
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        public static SerializedMessage Serialize(ActorSystem system, Address address, object message)
        {
#if DEBUG
            try
            {
#endif
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
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Cannot serialize object of type{message?.GetType().TypeQualifiedName()}");
                throw;
            }
#endif
        }
    }
}