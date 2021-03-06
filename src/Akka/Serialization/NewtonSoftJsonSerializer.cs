﻿//-----------------------------------------------------------------------
// <copyright file="NewtonSoftJsonSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using CuteAnt;
using CuteAnt.Collections;
using CuteAnt.Pool;
using CuteAnt.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SpanJson.Serialization;

namespace Akka.Serialization
{
    /// <summary>A typed settings for a <see cref="NewtonSoftJsonSerializer"/> class.</summary>
    public sealed class NewtonSoftJsonSerializerSettings
    {
#if DESKTOPCLR
        private const int c_initialBufferSize = 1024 * 80;
#else
        private const int c_initialBufferSize = 1024 * 64;
#endif
        private const int c_maxBufferSize = 1024 * 1024;

        /// <summary>
        /// A default instance of <see cref="NewtonSoftJsonSerializerSettings"/> used when no custom configuration has been provided.
        /// </summary>
        public static readonly NewtonSoftJsonSerializerSettings Default = new NewtonSoftJsonSerializerSettings(
            initialBufferSize: c_initialBufferSize,
            encodeTypeNames: true,
            preserveObjectReferences: true,
            converters: Enumerable.Empty<Type>());

        /// <summary>
        /// Creates a new instance of the <see cref="NewtonSoftJsonSerializerSettings"/> based on a provided <paramref name="config"/>.
        /// Config may define several key-values:
        /// <ul>
        /// <li>`encode-type-names` (boolean) mapped to <see cref="EncodeTypeNames"/></li>
        /// <li>`preserve-object-references` (boolean) mapped to <see cref="PreserveObjectReferences"/></li>
        /// <li>`converters` (type list) mapped to <see cref="Converters"/>. They must implement <see cref="JsonConverter"/> and define either default constructor or constructor taking <see cref="ExtendedActorSystem"/> as its only parameter.</li>
        /// </ul>
        /// </summary>
        /// <exception cref="ArgumentNullException">Raised when no <paramref name="config"/> was provided.</exception>
        /// <exception cref="ArgumentException">Raised when types defined in `converters` list didn't inherit <see cref="JsonConverter"/>.</exception>
        public static NewtonSoftJsonSerializerSettings Create(Config config)
        {
            if (config.IsNullOrEmpty())
            {
                throw ConfigurationException.NullOrEmptyConfig<NewtonSoftJsonSerializerSettings>();
            }

            return new NewtonSoftJsonSerializerSettings(
                initialBufferSize: (int)config.GetByteSize("initial-buffer-size", c_initialBufferSize),
                encodeTypeNames: config.GetBoolean("encode-type-names", true),
                preserveObjectReferences: config.GetBoolean("preserve-object-references", true),
                converters: GetConverterTypes(config));
        }

        private static IEnumerable<Type> GetConverterTypes(Config config)
        {
            var converterNames = config.GetStringList("converters", EmptyArray<string>.Instance);

            if (converterNames is object)
                foreach (var converterName in converterNames)
                {
                    var type = TypeUtils.ResolveType(converterName);//, true);
                    if (!typeof(JsonConverter).IsAssignableFrom(type))
                        throw new ArgumentException($"Type {type} doesn't inherit from a {typeof(JsonConverter)}.");

                    yield return type;
                }
        }

        /// <summary>The initial buffer size.</summary>
        public readonly int InitialBufferSize;

        /// <summary>
        /// When true, serializer will encode a type names into serialized json $type field. This must be true 
        /// if <see cref="NewtonSoftJsonSerializer"/> is a default serializer in order to support polymorphic 
        /// deserialization.
        /// </summary>
        public bool EncodeTypeNames { get; }

        /// <summary>
        /// When true, serializer will track a reference dependencies in serialized object graph. This must be 
        /// true if <see cref="NewtonSoftJsonSerializer"/>.
        /// </summary>
        public bool PreserveObjectReferences { get; }

