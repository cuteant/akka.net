using System.Threading;
using Akka.Actor;
using Akka.Serialization.Formatters;
using Akka.Serialization.Resolvers;
using CuteAnt.Extensions.Serialization;
using MessagePack;

namespace Akka.Serialization
{
    internal sealed class MsgPackSerializerHelper
    {
        private const int Locked = 1;
        private const int Unlocked = 0;

        private static int _registered = Unlocked;

        static MsgPackSerializerHelper()
        {
            MessagePackStandardResolver.RegisterTypelessObjectResolver(AkkaTypelessObjectResolver.Instance);
            MessagePackSerializer.Typeless.RegisterTypelessFormatter(AkkaTypelessFormatter.Instance);
        }

        public static void Register()
        {
            if (Interlocked.CompareExchange(ref _registered, Locked, Unlocked) == Locked) { return; }

            MessagePackStandardResolver.Register(
                AkkaResolver.Instance,

                AkkaHyperionExceptionResolver.Instance,

                AkkaHyperionExpressionResolver.Instance,

                AkkaHyperionResolver.Instance
            );
        }

#if NET451
        internal static readonly CuteAnt.AsyncLocalShim<ExtendedActorSystem> LocalSystem = new CuteAnt.AsyncLocalShim<ExtendedActorSystem>();
#else
        internal static readonly AsyncLocal<ExtendedActorSystem> LocalSystem = new AsyncLocal<ExtendedActorSystem>();
#endif
    }
}
