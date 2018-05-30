using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Formatters;
using Akka.Util;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    #region == AkkaResolver ==

    internal sealed class AkkaResolver : FormatterResolver
    {
        public static IFormatterResolver Instance = new AkkaResolver();
        private AkkaResolver() { }
        public override IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static IMessagePackFormatter<T> Formatter { get; }
            static FormatterCache() => Formatter = (IMessagePackFormatter<T>)AkkaResolverGetFormatterHelper.GetFormatter(typeof(T));
        }
    }

    internal static class AkkaResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>
        {
            {typeof(ActorPath), new ActorPathFormatter<ActorPath>()},
            {typeof(ChildActorPath), new ActorPathFormatter<ChildActorPath>()},
            {typeof(RootActorPath), new ActorPathFormatter<RootActorPath>()},
            {typeof(IActorRef), new ActorRefFormatter<IActorRef>()},
            {typeof(IInternalActorRef), new ActorRefFormatter<IInternalActorRef>()},
            {typeof(RepointableActorRef), new ActorRefFormatter<RepointableActorRef>()},
            //{typeof(Config), ActorConfigFormatter.Instnace},
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            //if (typeof(IInternalActorRef).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            //{
            //    return ActivatorUtils.FastCreateInstance(typeof(ActorRefFormatter<>).MakeGenericType(t));
            //}

            if (typeof(ISingletonMessage).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(SingletonMessageFormatter<>).GetCachedGenericType(t));
            }

            if (typeof(IActorRef).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(ActorRefFormatter<>).MakeGenericType(t));
            }

            //if (MsgPackSerializerHelper.UseRemotingSerializer)
            //{
            //    if (typeof(ISurrogated).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            //    {
            //        return ActivatorUtils.FastCreateInstance(typeof(WrappedPayloadFormatter<>).GetCachedGenericType(t));
            //    }
            //}

            return null;
        }
    }

    #endregion

    #region == AkkaTypelessObjectResolver ==

    internal sealed class AkkaTypelessObjectResolver : FormatterResolver
    {
        public static readonly IFormatterResolver Instance = new AkkaTypelessObjectResolver();

        AkkaTypelessObjectResolver()
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
                formatter = (typeof(T) == typeof(object))
                    ? (IMessagePackFormatter<T>)AkkaTypelessFormatter.Instance
                    : null;
            }
        }
    }

    #endregion
}
