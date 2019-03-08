using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Akka
{
    /// <summary>An asynchronous operation</summary>
    public interface IRunnable<TResult>
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        TResult Run();
    }

    /// <summary>TBD</summary>
    public interface IOverridingArgumentRunnable<T, TResult>
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        TResult Run(T arg);
    }

    /// <summary>TBD</summary>
    public abstract class OverridingArgumentRunnable<T, TResult> : IOverridingArgumentRunnable<T, TResult>
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public abstract TResult Run(T arg);
    }

    /// <summary>An asynchronous operation</summary>
    public interface IRunnableTask : IRunnable<Task> { }

    /// <summary>An asynchronous operation</summary>
    public interface IRunnableTask<TResult> : IRunnable<Task<TResult>> { }

    /// <summary>An asynchronous operation</summary>
    public interface IOverridingArgumentRunnableTask<T> : IOverridingArgumentRunnable<T, Task> { }

    /// <summary>An asynchronous operation</summary>
    public interface IOverridingArgumentRunnableTask<T, TResult> : IOverridingArgumentRunnable<T, Task<TResult>> { }

    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TResult}"/>.</summary>
    /// <typeparam name="TResult">TBD</typeparam>
    public class DefaultRunnable<TResult> : IRunnable<TResult>
    {
        private readonly Func<TResult> _func;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        public DefaultRunnable(Func<TResult> func) => _func = func;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run() => _func();
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableTask : DefaultRunnable<Task>, IRunnableTask
    {
        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        public DefaultRunnableTask(Func<Task> func) : base(func) { }
    }

    /// <summary><see cref="IRunnableTask"/> which executes an <see cref="Func{Task}"/>.</summary>
    public sealed class DefaultRunnableWithResultTask<TResult> : DefaultRunnable<Task<TResult>>, IRunnableTask<TResult>
    {
        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        public DefaultRunnableWithResultTask(Func<Task<TResult>> func) : base(func) { }
    }

    /// <summary>TBD</summary>
    public interface IHasPromise<TResult>
    {
        /// <summary>TBD</summary>
        TaskCompletionSource<TResult> Promise { get; }

        /// <summary>TBD</summary>
        void ResetPromise();
    }

    /// <summary>TBD</summary>
    public abstract class RunnableBase<TResult> : IRunnable<TResult>, IHasPromise<TResult>
    {
        private TaskCompletionSource<TResult> _promise;
        /// <summary>TBD</summary>
        public TaskCompletionSource<TResult> Promise => Volatile.Read(ref _promise) ?? EnsurePromiseCreated();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private TaskCompletionSource<TResult> EnsurePromiseCreated()
        {
            Interlocked.CompareExchange(ref _promise, new TaskCompletionSource<TResult>(), null);
            return _promise;
        }

        /// <summary>TBD</summary>
        public void ResetPromise() => Interlocked.Exchange(ref _promise, null);

        /// <summary>TBD</summary>
        public abstract TResult Run();
    }

    /// <summary>TBD</summary>
    public abstract class RunnableTaskBase : RunnableBase<Task>, IRunnableTask { }

    /// <summary>TBD</summary>
    public abstract class RunnableTaskBase<TResult> : RunnableBase<Task<TResult>>, IRunnableTask<TResult> { }
}
