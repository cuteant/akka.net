using System;
using System.Collections.Concurrent;
using System.Linq;
using Akka.Actor;
using Akka.Util;
using CuteAnt.Reflection;
using MessagePack;
using Microsoft.Extensions.Logging;
using SpanJson;
using SpanJson.Formatters;
using SpanJson.Resolvers;
using SpanJson.Linq;

namespace Akka.Serialization
{
    public class SerializationInitializer : ISerializationInitializer
    {
        private static readonly ILogger s_logger;
        private static readonly ConcurrentHashSet<Type> s_resolverFactoryTypes;
        private static readonly DeserializeDynamicDelegate<byte> s_utf8DynamicDelegate;

        static SerializationInitializer()
        {
            s_logger = TraceLogger.GetLogger<SerializationInitializer>();
            s_resolverFactoryTypes = new ConcurrentHashSet<Type>();
            s_utf8DynamicDelegate = ReadUtf8Dynamic;

            StandardResolvers.GetResolver<byte, ExcludeNullsCamelCaseResolver<byte>>().DynamicDeserializer = s_utf8DynamicDelegate;

            ExcludeNullsCamelCaseResolver<byte>.RegisterGlobalCustomFormatter<byte[], Base64StringFormatter>();
            ExcludeNullsCamelCaseResolver<byte>.TryRegisterGlobalCustomrResolver(JsonDynamicResolver.Instance);
        }

        private static object ReadUtf8Dynamic(ref JsonReader<byte> reader)
        {
            return JToken.Load(ref reader);
        }

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