        /// <summary>
        /// A collection of an additional converter types to be applied to a <see cref="NewtonSoftJsonSerializer"/>.
        /// Converters must inherit from <see cref="JsonConverter"/> class and implement a default constructor.
        /// </summary>
        public IEnumerable<Type> Converters { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="NewtonSoftJsonSerializerSettings"/>.
        /// </summary>
        /// <param name="initialBufferSize">The initial buffer size.</param>
        /// <param name="encodeTypeNames">Determines if a special `$type` field should be emitted into serialized JSON. Must be true if corresponding serializer is used as default.</param>
        /// <param name="preserveObjectReferences">Determines if object references should be tracked within serialized object graph. Must be true if corresponding serialize is used as default.</param>
        /// <param name="converters">A list of types implementing a <see cref="JsonConverter"/> to support custom types serialization.</param>
        public NewtonSoftJsonSerializerSettings(int initialBufferSize, bool encodeTypeNames, bool preserveObjectReferences, IEnumerable<Type> converters)
        {
            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > c_maxBufferSize) { initialBufferSize = c_maxBufferSize; }
            InitialBufferSize = initialBufferSize;

            EncodeTypeNames = encodeTypeNames;
            PreserveObjectReferences = preserveObjectReferences;
            Converters = converters ?? throw new ArgumentNullException(nameof(converters), $"{nameof(NewtonSoftJsonSerializerSettings)} requires a sequence of converters.");
        }
    }

    /// <summary>This is a special <see cref="Serializer"/> that serializes and deserializes javascript objects only.
    /// These objects need to be in the JavaScript Object Notation (JSON) format.</summary>
    public sealed class NewtonSoftJsonSerializer : Serializer
    {
        private readonly JsonSerializer _serializer;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly ObjectPool<JsonSerializer> _serializerPool;
        private readonly int _initialBufferSize;

        /// <summary>TBD</summary>
        public JsonSerializerSettings Settings => _serializerSettings;

        /// <summary>TBD</summary>
        public object Serializer => _serializer;

        /// <inheritdoc />
        public override bool IsJson => true;

        /// <summary>Initializes a new instance of the <see cref="NewtonSoftJsonSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public NewtonSoftJsonSerializer(ExtendedActorSystem system)
            : this(system, NewtonSoftJsonSerializerSettings.Default)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="NewtonSoftJsonSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        /// <param name="config"></param>
        public NewtonSoftJsonSerializer(ExtendedActorSystem system, Config config)
            : this(system, NewtonSoftJsonSerializerSettings.Create(config))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="NewtonSoftJsonSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        /// <param name="settings"></param>
        public NewtonSoftJsonSerializer(ExtendedActorSystem system, NewtonSoftJsonSerializerSettings settings)
            : base(system)
        {
            var converters = settings.Converters
                .Select(type => CreateConverter(type, system))
                .ToList();

            converters.Add(new SurrogateConverter(this));
            converters.Add(new DiscriminatedUnionConverter());
            converters.Add(CombGuidConverter.Instance);

            _initialBufferSize = settings.InitialBufferSize;
            _serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,

                FloatParseHandling = FloatParseHandling.Double,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,

                ObjectCreationHandling = ObjectCreationHandling.Replace, //important: if reuse, the serializer will overwrite properties in default references, e.g. Props.DefaultDeploy or Props.noArgs
                PreserveReferencesHandling = settings.PreserveObjectReferences
                    ? PreserveReferencesHandling.Objects
                    : PreserveReferencesHandling.None,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                TypeNameHandling = settings.EncodeTypeNames
                    ? TypeNameHandling.All
                    : TypeNameHandling.None,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ContractResolver = new AkkaContractResolver(),
                Converters = converters,
                SerializationBinder = JsonSerializationBinder.Instance,
            };

            _serializerPool = JsonConvertX.GetJsonSerializerPool(_serializerSettings);

            _serializer = JsonSerializer.Create(_serializerSettings);
        }

        private static JsonConverter CreateConverter(Type converterType, ExtendedActorSystem actorSystem)
        {
            var ctor = converterType.GetConstructors()
                .FirstOrDefault(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(ExtendedActorSystem);
                });

