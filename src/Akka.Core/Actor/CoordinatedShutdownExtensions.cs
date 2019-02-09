using System;
using System.Threading.Tasks;

namespace Akka.Actor
{
    public static class CoordinatedShutdownExtensions
    {
        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask(this CoordinatedShutdown coord, string phase, string taskName, Func<Task<Done>> task)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, Task<Done>> task, TArg1 arg1)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1, TArg2>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, TArg2, Task<Done>> task, TArg1 arg1, TArg2 arg2)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1, arg2));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1, TArg2, TArg3>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, TArg2, TArg3, Task<Done>> task, TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1, arg2, arg3));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1, TArg2, TArg3, TArg4>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, TArg2, TArg3, TArg4, Task<Done>> task, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1, arg2, arg3, arg4));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <typeparam name="TArg5">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1, TArg2, TArg3, TArg4, TArg5>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<Done>> task, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1, arg2, arg3, arg4, arg5));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <typeparam name="TArg5">TBD</typeparam>
        /// <typeparam name="TArg6">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<Done>> task, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1, arg2, arg3, arg4, arg5, arg6));

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
        /// <typeparam name="TArg1">TBD</typeparam>
        /// <typeparam name="TArg2">TBD</typeparam>
        /// <typeparam name="TArg3">TBD</typeparam>
        /// <typeparam name="TArg4">TBD</typeparam>
        /// <typeparam name="TArg5">TBD</typeparam>
        /// <typeparam name="TArg6">TBD</typeparam>
        /// <typeparam name="TArg7">TBD</typeparam>
        /// <param name="coord">TBD.</param>
        /// <param name="phase">The phase to add this task to.</param>
        /// <param name="taskName">The name of the task to add to this phase.</param>
        /// <param name="task">The delegate that produces a <see cref="Task"/> that will be executed.</param>
        /// <param name="arg1">TBD</param>
        /// <param name="arg2">TBD</param>
        /// <param name="arg3">TBD</param>
        /// <param name="arg4">TBD</param>
        /// <param name="arg5">TBD</param>
        /// <param name="arg6">TBD</param>
        /// <param name="arg7">TBD</param>
        /// <remarks>
        /// Tasks should typically be registered as early as possible after system
        /// startup. When running the <see cref="CoordinatedShutdown"/> tasks that have been
        /// registered will be performed but tasks that are added too late will not be run.
        ///
        ///
        /// It is possible to add a task to a later phase from within a task in an earlier phase
        /// and it will be performed.
        /// </remarks>
        public static void AddTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this CoordinatedShutdown coord, string phase, string taskName, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<Done>> task, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
            => coord.AddTask(phase, taskName, Runnable.CreateTask(task, arg1, arg2, arg3, arg4, arg5, arg6, arg7));

    }
}
