using System.Runtime.CompilerServices;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
using ExternalSerializedMessage = Akka.Serialization.Protocol.ExternalPayload;
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
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static byte[] Serialize(this Serialization serialization, object message)
        {
#if DEBUG
            try
            {
#endif
                return serialization.FindSerializerFor(message).ToBinary(message);
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
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static byte[] Serialize(this Serialization serialization, object message, string defaultSerializerName)
        {
#if DEBUG
            try
            {
#endif
                return serialization.FindSerializerFor(message, defaultSerializerName).ToBinary(message);
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
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static SerializedMessage SerializeMessage(this Serialization serialization, object message)
        {
#if DEBUG
            try
            {
#endif
                return serialization.FindSerializerFor(message).ToPayload(message);
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
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static SerializedMessage SerializeMessage(this Serialization serialization, object message, string defaultSerializerName)
        {
#if DEBUG
            try
            {
#endif
                return serialization.FindSerializerFor(message, defaultSerializerName).ToPayload(message);
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
