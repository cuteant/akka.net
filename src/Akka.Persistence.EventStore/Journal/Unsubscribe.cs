using Akka.Actor;

namespace Akka.Persistence.EventStore.Journal
{
    public readonly struct Unsubscribe
    {
        public readonly string StreamId;
        public readonly IActorRef Subscriber;

        public Unsubscribe(string streamId, IActorRef subscriber)
        {
            StreamId = streamId;
            Subscriber = subscriber;
        }
    }
}