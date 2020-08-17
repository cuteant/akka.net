using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;

namespace Akka.Dispatch
{
    internal static class ActorTaskSchedulerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LinkOutcome(this Task parent, ActorCell context)
        {
            parent.ContinueWith(
                LinkOutcomeContinuationAction,
                context,
                //CancellationToken.None,
                //TaskContinuationOptions.ExecuteSynchronously,
                context.TaskScheduler);
        }

        static readonly Action<Task, object> LinkOutcomeContinuationAction = LinkOutcomeContinuation;
        private static void LinkOutcomeContinuation(Task parent, object state)
        {
            Exception exception = GetTaskException(parent);
            var context = (ActorCell)state;
            var actorScheduler = context.TaskScheduler;
            if (exception is null)
            {
                context.Dispatcher.Resume(context);

                context.CheckReceiveTimeout();
            }
            else
            {
                context.Self.AsInstanceOf<IInternalActorRef>().SendSystemMessage(new ActorTaskSchedulerMessage(exception, actorScheduler.CurrentMessage));
            }
            //clear the current message field of the scheduler
            actorScheduler.CurrentMessage = null;
        }

        private static Exception GetTaskException(Task task)
        {
            if (task.IsCanceled)
            {
                return new TaskCanceledException();
            }
            else if (task.IsFaulted)
            {
                return TryUnwrapAggregateException(task.Exception);
            }

            return null;
        }

        private static Exception TryUnwrapAggregateException(AggregateException aggregateException)
        {
            if (aggregateException is null)
                return null;

            if (aggregateException.InnerExceptions.Count == 1)
                return aggregateException.InnerExceptions[0];

            return aggregateException;
        }
    }
}
