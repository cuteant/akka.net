//-----------------------------------------------------------------------
// <copyright file="JournalProtocol.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using MessagePack;

namespace Akka.Persistence
{
    /// <summary>
    /// Marker interface for internal journal messages
    /// </summary>
    public interface IJournalMessage : IPersistenceMessage { }

    /// <summary>
    /// Internal journal command
    /// </summary>
    public interface IJournalRequest : IJournalMessage { }

    /// <summary>
    /// Internal journal acknowledgement
    /// </summary>
    public interface IJournalResponse : IJournalMessage { }

    /// <summary>
    /// Reply message to a successful <see cref="Eventsourced.DeleteMessages"/> request.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteMessagesSuccess : IEquatable<DeleteMessagesSuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessagesSuccess"/> class.
        /// </summary>
        /// <param name="toSequenceNr">Inclusive upper sequence number bound where a replay should end.</param>
        [SerializationConstructor]
        public DeleteMessagesSuccess(long toSequenceNr)
        {
            ToSequenceNr = toSequenceNr;
        }

        /// <summary>
        /// Inclusive upper sequence number bound where a replay should end.
        /// </summary>
        [Key(0)]
        public readonly long ToSequenceNr;

        /// <inheritdoc/>
        public bool Equals(DeleteMessagesSuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return ToSequenceNr == other.ToSequenceNr;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteMessagesSuccess);

