﻿//-----------------------------------------------------------------------
// <copyright file="DIActorContextAdapter.cs" company="Akka.NET Project">
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
    /// objects using the dependency injection (DI) extension using a given actor context.
    /// </summary>
    public class DIActorContextAdapter
    {
        readonly DIExt _producer;
        readonly IActorContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DIActorContextAdapter"/> class.
        /// </summary>
        /// <param name="context">The actor context associated with a system that contains the DI extension.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="context"/> is undefined.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown when the Dependency Resolver has not been configured in the <see cref="ActorSystem" />.
        /// </exception>
        public DIActorContextAdapter(IActorContext context)
        {
            if (context is null) { ThrowHelper.ThrowArgumentNullException_Context(); }
            _context = context;
            _producer = context.System.GetExtension<DIExt>();
            if (_producer is null) ThrowHelper.ThrowInvalidOperationException_TheDependencyResolverHasNotBeenConfiguredYet();
        }

        /// <summary>
        /// Obsolete. Use <see cref="Props(Type)"/> or <see cref="Props{TActor}"/> methods for actor creation. This method will be removed in future versions.
        /// </summary>
        /// <typeparam name="TActor">N/A</typeparam>
        /// <param name="name">N/A</param>
        /// <returns>N/A</returns>
        [Obsolete("Use Props methods for actor creation. This method will be removed in future versions")]
        public IActorRef ActorOf<TActor>(string name = null) where TActor : ActorBase => _context.ActorOf(_producer.Props(typeof(TActor)), name);

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
