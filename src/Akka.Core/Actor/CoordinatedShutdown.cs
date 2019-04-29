﻿//-----------------------------------------------------------------------
// <copyright file="CoordinatedShutdown.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Event;
using Akka.Util;
using Akka.Util.Internal;
using static Akka.Pattern.FutureTimeoutSupport;
using static Akka.Util.Internal.TaskEx;

namespace Akka.Actor
{
    /// <summary>
    /// Used to register the <see cref="CoordinatedShutdown"/> extension with a given <see cref="ActorSystem"/>.
    /// </summary>
    public sealed class CoordinatedShutdownExtension : ExtensionIdProvider<CoordinatedShutdown>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CoordinatedShutdown"/> extension.
        /// </summary>
        /// <param name="system">The extended actor system.</param>
        /// <returns>A coordinated shutdown plugin.</returns>
        public override CoordinatedShutdown CreateExtension(ExtendedActorSystem system)
        {
            var conf = system.Settings.Config.GetConfig("akka.coordinated-shutdown");
            var phases = CoordinatedShutdown.PhasesFromConfig(conf);
            var coord = new CoordinatedShutdown(system, phases);
            CoordinatedShutdown.InitPhaseActorSystemTerminate(system, conf, coord);
            CoordinatedShutdown.InitClrHook(system, conf, coord);
            return coord;
        }
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal sealed class Phase
    {
        /// <summary>
        /// Creates a new <see cref="Phase"/>
        /// </summary>
        /// <param name="dependsOn">The list of other phases this phase depends upon.</param>
        /// <param name="timeout">A timeout value for any tasks running during this phase.</param>
        /// <param name="recover">When set to <c>true</c>, this phase can recover from a faulted state during shutdown.</param>
        public Phase(ImmutableHashSet<string> dependsOn, TimeSpan timeout, bool recover)
        {
            DependsOn = dependsOn ?? ImmutableHashSet<string>.Empty;
            Timeout = timeout;
            Recover = recover;
        }

        /// <summary>
        /// The names of other <see cref="Phase"/>s this phase depends upon.
        /// </summary>
        public ImmutableHashSet<string> DependsOn { get; }

        /// <summary>
        /// The amount of time this phase is allowed to run.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// If <c>true</c>, this phase has the ability to recover during a faulted state.
        /// </summary>
        public bool Recover { get; }

        private bool Equals(Phase other)
        {
            return DependsOn.SetEquals(other.DependsOn)
                && Timeout.Equals(other.Timeout)
                && Recover == other.Recover;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Phase phase && Equals(phase);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DependsOn?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ Timeout.GetHashCode();
                hashCode = (hashCode * 397) ^ Recover.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"DependsOn=[{string.Join(",", DependsOn)}], Timeout={Timeout}, Recover={Recover}";
        }
    }

    /// <summary>
    /// An <see cref="ActorSystem"/> extension used to help coordinate and sequence shutdown activities
    /// during graceful termination of actor systems, plugins, and so forth.
    /// </summary>
    public sealed class CoordinatedShutdown : IExtension
    {
        /// <summary>
        /// Initializes a new <see cref="CoordinatedShutdown"/> instance.
        /// </summary>
        /// <param name="system">Access to the <see cref="ExtendedActorSystem"/>.</param>
        /// <param name="phases">The list of <see cref="Phase"/>s provided by the HOCON configuration.</param>
        internal CoordinatedShutdown(ExtendedActorSystem system, Dictionary<string, Phase> phases)
        {
            System = system;
            Phases = phases;
            Log = Logging.GetLogger(System, GetType());
            _knownPhases = new HashSet<string>(Phases.Keys.Concat(Phases.Values.SelectMany(x => x.DependsOn)), StringComparer.Ordinal);
            OrderedPhases = TopologicalSort(Phases);
        }

        /// <summary>
        /// Retrieves the <see cref="CoordinatedShutdown"/> extension for the current <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="sys">The current actor system.</param>
        /// <returns>A <see cref="CoordinatedShutdown"/> instance.</returns>
        public static CoordinatedShutdown Get(ActorSystem sys)
        {
            return sys.WithExtension<CoordinatedShutdown, CoordinatedShutdownExtension>();
        }

