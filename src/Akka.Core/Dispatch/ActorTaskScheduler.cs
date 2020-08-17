//-----------------------------------------------------------------------
// <copyright file="ActorTaskScheduler.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util;

namespace Akka.Dispatch
{
    /// <summary>
    /// TBD
    /// </summary>
    public partial class ActorTaskScheduler : TaskScheduler
    {
        private readonly ActorCell _actorCell;
        /// <summary>
        /// TBD
        /// </summary>
        public object CurrentMessage { get; internal set; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actorCell">TBD</param>
        internal ActorTaskScheduler(ActorCell actorCell)
        {
            _actorCell = actorCell;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override int MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="task">TBD</param>
        protected override void QueueTask(Task task)
        {
            if ((task.CreationOptions & TaskCreationOptions.LongRunning) == TaskCreationOptions.LongRunning)
            {
                // Executing a LongRunning task in an ActorTaskScheduler is bad practice, it will potentially
                // hang the actor and starve the ThreadPool

                // The best thing we can do here is force a rescheduling to at least not execute the task inline.
                ScheduleTask(task);
                return;
            }

            // Schedule the task execution, run inline if we are already in the actor context.
            if (ActorCell.Current == _actorCell)
            {
                TryExecuteTask(task);
            }
            else
            {
                ScheduleTask(task);
            }
        }

        private void ScheduleTask(Task task)
        {
            //we are in a max concurrency level 1 scheduler. reading CurrentMessage should be OK
            _actorCell.SendSystemMessage(new ActorTaskSchedulerMessage(this, task, CurrentMessage));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="task">TBD</param>
        internal void ExecuteTask(Task task)
        {
            TryExecuteTask(task);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="task">TBD</param>
        /// <param name="taskWasPreviouslyQueued">TBD</param>
        /// <returns>TBD</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Prevent inline execution, it will execute inline anyway in QueueTask if we
            // are already in the actor context.
            return false;
        }

        //private static readonly Task s_completedTask = Akka.Util.Internal.TaskEx.CompletedTask;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="action">TBD</param>
        public static void RunTask(Action action)
        {
            var context = EnsureContext();

            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, context.TaskScheduler)
                        .LinkOutcome(context);
        }

        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        /// <param name="state"></param>
        public static void RunTask(Action<object> action, object state)
        {
            var context = EnsureContext();

            Task.Factory.StartNew(action, state, CancellationToken.None, TaskCreationOptions.None, context.TaskScheduler)
                        .LinkOutcome(context);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="runnable">TBD</param>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if this method is called outside an actor context.
        /// </exception>
        public static void RunTask(IRunnable runnable)
        {
            var context = EnsureContext();

            Task.Factory.StartNew(() => runnable.Run(), CancellationToken.None, TaskCreationOptions.None, context.TaskScheduler)
                        .LinkOutcome(context);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="asyncAction">TBD</param>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if this method is called outside an actor context.
        /// </exception>
        public static void RunTask(Func<Task> asyncAction)
        {
            var context = EnsureContext();

            Task<Task>.Factory.StartNew(asyncAction, CancellationToken.None, TaskCreationOptions.None, context.TaskScheduler)
                              .FastUnwrap()
                              .LinkOutcome(context);
        }

        /// <summary>TBD</summary>
        /// <param name="asyncAction">TBD</param>
        /// <param name="state"></param>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if this method is called outside an actor context.
        /// </exception>
        public static void RunTask(Func<object, Task> asyncAction, object state)
        {
            var context = EnsureContext();

            Task<Task>.Factory.StartNew(asyncAction, state, CancellationToken.None, TaskCreationOptions.None, context.TaskScheduler)
                              .FastUnwrap()
                              .LinkOutcome(context);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="task">TBD</param>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if this method is called outside an actor context.
        /// </exception>
        public static void RunTask(IRunnableTask task)
        {
            var context = EnsureContext();

            Task<Task>.Factory.StartNew(() => task.Run(), CancellationToken.None, TaskCreationOptions.None, context.TaskScheduler)
                              .FastUnwrap()
                              .LinkOutcome(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ActorCell EnsureContext()
        {
            var context = ActorCell.Current;

            if (context is null) AkkaThrowHelper.ThrowInvalidOperationException(AkkaExceptionResource.InvalidOperation_ActorTaskScheduler_RunTask);

            var dispatcher = context.Dispatcher;

            //suspend the mailbox
            dispatcher.Suspend(context);

            ActorTaskScheduler actorScheduler = context.TaskScheduler;
            actorScheduler.CurrentMessage = context.CurrentMessage;

            return context;
        }
    }
}

