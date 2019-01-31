//-----------------------------------------------------------------------
// <copyright file="MessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Akka.Actor;
using SerializedMessage = Akka.Serialization.Protocol.Payload;
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

        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage Serialize(ActorSystem system, Address address, object message)
        {
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
    }
}