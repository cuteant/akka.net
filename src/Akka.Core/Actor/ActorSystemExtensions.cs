using System.Runtime.CompilerServices;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
#if DEBUG
using System;
using Akka.Util;
using Microsoft.Extensions.Logging;
#endif

namespace Akka.Actor
{
    /// <summary>This class contains extension methods used for working with <see cref="ActorSystem"/>s.</summary>
    public static class ActorSystemExtensions
    {
#if DEBUG
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(ActorSystemExtensions));
#endif

        /// <summary>Deserializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(this ActorSystem system, in SerializedMessage messageProtocol)
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

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this ActorSystem system, object message)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = system.Serialization.FindSerializerForType(message.GetType());
                return serializer.ToPayload(message);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this ActorSystem system, object message, string defaultSerializerName)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = system.Serialization.FindSerializerForType(message.GetType(), defaultSerializerName);
                return serializer.ToPayload(message);
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
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this ActorSystem system, Address address, object message)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = system.Serialization.FindSerializerForType(message.GetType());
                return serializer.ToPayloadWithAddress(address, message);
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
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this ActorSystem system, Address address, object message, string defaultSerializerName)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = system.Serialization.FindSerializerForType(message.GetType(), defaultSerializerName);
                return serializer.ToPayloadWithAddress(address, message);
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