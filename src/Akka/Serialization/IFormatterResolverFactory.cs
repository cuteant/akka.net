using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization
{
    public interface IFormatterResolverFactory
    {
        IList<IMessagePackFormatter> GetFormatters();

        IList<IFormatterResolver> GetResolvers();
    }
}
