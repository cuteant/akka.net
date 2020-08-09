// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Event;
using Akka.Streams.Actors;
using Akka.Streams.Implementation.Fusing;
using Akka.Streams.Stage;

namespace Akka.Streams
{
    internal static class StreamsLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedDueTo(this ILoggingAdapter logger, Exception e)
        {
            logger.Debug("Failed due to: {0}", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailDueTo(this ILoggingAdapter logger, Exception e)
        {
            logger.Debug($"Fail due to: {e.Message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingElementBecauseStatusSuccessReceivedAlready(this ILoggingAdapter logger, object message, int used)
        {
            logger.Debug("Dropping element because Status.Success received already, only draining already buffered elements: [{0}] (pending: [{1}])",
                message, used);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BackpressuringBecauseBufferIsFull(this ILoggingAdapter logger)
        {
            logger.Debug("Backpressuring because buffer is full and overflowStrategy is: [Backpressure]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingTheNewElementBecauseBufferIsFull(this ILoggingAdapter logger)
        {
            logger.Debug("Dropping the new element because buffer is full and overflowStrategy is: [DropNew]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingAllTheBufferedElementsBecauseBufferIsFull(this ILoggingAdapter logger)
        {
            logger.Debug("Dropping all the buffered elements because buffer is full and overflowStrategy is: [DropBuffer]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingTheTailElementBecauseBufferIsFull(this ILoggingAdapter logger)
        {
            logger.Debug("Dropping the tail element because buffer is full and overflowStrategy is: [DropTail]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingTheHeadElementBecauseBufferIsFull(this ILoggingAdapter logger)
        {
            logger.Debug("Dropping the head element because buffer is full and overflowStrategy is: [DropHead]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingElementBecauseThereIsNoDownstreamDemand<T>(this ILoggingAdapter logger, T message)
        {
            logger.Debug("Dropping element because there is no downstream demand: [{0}]", message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AbortingTcpConnectionToBecauseOfUpstreamFailure(this ILoggingAdapter logger, EndPoint remoteAddress, Exception ex)
        {
            logger.Debug($"Aborting tcp connection to {remoteAddress} because of upstream failure: {ex.Message}\n{ex.StackTrace}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoMoreBytesAvailableToRead(this ILoggingAdapter logger)
        {
            logger.Debug("No more bytes available to read (got 0 from read)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoMoreBytesAvailableToRead(this ILoggingAdapter logger, long eofReachedAtOffset)
        {
            logger.Debug($"No more bytes available to read (got 0 from read), marking final bytes of file @ {eofReachedAtOffset}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RestartingGraphIn(this ILoggingAdapter logger, TimeSpan restartDelay)
        {
            logger.Debug($"Restarting graph in {restartDelay}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LastRestartAttemptWasMoreThan(this ILoggingAdapter logger, TimeSpan minBackoff)
        {
            logger.Debug($"Last restart attempt was more than {minBackoff} ago, resetting restart count");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GraphInFinished(this ILoggingAdapter logger)
        {
            logger.Debug("Graph in finished");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RestartingGraphDueToFinishedUpstream(this ILoggingAdapter logger)
        {
            logger.Debug("Restarting graph due to finished upstream");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExternallyRriggeredUnwatch(this ILoggingAdapter logger, IInternalActorRef watcher, IInternalActorRef watchee)
        {
            logger.Warning("externally triggered unwatch from {0} to {1} is illegal on StageActorRef", watcher, watchee);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExternallyRriggeredWatch(this ILoggingAdapter logger, IInternalActorRef watcher, IInternalActorRef watchee)
        {
            logger.Warning("externally triggered watch from {0} to {1} is illegal on StageActorRef", watcher, watchee);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FuzzingModeIsEnabledOnThisSystem(this ILoggingAdapter logger)
        {
            logger.Warning("Fuzzing mode is enabled on this system. If you see this warning on your production system then set 'akka.materializer.debug.fuzzing-mode' to off.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubstreamSubscriptionTimeoutTriggered(this ILoggingAdapter logger, TimeSpan timeout, int count)
        {
            logger.Warning($"Substream subscription timeout triggered after {timeout} in prefixAndTail({count}).");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ItIsNotAllowedToCallAbsorbTerminationFromOnDownstreamFinish(this ILoggingAdapter logger)
        {
            const string error = "It is not allowed to call AbsorbTermination() from OnDownstreamFinish.";
            logger.Error(error);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailingBecauseBufferIsFull(this ILoggingAdapter logger)
        {
            logger.Error("Failing because buffer is full and overflowStrategy is: [Fail]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TearingDownOutputStreamSinkDueToUpstreamError(this ILoggingAdapter logger, OnError error, long bytesWritten)
        {
            logger.Error(error.Cause,
                        $"Tearing down OutputStreamSink due to upstream error, wrote bytes: {bytesWritten}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TearingDownFileSinkDueToUpstreamError(this ILoggingAdapter logger, OnError error, FileInfo f)
        {
            logger.Error(error.Cause, $"Tearing down FileSink({f.FullName}) due to upstream error");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringPostStopIn(this ILoggingAdapter logger, Exception err, GraphAssembly asm, int id)
        {
            logger.Error(err, "Error during PostStop in [{0}]", asm.Stages[id]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErroInStage(this ILoggingAdapter logger, Exception e, GraphAssembly asm, int id)
        {
            logger.Error(e, $"Error in stage [{asm.Stages[id]}]: {e.Message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringPreStartIn(this ILoggingAdapter logger, Exception e, GraphAssembly asm, int id)
        {
            logger.Error(e, $"Error during PreStart in [{asm.Stages[id]}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InitializationOfGraphInterpreterShellFailed(this ILoggingAdapter logger, Exception e, GraphInterpreterShell shell)
        {
            logger.Error(e, "Initialization of GraphInterpreterShell failed for {0}", shell);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RestartingGraphDueToFailure(this ILoggingAdapter logger, Exception ex)
        {
            logger.Warning($"Restarting graph due to failure. Stacktrace: {ex.StackTrace}");
        }
    }
}
