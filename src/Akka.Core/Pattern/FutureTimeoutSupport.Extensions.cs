using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Pattern
{
    partial class FutureTimeoutSupport
    {
        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, Task<T>> value, TArg1 arg1)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, TArg2, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, TArg2, Task<T>> value, TArg1 arg1, TArg2 arg2)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1, arg2));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, TArg2, TArg3, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, TArg2, TArg3, Task<T>> value, TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1, arg2, arg3));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, TArg2, TArg3, TArg4, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, TArg2, TArg3, TArg4, Task<T>> value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1, arg2, arg3, arg4));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <typeparam name="TArg5">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, TArg2, TArg3, TArg4, TArg5, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<T>> value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1, arg2, arg3, arg4, arg5));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <typeparam name="TArg5">TBD</typeparam>
        /// <typeparam name="TArg6">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<T>> value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1, arg2, arg3, arg4, arg5, arg6));

        /// <summary>Returns a <see cref="Task"/> that will be completed with the success or failure
        /// of the provided value after the specified duration.</summary>
        /// <typeparam name="T">The return type of task.</typeparam>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <typeparam name="TArg5">TBD</typeparam>
        /// <typeparam name="TArg6">TBD</typeparam>
        /// <typeparam name="TArg7">TBD</typeparam>
        /// <param name="duration">The duration to wait.</param>
        /// <param name="scheduler">The scheduler instance to use.</param>
        /// <param name="value">The task we're going to wrap.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <returns>a <see cref="Task{T}"/> that will be completed with the success or failure
        /// of the provided value after the specified duration</returns>
        public static Task<T> After<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, T>(TimeSpan duration, IScheduler scheduler, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<T>> value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
            => After(duration, scheduler, Runnable.CreateTask(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7));

    }
}
