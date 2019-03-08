using Akka.Util;
using Xunit;

namespace Akka.Tests.Util
{
    public class StringExtensionsTests
    {
        sealed class IntValue
        {
            public readonly int Value;

            public IntValue(int value) => Value = value;

            public override string ToString()
            {
                return Value + "";
            }
        }

        [Fact]
        public void JoinWithPrefixTest()
        {
            var strs = new string[] { "a", "b", "c" };

            var intVs = new IntValue[] { new IntValue(1), new IntValue(2), new IntValue(3), };

            Assert.Equal("prefix:a-b-c", strs.JoinWithPrefix("-", "prefix:"));
            Assert.Equal("prefix:1-2-3", intVs.JoinWithPrefix("-", "prefix:"));
        }

        [Fact]
        public void JoinWithSuffixTest()
        {
            var strs = new string[] { "a", "b", "c" };

            var intVs = new IntValue[] { new IntValue(1), new IntValue(2), new IntValue(3), };

            Assert.Equal("a-b-c:suffix", strs.JoinWithSuffix("-", ":suffix"));
            Assert.Equal("1-2-3:suffix", intVs.JoinWithSuffix("-", ":suffix"));
        }

        [Fact]
        public void JoinTest()
        {
            var strs = new string[] { "a", "b", "c" };

            var intVs = new IntValue[] { new IntValue(1), new IntValue(2), new IntValue(3), };

            Assert.Equal("a-b-c", strs.Join("-", null, null));
            Assert.Equal("1-2-3", intVs.Join("-", null, null));

            Assert.Equal("prefix:a-b-c:suffix", strs.Join("-", "prefix:", ":suffix"));
            Assert.Equal("prefix:1-2-3:suffix", intVs.Join("-", "prefix:", ":suffix"));
        }
    }
}
