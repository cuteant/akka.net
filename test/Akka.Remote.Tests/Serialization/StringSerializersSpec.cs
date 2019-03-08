//-----------------------------------------------------------------------
// <copyright file="PrimitiveSerializersSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Remote.Configuration;
using Akka.Serialization;
using Akka.TestKit;
using FluentAssertions;
using Xunit;

namespace Akka.Remote.Tests.Serialization
{
    public class StringSerializersSpec : AkkaSpec
    {
        public StringSerializersSpec() : base(ConfigurationFactory.ParseString(@"
            akka {
                actor {
                    serializers {
                        string = ""Akka.Serialization.StringSerializer, Akka""
                    }
                    serialization-bindings {
                        ""System.String"" = string
                    }
                }
            }").WithFallback(RemoteConfigFactory.Default()))
        {
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("árvíztűrőütvefúrógép")]
        [InlineData("心有灵犀一点通")]
        public void Can_serialize_String(string value)
        {
            AssertEqual(value);
        }

        private T AssertAndReturn<T>(T message)
        {
            var serializer = Sys.Serialization.FindSerializerFor(message);
            serializer.Should().BeOfType<StringSerializer>();
            var serializedBytes = serializer.ToBinary(message);
            var primitiveSerializer = (StringSerializer)serializer;
            return (T)primitiveSerializer.FromBinary(serializedBytes, null);
        }

        private void AssertEqual<T>(T message)
        {
            var deserialized = AssertAndReturn(message);
            Assert.Equal(message, deserialized);
        }
    }
}
