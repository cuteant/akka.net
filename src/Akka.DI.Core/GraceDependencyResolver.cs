using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.DI.Core;
using CuteAnt.Reflection;
using Grace.DependencyInjection;

namespace Akka.DI.Grace
{
    /// <summary>
    /// Provides services to the <see cref="ActorSystem "/> extension system
    /// used to create actors using the Grace IoC container.
    /// </summary>
    public class GraceDependencyResolver : IDependencyResolver
    {
        private readonly IInjectionScope _container;
        private readonly ActorSystem _system;
        private readonly ConditionalWeakTable<ActorBase, IInjectionScope> _references;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraceDependencyResolver"/> class.
        /// </summary>
        /// <param name="container">The container used to resolve references</param>
        /// <param name="system">The actor system to plug into</param>
        /// <exception cref="ArgumentNullException">
        /// Either the <paramref name="container"/> or the <paramref name="system"/> was null.
        /// </exception>
        public GraceDependencyResolver(IInjectionScope container, ActorSystem system)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _system.AddDependencyResolver(this);
            _references = new ConditionalWeakTable<ActorBase, IInjectionScope>();
        }

        /// <summary>
        /// Retrieves an actor's type with the specified name
        /// </summary>
        /// <param name="actorName">The name of the actor to retrieve</param>
        /// <returns>The type with the specified actor name</returns>
        public Type GetType(string actorName) => TypeUtils.ResolveType(actorName);

        /// <summary>
        /// Creates a delegate factory used to create actors based on their type
        /// </summary>
        /// <param name="actorType">The type of actor that the factory builds</param>
        /// <returns>A delegate factory used to create actors</returns>
        public Func<ActorBase> CreateActorFactory(Type actorType) => () => CreateActor(actorType);

        private ActorBase CreateActor(Type actorType)
        {
            var nestedContainer = _container.CreateChildScope();
            var actor = (ActorBase)nestedContainer.Locate(actorType);
            _references.Add(actor, nestedContainer);
            return actor;
        }

        /// <summary>
        /// Used to register the configuration for an actor of the specified type <typeparamref name="TActor"/>
        /// </summary>
        /// <typeparam name="TActor">The type of actor the configuration is based</typeparam>
        /// <returns>The configuration object for the given actor type</returns>
        public Props Create<TActor>() where TActor : ActorBase => Create(typeof(TActor));

        /// <summary>
        /// Used to register the configuration for an actor of the specified type <paramref name="actorType"/> 
        /// </summary>
        /// <param name="actorType">The <see cref="Type"/> of actor the configuration is based</param>
        /// <returns>The configuration object for the given actor type</returns>
        public virtual Props Create(Type actorType) => _system.GetExtension<DIExt>().Props(actorType);

        /// <summary>
        /// Signals the container to release it's reference to the actor.
        /// </summary>
        /// <param name="actor">The actor to remove from the container</param>
        public void Release(ActorBase actor)
        {
            if (_references.TryGetValue(actor, out IInjectionScope nestedContainer))
            {
                nestedContainer.Dispose();
                _references.Remove(actor);
            }
        }
    }
}
