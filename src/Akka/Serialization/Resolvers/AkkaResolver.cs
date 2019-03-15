using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;
using Akka.Serialization.Formatters;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Akka.Serialization.Resolvers
{
    #region -- AkkaDefaultResolver --

    public sealed class AkkaDefaultResolver : DefaultResolver, IFormatterResolverContext<ExtendedActorSystem>, IFormatterResolverContext<Hyperion.Serializer>
    {
        private readonly ExtendedActorSystem _system;
        private readonly Hyperion.Serializer _serializer;

        public AkkaDefaultResolver(ExtendedActorSystem system, Hyperion.Serializer serializer)
            : base()
        {
            _system = system;
            _serializer = serializer;
        }

        ExtendedActorSystem IFormatterResolverContext<ExtendedActorSystem>.Value => _system;

        Hyperion.Serializer IFormatterResolverContext<Hyperion.Serializer>.Value => _serializer;
    }

    #endregion

    #region -- AkkaTypelessResolver --

    public sealed class AkkaTypelessResolver : TypelessDefaultResolver, IFormatterResolverContext<ExtendedActorSystem>, IFormatterResolverContext<Hyperion.Serializer>
    {
        private readonly ExtendedActorSystem _system;
        private readonly Hyperion.Serializer _serializer;

        public AkkaTypelessResolver(ExtendedActorSystem system, Hyperion.Serializer serializer)
            : base()
        {
            _system = system;
            _serializer = serializer;
        }

        ExtendedActorSystem IFormatterResolverContext<ExtendedActorSystem>.Value => _system;

        Hyperion.Serializer IFormatterResolverContext<Hyperion.Serializer>.Value => _serializer;
    }

    #endregion

    #region == AkkaResolverCore ==

    internal sealed class AkkaResolverCore : FormatterResolver
    {
        public static IFormatterResolver Instance = new AkkaResolverCore();
        private AkkaResolverCore() { }
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
            { typeof(ActorPath), new ActorPathFormatter<ActorPath>() },
            { typeof(ChildActorPath), new ActorPathFormatter<ChildActorPath>() },
            { typeof(RootActorPath), new ActorPathFormatter<RootActorPath>() },
            { typeof(IActorRef), new ActorRefFormatter<IActorRef>() },
            { typeof(IInternalActorRef), new ActorRefFormatter<IInternalActorRef>() },
            { typeof(RepointableActorRef), new ActorRefFormatter<RepointableActorRef>() },
            { typeof(ActorSelectionMessage), ActorSelectionMessageFormatter.Instance },
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            if (typeof(ISingletonMessage).IsAssignableFrom(t))
            {
                return ActivatorUtils.FastCreateInstance(typeof(SingletonMessageFormatter<>).GetCachedGenericType(t));
            }

            if (typeof(ActorPath).IsAssignableFrom(t))
            {
                return ActivatorUtils.FastCreateInstance(typeof(ActorPathFormatter<>).MakeGenericType(t));
            }

            if (typeof(IActorRef).IsAssignableFrom(t))
            {
                return ActivatorUtils.FastCreateInstance(typeof(ActorRefFormatter<>).MakeGenericType(t));
            }

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
