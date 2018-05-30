//-----------------------------------------------------------------------
// <copyright file="RealMessageEnvelope.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Serialization.Formatters;
using MessagePack;

namespace Akka.TestKit
{
    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public class RealMessageEnvelope : MessageEnvelope
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <param name="sender">TBD</param>
        [SerializationConstructor]
        public RealMessageEnvelope(object message, IActorRef sender)
        {
            Message = message;
            Sender = sender;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        [MessagePackFormatter(typeof(WrappedPayloadFormatter))]
        public override object Message { get; }
        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public override IActorRef Sender { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return "<" + (Message ?? "null") + "> from " + (Sender == ActorRefs.NoSender ? "NoSender" : Sender.ToString());
        }
    }
}
