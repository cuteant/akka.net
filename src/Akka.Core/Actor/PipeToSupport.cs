//-----------------------------------------------------------------------
// <copyright file="PipeToSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Util;

namespace Akka.Actor
{
    /// <summary>
    /// Creates the PipeTo pattern for automatically sending the results of completed tasks
    /// into the inbox of a designated Actor
    /// </summary>
    public static class PipeToSupport
    {
        /// <summary>
        /// Pipes the output of a Task directly to the <paramref name="recipient"/>'s mailbox once
        /// the task completes
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="taskToPipe">TBD</param>
        /// <param name="recipient">TBD</param>
        /// <param name="sender">TBD</param>
        /// <param name="success">TBD</param>
        /// <param name="failure">TBD</param>
        /// <returns>TBD</returns>
        public static Task PipeTo<T>(this Task<T> taskToPipe, ICanTell recipient, IActorRef sender = null, Func<T, object> success = null, Func<Exception, object> failure = null)
        {
            sender = sender ?? ActorRefs.NoSender;
            return taskToPipe.LinkOutcome(PipeToHelper<T>.PipeToContinuationAction,
                recipient, sender, success, failure,
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        sealed class PipeToHelper<T>
        {
            public static readonly Action<Task<T>, ICanTell, IActorRef, Func<T, object>, Func<Exception, object>> PipeToContinuationAction = PipeToContinuation;

            private static void PipeToContinuation(Task<T> tresult, ICanTell recipient, IActorRef sender, Func<T, object> success, Func<Exception, object> failure)
            {
                if (tresult.IsSuccessfully())
                {
                    recipient.Tell(success != null ? success(tresult.Result) : tresult.Result, sender);
                    return;
                }
                recipient.Tell(failure != null ? failure(tresult.Exception) : new Status.Failure(tresult.Exception), sender);
            }
        }

        /// <summary>
        /// Pipes the output of a Task directly to the <paramref name="recipient"/>'s mailbox once
        /// the task completes.  As this task has no result, only exceptions will be piped to the <paramref name="recipient"/>
        /// </summary>
        /// <param name="taskToPipe">TBD</param>
        /// <param name="recipient">TBD</param>
        /// <param name="sender">TBD</param>
        /// <param name="success">TBD</param>
        /// <param name="failure">TBD</param>
        /// <returns>TBD</returns>
        public static Task PipeTo(this Task taskToPipe, ICanTell recipient, IActorRef sender = null, Func<object> success = null, Func<Exception, object> failure = null)
        {
            sender = sender ?? ActorRefs.NoSender;
            return taskToPipe.LinkOutcome(PipeToContinuationAction,
                recipient, sender, success, failure,
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private static readonly Action<Task, ICanTell, IActorRef, Func<object>, Func<Exception, object>> PipeToContinuationAction = PipeToContinuation;
        private static void PipeToContinuation(Task tresult, ICanTell recipient, IActorRef sender, Func<object> success, Func<Exception, object> failure)
        {
            if (tresult.IsSuccessfully())
            {
                if (success != null) { recipient.Tell(success(), sender); }
                return;
            }
            recipient.Tell(failure != null ? failure(tresult.Exception) : new Status.Failure(tresult.Exception), sender);
        }
    }
}

