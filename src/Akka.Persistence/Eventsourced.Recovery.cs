//-----------------------------------------------------------------------
// <copyright file="Eventsourced.Recovery.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Persistence.Internal;

namespace Akka.Persistence
{
    internal delegate void StateReceive(Receive receive, object message);

    internal class EventsourcedState
    {
        public EventsourcedState(string name, bool isRecoveryRunning, StateReceive stateReceive)
        {
            Name = name;
            IsRecoveryRunning = isRecoveryRunning;
            StateReceive = stateReceive;
        }

        public string Name { get; }

        public bool IsRecoveryRunning { get; }

        public StateReceive StateReceive { get; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// TBD
    /// </summary>
    public abstract partial class Eventsourced
    {
        /// <summary>
        /// Initial state. Before starting the actual recovery it must get a permit from the `RecoveryPermitter`.
        /// When starting many persistent actors at the same time the journal and its data store is protected from
        /// being overloaded by limiting number of recoveries that can be in progress at the same time.
        /// When receiving `RecoveryPermitGranted` it switches to `recoveryStarted` state.
        /// All incoming messages are stashed.
        /// </summary>
        private EventsourcedState WaitingRecoveryPermit(Recovery recovery)
        {
            void stateReceive(Receive receive, object message)
            {
                if (message is RecoveryPermitGranted)
                    StartRecovery(recovery);
                else
                    StashInternally(message);
            }
            return new EventsourcedState("waiting for recovery permit", true, stateReceive);
        }

        /// <summary>
        /// Processes a loaded snapshot, if any. A loaded snapshot is offered with a <see cref="SnapshotOffer"/>
        /// message to the actor's <see cref="ReceiveRecover"/>. Then initiates a message replay, either starting
        /// from the loaded snapshot or from scratch, and switches to <see cref="ReplayStarted"/> state.
        /// All incoming messages are stashed.
        /// </summary>
        /// <param name="maxReplays">Maximum number of messages to replay</param>
        private EventsourcedState RecoveryStarted(long maxReplays)
        {
            // protect against snapshot stalling forever because journal overloaded and such
            var timeout = Extension.JournalConfigFor(JournalPluginId).GetTimeSpan("recovery-event-timeout", null, false);
            var timeoutCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(timeout, Self, new RecoveryTick(true), Self);

            bool recoveryBehavior(object message)
            {
                Receive receiveRecover = ReceiveRecover;
                switch (message)
                {
                    case IPersistentRepresentation pp when (IsRecovering):
                        return receiveRecover(pp.Payload);
                    case SnapshotOffer snapshotOffer:
                        return receiveRecover(snapshotOffer);
                    case RecoveryCompleted _:
                        return receiveRecover(RecoveryCompleted.Instance);
                    default:
                        return false;
                }
            }

            void stateReceive(Receive receive, object message)
            {
                try
                {
                    switch (message)
                    {
                        case LoadSnapshotResult res:
                            timeoutCancelable.Cancel();
                            if (res.Snapshot != null)
                            {
                                var snapshot = res.Snapshot;
                                LastSequenceNr = snapshot.Metadata.SequenceNr;
                                // Since we are recovering we can ignore the receive behavior from the stack
                                base.AroundReceive(recoveryBehavior, new SnapshotOffer(snapshot.Metadata, snapshot.Snapshot));
                            }

                            ChangeState(Recovering(recoveryBehavior, timeout));
                            Journal.Tell(new ReplayMessages(LastSequenceNr + 1L, res.ToSequenceNr, maxReplays, PersistenceId, Self));
                            break;
                        case LoadSnapshotFailed failed:
                            timeoutCancelable.Cancel();
                            try
                            {
                                OnRecoveryFailure(failed.Cause);
                            }
                            finally
                            {
                                Context.Stop(Self);
                            }
                            ReturnRecoveryPermit();
                            break;
                        case RecoveryTick tick when (tick.Snapshot):
                            try
                            {
                                OnRecoveryFailure(
                                    new RecoveryTimedOutException(
                                        $"Recovery timed out, didn't get snapshot within {timeout.TotalSeconds}s."));
                            }
                            finally
                            {
                                Context.Stop(Self);
                            }
                            ReturnRecoveryPermit();
                            break;
                        default:
                            StashInternally(message);
                            break;
                    }
                }
                catch (Exception)
                {
                    ReturnRecoveryPermit();
                    throw;
                }
            }

            return new EventsourcedState("recovery started - replay max: " + maxReplays, true, stateReceive);
        }

        private void ReturnRecoveryPermit()
        {
            Extension.RecoveryPermitter().Tell(Akka.Persistence.ReturnRecoveryPermit.Instance, Self);
        }

        /// <summary>
        /// Processes replayed messages, if any. The actor's <see cref="ReceiveRecover"/> is invoked with the replayed events.
        ///
        /// If replay succeeds it got highest stored sequence number response from the journal and then switches
        /// to <see cref="ProcessingCommands"/> state.
        /// If replay succeeds the <see cref="OnReplaySuccess"/> callback method is called, otherwise
        /// <see cref="OnRecoveryFailure"/>.
        ///
        /// All incoming messages are stashed.
        /// </summary>
        private EventsourcedState Recovering(Receive recoveryBehavior, TimeSpan timeout)
        {
            // protect against event replay stalling forever because of journal overloaded and such
            var timeoutCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(timeout, timeout, Self, new RecoveryTick(false), Self);
            var eventSeenInInterval = false;

            void stateReceive(Receive receive, object message)
            {
                try
                {
                    switch (message)
                    {
                        case ReplayedMessage replayedMsg:
                            try
                            {
                                eventSeenInInterval = true;
                                UpdateLastSequenceNr(replayedMsg.Persistent);
                                base.AroundReceive(recoveryBehavior, replayedMsg.Persistent);
                            }
                            catch (Exception cause)
                            {
                                timeoutCancelable.Cancel();
                                try
                                {
                                    OnRecoveryFailure(cause, replayedMsg.Persistent.Payload);
                                }
                                finally
                                {
                                    Context.Stop(Self);
                                }
                                ReturnRecoveryPermit();
                            }
                            break;

                        case RecoverySuccess recovery:
                            timeoutCancelable.Cancel();
                            OnReplaySuccess();
                            ChangeState(ProcessingCommands());
                            _sequenceNr = recovery.HighestSequenceNr;
                            LastSequenceNr = recovery.HighestSequenceNr;
                            _internalStash.UnstashAll();

                            base.AroundReceive(recoveryBehavior, RecoveryCompleted.Instance);
                            ReturnRecoveryPermit();
                            break;

                        case ReplayMessagesFailure failure:
                            timeoutCancelable.Cancel();
                            try
                            {
                                OnRecoveryFailure(failure.Cause, message: null);
                            }
                            finally
                            {
                                Context.Stop(Self);
                            }
                            ReturnRecoveryPermit();
                            break;

                        case RecoveryTick tick when (!tick.Snapshot):
                            if (!eventSeenInInterval)
                            {
                                timeoutCancelable.Cancel();
                                try
                                {
                                    OnRecoveryFailure(
                                        new RecoveryTimedOutException(
                                            $"Recovery timed out, didn't get event within {timeout.TotalSeconds}s, highest sequence number seen {_sequenceNr}."));
                                }
                                finally
                                {
                                    Context.Stop(Self);
                                }
                                ReturnRecoveryPermit();
                            }
                            else
                            {
                                eventSeenInInterval = false;
                            }
                            break;

                        default:
                            StashInternally(message);
                            break;
                    }
                }
                catch (Exception)
                {
                    ReturnRecoveryPermit();
                    throw;
                }
            }
            return new EventsourcedState("replay started", true, stateReceive);
        }

        /// <summary>
        /// If event persistence is pending after processing a command, event persistence
        /// is triggered and the state changes to <see cref="PersistingEvents"/>.
        /// </summary>
        private EventsourcedState ProcessingCommands()
        {
            void stateReceive(Receive receive, object message)
            {
                void onWriteMessageComplete(bool err)
                {
                    _pendingInvocations.Pop();
                    UnstashInternally(err);
                }
                var handled = CommonProcessingStateBehavior(message, onWriteMessageComplete);
                if (!handled)
                {
                    try
                    {
                        base.AroundReceive(receive, message);
                        OnProcessingCommandsAroundReceiveComplete(false);
                    }
                    catch (Exception)
                    {
                        OnProcessingCommandsAroundReceiveComplete(true);
                        throw;
                    }
                }
            }
            return new EventsourcedState("processing commands", false, stateReceive);
        }

        private void OnProcessingCommandsAroundReceiveComplete(bool err)
        {
            if (_eventBatch.Count > 0) FlushBatch();

            if (_asyncTaskRunning)
            {
                //do nothing, wait for the task to finish
            }
            else if (_pendingStashingPersistInvocations > 0)
                ChangeState(PersistingEvents());
            else
                UnstashInternally(err);
        }

        private void FlushBatch()
        {
            if (_eventBatch.Count > 0)
            {
                foreach (var p in _eventBatch.Reverse())
                {
                    _journalBatch.Add(p);
                }
                _eventBatch = new LinkedList<IPersistentEnvelope>();
            }

            FlushJournalBatch();
        }

        /// <summary>
        /// Remains until pending events are persisted and then changes state to <see cref="ProcessingCommands"/>.
        /// Only events to be persisted are processed. All other messages are stashed internally.
        /// </summary>
        private EventsourcedState PersistingEvents()
        {
            void stateReceive(Receive receive, object message)
            {
                void onWriteMessageComplete(bool err)
                {
                    var invocation = _pendingInvocations.Pop();

                    // enables an early return to `processingCommands`, because if this counter hits `0`,
                    // we know the remaining pendingInvocations are all `persistAsync` created, which
                    // means we can go back to processing commands also - and these callbacks will be called as soon as possible
                    if (invocation is StashingHandlerInvocation) { _pendingStashingPersistInvocations--; }

                    if (_pendingStashingPersistInvocations == 0)
                    {
                        ChangeState(ProcessingCommands());
                        UnstashInternally(err);
                    }
                }
                var handled = CommonProcessingStateBehavior(message, onWriteMessageComplete);

                if (!handled) { StashInternally(message); }
            }
            return new EventsourcedState("persisting events", false, stateReceive);
        }

        private void PeekApplyHandler(object payload)
        {
            try
            {
                _pendingInvocations.First.Value.Handler(payload);
            }
            finally
            {
                FlushBatch();
            }
        }

        private bool CommonProcessingStateBehavior(object message, Action<bool> onWriteMessageComplete)
        {
            // _instanceId mismatch can happen for persistAsync and defer in case of actor restart
            // while message is in flight, in that case we ignore the call to the handler
            switch (message)
            {
                case WriteMessageSuccess writeSuccess:
                    if (writeSuccess.ActorInstanceId == _instanceId)
                    {
                        UpdateLastSequenceNr(writeSuccess.Persistent);
                        try
                        {
                            PeekApplyHandler(writeSuccess.Persistent.Payload);
                            onWriteMessageComplete(false);
                        }
                        catch
                        {
                            onWriteMessageComplete(true);
                            throw;
                        }
                    }
                    break;
                case WriteMessageRejected writeRejected:
                    if (writeRejected.ActorInstanceId == _instanceId)
                    {
                        var p = writeRejected.Persistent;
                        UpdateLastSequenceNr(p);
                        onWriteMessageComplete(false);
                        OnPersistRejected(writeRejected.Cause, p.Payload, p.SequenceNr);
                    }
                    break;
                case WriteMessageFailure writeFailure:
                    if (writeFailure.ActorInstanceId == _instanceId)
                    {
                        var p = writeFailure.Persistent;
                        onWriteMessageComplete(false);
                        try
                        {
                            OnPersistFailure(writeFailure.Cause, p.Payload, p.SequenceNr);
                        }
                        finally
                        {
                            Context.Stop(Self);
                        }
                    }
                    break;
                case LoopMessageSuccess loopSuccess:
                    if (loopSuccess.ActorInstanceId == _instanceId)
                    {
                        try
                        {
                            PeekApplyHandler(loopSuccess.Message);
                            onWriteMessageComplete(false);
                        }
                        catch (Exception)
                        {
                            onWriteMessageComplete(true);
                            throw;
                        }
                    }
                    break;
                case WriteMessagesSuccessful _:
                    _isWriteInProgress = false;
                    FlushJournalBatch();
                    break;
                case WriteMessagesFailed _:
                    _isWriteInProgress = false;
                    // it will be stopped by the first WriteMessageFailure message
                    break;
                case RecoveryTick _:
                    // we may have one of these in the mailbox before the scheduled timeout
                    // is cancelled when recovery has completed, just consume it so the concrete actor never sees it
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
