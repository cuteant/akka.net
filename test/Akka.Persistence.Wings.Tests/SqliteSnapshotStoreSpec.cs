//-----------------------------------------------------------------------
// <copyright file="SqliteSnapshotStoreSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.TCK.Snapshot;
using Akka.Util.Internal;
using Xunit.Abstractions;

namespace Akka.Persistence.Wings.Tests
{
    public class SqliteSnapshotStoreSpec : SnapshotStoreSpec
    {
        private static AtomicCounter counter = new AtomicCounter(0);
        public SqliteSnapshotStoreSpec(ITestOutputHelper output)
            : base(CreateSpecConfig("App_Data/memdb-snapshot-" + counter.IncrementAndGet() + ".db"), "SqliteSnapshotStoreSpec", output)
        {
            WingPersistence.Get(Sys);
            CuteAnt.Wings.WingsConfig.DateTimeStyle = System.DateTimeKind.Utc;
            Initialize();
        }

        private static Config CreateSpecConfig(string connectionString)
        {
            return ConfigurationFactory.ParseString(@"
                akka.persistence {
                    publish-plugin-commands = on
                    snapshot-store {
                        plugin = ""akka.persistence.snapshot-store.wings""
                        wings {
                            class = ""Akka.Persistence.Wings.Snapshot.WingSnapshotStore, Akka.Persistence.Wings""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            auto-initialize = on
                            connection-string = """ + connectionString + @"""
                            dialect-provider = ""CuteAnt.Wings.Sqlite.SqliteDialectProvider, CuteAnt.Wings.Sqlite""
                        }
                    }
                }");
        }
    }
}
