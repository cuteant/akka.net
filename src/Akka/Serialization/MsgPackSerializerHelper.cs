using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Serialization.Formatters;
using Akka.Serialization.Resolvers;
using CuteAnt;
using MessagePack;
using MessagePack.ImmutableCollection;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
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
        private static readonly ILogger s_logger = TraceLogger.GetLogger(typeof(ActorSystemExtensions));
#endif
        internal static IFormatterResolver DefaultResolver;

        static MsgPackSerializerHelper()
        {
            MessagePackStandardResolver.RegisterTypelessObjectResolver(AkkaTypelessObjectResolver.Instance, AkkaTypelessFormatter.Instance);

            MessagePackStandardResolver.Register(
                AkkaResolverCore.Instance,

                ImmutableCollectionResolver.Instance,

                HyperionExceptionResolver2.Instance,

                HyperionExpressionResolver2.Instance,

                AkkaHyperionResolver.Instance
            );
        }

        [MethodImpl(InlineMethod.Value)]
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

        /// <summary>Serializes the specified message.</summary>
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this IFormatterResolver formatterResolver, object message)
        {
            if (null == message) { return SerializedMessage.Null; }

            var system = formatterResolver.GetActorSystem();
            var serializer = system.Serialization.FindSerializerForType(message.GetType());
            return serializer.ToPayload(message);
        }

        /// <summary>Serializes the specified message.</summary>
        /// <param name="formatterResolver">The formatter resolver.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(this IFormatterResolver formatterResolver, Address address, object message)
        {
            if (null == message) { return SerializedMessage.Null; }

#if DEBUG
            try
            {
#endif
                var system = formatterResolver.GetActorSystem();
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
    }
}
