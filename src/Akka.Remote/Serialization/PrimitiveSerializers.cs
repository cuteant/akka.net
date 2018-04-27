﻿//-----------------------------------------------------------------------
// <copyright file="PrimitiveSerializers.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using CuteAnt;
using Akka.Actor;
using Akka.Serialization;
using Akka.Util;

namespace Akka.Remote.Serialization
{
    public sealed class PrimitiveSerializers : Serializer
    {
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
                case string str:
                    return Encoding.UTF8.GetBytes(str);
                case int intValue:
                    return BitConverter.GetBytes((int)obj);
                case long longValue:
                    return BitConverter.GetBytes((long)obj);
                default:

                    throw new ArgumentException($"Cannot serialize object of type [{obj.GetType().TypeQualifiedName()}]");
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == TypeConstants.StringType) return Encoding.UTF8.GetString(bytes);
            if (type == TypeConstants.IntType) return BitConverter.ToInt32(bytes, 0);
            if (type == TypeConstants.LongType) return BitConverter.ToInt64(bytes, 0);

            throw new ArgumentException($"Unimplemented deserialization of message with manifest [{type.TypeQualifiedName()}] in [${nameof(PrimitiveSerializers)}]");
        }
    }
}
