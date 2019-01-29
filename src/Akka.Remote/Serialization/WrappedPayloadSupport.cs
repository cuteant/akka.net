//-----------------------------------------------------------------------
// <copyright file="WrappedPayloadSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Akka.Actor;
using SerializedMessage = Akka.Serialization.Protocol.Payload;

namespace Akka.Remote.Serialization
{
    internal static class WrappedPayloadSupport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializedMessage PayloadToProto(ActorSystem system, object payload)
        {
            if (null == payload) { return SerializedMessage.Null; }

            var serializer = system.Serialization.FindSerializerForType(payload.GetType());
            return serializer.ToPayload(payload);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object PayloadFrom(ActorSystem system, in SerializedMessage payload)
        {
            return system.Serialization.Deserialize(payload);
        }
    }
}
