﻿// -----------------------------------------------------------------------
//  <copyright file="SourceRefSurrogate.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2019 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Util;

namespace Akka.Streams.Serialization
{
    internal sealed class SourceRefSurrogate : ISurrogate
    {
        public SourceRefSurrogate(Type eventType, string originPath)
        {
            EventType = eventType;
            OriginPath = originPath;
        }

        public Type EventType { get; }
        public string OriginPath { get; }

        public ISurrogated FromSurrogate(ActorSystem system) =>
            SerializationTools.ToSourceRefImpl((ExtendedActorSystem)system, EventType, OriginPath);
    }
}