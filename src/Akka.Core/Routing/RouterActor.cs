//-----------------------------------------------------------------------
// <copyright file="RouterActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;

namespace Akka.Routing
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal class RouterActor : UntypedActor
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="ActorInitializationException">TBD</exception>
        protected RoutedActorCell Cell
        {
            get
            {
                var context = Context;
                var routedActorCell = context as RoutedActorCell;
                if (routedActorCell is null) { AkkaThrowHelper.ThrowActorInitializationException_RouterActor(context); }
                return routedActorCell;
            }
        }

        private IActorRef RoutingLogicController
        {
            get
            {
                var context = Context;
                var cell = Cell;
                return context.ActorOf(cell.RouterConfig.RoutingLogicController(cell.Router.RoutingLogic).
                    WithDispatcher(context.Props.Dispatcher), "routingLogicController");
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case GetRoutees _:
                    Sender.Tell(new Routees(Cell.Router.Routees));
                    break;
                case AddRoutee addRoutee:
                    Cell.AddRoutee(addRoutee.Routee);
                    break;
                case RemoveRoutee removeRoutee:
                    Cell.RemoveRoutee(removeRoutee.Routee, stopChild: true);
                    StopIfAllRouteesRemoved();
                    break;
                case Terminated terminated:
                    Cell.RemoveRoutee(new ActorRefRoutee(terminated.ActorRef), stopChild: false);
                    StopIfAllRouteesRemoved();
                    break;
                default:
                    RoutingLogicController?.Forward(message);
                    break;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected virtual void StopIfAllRouteesRemoved()
        {
            var cell = Cell;
            if (!cell.Router.Routees.Any() && cell.RouterConfig.StopRouterWhenAllRouteesRemoved)
            {
                Context.Stop(Self);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="cause">TBD</param>
        /// <param name="message">TBD</param>
        protected override void PreRestart(Exception cause, object message)
        {
            //do not scrap children
        }
    }
}

