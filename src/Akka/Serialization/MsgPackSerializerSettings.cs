using System;
using Akka.Configuration;

namespace Akka.Serialization
{
    /// <summary>A typed settings class for a <see cref="MsgPackSerializer"/>.</summary>
    public class MsgPackSerializerSettings
    {
#if DESKTOPCLR
        private const int c_initialBufferSize = 1024 * 80;
#else
        private const int c_initialBufferSize = 1024 * 64;
#endif
        private const int c_maxBufferSize = 1024 * 1024;

        /// <summary>Creates a new instance of a <see cref="MsgPackSerializerSettings"/>.</summary>
        /// <param name="initialBufferSize">The initial buffer size.</param>
        public MsgPackSerializerSettings(int initialBufferSize)
        {
            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > c_maxBufferSize) { initialBufferSize = c_maxBufferSize; }
            InitialBufferSize = initialBufferSize;
        }

        /// <summary>The initial buffer size.</summary>
        public readonly int InitialBufferSize;

        /// <summary>Default settings used by <see cref="MsgPackSerializer"/> when no config has been specified.</summary>
        public static readonly MsgPackSerializerSettings Default = new MsgPackSerializerSettings(
            initialBufferSize: c_initialBufferSize);

        /// <summary>Creates a new instance of <see cref="MsgPackSerializerSettings"/> using provided HOCON config.</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static MsgPackSerializerSettings Create(Config config)
        {
            if (config.IsNullOrEmpty())
            {
                throw ConfigurationException.NullOrEmptyConfig<MsgPackSerializerSettings>("akka.serializers.msgpack");
            }

            return new MsgPackSerializerSettings(
                initialBufferSize: (int)config.GetByteSize("initial-buffer-size", c_initialBufferSize));
        }
    }
}
