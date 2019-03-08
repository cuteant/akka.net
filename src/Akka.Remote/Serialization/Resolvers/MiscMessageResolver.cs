using System;
using System.Collections.Generic;
using Akka.Remote.Routing;
using Akka.Remote.Serialization.Formatters;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Resolvers
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
            { typeof(RemoteWatcher.HeartbeatRsp), RemoteWatcherHeartbeatRspFormatter.Instance },
            { typeof(RemoteRouterConfig), RemoteRouterConfigFormatter.Instance },
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            return null;
        }
    }
}
