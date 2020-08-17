//-----------------------------------------------------------------------
// <copyright file="ProtobufSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Configuration;
using CuteAnt.Buffers;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Serialization
{
    /// <summary>This is a special <see cref="Serializer"/> that serializes and deserializes Google protobuf messages only.</summary>
    public sealed class ProtobufSerializer : SerializerWithTypeManifest
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, MessageParser> TypeLookup = new ConcurrentDictionary<RuntimeTypeHandle, MessageParser>();

        private static readonly ArrayPool<byte> s_bufferPool = BufferManager.Shared;

        private readonly int _initialBufferSize;

        /// <summary>Initializes a new instance of the <see cref="ProtobufSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public ProtobufSerializer(ExtendedActorSystem system) : this(system, ProtobufSerializerSettings.Default) { }

        public ProtobufSerializer(ExtendedActorSystem system, Config config) : this(system, ProtobufSerializerSettings.Create(config)) { }

        public ProtobufSerializer(ExtendedActorSystem system, ProtobufSerializerSettings settings) : base(system) => _initialBufferSize = settings.InitialBufferSize;

        /// <inheritdoc />
        public sealed override int Identifier => 109;

        /// <inheritdoc />
        public override object DeepCopy(object source)
        {
            if (source is null) { return null; }
            dynamic dynamicSource = source;
            return dynamicSource.Clone();
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(_initialBufferSize, s_bufferPool);
                ((IMessage)obj).WriteTo(outputStream);
                return outputStream.ToByteArray();
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (TypeLookup.TryGetValue(type.TypeHandle, out var parser))
            {
                return parser.ParseFrom(bytes);
            }

            return InternalParseFrom(bytes, type);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object InternalParseFrom(byte[] bytes, Type type)
        {
            // MethodParser is not in the cache, look it up with reflection
            var msg = ActivatorUtils.FastCreateInstance<IMessage>(type);
            var parser = msg.Descriptor.Parser;
            TypeLookup.TryAdd(type.TypeHandle, parser);
            return parser.ParseFrom(bytes);
        }
    }
}
