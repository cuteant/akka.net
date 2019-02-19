//-----------------------------------------------------------------------
// <copyright file="IRunnable.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka/*.Dispatch*/
{
    /// <summary>
    /// An asynchronous operation will be executed by a <see cref="Akka.Dispatch.MessageDispatcher"/>.
    /// </summary>
    public interface IRunnable
    {
        /// <summary>TBD</summary>
        void Run();
    }

    /// <summary>TBD</summary>
    public interface IOverridingArgumentRunnable<T>
    {
        /// <summary>TBD</summary>
        void Run(T arg);
    }

    /// <summary>TBD</summary>
    public abstract class OverridingArgumentRunnable<T> : IOverridingArgumentRunnable<T>
    {
        /// <summary>TBD</summary>
        public abstract void Run(T arg);
    }

    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action"/>
    /// </summary>
    public sealed class ActionRunnable : IRunnable
    {
        private readonly Action _action;

        /// <summary>TBD</summary>
        /// <param name="action">TBD</param>
        public ActionRunnable(Action action) => _action = action;

        /// <summary>TBD</summary>
        public void Run() => _action();
    }

    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{state}"/> and an <see cref="object"/> representing the state.
    /// </summary>
    public sealed class ActionWithStateRunnable : ActionWithStateRunnable<object>
    {
        /// <summary>TBD</summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="state">TBD</param>
        public ActionWithStateRunnable(Action<object> actionWithState, object state)
            : base(actionWithState, state) { }
    }
}

