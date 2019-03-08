using System.Collections.Generic;
using MessagePack;

namespace Akka.Serialization.Protocol
{
    #region -- Common types --

    /// <summary>Defines a remote ActorRef that "remembers" and uses its original Actor instance on the original node.</summary>
    [MessagePackObject]
    public readonly struct ReadOnlyActorRefData
    {
        [Key(0)]
        public readonly string Path;

        [SerializationConstructor]
        public ReadOnlyActorRefData(string path) => Path = path;
    }
    [MessagePackObject]
    public sealed class ActorRefData
    {
        [Key(0)]
        public readonly string Path;

        [SerializationConstructor]
        public ActorRefData(string path) => Path = path;
    }

    /// <summary>Defines a remote address.</summary>
    [MessagePackObject]
    public readonly struct AddressData
    {
        [SerializationConstructor]
        public AddressData(string system, string hostname, uint port, string protocol)
        {
            System = system;
            Hostname = hostname;
            Port = port;
            Protocol = protocol;
        }

        [Key(0)]
        public readonly string System;

        [Key(1)]
        public readonly string Hostname;

        [Key(2)]
        public readonly uint Port;

        [Key(3)]
        public readonly string Protocol;
    }

    [MessagePackObject]
    public readonly struct Identify
    {
        [Key(0)]
        public readonly Payload MessageId;

        [SerializationConstructor]
        public Identify(Payload messageId) => MessageId = messageId;
    }

    [MessagePackObject]
    public readonly struct ActorIdentity
    {
        [Key(0)]
        public readonly Payload CorrelationId;

        [Key(1)]
        public readonly string Path;

        [SerializationConstructor]
        public ActorIdentity(Payload correlationId, string path)
        {
            CorrelationId = correlationId;
            Path = path;
        }
    }

    [MessagePackObject]
    public readonly struct RemoteWatcherHeartbeatResponse
    {
        [Key(0)]
        public readonly ulong Uid;

        [SerializationConstructor]
        public RemoteWatcherHeartbeatResponse(ulong uid) => Uid = uid;
    }

    [MessagePackObject]
    public sealed class ExceptionData
    {
        [Key(0)]
        public string TypeName { get; set; }

        [Key(1)]
        public string Message { get; set; }

        [Key(2)]
        public string StackTrace { get; set; }

        [Key(3)]
        public string Source { get; set; }

        [Key(4)]
        public ExceptionData InnerException { get; set; }

        [Key(5)]
        public Dictionary<string, Payload> CustomFields { get; set; }
    }

    #endregion

    #region -- ActorSelection related formats --

    [MessagePackObject]
    public readonly struct SelectionEnvelope
    {
        [SerializationConstructor]
        public SelectionEnvelope(Payload payload, List<Selection> pattern)
        {
            Payload = payload;
            Pattern = pattern;
        }

        [Key(0)]
        public readonly Payload Payload;

        [Key(1)]
        public readonly List<Selection> Pattern;
    }

    [MessagePackObject]
    public readonly struct Selection
    {
        public enum PatternType
        {
            NoPatern = 0,
            Parent = 1,
            ChildName = 2,
            ChildPattern = 3,
        }

        [SerializationConstructor]
        public Selection(PatternType type, string matcher)
        {
            Type = type;
            Matcher = matcher;
        }

        [Key(0)]
        public readonly PatternType Type;

        [Key(1)]
        public readonly string Matcher;
    }

    #endregion
}
