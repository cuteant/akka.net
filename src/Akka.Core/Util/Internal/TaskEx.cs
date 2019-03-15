//-----------------------------------------------------------------------
// <copyright file="TaskEx.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Akka.Util.Internal
{
    /// <summary>
    /// INTERNAL API.
    /// 
    /// Renamed from <see cref="T:Akka.Util.Internal.TaskExtensions"/> so it doesn't collide
    /// with a helper class in the same namespace defined in System.Threading.Tasks.
    /// </summary>
    internal static class TaskEx
    {
        public static readonly Task CompletedTask =
#if NET451
            Task.FromResult(0);
#else
            Task.CompletedTask;
#endif
#if NET451
        public static readonly bool IsRunContinuationsAsynchronouslyAvailable = false;
#else
        public static readonly bool IsRunContinuationsAsynchronouslyAvailable = true;
#endif

        /// <summary>
        /// Creates a new <see cref="TaskCompletionSource{TResult}"/> which will run in asynchronous,
        /// non-blocking fashion upon calling <see cref="TaskCompletionSource{TResult}.TrySetResult"/>.
        ///
        /// This behavior is not available on all supported versions of .NET framework, in this case it
        /// should be used only together with <see cref="NonBlockingTrySetResult{T}"/> and
        /// <see cref="NonBlockingTrySetException{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TaskCompletionSource<T> NonBlockingTaskCompletionSource<T>()
        {
#if NET451
            return new TaskCompletionSource<T>();
#else
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }

        /// <summary>
        /// Tries to complete given <paramref name="taskCompletionSource"/> in asynchronous, non-blocking
        /// fashion. For safety reasons, this method should be called only on tasks created via
        /// <see cref="NonBlockingTaskCompletionSource{T}"/> method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NonBlockingTrySetResult<T>(this TaskCompletionSource<T> taskCompletionSource, T value)
        {
#if NET451
            Task.Run(() => taskCompletionSource.TrySetResult(value));
#else
            taskCompletionSource.TrySetResult(value);
#endif
        }

        /// <summary>
        /// Tries to set <paramref name="exception"/> given <paramref name="taskCompletionSource"/>
        /// in asynchronous, non-blocking fashion. For safety reasons, this method should be called only
        /// on tasks created via <see cref="NonBlockingTaskCompletionSource{T}"/> method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NonBlockingTrySetException<T>(this TaskCompletionSource<T> taskCompletionSource, Exception exception)
        {
#if NET451
            Task.Run(() => taskCompletionSource.TrySetUnwrappedException(exception));
#else
            taskCompletionSource.TrySetUnwrappedException(exception);
#endif
        }

        /// <summary>
        /// A completed task
        /// </summary>
        public static readonly Task<Done> Completed = Task.FromResult(Done.Instance);

        /// <summary>
        /// Creates a failed <see cref="Task"/>
        /// </summary>
        /// <param name="ex">The exception to use to fail the task.</param>
        /// <returns>A failed task.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Task FromException(Exception ex)
        {
            return FromException<object>(ex);
        }

        /// <summary>
        /// Creates a failed <see cref="Task"/>
        /// </summary>
        /// <param name="ex">The exception to use to fail the task.</param>
        /// <returns>A failed task.</returns>
        /// <typeparam name="T">The type of <see cref="Task{T}"/></typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Task<T> FromException<T>(Exception ex)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.TrySetUnwrappedException<T>(ex);
            return tcs.Task;
        }
    }
}
