//-----------------------------------------------------------------------
// <copyright file="FutureTimeoutSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Pattern
{
    /// <summary>
    /// Used to help make it easier to schedule timeouts in conjunction
    /// with the built-in <see cref="IScheduler"/>
    /// </summary>
    public static partial class FutureTimeoutSupport
    {
        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<T>(TimeSpan duration, IScheduler scheduler, Func<Task<T>> value)
            => After(duration, scheduler, Runnable.CreateTask(value));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="task">The task we're going to wrap.</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<T>(TimeSpan duration, IScheduler scheduler, IRunnableTask<T> task)
        {
            if (duration < TimeSpan.MaxValue && duration.Ticks < 1)
            {
                // no need to schedule
                try
                {
                    return task.Run();
                }
                catch (Exception ex)
                {
                    return TaskEx.FromException<T>(ex);
                }
            }

            var tcs = new TaskCompletionSource<T>();
            scheduler.Advanced.ScheduleOnce(duration, Helper<T>.LinkOutcomeAction, task, tcs);
            return tcs.Task;
        }

        private sealed class Helper<T>
        {
            public static readonly Action<IRunnableTask<T>, TaskCompletionSource<T>> LinkOutcomeAction = LinkOutcome;
            private static void LinkOutcome(IRunnableTask<T> task, TaskCompletionSource<T> tcs)
            {
                try
                {
                    task.Run().ContinueWith(LinkOutcomeContinuationAction, tcs);
                }
                catch (Exception ex)
                {
                    // in case the value() function faults
                    tcs.TrySetUnwrappedException(ex);
                }
            }

            private static readonly Action<Task<T>, object> LinkOutcomeContinuationAction = LinkOutcomeContinuation;
            private static void LinkOutcomeContinuation(Task<T> tr, object state)
            {
                var tcs = (TaskCompletionSource<T>)state;
                try
                {
                    if (tr.IsSuccessfully())
                    {
                        tcs.TrySetResult(tr.Result);
                    }
                    else
                    {
                        tcs.TrySetException(tr.Exception.InnerException);
                    }
                }
                catch (AggregateException ex)
                {
                    // in case the task faults
                    tcs.TrySetException(ex.InnerExceptions);
                }
            }
        }
    }
}
