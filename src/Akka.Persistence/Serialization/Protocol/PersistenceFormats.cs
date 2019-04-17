using MessagePack;
using PersistentPayload = Akka.Serialization.Protocol.Payload;

namespace Akka.Persistence.Serialization.Protocol
{
    [MessagePackObject]
    public sealed class PersistentMessage
    {
        [Key(0)]
        public PersistentPayload Payload { get; set; }

        [Key(1)]
        public long SequenceNr { get; set; }

        [Key(2)]
        public string PersistenceId { get; set; }

        [Key(3)]
        public bool Deleted { get; set; } // not used in new records from 2.4

        [Key(4)]
        public string Sender { get; set; } // not stored in journal, needed for remote serialization 

        [Key(5)]
        public string Manifest { get; set; }

        [Key(6)]
        public string WriterGuid { get; set; }
    }

    [MessagePackObject]
    public readonly struct AtomicWrite
    {
        [Key(0)]
        public readonly PersistentMessage[] Payload;

        [SerializationConstructor]
        public AtomicWrite(PersistentMessage[] payload) => Payload = payload;
    }

    [MessagePackObject]
    public readonly struct UnconfirmedDelivery
    {
        [Key(0)]
        public readonly long DeliveryId;

        [Key(1)]
        public readonly string Destination;

        [Key(2)]
        public readonly PersistentPayload Payload;

        [SerializationConstructor]
        public UnconfirmedDelivery(long deliveryId, string destination, in PersistentPayload payload)
        {
            DeliveryId = deliveryId;
            Destination = destination;
            Payload = payload;
        }
    }

    [MessagePackObject]
    public readonly struct AtLeastOnceDeliverySnapshot
    {
        [Key(0)]
        public readonly long CurrentDeliveryId;

        [Key(1)]
        public readonly UnconfirmedDelivery[] UnconfirmedDeliveries;

        [SerializationConstructor]
        public AtLeastOnceDeliverySnapshot(long currentDeliveryId, UnconfirmedDelivery[] unconfirmedDeliveries)
        {
            CurrentDeliveryId = currentDeliveryId;
            UnconfirmedDeliveries = unconfirmedDeliveries;
        }
    }

    [MessagePackObject]
    public readonly struct PersistentStateChangeEvent
    {
        [Key(0)]
        public readonly string StateIdentifier;

        [Key(1)]
        public readonly long TimeoutMillis;

        [SerializationConstructor]
        public PersistentStateChangeEvent(string stateIdentifier, long timeoutMillis)
        {
            StateIdentifier = stateIdentifier;
            TimeoutMillis = timeoutMillis;
        }
    }

    [MessagePackObject]
    public readonly struct PersistentFSMSnapshot
    {
        [Key(0)]
        public readonly string StateIdentifier;

        [Key(1)]
        public readonly PersistentPayload Data;

        [Key(2)]
        public readonly long TimeoutMillis;

        [SerializationConstructor]
        public PersistentFSMSnapshot(string stateIdentifier, in PersistentPayload data, long timeoutMillis)
        {
            StateIdentifier = stateIdentifier;
            Data = data;
            TimeoutMillis = timeoutMillis;
        }
    }
}
