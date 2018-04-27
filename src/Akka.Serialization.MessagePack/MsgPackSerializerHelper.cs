using System.Threading;
using Akka.Actor;
using CuteAnt.Extensions.Serialization;
using MessagePack;

namespace Akka.Serialization.MessagePack.Resolvers
{
    internal sealed class MsgPackSerializerHelper
    {
        private const int Locked = 1;
        private const int Unlocked = 0;

        private static int _registered = Unlocked;

        public static void Register()
        {
            if (Interlocked.CompareExchange(ref _registered, Locked, Unlocked) == Locked) { return; }

            MessagePackStandardResolver.Register(
#if SERIALIZATION
                SerializableResolver.Instance,
#endif
                //HyperionExceptionResolver.Instance,

                HyperionExpressionResolver.Instance,

                HyperionResolver.Instance,

                AkkaResolver.Instance);
        }

#if NET451
        internal static readonly CuteAnt.AsyncLocalShim<ActorSystem> LocalSystem = new CuteAnt.AsyncLocalShim<ActorSystem>();
#else
        internal static readonly AsyncLocal<ActorSystem> LocalSystem = new AsyncLocal<ActorSystem>();
#endif
    }
}
