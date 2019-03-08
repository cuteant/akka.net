using System.Collections.Generic;
using Akka.Serialization.Resolvers;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization
{
    public sealed class ProtobufNetResolverFactory : IFormatterResolverFactory
    {
        public IList<IMessagePackFormatter> GetFormatters() => new List<IMessagePackFormatter>();

        public IList<IFormatterResolver> GetResolvers()
        {
            return new List<IFormatterResolver>()
            {
                ProtobufDataResolver.Instance
            };
        }
    }
}
