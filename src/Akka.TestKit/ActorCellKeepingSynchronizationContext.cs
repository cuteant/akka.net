﻿//-----------------------------------------------------------------------
// <copyright file="ActorCellKeepingSynchronizationContext.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Util;

namespace Akka.TestKit
{
    /// <summary>
    /// TBD
    /// </summary>
    class ActorCellKeepingSynchronizationContext : SynchronizationContext
    {
        private readonly ActorCell _cell;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="cell">TBD</param>
        public ActorCellKeepingSynchronizationContext(ActorCell cell)
        {
            _cell = cell;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="d">TBD</param>
        /// <param name="state">TBD</param>
        public override void Post(SendOrPostCallback d, object state)
        {
#if UNSAFE_THREADING
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
#else
            ThreadPool.QueueUserWorkItem(_ =>
#endif
            {
                var oldCell = InternalCurrentActorCellKeeper.Current;
                var oldContext = Current;
                SetSynchronizationContext(this);
                InternalCurrentActorCellKeeper.Current = _cell;
                
                try
                {
                    d(state);
                }
                finally
                {
                    InternalCurrentActorCellKeeper.Current = oldCell;
                    SetSynchronizationContext(oldContext);
                }
            }, state);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="d">TBD</param>
        /// <param name="state">TBD</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            var tcs = new TaskCompletionSource<int>();
            Post(_ =>
            {
                try
                {
                    d(state);
                    tcs.SetResult(0);
                }
                catch (Exception e)
                {
                    tcs.TrySetUnwrappedException(e);
                }
            }, state);
            tcs.Task.Wait();
        }
    }
} 
