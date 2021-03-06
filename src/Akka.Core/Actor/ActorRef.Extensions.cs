﻿//-----------------------------------------------------------------------
// <copyright file="ActorRef.Extensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka.Actor
{
    /// <summary>
    /// This class contains extension methods used for working with <see cref="IActorRef">ActorRefs</see>.
    /// </summary>
    public static class ActorRefExtensions
    {
        /// <summary>
        /// Determines if the specified <paramref name="actorRef"/> is valid.
        /// An <paramref name="actorRef"/> is thought to be invalid if it's one of the following:
        ///    <see langword="null"/>, <see cref="Nobody"/>, and <see cref="DeadLetterActorRef"/>
        /// </summary>
        /// <param name="actorRef">The actor that is being tested.</param>
        /// <returns><c>true</c> if the <paramref name="actorRef"/> is valid; otherwise <c>false</c>.</returns>
        public static bool IsNobody(this IActorRef actorRef)
        {
            //return actorRef is null || actorRef is Nobody || actorRef is DeadLetterActorRef;
            switch (actorRef)
            {
                case null:
                case Nobody _:
                case DeadLetterActorRef _:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the <paramref name="actorRef"/>'s value if it's not <see langword="null"/>, <see cref="Nobody"/>, 
        /// or <see cref="DeadLetterActorRef"/>. Otherwise return the result of evaluating `elseValue`.
        /// </summary>
        /// <param name="actorRef">The actor that is being tested.</param>
        /// <param name="elseValue">TBD</param>
        public static IActorRef GetOrElse(this IActorRef actorRef, Func<IActorRef> elseValue)
        {
            return actorRef.IsNobody() ? elseValue() : actorRef;
        }
    }

    /// <summary>ActorRefComparer</summary>
    public sealed class ActorRefComparer : System.Collections.Generic.IEqualityComparer<IActorRef>
    {
        /// <summary>ActorRefComparer.Instance</summary>
        public static readonly ActorRefComparer Instance = new ActorRefComparer();

        /// <summary>Determines whether the specified <see cref="IActorRef"/>s are equal.</summary>
        public bool Equals(IActorRef x, IActorRef y)
        {
            if (ReferenceEquals(x, y)) { return true; }
            if (x is null/* || y is null*/) { return false; }
            return x.Equals(y);
        }

        /// <summary>Returns a hash code for the specified <see cref="IActorRef"/>.</summary>
        public int GetHashCode(IActorRef obj) => obj.GetHashCode();
    }
}

