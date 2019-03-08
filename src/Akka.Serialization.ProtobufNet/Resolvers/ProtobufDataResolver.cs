using System;
using System.Collections.Generic;
using System.Data;
using Akka.Serialization.Formatters;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    internal sealed class ProtobufDataResolver : FormatterResolver
    {
        public static IFormatterResolver Instance = new ProtobufDataResolver();
        private ProtobufDataResolver() { }
        public override IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static IMessagePackFormatter<T> Formatter { get; }
            static FormatterCache() => Formatter = (IMessagePackFormatter<T>)ProtobufDataResolverHelper.GetFormatter(typeof(T));
        }
    }

    internal static class ProtobufDataResolverHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>
        {
            { typeof(DataSet), DataSetFormatter.Instance },
            { typeof(DataTable), DataTableFormatter.Instance },
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            return null;
        }
    }
}
