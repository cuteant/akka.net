using System;
using Akka.Configuration;

namespace Akka.Serialization
{
    /// <summary>A typed settings class for a <see cref="MsgPackSerializer"/>.</summary>
    public class MsgPackSerializerSettings
    {
        /// <summary>Creates a new instance of a <see cref="MsgPackSerializerSettings"/>.</summary>
        /// <param name="initialBufferSize">The initial buffer size.</param>
        public MsgPackSerializerSettings(int initialBufferSize)
        {
            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > 81920) { initialBufferSize = 81920; }
            InitialBufferSize = initialBufferSize;
        }

        /// <summary>The initial buffer size.</summary>
        public readonly int InitialBufferSize;

        /// <summary>Default settings used by <see cref="MsgPackSerializer"/> when no config has been specified.</summary>
        public static readonly MsgPackSerializerSettings Default = new MsgPackSerializerSettings(
            initialBufferSize: 1024 * 64);

        /// <summary>Creates a new instance of <see cref="MsgPackSerializerSettings"/> using provided HOCON config.</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static MsgPackSerializerSettings Create(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "MsgPackSerializerSettings require a config, default path: `akka.serializers.msgpack`");

            return new MsgPackSerializerSettings(
                initialBufferSize: config.GetInt("initial-buffer-size", 1024 * 64));
        }
    }
}
