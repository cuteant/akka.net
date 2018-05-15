﻿//-----------------------------------------------------------------------
// <copyright file="ProtobufSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Util;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Serialization
{
    /// <summary>This is a special <see cref="Serializer"/> that serializes and deserializes Google protobuf messages only.</summary>
    public sealed class ProtobufSerializer : Serializer
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, MessageParser> TypeLookup = new ConcurrentDictionary<RuntimeTypeHandle, MessageParser>();

        /// <summary>Initializes a new instance of the <see cref="ProtobufSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public ProtobufSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override bool IncludeManifest => true;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            if (obj is IMessage message)
            {
                return message.ToByteArray();
            }

            throw new ArgumentException($"Can't serialize a non-protobuf message using protobuf [{obj.GetType().TypeQualifiedName()}]");
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (TypeLookup.TryGetValue(type.TypeHandle, out var parser))
            {
                return parser.ParseFrom(bytes);
            }
            // MethodParser is not in the cache, look it up with reflection
            if (ActivatorUtils.FastCreateInstance(type) is IMessage msg)
            {
                parser = msg.Descriptor.Parser;
                TypeLookup.TryAdd(type.TypeHandle, parser);
                return parser.ParseFrom(bytes);
            }
            throw new ArgumentException($"Can't deserialize a non-protobuf message using protobuf [{type.TypeQualifiedName()}]");
        }
    }
}