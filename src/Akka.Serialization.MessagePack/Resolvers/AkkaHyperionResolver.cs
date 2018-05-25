using System;
using System.Linq.Expressions;
using System.Reflection;
using Akka.Util;
using CuteAnt.Reflection;
using Hyperion;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    #region == AkkaHyperionExceptionFormatter ==

    internal class AkkaHyperionExceptionFormatter<TException> : HyperionExceptionFormatter<TException>
        where TException : Exception
    {
        public AkkaHyperionExceptionFormatter() : base(
            new SerializerOptions(
                versionTolerance: false,
                preserveObjectReferences: true,
                surrogates: new[] { Surrogate
                    .Create<ISurrogated, ISurrogate>(
                    from => from.ToSurrogate(MsgPackSerializerHelper.LocalSystem.Value),
                    to => to.FromSurrogate(MsgPackSerializerHelper.LocalSystem.Value)) }))
        { }
    }

    #endregion

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


    #region == AkkaHyperionExpressionFormatter ==

    internal class AkkaHyperionExpressionFormatter<TExpression> : HyperionExpressionFormatter<TExpression>
        where TExpression : Expression
    {
        public AkkaHyperionExpressionFormatter() : base(
            new SerializerOptions(
                versionTolerance: false,
                preserveObjectReferences: true,
                surrogates: new[] { Surrogate
                    .Create<ISurrogated, ISurrogate>(
                    from => from.ToSurrogate(MsgPackSerializerHelper.LocalSystem.Value),
                    to => to.FromSurrogate(MsgPackSerializerHelper.LocalSystem.Value)) }))
        { }
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


    #region == AkkaHyperionFormatter ==

    internal class AkkaHyperionFormatter<T> : HyperionFormatterBase<T>
    {
        public AkkaHyperionFormatter() : base(
            new SerializerOptions(
                versionTolerance: false,
                preserveObjectReferences: true,
                surrogates: new[] { Surrogate
                    .Create<ISurrogated, ISurrogate>(
                    from => from.ToSurrogate(MsgPackSerializerHelper.LocalSystem.Value),
                    to => to.FromSurrogate(MsgPackSerializerHelper.LocalSystem.Value)) }))
        { }
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
        internal static object GetFormatter(Type t)
        {
            if (typeof(IObjectReferences).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(AkkaHyperionFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }

    #endregion
}
