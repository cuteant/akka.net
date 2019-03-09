//-----------------------------------------------------------------------
// <copyright file="Serialization.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Util;
using Akka.Util.Internal;
using Akka.Serialization.Protocol;
using CuteAnt;
using CuteAnt.Collections;
using CuteAnt.Reflection;

namespace Akka.Serialization
{
    /// <summary>Serialization information needed for serializing local actor refs.</summary>
    internal class Information
    {
        public Address Address { get; set; }

        public ActorSystem System { get; set; }
    }

    /// <summary>TBD</summary>
    public sealed class Serialization
    {
        #region -- SerializeWithTransport --

        [ThreadStatic]
        private static Information _currentTransportInformation;
        private static Information TransportInformationCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentTransportInformation ?? EnsureTransportInformationCreated();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Information EnsureTransportInformationCreated()
        {
            _currentTransportInformation = new Information();
            return _currentTransportInformation;
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="system">TBD</param>
        /// <param name="address">TBD</param>
        /// <param name="func">TBD</param>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public static T SerializeWithTransport<T>(ActorSystem system, Address address, Func<object, T> func, object obj)
        {
            var currentTransportInformation = TransportInformationCache;
            currentTransportInformation.System = system;
            currentTransportInformation.Address = address;

            var res = func(obj);

            currentTransportInformation.System = null;
            currentTransportInformation.Address = null;
            return res;
        }

        /// <summary>TBD</summary>
        /// <param name="system"></param>
        /// <param name="address"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] SerializeWithTransport(ActorSystem system, Address address, Serializer serializer, object obj)
        {
            var currentTransportInformation = TransportInformationCache;
            currentTransportInformation.System = system;
            currentTransportInformation.Address = address;

            var res = serializer.ToBinary(obj);

            currentTransportInformation.System = null;
            currentTransportInformation.Address = null;
            return res;
        }

        /// <summary>TBD</summary>
        /// <param name="system"></param>
        /// <param name="address"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static byte[] SerializeWithTransport(ActorSystem system, Address address, SerializerWithStringManifest serializer, object obj, out byte[] manifest)
        {
            var currentTransportInformation = TransportInformationCache;
            currentTransportInformation.System = system;
            currentTransportInformation.Address = address;

            var res = serializer.ToBinary(obj, out manifest);

            currentTransportInformation.System = null;
            currentTransportInformation.Address = null;
            return res;
        }

        /// <summary>TBD</summary>
        /// <param name="system"></param>
        /// <param name="address"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static byte[] SerializeWithTransport(ActorSystem system, Address address, SerializerWithIntegerManifest serializer, object obj, out int manifest)
        {
            var currentTransportInformation = TransportInformationCache;
            currentTransportInformation.System = system;
            currentTransportInformation.Address = address;

            var res = serializer.ToBinary(obj, out manifest);

            currentTransportInformation.System = null;
            currentTransportInformation.Address = null;
            return res;
        }

        #endregion

        private readonly Serializer _nullSerializer;

        private readonly CachedReadConcurrentDictionary<Type, Serializer> _serializerMap = new CachedReadConcurrentDictionary<Type, Serializer>();
        private readonly Dictionary<int, Serializer> _serializersById = new Dictionary<int, Serializer>();
        private readonly Dictionary<string, Serializer> _serializersByName = new Dictionary<string, Serializer>(StringComparer.Ordinal);

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        public Serialization(ExtendedActorSystem system)
        {
            System = system;

            _nullSerializer = new NullSerializer(system);
            AddSerializer("null", _nullSerializer);

            var config = system.Settings.Config;
            var effective = config.GetBoolean("akka.actor.serialization");
            if (!effective) { return; }

            var initializerType = config.GetString("akka.actor.serialization-initializer", "Akka.Serialization.SerializationInitializer, Akka");
            var serializationInitializer = ActivatorUtils.FastCreateInstance<ISerializationInitializer>(TypeUtil.ResolveType(initializerType));
            serializationInitializer.InitActorSystem(system);

            var serializersConfig = config.GetConfig("akka.actor.serializers").AsEnumerable().ToList();
            var serializerBindingConfig = config.GetConfig("akka.actor.serialization-bindings").AsEnumerable().ToList();
            var serializerSettingsConfig = config.GetConfig("akka.actor.serialization-settings");

            var systemLog = system.Log;
            var warnEnabled = systemLog.IsWarningEnabled;
            foreach (var kvp in serializersConfig)
            {
                var serializerTypeName = kvp.Value.GetString();
                var serializerType = TypeUtil.ResolveType(serializerTypeName);
                if (serializerType == null)
                {
                    if (warnEnabled) systemLog.TheTypeNameForSerializerDidNotResolveToAnActualType(kvp.Key, serializerTypeName);
                    continue;
                }

                var serializerConfig = serializerSettingsConfig.GetConfig(kvp.Key);

                var serializer = serializerConfig != null
                    ? (Serializer)Activator.CreateInstance(serializerType, system, serializerConfig)
                    : (Serializer)Activator.CreateInstance(serializerType, system);

                AddSerializer(kvp.Key, serializer);
            }

            foreach (var kvp in serializerBindingConfig)
            {
                var typename = kvp.Key;
                var serializerName = kvp.Value.GetString();
                var messageType = TypeUtil.ResolveType(typename);

                if (messageType == null)
                {
                    if (warnEnabled) systemLog.TheTypeNameForMessageAndSerializerBindingDidNotResolveToAnActualType(serializerName, typename);
                    continue;
                }

                if (!_serializersByName.TryGetValue(serializerName, out var serializer))
                {
                    if (warnEnabled) systemLog.SerializationBindingToNonExistingSerializer(serializerName);
                    continue;
                }

                AddSerializationMap(messageType, serializer);
            }
        }

        private Serializer GetSerializerByName(string name)
        {
            if (name == null) { return null; }

            _serializersByName.TryGetValue(name, out Serializer serializer);
            return serializer;
        }

        /// <summary>TBD</summary>
        public ActorSystem System { get; }

        /// <summary>Adds the serializer to the internal state of the serialization subsystem</summary>
        /// <param name="serializer">Serializer instance</param>
        [Obsolete("No longer supported. Use the AddSerializer(name, serializer) overload instead.", true)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSerializer(Serializer serializer)
        {
            _serializersById.Add(serializer.Identifier, serializer);
        }

        /// <summary>Adds the serializer to the internal state of the serialization subsystem</summary>
        /// <param name="name">Configuration name of the serializer</param>
        /// <param name="serializer">Serializer instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSerializer(string name, Serializer serializer)
        {
            _serializersById.Add(serializer.Identifier, serializer);
            _serializersByName.Add(name, serializer);
        }

        /// <summary>TBD</summary>
        /// <param name="type">TBD</param>
        /// <param name="serializer">TBD</param>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSerializationMap(Type type, Serializer serializer)
        {
            _serializerMap[type] = serializer;
        }

        /// <summary>Deserializes the given array of bytes using the specified serializer id, using the
        /// optional type hint to the Serializer.</summary>
        /// <param name="bytes">TBD</param>
        /// <param name="serializerId">TBD</param>
        /// <param name="type">TBD</param>
        /// <exception cref="SerializationException">This exception is thrown if the system cannot find 
        /// the serializer with the given <paramref name="serializerId"/>.</exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(byte[] bytes, int serializerId, Type type)
        {
            if (_serializersById.TryGetValue(serializerId, out var serializer))
            {
                return serializer.FromBinary(bytes, type);
            }

            ThrowSerializationException(serializerId); return null;
        }

        /// <summary>Deserializes the given array of bytes using the specified serializer id, using the
        /// optional type hint to the Serializer.</summary>
        /// <param name="bytes">TBD</param>
        /// <param name="serializerId">TBD</param>
        /// <exception cref="SerializationException">This exception is thrown if the system cannot find 
        /// the serializer with the given <paramref name="serializerId"/>.</exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(byte[] bytes, int serializerId)
        {
            if (_serializersById.TryGetValue(serializerId, out var serializer))
            {
                return serializer.FromBinary(bytes, null);
            }

            ThrowSerializationException(serializerId); return null;
        }

        /// <summary>Deserializes the given array of bytes using the specified serializer id, using the
        /// optional type hint to the Serializer.</summary>
        /// <param name="payload">TBD</param>
        /// <exception cref="SerializationException">
        /// This exception is thrown if the system cannot find the serializer with the given
        /// <paramref name="payload"/>.</exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(in Payload payload)
        {
            if (_serializersById.TryGetValue(payload.SerializerId, out var serializer))
            {
                return serializer.FromPayload(payload);
            }

            ThrowSerializationException(payload.SerializerId); return null;
        }

        /// <summary>Deserializes the given array of bytes using the specified serializer id, using the
        /// optional type hint to the Serializer.</summary>
        /// <param name="payload">TBD</param>
        /// <exception cref="SerializationException">
        /// This exception is thrown if the system cannot find the serializer with the given
        /// <paramref name="payload"/>.</exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(in ExternalPayload payload)
        {
            if (_serializersById.TryGetValue(payload.Identifier, out var serializer))
            {
                return serializer.FromExternalPayload(payload);
            }

            ThrowSerializationException(payload.Identifier); return null;
        }

        /// <summary>Deserializes the given array of bytes using the specified serializer id, using the
        /// optional type hint to the Serializer.</summary>
        /// <param name="bytes">TBD</param>
        /// <param name="serializerId">TBD</param>
        /// <param name="manifest">TBD</param>
        /// <exception cref="SerializationException">
        /// This exception is thrown if the system cannot find the serializer with the given
        /// <paramref name="serializerId"/> or it couldn't find the given <paramref name="manifest"/>
        /// with the given <paramref name="serializerId"/>.</exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(byte[] bytes, int serializerId, string manifest)
        {
            if (_serializersById.TryGetValue(serializerId, out var serializer))
            {
                if (serializer is SerializerWithStringManifest manifestSerializer)
                {
                    return manifestSerializer.FromBinary(bytes, manifest);
                }

                if (string.IsNullOrEmpty(manifest))
                {
                    return serializer.FromBinary(bytes, null);
                }

                Type type = null;
                try
                {
                    type = TypeUtils.ResolveType(manifest);
                }
                catch (Exception ex)
                {
                    ThrowSerializationException_D(manifest, serializerId, ex);
                }

                return serializer.FromBinary(bytes, type);
            }

            ThrowSerializationException(serializerId); return null;
        }

        /// <summary>Returns the Serializer configured for the given object, returns the NullSerializer if it's null.</summary>
        /// <param name="obj">The object that needs to be serialized</param>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerFor(object obj)
        {
            return obj == null ? _nullSerializer : FindSerializerForType(obj.GetType());
        }

        /// <summary>Returns the Serializer configured for the given object, returns the NullSerializer if it's null.</summary>
        /// <param name="obj">The object that needs to be serialized</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerFor(object obj, string defaultSerializerName)
        {
            return obj == null ? _nullSerializer : FindSerializerForType(obj.GetType(), defaultSerializerName);
        }

        /// <summary>Returns the configured Serializer for the given Class. The configured Serializer is used
        /// if the configured class `IsAssignableFrom` from the <see cref="Type">type</see>, i.e. the
        /// configured class is a super class or implemented interface. In case of ambiguity it is
        /// primarily using the most specific configured class, and secondly the entry configured first.</summary>
        /// <param name="objectType">TBD</param>
        /// <exception cref="SerializationException">This exception is thrown if the serializer of the given <paramref name="objectType"/>
        /// could not be found.</exception>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerForType(Type objectType)
        {
            if (_serializerMap.TryGetValue(objectType, out var fullMatchSerializer))
            {
                return fullMatchSerializer;
            }

            return InternalFindSerializerForType(this, objectType, null);
        }

        /// <summary>Returns the configured Serializer for the given Class. The configured Serializer is used
        /// if the configured class `IsAssignableFrom` from the <see cref="Type">type</see>, i.e. the
        /// configured class is a super class or implemented interface. In case of ambiguity it is
        /// primarily using the most specific configured class, and secondly the entry configured first.</summary>
        /// <param name="objectType">TBD</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <exception cref="SerializationException">This exception is thrown if the serializer of the given <paramref name="objectType"/>
        /// could not be found.</exception>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerForType(Type objectType, string defaultSerializerName)
        {
            if (_serializerMap.TryGetValue(objectType, out var fullMatchSerializer))
            {
                return fullMatchSerializer;
            }

            return InternalFindSerializerForType(this, objectType, defaultSerializerName);
        }

        //cache to eliminate lots of typeof operator calls
        private static readonly Type s_objectType = TypeConstants.ObjectType;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Serializer InternalFindSerializerForType(Serialization serialization, Type objectType, string defaultSerializerName)
        {
            Serializer serializer = null;
            var serializerMap = serialization._serializerMap;

            // TODO: see if we can do a better job with proper type sorting here - most specific to
            //       least specific (object serializer goes last)
            foreach (var serializerType in serializerMap)
            {
                // force deferral of the base "object" serializer until all other higher-level types
                // have been evaluated
                if (serializerType.Key.IsAssignableFrom(objectType) && serializerType.Key != s_objectType)
                {
                    serializer = serializerType.Value;
                    break;
                }
            }

            if (serializer == null)
            {
                serializer = serialization.GetSerializerByName(defaultSerializerName);
            }

            // do a final check for the "object" serializer
            if (serializer == null)
            {
                serializerMap.TryGetValue(s_objectType, out serializer);
            }

            if (serializer == null)
            {
                ThrowSerializationException(objectType.Name);
            }

            serialization.AddSerializationMap(objectType, serializer);
            return serializer;
        }

        /// <summary>TBD</summary>
        /// <param name="actorRef">TBD</param>
        /// <returns>TBD</returns>
        public static string SerializedActorPath(IActorRef actorRef)
        {
            if (Equals(actorRef, ActorRefs.NoSender)) { return String.Empty; }

            var path = actorRef.Path;
            ExtendedActorSystem originalSystem = null;
            if (actorRef is ActorRefWithCell actorRefWithCell)
            {
                originalSystem = actorRefWithCell.Underlying.System.AsInstanceOf<ExtendedActorSystem>();
            }

            if (_currentTransportInformation == null)
            {
                if (originalSystem == null)
                {
                    var res = path.ToSerializationFormat();
                    return res;
                }
                else
                {
                    var defaultAddress = originalSystem.Provider.DefaultAddress;
                    var res = path.ToSerializationFormatWithAddress(defaultAddress);
                    return res;
                }
            }

            //CurrentTransportInformation exists
            var system = _currentTransportInformation.System;
            var address = _currentTransportInformation.Address;
            if (originalSystem == null || originalSystem == system)
            {
                var res = path.ToSerializationFormatWithAddress(address);
                return res;
            }
            else
            {
                var provider = originalSystem.Provider;
                var res =
                    path.ToSerializationFormatWithAddress(provider.GetExternalAddressFor(address) ?? provider.DefaultAddress);
                return res;
            }
        }

        internal Serializer GetSerializerById(int serializerId) => _serializersById[serializerId];

        #region ** ThrowHelper **

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSerializationException(int serializerId)
        {
            throw GetSerializationException();
            SerializationException GetSerializationException()
            {
                return new SerializationException(
                    $"Cannot find serializer with id [{serializerId}]. The most probable reason" +
                    " is that the configuration entry 'akka.actor.serializers' is not in sync between the two systems.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSerializationException(string typeName)
        {
            throw GetSerializationException();
            SerializationException GetSerializationException()
            {
                return new SerializationException($"Serializer not found for type {typeName}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSerializationException_D(string manifest, int serializerId, Exception ex)
        {
            throw GetSerializationException();
            SerializationException GetSerializationException()
            {
                return new SerializationException($"Cannot find manifest class [{manifest}] for serializer with id [{serializerId}].", ex);
            }
        }

        #endregion
    }
}