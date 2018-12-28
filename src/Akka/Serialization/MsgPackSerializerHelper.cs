using System.Runtime.CompilerServices;
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
        internal static IFormatterResolver DefaultResolver;

        static MsgPackSerializerHelper()
        {
            MessagePackStandardResolver.RegisterTypelessObjectResolver(AkkaTypelessObjectResolver.Instance);
            MessagePackSerializer.Typeless.RegisterTypelessFormatter(AkkaTypelessFormatter.Instance);

            MessagePackStandardResolver.Register(
                AkkaResolver.Instance,

                HyperionExceptionResolver2.Instance,

                HyperionExpressionResolver.Instance,

                AkkaHyperionResolver.Instance
            );
        }

        internal const int ActorSystemIdentifier = 1;
        [MethodImpl(InlineMethod.Value)]
        public static ExtendedActorSystem GetActorSystem(this IFormatterResolver formatterResolver)
            => formatterResolver.GetContextValue<ExtendedActorSystem>(ActorSystemIdentifier);
    }
}
