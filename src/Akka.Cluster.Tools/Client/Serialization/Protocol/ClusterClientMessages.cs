using MessagePack;

namespace Akka.Cluster.Tools.Client.Serialization.Protocol
{
    [MessagePackObject]
    public readonly struct Contacts
    {
        [Key(0)]
        public readonly string[] ContactPoints;

        [SerializationConstructor]
        public Contacts(string[] contactPoints) => ContactPoints = contactPoints;
    }

}
