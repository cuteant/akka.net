//-----------------------------------------------------------------------
// <copyright file="SnapshotStore.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Pattern;
using Akka.Util;

namespace Akka.Persistence.Snapshot
{
    /// <summary>
    /// Abstract snapshot store.
    /// </summary>
    public abstract class SnapshotStore : ActorBase
    {
        private readonly TaskContinuationOptions _continuationOptions = TaskContinuationOptions.ExecuteSynchronously;
        private readonly bool _publish;
        private readonly CircuitBreaker _breaker;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotStore"/> class.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the associated Persistence extension has not been used in current actor system context.
        /// </exception>
        protected SnapshotStore()
        {
            var extension = Persistence.Instance.Apply(Context.System);
            if (extension == null) { ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_Init_SnapshotStore); }

            _publish = extension.Settings.Internal.PublishPluginCommands;
            var config = extension.ConfigFor(Self);
            _breaker = CircuitBreaker.Create(
                config.GetInt("circuit-breaker.max-failures", 0),
                config.GetTimeSpan("circuit-breaker.call-timeout", null),
                config.GetTimeSpan("circuit-breaker.reset-timeout", null));
        }

        /// <inheritdoc/>
        protected sealed override bool Receive(object message)
        {
            return ReceiveSnapshotStore(message) || ReceivePluginInternal(message);
        }

