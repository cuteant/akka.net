using System;
using System.Threading.Tasks;

namespace Akka
{
    /// <summary>An asynchronous operation</summary>
    public readonly struct RunnableTaskWrapper<TResult>
    {
        public readonly Func<object, TResult> Task;
        public readonly object State;

        public RunnableTaskWrapper(Func<object, TResult> task, object state)
        {
            Task = task;
            State = state;
        }
    }

    /// <summary>An asynchronous operation</summary>
    public interface IRunnable<TResult>
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        TResult Run();

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        RunnableTaskWrapper<TResult> WrapTask();
    }

    public interface IArgumentOverrides<T, TResult>
    {
        TResult Run(T arg);
    }

    /// <summary>An asynchronous operation</summary>
    public interface IRunnableTask : IRunnable<Task> { }

    /// <summary>An asynchronous operation</summary>
    public interface IRunnableTask<TResult> : IRunnable<Task<TResult>> { }

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

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public RunnableTaskWrapper<TResult> WrapTask() => new RunnableTaskWrapper<TResult>(InternalWrapTaskFunc, this);

        private static readonly Func<object, TResult> InternalWrapTaskFunc = InternalWrapTask;
        private static TResult InternalWrapTask(object state)
        {
            var owner = (DefaultRunnable<TResult>)state;
            return owner._func();
        }
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
}
