//-----------------------------------------------------------------------
// <copyright file="MsgPackSerializersTests.cs" company="Akka.NET Project">
//     Copyright (C) 2017 Akka.NET Contrib <https://github.com/AkkaNetContrib/Akka.Serialization.MessagePack>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Serialization.Testkit;
using Xunit;

namespace Akka.Serialization.MessagePack.Tests
{
    public class LZ4TypelessMsgPackAkkaMessagesTests : AkkaMessagesTests
    {
        public LZ4TypelessMsgPackAkkaMessagesTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }

    public class LZ4TypelessMsgPackCollectionsTests : CollectionsTests
    {
        public LZ4TypelessMsgPackCollectionsTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }

    public class LZ4TypelessMsgPackCustomMessagesTests : CustomMessagesTests
    {
        public LZ4TypelessMsgPackCustomMessagesTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }

//#if SERIALIZABLE
    public class LZ4TypelessMsgPackExceptionsTests : ExceptionsTests
    {
        public LZ4TypelessMsgPackExceptionsTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }
//#endif

    public class LZ4TypelessMsgPackImmutableMessagesTests : ImmutableMessagesTests
    {
        public LZ4TypelessMsgPackImmutableMessagesTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }

    public class LZ4TypelessMsgPackPolymorphismTests : PolymorphismTests
    {
        public LZ4TypelessMsgPackPolymorphismTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }

    public class LZ4TypelessMsgPackIncapsulationTests : IncapsulationTests
    {
        public LZ4TypelessMsgPackIncapsulationTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }

        [Fact(Skip = "Not supported yet")]
        public override void Can_Serialize_a_class_with_internal_constructor()
        {
        }

        [Fact(Skip = "Not supported yet")]
        public override void Can_Serialize_a_class_with_private_constructor()
        {
        }

        [Fact(Skip = "Not supported yet")]
        public override void Can_Serialize_internal_class()
        {
        }
    }

    public class LZ4TypelessMsgPackPrimiviteSerializerTests : PrimitiveSerializerTests
    {
        public LZ4TypelessMsgPackPrimiviteSerializerTests() : base(typeof(LZ4MsgPackTypelessSerializer))
        {
        }
    }
}
