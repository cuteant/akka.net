using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Util.Internal;

namespace Akka.Util
{
    /// <summary>TBD</summary>
    public static partial class TaskAsyncHelper
    {
        #region -- FastUnwrap --

        /// <summary>TBD</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FastUnwrap(this Task<Task> task)
        {
            if (task.IsSuccessfully()) { return task.Result; }
            return task.Unwrap();
        }

        /// <summary>TBD</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> FastUnwrap<T>(this Task<Task<T>> task)
        {
            if (task.IsSuccessfully()) { return task.Result; }
            return task.Unwrap();
        }

        #endregion

        #region -- IsSuccessfully --

        /// <summary>TBD</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuccessfully(this Task task)
        {
#if NETCOREAPP
            return task.IsCompletedSuccessfully;
#else
            return task.IsCompleted && !task.IsFaulted && !task.IsCanceled;
#endif
        }

        /// <summary>TBD</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuccessfully<T>(this Task<T> task)
        {
#if NETCOREAPP
            return task.IsCompletedSuccessfully;
#else
            return task.IsCompleted && !task.IsFaulted && !task.IsCanceled;
#endif
        }

        #endregion

        #region -- TrySetUnwrappedException --

        /// <summary>TBD</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tcs"></param>
        /// <param name="e"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            if (e is AggregateException aggregateException)
            {
                tcs.TrySetException(aggregateException.InnerExceptions);
            }
            else
            {
                tcs.TrySetException(e);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tcs"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TrySetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            if (e is AggregateException aggregateException)
            {
                return tcs.TrySetException(aggregateException.InnerExceptions);
            }
            else
            {
                return tcs.TrySetException(e);
            }
        }

        #endregion

        #region -- FromMethod --

