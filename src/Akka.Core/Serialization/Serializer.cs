//-----------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Annotations;
using Akka.Serialization.Protocol;
using Akka.Util;
using CuteAnt.Reflection;

namespace Akka.Serialization
{
    /// <summary>
    /// A Serializer represents a bimap between an object and an array of bytes representing that object.
    ///
    /// Serializers are loaded using reflection during <see cref="ActorSystem"/>
    /// start-up, where two constructors are tried in order:
    ///
    /// <ul>
    /// <li>taking exactly one argument of type <see cref="ExtendedActorSystem"/>;
    /// this should be the preferred one because all reflective loading of classes
    /// during deserialization should use ExtendedActorSystem.dynamicAccess (see
    /// [[akka.actor.DynamicAccess]]), and</li>
    /// <li>without arguments, which is only an option if the serializer does not
    /// load classes using reflection.</li>
    /// </ul>
    ///
    /// <b>Be sure to always use the PropertyManager for loading classes!</b> This is necessary to
    /// avoid strange match errors and inequalities which arise from different class loaders loading
    /// the same class.
    /// </summary>
    public abstract class Serializer
    {
        /// <summary>
        /// The actor system to associate with this serializer.
        /// </summary>
        protected readonly ExtendedActorSystem _system;

        private readonly FastLazy<int> _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Serializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        protected Serializer(ExtendedActorSystem system)
        {
            _system = system;
            _value = new FastLazy<int>(() => SerializerIdentifierHelper.GetSerializerIdentifierFromConfig(GetType(), system));
        }

        /// <summary>
        /// Completely unique value to identify this implementation of Serializer, used to optimize network traffic
        /// Values from 0 to 16 is reserved for Akka internal usage
        /// </summary>
        public virtual int Identifier => _value.Value;

        /// <summary>
        /// Returns whether this serializer needs a manifest in the fromBinary method
        /// </summary>
        public virtual bool IncludeManifest => false;

        /// <summary>TBD</summary>
        public virtual bool IsJson => false;

        /// <summary>Tries to create a copy of source.</summary>
        /// <param name="source">The item to create a copy of</param>
        /// <returns>The copy</returns>
        public virtual object DeepCopy(object source)
        {
            if (null == source) { return null; }
            var objType = source.GetType();
            var bts = ToBinary(source);
            return FromBinary(bts, objType);
        }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A byte array containing the serialized object</returns>
        public abstract byte[] ToBinary(object obj);

        ///// <summary>
        ///// Serializes the given object into a byte array and uses the given address to decorate serialized ActorRef's
        ///// </summary>
        ///// <param name="address">The address to use when serializing local ActorRef´s</param>
        ///// <param name="obj">The object to serialize</param>
        ///// <returns>TBD</returns>
        //public byte[] ToBinaryWithAddress(Address address, object obj)
        //{
        //    return Serialization.WithTransport(system, address, this, obj);
        //}

        /// <summary>
        /// Deserializes a byte array into an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public abstract object FromBinary(byte[] bytes, Type type);

        /// <summary>
        /// Deserializes a byte array into an object.
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <returns>The object contained in the array</returns>
        public T FromBinary<T>(byte[] bytes) => (T)FromBinary(bytes, typeof(T));

        /// <summary>
        /// Serializes the given object into a <see cref="Payload"/>
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A <see cref="Payload"/> containing the serialized object</returns>
        public virtual Payload ToPayload(object obj)
        {
            return new Payload(ToBinary(obj), Identifier);
        }

        ///// <summary>
        ///// Serializes the given object into a <see cref="Payload"/> and uses the given address to decorate serialized ActorRef's
        ///// </summary>
        ///// <param name="address">The address to use when serializing local ActorRef´s</param>
        ///// <param name="obj">The object to serialize</param>
        ///// <returns>A <see cref="Payload"/> containing the serialized object</returns>
        //public virtual Payload ToPayloadWithAddress(Address address, object obj)
        //{
        //    return new Payload(Serialization.WithTransport(system, address, this, obj), Identifier);
        //}

        /// <summary>
        /// Deserializes a <see cref="Payload"/> into an object.
        /// </summary>
        /// <param name="payload">The <see cref="Payload"/> containing the serialized object</param>
        /// <returns>The object contained in the <see cref="Payload"/></returns>
        public virtual object FromPayload(in Payload payload)
        {
            return FromBinary(payload.Message, null);
        }

        /// <summary>
        /// Serializes the given object into a <see cref="ExternalPayload"/>
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A <see cref="ExternalPayload"/> containing the serialized object</returns>
        public virtual ExternalPayload ToExternalPayload(object obj)
        {
            return new ExternalPayload(ToBinary(obj), Identifier, IsJson, obj.GetType());
        }

        /// <summary>
        /// Deserializes a <see cref="ExternalPayload"/> into an object.
        /// </summary>
        /// <param name="payload">The <see cref="Payload"/> containing the serialized object</param>
        /// <returns>The object contained in the <see cref="Payload"/></returns>
        public virtual object FromExternalPayload(in ExternalPayload payload)
        {
            return FromBinary(payload.Message, null);
        }
    }

    #region -- SerializerIdentifierHelper --

    /// <summary>
    /// INTERNAL API.
    /// </summary>
    [InternalApi]
    public static class SerializerIdentifierHelper
    {
        internal const string SerializationIdentifiers = "akka.actor.serialization-identifiers";

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="type">TBD</param>
        /// <param name="system">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if the system couldn't find the given serializer <paramref name="type"/> id in the configuration.
        /// </exception>
        /// <returns>TBD</returns>
        public static int GetSerializerIdentifierFromConfig(Type type, ExtendedActorSystem system)
        {
            var config = system.Settings.Config.GetConfig(SerializationIdentifiers);
            var identifiers = config.AsEnumerable()
                .ToDictionary(pair => TypeUtils.ResolveType(pair.Key), pair => pair.Value.GetInt()); // Type.GetType(pair.Key, true)

            if (!identifiers.TryGetValue(type, out int value))
                throw new ArgumentException($"Couldn't find serializer id for [{type}] under [{SerializationIdentifiers}] HOCON path", nameof(type));

            return value;
        }
    }

    #endregion
}

