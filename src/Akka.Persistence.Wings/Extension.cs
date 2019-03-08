using Akka.Actor;
using Akka.Configuration;

namespace Akka.Persistence.Wings
{
    /// <summary>TBD</summary>
    public class WingPersistence : IExtension
    {
        /// <summary>Returns a default configuration for akka persistence Wings-based journals and snapshot stores.</summary>
        /// <returns>TBD</returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<WingPersistence>("Akka.Persistence.Wings.reference.conf");
        }

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public static WingPersistence Get(ActorSystem system)
        {
            return system.WithExtension<WingPersistence, WingPersistenceProvider>();
        }

        /// <summary>Journal-related settings loaded from HOCON configuration.</summary>
        public readonly Config DefaultJournalConfig;

        /// <summary>Snapshot store related settings loaded from HOCON configuration.</summary>
        public readonly Config DefaultSnapshotConfig;

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        public WingPersistence(ExtendedActorSystem system)
        {
            var defaultConfig = DefaultConfiguration();
            system.Settings.InjectTopLevelFallback(defaultConfig);

            DefaultJournalConfig = defaultConfig.GetConfig(WingJournalSettings.ConfigPath);
            DefaultSnapshotConfig = defaultConfig.GetConfig(WingSnapshotStoreSettings.ConfigPath);
        }
    }

    /// <summary>TBD</summary>
    public class WingPersistenceProvider : ExtensionIdProvider<WingPersistence>
    {
        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public override WingPersistence CreateExtension(ExtendedActorSystem system)
        {
            return new WingPersistence(system);
        }
    }
}
