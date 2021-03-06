﻿//-----------------------------------------------------------------------
// <copyright file="ResizablePoolActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.Routing
{
    /// <summary>
    /// INTERNAL API.
    /// 
    /// Defines <see cref="Pool"/> routers who can resize the number of routees
    /// they use based on a defined <see cref="Resizer"/>
    /// </summary>
    internal class ResizablePoolActor : RouterPoolActor
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="supervisorStrategy">TBD</param>
        public ResizablePoolActor(SupervisorStrategy supervisorStrategy) : base(supervisorStrategy)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="ActorInitializationException">TBD</exception>
        protected ResizablePoolCell ResizerCell
        {
            get
            {
                var context = Context;
                var resizablePoolCell = context as ResizablePoolCell;
                if (resizablePoolCell is null) { AkkaThrowHelper.ThrowActorInitializationException_ResizablePoolActor(context); }
                return resizablePoolCell;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override void OnReceive(object message)
        {
            if (message is Resize && ResizerCell is object)
            {
                ResizerCell.Resize(false);
            }
            else
            {
                base.OnReceive(message);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void StopIfAllRouteesRemoved()
        {
            //we don't care if routees are removed
        }
    }

    /// <summary>
    /// Command used to resize a <see cref="ResizablePoolActor"/>
    /// </summary>
    public class Resize : RouterManagementMessage
    {
    }
}
