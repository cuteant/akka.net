//-----------------------------------------------------------------------
// <copyright file="SqliteJournalSerializationSpec.cs" company="Akka.NET Project">
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
    public class SqliteJournalSerializationSpec : JournalSerializationSpec
    {
        private static AtomicCounter Counter { get; } = new AtomicCounter(0);

        public SqliteJournalSerializationSpec(ITestOutputHelper output)
            : base(CreateSpecConfig("Filename=file:serialization-journal-" + Counter.IncrementAndGet() + ".db"), "SqliteJournalSerializationSpec", output)
        {
        }

        private static Config CreateSpecConfig(string connectionString)
        {
            return ConfigurationFactory.ParseString(@"
                akka.persistence {
                    publish-plugin-commands = on
                    journal {
                        plugin = ""akka.persistence.journal.wings""
                        wings {
                            event-adapters {
                                custom-adapter = ""Akka.Persistence.TCK.Serialization.TestJournal+MyWriteAdapter, Akka.Persistence.TCK""
                            }
                            event-adapter-bindings = {
                                ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayload3, Akka.Persistence.TCK"" = custom-adapter
                            }
                            class = ""Akka.Persistence.Wings.Journal.WingJournal, Akka.Persistence.Wings""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            auto-initialize = on
                            connection-string = """ + connectionString + @"""
                            dialect-provider = ""CuteAnt.Wings.Sqlite.SqliteDialectProvider, CuteAnt.Wings.Sqlite""
                        }
                    }
                }");
        }

        [Fact(Skip = "Sql plugin does not support EventAdapter.Manifest")]
        public override void Journal_should_serialize_Persistent_with_EventAdapter_manifest()
        {
        }
    }
}