        /// <summary>TBD</summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task FromMethod(Action func)
        {
            try
            {
                func();
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task FromMethod(Func<Task> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<TResult> FromMethod<TResult>(Func<TResult> func)
        {
            try
            {
                return Task.FromResult(func());
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<TResult> FromMethod<TResult>(Func<Task<TResult>> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        #endregion

        #region -- FromRunnable --

        /// <summary>TBD</summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task FromRunnable(IRunnable func)
        {
            try
            {
                func.Run();
                return TaskEx.CompletedTask;
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task FromRunnable(IRunnableTask func)
        {
            try
            {
                return func.Run();
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<TResult> FromRunnable<TResult>(IRunnable<TResult> func)
        {
            try
            {
                return Task.FromResult(func.Run());
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<TResult> FromRunnable<TResult>(IRunnableTask<TResult> func)
        {
            try
            {
                return func.Run();
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<TResult>(ex);
            }
        }

        #endregion

        #region -- LinkOutcome --

        /// <summary>TBD</summary>
        public static Task LinkOutcome(this Task task, Action<Task> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome(this Task task, Action<Task> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper
                .RunTask(task, (IArgumentOverrides<Task>)Runnable.Create(processor, default),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome(this Task task, Func<Task, Task> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome(this Task task, Func<Task, Task> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<Task>
                .RunTask(task, (IArgumentOverrides<Task, Task>)Runnable.CreateTask(processor, default),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TResult>(this Task task, Func<Task, TResult> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TResult>(this Task task, Func<Task, TResult> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task, TResult>)Runnable.Create(processor, default),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TResult>(this Task task, Func<Task, Task<TResult>> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> LinkOutcome<TResult>(this Task task, Func<Task, Task<TResult>> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<Task<TResult>>
                .RunTask(task, (IArgumentOverrides<Task, Task<TResult>>)Runnable.CreateTask(processor, default),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        #endregion

        #region -- LinkOutcome<TResult> --

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult>(this Task<TResult> task, Action<Task<TResult>> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult>(this Task<TResult> task, Action<Task<TResult>> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<TResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>>)Runnable.Create(processor, default),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult>(this Task<TResult> task, Func<Task<TResult>, Task> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task LinkOutcome<TResult>(this Task<TResult> task, Func<Task<TResult>, Task> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<TResult, Task>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task>)Runnable.CreateTask(processor, default),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TNewResult> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TNewResult> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, TNewResult>)Runnable.Create(processor, default),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, Task<TNewResult>> processor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return LinkOutcome(task, processor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> LinkOutcome<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, Task<TNewResult>> processor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (task.IsCompleted) { return FromMethod(processor, task); }
            return LinkOutcomeHelper<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<Task<TResult>, Task<TNewResult>>)Runnable.CreateTask(processor, default),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        #endregion

        #region ** class LinkOutcomeHelper **

        private static class LinkOutcomeHelper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task RunTask(Task task, IArgumentOverrides<Task> processor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(LinkOutcomeContinuationAction, Tuple.Create(processor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task, object> LinkOutcomeContinuationAction = LinkOutcomeContinuation;
            private static void LinkOutcomeContinuation(Task task, object state)
            {
                var wrapped = (Tuple<IArgumentOverrides<Task>, TaskCompletionSource<object>>)state;
                var tcs = wrapped.Item2;
                try
                {
                    wrapped.Item1.Run(task);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                }
            }
        }

        #endregion

        #region ** class LinkOutcomeHelper<TResult> **

        private static class LinkOutcomeHelper<TResult>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task RunTask(Task<TResult> task, IArgumentOverrides<Task<TResult>> processor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(RunWithResultTaskContinuationAction, Tuple.Create(processor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task<TResult>, object> RunWithResultTaskContinuationAction = RunWithResultTaskContinuation;
            private static void RunWithResultTaskContinuation(Task<TResult> task, object state)
            {
                var wrapped = (Tuple<IArgumentOverrides<Task<TResult>>, TaskCompletionSource<object>>)state;
                var tcs = wrapped.Item2;
                try
                {
                    wrapped.Item1.Run(task);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task<TResult> RunTask(Task task, IArgumentOverrides<Task, TResult> processor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(LinkOutcomeContinuationAction, Tuple.Create(processor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task, object> LinkOutcomeContinuationAction = LinkOutcomeContinuation;
            private static void LinkOutcomeContinuation(Task task, object state)
            {
                var wrapped = (Tuple<IArgumentOverrides<Task, TResult>, TaskCompletionSource<TResult>>)state;
                var tcs = wrapped.Item2;
                try
                {
                    tcs.TrySetResult(wrapped.Item1.Run(task));
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                }
            }
        }

        #endregion

        #region ** class LinkOutcomeHelper<TResult, TNewResult> **

        private static class LinkOutcomeHelper<TResult, TNewResult>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task<TNewResult> RunTask(Task<TResult> task, IArgumentOverrides<Task<TResult>, TNewResult> processor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<TNewResult>();

                task.ContinueWith(LinkOutcomeContinuationAction, Tuple.Create(processor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task<TResult>, object> LinkOutcomeContinuationAction = LinkOutcomeContinuation;
            private static void LinkOutcomeContinuation(Task<TResult> task, object state)
            {
                var wrapped = (Tuple<IArgumentOverrides<Task<TResult>, TNewResult>, TaskCompletionSource<TNewResult>>)state;
                var tcs = wrapped.Item2;
                try
                {
                    tcs.TrySetResult(wrapped.Item1.Run(task));
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                }
            }
        }

        #endregion

        #region -- Then(IRunnable) --

        /// <summary>TBD</summary>
        public static Task Then(this Task task, IRunnable successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners.RunTask(task, successor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then(this Task task, IRunnable successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners.RunTask(task, successor, cancellationToken, continuationOptions, scheduler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryRunIfCompleted(Task task, IRunnable successor, out Task result)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                result = FromRunnable(successor); return true;
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                result = task; return true;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                result = task; return true;
            }
            else if (task.IsCompleted)
            {
                result = FromRunnable(successor); return true;
            }
#endif
            result = null; return false;
        }

        #endregion

        #region -- Then(IRunnable<TResult>) --

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TResult>(this Task task, IRunnable<TResult> successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners<TResult>.RunTask(task, successor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TResult>(this Task task, IRunnable<TResult> successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners<TResult>.RunTask(task, successor, cancellationToken, continuationOptions, scheduler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryRunIfCompleted<TResult>(Task task, IRunnable<TResult> successor, out Task<TResult> result)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                result = FromRunnable(successor); return true;
            }
            else if (task.IsCanceled)
            {
                result = Canceled<TResult>(); return true;
            }
            else if (task.IsFaulted)
            {
                result = TaskEx.FromException<TResult>(task.Exception); return true;
            }
#else
            if (task.IsCanceled)
            {
                result = Canceled<TResult>(); return true;
            }
            else if (task.IsFaulted)
            {
                result = TaskEx.FromException<TResult>(task.Exception); return true;
            }
            else if (task.IsCompleted)
            {
                result = FromRunnable(successor); return true;
            }
#endif
            result = null; return false;
        }

        #endregion

        #region -- Then(IRunnableTask) --

        /// <summary>TBD</summary>
        public static Task Then(this Task task, IRunnableTask successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners<Task>
                .RunTask(task, successor, default, continuationOptions, TaskScheduler.Current)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task Then(this Task task, IRunnableTask successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners<Task>
                .RunTask(task, successor, cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryRunIfCompleted(Task task, IRunnableTask successor, out Task result)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                result = FromRunnable(successor); return true;
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                result = task; return true;
            }
#else
            if (task.IsCanceled || task.IsFaulted)
            {
                result = task; return true;
            }
            else if (task.IsCompleted)
            {
                result = FromRunnable(successor); return true;
            }
#endif
            result = null; return false;
        }

        #endregion

        #region -- Then(IRunnable<TResult>) --

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TResult>(this Task task, IRunnableTask<TResult> successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners<Task<TResult>>
                .RunTask(task, successor, default, continuationOptions, TaskScheduler.Current)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TResult> Then<TResult>(this Task task, IRunnableTask<TResult> successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (TryRunIfCompleted(task, successor, out var result)) { return result; }
            return TaskRunners<Task<TResult>>
                .RunTask(task, successor, cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryRunIfCompleted<TResult>(Task task, IRunnableTask<TResult> successor, out Task<TResult> result)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                result = FromRunnable(successor); return true;
            }
            else if (task.IsCanceled)
            {
                result = Canceled<TResult>(); return true;
            }
            else if (task.IsFaulted)
            {
                result = TaskEx.FromException<TResult>(task.Exception); return true;
            }
#else
            if (task.IsCanceled)
            {
                result = Canceled<TResult>(); return true;
            }
            else if (task.IsFaulted)
            {
                result = TaskEx.FromException<TResult>(task.Exception); return true;
            }
            else if (task.IsCompleted)
            {
                result = FromRunnable(successor); return true;
            }
#endif
            result = null; return false;
        }

        #endregion

        #region -- Then<TResult> --

        /// <summary>TBD</summary>
        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result);
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
                return FromMethod(successor, task.Result);
            }
#endif
            return TaskRunners<TResult>
                .RunTask(task, (IArgumentOverrides<TResult>)Runnable.Create(successor, default),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult>(this Task<TResult> task, Func<TResult, Task> successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task Then<TResult>(this Task<TResult> task, Func<TResult, Task> successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result);
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
                return FromMethod(successor, task.Result);
            }
#endif
            return TaskRunners<TResult, Task>
                .RunTask(task, (IArgumentOverrides<TResult, Task>)Runnable.CreateTask(successor, default),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TNewResult>(this Task<TResult> task, Func<TResult, TNewResult> successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TNewResult>(this Task<TResult> task, Func<TResult, TNewResult> successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result);
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
                return FromMethod(successor, task.Result);
            }
#endif
            return TaskRunners<TResult, TNewResult>
                .RunTask(task, (IArgumentOverrides<TResult, TNewResult>)Runnable.Create(successor, default),
                    cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TNewResult>(this Task<TResult> task, Func<TResult, Task<TNewResult>> successor, TaskContinuationOptions continuationOptions = TaskContinuationOptions.None)
        {
            return Then(task, successor, default, continuationOptions, TaskScheduler.Current);
        }

        /// <summary>TBD</summary>
        public static Task<TNewResult> Then<TResult, TNewResult>(this Task<TResult> task, Func<TResult, Task<TNewResult>> successor,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
#if NETCOREAPP
            if (task.IsCompletedSuccessfully)
            {
                return FromMethod(successor, task.Result);
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
                return FromMethod(successor, task.Result);
            }
#endif
            return TaskRunners<TResult, Task<TNewResult>>
                .RunTask(task, (IArgumentOverrides<TResult, Task<TNewResult>>)Runnable.CreateTask(successor, default),
                    cancellationToken, continuationOptions, scheduler)
                .FastUnwrap();
        }

        #endregion

        #region ** class TaskRunners **

        private static class TaskRunners
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task RunTask(Task task, IRunnable successor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<object>();

                task.ContinueWith(RunTaskContinuationAction, Tuple.Create(successor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task, object> RunTaskContinuationAction = RunTaskContinuation;
            private static void RunTaskContinuation(Task task, object state)
            {
                var wrapped = (Tuple<IRunnable, TaskCompletionSource<object>>)state;
                var tcs = wrapped.Item2;
#if NETCOREAPP
                if (task.IsCompletedSuccessfully)
                {
                    try
                    {
                        wrapped.Item1.Run();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
#else
                if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCompleted)
                {
                    try
                    {
                        wrapped.Item1.Run();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
#endif
            }
        }

        #endregion

        #region ** class TaskRunners<TResult> **

        private static class TaskRunners<TResult>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task<TResult> RunTask(Task task, IRunnable<TResult> successor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<TResult>();

                task.ContinueWith(RunTaskContinuationAction, Tuple.Create(successor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task, object> RunTaskContinuationAction = RunTaskContinuation;
            private static void RunTaskContinuation(Task task, object state)
            {
                var wrapped = (Tuple<IRunnable<TResult>, TaskCompletionSource<TResult>>)state;
                var tcs = wrapped.Item2;
#if NETCOREAPP
                if (task.IsCompletedSuccessfully)
                {
                    try
                    {
                        tcs.TrySetResult(wrapped.Item1.Run());
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
#else
                if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCompleted)
                {
                    try
                    {
                        tcs.TrySetResult(wrapped.Item1.Run());
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
#endif
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task RunTask(Task<TResult> task, IArgumentOverrides<TResult> successor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(RunWithResultTaskContinuationAction, Tuple.Create(successor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task<TResult>, object> RunWithResultTaskContinuationAction = RunWithResultTaskContinuation;
            private static void RunWithResultTaskContinuation(Task<TResult> task, object state)
            {
                var wrapped = (Tuple<IArgumentOverrides<TResult>, TaskCompletionSource<object>>)state;
                var tcs = wrapped.Item2;
#if NETCOREAPP
                if (task.IsCompletedSuccessfully)
                {
                    try
                    {
                        wrapped.Item1.Run(task.Result);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
#else
                if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCompleted)
                {
                    try
                    {
                        wrapped.Item1.Run(task.Result);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
#endif
            }
        }

        #endregion

        #region ** class TaskRunners<TResult, TNewResult> **

        private static class TaskRunners<TResult, TNewResult>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static Task<TNewResult> RunTask(Task<TResult> task, IArgumentOverrides<TResult, TNewResult> successor,
                CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            {
                var tcs = new TaskCompletionSource<TNewResult>();

                task.ContinueWith(RunTaskContinuationAction, Tuple.Create(successor, tcs),
                    cancellationToken, continuationOptions, scheduler);

                return tcs.Task;
            }

            private static readonly Action<Task<TResult>, object> RunTaskContinuationAction = RunTaskContinuation;
            private static void RunTaskContinuation(Task<TResult> task, object state)
            {
                var wrapped = (Tuple<IArgumentOverrides<TResult, TNewResult>, TaskCompletionSource<TNewResult>>)state;
                var tcs = wrapped.Item2;
#if NETCOREAPP
                if (task.IsCompletedSuccessfully)
                {
                    try
                    {
                        tcs.TrySetResult(wrapped.Item1.Run(task.Result));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
#else
                if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCompleted)
                {
                    try
                    {
                        tcs.TrySetResult(wrapped.Item1.Run(task.Result));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
#endif
            }
        }

        #endregion

        #region ** Canceled **

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task Canceled()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();
            return tcs.Task;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task<T> Canceled<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.TrySetCanceled();
            return tcs.Task;
        }

        #endregion
    }
}
