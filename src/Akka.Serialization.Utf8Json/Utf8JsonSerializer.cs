using System;
using Akka.Actor;
using Akka.Configuration;
using Utf8Json;
using Utf8Json.ImmutableCollection;

namespace Akka.Serialization
{
    /// <summary>This is a special <see cref="Serializer"/> that serializes and deserializes javascript objects only.
    /// These objects need to be in the JavaScript Object Notation (JSON) format.</summary>
    public sealed class Utf8JsonSerializer : SerializerWithTypeManifest
    {
        private readonly IJsonFormatterResolver _resolver;
        private readonly int _initialBufferSize;

        static Utf8JsonSerializer()
        {
            Utf8JsonStandardResolver.TryRegister(ImmutableCollectionResolver.Instance);
        }

        /// <summary>Initializes a new instance of the <see cref="Utf8JsonSerializer"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public Utf8JsonSerializer(ExtendedActorSystem system) : this(system, Utf8JsonSerializerSettings.Default) { }

        /// <summary>Initializes a new instance of the <see cref="Utf8JsonSerializer"/> class.</summary>
        public Utf8JsonSerializer(ExtendedActorSystem system, Config config) : this(system, Utf8JsonSerializerSettings.Create(config)) { }

        /// <summary>Initializes a new instance of the <see cref="Utf8JsonSerializer"/> class.</summary>
        public Utf8JsonSerializer(ExtendedActorSystem system, Utf8JsonSerializerSettings settings) : base(system)
        {
            _initialBufferSize = settings.InitialBufferSize;
            if (!settings.EnumAsString)
            {
                Utf8JsonStandardResolver.TryRegister(Utf8Json.Resolvers.EnumResolver.UnderlyingValue);
            }
            var nameMutate = settings.NameMutate.ToLowerInvariant();
            switch (nameMutate)
            {
                case "camelcase":
                    _resolver = Utf8JsonStandardResolver.CamelCase;
                    break;
                case "snakecase":
                    _resolver = Utf8JsonStandardResolver.SnakeCase;
                    break;
                case "original":
                default:
                    _resolver = Utf8JsonStandardResolver.Default;
                    break;
            }
        }

        /// <inheritdoc />
        public sealed override int Identifier => 106;

        /// <inheritdoc />
        public override bool IsJson => true;

        /// <inheritdoc />
        public sealed override object DeepCopy(object source)
        {
            if (source == null) { return null; }

            var type = source.GetType();
            var serializedObject = JsonSerializer.SerializeUnsafe(source, _resolver);
            return JsonSerializer.NonGeneric.Deserialize(type, serializedObject.Array, serializedObject.Offset, _resolver);
        }

        /// <inheritdoc />
        public sealed override object FromBinary(byte[] bytes, Type type) => JsonSerializer.NonGeneric.Deserialize(type, bytes, 0, _resolver);

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object obj)
        {
            //if (null == obj) { return EmptyArray<byte>.Instance; } // 空对象交由 NullSerializer 处理
            return JsonSerializer.Serialize(obj, _resolver);
        }
    }
}
