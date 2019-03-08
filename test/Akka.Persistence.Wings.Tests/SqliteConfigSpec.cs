//-----------------------------------------------------------------------
// <copyright file="SqliteConfigSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Wings.Tests
{
    public class SqliteConfigSpec : Akka.TestKit.Xunit2.TestKit
    {
        public SqliteConfigSpec(ITestOutputHelper output) : base(output: output)
        {
        }

        [Fact]
        public void Should_sqlite_journal_has_default_config()
        {
            WingPersistence.Get(Sys);

            var config = Sys.Settings.Config.GetConfig("akka.persistence.journal.wings");

            Assert.NotNull(config);
            Assert.Equal("Akka.Persistence.Wings.Journal.WingJournal, Akka.Persistence.Wings", config.GetString("class"));
            Assert.Equal("akka.actor.default-dispatcher", config.GetString("plugin-dispatcher"));
            //Assert.Equal(string.Empty, config.GetString("connection-string"));
            //Assert.Equal(string.Empty, config.GetString("connection-string-name"));
            //Assert.Equal(TimeSpan.FromSeconds(30), config.GetTimeSpan("connection-timeout"));
            //Assert.Equal("event_journal", config.GetString("table-name"));
            //Assert.Equal("journal_metadata", config.GetString("metadata-table-name"));
            Assert.False(config.GetBoolean("auto-initialize"));
            Assert.Equal("Akka.Persistence.Wings.Journal.DefaultTimestampProvider, Akka.Persistence.Wings", config.GetString("timestamp-provider"));
        }

        [Fact]
        public void Should_sqlite_snapshot_has_default_config()
        {
            WingPersistence.Get(Sys);

            var config = Sys.Settings.Config.GetConfig("akka.persistence.snapshot-store.wings");

            Assert.NotNull(config);
            Assert.Equal("Akka.Persistence.Wings.Snapshot.WingSnapshotStore, Akka.Persistence.Wings", config.GetString("class"));
            Assert.Equal("akka.actor.default-dispatcher", config.GetString("plugin-dispatcher"));
            //Assert.Equal(string.Empty, config.GetString("connection-string"));
            //Assert.Equal(string.Empty, config.GetString("connection-string-name"));
            //Assert.Equal(TimeSpan.FromSeconds(30), config.GetTimeSpan("connection-timeout"));
            //Assert.Equal("snapshot_store", config.GetString("table-name"));
            Assert.False(config.GetBoolean("auto-initialize"));
        }
    }
}