        public const string PhaseBeforeServiceUnbind = "before-service-unbind";
        public const string PhaseServiceUnbind = "service-unbind";
        public const string PhaseServiceRequestsDone = "service-requests-done";
        public const string PhaseServiceStop = "service-stop";
        public const string PhaseBeforeClusterShutdown = "before-cluster-shutdown";
        public const string PhaseClusterShardingShutdownRegion = "cluster-sharding-shutdown-region";
        public const string PhaseClusterLeave = "cluster-leave";
        public const string PhaseClusterExiting = "cluster-exiting";
        public const string PhaseClusterExitingDone = "cluster-exiting-done";
        public const string PhaseClusterShutdown = "cluster-shutdown";
        public const string PhaseBeforeActorSystemTerminate = "before-actor-system-terminate";
        public const string PhaseActorSystemTerminate = "actor-system-terminate";

        /// <summary>
        /// Reason for the shutdown, which can be used by tasks in case they need to do
        /// different things depending on what caused the shutdown. There are some
        /// predefined reasons, but external libraries applications may also define
        /// other reasons.
        /// </summary>
        public class Reason : ISingletonMessage
        {
            protected Reason() { }
        }

        /// <summary>
        /// The reason for the shutdown was unknown. Needed for backwards compatibility.
        /// </summary>
        public class UnknownReason : Reason
        {
            public static readonly Reason Instance = new UnknownReason();

            private UnknownReason() { }
        }

        /// <summary>
        /// The shutdown was initiated by a CLR shutdown hook
        /// </summary>
        public class ClrExitReason : Reason
        {
            public static readonly Reason Instance = new ClrExitReason();

            private ClrExitReason() { }
        }


        /// <summary>
        /// The shutdown was initiated by Cluster downing.
        /// </summary>
        public class ClusterDowningReason : Reason
        {
            public static readonly Reason Instance = new ClusterDowningReason();

            private ClusterDowningReason() { }
        }


        /// <summary>
        /// The shutdown was initiated by Cluster leaving.
        /// </summary>
        public class ClusterLeavingReason : Reason
        {
            public static readonly Reason Instance = new ClusterLeavingReason();

            private ClusterLeavingReason() { }
        }

        /// <summary>
        /// The <see cref="ActorSystem"/>
        /// </summary>
        public ExtendedActorSystem System { get; }

        /// <summary>
        /// The set of named <see cref="Phase"/>s that will be executed during coordinated shutdown.
        /// </summary>
        internal Dictionary<string, Phase> Phases { get; }

        /// <summary>
        /// INTERNAL API
        /// </summary>
        internal ILoggingAdapter Log { get; }

        private readonly HashSet<string> _knownPhases;

        /// <summary>
        /// INTERNAL API
        /// </summary>
        internal readonly List<string> OrderedPhases;

        private readonly ConcurrentBag<Func<Task<Done>>> _clrShutdownTasks = new ConcurrentBag<Func<Task<Done>>>();
        private readonly ConcurrentDictionary<string, ImmutableList<Tuple<string, IRunnableTask<Done>>>> _tasks = new ConcurrentDictionary<string, ImmutableList<Tuple<string, IRunnableTask<Done>>>>(StringComparer.Ordinal);
        private readonly AtomicReference<Reason> _runStarted = new AtomicReference<Reason>(null);
        private readonly AtomicBoolean _clrHooksStarted = new AtomicBoolean(false);
        private readonly TaskCompletionSource<Done> _runPromise = new TaskCompletionSource<Done>();
        private readonly TaskCompletionSource<Done> _hooksRunPromise = new TaskCompletionSource<Done>();

        private int _runningClrHook = Constants.False;
        private bool RunningClrHook
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Constants.True == Volatile.Read(ref _runningClrHook);
            set => Interlocked.Exchange(ref _runningClrHook, value ? Constants.True : Constants.False);
        }

        /// <summary>
        /// INTERNAL API
        ///
        /// Signals when CLR shutdown hooks have been completed
        /// </summary>
        internal Task<Done> ClrShutdownTask => _hooksRunPromise.Task;

