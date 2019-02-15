//-----------------------------------------------------------------------
// <copyright file="Eventsourced.Lifecycle.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;

namespace Akka.Persistence
{
    /// <summary>
    /// TBD
    /// </summary>
    public partial class Eventsourced
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Func<Envelope, bool> UnstashFilterPredicate =
            envelope => !(envelope.Message is WriteMessageSuccess || envelope.Message is ReplayedMessage);

        private void StartRecovery(Recovery recovery)
        {
            ChangeState(RecoveryStarted(recovery.ReplayMax));
            LoadSnapshot(SnapshotterId, recovery.FromSnapshot, recovery.ToSequenceNr);
        }

        private void RequestRecoveryPermit()
        {
            Extension.RecoveryPermitter().Tell(Akka.Persistence.RequestRecoveryPermit.Instance, Self);
            ChangeState(WaitingRecoveryPermit(Recovery));
        }

        protected internal override bool AroundReceive(Receive receive, object message)
        {
            _currentState.StateReceive(receive, message);
            return true;
        }

        /// <inheritdoc/>
        public override void AroundPreStart()
        {
            if (PersistenceId == null) ThrowHelper.ThrowArgumentNullException_Eventsourced(Self);

            // Fail fast on missing plugins.
            var j = Journal;
            var s = SnapshotStore;
            RequestRecoveryPermit();
            base.AroundPreStart();
        }

        /// <inheritdoc/>
        public override void AroundPreRestart(Exception cause, object message)
        {
            try
            {
                _internalStash.UnstashAll();
                Stash.UnstashAll(UnstashFilterPredicate);
            }
            finally
            {
                object inner;
                switch (message)
                {
                    case WriteMessageSuccess writeMsg:
                        inner = writeMsg.Persistent;
                        break;
                    case LoopMessageSuccess loopMsg:
                        inner = loopMsg.Message;
                        break;
                    case ReplayedMessage replayedMsg:
                        inner = replayedMsg.Persistent;
                        break;
                    default:
                        inner = message;
                        break;
                }

                FlushJournalBatch();
                base.AroundPreRestart(cause, inner);
            }
        }

        /// <inheritdoc/>
        public override void AroundPostRestart(Exception reason, object message)
        {
            RequestRecoveryPermit();
            base.AroundPostRestart(reason, message);
        }

        /// <inheritdoc/>
        public override void AroundPostStop()
        {
            try
            {
                _internalStash.UnstashAll();
                Stash.UnstashAll(UnstashFilterPredicate);
            }
            finally
            {
                base.AroundPostStop();
            }
        }

        /// <inheritdoc/>
        protected override void Unhandled(object message)
        {
            switch (message)
            {
                case RecoveryCompleted _:
                    // ignore
                    return;
                case SaveSnapshotFailure saveSnapshotFailure:
                    if (Log.IsWarningEnabled)
                    {
                        Log.FailedToSaveSnapshotGivenMetadata(saveSnapshotFailure);
                    }
                    break;
                case DeleteSnapshotFailure deleteSnapshotFailure:
                    if (Log.IsWarningEnabled)
                    {
                        Log.FailedToDeleteSnapshotGivenMetadata(deleteSnapshotFailure);
                    }
                    break;
                case DeleteSnapshotsFailure deleteSnapshotsFailure:
                    if (Log.IsWarningEnabled)
                    {
                        Log.FailedToDeleteSnapshotsGivenCriteria(deleteSnapshotsFailure);
                    }
                    break;
                case DeleteMessagesFailure deleteMessagesFailure:
                    if (Log.IsWarningEnabled)
                    {
                        Log.FailedToDeleteMessagesToSequenceNrForPersistenceId(deleteMessagesFailure, PersistenceId);
                    }
                    break;
                default:
                    base.Unhandled(message);
                    break;
            }
        }
    }
}
