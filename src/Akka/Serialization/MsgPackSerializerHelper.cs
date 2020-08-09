using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Serialization.Formatters;
using Akka.Serialization.Resolvers;
using MessagePack;
using MessagePack.ImmutableCollection;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
using ExternalSerializedMessage = Akka.Serialization.Protocol.ExternalPayload;
#if DEBUG
using System;
using Akka.Util;
using Microsoft.Extensions.Logging;
#endif

namespace Akka.Serialization
{
    public static class MsgPackSerializerHelper
    {
#if DEBUG
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(ExtendedActorSystemExtensions));
#endif
        internal static IFormatterResolver DefaultResolver;

        static MsgPackSerializerHelper()
        {
            MessagePackBinary.Shared = CuteAnt.Buffers.BufferManager.Shared;

            MessagePackStandardResolver.RegisterTypelessObjectResolver(AkkaTypelessObjectResolver.Instance, AkkaTypelessFormatter.Instance);

            MessagePackStandardResolver.Register(
                SystemMessageResolver.Instance,
                MiscMessageResolver.Instance,
                AkkaResolverCore.Instance,

                ImmutableCollectionResolver.Instance,

                HyperionExceptionResolver2.Instance,

                HyperionExpressionResolver2.Instance,

                AkkaHyperionResolver.Instance
            );
        }

        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static ExtendedActorSystem GetActorSystem(this IFormatterResolver formatterResolver)
            => ((IFormatterResolverContext<ExtendedActorSystem>)formatterResolver).Value;

        /// <summary>Deserializes the specified message.</summary>
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(this IFormatterResolver formatterResolver, in SerializedMessage messageProtocol)
        {
#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
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
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="messageProtocol">The message protocol.</param>
        /// <returns>System.Object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(this IFormatterResolver formatterResolver, in ExternalSerializedMessage messageProtocol)
        {
#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
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
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="message">The message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Serialize(this IFormatterResolver formatterResolver, object message)
        {
#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
                var serializer = system.Serialization.FindSerializerForType(message.GetType());
                return serializer.ToBinary(message);
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
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Serialize(this IFormatterResolver formatterResolver, object message, string defaultSerializerName)
        {
#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
                var serializer = system.Serialization.FindSerializerForType(message.GetType(), defaultSerializerName);
                return serializer.ToBinary(message);
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
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="message">The message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage SerializeMessage(this IFormatterResolver formatterResolver, object message)
        {
#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
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
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage SerializeMessage(this IFormatterResolver formatterResolver, object message, string defaultSerializerName)
        {
#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
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
    }
}
