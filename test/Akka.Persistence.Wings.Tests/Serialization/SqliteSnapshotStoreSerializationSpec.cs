﻿//-----------------------------------------------------------------------
// <copyright file="SqliteSnapshotStoreSerializationSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.TCK.Serialization;
using Akka.Util.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Wings.Tests.Serialization
{
    public class SqliteSnapshotStoreSerializationSpec : SnapshotStoreSerializationSpec
    {
        private static AtomicCounter Counter { get; } = new AtomicCounter(0);

        public SqliteSnapshotStoreSerializationSpec(ITestOutputHelper output)
            : base(CreateSpecConfig("Filename=file:serialization-snapshot-" + Counter.IncrementAndGet() + ".db"), "SqliteSnapshotStoreSerializationSpec", output)
        {
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
