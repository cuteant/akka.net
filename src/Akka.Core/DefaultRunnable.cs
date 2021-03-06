﻿using System;

namespace Akka
{
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TResult> _func;
        private readonly TArg1 _arg1;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        public DefaultRunnable(Func<TArg1, TResult> func, TArg1 arg1)
        {
            _func = func;
            _arg1 = arg1;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg);
        }
    }
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TArg2, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TArg2, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TArg2, TResult> _func;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        public DefaultRunnable(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2)
        {
            _func = func;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1, _arg2);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg, _arg2);
        }
    }
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TArg2, TArg3, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TArg2, TArg3, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TArg2, TArg3, TResult> _func;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        public DefaultRunnable(Func<TArg1, TArg2, TArg3, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            _func = func;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1, _arg2, _arg3);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg, _arg2, _arg3);
        }
    }
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TArg2, TArg3, TArg4, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TArg2, TArg3, TArg4, TResult> _func;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        public DefaultRunnable(Func<TArg1, TArg2, TArg3, TArg4, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            _func = func;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1, _arg2, _arg3, _arg4);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg, _arg2, _arg3, _arg4);
        }
    }
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TArg2, TArg3, TArg4, TArg5, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> _func;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;
        private readonly TArg5 _arg5;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        public DefaultRunnable(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            _func = func;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1, _arg2, _arg3, _arg4, _arg5);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg, _arg2, _arg3, _arg4, _arg5);
        }
    }
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> _func;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;
        private readonly TArg5 _arg5;
        private readonly TArg6 _arg6;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        public DefaultRunnable(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            _func = func;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
            _arg6 = arg6;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg, _arg2, _arg3, _arg4, _arg5, _arg6);
        }
    }
    /// <summary><see cref="IRunnable{TResult}"/> which executes an <see cref="Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult}"/>.</summary>
    public class DefaultRunnable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> : OverridingArgumentRunnable<TArg1, TResult>, IRunnable<TResult>
    {
        private readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> _func;
        private readonly TArg1 _arg1;
        private readonly TArg2 _arg2;
        private readonly TArg3 _arg3;
        private readonly TArg4 _arg4;
        private readonly TArg5 _arg5;
        private readonly TArg6 _arg6;
        private readonly TArg7 _arg7;

        /// <summary>TBD</summary>
        /// <param name="func">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        public DefaultRunnable(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            _func = func;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
            _arg6 = arg6;
            _arg7 = arg7;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public TResult Run()
        {
            return _func(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);
        }

        /// <inheritdoc />
        public override TResult Run(TArg1 arg)
        {
            return _func(arg, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);
        }
    }
}
