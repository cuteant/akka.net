using System;
using Akka.Actor;
using Akka.Configuration;
using CuteAnt.Reflection;

namespace Akka.Serialization
{
    /// <summary>A typed settings class for a <see cref="Hyperion.Serializer"/>.</summary>
    public sealed class HyperionSerializerSettings
    {
#if DESKTOPCLR
        private const int c_initialBufferSize = 1024 * 80;
#else
        private const int c_initialBufferSize = 1024 * 64;
#endif
        private const int c_maxBufferSize = 1024 * 1024;

        /// <summary>Default settings used by <see cref="Hyperion.Serializer"/> when no config has been specified.</summary>
        public static readonly HyperionSerializerSettings Default = new HyperionSerializerSettings(
            initialBufferSize: c_initialBufferSize,
            preserveObjectReferences: true,
            versionTolerance: true,
            knownTypesProvider: typeof(NoKnownTypes));

        /// <summary>Creates a new instance of <see cref="HyperionSerializerSettings"/> using provided HOCON
        /// config. Config can contain several key-values, that are mapped to a class fields:
        /// <ul><li>`preserve-object-references` (boolean) mapped to <see
        /// cref="PreserveObjectReferences"/></li><li>`version-tolerance` (boolean) mapped to <see
        /// cref="VersionTolerance"/></li><li>`known-types-provider` (fully qualified type name)
        /// mapped to <see cref="KnownTypesProvider"/></li></ul>.</summary>
        /// <exception cref="ArgumentNullException">Raised when <paramref name="config"/> was not provided.</exception>
        /// <exception cref="ArgumentException">Raised when `known-types-provider` type doesn't implement <see cref="IKnownTypesProvider"/> interface.</exception>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HyperionSerializerSettings Create(Config config)
        {
            if (config.IsNullOrEmpty())
            {
                throw ConfigurationException.NullOrEmptyConfig<HyperionSerializerSettings>("akka.serializers.hyperion");
            }

            var typeName = config.GetString("known-types-provider", null);
            var type = !string.IsNullOrEmpty(typeName) ? TypeUtils.ResolveType(typeName) : null; // Type.GetType(typeName, true)

            return new HyperionSerializerSettings(
                initialBufferSize: (int)config.GetByteSize("initial-buffer-size", c_initialBufferSize),
                preserveObjectReferences: config.GetBoolean("preserve-object-references", true),
                versionTolerance: config.GetBoolean("version-tolerance", true),
                knownTypesProvider: type);
        }

        /// <summary>The initial buffer size.</summary>
        public readonly int InitialBufferSize;

        /// <summary>When true, it tells <see cref="Hyperion.Serializer"/> to keep track of references in
        /// serialized/deserialized object graph.</summary>
        public readonly bool PreserveObjectReferences;

        /// <summary>When true, it tells <see cref="Hyperion.Serializer"/> to encode a list of currently
        /// serialized fields into type manifest.</summary>
        public readonly bool VersionTolerance;

        /// <summary>A type implementing <see cref="IKnownTypesProvider"/>, that will be used when <see
        /// cref="Hyperion.Serializer"/> is being constructed to provide a list of message types that
        /// are supposed to be known implicitly by all communicating parties. Implementing class must
        /// provide either a default constructor or a constructor taking <see
        /// cref="ExtendedActorSystem"/> as its only parameter.</summary>
        public readonly Type KnownTypesProvider;

        /// <summary>Creates a new instance of a <see cref="HyperionSerializerSettings"/>.</summary>
        /// <param name="initialBufferSize">The initial buffer size.</param>
        /// <param name="preserveObjectReferences">Flag which determines if serializer should keep track of references in serialized object graph.</param>
        /// <param name="versionTolerance">Flag which determines if field data should be serialized as part of type manifest.</param>
        /// <param name="knownTypesProvider">Type implementing <see cref="IKnownTypesProvider"/> to be used to determine a list of
        /// types implicitly known by all cooperating serializer.</param>
        /// <exception cref="ArgumentException">Raised when `known-types-provider` type doesn't implement <see cref="IKnownTypesProvider"/> interface.</exception>
        public HyperionSerializerSettings(int initialBufferSize, bool preserveObjectReferences, bool versionTolerance, Type knownTypesProvider)
        {
            knownTypesProvider = knownTypesProvider ?? typeof(NoKnownTypes);
            if (!typeof(IKnownTypesProvider).IsAssignableFrom(knownTypesProvider))
                throw new ArgumentException($"Known types provider must implement an interface {typeof(IKnownTypesProvider).FullName}");

            if (initialBufferSize < 1024) { initialBufferSize = 1024; }
            if (initialBufferSize > c_maxBufferSize) { initialBufferSize = c_maxBufferSize; }
            InitialBufferSize = initialBufferSize;

            PreserveObjectReferences = preserveObjectReferences;
            VersionTolerance = versionTolerance;
            KnownTypesProvider = knownTypesProvider;
        }
    }
}