using System.Collections.Generic;
using Akka.Remote.Serialization.Formatters;
using Akka.Remote.Serialization.Resolvers;
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
                ActorSelectionMessageFormatter.Instance,

                ByteStringFormatter.Instance,

                DaemonMsgCreateFormatter.Instance,
            };
        }

        public IList<IFormatterResolver> GetResolvers()
        {
            return new List<IFormatterResolver>()
            {
                SystemMessageResolver.Instance,
                MiscMessageResolver.Instance,
            };
        }
    }
}