        /// <summary>
        /// Add a task to a phase. It doesn't remove previously added tasks.
        ///
        /// Tasks added to the same phase are executed in parallel without any
        /// ordering assumptions. Next phase will not start until all tasks of
        /// previous phase have completed.
        /// </summary>
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
        public void AddTask(string phase, string taskName, IRunnableTask<Done> task)
        {
            if (!_knownPhases.Contains(phase))
            {
                AkkaThrowHelper.ThrowConfigurationException_UnknownPhase(phase, _knownPhases);
            }

            if (!_tasks.TryGetValue(phase, out var current))
            {
                if (!_tasks.TryAdd(phase, ImmutableList<Tuple<string, IRunnableTask<Done>>>.Empty.Add(Tuple.Create(taskName, task))))
                    AddTask(phase, taskName, task); // CAS failed, retry
            }
            else
            {
                if (!_tasks.TryUpdate(phase, current.Add(Tuple.Create(taskName, task)), current))
                    AddTask(phase, taskName, task); // CAS failed, retry
            }
        }

        /// <summary>
        /// Add a shutdown hook that will execute when the CLR process begins
        /// its shutdown sequence, invoked via <see cref="AppDomain.ProcessExit"/>.
        ///
        /// Added hooks may run in any order concurrently, but they are run before
        /// the Akka.NET internal shutdown hooks execute.
        /// </summary>
        /// <param name="hook">A task that will be executed during shutdown.</param>
        internal void AddClrShutdownHook(Func<Task<Done>> hook)
        {
            if (!_clrHooksStarted)
            {
                _clrShutdownTasks.Add(hook);
            }
        }


        /// <summary>
        /// INTERNAL API
        ///
        /// Should only be called directly by the <see cref="AppDomain.ProcessExit"/> event
        /// in production.
        ///
        /// Safe to call multiple times, but hooks will only be run once.
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> that will be completed once the process exits.</returns>
        private Task<Done> RunClrHooks()
        {
            if (_clrHooksStarted.CompareAndSet(false, true))
            {
                Task.WhenAll(_clrShutdownTasks.Select(hook =>
                {
                    try
                    {
                        var t = hook();
                        return t;
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorOccurredWhileExecutingCLRShutdownHook(ex);
                        return TaskEx.FromException<Done>(ex);
                    }
                })).ContinueWith(AfterRunClrHooksAction, _hooksRunPromise);
            }

            return ClrShutdownTask;
        }
        private static readonly Action<Task<Done[]>, object> AfterRunClrHooksAction = AfterRunClrHooks;
        private static void AfterRunClrHooks(Task<Done[]> tr, object state)
        {
            var hooksRunPromise = (TaskCompletionSource<Done>)state;
            if (tr.IsSuccessfully())
            {
                hooksRunPromise.TrySetResult(Done.Instance);
            }
            else
            {
                hooksRunPromise.TrySetException(tr.Exception.InnerExceptions);
            }
        }

        /// <summary>
        /// The <see cref="Reason"/> for the shutdown as passed to the <see cref="Run(Reason, string)"/> method. <see langword="null"/> if the shutdown
        /// has not been started.
        /// </summary>
        public Reason ShutdownReason => _runStarted.Value;

        /// <summary>
        /// Run tasks of all phases including and after the given phase.
        /// </summary>
        /// <param name="fromPhase">Optional. The phase to start the run from.</param>
        /// <returns>A task that is completed when all such tasks have been completed, or
        /// there is failure when <see cref="Phase.Recover"/> is disabled.</returns>
        /// <remarks>
        /// It is safe to call this method multiple times. It will only run the shutdown sequence once.
        /// </remarks>
        [Obsolete("Use the method with 'reason' parameter instead")]
        public Task<Done> Run(string fromPhase = null)
        {
            return Run(UnknownReason.Instance, fromPhase);
        }

        /// <summary>
        /// Run tasks of all phases including and after the given phase.
        /// </summary>
        /// <param name="reason">Reason of the shutdown</param>
        /// <param name="fromPhase">Optional. The phase to start the run from.</param>
        /// <returns>A task that is completed when all such tasks have been completed, or
        /// there is failure when <see cref="Phase.Recover"/> is disabled.</returns>
        /// <remarks>
        /// It is safe to call this method multiple times. It will only run the shutdown sequence once.
        /// </remarks>
        public Task<Done> Run(Reason reason, string fromPhase = null)
        {
            if (_runStarted.CompareAndSet(null, reason))
            {
                var runningPhases = (fromPhase == null
                    ? OrderedPhases // all
                    : OrderedPhases.From(fromPhase)).ToList();

                var done = Loop(this, runningPhases);

                done.LinkOutcome(LoopContinuationAction, _runPromise);
            }
            return _runPromise.Task;
        }