        /// <inheritdoc/>
        public override int GetHashCode() => ToSequenceNr.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"DeleteMessagesSuccess<toSequenceNr: {ToSequenceNr}>";
    }

    /// <summary>
    /// Reply message to failed <see cref="Eventsourced.DeleteMessages"/> request.
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteMessagesFailure : IEquatable<DeleteMessagesFailure>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessagesFailure"/> class.
        /// </summary>
        /// <param name="cause">Failure cause.</param>
        /// <param name="toSequenceNr">Inclusive upper sequence number bound where a replay should end.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="cause"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public DeleteMessagesFailure(Exception cause, long toSequenceNr)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause), "DeleteMessagesFailure cause exception cannot be null");
            ToSequenceNr = toSequenceNr;
        }

        /// <summary>
        /// Failure cause.
        /// </summary>
        [Key(0)]
        public readonly Exception Cause;

        /// <summary>
        /// Inclusive upper sequence number bound where a replay should end.
        /// </summary>
        [Key(1)]
        public readonly long ToSequenceNr;

        /// <inheritdoc/>
        public bool Equals(DeleteMessagesFailure other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause) && ToSequenceNr == other.ToSequenceNr;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteMessagesFailure);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Cause != null ? Cause.GetHashCode() : 0) * 397) ^ ToSequenceNr.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"DeleteMessagesFailure<cause: {Cause}, toSequenceNr: {ToSequenceNr}>";
    }

    /// <summary>
    /// Request to delete all persistent messages with sequence numbers up to `toSequenceNr` (inclusive).  
    /// </summary>
    [MessagePackObject]
    public sealed class DeleteMessagesTo : IJournalRequest, IEquatable<DeleteMessagesTo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessagesTo"/> class.
        /// </summary>
        /// <param name="persistenceId">Requesting persistent actor id.</param>
        /// <param name="toSequenceNr">Sequence number where replay should end (inclusive).</param>
        /// <param name="persistentActor">Requesting persistent actor.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="persistenceId"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public DeleteMessagesTo(string persistenceId, long toSequenceNr, IActorRef persistentActor)
        {
            if (string.IsNullOrEmpty(persistenceId))
                throw new ArgumentNullException(nameof(persistenceId), "DeleteMessagesTo requires persistence id to be provided");

            PersistenceId = persistenceId;
            ToSequenceNr = toSequenceNr;
            PersistentActor = persistentActor;
        }

        /// <summary>
        /// Requesting persistent actor id.
        /// </summary>
        [Key(0)]
        public string PersistenceId { get; }

        /// <summary>
        /// Sequence number where replay should end (inclusive).
        /// </summary>
        [Key(1)]
        public long ToSequenceNr { get; }

        /// <summary>
        /// Requesting persistent actor.
        /// </summary>
        [Key(2)]
        public IActorRef PersistentActor { get; }

        /// <inheritdoc/>
        public bool Equals(DeleteMessagesTo other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(PersistenceId, other.PersistenceId, StringComparison.Ordinal) &&
                   ToSequenceNr == other.ToSequenceNr &&
                   Equals(PersistentActor, other.PersistentActor);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DeleteMessagesTo);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (PersistenceId != null ? PersistenceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ToSequenceNr.GetHashCode();
                hashCode = (hashCode * 397) ^ (PersistentActor != null ? PersistentActor.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"DeleteMessagesTo<pid: {PersistenceId}, seqNr: {ToSequenceNr}, persistentActor: {PersistentActor}>";
    }

    /// <summary>
    /// Request to write messages.
    /// </summary>
    [MessagePackObject]
    public sealed class WriteMessages : IJournalRequest, INoSerializationVerificationNeeded, IEquatable<WriteMessages>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessages"/> class.
        /// </summary>
        /// <param name="messages">Messages to be written.</param>
        /// <param name="persistentActor">Write requester.</param>
        /// <param name="actorInstanceId">TBD</param>
        [SerializationConstructor]
        public WriteMessages(IEnumerable<IPersistentEnvelope> messages, IActorRef persistentActor, int actorInstanceId)
        {
            Messages = messages;
            PersistentActor = persistentActor;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Messages to be written.
        /// </summary>
        [Key(0)]
        public readonly IEnumerable<IPersistentEnvelope> Messages;

        /// <summary>
        /// Write requester.
        /// </summary>
        [Key(1)]
        public readonly IActorRef PersistentActor;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public readonly int ActorInstanceId;

        /// <inheritdoc/>
        public bool Equals(WriteMessages other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(PersistentActor, other.PersistentActor)
                   && Equals(Messages, other.Messages);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as WriteMessages);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Messages != null ? Messages.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PersistentActor != null ? PersistentActor.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActorInstanceId;
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"WriteMessages<actorInstanceId: {ActorInstanceId}, actor: {PersistentActor}>";
    }

    /// <summary>
    /// Reply message to a successful <see cref="WriteMessages"/> request. This reply is sent 
    /// to the requester before all subsequent <see cref="WriteMessageSuccess"/> replies.
    /// </summary>
    public sealed class WriteMessagesSuccessful : IJournalResponse, ISingletonMessage
    {
        /// <summary>
        /// The singleton instance of <see cref="WriteMessagesSuccessful"/>.
        /// </summary>
        public static readonly WriteMessagesSuccessful Instance = new WriteMessagesSuccessful();

        private WriteMessagesSuccessful() { }
    }

    /// <summary>
    /// Reply message to a failed <see cref="WriteMessages"/> request. This reply is sent 
    /// to the requester before all subsequent <see cref="WriteMessageFailure"/> replies.
    /// </summary>
    [MessagePackObject]
    public sealed class WriteMessagesFailed : IJournalResponse, IEquatable<WriteMessagesFailed>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessagesFailed"/> class.
        /// </summary>
        /// <param name="cause">The cause of the failed <see cref="WriteMessages"/> request.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="cause"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public WriteMessagesFailed(Exception cause)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause), "WriteMessagesFailed cause exception cannot be null");
        }

        /// <summary>
        /// The cause of the failed <see cref="WriteMessages"/> request.
        /// </summary>
        [Key(0)]
        public readonly Exception Cause;

        /// <inheritdoc/>
        public bool Equals(WriteMessagesFailed other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as WriteMessagesFailed);

        /// <inheritdoc/>
        public override int GetHashCode() => Cause != null ? Cause.GetHashCode() : 0;

        /// <inheritdoc/>
        public override string ToString() => $"WriteMessagesFailed<cause: {Cause}>";
    }

    /// <summary>
    /// Reply message to a successful <see cref="WriteMessages"/> request. For each contained 
    /// <see cref="IPersistentRepresentation"/> message in the request, a separate reply is sent to the requester.
    /// </summary>
    [MessagePackObject]
    public sealed class WriteMessageSuccess : IJournalResponse, IEquatable<WriteMessageSuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageSuccess"/> class.
        /// </summary>
        /// <param name="persistent">Successfully written message.</param>
        /// <param name="actorInstanceId">TBD</param>
        [SerializationConstructor]
        public WriteMessageSuccess(IPersistentRepresentation persistent, int actorInstanceId)
        {
            Persistent = persistent;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Successfully written message.
        /// </summary>
        [Key(0)]
        public readonly IPersistentRepresentation Persistent;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly int ActorInstanceId;

        /// <inheritdoc/>
        public bool Equals(WriteMessageSuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Persistent, other.Persistent);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as WriteMessageSuccess);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Persistent != null ? Persistent.GetHashCode() : 0) * 397) ^ ActorInstanceId;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"WriteMessageSuccess<actorInstanceId: {ActorInstanceId}, message: {Persistent}>";
    }

    /// <summary>
    /// Reply message to a rejected <see cref="WriteMessages"/> request. The write of this message was rejected
    /// before it was stored, e.g. because it could not be serialized. For each contained 
    /// <see cref="IPersistentRepresentation"/> message in the request, a separate reply is sent to the requester.
    /// </summary>
    [MessagePackObject]
    public sealed class WriteMessageRejected : IJournalResponse, INoSerializationVerificationNeeded, IEquatable<WriteMessageRejected>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageRejected"/> class.
        /// </summary>
        /// <param name="persistent">Message rejected to be written.</param>
        /// <param name="cause">Failure cause.</param>
        /// <param name="actorInstanceId">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="cause"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public WriteMessageRejected(IPersistentRepresentation persistent, Exception cause, int actorInstanceId)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause), "WriteMessageRejected cause exception cannot be null");
            Persistent = persistent;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Message failed to be written.
        /// </summary>
        [Key(0)]
        public readonly IPersistentRepresentation Persistent;

        /// <summary>
        /// The cause of the failure
        /// </summary>
        [Key(1)]
        public readonly Exception Cause;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public readonly int ActorInstanceId;

        /// <inheritdoc/>
        public bool Equals(WriteMessageRejected other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Persistent, other.Persistent)
                   && Equals(Cause, other.Cause);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as WriteMessageRejected);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Cause != null ? Cause.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActorInstanceId;
                hashCode = (hashCode * 397) ^ (Persistent != null ? Persistent.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"WriteMessageRejected<actorInstanceId: {ActorInstanceId}, message: {Persistent}, cause: {Cause}>";
    }

    /// <summary>
    /// Reply message to a failed <see cref="WriteMessages"/> request. For each contained 
    /// <see cref="IPersistentRepresentation"/> message in the request, a separate reply is sent to the requester.
    /// </summary>
    [MessagePackObject]
    public sealed class WriteMessageFailure : IJournalResponse, INoSerializationVerificationNeeded, IEquatable<WriteMessageFailure>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageFailure"/> class.
        /// </summary>
        /// <param name="persistent">Message failed to be written.</param>
        /// <param name="cause">Failure cause.</param>
        /// <param name="actorInstanceId">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="cause"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public WriteMessageFailure(IPersistentRepresentation persistent, Exception cause, int actorInstanceId)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause), "WriteMessageFailure cause exception cannot be null");
            Persistent = persistent;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// Message failed to be written.
        /// </summary>
        [Key(0)]
        public readonly IPersistentRepresentation Persistent;

        /// <summary>
        /// The cause of the failure
        /// </summary>
        [Key(1)]
        public readonly Exception Cause;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public readonly int ActorInstanceId;

        /// <inheritdoc/>
        public bool Equals(WriteMessageFailure other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Persistent, other.Persistent)
                   && Equals(Cause, other.Cause);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as WriteMessageFailure);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Cause != null ? Cause.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActorInstanceId;
                hashCode = (hashCode * 397) ^ (Persistent != null ? Persistent.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"WriteMessageFailure<actorInstanceId: {ActorInstanceId}, message: {Persistent}, cause: {Cause}>";
    }

    /// <summary>
    /// Reply message to a <see cref="WriteMessages"/> with a non-persistent message.
    /// </summary>
    [MessagePackObject]
    public sealed class LoopMessageSuccess : IJournalResponse, INoSerializationVerificationNeeded, IEquatable<LoopMessageSuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoopMessageSuccess"/> class.
        /// </summary>
        /// <param name="message">A looped message.</param>
        /// <param name="actorInstanceId">TBD</param>
        [SerializationConstructor]
        public LoopMessageSuccess(object message, int actorInstanceId)
        {
            Message = message;
            ActorInstanceId = actorInstanceId;
        }

        /// <summary>
        /// A looped message.
        /// </summary>
        [Key(0)]
        public readonly object Message;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly int ActorInstanceId;

        /// <inheritdoc/>
        public bool Equals(LoopMessageSuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(ActorInstanceId, other.ActorInstanceId)
                   && Equals(Message, other.Message);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as LoopMessageSuccess);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Message != null ? Message.GetHashCode() : 0) * 397) ^ ActorInstanceId;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"LoopMessageSuccess<actorInstanceId: {ActorInstanceId}, message: {Message}>";
    }

    /// <summary>
    /// Request to replay messages to the <see cref="PersistentActor"/>.
    /// </summary>
    [MessagePackObject]
    public sealed class ReplayMessages : IJournalRequest, IEquatable<ReplayMessages>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayMessages"/> class.
        /// </summary>
        /// <param name="fromSequenceNr">Sequence number where replay should start (inclusive).</param>
        /// <param name="toSequenceNr">Sequence number where replay should end (inclusive).</param>
        /// <param name="max">Maximum number of messages to be replayed.</param>
        /// <param name="persistenceId">Requesting persistent actor identifier.</param>
        /// <param name="persistentActor">Requesting persistent actor.</param>
        [SerializationConstructor]
        public ReplayMessages(long fromSequenceNr, long toSequenceNr, long max, string persistenceId, IActorRef persistentActor)
        {
            FromSequenceNr = fromSequenceNr;
            ToSequenceNr = toSequenceNr;
            Max = max;
            PersistenceId = persistenceId;
            PersistentActor = persistentActor;
        }

        /// <summary>
        /// Inclusive lower sequence number bound where a replay should start.
        /// </summary>
        [Key(0)]
        public readonly long FromSequenceNr;

        /// <summary>
        /// Inclusive upper sequence number bound where a replay should end.
        /// </summary>
        [Key(1)]
        public readonly long ToSequenceNr;

        /// <summary>
        /// Maximum number of messages to be replayed.
        /// </summary>
        [Key(2)]
        public readonly long Max;

        /// <summary>
        /// Requesting persistent actor identifier.
        /// </summary>
        [Key(3)]
        public readonly string PersistenceId;

        /// <summary>
        /// Requesting persistent actor.
        /// </summary>
        [Key(4)]
        public readonly IActorRef PersistentActor;

        /// <inheritdoc/>
        public bool Equals(ReplayMessages other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(PersistenceId, other.PersistenceId)
                   && Equals(PersistentActor, other.PersistentActor)
                   && Equals(FromSequenceNr, other.FromSequenceNr)
                   && Equals(ToSequenceNr, other.ToSequenceNr)
                   && Equals(Max, other.Max);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as ReplayMessages);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FromSequenceNr.GetHashCode();
                hashCode = (hashCode * 397) ^ ToSequenceNr.GetHashCode();
                hashCode = (hashCode * 397) ^ Max.GetHashCode();
                hashCode = (hashCode * 397) ^ (PersistenceId != null ? PersistenceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PersistentActor != null ? PersistentActor.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"ReplayMessages<fromSequenceNr: {FromSequenceNr}, toSequenceNr: {ToSequenceNr}, max: {Max}, persistenceId: {PersistenceId}>";
    }

    /// <summary>
    /// Reply message to a <see cref="ReplayMessages"/> request. A separate reply is sent to the requester for each replayed message.
    /// </summary>
    [MessagePackObject]
    public sealed class ReplayedMessage : IJournalResponse, IDeadLetterSuppression, INoSerializationVerificationNeeded, IEquatable<ReplayedMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayedMessage"/> class.
        /// </summary>
        /// <param name="persistent">Replayed message.</param>
        [SerializationConstructor]
        public ReplayedMessage(IPersistentRepresentation persistent)
        {
            Persistent = persistent;
        }

        /// <summary>
        /// Replayed message.
        /// </summary>
        [Key(0)]
        public readonly IPersistentRepresentation Persistent;

        /// <inheritdoc/>
        public bool Equals(ReplayedMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Persistent, other.Persistent);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as ReplayedMessage);

        /// <inheritdoc/>
        public override int GetHashCode() => Persistent != null ? Persistent.GetHashCode() : 0;

        /// <inheritdoc/>
        public override string ToString() => $"ReplayedMessage<message: {Persistent}>";
    }

    /// <summary>
    /// Reply message to a successful <see cref="ReplayMessages"/> request. This reply is sent 
    /// to the requester after all <see cref="ReplayedMessage"/> have been sent (if any).
    /// 
    /// It includes the highest stored sequence number of a given persistent actor.
    /// Note that the replay might have been limited to a lower sequence number.
    /// </summary>
    [MessagePackObject]
    public sealed class RecoverySuccess : IJournalResponse, IDeadLetterSuppression, IEquatable<RecoverySuccess>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecoverySuccess"/> class.
        /// </summary>
        /// <param name="highestSequenceNr">Highest stored sequence number.</param>
        [SerializationConstructor]
        public RecoverySuccess(long highestSequenceNr)
        {
            HighestSequenceNr = highestSequenceNr;
        }

        /// <summary>
        /// Highest stored sequence number.
        /// </summary>
        [Key(0)]
        public readonly long HighestSequenceNr;

        /// <inheritdoc/>
        public bool Equals(RecoverySuccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(HighestSequenceNr, other.HighestSequenceNr);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as RecoverySuccess);

        /// <inheritdoc/>
        public override int GetHashCode() => HighestSequenceNr.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"RecoverySuccess<highestSequenceNr: {HighestSequenceNr}>";
    }

    /// <summary>
    /// Reply message to a failed <see cref="ReplayMessages"/> request. This reply is sent to the requester
    /// if a replay could not be successfully completed.
    /// </summary>
    [MessagePackObject]
    public sealed class ReplayMessagesFailure : IJournalResponse, IDeadLetterSuppression, IEquatable<ReplayMessagesFailure>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayMessagesFailure"/> class.
        /// </summary>
        /// <param name="cause">The cause of the failure.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="cause"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public ReplayMessagesFailure(Exception cause)
        {
            Cause = cause ?? throw new ArgumentNullException(nameof(cause), "ReplayMessagesFailure cause exception cannot be null");
        }

        /// <summary>
        /// The cause of the failure
        /// </summary>
        [Key(0)]
        public readonly Exception Cause;

        /// <inheritdoc/>
        public bool Equals(ReplayMessagesFailure other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Cause, other.Cause);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as ReplayMessagesFailure);

        /// <inheritdoc/>
        public override int GetHashCode() => Cause.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"ReplayMessagesFailure<cause: {Cause}>";
    }
}
