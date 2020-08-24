using System;
using Akka.DI.Core;
using Akka.DI.Grace;
using Grace.DependencyInjection;

namespace Akka.Actor
{
    /// <summary>
    /// Extension methods for <see cref="ActorSystem"/> to configure Autofac
    /// </summary>
    public static class GraceActorSystemExtensions
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GraceDependencyResolver"/> class
        /// associated with the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="system">The actor system to plug into</param>
        /// <param name="container">The container used to resolve references</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="container"/> parameter is null.
        /// </exception>
        public static ActorSystem UseGrace(this ActorSystem system, IInjectionScope container)
        {
            UseGrace(system, container, out _);
            return system;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GraceDependencyResolver"/> class
        /// associated with the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="system">The actor system to plug into</param>
        /// <param name="container">The container used to resolve references</param>
        /// <param name="dependencyResolver">The Autofac dependency resolver instance created</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="container"/> parameter is null.
        /// </exception>
        public static ActorSystem UseGrace(this ActorSystem system, IInjectionScope container, out IDependencyResolver dependencyResolver)
        {
            if (container is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.container); }

            dependencyResolver = new GraceDependencyResolver(container, system);
            return system;
        }
    }
}