        private static readonly Action<Task<Done>, TaskCompletionSource<Done>> LoopContinuationAction = LoopContinuation;
        private static void LoopContinuation(Task<Done> tr, TaskCompletionSource<Done> runPromise)
        {
            if (tr.IsSuccessfully())
            {
                runPromise.SetResult(tr.Result);
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                runPromise.TrySetException(tr.Exception.InnerExceptions);
            }
        }

        private static Task<Done> Loop(CoordinatedShutdown owner, List<string> remainingPhases)
        {
            var phase = remainingPhases.FirstOrDefault();
            if (phase == null) { return TaskEx.Completed; }
            var remaining = remainingPhases.Skip(1).ToList();
            Task<Done> phaseResult = null;
            var log = owner.Log;
            var debugEnabled = log.IsDebugEnabled;
            if (!owner._tasks.TryGetValue(phase, out var phaseTasks))
            {
                if (debugEnabled) { log.PerformingPhaseWithTasks(phase); }
                phaseResult = TaskEx.Completed;
            }
            else
            {
                if (debugEnabled) { log.PerformingPhaseWithTasks(phase, phaseTasks); }

                // note that tasks within same phase are performed in parallel
                var phaseValue = owner.Phases[phase];
                var recoverEnabled = phaseValue.Recover;
                var result = Task
                    .WhenAll<Done>(phaseTasks.Select(x =>
                    {
                        var taskName = x.Item1;
                        var task = x.Item2;
                        try
                        {
                            // need to begin execution of task
                            var r = task.Run();

                            if (recoverEnabled)
                            {
                                return r.LinkOutcome(AfterRunPhaseTaskFunc, log, taskName, phase);
                            }

                            return r;
                        }
                        catch (Exception ex)
                        {
                            // in case task.Start() throws
                            if (recoverEnabled)
                            {
                                log.TaskFailedInPhase(taskName, phase, ex);
                                return TaskEx.Completed;
                            }

                            return TaskEx.FromException<Done>(ex);
                        }
                    }))
                    .ContinueWith(AfterRunPhaseTasksFunc);

                Task<Done> timeoutFunction = null;
                try
                {
                    timeoutFunction = After(phaseValue.Timeout, owner.System.Scheduler, InvokeTimeoutFunc, owner, result, phase, phaseValue);
                }
                catch (SchedulerException)
                {
                    // The call to `after` threw SchedulerException, triggered by system termination
                    timeoutFunction = result;
                }
                catch (InvalidOperationException)
                {
                    // The call to `after` threw SchedulerException, triggered by Scheduler being in unset state
                    timeoutFunction = result;
                }

                phaseResult = Task.WhenAny<Done>(result, timeoutFunction).FastUnwrap();
            }

            if (remaining.Count <= 0) { return phaseResult; }
            return phaseResult.LinkOutcome(InvokeLoopFunc, owner, remaining);
        }

        private static readonly Func<Task<Done>, ILoggingAdapter, string, string, Done> AfterRunPhaseTaskFunc = AfterRunPhaseTask;
        private static Done AfterRunPhaseTask(Task<Done> tr, ILoggingAdapter log, string taskName, string phase)
        {
            if (!tr.IsSuccessfully())
            {
                log.TaskFailedInPhase(taskName, phase, tr);
            }
            return Done.Instance;
        }

        private static readonly Func<Task<Done[]>, Done> AfterRunPhaseTasksFunc = AfterRunPhaseTasks;
        private static Done AfterRunPhaseTasks(Task<Done[]> tr)
        {
            // forces downstream error propagation if recover is disabled
            var force = tr.Result;
            return Done.Instance;
        }

