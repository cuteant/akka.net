using System;
using System.Collections.Concurrent;
using System.Linq;
using Akka.Actor;
using Akka.Util;
using CuteAnt.Reflection;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Akka.Serialization
{
    public class SerializationInitializer : ISerializationInitializer
    {
        static readonly ILogger s_logger = TraceLogger.GetLogger<SerializationInitializer>();
        static readonly ConcurrentHashSet<Type> s_resolverFactoryTypes = new ConcurrentHashSet<Type>();

        public SerializationInitializer()
        {
            // First Registration
            var defaultResolver = MsgPackSerializerHelper.DefaultResolver;
        }

        public virtual void InitActorSystem(ExtendedActorSystem system)
        {
            var resolversConfig = system.Settings.Config.GetConfig("akka.actor.serialization-resolver-factories").AsEnumerable().ToList();
            foreach (var kvp in resolversConfig)
            {
                var resolverTypeKey = kvp.Key;
                var resolverTypeName = kvp.Value.GetString();
                var resolverType = TypeUtil.ResolveType(resolverTypeName);

                if (s_resolverFactoryTypes.TryAdd(resolverType))
                {
                    if (s_logger.IsEnabled(LogLevel.Trace))
                    {
                        s_logger.LogTrace("Loading FormatterResolver factory for MessagePack: {0}", resolverType.FullName);
                    }
                    var resolverFactory = ActivatorUtils.FastCreateInstance<IFormatterResolverFactory>(resolverType);
                    var formatters = resolverFactory.GetFormatters();
                    if (!MessagePackStandardResolver.TryRegister(formatters.ToArray()))
                    {
                        if (s_logger.IsEnabled(LogLevel.Debug))
                        {
                            s_logger.LogDebug("Register must call on startup(before use GetFormatter<T>).: {0}", string.Join(",", formatters.Select(_ => _.GetType().FullName)));
                        }
                    }
                    var resolvers = resolverFactory.GetResolvers();
                    if (!MessagePackStandardResolver.TryRegister(resolvers.ToArray()))
                    {
                        if (s_logger.IsEnabled(LogLevel.Debug))
                        {
                            s_logger.LogDebug("Register must call on startup(before use GetFormatter<T>).: {0}", string.Join(",", formatters.Select(_ => _.GetType().FullName)));
                        }
                    }
                }
            }
        }
    }
}
