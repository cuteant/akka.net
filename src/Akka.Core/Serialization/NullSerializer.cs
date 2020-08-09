//-----------------------------------------------------------------------
// <copyright file="NullSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Serialization.Protocol;

namespace Akka.Serialization
{
    /// <summary>
    /// This is a special <see cref="Serializer"/> that serializes and deserializes nulls only
    /// </summary>
    public class NullSerializer : Serializer
    {
        private static readonly byte[] EmptyBytes = { };

        /// <summary>
        /// Initializes a new instance of the <see cref="NullSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public NullSerializer(ExtendedActorSystem system)
            : base(system)
        {
        }

        /// <summary>
        /// Completely unique value to identify this implementation of the <see cref="Serializer"/> used to optimize network traffic
        /// </summary>
        public override int Identifier => 0;

        /// <inheritdoc />
        public override object DeepCopy(object source) => null;

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj) => EmptyBytes;

        /// <summary>
        /// Deserializes a byte array into an object of type <paramref name="type"/>
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, Type type) => null;

        /// <inheritdoc />
        public override Payload ToPayload(object obj) => Payload.Null;

        ///// <inheritdoc />
        //public override Payload ToPayloadWithAddress(Address address, object obj) => Payload.Null;

        /// <inheritdoc />
        public override object FromPayload(in Payload payload) => null;

        /// <inheritdoc />
        public override ExternalPayload ToExternalPayload(object obj) => ExternalPayload.Null;

        /// <inheritdoc />
        public override object FromExternalPayload(in ExternalPayload payload) => null;
    }
}
