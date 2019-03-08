using MessagePack;

namespace Akka.Serialization.Protocol
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
        public readonly ReadOnlyActorRefData Child;

        [Key(1)]
        public readonly bool Async;

        [SerializationConstructor]
        public SuperviseData(ReadOnlyActorRefData child, bool async)
        {
            Child = child;
            Async = async;
        }
    }

    [MessagePackObject]
    public readonly struct WatchData
    {
        [Key(0)]
        public readonly ReadOnlyActorRefData Watchee;

        [Key(1)]
        public readonly ReadOnlyActorRefData Watcher;

        [SerializationConstructor]
        public WatchData(ReadOnlyActorRefData watchee, ReadOnlyActorRefData watcher)
        {
            Watchee = watchee;
            Watcher = watcher;
        }
    }

    [MessagePackObject]
    public readonly struct FailedData
    {
        [Key(0)]
        public readonly ReadOnlyActorRefData Child;

        [Key(1)]
        public readonly ExceptionData Cause;

        [Key(2)]
        public readonly ulong Uid;

        [SerializationConstructor]
        public FailedData(ReadOnlyActorRefData child, ExceptionData cause, ulong uid)
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
        public readonly ReadOnlyActorRefData Actor;

        [Key(1)]
        public readonly bool ExistenceConfirmed;

        [Key(2)]
        public readonly bool AddressTerminated;

        [SerializationConstructor]
        public DeathWatchNotificationData(ReadOnlyActorRefData actor, bool existenceConfirmed, bool addressTerminated)
        {
            Actor = actor;
            ExistenceConfirmed = existenceConfirmed;
            AddressTerminated = addressTerminated;
        }
    }
}
