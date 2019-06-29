using System.Collections.Generic;
using System.Runtime.Serialization;
using EventStore.ClientAPI;
using SpanJson;

namespace Akka.Extension.EventStore
{
    public class AkkaEventMetadata : IEventMetadata
    {
        [DataMember(Name = "clrType")]
        public string ClrEventType { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Context { get; set; }

        public int Identifier { get; set; }

        public string Manifest { get; set; }
    }
}
