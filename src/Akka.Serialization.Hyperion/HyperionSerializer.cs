//-----------------------------------------------------------------------
// <copyright file="HyperionSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using CuteAnt.Extensions.Serialization;
using CuteAnt.Reflection;
using Hyperion;

// ReSharper disable once CheckNamespace
namespace Akka.Serialization
{
    /// <summary>
    /// This is a special <see cref="Serializer"/> that serializes and deserializes plain old CLR
    /// objects (POCOs).
    /// </summary>
    public sealed class HyperionSerializer : Serializer
    {
        /// <summary>Settings used for an underlying Hyperion serializer implementation.</summary>
        public readonly HyperionSerializerSettings Settings;

        private readonly HyperionMessageFormatter _serializer;
        private readonly int _initialBufferSize;

        /// <summary>Initializes a new instance of the <see cref="HyperionSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public HyperionSerializer(ExtendedActorSystem system)
            : this(system, HyperionSerializerSettings.Default)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="HyperionSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        /// <param name="config">Configuration passed from related HOCON config path.</param>
        public HyperionSerializer(ExtendedActorSystem system, Config config)
            : this(system, HyperionSerializerSettings.Create(config))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="HyperionSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        /// <param name="settings">Serializer settings.</param>
        public HyperionSerializer(ExtendedActorSystem system, HyperionSerializerSettings settings)
            : base(system)
        {
            this.Settings = settings;
            var akkaSurrogate =
                Surrogate
                .Create<ISurrogated, ISurrogate>(
                from => from.ToSurrogate(system),
                to => to.FromSurrogate(system));

            var provider = CreateKnownTypesProvider(system, settings.KnownTypesProvider);

            _initialBufferSize = settings.InitialBufferSize;
            _serializer =
                new HyperionMessageFormatter(new SerializerOptions(
                    preserveObjectReferences: settings.PreserveObjectReferences,
                    versionTolerance: settings.VersionTolerance,
                    surrogates: new[] { akkaSurrogate },
                    knownTypes: provider.GetKnownTypes(),
                    ignoreISerializable: true));
        }

        /// <summary>Completely unique value to identify this implementation of Serializer, used to optimize network traffic.</summary>
        public sealed override int Identifier => -5; //104

        /// <inheritdoc />
        public sealed override object DeepCopy(object source) => _serializer.DeepCopyObject(source);

        /// <summary>Serializes the given object into a byte array</summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>A byte array containing the serialized object</returns>
        public sealed override byte[] ToBinary(object obj) => _serializer.SerializeObject(obj, _initialBufferSize);

        /// <summary>Deserializes a byte array into an object of type <paramref name="type"/>.</summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public sealed override object FromBinary(byte[] bytes, Type type) => _serializer.Deserialize(type, bytes);

        private IKnownTypesProvider CreateKnownTypesProvider(ExtendedActorSystem system, Type type)
        {
            var ctors = type.GetConstructors();
            var ctor = ctors.FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return 1 == parameters.Length && (parameters[0].ParameterType == typeof(ActorSystem)
                    || parameters[0].ParameterType == typeof(ExtendedActorSystem));
            });

            return ctor == null
                ? ActivatorUtils.FastCreateInstance<IKnownTypesProvider>(type)
                : (IKnownTypesProvider)ctor.Invoke(new object[] { system });
        }
    }
}