        private static readonly Func<CoordinatedShutdown, Task<Done>, string, Phase, Task<Done>> InvokeTimeoutFunc = InvokeTimeout;
        private static Task<Done> InvokeTimeout(CoordinatedShutdown owner, Task<Done> result, string phase, Phase phaseValue)
        {
            var timeout = phaseValue.Timeout;
            var deadLine = MonotonicClock.Elapsed + timeout;

            if (phase == CoordinatedShutdown.PhaseActorSystemTerminate && MonotonicClock.ElapsedHighRes < deadLine)
            {
                return result; // too early, i.e. triggered by system termination
            }

            if (result.IsCompleted) { return TaskEx.Completed; }

            // note that tasks within same phase are performed in parallel
            var recoverEnabled = phaseValue.Recover;
            if (recoverEnabled)
            {
                var log = owner.Log;
                if (log.IsWarningEnabled) log.CoordinatedShutdownPhaseTimedOutAfter(phase, timeout);
                return TaskEx.Completed;
            }

            return TaskEx.FromException<Done>(new TimeoutException($"Coordinated shutdown phase[{phase}] timed out after {timeout}"));
        }

        private static readonly Func<Task<Done>, CoordinatedShutdown, List<string>, Task<Done>> InvokeLoopFunc = InvokeLoop;
        private static Task<Done> InvokeLoop(Task<Done> tr, CoordinatedShutdown owner, List<string> remaining)
        {
            // force any exceptions to be rethrown so next phase stops
            // and so failure gets propagated back to caller
            var r = tr.Result;
            return Loop(owner, remaining);
        }

        /// <summary>
        /// The configured timeout for a given <see cref="Phase"/>.
        /// </summary>
        /// <param name="phase">The name of the phase.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="phase"/> doesn't exist in the set of registered phases.</exception>
        /// <returns>Returns the timeout if ti exists.</returns>
        public TimeSpan Timeout(string phase)
        {
            if (!Phases.TryGetValue(phase, out var p))
            {
                AkkaThrowHelper.ThrowArgumentException_CoordinatedShutdownTimeout(phase);
            }
            return p.Timeout;
        }

        /// <summary>
        /// The sum of timeouts of all phases that have some task.
        /// </summary>
        public TimeSpan TotalTimeout
        {
            get { return _tasks.Keys.Aggregate(TimeSpan.Zero, (span, s) => span.Add(Timeout(s))); }
        }

