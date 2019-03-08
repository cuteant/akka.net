//-----------------------------------------------------------------------
// <copyright file="ProtobufSerializerSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Configuration;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using Akka.TestKit;
using MessagePack;
using FluentAssertions;
using Xunit;

namespace Akka.Remote.Tests.Serialization
{
    public class ProtobufSerializerSpec : AkkaSpec
    {
        public ProtobufSerializerSpec() : base(ConfigurationFactory.ParseString("").WithFallback(RemoteConfigFactory.Default()))
        {
        }

        [Fact]
        public void Can_serialize_ProtobufMessage()
        {
            var message = new AddressData("sys", "localhost", 54645, "akka.tcp");
            //AssertEqual(message);

            var serializedBytes = MessagePackSerializer.Serialize(message);
            var deserialized =MessagePackSerializer.Deserialize<AddressData>(serializedBytes);
            Assert.Equal(message.System, deserialized.System);
            Assert.Equal(message.Hostname, deserialized.Hostname);
            Assert.Equal(message.Port, deserialized.Port);
            Assert.Equal(message.Protocol, deserialized.Protocol);
        }

        private T AssertAndReturn<T>(T message)
        {
            var serializer = Sys.Serialization.FindSerializerFor(message);
            serializer.Should().BeOfType<ProtobufSerializer>();
            var serializedBytes = serializer.ToBinary(message);
            return (T)serializer.FromBinary(serializedBytes, typeof(T));
        }

        private void AssertEqual<T>(T message)
        {
            var deserialized = AssertAndReturn(message);
            Assert.Equal(message, deserialized);
        }
    }
}
