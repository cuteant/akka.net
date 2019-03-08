using Akka.Serialization.Protocol;
using MessagePack;

namespace Akka.Remote.TestKit.Protocol
{
    public interface IMessage { }

    [MessagePackObject]
    public sealed class Wrapper : IMessage
    {
        [Key(0)]
        public Hello Hello { get; set; }

        [Key(1)]
        public EnterBarrier Barrier { get; set; }

        [Key(2)]
        public InjectFailure Failure { get; set; }

        [Key(3)]
        public string Done { get; set; }

        [Key(4)]
        public AddressRequest Addr { get; set; }
    }

    [MessagePackObject]
    public sealed class Hello : IMessage
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public AddressData Address { get; set; }
    }

    [MessagePackObject]
    public sealed class EnterBarrier : IMessage
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public BarrierOp Op { get; set; }

        [Key(2)]
        public long Timeout { get; set; }
    }

    [MessagePackObject]
    public sealed class AddressRequest : IMessage
    {
        [Key(0)]
        public string Node { get; set; }

        [Key(1)]
        public AddressData Addr { get; set; }

    }

    [MessagePackObject]
    public sealed class InjectFailure : IMessage
    {
        [Key(0)]
        public FailType Failure { get; set; }

        [Key(1)]
        public Direction Direction { get; set; }

        [Key(2)]
        public AddressData Address { get; set; }

        [Key(3)]
        public float RateMBit { get; set; }

        [Key(4)]
        public int ExitValue { get; set; }
    }

    public enum BarrierOp
    {
        Enter = 0,
        Fail = 1,
        Succeeded = 2,
        Failed = 3
    }

    public enum FailType
    {
        Throttle = 0,
        Disconnect = 1,
        Abort = 2,
        Exit = 3,
        Shutdown = 4,
        ShutdownAbrupt = 5
    }

    public enum Direction
    {
        Send = 0,
        Receive = 1,
        Both = 2,
    }
}
