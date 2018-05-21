//-----------------------------------------------------------------------
// <copyright file="DIActorSystemAdapter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;

namespace Akka.DI.Core
{
    /// <summary>
    /// This class represents an adapter used to generate <see cref="Akka.Actor.Props"/> configuration
    /// objects using the dependency injection (DI) extension using a given actor system.
    /// </summary>
    public class DIActorSystemAdapter
    {
        readonly DIExt _producer;
        readonly ActorSystem _system;

        /// <summary>
        /// Initializes a new instance of the <see cref="DIActorSystemAdapter"/> class.
        /// </summary>
        /// <param name="system">The actor system that contains the DI extension.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="system"/> is undefined.
        /// </exception>
        public DIActorSystemAdapter(ActorSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system), $"DIActorSystemAdapter requires {nameof(system)} to be provided");
            _producer = system.GetExtension<DIExt>();
        }

        /// <summary>
        /// Creates a <see cref="Akka.Actor.Props"/> configuration object for a given actor type.
        /// </summary>
        /// <param name="actorType">The actor type for which to create the <see cref="Akka.Actor.Props"/> configuration.</param>
        /// <returns>A <see cref="Akka.Actor.Props"/> configuration object for the given actor type.</returns>
        public Props Props(Type actorType) => _producer.Props(actorType);

        /// <summary>
        /// Creates a <see cref="Akka.Actor.Props"/> configuration object for a given actor type.
        /// </summary>
        /// <typeparam name="TActor">The actor type for which to create the <see cref="Akka.Actor.Props"/> configuration.</typeparam>
        /// <returns>A <see cref="Akka.Actor.Props"/> configuration object for the given actor type.</returns>
        public Props Props<TActor>() where TActor : ActorBase => Props(typeof(TActor));
    }
}