        private bool ReceiveSnapshotStore(object message)
        {
            var senderPersistentActor = Sender; // Sender is PersistentActor
            var self = Self; //Self MUST BE CLOSED OVER here, or the code below will be subject to race conditions

            switch (message)
            {
                case LoadSnapshot loadSnapshot:
                    if (loadSnapshot.Criteria == SnapshotSelectionCriteria.None)
                    {
                        senderPersistentActor.Tell(new LoadSnapshotResult(null, loadSnapshot.ToSequenceNr));
                    }
                    else
                    {
                        _breaker.WithCircuitBreaker(InvokeLoadSnapshotFunc, this, loadSnapshot)
                                .LinkOutcome(AfterLoadSnapshotFunc, loadSnapshot, _continuationOptions)
                                .PipeTo(senderPersistentActor);
                    }
                    break;
                case SaveSnapshot saveSnapshot:
                    var metadata = new SnapshotMetadata(saveSnapshot.Metadata.PersistenceId, saveSnapshot.Metadata.SequenceNr, DateTime.UtcNow);

                    _breaker.WithCircuitBreaker(InvokeSaveSnapshotFunc, this, metadata, saveSnapshot)
                            .LinkOutcome(AfterSaveSnapshotFunc, metadata, saveSnapshot, _continuationOptions)
                            .PipeTo(self, senderPersistentActor);
                    break;
                case SaveSnapshotFailure saveSnapshotFailure:
                    try
                    {
                        ReceivePluginInternal(message);
                        _breaker.WithCircuitBreaker(InvokeSaveSnapshotFailureFunc, this, saveSnapshotFailure);
                    }
                    finally
                    {
                        senderPersistentActor.Tell(message);
                    }
                    break;
                case DeleteSnapshot deleteSnapshot:
                    var eventStream0 = Context.System.EventStream;
                    _breaker.WithCircuitBreaker(InvokeDeleteSnapshotFunc, this, deleteSnapshot)
                            .LinkOutcome(AfterDeleteSnapshotFunc, deleteSnapshot, _continuationOptions)
                            .PipeTo(self, senderPersistentActor)
                            .LinkOutcome(InvokePublishAction, _publish, eventStream0, message, _continuationOptions);
                    break;
                case DeleteSnapshots deleteSnapshots:
                    var eventStream = Context.System.EventStream;
                    _breaker.WithCircuitBreaker(InvokeDeleteSnapshotsFunc, this, deleteSnapshots)
                            .LinkOutcome(AfterDeleteSnapshotsFunc, deleteSnapshots, _continuationOptions)
                            .PipeTo(self, senderPersistentActor)
                            .LinkOutcome(InvokePublishAction, _publish, eventStream, message, _continuationOptions);
                    break;
                case SaveSnapshotSuccess _:
                case DeleteSnapshotSuccess _:
                case DeleteSnapshotFailure _:
                case DeleteSnapshotsSuccess _:
                case DeleteSnapshotsFailure _:
                    try
                    {
                        ReceivePluginInternal(message);
                    }
                    finally
                    {
                        senderPersistentActor.Tell(message);
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static readonly Func<SnapshotStore, LoadSnapshot, Task<SelectedSnapshot>> InvokeLoadSnapshotFunc = InvokeLoadSnapshot;
        private static Task<SelectedSnapshot> InvokeLoadSnapshot(SnapshotStore owner, LoadSnapshot loadSnapshot)
        {
            return owner.LoadAsync(loadSnapshot.PersistenceId, loadSnapshot.Criteria.Limit(loadSnapshot.ToSequenceNr));
        }

        private static readonly Func<Task<SelectedSnapshot>, LoadSnapshot, ISnapshotResponse> AfterLoadSnapshotFunc = AfterLoadSnapshot;
        public static ISnapshotResponse AfterLoadSnapshot(Task<SelectedSnapshot> t, LoadSnapshot loadSnapshot)
        {
            return t.IsSuccessfully()
                ? new LoadSnapshotResult(t.Result, loadSnapshot.ToSequenceNr) as ISnapshotResponse
                : new LoadSnapshotFailed(t.IsFaulted
                        ? TryUnwrapException(t.Exception)
                        : new OperationCanceledException("LoadAsync canceled, possibly due to timing out."));
        }

        private static readonly Func<SnapshotStore, SnapshotMetadata, SaveSnapshot, Task> InvokeSaveSnapshotFunc = InvokeSaveSnapshot;
        private static Task InvokeSaveSnapshot(SnapshotStore owner, SnapshotMetadata metadata, SaveSnapshot saveSnapshot)
        {
            return owner.SaveAsync(metadata, saveSnapshot.Snapshot);
        }

        private static readonly Func<Task, SnapshotMetadata, SaveSnapshot, ISnapshotResponse> AfterSaveSnapshotFunc = AfterSaveSnapshot;
        public static ISnapshotResponse AfterSaveSnapshot(Task t, SnapshotMetadata metadata, SaveSnapshot saveSnapshot)
        {
            return t.IsSuccessfully()
                ? new SaveSnapshotSuccess(metadata) as ISnapshotResponse
                : new SaveSnapshotFailure(saveSnapshot.Metadata,
                    t.IsFaulted
                        ? TryUnwrapException(t.Exception)
                        : new OperationCanceledException("SaveAsync canceled, possibly due to timing out."));
        }

        private static readonly Func<SnapshotStore, SaveSnapshotFailure, Task> InvokeSaveSnapshotFailureFunc = InvokeSaveSnapshotFailure;
        private static Task InvokeSaveSnapshotFailure(SnapshotStore owner, SaveSnapshotFailure saveSnapshotFailure)
        {
            return owner.DeleteAsync(saveSnapshotFailure.Metadata);
        }

        private static readonly Func<SnapshotStore, DeleteSnapshot, Task> InvokeDeleteSnapshotFunc = InvokeDeleteSnapshot;
        private static Task InvokeDeleteSnapshot(SnapshotStore owner, DeleteSnapshot deleteSnapshot)
        {
            return owner.DeleteAsync(deleteSnapshot.Metadata);
        }

        private static readonly Func<Task, DeleteSnapshot, ISnapshotResponse> AfterDeleteSnapshotFunc = AfterDeleteSnapshot;
        public static ISnapshotResponse AfterDeleteSnapshot(Task t, DeleteSnapshot deleteSnapshot)
        {
            return t.IsSuccessfully()
                ? new DeleteSnapshotSuccess(deleteSnapshot.Metadata) as ISnapshotResponse
                : new DeleteSnapshotFailure(deleteSnapshot.Metadata,
                    t.IsFaulted
                        ? TryUnwrapException(t.Exception)
                        : new OperationCanceledException("DeleteAsync canceled, possibly due to timing out."));
        }

        private static readonly Func<SnapshotStore, DeleteSnapshots, Task> InvokeDeleteSnapshotsFunc = InvokeDeleteSnapshots;
        private static Task InvokeDeleteSnapshots(SnapshotStore owner, DeleteSnapshots deleteSnapshots)
        {
            return owner.DeleteAsync(deleteSnapshots.PersistenceId, deleteSnapshots.Criteria);
        }

        private static readonly Func<Task, DeleteSnapshots, ISnapshotResponse> AfterDeleteSnapshotsFunc = AfterDeleteSnapshots;
        public static ISnapshotResponse AfterDeleteSnapshots(Task t, DeleteSnapshots deleteSnapshots)
        {
            return t.IsSuccessfully()
                ? new DeleteSnapshotsSuccess(deleteSnapshots.Criteria) as ISnapshotResponse
                : new DeleteSnapshotsFailure(deleteSnapshots.Criteria,
                    t.IsFaulted
                        ? TryUnwrapException(t.Exception)
                        : new OperationCanceledException("DeleteAsync canceled, possibly due to timing out."));
        }

        public static readonly Action<Task, bool, Event.EventStream, object> InvokePublishAction = InvokePublish;
        public static void InvokePublish(Task t, bool publish, Event.EventStream eventStream, object message)
        {
            if (publish) { eventStream.Publish(message); }
        }

        private static Exception TryUnwrapException(Exception e)
        {
            if (e is AggregateException aggregateException)
            {
                aggregateException = aggregateException.Flatten();
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    return aggregateException.InnerExceptions[0];
                }
            }
            return e;
        }

        /// <summary>
        /// Plugin API: Asynchronously loads a snapshot.
        /// 
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="persistenceId">Id of the persistent actor.</param>
        /// <param name="criteria">Selection criteria for loading.</param>
        /// <returns>TBD</returns>
        protected abstract Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria);

        /// <summary>
        /// Plugin API: Asynchronously saves a snapshot.
        /// 
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <param name="snapshot">Snapshot.</param>
        /// <returns>TBD</returns>
        protected abstract Task SaveAsync(SnapshotMetadata metadata, object snapshot);

        /// <summary>
        /// Plugin API: Deletes the snapshot identified by <paramref name="metadata"/>.
        /// 
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <returns>TBD</returns>
        protected abstract Task DeleteAsync(SnapshotMetadata metadata);

        /// <summary>
        /// Plugin API: Deletes all snapshots matching provided <paramref name="criteria"/>.
        /// 
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="persistenceId">Id of the persistent actor.</param>
        /// <param name="criteria">Selection criteria for deleting.</param>
        /// <returns>TBD</returns>
        protected abstract Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria);

        /// <summary>
        /// Plugin API: Allows plugin implementers to use f.PipeTo(Self)
        /// and handle additional messages for implementing advanced features
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected virtual bool ReceivePluginInternal(object message)
        {
            return false;
        }
    }
}
