using Akka.Configuration;
using System;

namespace Akka.Persistence.EventStore
{
    /// <summary>Settings for the EventStore journal implementation, parsed from HOCON configuration.</summary>
    public class EventStoreJournalSettings
    {
        public EventStoreJournalSettings(Config config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config),
                    "EventStore journal settings cannot be initialized, because required HOCON section couldn't been found");
            }

            ReadBatchSize = config.GetInt("read-batch-size", 500);
        }

        public int ReadBatchSize { get; }
    }

    /// <summary>Settings for the EventStore snapshot-store implementation, parsed from HOCON configuration.</summary>
    public class EventStoreSnapshotSettings
    {
        public EventStoreSnapshotSettings(Config config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config),
                    "EventStore snapshot-store settings cannot be initialized, because required HOCON section couldn't been found");
            }

            ReadBatchSize = config.GetInt("read-batch-size", 500);
            Prefix = config.GetString("prefix", "snapshot@");
        }

        public int ReadBatchSize { get; }

        public string Prefix { get; }
    }
}