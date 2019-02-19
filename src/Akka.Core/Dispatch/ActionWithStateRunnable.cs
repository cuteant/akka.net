using System;

namespace Akka/*.Dispatch*/
{
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1> _actionWithState;
        private readonly TArg1 _arg1;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        public ActionWithStateRunnable(Action<TArg1> actionWithState, TArg1 arg1)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg);
        }
    }
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1, TArg2}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1, TArg2> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1, TArg2> _actionWithState;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        public ActionWithStateRunnable(Action<TArg1, TArg2> actionWithState, TArg1 arg1, TArg2 arg2)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1, _arg2);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg, _arg2);
        }
    }
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1, TArg2, TArg3}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1, TArg2, TArg3> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1, TArg2, TArg3> _actionWithState;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        public ActionWithStateRunnable(Action<TArg1, TArg2, TArg3> actionWithState, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1, _arg2, _arg3);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg, _arg2, _arg3);
        }
    }
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1, TArg2, TArg3, TArg4}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1, TArg2, TArg3, TArg4> _actionWithState;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        public ActionWithStateRunnable(Action<TArg1, TArg2, TArg3, TArg4> actionWithState, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1, _arg2, _arg3, _arg4);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg, _arg2, _arg3, _arg4);
        }
    }
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1, TArg2, TArg3, TArg4, TArg5}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4, TArg5> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5> _actionWithState;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;
        private readonly TArg5 _arg5;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        public ActionWithStateRunnable(Action<TArg1, TArg2, TArg3, TArg4, TArg5> actionWithState, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1, _arg2, _arg3, _arg4, _arg5);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg, _arg2, _arg3, _arg4, _arg5);
        }
    }
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> _actionWithState;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;
        private readonly TArg5 _arg5;
        private readonly TArg6 _arg6;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        public ActionWithStateRunnable(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> actionWithState, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
            _arg6 = arg6;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg, _arg2, _arg3, _arg4, _arg5, _arg6);
        }
    }
    /// <summary>
    /// <see cref="IRunnable"/> which executes an <see cref="Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7}"/> representing the state.
    /// </summary>
    public class ActionWithStateRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : OverridingArgumentRunnable<TArg1>, IRunnable
    {
        private readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> _actionWithState;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;
        private readonly TArg5 _arg5;
        private readonly TArg6 _arg6;
        private readonly TArg7 _arg7;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actionWithState">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        public ActionWithStateRunnable(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> actionWithState, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            _actionWithState = actionWithState;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
            _arg6 = arg6;
            _arg7 = arg7;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void Run()
        {
            _actionWithState(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);
        }

        /// <inheritdoc />
        public override void Run(TArg1 arg)
        {
            _actionWithState(arg, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);
        }
    }
}
