using MessagePack;

namespace Akka.Remote.Serialization.Protocol
{
    [MessagePackObject]
    public readonly struct CreateData
    {
        [Key(0)]
        public readonly ExceptionData Cause;

        [SerializationConstructor]
        public CreateData(ExceptionData cause) => Cause = cause;
    }

    [MessagePackObject]
    public readonly struct RecreateData
    {
        [Key(0)]
        public readonly ExceptionData Cause;

        [SerializationConstructor]
        public RecreateData(ExceptionData cause) => Cause = cause;
    }

    [MessagePackObject]
    public readonly struct ResumeData
    {
        [Key(0)]
        public readonly ExceptionData Cause;

        [SerializationConstructor]
        public ResumeData(ExceptionData cause) => Cause = cause;
    }

    [MessagePackObject]
    public readonly struct SuperviseData
    {
        [Key(0)]
        public readonly ActorRefData Child;

        [Key(1)]
        public readonly bool Async;

        [SerializationConstructor]
        public SuperviseData(ActorRefData child, bool async)
        {
            Child = child;
            Async = async;
        }
    }

    [MessagePackObject]
    public readonly struct WatchData
    {
        [Key(0)]
        public readonly ActorRefData Watchee;

        [Key(1)]
        public readonly ActorRefData Watcher;

        [SerializationConstructor]
        public WatchData(ActorRefData watchee, ActorRefData watcher)
        {
            Watchee = watchee;
            Watcher = watcher;
        }
    }

    [MessagePackObject]
    public readonly struct FailedData
    {
        [Key(0)]
        public readonly ActorRefData Child;

        [Key(1)]
        public readonly ExceptionData Cause;

        [Key(2)]
        public readonly ulong Uid;

        [SerializationConstructor]
        public FailedData(ActorRefData child, ExceptionData cause, ulong uid)
        {
            Child = child;
            Cause = cause;
            Uid = uid;
        }
    }

    [MessagePackObject]
    public readonly struct DeathWatchNotificationData
    {
        [Key(0)]
        public readonly ActorRefData Actor;

        [Key(1)]
        public readonly bool ExistenceConfirmed;

        [Key(2)]
        public readonly bool AddressTerminated;

        [SerializationConstructor]
        public DeathWatchNotificationData(ActorRefData actor, bool existenceConfirmed, bool addressTerminated)
        {
            Actor = actor;
            ExistenceConfirmed = existenceConfirmed;
            AddressTerminated = addressTerminated;
        }
    }
}