        /// <summary>
        /// INTERNAL API
        /// </summary>
        /// <param name="config">The HOCON configuration for the <see cref="CoordinatedShutdown"/></param>
        /// <returns>A map of all of the phases of the shutdown.</returns>
        internal static Dictionary<string, Phase> PhasesFromConfig(Config config)
        {
            var defaultPhaseTimeout = config.GetString("default-phase-timeout");
            var phasesConf = config.GetConfig("phases");
            var defaultPhaseConfig = ConfigurationFactory.ParseString($"timeout = {defaultPhaseTimeout}" + @"
                recover = true
                depends-on = []
            ");

            return phasesConf.Root.GetObject().Unwrapped.ToDictionary(x => x.Key, v =>
             {
                 var c = phasesConf.GetConfig(v.Key).WithFallback(defaultPhaseConfig);
                 var dependsOn = c.GetStringList("depends-on").ToImmutableHashSet(StringComparer.Ordinal);
                 var timeout = c.GetTimeSpan("timeout", allowInfinite: false);
                 var recover = c.GetBoolean("recover");
                 return new Phase(dependsOn, timeout, recover);
             }, StringComparer.Ordinal);
        }

        /// <summary>
        /// INTERNAL API: https://en.wikipedia.org/wiki/Topological_sorting
        /// </summary>
        /// <param name="phases">The set of phases to sort.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when a cycle is detected in the phase graph.
        /// The graph must be a directed acyclic graph (DAG).
        /// </exception>
        /// <returns>A topologically sorted list of phases.</returns>
        internal static List<string> TopologicalSort(Dictionary<string, Phase> phases)
        {
            var result = new List<string>();
            // in case dependent phase is not defined as key
            var unmarked = new HashSet<string>(phases.Keys.Concat(phases.Values.SelectMany(x => x.DependsOn)), StringComparer.Ordinal);
            var tempMark = new HashSet<string>(StringComparer.Ordinal); // for detecting cycles

            void DepthFirstSearch(string u)
            {
                if (tempMark.Contains(u)) { AkkaThrowHelper.ThrowArgumentException_CoordinatedShutdownSort(u, phases); }
                if (unmarked.Contains(u))
                {
                    tempMark.Add(u);
                    if (phases.TryGetValue(u, out var p) && p.DependsOn.Count > 0)
                        p.DependsOn.ForEach(DepthFirstSearch);
                    unmarked.Remove(u); //permanent mark
                    tempMark.Remove(u);
                    result = new[] { u }.Concat(result).ToList();
                }
            }

            while (unmarked.Count > 0)
            {
                DepthFirstSearch(unmarked.Head());
            }

            result.Reverse();
            return result;
        }

        /// <summary>
        /// INTERNAL API
        ///
        /// Primes the <see cref="CoordinatedShutdown"/> with the default phase for
        /// <see cref="ActorSystem.Terminate"/>
        /// </summary>
        /// <param name="system">The actor system for this extension.</param>
        /// <param name="conf">The HOCON configuration.</param>
        /// <param name="coord">The <see cref="CoordinatedShutdown"/> plugin instance.</param>
        internal static void InitPhaseActorSystemTerminate(ActorSystem system, Config conf, CoordinatedShutdown coord)
        {
            var terminateActorSystem = conf.GetBoolean("terminate-actor-system");
            var exitClr = conf.GetBoolean("exit-clr");
            if (terminateActorSystem || exitClr)
            {
                coord.AddTask(PhaseActorSystemTerminate, "terminate-system",
                    Runnable.CreateTask(InvokeTerminateSystemAction, system, coord, terminateActorSystem, exitClr));
            }
        }

        private static readonly Func<ActorSystem, CoordinatedShutdown, bool, bool, Task<Done>> InvokeTerminateSystemAction = InvokeTerminateSystem;
        private static Task<Done> InvokeTerminateSystem(ActorSystem system, CoordinatedShutdown coord, bool terminateActorSystem, bool exitClr)
        {
            if (exitClr && terminateActorSystem)
            {
                // In case ActorSystem shutdown takes longer than the phase timeout,
                // exit the JVM forcefully anyway.

                // We must spawn a separate Task to not block current thread,
                // since that would have blocked the shutdown of the ActorSystem.
                var timeout = coord.Timeout(PhaseActorSystemTerminate);
                return Task.Run(() =>
                {
                    if (!system.WhenTerminated.Wait(timeout) && !coord.RunningClrHook)
                    {
                        Environment.Exit(0);
                    }
                    return Done.Instance;
                });
            }

            if (terminateActorSystem)
            {
                return system.Terminate().LinkOutcome(AfterSystemTerminateFunc, exitClr, coord.RunningClrHook);
            }
            else if (exitClr)
            {
                Environment.Exit(0);
                return TaskEx.Completed;
            }
            else
            {
                return TaskEx.Completed;
            }
        }

        private static readonly Func<Task, bool, bool, Done> AfterSystemTerminateFunc = AfterSystemTerminate;
        private static Done AfterSystemTerminate(Task tr, bool exitClr, bool runningClrHook)
        {
            if (exitClr && !runningClrHook)
            {
                Environment.Exit(0);
            }
            return Done.Instance;
        }

        /// <summary>
        /// Initializes the CLR hook
        /// </summary>
        /// <param name="system">The actor system for this extension.</param>
        /// <param name="conf">The HOCON configuration.</param>
        /// <param name="coord">The <see cref="CoordinatedShutdown"/> plugin instance.</param>
        internal static void InitClrHook(ActorSystem system, Config conf, CoordinatedShutdown coord)
        {
            var runByClrShutdownHook = conf.GetBoolean("run-by-clr-shutdown-hook");
            if (runByClrShutdownHook)
            {
#if APPDOMAIN
                // run all hooks during termination sequence
                AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                {
                    // have to block, because if this method exits the process exits.
                    coord.RunClrHooks().Wait(coord.TotalTimeout);
                };
#else
                // TODO: what to do for NetCore?
#endif
                coord.AddClrShutdownHook(() =>
                {
                    coord.RunningClrHook = true;
                    return Task.Run(() =>
                    {
                        if (!system.WhenTerminated.IsCompleted)
                        {
                            var coordLog = coord.Log;
                            if (coordLog.IsInfoEnabled) coordLog.StartingCoordinatedShutdownFromCLRTerminationHook();
                            try
                            {
                                coord.Run(ClrExitReason.Instance).Wait(coord.TotalTimeout);
                            }
                            catch (Exception ex)
                            {
                                if (coordLog.IsWarningEnabled) coordLog.CoordinatedShutdownFromCLRShutdownFailed(ex);
                            }
                        }
                        return Done.Instance;
                    });
                });
            }
        }
    }
}