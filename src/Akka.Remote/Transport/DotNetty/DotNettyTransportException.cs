//-----------------------------------------------------------------------
// <copyright file="DotNettyTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Akka.Remote.Transport.DotNetty
{
    internal sealed class DotNettyTransportException : RemoteTransportException
    {
        /// <summary>Initializes a new instance of the <see cref="DotNettyTransportException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public DotNettyTransportException(string message, Exception cause = null)
            : base(message, cause) { }

#if SERIALIZATION
        /// <summary>Initializes a new instance of the <see cref="DotNettyTransportException"/> class.</summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the
        /// exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source
        /// or destination.</param>
        private DotNettyTransportException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif
    }
}