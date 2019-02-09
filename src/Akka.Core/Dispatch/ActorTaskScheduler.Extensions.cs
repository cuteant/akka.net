using System;
using System.Threading.Tasks;

namespace Akka.Dispatch
{
    partial class ActorTaskScheduler
    {
        /// <summary>TBD</summary>
        public static void RunTask<TArg1>(Action<TArg1> action, TArg1 arg1)
        {
            RunTask(Runnable.Create(action, arg1));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1>(Func<TArg1, Task> asyncAction, TArg1 arg1)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            RunTask(Runnable.Create(action, arg1, arg2));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2>(Func<TArg1, TArg2, Task> asyncAction, TArg1 arg1, TArg2 arg2)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1, arg2));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            RunTask(Runnable.Create(action, arg1, arg2, arg3));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, Task> asyncAction, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1, arg2, arg3));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            RunTask(Runnable.Create(action, arg1, arg2, arg3, arg4));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, Task> asyncAction, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1, arg2, arg3, arg4));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            RunTask(Runnable.Create(action, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> asyncAction, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            RunTask(Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> asyncAction, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            RunTask(Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>TBD</summary>
        public static void RunTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> asyncAction, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            RunTask(Runnable.CreateTask(asyncAction, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

    }
}
