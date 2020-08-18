﻿//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using CuteAnt.Reflection;

namespace Akka.DI.Core
{
    /// <summary>
    /// This class contains extension methods used to simplify working with dependency injection (DI) inside an <see cref="ActorSystem"/>.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Registers a dependency resolver with a given actor system.
        /// </summary>
        /// <param name="system">The actor system in which to register the given dependency resolver.</param>
        /// <param name="dependencyResolver">The dependency resolver being registered to the actor system.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when either the specified <paramref name="system"/> or the specified <paramref name="dependencyResolver"/> is undefined.
        /// </exception>
        public static void AddDependencyResolver(this ActorSystem system, IDependencyResolver dependencyResolver)
        {
            if (system is null) ThrowHelper.ThrowArgumentNullException_RequiresSystem();
            if (dependencyResolver is null) ThrowHelper.ThrowArgumentNullException_RequiresDR();
            system.RegisterExtension(DIExtension.DIExtensionProvider);
            DIExtension.DIExtensionProvider.Get(system).Initialize(dependencyResolver);
        }

        /// <summary>
        /// Creates an adapter used to generate <see cref="Akka.Actor.Props"/> configuration objects using the DI extension using a given actor system.
        /// </summary>
        /// <param name="system">The actor system that contains the DI extension.</param>
        /// <returns>An adapter used to generate <see cref="Akka.Actor.Props"/> configuration objects using the DI extension.</returns>
        public static DIActorSystemAdapter DI(this ActorSystem system) => new DIActorSystemAdapter(system);

        /// <summary>
        /// Creates an adapter used to generate <see cref="Akka.Actor.Props"/> configuration objects using the DI extension using a given actor context.
        /// </summary>
        /// <param name="context">The actor context associated with a system that contains the DI extension.</param>
        /// <returns>An adapter used to generate <see cref="Akka.Actor.Props"/> configuration objects using the DI extension.</returns>
        public static DIActorContextAdapter DI(this IActorContext context) => new DIActorContextAdapter(context);

        /// <summary>
        /// Retrieves the <see cref="Type"/> with a given name from the current <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="typeName">The string representation of the type to retrieve.</param>
        /// <returns>The <see cref="Type"/> with the given name.</returns>
        public static Type GetTypeValue(this string typeName) => TypeUtils.TryResolveType(typeName, out var type) ? type : null;
    }
}
