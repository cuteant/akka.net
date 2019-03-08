using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;
using Akka.Routing;
using Akka.Serialization.Formatters;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    internal sealed class MiscMessageResolver : FormatterResolver
    {
        public static IFormatterResolver Instance = new MiscMessageResolver();
        private MiscMessageResolver() { }
        public override IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static IMessagePackFormatter<T> Formatter { get; }
            static FormatterCache() => Formatter = (IMessagePackFormatter<T>)MiscMessageResolverHelper.GetFormatter(typeof(T));
        }
    }

    internal static class MiscMessageResolverHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>
        {
            { typeof(Identify), IdentifyFormatter.Instance },
            { typeof(ActorIdentity), ActorIdentityFormatter.Instance },
            { typeof(RemoteScope), RemoteScopeFormatter.Instance },
            //{ typeof(Config), ConfigFormatter.Instance },
            { typeof(FromConfig), FromConfigFormatter.Instance },
            { typeof(DefaultResizer), DefaultResizerFormatter.Instance },
            { typeof(RoundRobinPool), RoundRobinPoolFormatter.Instance },
            { typeof(BroadcastPool), BroadcastPoolFormatter.Instance },
            { typeof(RandomPool), RandomPoolFormatter.Instance },
            { typeof(ScatterGatherFirstCompletedPool), ScatterGatherFirstCompletedPoolFormatter.Instance },
            { typeof(TailChoppingPool), TailChoppingPoolFormatter.Instance },
            { typeof(ConsistentHashingPool), ConsistentHashingPoolFormatter.Instance },
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            if (typeof(ActorInitializationException).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(ActorInitializationExceptionFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }
}
