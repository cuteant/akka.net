﻿//-----------------------------------------------------------------------
// <copyright file="SnapshotProtocol.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MessagePack;

namespace Akka.Persistence
{
    /// <summary>
    /// Marker interface for internal snapshot messages
    /// </summary>
    public interface ISnapshotMessage : IPersistenceMessage { }

    /// <summary>
    /// Internal snapshot command
    /// </summary>
    public interface ISnapshotRequest : ISnapshotMessage { }

    /// <summary>
    /// Internal snapshot acknowledgement
    /// </summary>
    public interface ISnapshotResponse : ISnapshotMessage { }

    /// <summary>
    /// Metadata for all persisted snapshot records.
    /// </summary>
    [MessagePackObject]
    public sealed class SnapshotMetadata : IEquatable<SnapshotMetadata>
    {
        /// <summary>
        /// This class represents an <see cref="IComparer{T}"/> used when comparing two <see cref="SnapshotMetadata"/> objects.
        /// </summary>
        internal class SnapshotMetadataComparer : IComparer<SnapshotMetadata>
        {
            /// <inheritdoc/>
            public int Compare(SnapshotMetadata x, SnapshotMetadata y)
            {
                var compare = string.Compare(x.PersistenceId, y.PersistenceId, StringComparison.Ordinal);
                if (compare == 0)
                {
                    compare = Math.Sign(x.SequenceNr - y.SequenceNr);
                    if (compare == 0)
                        compare = x.Timestamp.CompareTo(y.Timestamp);
                }
                return compare;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public static IComparer<SnapshotMetadata> Comparer { get; } = new SnapshotMetadataComparer();

        /// <summary>
        /// TBD
        /// </summary>
        public static DateTime TimestampNotSpecified = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotMetadata"/> class.
        /// </summary>
        /// <param name="persistenceId">The id of the persistent actor for which the snapshot was taken.</param>
        /// <param name="sequenceNr">The sequence number at which the snapshot was taken.</param>
        public SnapshotMetadata(string persistenceId, long sequenceNr)
            : this(persistenceId, sequenceNr, TimestampNotSpecified)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotMetadata"/> class.
        /// </summary>
        /// <param name="persistenceId">The id of the persistent actor for which the snapshot was taken.</param>
        /// <param name="sequenceNr">The sequence number at which the snapshot was taken.</param>
        /// <param name="timestamp">The time at which the snapshot was saved.</param>
        [SerializationConstructor]
        public SnapshotMetadata(string persistenceId, long sequenceNr, DateTime timestamp)
        {
            PersistenceId = persistenceId;
            SequenceNr = sequenceNr;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Id of the persistent actor from which the snapshot was taken.
        /// </summary>
        [Key(0)]
        public readonly string PersistenceId;

        /// <summary>
        /// Sequence number at which a snapshot was taken.
        /// </summary>
        [Key(1)]
        public readonly long SequenceNr;

        /// <summary>
        /// Time at which the snapshot was saved.
        /// </summary>
        [Key(2)]
        public readonly DateTime Timestamp;

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SnapshotMetadata);

        /// <inheritdoc/>
        public bool Equals(SnapshotMetadata other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(PersistenceId, other.PersistenceId, StringComparison.Ordinal) && SequenceNr == other.SequenceNr && Timestamp.Equals(other.Timestamp);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (PersistenceId != null ? PersistenceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SequenceNr.GetHashCode();
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"SnapshotMetadata<pid: {PersistenceId}, seqNr: {SequenceNr}, timestamp: {Timestamp:yyyy/MM/dd}>";
    }

    /// <summary>
    /// Sent to <see cref="PersistentActor"/> after successful saving of a snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class SaveSnapshotSuccess : ISnapshotResponse, IEquatable<SaveSnapshotSuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSnapshotSuccess"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        [SerializationConstructor]
        public SaveSnapshotSuccess(SnapshotMetadata metadata)
        {
            Metadata = metadata;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <inheritdoc/>
        public bool Equals(SaveSnapshotSuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Metadata, other.Metadata);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SaveSnapshotSuccess);

        /// <inheritdoc/>
        public override int GetHashCode() => Metadata != null ? Metadata.GetHashCode() : 0;

        /// <inheritdoc/>
        public override string ToString() => $"SaveSnapshotSuccess<{Metadata}>";
    }

    /// <summary>
    /// Sent to <see cref="PersistentActor"/> after successful deletion of a snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteSnapshotSuccess : ISnapshotResponse, IEquatable<DeleteSnapshotSuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSnapshotSuccess"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        [SerializationConstructor]
        public DeleteSnapshotSuccess(SnapshotMetadata metadata)
        {
            Metadata = metadata;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <inheritdoc/>
        public bool Equals(DeleteSnapshotSuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Metadata, other.Metadata);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteSnapshotSuccess);

        /// <inheritdoc/>
        public override int GetHashCode() => Metadata != null ? Metadata.GetHashCode() : 0;

        /// <inheritdoc/>
        public override string ToString() => $"DeleteSnapshotSuccess<{Metadata}>";
    }

