using System.Collections.Generic;
using Akka.Remote.Serialization.Formatters;
using Akka.Serialization;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization
{
    public sealed class RemoteFormatterResolverFactory : IFormatterResolverFactory
    {
        public IList<IMessagePackFormatter> GetFormatters()
        {
            return new List<IMessagePackFormatter>()
            {
                IdentifyFormatter.Instance,
                ActorIdentityFormatter.Instance,
                RemoteWatcherHeartbeatRspFormatter.Instance,
                AddressFormatter.Instance,
                RemoteScopeFormatter.Instance,
                ConfigFormatter.Instance,
                FromConfigFormatter.Instance,
                DefaultResizerFormatter.Instance,
                RoundRobinPoolFormatter.Instance,
                BroadcastPoolFormatter.Instance,
                RandomPoolFormatter.Instance,
                ScatterGatherFirstCompletedPoolFormatter.Instance,
                TailChoppingPoolFormatter.Instance,
                ConsistentHashingPoolFormatter.Instance,
                RemoteRouterConfigFormatter.Instance,

                ActorSelectionMessageFormatter.Instance,
            };
        }

        public IList<IFormatterResolver> GetResolvers()
        {
            return new List<IFormatterResolver>();
        }
    }
}
