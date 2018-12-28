using Akka.Actor;

namespace Akka.Serialization
{
    public interface ISerializationInitializer
    {
        void InitActorSystem(ExtendedActorSystem system);
    }
}
