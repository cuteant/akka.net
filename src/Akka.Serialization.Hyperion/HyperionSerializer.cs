//-----------------------------------------------------------------------
// <copyright file="HyperionSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
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

        private readonly Hyperion.Serializer _serializer;

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

            _serializer =
                new Hyperion.Serializer(new SerializerOptions(
                    preserveObjectReferences: settings.PreserveObjectReferences,
                    versionTolerance: settings.VersionTolerance,
                    surrogates: new[] { akkaSurrogate },
                    knownTypes: provider.GetKnownTypes(),
                    ignoreISerializable: true));
        }

        /// <summary>Completely unique value to identify this implementation of Serializer, used to optimize network traffic.</summary>
        public sealed override int Identifier => -5; //104

        /// <summary>Serializes the given object into a byte array</summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>A byte array containing the serialized object</returns>
        public sealed override byte[] ToBinary(object obj)
        {
            using (var ms = new MemoryStream())
            {
                _serializer.Serialize(obj, ms);
                return ms.ToArray();
            }
        }

        /// <summary>Deserializes a byte array into an object of type <paramref name="type"/>.</summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public sealed override object FromBinary(byte[] bytes, Type type)
        {
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    var res = _serializer.Deserialize<object>(ms);
                    return res;
                }
            }
            catch (TypeLoadException e)
            {
                throw GetSerializationException(e);
            }
            catch (NotSupportedException e)
            {
                throw GetSerializationException(e);
            }
            catch (ArgumentException e)
            {
                throw GetSerializationException(e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetSerializationException(Exception e)
        {
            return new SerializationException(e.Message, e);
        }

        private IKnownTypesProvider CreateKnownTypesProvider(ExtendedActorSystem system, Type type)
        {
            var ctors = type.GetConstructors();
            var ctor = ctors.FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return 1 == parameters.Length && (parameters[0].ParameterType == typeof(ActorSystem)
                    || parameters[0].ParameterType == typeof(ExtendedActorSystem));
            });

            return ctor is null
                ? ActivatorUtils.FastCreateInstance<IKnownTypesProvider>(type)
                : (IKnownTypesProvider)ctor.Invoke(new object[] { system });
        }
    }
}