using System;

namespace Akka.Dispatch
{
    public static class AbstractDispatcherExtensions
    {
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule(this MessageDispatcher dispatcher, Action run)
        {
            dispatcher.Schedule(Runnable.Create(run));
        }

        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1>(this MessageDispatcher dispatcher, Action<TArg1> run, TArg1 arg1)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1));
        }
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1, TArg2>(this MessageDispatcher dispatcher, Action<TArg1, TArg2> run, TArg1 arg1, TArg2 arg2)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1, arg2));
        }
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1, TArg2, TArg3>(this MessageDispatcher dispatcher, Action<TArg1, TArg2, TArg3> run, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1, arg2, arg3));
        }
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1, TArg2, TArg3, TArg4>(this MessageDispatcher dispatcher, Action<TArg1, TArg2, TArg3, TArg4> run, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1, arg2, arg3, arg4));
        }
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1, TArg2, TArg3, TArg4, TArg5>(this MessageDispatcher dispatcher, Action<TArg1, TArg2, TArg3, TArg4, TArg5> run, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1, arg2, arg3, arg4, arg5));
        }
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this MessageDispatcher dispatcher, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> run, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1, arg2, arg3, arg4, arg5, arg6));
        }
        /// <summary>Schedules the specified delegate.</summary>
        public static void Schedule<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this MessageDispatcher dispatcher, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> run, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            dispatcher.Schedule(Runnable.Create(run, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }
    }
}
