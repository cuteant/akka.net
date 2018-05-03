using System.Text;
using Akka.Serialization;
using Xunit;

namespace Akka.Remote.Tests.Serialization
{
    public class ProtobufUtilSpec
    {
        [Fact]
        public void CanResolveType()
        {
            var type = ProtobufUtil.ByteStringUnsafeType;

            Assert.NotNull(type);
            Assert.Equal("Google.Protobuf.ByteString+Unsafe", type.FullName);
        }

        [Fact]
        public void FromBytesAndGetBuffer()
        {
            var str = "this is a test.";
            var bts = Encoding.ASCII.GetBytes(str);

            var byteString = ProtobufUtil.FromBytes(bts);
            Assert.NotNull(byteString);

            var buffer = ProtobufUtil.GetBuffer(byteString);
            Assert.NotNull(buffer);

            Assert.Equal(bts, buffer);
            Assert.True(ReferenceEquals(bts, buffer));
            Assert.Equal(str, Encoding.ASCII.GetString(buffer));
        }
    }
}
