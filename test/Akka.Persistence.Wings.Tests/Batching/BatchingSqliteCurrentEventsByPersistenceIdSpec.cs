//-----------------------------------------------------------------------
// <copyright file="BatchingSqliteCurrentEventsByPersistenceIdSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Wings.Query;
using Akka.Util.Internal;
using Akka.Persistence.TCK.Query;
using Xunit.Abstractions;

namespace Akka.Persistence.Wings.Tests.Batching
{
    public class BatchingSqliteCurrentEventsByPersistenceIdSpec : CurrentEventsByPersistenceIdSpec
    {
        public static readonly AtomicCounter Counter = new AtomicCounter(0);
        public static Config Config(int id) => ConfigurationFactory.ParseString($@"
            akka.loglevel = INFO
            akka.persistence.journal.plugin = ""akka.persistence.journal.wings""
            akka.persistence.journal.wings {{
                class = ""Akka.Persistence.Wings.Journal.BatchingWingJournal, Akka.Persistence.Wings""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                auto-initialize = on
                connection-string = ""App_Data/memdb-journal-batch-currenteventsbypersistenceid-{id}.db""
                dialect-provider = ""CuteAnt.Wings.Sqlite.SqliteDialectProvider, CuteAnt.Wings.Sqlite""
                refresh -interval = 1s
            }}
            akka.test.single-expect-default = 10s")
            .WithFallback(WingReadJournal.DefaultConfiguration());

        public BatchingSqliteCurrentEventsByPersistenceIdSpec(ITestOutputHelper output) : base(Config(Counter.GetAndIncrement()), nameof(BatchingSqliteCurrentEventsByPersistenceIdSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<WingReadJournal>(WingReadJournal.Identifier);
        }
    }
}