            return ctor is null
                ? ActivatorUtils.FastCreateInstance<JsonConverter>(converterType)
                : (JsonConverter)Activator.CreateInstance(converterType, actorSystem);
        }

        internal class AkkaContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                {
                    if (member is PropertyInfo property)
                    {
                        var hasPrivateSetter = property.GetSetMethod(true) is object;
                        prop.Writable = hasPrivateSetter;
                    }
                }

                return prop;
            }
        }

        /// <inheritdoc />
        public override int Identifier => 105;

        /// <summary>Serializes the given object into a byte array.</summary>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj)
        {
            return _serializerPool.SerializeToByteArray(obj, null, _initialBufferSize);
        }

        /// <summary>Deserializes a byte array into an object of type <paramref name="type"/>.</summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, Type type)
        {
            object res = _serializerPool.DeserializeFromByteArray(bytes, type);
            return TranslateSurrogate(res, this, type);
        }

        private static object TranslateSurrogate(object deserializedValue, NewtonSoftJsonSerializer parent, Type type)
        {
            if (deserializedValue is JObject j)
            {
                //The JObject represents a special akka.net wrapper for primitives (int,float,decimal) to preserve correct type when deserializing
                if (j["$"] is object)
                {
                    var value = j["$"].Value<string>();
                    return GetValue(value);
                }

                //The JObject is not of our concern, let Json.NET deserialize it.
                return j.ToObject(type, parent._serializer);
            }

            //The deserialized object is a surrogate, unwrap it
            if (deserializedValue is ISurrogate surrogate)
            {
                return surrogate.FromSurrogate(parent._system);
            }
            return deserializedValue;
        }

        private static object GetValue(string V)
        {
            var t = V.Substring(0, 1);
            var v = V.Substring(1);
            switch (t)
            {
                case "I":
                    return int.Parse(v, NumberFormatInfo.InvariantInfo);
                case "F":
                    return float.Parse(v, NumberFormatInfo.InvariantInfo);
                case "M":
                    return decimal.Parse(v, NumberFormatInfo.InvariantInfo);
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>TBD</summary>
        internal class SurrogateConverter : JsonConverter
        {
            private readonly NewtonSoftJsonSerializer _parent;
            /// <summary>TBD</summary>
            /// <param name="parent">TBD</param>
            public SurrogateConverter(NewtonSoftJsonSerializer parent)
            {
                _parent = parent;
            }

            private static readonly CachedReadConcurrentDictionary<Type, bool> s_canConvertedTypes =
                new CachedReadConcurrentDictionary<Type, bool>(DictionaryCacheConstants.SIZE_SMALL);
            private static readonly Func<Type, bool> s_canConvertFunc = CanConvertInternal;

            /// <summary>Determines whether this instance can convert the specified object type.</summary>
            /// <param name="objectType">Type of the object.</param>
            /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
            public override bool CanConvert(Type objectType)
            {
                return s_canConvertedTypes.GetOrAdd(objectType, s_canConvertFunc);
            }
            private static bool CanConvertInternal(Type objectType)
            {
                if (objectType == typeof(int) || objectType == typeof(float) || objectType == typeof(decimal)) { return true; }

                if (typeof(ISurrogated).IsAssignableFrom(objectType)) { return true; }

                if (objectType == typeof(object)) { return true; }

                return false;
            }

            /// <summary>Reads the JSON representation of the object.</summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>The object value.</returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return DeserializeFromReader(reader, serializer, objectType);
            }

            private object DeserializeFromReader(JsonReader reader, JsonSerializer serializer, Type objectType)
            {
                var surrogate = serializer.Deserialize(reader);
                return TranslateSurrogate(surrogate, _parent, objectType);
            }

            /// <summary>Writes the JSON representation of the object.</summary>
            /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
            /// <param name="value">The value.</param>
            /// <param name="serializer">The calling serializer.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                switch (value)
                {
                    case int _:
                    case decimal _:
                    case float _:
                        writer.WriteStartObject();
                        writer.WritePropertyName("$");
                        writer.WriteValue(GetString(value));
                        writer.WriteEndObject();
                        break;

                    case ISurrogated surrogated:
                        var surrogate = surrogated.ToSurrogate(_parent._system);
                        serializer.Serialize(writer, surrogate);
                        break;

                    default:
                        serializer.Serialize(writer, value);
                        break;
                }
            }

            private object GetString(object value)
            {
                switch (value)
                {
                    case int intValue:
                        return "I" + intValue.ToString(NumberFormatInfo.InvariantInfo);

                    case float floatValue:
                        return "F" + floatValue.ToString(NumberFormatInfo.InvariantInfo);

                    case decimal decimalValue:
                        return "M" + decimalValue.ToString(NumberFormatInfo.InvariantInfo);

                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}
