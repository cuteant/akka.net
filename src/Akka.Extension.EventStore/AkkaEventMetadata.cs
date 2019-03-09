using System.Collections.Generic;
using System.Runtime.Serialization;
using EventStore.ClientAPI;

namespace Akka.Extension.EventStore
{
    public class AkkaEventMetadata : IEventMetadata
    {
        [DataMember(Name = "clrType")]
        public string ClrEventType { get; set; }

        [DataMember(Name = "meta")]
        public Dictionary<string, object> Context { get; set; }

        [DataMember(Name = "identifier")]
        public int Identifier { get; set; }

        [DataMember(Name = "manifest")]
        public string Manifest { get; set; }
    }
}
