using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Util.Internal;


namespace Akka.Util
{
    partial class TaskAsyncHelper
    {
        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1>(Action<TArg1> func, TArg1 arg1)
        {
            try
            {
                func(arg1);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1>(Func<TArg1, Task> func, TArg1 arg1)
        {
            try
            {
                return func(arg1);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TResult>(Func<TArg1, TResult> func, TArg1 arg1)
        {
            try
            {
                return Task.FromResult(func(arg1));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TResult>(Func<TArg1, Task<TResult>> func, TArg1 arg1)
        {
            try
            {
                return func(arg1);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1>(this Task task, Action<TArg1> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1>(this Task task, Action<TArg1> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TResult>(this Task task, Func<TArg1, TResult> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TResult>(this Task task, Func<TArg1, TResult> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1>(this Task task, Func<TArg1, Task> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1>(this Task task, Func<TArg1, Task> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TResult>(this Task task, Func<TArg1, Task<TResult>> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TResult>(this Task task, Func<TArg1, Task<TResult>> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1), cancellationToken, continuationOptions, scheduler);
        }


        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1>(this Task<TResult> task, Action<TResult, TArg1> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1>(this Task<TResult> task, Action<TResult, TArg1> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default, arg1),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1>(this Task<TResult> task, Func<TResult, TArg1, Task> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1>(this Task<TResult> task, Func<TResult, TArg1, Task> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default, arg1),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TNewResult> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TNewResult> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default, arg1),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, Task<TNewResult>> successor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, Task<TNewResult>> successor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default, arg1),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1>(this Task task, Action<Task, TArg1> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1>(this Task task, Action<Task, TArg1> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1>(this Task task, Func<Task, TArg1, Task> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1>(this Task task, Func<Task, TArg1, Task> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TResult>(this Task task, Func<Task, TArg1, TResult> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TResult>(this Task task, Func<Task, TArg1, TResult> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TResult>(this Task task, Func<Task, TArg1, Task<TResult>> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TResult>(this Task task, Func<Task, TArg1, Task<TResult>> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1>(this Task<TResult> task, Action<Task<TResult>, TArg1> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1>(this Task<TResult> task, Action<Task<TResult>, TArg1> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1>(this Task<TResult> task, Func<Task<TResult>, TArg1, Task> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1>(this Task<TResult> task, Func<Task<TResult>, TArg1, Task> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TNewResult> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TNewResult> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, Task<TNewResult>> processor, TArg1 arg1, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, Task<TNewResult>> processor, TArg1 arg1,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default, arg1),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2>(Action<TArg1, TArg2> func, TArg1 arg1, TArg2 arg2)
        {
            try
            {
                func(arg1, arg2);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2>(Func<TArg1, TArg2, Task> func, TArg1 arg1, TArg2 arg2)
        {
            try
            {
                return func(arg1, arg2);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2)
        {
            try
            {
                return Task.FromResult(func(arg1, arg2));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TResult>(Func<TArg1, TArg2, Task<TResult>> func, TArg1 arg1, TArg2 arg2)
        {
            try
            {
                return func(arg1, arg2);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2>(this Task task, Action<TArg1, TArg2> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2>(this Task task, Action<TArg1, TArg2> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TResult>(this Task task, Func<TArg1, TArg2, TResult> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TResult>(this Task task, Func<TArg1, TArg2, TResult> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2>(this Task task, Func<TArg1, TArg2, Task> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2>(this Task task, Func<TArg1, TArg2, Task> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TResult>(this Task task, Func<TArg1, TArg2, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TResult>(this Task task, Func<TArg1, TArg2, Task<TResult>> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2), cancellationToken, continuationOptions, scheduler);
        }


        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2>(this Task<TResult> task, Action<TResult, TArg1, TArg2> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2>(this Task<TResult> task, Action<TResult, TArg1, TArg2> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2>(this Task<TResult> task, Func<TResult, TArg1, TArg2, Task> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2>(this Task<TResult> task, Func<TResult, TArg1, TArg2, Task> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TNewResult> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TNewResult> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2>(this Task task, Action<Task, TArg1, TArg2> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2>(this Task task, Action<Task, TArg1, TArg2> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2>(this Task task, Func<Task, TArg1, TArg2, Task> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2>(this Task task, Func<Task, TArg1, TArg2, Task> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TResult>(this Task task, Func<Task, TArg1, TArg2, TResult> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TResult>(this Task task, Func<Task, TArg1, TArg2, TResult> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TResult>(this Task task, Func<Task, TArg1, TArg2, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TResult>(this Task task, Func<Task, TArg1, TArg2, Task<TResult>> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, Task> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, Task> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TNewResult> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TNewResult> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default, arg1, arg2),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            try
            {
                func(arg1, arg2, arg3);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            try
            {
                return func(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            try
            {
                return Task.FromResult(func(arg1, arg2, arg3));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            try
            {
                return func(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3>(this Task task, Action<TArg1, TArg2, TArg3> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3>(this Task task, Action<TArg1, TArg2, TArg3> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3>(this Task task, Func<TArg1, TArg2, TArg3, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3>(this Task task, Func<TArg1, TArg2, TArg3, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TResult>(this Task task, Func<TArg1, TArg2, TArg3, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TResult>(this Task task, Func<TArg1, TArg2, TArg3, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3), cancellationToken, continuationOptions, scheduler);
        }


        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3>(this Task task, Action<Task, TArg1, TArg2, TArg3> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3>(this Task task, Action<Task, TArg1, TArg2, TArg3> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3>(this Task task, Func<Task, TArg1, TArg2, TArg3, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3>(this Task task, Func<Task, TArg1, TArg2, TArg3, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            try
            {
                func(arg1, arg2, arg3, arg4);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            try
            {
                return Task.FromResult(func(arg1, arg2, arg3, arg4));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4>(this Task task, Action<TArg1, TArg2, TArg3, TArg4> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4>(this Task task, Action<TArg1, TArg2, TArg3, TArg4> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4), cancellationToken, continuationOptions, scheduler);
        }


        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3, TArg4> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3, TArg4> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4>(this Task task, Action<Task, TArg1, TArg2, TArg3, TArg4> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4>(this Task task, Action<Task, TArg1, TArg2, TArg3, TArg4> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3, TArg4> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3, TArg4> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            try
            {
                func(arg1, arg2, arg3, arg4, arg5);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            try
            {
                return Task.FromResult(func(arg1, arg2, arg3, arg4, arg5));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Action<TArg1, TArg2, TArg3, TArg4, TArg5> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Action<TArg1, TArg2, TArg3, TArg4, TArg5> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5), cancellationToken, continuationOptions, scheduler);
        }


        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3, TArg4, TArg5> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3, TArg4, TArg5> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Action<Task, TArg1, TArg2, TArg3, TArg4, TArg5> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Action<Task, TArg1, TArg2, TArg3, TArg4, TArg5> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            try
            {
                func(arg1, arg2, arg3, arg4, arg5, arg6);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            try
            {
                return Task.FromResult(func(arg1, arg2, arg3, arg4, arg5, arg6));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken, continuationOptions, scheduler);
        }


        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Action<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                return task;
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TNewResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            else if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
#else
            if (task.IsCanceled)
            {
                return Canceled<TNewResult>();
            }
            else if (task.IsFaulted)
            {
                return TaskEx.FromException<TNewResult>(task.Exception);
            }
            else if (task.IsCompleted)
            {
                return FromMethod(successor, task.Result, arg1, arg2, arg3, arg4, arg5, arg6);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Action<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Action<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Task task, Func<Task, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Action<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, arg1, arg2, arg3, arg4, arg5, arg6, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TNewResult>> processor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task, arg1, arg2, arg3, arg4, arg5, arg6); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default, arg1, arg2, arg3, arg4, arg5, arg6),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            try
            {
                func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            try
            {
                return Task.FromResult(func(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task<TResult> FromMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this Task task, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this Task task, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.Create(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), continuationOptions);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this Task task, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>> successor, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return Then(task, Runnable.CreateTask(successor, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken, continuationOptions, scheduler);
        }


    }
}