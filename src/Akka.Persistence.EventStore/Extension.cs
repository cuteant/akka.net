using Akka.Actor;
using Akka.Configuration;

namespace Akka.Persistence.EventStore
{
    public class EventStorePersistence : IExtension
    {
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<EventStorePersistence>("Akka.Persistence.EventStore.reference.conf");
        }

        public static EventStorePersistence Get(ActorSystem system)
        {
            return system.WithExtension<EventStorePersistence, EventStorePersistenceProvider>();
        }

        public EventStorePersistence(ExtendedActorSystem system)
        {
            if (system is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.system); }

            // Initialize fallback configuration defaults
            system.Settings.InjectTopLevelFallback(DefaultConfiguration());

            // Read config
            var journalConfig = system.Settings.Config.GetConfig("akka.persistence.journal.eventstore");
            JournalSettings = new EventStoreJournalSettings(journalConfig);
            var snapshotConfig = system.Settings.Config.GetConfig("akka.persistence.snapshot-store.eventstore");
            SnapshotStoreSettings = new EventStoreSnapshotSettings(snapshotConfig);
        }

        /// <summary>The settings for the EventStore journal.</summary>
        public EventStoreJournalSettings JournalSettings { get; }
        
        /// <summary>The settings for the EventStore snapshot store.</summary>
        public EventStoreSnapshotSettings SnapshotStoreSettings { get; }

    }

    /// <summary>Extension Id provider for the EventStore Persistence extension.</summary>
    public class EventStorePersistenceProvider : ExtensionIdProvider<EventStorePersistence>
    {
        /// <summary>Creates an actor system extension for akka persistence EventStore support.</summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public override EventStorePersistence CreateExtension(ExtendedActorSystem system)
        {
            return new EventStorePersistence(system);
        }
    }
}