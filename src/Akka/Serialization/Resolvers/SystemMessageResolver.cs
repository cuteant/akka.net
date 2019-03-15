using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Serialization.Formatters;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    internal sealed class SystemMessageResolver : FormatterResolver
    {
        public static IFormatterResolver Instance = new SystemMessageResolver();
        private SystemMessageResolver() { }
        public override IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static IMessagePackFormatter<T> Formatter { get; }
            static FormatterCache() => Formatter = (IMessagePackFormatter<T>)SystemMessageFormatterHelper.GetFormatter(typeof(T));
        }
    }

    internal static class SystemMessageFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>
        {
            { typeof(ActorInitializationException), ActorInitializationExceptionFormatter.Instance },
            { typeof(Create), SystemMsgCreateFormatter.Instance },
            { typeof(Recreate), SystemMsgRecreateFormatter.Instance },
            { typeof(Suspend), SystemMsgSuspendFormatter.Instance },
            { typeof(Resume), SystemMsgResumeFormatter.Instance },
            { typeof(Terminate), SystemMsgTerminateFormatter.Instance },
            { typeof(Supervise), SystemMsgSuperviseFormatter.Instance },
            { typeof(Watch), SystemMsgWatchFormatter.Instance },
            { typeof(Unwatch), SystemMsgUnwatchFormatter.Instance },
            { typeof(Failed), SystemMsgFailedFormatter.Instance },
            { typeof(DeathWatchNotification), SystemMsgDeathWatchNotificationFormatter.Instance },
            { typeof(NoMessage), SystemMsgNoMessageFormatter.Instance },
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            if (typeof(ActorInitializationException).IsAssignableFrom(t))
            {
                return ActivatorUtils.FastCreateInstance(typeof(ActorInitializationExceptionFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }
}
