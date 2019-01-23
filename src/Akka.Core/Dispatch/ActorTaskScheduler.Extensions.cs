using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;

namespace Akka.Dispatch
{
    internal static class ActorTaskSchedulerExtensions
    {
        private static void ThenRunTask(this Task parent, MessageDispatcher dispatcher, ActorCell context, ActorTaskScheduler actorScheduler)
        {
            Exception exception = GetTaskException(parent);

            if (exception == null)
            {
                dispatcher.Resume(context);

                context.CheckReceiveTimeout();
            }
            else
            {
                context.Self.AsInstanceOf<IInternalActorRef>().SendSystemMessage(new ActorTaskSchedulerMessage(exception, actorScheduler.CurrentMessage));
            }
            //clear the current message field of the scheduler
            actorScheduler.CurrentMessage = null;
        }

        private static void ThenRunTask(this Task parent, object state)
        {
            Exception exception = GetTaskException(parent);
            var wrapped = (Tuple<MessageDispatcher, ActorCell, ActorTaskScheduler>)state;
            var actorScheduler = wrapped.Item3;
            if (exception == null)
            {
                var context = wrapped.Item2;
                wrapped.Item1.Resume(context);

                context.CheckReceiveTimeout();
            }
            else
            {
                wrapped.Item2.Self.AsInstanceOf<IInternalActorRef>().SendSystemMessage(new ActorTaskSchedulerMessage(exception, actorScheduler.CurrentMessage));
            }
            //clear the current message field of the scheduler
            actorScheduler.CurrentMessage = null;
        }

        public static void LinkOutcome(this Task parent, MessageDispatcher dispatcher, ActorCell context, ActorTaskScheduler actorScheduler)
        {
            switch (parent.Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    parent.ThenRunTask(dispatcher, context, actorScheduler);
                    break;
                default:
                    parent.ContinueWith(
                        LinkOutcomeContinuationAction,
                        Tuple.Create(dispatcher, context, actorScheduler),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        actorScheduler);
                    break;
            }
        }

        static readonly Action<Task, object> LinkOutcomeContinuationAction = LinkOutcomeContinuation;
        private static void LinkOutcomeContinuation(Task t, object state)
        {
            switch (t.Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    t.ThenRunTask(state);
                    break;
                default:
                    AkkaThrowHelper.ThrowArgumentOutOfRangeException(); break;
            }
        }

        private static Exception GetTaskException(Task task)
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    return new TaskCanceledException();

                case TaskStatus.Faulted:
                    return TryUnwrapAggregateException(task.Exception);
            }

            return null;
        }

        private static Exception TryUnwrapAggregateException(AggregateException aggregateException)
        {
            if (aggregateException == null)
                return null;

            if (aggregateException.InnerExceptions.Count == 1)
                return aggregateException.InnerExceptions[0];

            return aggregateException;
        }
    }
}
