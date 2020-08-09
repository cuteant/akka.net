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
using Akka.Annotations;
using Akka.Configuration;
using Akka.Util;
using Akka.Util.Internal;
using Akka.Serialization.Protocol;
using CuteAnt;
using CuteAnt.Collections;
using CuteAnt.Reflection;

namespace Akka.Serialization
{
    /// <summary>
    /// INTERNAL API.
    /// 
    /// Serialization information needed for serializing local actor refs.
    /// </summary>
    [InternalApi]
    public sealed class Information : IEquatable<Information>
    {
        public Information(Address address, ActorSystem system)
        {
            Address = address;
            System = system;
        }

        public Address Address { get; }

        public ActorSystem System { get; }

        public bool Equals(Information other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Information)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Address.GetHashCode() * 397);
            }
        }

        public static bool operator ==(Information left, Information right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Information left, Information right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>TBD</summary>
    public sealed class Serialization
    {
        /// <summary>
        /// Used to determine the manifest for a message, if applicable.
        /// </summary>
        /// <param name="s">The serializer we want to use on the message.</param>
        /// <param name="msg">The message payload.</param>
        /// <returns>A populated string is applicable; <see cref="string.Empty"/> otherwise.</returns>
        /// <remarks>
        /// WARNING: if you change this method it's likely that the DaemonMsgCreateSerializer and other calls will need changes too.
        /// </remarks>
        public static string ManifestFor(Serializer s, object msg)
        {
            switch (s)
            {
                case SerializerWithStringManifest s2:
                    return s2.Manifest(msg);
                case Serializer s3 when s3.IncludeManifest:
                    return RuntimeTypeNameFormatter.Serialize(msg.GetType());
                default:
                    return string.Empty;
            }
        }

        #region -- SerializeWithTransport --

        /// <summary>
        /// Needs to be INTERNAL so it can be accessed from tests. Should never be set directly.
        /// </summary>
        [ThreadStatic]
        internal static Information CurrentTransportInformation;

        /// <summary>
        ///  Retrieves the <see cref="Information"/> used for serializing and deserializing
        /// <see cref="IActorRef"/> instances in all serializers.
        /// </summary>
        public static Information GetCurrentTransportInformation()
        {
            var currentTransportInformation = CurrentTransportInformation;
            if (currentTransportInformation is null)
            {
                AkkaThrowHelper.ThrowInvalidOperationException_CurrentTransportInformation_is_not_set();
            }
            return currentTransportInformation;
        }

        /// <summary>TBD</summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="system">TBD</param>
        /// <param name="address">TBD</param>
        /// <param name="func">TBD</param>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        [Obsolete("Obsolete. Use the WithTransport<T>(ExtendedActorSystem) method instead.")]
        public static T WithTransport<T>(ActorSystem system, Address address, Func<object, T> func, object obj)
        {
            CurrentTransportInformation = new Information(address, system);

            var res = func(obj);

            CurrentTransportInformation = null;
            return res;
        }

        /// <summary>TBD</summary>
        /// <param name="system"></param>
        /// <param name="address"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        [Obsolete("Obsolete. Use the WithTransport<T>(ExtendedActorSystem) method instead.")]
        public static byte[] WithTransport(ActorSystem system, Address address, Serializer serializer, object obj)
        {
            CurrentTransportInformation = new Information(address, system);

            var res = serializer.ToBinary(obj);

            CurrentTransportInformation = null;
            return res;
        }

        /// <summary>TBD</summary>
        /// <param name="system"></param>
        /// <param name="address"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        [Obsolete("Obsolete. Use the WithTransport<T>(ExtendedActorSystem) method instead.")]
        public static byte[] WithTransport(ActorSystem system, Address address, SerializerWithStringManifest serializer, object obj, out string manifest)
        {
            CurrentTransportInformation = new Information(address, system);

            var res = serializer.ToBinary(obj, out manifest);

            CurrentTransportInformation = null;
            return res;
        }

        #endregion

        private readonly Serializer _nullSerializer;

        private readonly CachedReadConcurrentDictionary<Type, Serializer> _serializerMap = new CachedReadConcurrentDictionary<Type, Serializer>();
        private readonly Dictionary<int, Serializer> _serializersById = new Dictionary<int, Serializer>();
        private readonly Dictionary<string, Serializer> _serializersByName = new Dictionary<string, Serializer>(StringComparer.Ordinal);

        /// <summary>
        /// Serialization module. Contains methods for serialization and deserialization as well as
        /// locating a Serializer for a particular class as defined in the mapping in the configuration.
        /// </summary>
        /// <param name="system">The ActorSystem to which this serializer belongs.</param>
        public Serialization(ExtendedActorSystem system)
        {
            System = system;

            _nullSerializer = new NullSerializer(system);
            AddSerializer("null", _nullSerializer);

            var config = system.Settings.Config;
            var effective = config.GetBoolean("akka.actor.serialization", true);
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

                var serializer = !serializerConfig.IsNullOrEmpty()
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

        private Information SerializationInfo => System.Provider.SerializationInformation;

        /// <summary>
        /// Performs the requested serialization function while also setting
        /// the <see cref="CurrentTransportInformation"/> based on available data
        /// from the <see cref="ActorSystem"/>. Useful when serializing <see cref="IActorRef"/>s.
        /// </summary>
        /// <typeparam name="T">The type of message being serialized.</typeparam>
        /// <param name="system">The <see cref="ActorSystem"/> performing serialization.</param>
        /// <param name="action">The serialization function.</param>
        /// <returns>The serialization output.</returns>
        public static T WithTransport<T>(ExtendedActorSystem system, Func<T> action)
        {
            var info = system.Provider.SerializationInformation;
            if (CurrentTransportInformation == info)
            {
                // already set
                return action();
            }

            var oldInfo = CurrentTransportInformation;
            try
            {
                CurrentTransportInformation = info;
                return action();
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>TBD</summary>
        /// <param name="system"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] WithTransport(ExtendedActorSystem system, Serializer serializer, object obj)
        {
            var info = system.Provider.SerializationInformation;
            if (CurrentTransportInformation == info)
            {
                // already set
                return serializer.ToBinary(obj);
            }

            var oldInfo = CurrentTransportInformation;
            try
            {
                CurrentTransportInformation = info;
                return serializer.ToBinary(obj);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        ///// <summary>TBD</summary>
        ///// <param name="system"></param>
        ///// <param name="serializer"></param>
        ///// <param name="obj"></param>
        ///// <param name="manifest"></param>
        ///// <returns></returns>
        //public static byte[] WithTransport(ExtendedActorSystem system, SerializerWithStringManifest serializer, object obj, out string manifest)
        //{
        //    var info = system.Provider.SerializationInformation;
        //    if (CurrentTransportInformation == info)
        //    {
        //        // already set
        //        return serializer.ToBinary(obj, out manifest);
        //    }

        //    var oldInfo = CurrentTransportInformation;
        //    try
        //    {
        //        CurrentTransportInformation = info;
        //        return serializer.ToBinary(obj, out manifest);
        //    }
        //    finally
        //    {
        //        CurrentTransportInformation = oldInfo;
        //    }
        //}

        //private T WithTransport<T>(Func<T> action)
        //{
        //    var oldInfo = CurrentTransportInformation;
        //    try
        //    {
        //        if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
        //        return action();
        //    }
        //    finally
        //    {
        //        CurrentTransportInformation = oldInfo;
        //    }
        //}

        private Serializer GetSerializerByName(string name)
        {
            if (name == null) { return null; }

            _serializersByName.TryGetValue(name, out Serializer serializer);
            return serializer;
        }

        /// <summary>
        /// The ActorSystem to which <see cref="Serialization"/> is bound.
        /// </summary>
        public ExtendedActorSystem System { get; }

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

        ///// <summary>
        ///// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        ///// </summary>
        ///// <param name="o">The message being serialized.</param>
        ///// <returns>A byte array containing the serialized message.</returns>
        //[MethodImpl(InlineOptions.AggressiveOptimization)]
        //public byte[] Serialize(object o)
        //{
        //    return FindSerializerFor(o).ToBinary(o);
        //}

        ///// <summary>
        ///// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        ///// </summary>
        ///// <param name="o">The message being serialized.</param>
        ///// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        ///// <returns>A byte array containing the serialized message.</returns>
        //[MethodImpl(InlineOptions.AggressiveOptimization)]
        //public byte[] Serialize(object o, string defaultSerializerName)
        //{
        //    return FindSerializerFor(o, defaultSerializerName).ToBinary(o);
        //}

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        /// <returns>A byte array containing the serialized message.</returns>
        public byte[] SerializeWithTransport(object o)
        {
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                return FindSerializerFor(o).ToBinary(o);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>A byte array containing the serialized message.</returns>
        public byte[] SerializeWithTransport(object o, string defaultSerializerName)
        {
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                return FindSerializerFor(o, defaultSerializerName).ToBinary(o);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        public Payload SerializeMessageWithTransport(object o)
        {
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                return FindSerializerFor(o).ToPayload(o);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        public Payload SerializeMessageWithTransport(object o, string defaultSerializerName)
        {
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                return FindSerializerFor(o, defaultSerializerName).ToPayload(o);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        public ExternalPayload SerializeExternalMessageWithTransport(object o)
        {
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                return FindSerializerFor(o).ToExternalPayload(o);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        public ExternalPayload SerializeExternalMessageWithTransport(object o, string defaultSerializerName)
        {
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                return FindSerializerFor(o, defaultSerializerName).ToExternalPayload(o);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
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
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                if (!_serializersById.TryGetValue(serializerId, out var serializer))
                {
                    ThrowSerializationException(serializerId, type);
                }
                return serializer.FromBinary(bytes, type);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
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
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                if (!_serializersById.TryGetValue(serializerId, out var serializer))
                {
                    ThrowSerializationException(serializerId);
                }
                return serializer.FromBinary(bytes, null);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
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
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                if (!_serializersById.TryGetValue(payload.SerializerId, out var serializer))
                {
                    ThrowSerializationException(payload.SerializerId);
                }
                return serializer.FromPayload(payload);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
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
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }
                if (!_serializersById.TryGetValue(payload.Identifier, out var serializer))
                {
                    ThrowSerializationException(payload.Identifier);
                }
                return serializer.FromExternalPayload(payload);
            }
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
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
            var oldInfo = CurrentTransportInformation;
            try
            {
                if (oldInfo is null) { CurrentTransportInformation = SerializationInfo; }

                if (!_serializersById.TryGetValue(serializerId, out var serializer))
                {
                    ThrowSerializationException(serializerId, manifest);
                }

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
            finally
            {
                CurrentTransportInformation = oldInfo;
            }
        }

        /// <summary>Returns the Serializer configured for the given object, returns the NullSerializer if it's null.</summary>
        /// <param name="obj">The object that needs to be serialized</param>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerFor(object obj)
        {
            return obj is object ? FindSerializerForType(obj.GetType()) : _nullSerializer;
        }

        /// <summary>Returns the Serializer configured for the given object, returns the NullSerializer if it's null.</summary>
        /// <param name="obj">The object that needs to be serialized</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present.</param>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerFor(object obj, string defaultSerializerName)
        {
            return obj is object ? FindSerializerForType(obj.GetType(), defaultSerializerName) : _nullSerializer;
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

            if (serializer is null)
            {
                serializer = serialization.GetSerializerByName(defaultSerializerName);
            }

            // do a final check for the "object" serializer
            if (serializer is null)
            {
                serializerMap.TryGetValue(s_objectType, out serializer);
            }

            if (serializer is null)
            {
                ThrowSerializationException(objectType.Name);
            }

            serialization.AddSerializationMap(objectType, serializer);
            return serializer;
        }

        /// <summary>
        /// The serialized path of an actorRef, based on the current transport serialization information.
        /// If there is no external address available for the requested address then the systems default
        /// address will be used.
        ///
        /// If there is no external address available in the given <see cref="IActorRef"/> then the systems default
        /// address will be used and that is retrieved from the ThreadLocal <see cref="Information"/>
        /// that was set with <see cref="Serialization.WithTransport{T}(ExtendedActorSystem, Func{T})"/>
        /// </summary>
        /// <param name="actorRef">The <see cref="IActorRef"/> to be serialized.</param>
        /// <returns>Absolute path to the serialized actor.</returns>
        public static string SerializedActorPath(IActorRef actorRef)
        {
            if (Equals(actorRef, ActorRefs.NoSender)) { return string.Empty; }

            var path = actorRef.Path;
            ExtendedActorSystem originalSystem = null;
            if (actorRef is ActorRefWithCell actorRefWithCell)
            {
                originalSystem = actorRefWithCell.Underlying.System.AsInstanceOf<ExtendedActorSystem>();
            }

            if (CurrentTransportInformation is null)
            {
                if (originalSystem is null)
                {
                    var res = path.ToSerializationFormat();
                    return res;
                }

                try
                {
                    var defaultAddress = originalSystem.Provider.DefaultAddress;
                    var res = path.ToSerializationFormatWithAddress(defaultAddress);
                    return res;
                }
                catch
                {
                    return path.ToSerializationFormat();
                }
            }

            //CurrentTransportInformation exists
            var system = CurrentTransportInformation.System;
            var address = CurrentTransportInformation.Address;
            if (originalSystem is null || originalSystem == system)
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
        private static void ThrowSerializationException(int serializerId, Type type)
        {
            throw GetSerializationException();
            SerializationException GetSerializationException()
            {
                return new SerializationException(
                    $"Cannot find serializer with id [{serializerId}] (class [{type?.Name}]). The most probable reason" +
                    " is that the configuration entry 'akka.actor.serializers' is not in sync between the two systems.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSerializationException(int serializerId, string manifest)
        {
            throw GetSerializationException();
            SerializationException GetSerializationException()
            {
                return new SerializationException(
                    $"Cannot find serializer with id [{serializerId}] (manifest [{manifest}]). The most probable reason" +
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