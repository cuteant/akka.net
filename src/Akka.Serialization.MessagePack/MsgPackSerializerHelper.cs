using System.Threading;
using Akka.Actor;
using CuteAnt.Extensions.Serialization;

namespace Akka.Serialization.Resolvers
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
                AkkaResolver.Instance,

                AkkaHyperionExceptionResolver.Instance,

                AkkaHyperionExpressionResolver.Instance,

                AkkaHyperionResolver.Instance);
        }

#if NET451
        internal static readonly CuteAnt.AsyncLocalShim<ExtendedActorSystem> LocalSystem = new CuteAnt.AsyncLocalShim<ExtendedActorSystem>();
#else
        internal static readonly AsyncLocal<ExtendedActorSystem> LocalSystem = new AsyncLocal<ExtendedActorSystem>();
#endif
    }
}
