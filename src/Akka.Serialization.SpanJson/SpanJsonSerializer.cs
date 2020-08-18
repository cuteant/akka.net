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
}
