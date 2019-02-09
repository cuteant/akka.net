// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Routing;

namespace Akka
{
    internal static class CoreLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DisposingSystem(this ILoggingAdapter logger)
        {
            logger.Debug("Disposing system");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SystemShutdownInitiated(this ILoggingAdapter logger)
        {
            logger.Debug("System shutdown initiated");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResolveOfUnknownPathFailed(this ILoggingAdapter logger, string path)
        {
            logger.Debug("Resolve of unknown path [{0}] failed. Invalid format.", path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResolveOfForeignActorPathFailed(this ILoggingAdapter logger, ActorPath path)
        {
            logger.Debug("Resolve of foreign ActorPath [{0}] failed", path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResolveOfEmptyPathSequenceFailsPerDefinition(this ILoggingAdapter logger)
        {
            logger.Debug("Resolve of empty path sequence fails (per definition)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResolveOfPathSequenceFailed(this ILoggingAdapter logger, IReadOnlyCollection<string> pathElements)
        {
            logger.Debug("Resolve of path sequence [/{0}] failed", ActorPath.FormatPathElements(pathElements));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TerminatingOnRestartWhichExceedsMaxAllowedRestarts(this ILoggingAdapter logger, int nextRestartCount, int maxNumberOfRetries)
        {
            logger.Debug($"Terminating on restart #{0} which exceeds max allowed restarts ({1})", nextRestartCount, maxNumberOfRetries);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TerminatingBecauseChildTerminatedItself(this ILoggingAdapter logger, IActorRef child)
        {
            logger.Debug($"Terminating, because child {child} terminated itself");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TransitionState(this ILoggingAdapter logger, object oldState, object nextState)
        {
            logger.Debug("transition {0} -> {1}", oldState, nextState);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProcessingFsmEventFromInState(this ILoggingAdapter logger, object fsmEvent, string srcStr, object StateName)
        {
            logger.Debug("processing {0} from {1} in state {2}", fsmEvent, srcStr, StateName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CancellingTimer(this ILoggingAdapter logger, string name)
        {
            logger.Debug($"Cancelling timer {name}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SettingRepeatingTimer(this ILoggingAdapter logger, string name, object msg, TimeSpan timeout, bool repeat)
        {
            logger.Debug($"setting {(repeat ? "repeating" : "")} timer {name}/{timeout}: {msg}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PerformingPhaseWithTasks(this ILoggingAdapter logger, string phase, ImmutableList<Tuple<string, IRunnableTask<Done>>> phaseTasks)
        {
            logger.Debug("Performing phase [{0}] with [{1}] tasks: [{2}]", phase, phaseTasks.Count, string.Join(",", phaseTasks.Select(x => x.Item1)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PerformingPhaseWithTasks(this ILoggingAdapter logger, string phase)
        {
            logger.Debug("Performing phase [{0}] with [0] tasks.", phase);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingCoordinatedShutdownFromCLRTerminationHook(this ILoggingAdapter logger)
        {
            logger.Info("Starting coordinated shutdown from CLR termination hook.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogConfigOnStart(this ILoggingAdapter logger, Settings settings)
        {
            logger.Info(settings.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheTypeNameForSerializerDidNotResolveToAnActualType(this ILoggingAdapter logger, string serializerKey, string serializerTypeName)
        {
            logger.Warning("The type name for serializer '{0}' did not resolve to an actual Type: '{1}'", serializerKey, serializerTypeName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheTypeNameForMessageAndSerializerBindingDidNotResolveToAnActualType(this ILoggingAdapter logger, string serializerName, string typename)
        {
            logger.Warning("The type name for message/serializer binding '{0}' did not resolve to an actual Type: '{1}'", serializerName, typename);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SerializationBindingToNonExistingSerializer(this ILoggingAdapter logger, string serializerName)
        {
            logger.Warning("Serialization binding to non existing serializer: '{0}'", serializerName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CoordinatedShutdownFromCLRShutdownFailed(this ILoggingAdapter logger, Exception ex)
        {
            logger.Warning("CoordinatedShutdown from CLR shutdown failed: {0}", ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TaskFailedInPhase(this ILoggingAdapter logger, string taskName, string phase, Task<Done> tr)
        {
            logger.Warning("Task [{0}] failed in phase [{1}]: {2}", taskName, phase, tr.Exception?.Flatten().Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TaskFailedInPhase(this ILoggingAdapter logger, string taskName, string phase, Exception ex)
        {
            logger.Warning("Task [{0}] failed in phase [{1}]: {2}", taskName, phase, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MessageMustBeHandledByHashMapping(this ILoggingAdapter logger, object message)
        {
            logger.Warning("Message [{0}] must be handled by hashMapping, or implement [{1}] or be wrapped in [{2}]",
                    message.GetType().Name, typeof(IConsistentHashable).Name, typeof(ConsistentHashableEnvelope).Name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldnotRouteMessageWithConsistentHashKey(this ILoggingAdapter logger, object hashData, Exception ex)
        {
            logger.Warning("Couldn't route message with consistent hash key [{0}] due to [{1}]", hashData, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingMessageInboxSizeHasBeenExceeded(this ILoggingAdapter logger, int size)
        {
            logger.Warning("Dropping message: Inbox size has been exceeded, use akka.actor.inbox.inbox-size to increase maximum allowed inbox size. Current is {0}", size);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnhandledEventInState<TData>(this ILoggingAdapter logger, FSMBase.Event<TData> @event, object stateName)
        {
            logger.Warning("unhandled event {0} in state {1}", @event.FsmEvent, stateName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CoordinatedShutdownPhaseTimedOutAfter(this ILoggingAdapter logger, string phase, TimeSpan timeout)
        {
            logger.Warning("Coordinated shutdown phase [{0}] timed out after {1}", phase, timeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConfigurationSaysThatShouldBeARouterButCodeDisagrees(this ILoggingAdapter logger, ActorPath path)
        {
            logger.Warning(
                "Configuration says that [{0}] should be a router, but code disagrees. Remove the config or add a RouterConfig to its Props.",
                path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToRemoveNonChild(this ILoggingAdapter logger, ActorPath path, string name)
        {
            logger.Warning("{0} trying to remove non-child {1}", path, name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReplacingChild(this ILoggingAdapter logger, string name, IInternalActorRef actor, IInternalActorRef v)
        {
            logger.Warning("{0} replacing child {1} ({2} -> {3})", name, actor, v, actor);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToShutdownSchedulerWithin(this ILoggingAdapter logger, TimeSpan shutdownTimeout)
        {
            logger.Warning("Failed to shutdown scheduler within {0}", shutdownTimeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToShutdownDedicatedThreadSchedulerWithin(this ILoggingAdapter logger, TimeSpan shutdownTimeout)
        {
            logger.Warning("Failed to shutdown DedicatedThreadScheduler within {0}", shutdownTimeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ActorReceivedUnexpectedSystemMessage(this ILoggingAdapter logger, ActorPath path, ISystemMessage systemMessage)
        {
            logger.Error("{0} received unexpected system message [{1}]", path, systemMessage);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GuardianFailedShuttingDown(this ILoggingAdapter logger, Exception cause, IActorRef child)
        {
            logger.Error(cause, "guardian {0} failed, shutting down!", child);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ActorReceivedUnexpectedMessage(this ILoggingAdapter logger, ActorPath path, object message)
        {
            logger.Error("{0} received unexpected message [{1}]", path, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorOccurredWhileExecutingCLRShutdownHook(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, "Error occurred while executing CLR shutdown hook");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GuardianFailedShuttingDownSystem(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, "Guardian failed. Shutting down system");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnErrorOccurredWhileDisposingActor(this ILoggingAdapter logger, ActorBase actor, Exception e)
        {
            logger.Error(e, "An error occurred while disposing {0} actor. Reason: {1}",
                                actor.GetType(), e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DedicatedThreadSchedulerFailedToExecuteAction(this ILoggingAdapter logger, Exception x)
        {
            logger.Error(x, "DedicatedThreadScheduler failed to execute action");
        }
    }
}
