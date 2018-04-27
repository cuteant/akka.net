//-----------------------------------------------------------------------
// <copyright file="MsgPackSerializersTests.cs" company="Akka.NET Project">
//     Copyright (C) 2017 Akka.NET Contrib <https://github.com/AkkaNetContrib/Akka.Serialization.MessagePack>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Serialization.Testkit;
using Xunit;

namespace Akka.Serialization.MessagePack.Tests
{
    public class TypelessMsgPackAkkaMessagesTests : AkkaMessagesTests
    {
        public TypelessMsgPackAkkaMessagesTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }

    public class TypelessMsgPackCollectionsTests : CollectionsTests
    {
        public TypelessMsgPackCollectionsTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }

    public class TypelessMsgPackCustomMessagesTests : CustomMessagesTests
    {
        public TypelessMsgPackCustomMessagesTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }

//#if SERIALIZABLE
    public class TypelessMsgPackExceptionsTests : ExceptionsTests
    {
        public TypelessMsgPackExceptionsTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }
//#endif

    public class TypelessMsgPackImmutableMessagesTests : ImmutableMessagesTests
    {
        public TypelessMsgPackImmutableMessagesTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }

    public class TypelessMsgPackPolymorphismTests : PolymorphismTests
    {
        public TypelessMsgPackPolymorphismTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }

    public class TypelessMsgPackIncapsulationTests : IncapsulationTests
    {
        public TypelessMsgPackIncapsulationTests() : base(typeof(TypelessMsgPackSerializer))
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

    public class TypelessMsgPackPrimiviteSerializerTests : PrimitiveSerializerTests
    {
        public TypelessMsgPackPrimiviteSerializerTests() : base(typeof(TypelessMsgPackSerializer))
        {
        }
    }
}
