using System.Runtime.CompilerServices;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
using ExternalSerializedMessage = Akka.Serialization.Protocol.ExternalPayload;
#if DEBUG
using System;
using Akka.Util;
using Microsoft.Extensions.Logging;
#endif

namespace Akka.Actor
{
    /// <summary>This class contains extension methods used for working with <see cref="ExtendedActorSystem"/>s.</summary>
    public static class ExtendedActorSystemExtensions
    {
#if DEBUG
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(ExtendedActorSystemExtensions));
#endif

        /// <summary>Deserializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(this ExtendedActorSystem system, in SerializedMessage messageProtocol)
        {
#if DEBUG
            try
            {
#endif
                return system.Serialization.Deserialize(messageProtocol);
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Unimplemented deserialization of message with serializerId [{messageProtocol.SerializerId}]");
                throw;
            }
#endif
        }

        /// <summary>Deserializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(this ExtendedActorSystem system, in ExternalSerializedMessage messageProtocol)
        {
#if DEBUG
            try
            {
#endif
                return system.Serialization.Deserialize(messageProtocol);
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Unimplemented deserialization of message with serializerId [{messageProtocol.Identifier}]");
                throw;
            }
#endif
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static byte[] Serialize(this ExtendedActorSystem system, object message)
        {
#if DEBUG
            try
            {
#endif
                return system.Serialization.FindSerializerFor(message).ToBinary(message);
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Cannot serialize object of type{message?.GetType().TypeQualifiedName()}");
                throw;
            }
#endif
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static byte[] Serialize(this ExtendedActorSystem system, object message, string defaultSerializerName)
        {
#if DEBUG
            try
            {
#endif
                return system.Serialization.FindSerializerFor(message, defaultSerializerName).ToBinary(message);
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Cannot serialize object of type{message?.GetType().TypeQualifiedName()}");
                throw;
            }
#endif
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="message">The message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage SerializeMessage(this ExtendedActorSystem system, object message)
        {
#if DEBUG
            try
            {
#endif
                return system.Serialization.FindSerializerFor(message).ToPayload(message);
#if DEBUG
            }
            catch (Exception exc)
            {
                s_logger.LogWarning(exc, $"Cannot serialize object of type{message?.GetType().TypeQualifiedName()}");
                throw;
            }
#endif
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage SerializeMessage(this ExtendedActorSystem system, object message, string defaultSerializerName)
        {
#if DEBUG
            try
            {
#endif
                return system.Serialization.FindSerializerFor(message, defaultSerializerName).ToPayload(message);
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