using System.Collections.Generic;
using Akka.Serialization.Formatters;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization
{
    public sealed class ProtobufResolverFactory : IFormatterResolverFactory
    {
        public IList<IMessagePackFormatter> GetFormatters()
        {
            return new List<IMessagePackFormatter>()
            {
                ByteStringFormatter.Instance,
            };
        }

        public IList<IFormatterResolver> GetResolvers() => new List<IFormatterResolver>();
    }
}