    /// <summary>
    /// Sent to <see cref="PersistentActor"/> after successful deletion of a specified range of snapshots.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteSnapshotsSuccess : ISnapshotResponse, IEquatable<DeleteSnapshotsSuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSnapshotsSuccess"/> class.
        /// </summary>
        /// <param name="criteria">Snapshot selection criteria.</param>
        [SerializationConstructor]
        public DeleteSnapshotsSuccess(SnapshotSelectionCriteria criteria)
        {
            Criteria = criteria;
        }

        /// <summary>
        /// Snapshot selection criteria.
        /// </summary>
        [Key(0)]
        public readonly SnapshotSelectionCriteria Criteria;

        /// <inheritdoc/>
        public bool Equals(DeleteSnapshotsSuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Criteria, other.Criteria);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as DeleteSnapshotsSuccess);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Criteria != null ? Criteria.GetHashCode() : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"DeleteSnapshotsSuccess<{Criteria}>";
        }
    }

    /// <summary>
    /// Sent to <see cref="PersistentActor"/> after failed saving a snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class SaveSnapshotFailure : ISnapshotResponse, IEquatable<SaveSnapshotFailure>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSnapshotFailure"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <param name="cause">A failure cause.</param>
        [SerializationConstructor]
        public SaveSnapshotFailure(SnapshotMetadata metadata, Exception cause)
        {
            Metadata = metadata;
            Cause = cause;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <summary>
        /// A failure cause.
        /// </summary>
        [Key(1)]
        public readonly Exception Cause;

        /// <inheritdoc/>
        public bool Equals(SaveSnapshotFailure other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause) && Equals(Metadata, other.Metadata);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SaveSnapshotFailure);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Metadata != null ? Metadata.GetHashCode() : 0) * 397) ^ (Cause != null ? Cause.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"SaveSnapshotFailure<meta: {Metadata}, cause: {Cause}>";
        }
    }

    /// <summary>
    /// Sent to <see cref="PersistentActor"/> after failed deletion of a snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteSnapshotFailure : ISnapshotResponse, IEquatable<DeleteSnapshotFailure>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSnapshotFailure"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <param name="cause">A failure cause.</param>
        [SerializationConstructor]
        public DeleteSnapshotFailure(SnapshotMetadata metadata, Exception cause)
        {
            Metadata = metadata;
            Cause = cause;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <summary>
        /// A failure cause.
        /// </summary>
        [Key(1)]
        public readonly Exception Cause;

        /// <inheritdoc/>
        public bool Equals(DeleteSnapshotFailure other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause) && Equals(Metadata, other.Metadata);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteSnapshotFailure);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Metadata != null ? Metadata.GetHashCode() : 0) * 397) ^ (Cause != null ? Cause.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"DeleteSnapshotFailure<meta: {Metadata}, cause: {Cause}>";
    }

    /// <summary>
    /// Sent to <see cref="PersistentActor"/> after failed deletion of a range of snapshots.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteSnapshotsFailure : ISnapshotResponse, IEquatable<DeleteSnapshotsFailure>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSnapshotsFailure"/> class.
        /// </summary>
        /// <param name="criteria">Snapshot selection criteria.</param>
        /// <param name="cause">A failure cause.</param>
        [SerializationConstructor]
        public DeleteSnapshotsFailure(SnapshotSelectionCriteria criteria, Exception cause)
        {
            Criteria = criteria;
            Cause = cause;
        }

        /// <summary>
        /// Snapshot selection criteria.
        /// </summary>
        [Key(0)]
        public readonly SnapshotSelectionCriteria Criteria;

        /// <summary>
        /// A failure cause.
        /// </summary>
        [Key(1)]
        public readonly Exception Cause;

        /// <inheritdoc/>
        public bool Equals(DeleteSnapshotsFailure other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause) && Equals(Criteria, other.Criteria);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteSnapshotsFailure);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Criteria != null ? Criteria.GetHashCode() : 0) * 397) ^ (Cause != null ? Cause.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"DeleteSnapshotsFailure<criteria: {Criteria}, cause: {Cause}>";
    }

    /// <summary>
    /// Offers a <see cref="PersistentActor"/> a previously saved snapshot during recovery.
    /// This offer is received before any further replayed messages.
    /// </summary>
    [MessagePackObject]
    public sealed class SnapshotOffer : IEquatable<SnapshotOffer>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotOffer"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <param name="snapshot">Snapshot.</param>
        [SerializationConstructor]
        public SnapshotOffer(SnapshotMetadata metadata, object snapshot)
        {
            Metadata = metadata;
            Snapshot = snapshot;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <summary>
        /// Snapshot.
        /// </summary>
        [Key(1)]
        public readonly object Snapshot;

        /// <inheritdoc/>
        public bool Equals(SnapshotOffer other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Metadata, other.Metadata) && Equals(Snapshot, other.Snapshot);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SnapshotOffer);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Metadata != null ? Metadata.GetHashCode() : 0) * 397) ^ (Snapshot != null ? Snapshot.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"SnapshotOffer<meta: {Metadata}, snapshot: {Snapshot}>";
    }

    /// <summary>
    /// Selection criteria for loading and deleting a snapshots.
    /// </summary>
    [MessagePackObject]
    public sealed class SnapshotSelectionCriteria : IEquatable<SnapshotSelectionCriteria>
    {
        /// <summary>
        /// The latest saved snapshot.
        /// </summary>
        public static readonly SnapshotSelectionCriteria Latest = new SnapshotSelectionCriteria(long.MaxValue, DateTime.MaxValue);

        /// <summary>
        /// No saved snapshot matches.
        /// </summary>
        public static readonly SnapshotSelectionCriteria None = new SnapshotSelectionCriteria(0L, DateTime.MinValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotSelectionCriteria"/> class.
        /// </summary>
        /// <param name="maxSequenceNr">Upper bound for a selected snapshot's sequence number.</param>
        /// <param name="maxTimeStamp">Upper bound for a selected snapshot's timestamp.</param>
        /// <param name="minSequenceNr">Lower bound for a selected snapshot's sequence number</param>
        /// <param name="minTimestamp">Lower bound for a selected snapshot's timestamp</param>
        [SerializationConstructor]
        public SnapshotSelectionCriteria(long maxSequenceNr, DateTime maxTimeStamp, long minSequenceNr = 0L, DateTime? minTimestamp = null)
        {
            MaxSequenceNr = maxSequenceNr;
            MaxTimeStamp = maxTimeStamp;
            MinSequenceNr = minSequenceNr;
            MinTimestamp = minTimestamp ?? DateTime.MinValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotSelectionCriteria"/> class.
        /// </summary>
        /// <param name="maxSequenceNr">Upper bound for a selected snapshot's sequence number.</param>
        public SnapshotSelectionCriteria(long maxSequenceNr) : this(maxSequenceNr, DateTime.MaxValue)
        {
        }

        /// <summary>
        /// Upper bound for a selected snapshot's sequence number.
        /// </summary>
        [Key(0)]
        public readonly long MaxSequenceNr;

        /// <summary>
        /// Upper bound for a selected snapshot's timestamp.
        /// </summary>
        [Key(1)]
        public readonly DateTime MaxTimeStamp;

        /// <summary>
        /// Lower bound for a selected snapshot's sequence number
        /// </summary>
        [Key(2)]
        public readonly long MinSequenceNr;

        /// <summary>
        /// Lower bound for a selected snapshot's timestamp
        /// </summary>
        [Key(3)]
        public readonly DateTime? MinTimestamp;

        internal SnapshotSelectionCriteria Limit(long toSequenceNr)
        {
            return toSequenceNr < MaxSequenceNr
                ? new SnapshotSelectionCriteria(toSequenceNr, MaxTimeStamp, MinSequenceNr, MinTimestamp)
                : this;
        }

        internal bool IsMatch(SnapshotMetadata metadata)
        {
            return metadata.SequenceNr <= MaxSequenceNr && metadata.Timestamp <= MaxTimeStamp &&
                   metadata.SequenceNr >= MinSequenceNr && metadata.Timestamp >= MinTimestamp;
        }

        /// <inheritdoc/>
        public bool Equals(SnapshotSelectionCriteria other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return MaxSequenceNr == other.MaxSequenceNr && MaxTimeStamp == other.MaxTimeStamp &&
                   MinSequenceNr == other.MinSequenceNr && MinTimestamp == other.MinTimestamp;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SnapshotSelectionCriteria);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + MaxSequenceNr.GetHashCode();
                hash = hash * 23 + MaxTimeStamp.GetHashCode();
                hash = hash * 23 + MinSequenceNr.GetHashCode();
                hash = hash * 23 + MinTimestamp.GetHashCode();
                return hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"SnapshotSelectionCriteria<maxSeqNr: {MaxSequenceNr}, maxTimestamp: {MaxTimeStamp:yyyy/MM/dd}, minSeqNr: {MinSequenceNr}, minTimestamp: {MinTimestamp:yyyy/MM/dd}>";
    }

    /// <summary>
    /// A selected snapshot matching <see cref="SnapshotSelectionCriteria"/>.
    /// </summary>
    [MessagePackObject]
    public sealed class SelectedSnapshot : IEquatable<SelectedSnapshot>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedSnapshot"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <param name="snapshot">Snapshot.</param>
        [SerializationConstructor]
        public SelectedSnapshot(SnapshotMetadata metadata, object snapshot)
        {
            Metadata = metadata;
            Snapshot = snapshot;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <summary>
        /// Snapshot.
        /// </summary>
        [Key(1)]
        public readonly object Snapshot;

        /// <inheritdoc/>
        public bool Equals(SelectedSnapshot other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Metadata, other.Metadata) && Equals(Snapshot, other.Snapshot);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SelectedSnapshot);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Metadata != null ? Metadata.GetHashCode() : 0) * 397) ^ (Snapshot != null ? Snapshot.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"SelectedSnapshot<meta: {Metadata}, snapshot: {Snapshot}>";
    }

    /// <summary>
    /// Instructs a snapshot store to load the snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class LoadSnapshot : ISnapshotRequest, IEquatable<LoadSnapshot>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadSnapshot"/> class.
        /// </summary>
        /// <param name="persistenceId">Persistent actor identifier.</param>
        /// <param name="criteria">Criteria for selecting snapshot, from which the recovery should start.</param>
        /// <param name="toSequenceNr">Upper, inclusive sequence number bound for recovery.</param>
        [SerializationConstructor]
        public LoadSnapshot(string persistenceId, SnapshotSelectionCriteria criteria, long toSequenceNr)
        {
            PersistenceId = persistenceId;
            Criteria = criteria;
            ToSequenceNr = toSequenceNr;
        }

        /// <summary>
        /// Persistent actor identifier.
        /// </summary>
        [Key(0)]
        public readonly string PersistenceId;

        /// <summary>
        /// Criteria for selecting snapshot, from which the recovery should start.
        /// </summary>
        [Key(1)]
        public readonly SnapshotSelectionCriteria Criteria;

        /// <summary>
        /// Upper, inclusive sequence number bound for recovery.
        /// </summary>
        [Key(2)]
        public readonly long ToSequenceNr;

        /// <inheritdoc/>
        public bool Equals(LoadSnapshot other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ToSequenceNr, other.ToSequenceNr)
                   && Equals(PersistenceId, other.PersistenceId)
                   && Equals(Criteria, other.Criteria);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as LoadSnapshot);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (PersistenceId != null ? PersistenceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Criteria != null ? Criteria.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ToSequenceNr.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"LoadSnapshot<pid: {PersistenceId}, toSeqNr: {ToSequenceNr}, criteria: {Criteria}>";
    }

    /// <summary>
    /// Response to a <see cref="LoadSnapshot"/> message.
    /// </summary>
    [MessagePackObject]
    public sealed class LoadSnapshotResult : ISnapshotResponse, IEquatable<LoadSnapshotResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadSnapshotResult"/> class.
        /// </summary>
        /// <param name="snapshot">Loaded snapshot or null if none provided.</param>
        /// <param name="toSequenceNr">Upper sequence number bound (inclusive) for recovery.</param>
        [SerializationConstructor]
        public LoadSnapshotResult(SelectedSnapshot snapshot, long toSequenceNr)
        {
            Snapshot = snapshot;
            ToSequenceNr = toSequenceNr;
        }

        /// <summary>
        /// Loaded snapshot or null if none provided.
        /// </summary>
        [Key(0)]
        public readonly SelectedSnapshot Snapshot;

        /// <summary>
        /// Upper sequence number bound (inclusive) for recovery.
        /// </summary>
        [Key(1)]
        public readonly long ToSequenceNr;

        /// <inheritdoc/>
        public bool Equals(LoadSnapshotResult other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ToSequenceNr, other.ToSequenceNr)
                   && Equals(Snapshot, other.Snapshot);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as LoadSnapshotResult);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Snapshot != null ? Snapshot.GetHashCode() : 0) * 397) ^ ToSequenceNr.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"LoadSnapshotResult<toSeqNr: {ToSequenceNr}, snapshot: {Snapshot}>";
    }

    /// <summary>
    /// Reply message to a failed <see cref="LoadSnapshot"/> request.
    /// </summary>
    [MessagePackObject]
    public sealed class LoadSnapshotFailed : ISnapshotResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadSnapshotFailed"/> class.
        /// </summary>
        /// <param name="cause">Failure cause.</param>
        [SerializationConstructor]
        public LoadSnapshotFailed(Exception cause)
        {
            Cause = cause;
        }

        /// <summary>
        /// Failure cause.
        /// </summary>
        [Key(0)]
        public readonly Exception Cause;

        private bool Equals(LoadSnapshotFailed other) => Equals(Cause, other.Cause);

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LoadSnapshotFailed loadSnapshotFailed && Equals(loadSnapshotFailed);
        }

        public override int GetHashCode() => Cause?.GetHashCode() ?? 0;

        public override string ToString() => $"LoadSnapshotFailed<Cause: {Cause}>";
    }

    /// <summary>
    /// Instructs a snapshot store to save a snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class SaveSnapshot : ISnapshotRequest, IEquatable<SaveSnapshot>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSnapshot"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <param name="snapshot">Snapshot.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="metadata"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public SaveSnapshot(SnapshotMetadata metadata, object snapshot)
        {
            if (null == metadata) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.metadata, ExceptionResource.ArgumentNull_SaveSnapshot); }
            Metadata = metadata;
            Snapshot = snapshot;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <summary>
        /// Snapshot.
        /// </summary>
        [Key(1)]
        public readonly object Snapshot;

        /// <inheritdoc/>
        public bool Equals(SaveSnapshot other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Metadata, other.Metadata) && Equals(Snapshot, other.Snapshot);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as SaveSnapshot);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Metadata != null ? Metadata.GetHashCode() : 0) * 397) ^ (Snapshot != null ? Snapshot.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"SaveSnapshot<meta: {Metadata}, snapshot: {Snapshot}>";
    }

    /// <summary>
    /// Instructs a snapshot store to delete a snapshot.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteSnapshot : ISnapshotRequest, IEquatable<DeleteSnapshot>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSnapshot"/> class.
        /// </summary>
        /// <param name="metadata">Snapshot metadata.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="metadata"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public DeleteSnapshot(SnapshotMetadata metadata)
        {
            if (null == metadata) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.metadata, ExceptionResource.ArgumentNull_DeleteSnapshot); }
            Metadata = metadata;
        }

        /// <summary>
        /// Snapshot metadata.
        /// </summary>
        [Key(0)]
        public readonly SnapshotMetadata Metadata;

        /// <inheritdoc/>
        public bool Equals(DeleteSnapshot other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Metadata, other.Metadata);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteSnapshot);

        /// <inheritdoc/>
        public override int GetHashCode() => Metadata.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"DeleteSnapshot<meta: {Metadata}>";
    }

    /// <summary>
    /// Instructs a snapshot store to delete all snapshots that match provided criteria.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteSnapshots : ISnapshotRequest, IEquatable<DeleteSnapshots>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSnapshots"/> class.
        /// </summary>
        /// <param name="persistenceId">Persistent actor id.</param>
        /// <param name="criteria">Criteria for selecting snapshots to be deleted.</param>
        [SerializationConstructor]
        public DeleteSnapshots(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            PersistenceId = persistenceId;
            Criteria = criteria;
        }

        /// <summary>
        /// Persistent actor id.
        /// </summary>
        [Key(0)]
        public readonly string PersistenceId;

        /// <summary>
        /// Criteria for selecting snapshots to be deleted.
        /// </summary>
        [Key(1)]
        public readonly SnapshotSelectionCriteria Criteria;

        /// <inheritdoc/>
        public bool Equals(DeleteSnapshots other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(PersistenceId, other.PersistenceId)
                   && Equals(Criteria, other.Criteria);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteSnapshots);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((PersistenceId != null ? PersistenceId.GetHashCode() : 0) * 397) ^ (Criteria != null ? Criteria.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"DeleteSnapshots<pid: {PersistenceId}, criteria: {Criteria}>";
    }
}
