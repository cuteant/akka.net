using System;
using CuteAnt;
using CuteAnt.Wings.DataAnnotations;

namespace Akka.Persistence.Wings.Snapshot
{
    [CompositeIndex(true, "PersistenceId", "SequenceNr")]
    public sealed class Snapshots
    {
        [PrimaryKey]
        public CombGuid Id { get; set; }

        [Required, StringLength(255, false)]
        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public byte[] Payload { get; set; }

        public int SerializerId { get; set; }

        [Required, StringLength(500, false)]
        public string Manifest { get; set; }

        public int ExtensibleData { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
