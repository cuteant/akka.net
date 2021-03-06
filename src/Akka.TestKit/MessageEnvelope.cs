﻿//-----------------------------------------------------------------------
// <copyright file="MessageEnvelope.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using MessagePack;

namespace Akka.TestKit
{
    /// <summary>
    /// TBD
    /// </summary>
    public abstract class MessageEnvelope : IObjectReferences   //this is called Message in Akka JVM
    {
        /// <summary>
        /// TBD
        /// </summary>
        public abstract object Message { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public abstract IActorRef Sender { get; }
    }
}
