//-----------------------------------------------------------------------
// <copyright file="MsgPackSerializersTests.cs" company="Akka.NET Project">
//     Copyright (C) 2017 Akka.NET Contrib <https://github.com/AkkaNetContrib/Akka.Serialization.MessagePack>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Serialization.Testkit;
using Xunit;

namespace Akka.Serialization.MessagePack.Tests
{
    public class LZ4MsgPackAkkaMessagesTests : AkkaMessagesTests
    {
        public LZ4MsgPackAkkaMessagesTests() : base(typeof(LZ4MsgPackSerializer))
        {
        }
    }

    public class LZ4MsgPackCollectionsTests : CollectionsTests
    {
        public LZ4MsgPackCollectionsTests() : base(typeof(LZ4MsgPackSerializer))
        {
        }
    }

    public class LZ4MsgPackCustomMessagesTests : CustomMessagesTests
    {
        public LZ4MsgPackCustomMessagesTests() : base(typeof(LZ4MsgPackSerializer))
        {
        }
    }

//#if SERIALIZABLE
    public class LZ4MsgPackExceptionsTests : ExceptionsTests
    {
        public LZ4MsgPackExceptionsTests() : base(typeof(LZ4MsgPackSerializer))
        {
        }
    }
//#endif

    public class LZ4MsgPackImmutableMessagesTests : ImmutableMessagesTests
    {
        public LZ4MsgPackImmutableMessagesTests() : base(typeof(LZ4MsgPackSerializer))
        {
        }
    }

    //public class LZ4MsgPackPolymorphismTests : PolymorphismTests
    //{
    //    public LZ4MsgPackPolymorphismTests() : base(typeof(LZ4MsgPackSerializer))
    //    {
    //    }
    //}

    public class LZ4MsgPackIncapsulationTests : IncapsulationTests
    {
        public LZ4MsgPackIncapsulationTests() : base(typeof(LZ4MsgPackSerializer))
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

    public class LZ4MsgPackPrimiviteSerializerTests : PrimitiveSerializerTests
    {
        public LZ4MsgPackPrimiviteSerializerTests() : base(typeof(LZ4MsgPackSerializer))
        {
        }
    }
}
