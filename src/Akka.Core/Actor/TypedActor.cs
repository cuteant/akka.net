//-----------------------------------------------------------------------
// <copyright file="TypedActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;

namespace Akka.Actor
{
    /// <summary>
    ///     Class TypedActor.
    /// </summary>
    [Obsolete("TypedActor in its current shape will be removed in v1.5")]
    public abstract class TypedActor : ActorBase
    {
        /// <summary>
        ///     Processor for user defined messages.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>TBD</returns>
        protected sealed override bool Receive(object message)
        {
            MethodInfo method = GetType().GetMethod("Handle", new[] { message.GetType() });
            if (method is null)
            {
                return false;
            }

            method.Invoke(this, new[] { message });
            return true;
        }
    }
}
