﻿//-----------------------------------------------------------------------
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
        /// <summary>
        /// TBD
        /// </summary>
        void Run();
    }

    public interface IArgumentOverrides<T>
    {
        void Run(T arg);
    }

    public interface IRunnable2 : IRunnable
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        RunnableTaskWrapper WrapTask();
    }

    /// <summary>An asynchronous operation</summary>
    public readonly struct RunnableTaskWrapper
    {
        public readonly Action<object> Task;
        public readonly object State;

        public RunnableTaskWrapper(Action<object> task, object state)
        {
            Task = task;
            State = state;
        }
    }

    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action"/>
    /// </summary>
    public sealed class ActionRunnable : IRunnable2
    {
        private readonly Action _action;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="action">TBD</param>
        public ActionRunnable(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _action();
        }

        public RunnableTaskWrapper WrapTask() => new RunnableTaskWrapper(InternalWrapTaskAction, this);

        private static readonly Action<object> InternalWrapTaskAction = InternalWrapTask;
        private static void InternalWrapTask(object state)
        {
            var owner = (ActionRunnable)state;
            owner._action();
        }
    }

    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{state}"/> and an <see cref="object"/> representing the state.
    /// </summary>
    public sealed class ActionWithStateRunnable : ActionWithStateRunnable<object>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="state">TBD</param>
        public ActionWithStateRunnable(Action<object> actionWithState, object state)
            : base(actionWithState, state) { }
    }
}

