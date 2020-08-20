using System;
using Akka.Actor;
using Akka.Configuration;
using SpanJson;

namespace Akka.Serialization
{
    /// <summary>This is a special <see cref="Serializer"/> that serializes and deserializes javascript objects only.
    /// These objects need to be in the JavaScript Object Notation (JSON) format.</summary>
    public sealed class SpanJsonSerializer : SerializerWithTypeManifest
    {
        /// <summary>Initializes a new instance of the <see cref="SpanJsonSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public SpanJsonSerializer(ExtendedActorSystem system) : this(system, SpanJsonSerializerSettings.Default) { }

        /// <summary>Initializes a new instance of the <see cref="SpanJsonSerializer"/> class.</summary>
        public SpanJsonSerializer(ExtendedActorSystem system, Config config) : this(system, SpanJsonSerializerSettings.Create(config)) { }

        /// <summary>Initializes a new instance of the <see cref="SpanJsonSerializer"/> class.</summary>
        public SpanJsonSerializer(ExtendedActorSystem system, SpanJsonSerializerSettings settings) : base(system)
        {
        }

        /// <inheritdoc />
        public sealed override int Identifier => 106;

        /// <inheritdoc />
        public override bool IsJson => true;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source is null) { return null; }

            var type = source.GetType();
            var serializedObject = JsonSerializer.NonGeneric.Utf8.Serialize(source);
            return JsonSerializer.NonGeneric.Utf8.Deserialize(serializedObject, type);
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, Type type)
        {
            return JsonCamelCaseSerializer.NonGeneric.Utf8.Deserialize(bytes, type);
        }

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object obj)
        {
            //if (obj is null) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理
            return JsonCamelCaseSerializer.NonGeneric.Utf8.Serialize(obj);
        }
    }

    public sealed class SpanJsonSerializerSettings
    {
        public static readonly SpanJsonSerializerSettings Default = new SpanJsonSerializerSettings(
            includeNulls: false,
            nameMutate: "original",
            enumAsString: true,
            escapeHandling: "default");

        public static SpanJsonSerializerSettings Create(Config config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config), "MsgPackSerializerSettings require a config, default path: `akka.serializers.spanjson`");

            return new SpanJsonSerializerSettings(
                includeNulls: config.GetBoolean("include-nulls", false),
                nameMutate: config.GetString("name-mutate", "original"),
                enumAsString: config.GetBoolean("enum-as-string", true),
                escapeHandling: config.GetString("escape-handling", "default"));
        }

        public SpanJsonSerializerSettings(bool includeNulls, string nameMutate, bool enumAsString, string escapeHandling)
        {
            IncludeNulls = includeNulls;
            NameMutate = nameMutate;
            EnumAsString = enumAsString;
            EscapeHandling = escapeHandling;
        }

        public string EscapeHandling { get; }

        /// <summary>The initial buffer size.</summary>
        public bool IncludeNulls;

        public string NameMutate { get; }

        public bool EnumAsString { get; }
    }
}
