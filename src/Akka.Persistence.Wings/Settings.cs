//-----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Annotations;
using Akka.Configuration;
using Akka.Persistence.Wings.Journal;
using CuteAnt;
using CuteAnt.Wings;

namespace Akka.Persistence.Wings
{
    /// <summary>
    /// Configuration settings representation targeting Sql Server journal actor.
    /// </summary>
    public class WingJournalSettings : IShardingOption
    {
        public const string ConfigPath = "akka.persistence.journal.wings";

        public bool UseOneDbServer { get; }
        public ShardingKeyMode DbServerShardingMode { get; }

        public bool UseOneDatabase { get; }
        public ShardingKeyMode DatabaseShardingMode { get; }

        public bool UseOneTable { get; }
        public ShardingKeyMode TableShardingMode { get; }

        public string ConnectionName { get; set; }

        public string DatabaseName { get; set; }

        /// <summary>
        /// Fully qualified type name for <see cref="ITimestampProvider"/> used to generate journal timestamps.
        /// </summary>
        public string TimestampProvider { get; set; }

        /// <summary>
        /// Flag determining in in case of event journal or metadata table missing, they should be automatically initialized.
        /// </summary>
        public bool AutoInitialize { get; private set; }

        /// <summary>
        /// The default serializer being used if no type match override is specified
        /// </summary>
        public string DefaultSerializer { get; }

        [InternalApi]
        internal readonly IWingsConnectionFactory Factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WingJournalSettings"/> class.
        /// </summary>
        /// <param name="config">The configuration used to configure the settings.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="config"/> is undefined.
        /// </exception>
        public WingJournalSettings(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "SqlServer journal settings cannot be initialized, because required HOCON section couldn't been found");

            UseOneDbServer = config.GetBoolean("singleton-db-server", true);
            if (Utils.TryParseShardingKey(config.GetString("db-server-sharding-mode"), out var mode))
            {
                DbServerShardingMode = mode;
            }
            else
            {
                UseOneDbServer = true;
            }
            UseOneDatabase = config.GetBoolean("singleton-database", true);
            if (Utils.TryParseShardingKey(config.GetString("database-sharding-mode"), out mode))
            {
                DatabaseShardingMode = mode;
            }
            else
            {
                UseOneDatabase = true;
            }
            UseOneTable = config.GetBoolean("singleton-table", true);
            if (Utils.TryParseShardingKey(config.GetString("table-sharding-mode"), out mode))
            {
                TableShardingMode = mode;
            }
            else
            {
                UseOneTable = true;
            }

            ConnectionName = config.GetString("connection-name");
            DatabaseName = config.GetString("database-name");

            TimestampProvider = config.GetString("timestamp-provider");
            AutoInitialize = config.GetBoolean("auto-initialize");
            DefaultSerializer = config.GetString("serializer");

            var connStr = config.GetString("connection-string");
            var dialectProviderType = config.GetString("dialect-provider");
            if (!string.IsNullOrEmpty(connStr) && !string.IsNullOrEmpty(dialectProviderType))
            {
                Factory = new WingsConnectionFactory(connStr, Utils.GetDialectProvider(dialectProviderType));
                Factory.TryRegister("Default", Factory);
                WingsConnection.Factory = Factory;
            }
        }
    }

    /// <summary>
    /// Configuration settings representation targeting Sql Server snapshot store actor.
    /// </summary>
    public class WingSnapshotStoreSettings : IShardingOption
    {
        public const string ConfigPath = "akka.persistence.snapshot-store.wings";

        public bool UseOneDbServer { get; }
        public ShardingKeyMode DbServerShardingMode { get; }

        public bool UseOneDatabase { get; }
        public ShardingKeyMode DatabaseShardingMode { get; }

        public bool UseOneTable { get; }
        public ShardingKeyMode TableShardingMode { get; }

        public string ConnectionName { get; set; }

        public string DatabaseName { get; set; }

        /// <summary>
        /// Flag determining in in case of snapshot store table missing, they should be automatically initialized.
        /// </summary>
        public bool AutoInitialize { get; }

        /// <summary>
        /// The default serializer being used if no type match override is specified
        /// </summary>
        public string DefaultSerializer { get; }

        [InternalApi]
        internal readonly IWingsConnectionFactory Factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WingSnapshotStoreSettings"/> class.
        /// </summary>
        /// <param name="config">The configuration used to configure the settings.</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="config"/> is undefined.
        /// </exception>
        public WingSnapshotStoreSettings(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "SqlServer snapshot store settings cannot be initialized, because required HOCON section couldn't been found");

            UseOneDbServer = config.GetBoolean("singleton-db-server", true);
            if (Utils.TryParseShardingKey(config.GetString("db-server-sharding-mode"), out var mode))
            {
                DbServerShardingMode = mode;
            }
            else
            {
                UseOneDbServer = true;
            }
            UseOneDatabase = config.GetBoolean("singleton-database", true);
            if (Utils.TryParseShardingKey(config.GetString("database-sharding-mode"), out mode))
            {
                DatabaseShardingMode = mode;
            }
            else
            {
                UseOneDatabase = true;
            }
            UseOneTable = config.GetBoolean("singleton-table", true);
            if (Utils.TryParseShardingKey(config.GetString("table-sharding-mode"), out mode))
            {
                TableShardingMode = mode;
            }
            else
            {
                UseOneTable = true;
            }

            ConnectionName = config.GetString("connection-name");
            DatabaseName = config.GetString("database-name");

            AutoInitialize = config.GetBoolean("auto-initialize");
            DefaultSerializer = config.GetString("serializer");

            var connStr = config.GetString("connection-string");
            var dialectProviderType = config.GetString("dialect-provider");
            if (!string.IsNullOrEmpty(connStr) && !string.IsNullOrEmpty(dialectProviderType))
            {
                Factory = new WingsConnectionFactory(connStr, Utils.GetDialectProvider(dialectProviderType));
                Factory.TryRegister("Default", Factory);
                WingsConnection.Factory = Factory;
            }
        }

    }

    static class Utils
    {
        internal static IWingsDialectProvider GetDialectProvider(string typeName)
        {
            var type = Type.GetType(typeName, true);
            return (IWingsDialectProvider)Activator.CreateInstance(type);
        }

        public static bool TryParseShardingKey(string str, out ShardingKeyMode mode)
        {
            str = str?.ToLowerInvariant();
            switch (str)
            {
                case "guidbasedtiny":
                    mode = ShardingKeyMode.GuidBasedTiny; return true;
                case "guidbasedsmall":
                    mode = ShardingKeyMode.GuidBasedSmall; return true;
                case "guidbasedmedium":
                    mode = ShardingKeyMode.GuidBasedMedium; return true;
                case "guidbasedlarge":
                    mode = ShardingKeyMode.GuidBasedLarge; return true;
                case "guidbasedlarge32":
                    mode = ShardingKeyMode.GuidBasedLarge32; return true;
                case "guidbasedlarge64":
                    mode = ShardingKeyMode.GuidBasedLarge64; return true;
                case "guidbasedlarge128":
                    mode = ShardingKeyMode.GuidBasedLarge128; return true;
                case "guidbasedxlarge":
                    mode = ShardingKeyMode.GuidBasedXLarge; return true;

                case "datetimebasedyear":
                    mode = ShardingKeyMode.DateTimeBasedYear; return true;
                case "datetimebasedmonth":
                    mode = ShardingKeyMode.DateTimeBasedMonth; return true;
                case "datetimebasedday":
                    mode = ShardingKeyMode.DateTimeBasedDay; return true;

                default:
                    mode = default; return false;
            }
        }
    }
}
