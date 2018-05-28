using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Akka.Serialization.Formatters;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    #region == AkkaHyperionExceptionResolver ==

    internal sealed class AkkaHyperionExceptionResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new AkkaHyperionExceptionResolver();

        AkkaHyperionExceptionResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)AkkaHyperionExceptionGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class AkkaHyperionExceptionGetFormatterHelper
    {
        internal static object GetFormatter(Type t)
        {
            if (typeof(Exception).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(AkkaHyperionExceptionFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }

    #endregion

    #region == AkkaHyperionExpressionResolver ==

    internal sealed class AkkaHyperionExpressionResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new AkkaHyperionExpressionResolver();

        AkkaHyperionExpressionResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)AkkaHyperionExpressionGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class AkkaHyperionExpressionGetFormatterHelper
    {
        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();
            if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(Expression<>))
            {
                return ActivatorUtils.FastCreateInstance(typeof(AkkaHyperionExpressionFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }

    #endregion

    #region == AkkaHyperionResolver ==

    internal sealed class AkkaHyperionResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new AkkaHyperionResolver();

        AkkaHyperionResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
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
                return ActivatorUtils.FastCreateInstance(typeof(AkkaHyperionFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }

    #endregion
}
