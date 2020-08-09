// -----------------------------------------------------------------------
//  <copyright file="SerializationTools.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2019 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Streams.Implementation.StreamRef;
using Akka.Streams.Serialization.Protocol;
using Akka.Util;

namespace Akka.Streams.Serialization
{
    internal static class SerializationTools
    {
        public static SourceRef ToSourceRef(SourceRefImpl sourceRef)
        {
            return new SourceRef(new ActorRef(Akka.Serialization.Serialization.SerializedActorPath(sourceRef.InitialPartnerRef)), sourceRef.EventType);
        }

        public static ISurrogate ToSurrogate(SourceRefImpl sourceRef)
        {
            var srcRef = ToSourceRef(sourceRef);
            return new SourceRefSurrogate(srcRef.EventType, srcRef.OriginRef.Path);
        }

        public static SourceRefImpl ToSourceRefImpl(ExtendedActorSystem system, Type eventType, string originPath)
        {
            var originRef = system.Provider.ResolveActorRef(originPath);

            return SourceRefImpl.Create(eventType, originRef);
        }
    }
}