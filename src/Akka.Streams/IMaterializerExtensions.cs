using System;
using Akka.Actor;

namespace Akka.Streams
{
    public static class IMaterializerExtensions
    {
        /// <summary>TBD</summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <returns>TBD</returns>
        public static ICancelable ScheduleOnce(this IMaterializer materializer, TimeSpan delay, Action action)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action));
        }

        /// <summary>TBD</summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <returns>TBD</returns>
        public static ICancelable ScheduleRepeatedly(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action action)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1>(this IMaterializer materializer, TimeSpan delay, Action<TArg1> action, TArg1 arg1)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1> action, TArg1 arg1)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1, TArg2>(this IMaterializer materializer, TimeSpan delay, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1, TArg2>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1, TArg2, TArg3>(this IMaterializer materializer, TimeSpan delay, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1, TArg2, TArg3>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1, TArg2, TArg3, TArg4>(this IMaterializer materializer, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5>(this IMaterializer materializer, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IMaterializer materializer, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// single task with the given delay.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="delay">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IMaterializer materializer, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return materializer.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// Interface for stages that need timer services for their functionality. Schedules a
        /// repeated task with the given interval between invocations.
        /// </summary>
        /// <param name="materializer">TBD</param>
        /// <param name="initialDelay">TBD</param>
        /// <param name="interval">TBD</param>
        /// <param name="action">TBD</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <returns>
        /// A <see cref="ICancelable"/> that allows cancelling the timer. Cancelling is best effort, 
        /// if the event has been already enqueued it will not have an effect.
        /// </returns>
        public static ICancelable ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IMaterializer materializer, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return materializer.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

    }
}
