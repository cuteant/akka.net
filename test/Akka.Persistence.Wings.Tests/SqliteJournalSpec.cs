//-----------------------------------------------------------------------
// <copyright file="SqliteJournalSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.TCK.Journal;
using Akka.Util.Internal;
using Xunit.Abstractions;

namespace Akka.Persistence.Wings.Tests
{
    public class SqliteJournalSpec : JournalSpec
    {
        private static AtomicCounter counter = new AtomicCounter(0);

        public SqliteJournalSpec(ITestOutputHelper output)
            : base(CreateSpecConfig("App_Data/memdb-journal-" + counter.IncrementAndGet() + ".db"), "SqliteJournalSpec", output)
        {
            WingPersistence.Get(Sys);

            Initialize();
        }

        private static Config CreateSpecConfig(string connectionString)
        {
            return ConfigurationFactory.ParseString(@"
                akka.persistence {
                    publish-plugin-commands = on
                    journal {
                        plugin = ""akka.persistence.journal.wings""
                        wings {
                            class = ""Akka.Persistence.Wings.Journal.WingJournal, Akka.Persistence.Wings""
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
