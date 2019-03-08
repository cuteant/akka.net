//-----------------------------------------------------------------------
// <copyright file="PrimitiveSerializers.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Actor;
using CuteAnt.Text;

namespace Akka.Serialization
{
    public sealed class PrimitiveSerializers : Serializer
    {
        private static readonly Encoding s_encodingUtf8 = StringHelper.UTF8NoBOM;
        private static readonly Encoding s_decodingUtf8 = Encoding.UTF8;

        /// <summary>Initializes a new instance of the <see cref="PrimitiveSerializers" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public PrimitiveSerializers(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override object DeepCopy(object source) => source;

        public override object FromBinary(byte[] bytes, Type type)
        {
            return s_decodingUtf8.GetString(bytes);
        }

        public override byte[] ToBinary(object obj)
        {
            return s_encodingUtf8.GetBytes((string)obj);
        }
    }
}
