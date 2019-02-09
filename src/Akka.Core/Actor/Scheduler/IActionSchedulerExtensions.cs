using System;

namespace Akka.Actor
{
    public static class IActionSchedulerExtensions
    {
        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1> action, TArg1 arg1, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1> action, TArg1 arg1, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1> action, TArg1 arg1)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1> action, TArg1 arg1)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1> action, TArg1 arg1, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1> action, TArg1 arg1, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1> action, TArg1 arg1)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1> action, TArg1 arg1)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1, TArg2>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1, TArg2>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4, TArg5>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable that can be used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleOnce<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, ICancelable cancelable = null)
        {
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="millisecondsDelay">The time in milliseconds that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, int millisecondsDelay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(TimeSpan.FromMilliseconds(millisecondsDelay), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        public static ICancelable ScheduleOnceCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, TimeSpan delay, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleOnce(delay, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <param name="cancelable">A cancelable used to cancel the action from being executed.</param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <param name="cancelable">OPTIONAL. A cancelable used to cancel the action from being executed. Defaults to <c>null</c></param>
        public static void ScheduleRepeatedly<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, ICancelable cancelable = null)
        {
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialMillisecondsDelay">The time in milliseconds that has to pass before first invocation of the action.</param>
        /// <param name="millisecondsInterval">The time in milliseconds that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, int initialMillisecondsDelay, int millisecondsInterval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(TimeSpan.FromMilliseconds(initialMillisecondsDelay), TimeSpan.FromMilliseconds(millisecondsInterval), Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
            return cancelable;
        }

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="scheduler">The scheduler used to schedule the invocation of the action.</param>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        public static ICancelable ScheduleRepeatedlyCancelable<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IActionScheduler scheduler, TimeSpan initialDelay, TimeSpan interval, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            var cancelable = new Cancelable(scheduler);
            scheduler.ScheduleRepeatedly(initialDelay, interval, Runnable.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancelable);
            return cancelable;
        }

    }
}
