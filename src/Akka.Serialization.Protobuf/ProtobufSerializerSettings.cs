﻿using System;
using Akka.Configuration;

namespace Akka.Serialization
{
    /// <summary>A typed settings class for a <see cref="ProtobufSerializer"/>.</summary>
    public class ProtobufSerializerSettings
    {
#if DESKTOPCLR
        private const int c_initialBufferSize = 1024 * 80;
#else
        private const int c_initialBufferSize = 1024 * 64;
#endif
        private const int c_maxBufferSize = 1024 * 1024;

        /// <summary>Creates a new instance of a <see cref="ProtobufSerializerSettings"/>.</summary>
        /// <param name="initialBufferSize">The initial buffer size.</param>
        public ProtobufSerializerSettings(int initialBufferSize)
        {
            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > c_maxBufferSize) { initialBufferSize = c_maxBufferSize; }
            InitialBufferSize = initialBufferSize;
        }

        /// <summary>The initial buffer size.</summary>
        public readonly int InitialBufferSize;

        /// <summary>Default settings used by <see cref="ProtobufSerializer"/> when no config has been specified.</summary>
        public static readonly ProtobufSerializerSettings Default = new ProtobufSerializerSettings(
            initialBufferSize: c_initialBufferSize);

        /// <summary>Creates a new instance of <see cref="ProtobufSerializerSettings"/> using provided HOCON config.</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ProtobufSerializerSettings Create(Config config)
        {
            if (config.IsNullOrEmpty())
            {
                throw ConfigurationException.NullOrEmptyConfig<ProtobufSerializerSettings>("akka.serializers.protobuf");
            }
            if (config == null) throw new ArgumentNullException(nameof(config), "MsgPackSerializerSettings require a config, default path: `akka.serializers.protobuf`");

            return new ProtobufSerializerSettings(
                initialBufferSize: (int)config.GetByteSize("initial-buffer-size", c_initialBufferSize));
        }
    }
}
