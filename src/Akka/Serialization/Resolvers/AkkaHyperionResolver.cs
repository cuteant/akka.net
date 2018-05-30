using System;
using System.Collections.Generic;
using System.Reflection;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    internal sealed class AkkaHyperionResolver : FormatterResolver
    {
        public static readonly IFormatterResolver Instance = new AkkaHyperionResolver();

        AkkaHyperionResolver()
        {
        }

        public override IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)AkkaHyperionGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class AkkaHyperionGetFormatterHelper
    {
        private static readonly HashSet<Type> s_ignoreTypes = new HashSet<Type>()
        {
            typeof(Akka.Actor.Address)
        };
        internal static object GetFormatter(Type t)
        {
            if (s_ignoreTypes.Contains(t)) { return null; }

            if (typeof(IObjectReferences).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(SimpleHyperionFormatter2<>).GetCachedGenericType(t));
            }

            return null;
        }
    }
}
