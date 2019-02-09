using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Akka
{
    partial class Runnable
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1>(Action<TArg1> action, TArg1 arg1)
        {
            return new ActionWithStateRunnable<TArg1>(action, arg1);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TResult>(Func<TArg1, TResult> func, TArg1 arg1)
        {
            return new DefaultRunnable<TArg1, TResult>(func, arg1);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1>(Func<TArg1, Task> func, TArg1 arg1)
        {
            return new DefaultRunnableTask<TArg1>(func, arg1);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TResult>(Func<TArg1, Task<TResult>> func, TArg1 arg1)
        {
            return new DefaultRunnableWithResultTask<TArg1, TResult>(func, arg1);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            return new ActionWithStateRunnable<TArg1, TArg2>(action, arg1, arg2);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2)
        {
            return new DefaultRunnable<TArg1, TArg2, TResult>(func, arg1, arg2);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1, TArg2>(Func<TArg1, TArg2, Task> func, TArg1 arg1, TArg2 arg2)
        {
            return new DefaultRunnableTask<TArg1, TArg2>(func, arg1, arg2);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TArg2, TResult>(Func<TArg1, TArg2, Task<TResult>> func, TArg1 arg1, TArg2 arg2)
        {
            return new DefaultRunnableWithResultTask<TArg1, TArg2, TResult>(func, arg1, arg2);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return new ActionWithStateRunnable<TArg1, TArg2, TArg3>(action, arg1, arg2, arg3);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return new DefaultRunnable<TArg1, TArg2, TArg3, TResult>(func, arg1, arg2, arg3);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return new DefaultRunnableTask<TArg1, TArg2, TArg3>(func, arg1, arg2, arg3);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return new DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TResult>(func, arg1, arg2, arg3);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return new ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4>(action, arg1, arg2, arg3, arg4);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return new DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TResult>(func, arg1, arg2, arg3, arg4);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return new DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4>(func, arg1, arg2, arg3, arg4);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return new DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TResult>(func, arg1, arg2, arg3, arg4);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return new ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4, TArg5>(action, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return new DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(func, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return new DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4, TArg5>(func, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return new DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(func, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return new ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(action, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return new DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return new DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(func, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return new DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable2 Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return new ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return new DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return new DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return new DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

    }
}
