using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Akka
{
    /// <summary>TBD</summary>
    public static partial class Runnable
    {
        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable Create(Action action)
        {
            return new ActionRunnable(action);
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TResult">TBD</typeparam>
        /// <param name="func">TBD</param>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnable<TResult> Create<TResult>(Func<TResult> func)
        {
            return new DefaultRunnable<TResult>(func);
        }

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask CreateTask(Func<Task> func)
        {
            return new DefaultRunnableTask(func);
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TResult">TBD</typeparam>
        /// <param name="func">TBD</param>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRunnableTask<TResult> CreateTask<TResult>(Func<Task<TResult>> func)
        {
            return new DefaultRunnableWithResultTask<TResult>(func);
        }
    }
}
