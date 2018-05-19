//-----------------------------------------------------------------------
// <copyright file="PersistentView.Recovery.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Util.Internal;

namespace Akka.Persistence
{
    //TODO: There are some duplication of the recovery state management here and in EventsourcedState,
    //      but the enhanced PersistentView will not be based on recovery infrastructure, and
    //      therefore this code will be replaced anyway

    /// <summary>
    /// TBD
    /// </summary>
    internal class ViewState
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="name">TBD</param>
        /// <param name="isRecoveryRunning">TBD</param>
        /// <param name="stateReceive">TBD</param>
        public ViewState(string name, bool isRecoveryRunning, StateReceive stateReceive)
        {
            Name = name;
            StateReceive = stateReceive;
            IsRecoveryRunning = isRecoveryRunning;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// TBD
        /// </summary>
        public bool IsRecoveryRunning { get; private set; }
        /// <summary>
        /// TBD
        /// </summary>
        public StateReceive StateReceive { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public partial class PersistentView
    {
        /// <summary>
        /// Processes a loaded snapshot, if any. A loaded snapshot is offered with a <see cref="SnapshotOffer"/>
        /// message to the actor's <see cref="ActorBase.Receive"/> method. Then initiates a message replay, either 
        /// starting from the loaded snapshot or from scratch, and switches to <see cref="ReplayStarted"/> state.
        /// All incoming messages are stashed.
        /// </summary>
        private ViewState RecoveryStarted(long replayMax)
        {
            void stateReceive(Receive receive, object message)
            {
                if (message is LoadSnapshotResult loadResult)
                {
                    if (loadResult.Snapshot != null)
                    {
                        var selectedSnapshot = loadResult.Snapshot;
                        LastSequenceNr = selectedSnapshot.Metadata.SequenceNr;
                        base.AroundReceive(receive, new SnapshotOffer(selectedSnapshot.Metadata, selectedSnapshot.Snapshot));
                    }
                    ChangeState(ReplayStarted(true));
                    Journal.Tell(new ReplayMessages(LastSequenceNr + 1, loadResult.ToSequenceNr, replayMax, PersistenceId, Self));
                }
                else _internalStash.Stash();
            }
            return new ViewState("recovery started - replayMax: " + replayMax, true, stateReceive);
        }

        /// <summary>
        /// Processes replayed message, if any. The actor's <see cref="ActorBase.Receive"/> is invoked 
        /// with the replayed events.
        /// 
        /// If replay succeeds it got highest stored sequence number response from the journal and
        /// then switch it switches to <see cref="Idle"/> state.
        /// 
        /// 
        /// If replay succeeds the <see cref="OnReplaySuccess"/> callback method is called, otherwise
        /// <see cref="OnReplayError"/> is called and remaining replay events are consumed (ignored).
        /// 
        /// All incoming messages are stashed when <paramref name="shouldAwait"/> is true.
        /// </summary>
        private ViewState ReplayStarted(bool shouldAwait)
        {
            void stateReceive(Receive receive, object message)
            {
                switch (message)
                {
                    case ReplayedMessage replayedMessage:
                        try
                        {
                            UpdateLastSequenceNr(replayedMessage.Persistent);
                            base.AroundReceive(receive, replayedMessage.Persistent.Payload);
                        }
                        catch (Exception ex)
                        {
                            ChangeState(IgnoreRemainingReplay(ex));
                        }
                        break;
                    case RecoverySuccess _:
                        OnReplayComplete();
                        break;
                    case ReplayMessagesFailure replayFailureMessage:
                        try
                        {
                            OnReplayError(replayFailureMessage.Cause);
                        }
                        finally
                        {
                            OnReplayComplete();
                        }
                        break;
                    case ScheduledUpdate _:
                        // ignore
                        break;
                    case Update u:
                        if (u.IsAwait)
                        {
                            _internalStash.Stash();
                        }
                        break;
                    default:
                        if (shouldAwait)
                        {
                            _internalStash.Stash();
                        }
                        else
                        {
                            try
                            {
                                base.AroundReceive(receive, message);
                            }
                            catch (Exception ex)
                            {
                                ChangeState(IgnoreRemainingReplay(ex));
                            }
                        }
                        break;
                }
            }
            return new ViewState("replay started", true, stateReceive);
        }

        /// <summary>
        /// Switches to <see cref="Idle"/>.
        /// </summary>
        private void OnReplayComplete()
        {
            ChangeState(Idle());

            _internalStash.UnstashAll();
        }

        /// <summary>
        /// Consumes remaining replayed messages and then throws the exception.
        /// </summary>
        private ViewState IgnoreRemainingReplay(Exception cause)
        {
            void stateReceive(Receive receive, object message)
            {
                switch (message)
                {
                    case ReplayedMessage _:
                        // ignore
                        break;
                    case ReplayMessagesFailure _:
                        // journal couldn't tell the maximum stored sequence number, hence the next
                        // replay must be a full replay (up to the highest stored sequence number)
                        // Recover(lastSequenceNr) is sent by preRestart
                        LastSequenceNr = long.MaxValue;
                        OnReplayFailureCompleted(cause);
                        break;
                    case RecoverySuccess _:
                        OnReplayFailureCompleted(cause);
                        break;
                    default:
                        _internalStash.Stash();
                        break;
                }
            }
            return new ViewState("replay failed", true, stateReceive);
        }

        private void OnReplayFailureCompleted(Exception cause)
        {
            ChangeState(Idle());
            _internalStash.UnstashAll();
            throw cause;
        }


        /// <summary>
        /// When receiving an <see cref="Update"/> event, switches to <see cref="ReplayStarted"/> state
        /// and triggers an incremental message replay. For any other message invokes actor default behavior.
        /// </summary>
        private ViewState Idle()
        {
            void stateReceive(Receive receive, object message)
            {
                switch (message)
                {
                    case ReplayedMessage replayed:
                        // we can get ReplayedMessage here if it was stashed by user during replay
                        // unwrap the payload
                        base.AroundReceive(receive, replayed.Persistent.Payload);
                        break;

                    case ScheduledUpdate scheduled:
                        ChangeStateToReplayStarted(false, scheduled.ReplayMax);
                        break;

                    case Update update:
                        ChangeStateToReplayStarted(update.IsAwait, update.ReplayMax);
                        break;

                    default:
                        base.AroundReceive(receive, message);
                        break;
                }
            }
            return new ViewState("idle", false, stateReceive);
        }

        private void ChangeStateToReplayStarted(bool isAwait, long replayMax)
        {
            ChangeState(ReplayStarted(isAwait));
            Journal.Tell(new ReplayMessages(LastSequenceNr + 1, long.MaxValue, replayMax, PersistenceId, Self));
        }
    }
}
