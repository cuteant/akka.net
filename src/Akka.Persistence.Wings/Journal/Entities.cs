using System;
using CuteAnt;
using CuteAnt.Wings.DataAnnotations;

namespace Akka.Persistence.Wings.Journal
{
    [CompositeIndex(true, "PersistenceId", "SequenceNr")]
    public sealed class JournalEvent
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [Required, StringLength(255, true)]
        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        [Required, StringLength(255, true)]
        public string Tags { get; set; }

        public byte[] Payload { get; set; }

        public int SerializerId { get; set; }

        [Required, StringLength(500, false)]
        public string Manifest { get; set; }

        public int ExtensibleData { get; set; }

        public bool IsDeleted { get; set; }

        public long CreatedAt { get; set; }
    }

    [CompositeIndex(true, "PersistenceId", "SequenceNr")]
    public sealed class JournalMetadata
    {
        [PrimaryKey]
        public CombGuid Id { get; set; }

        [Required, StringLength(255, true)]
        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }
    }

    [CompositeIndex(true, "Id", "Tag")]
    public sealed class JournalTags
    {
        [PrimaryKey]
        public CombGuid Id { get; set; }

        [Required, StringLength(255, true)]
        public string Tag { get; set; }
    }
}
