﻿//-----------------------------------------------------------------------
// <copyright file="ReplicatedDataSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using Akka.Actor;
using Akka.Util;
using CuteAnt.Buffers;
using Hyperion;
using Serializer = Akka.Serialization.Serializer;

namespace Akka.DistributedData.Serialization
{
    public sealed class ReplicatedDataSerializer : Serializer
    {
        private readonly Hyperion.Serializer _serializer;
        
        public ReplicatedDataSerializer(ExtendedActorSystem system) : base(system)
        {
            var akkaSurrogate =
                Surrogate
                .Create<ISurrogated, ISurrogate>(
                from => from.ToSurrogate(system),
                to => to.FromSurrogate(system));

            _serializer =
                new Hyperion.Serializer(new SerializerOptions(
                    preserveObjectReferences: true,
                    versionTolerance: true,
                    surrogates: new[]
                    {
                        akkaSurrogate
                    }));
        }

        private const int c_initialBufferSize = 1024 * 80;
        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>A byte array containing the serialized object </returns>
        public override byte[] ToBinary(object obj)
        {
            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(c_initialBufferSize);

                _serializer.Serialize(obj, outputStream);
                return outputStream.ToByteArray();
            }
        }

        /// <summary>
        /// Deserializes a byte array into an object of type <paramref name="type" />.
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, Type type)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var res = _serializer.Deserialize(ms);
                return res;
            }
        }
    }
}
