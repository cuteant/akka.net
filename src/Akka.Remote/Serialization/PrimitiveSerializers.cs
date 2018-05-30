//-----------------------------------------------------------------------
// <copyright file="PrimitiveSerializers.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CuteAnt;
using CuteAnt.Text;
using Akka.Actor;
using Akka.Serialization;
using Akka.Util;
using Google.Protobuf;

namespace Akka.Remote.Serialization
{
    public sealed class PrimitiveSerializers : Serializer
    {
        private static readonly Encoding s_encodingUtf8 = StringHelper.UTF8NoBOM;
        private static readonly Encoding s_decodingUtf8 = Encoding.UTF8;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSerializers" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public PrimitiveSerializers(ExtendedActorSystem system) : base(system)
        {
        }

        /// <inheritdoc />
        public override bool IncludeManifest { get; } = true;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case ByteString bytes:
                    return ProtobufUtil.GetBuffer(bytes);
                case string str:
                    return s_encodingUtf8.GetBytes(str);
                case int intValue:
                    return BitConverter.GetBytes((int)obj);
                case long longValue:
                    return BitConverter.GetBytes((long)obj);
                default:
                    throw new ArgumentException($"Cannot serialize object of type [{obj.GetType().TypeQualifiedName()}]");
            }
        }

        private static readonly Dictionary<Type, Func<byte[], object>> s_fromBinaryMap = new Dictionary<Type, Func<byte[], object>>()
        {
            { TypeConstants.StringType, bytes => s_decodingUtf8.GetString(bytes) },
            { TypeConstants.IntType, bytes => BitConverter.ToInt32(bytes, 0) },
            { TypeConstants.LongType, bytes => BitConverter.ToInt64(bytes, 0) },
            { typeof(ByteString), bytes => ProtobufUtil.FromBytes(bytes) },
        };

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (s_fromBinaryMap.TryGetValue(type, out var factory))
            {
                return factory(bytes);
            }

            throw new ArgumentException($"Unimplemented deserialization of message with manifest [{type.TypeQualifiedName()}] in [${nameof(PrimitiveSerializers)}]");
        }
    }
}
