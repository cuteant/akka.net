using System;
using Akka.Configuration;

namespace Akka.Serialization
{
    public sealed class Utf8JsonSerializerSettings
    {
        public static readonly Utf8JsonSerializerSettings Default = new Utf8JsonSerializerSettings(
            initialBufferSize: 1024 * 80,
            nameMutate: "original",
            enumAsString: true);

        public static Utf8JsonSerializerSettings Create(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "MsgPackSerializerSettings require a config, default path: `akka.serializers.msgpack`");

            return new Utf8JsonSerializerSettings(
                initialBufferSize: config.GetInt("initial-buffer-size", 1024 * 80),
                nameMutate: config.GetString("name-mutate", "original"),
                enumAsString: config.GetBoolean("enum-as-string", true));
        }

        public Utf8JsonSerializerSettings(int initialBufferSize, string nameMutate, bool enumAsString)
        {
            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > 81920) { initialBufferSize = 81920; }
            InitialBufferSize = initialBufferSize;

            NameMutate = nameMutate;
            EnumAsString = enumAsString;
        }


        /// <summary>The initial buffer size.</summary>
        public readonly int InitialBufferSize;

        public string NameMutate { get; }

        public bool EnumAsString { get; }
    }
}
