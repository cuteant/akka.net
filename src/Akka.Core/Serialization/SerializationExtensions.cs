using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Serialization.Protocol;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
#if DEBUG
using System;
using Akka.Util;
using Microsoft.Extensions.Logging;
#endif

namespace Akka.Serialization
{
    public static class SerializationExtensions
    {
#if DEBUG
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(SerializationExtensions));
#endif

        /// <summary>Serializes the specified message.</summary>
        /// <param name="serialization">The serialization.</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this Serialization serialization, object message)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = serialization.FindSerializerForType(message.GetType());
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
        /// <param name="serialization">The serialization.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this Serialization serialization, object message, string defaultSerializerName)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = serialization.FindSerializerForType(message.GetType(), defaultSerializerName);
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
        /// <param name="serialization">The serialization.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this Serialization serialization, Address address, object message)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = serialization.FindSerializerForType(message.GetType());
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
        /// <param name="serialization">The serialization.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this Serialization serialization, Address address, object message, string defaultSerializerName)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = serialization.FindSerializerForType(message.GetType(), defaultSerializerName);
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
        /// <param name="serialization">The serialization.</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExternalPayload ToExternalPayload(this Serialization serialization, object message)
        {
            if (null == message) { return ExternalPayload.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = serialization.FindSerializerForType(message.GetType());
                return serializer.ToExternalPayload(message);
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
        /// <param name="serialization">The serialization.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExternalPayload ToExternalPayload(this Serialization serialization, object message, string defaultSerializerName)
        {
            if (null == message) { return ExternalPayload.Null; }

#if DEBUG
            try
            {
#endif
                var serializer = serialization.FindSerializerForType(message.GetType(), defaultSerializerName);
                return serializer.ToExternalPayload(message);
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
