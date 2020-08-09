using System;
using Akka.Serialization.Protocol;
using MessagePack;

namespace Akka.Streams.Serialization.Protocol
{
    //[MessagePackObject]
    //public readonly struct EventType
    //{
    //    [Key(0)]
    //    public readonly string TypeName;

    //    [SerializationConstructor]
    //    public EventType(string typeName)
    //    {
    //        TypeName = typeName;
    //    }
    //}

    [MessagePackObject]
    public readonly struct ActorRef
    {
        [Key(0)]
        public readonly string Path;

        [SerializationConstructor]
        public ActorRef(string path)
        {
            Path = path;
        }
    }

    [MessagePackObject]
    public readonly struct SinkRef
    {
        [Key(0)]
        public readonly ActorRef TargetRef;

        [Key(1)]
        public readonly Type EventType;

        [SerializationConstructor]
        public SinkRef(ActorRef targetRef, Type eventType)
        {
            TargetRef = targetRef;
            EventType = eventType;
        }
    }

    [MessagePackObject]
    public readonly struct SourceRef
    {
        [Key(0)]
        public readonly ActorRef OriginRef;

        [Key(1)]
        public readonly Type EventType;

        [SerializationConstructor]
        public SourceRef(ActorRef originRef, Type eventType)
        {
            OriginRef = originRef;
            EventType = eventType;
        }
    }

    // stream refs protocol

    [MessagePackObject]
    public readonly struct OnSubscribeHandshake
    {
        [Key(0)]
        public readonly ActorRef TargetRef;

        [SerializationConstructor]
        public OnSubscribeHandshake(ActorRef targetRef)
        {
            TargetRef = targetRef;
        }
    }

    [MessagePackObject]
    public readonly struct CumulativeDemand
    {
        [Key(0)]
        public readonly long SeqNr;

        [SerializationConstructor]
        public CumulativeDemand(long seqNr)
        {
            SeqNr = seqNr;
        }
    }

    [MessagePackObject]
    public readonly struct SequencedOnNext
    {
        [Key(0)]
        public readonly long SeqNr;

        [Key(1)]
        public readonly Payload Payload;

        [SerializationConstructor]
        public SequencedOnNext(long seqNr, Payload payload)
        {
            SeqNr = seqNr;
            Payload = payload;
        }
    }

    [MessagePackObject]
    public readonly struct RemoteStreamFailure
    {
        [Key(0)]
        public readonly string Cause;

        [SerializationConstructor]
        public RemoteStreamFailure(string cause)
        {
            Cause = cause;
        }
    }

    [MessagePackObject]
    public readonly struct RemoteStreamCompleted
    {
        [Key(0)]
        public readonly long SeqNr;

        [SerializationConstructor]
        public RemoteStreamCompleted(long seqNr)
        {
            SeqNr = seqNr;
        }
    }
}
