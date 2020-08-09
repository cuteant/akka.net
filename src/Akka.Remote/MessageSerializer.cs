//-----------------------------------------------------------------------
// <copyright file="MessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Akka.Actor;
using SerializedMessage = Akka.Serialization.Protocol.Payload;

namespace Akka.Remote
{
    /// <summary>
    /// INTERNAL API.
    ///
    /// MessageSerializer is a helper for serializing and deserialize messages.
    /// </summary>
    internal static class MessageSerializer
    {
        /// <summary>Serializes the specified message.</summary>
        /// <param name="system">The system.</param>
        /// <param name="address">TBD</param>
        /// <param name="message">The message.</param>
        /// <returns>SerializedMessage.</returns>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static SerializedMessage Serialize(ExtendedActorSystem system, Address address, object message)
        {
            return system.Serialization.SerializeMessageWithTransport(message);
        }
    }
}