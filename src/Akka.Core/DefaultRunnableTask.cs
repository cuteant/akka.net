using System;
using System.Threading.Tasks;

namespace Akka
{
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1> : DefaultRunnable<TArg1, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, Task> func, TArg1 arg1) : base(func, arg1) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TResult> : DefaultRunnable<TArg1, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, Task<TResult>> func, TArg1 arg1) : base(func, arg1) { }
    }
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1, TArg2> : DefaultRunnable<TArg1, TArg2, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, TArg2, Task> func, TArg1 arg1, TArg2 arg2) : base(func, arg1, arg2) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TArg2, TResult> : DefaultRunnable<TArg1, TArg2, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, TArg2, Task<TResult>> func, TArg1 arg1, TArg2 arg2) : base(func, arg1, arg2) { }
    }
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1, TArg2, TArg3> : DefaultRunnable<TArg1, TArg2, TArg3, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, TArg2, TArg3, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3) : base(func, arg1, arg2, arg3) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TResult> : DefaultRunnable<TArg1, TArg2, TArg3, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3) : base(func, arg1, arg2, arg3) { }
    }
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, TArg2, TArg3, TArg4, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) : base(func, arg1, arg2, arg3, arg4) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TResult> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, TArg2, TArg3, TArg4, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) : base(func, arg1, arg2, arg3, arg4) { }
    }
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4, TArg5> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) : base(func, arg1, arg2, arg3, arg4, arg5) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) : base(func, arg1, arg2, arg3, arg4, arg5) { }
    }
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) : base(func, arg1, arg2, arg3, arg4, arg5, arg6) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) : base(func, arg1, arg2, arg3, arg4, arg5, arg6) { }
    }
    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        public DefaultRunnableTask(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) : base(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> : DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        public DefaultRunnableWithResultTask(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) : base(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7) { }
    }
}
