using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Actor;
using Akka.Serialization.Formatters;
using Akka.Serialization.Resolvers;
using CuteAnt;
using MessagePack;
using MessagePack.Resolvers;

namespace Akka.Serialization
{
    public static class MsgPackSerializerHelper
    {
        private const int Locked = 1;
        private const int Unlocked = 0;

        private static int _registered = Unlocked;

        static MsgPackSerializerHelper()
        {
            MessagePackStandardResolver.RegisterTypelessObjectResolver(AkkaTypelessObjectResolver.Instance);
            MessagePackSerializer.Typeless.RegisterTypelessFormatter(AkkaTypelessFormatter.Instance);
        }

        internal static void Register()
        {
            if (Interlocked.CompareExchange(ref _registered, Locked, Unlocked) == Locked) { return; }

            MessagePackStandardResolver.Register(
                AkkaResolver.Instance,

                HyperionExceptionResolver2.Instance,

                HyperionExpressionResolver.Instance,

                AkkaHyperionResolver.Instance
            );
        }

        internal const string ActorSystem = "ACTORSYSTEM";
        [MethodImpl(InlineMethod.Value)]
        public static ExtendedActorSystem GetActorSystem(this IFormatterResolver formatterResolver)
            => formatterResolver.GetContextValue<ExtendedActorSystem>(ActorSystem);
    }
}
