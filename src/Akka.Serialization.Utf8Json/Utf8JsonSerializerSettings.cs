using System;
using Akka.Configuration;

namespace Akka.Serialization
{
    public sealed class Utf8JsonSerializerSettings
    {
#if DESKTOPCLR
        private const int c_initialBufferSize = 1024 * 80;
#else
        private const int c_initialBufferSize = 1024 * 64;
#endif
        private const int c_maxBufferSize = 1024 * 1024;

        public static readonly Utf8JsonSerializerSettings Default = new Utf8JsonSerializerSettings(
            initialBufferSize: c_initialBufferSize,
            nameMutate: "original",
            enumAsString: true);

        public static Utf8JsonSerializerSettings Create(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "MsgPackSerializerSettings require a config, default path: `akka.serializers.msgpack`");

            return new Utf8JsonSerializerSettings(
                initialBufferSize: (int)config.GetByteSize("initial-buffer-size", c_initialBufferSize),
                nameMutate: config.GetString("name-mutate", "original"),
                enumAsString: config.GetBoolean("enum-as-string", true));
        }

        public Utf8JsonSerializerSettings(int initialBufferSize, string nameMutate, bool enumAsString)
        {
            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > c_maxBufferSize) { initialBufferSize = c_maxBufferSize; }
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
