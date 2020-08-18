using System;
using Akka.Configuration;

namespace Akka.Serialization
{
    public sealed class SpanJsonSerializerSettings
    {
        public static readonly SpanJsonSerializerSettings Default = new SpanJsonSerializerSettings(
            includeNulls: false,
            nameMutate: "original",
            enumAsString: true,
            escapeHandling: "default");

        public static SpanJsonSerializerSettings Create(Config config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config), "MsgPackSerializerSettings require a config, default path: `akka.serializers.json-span`");

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
