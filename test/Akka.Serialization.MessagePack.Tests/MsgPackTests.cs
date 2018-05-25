using Akka.Tests.Serialization;
using Xunit;

namespace Akka.Serialization.MessagePack.Tests
{
    public class MsgPackTests : AkkaSerializationSpec
    {
        public MsgPackTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }

        [Fact(Skip = "Not supported yet")]
        public override void CanSerializeImmutableMessagesWithPrivateCtor()
        {
        }
    }
}
