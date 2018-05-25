//-----------------------------------------------------------------------
// <copyright file="AkkaResolver.cs" company="Akka.NET Project">
//     Copyright (C) 2017 Akka.NET Contrib <https://github.com/AkkaNetContrib/Akka.Serialization>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Resolvers
{
    internal sealed class AkkaResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new AkkaResolver();
        private AkkaResolver() { }
        public IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;

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
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter)) return formatter;

            if (typeof(IActorRef).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(ActorRefFormatter<>).GetCachedGenericType(t));
            }

            if (typeof(ISingletonMessage).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
            {
                return ActivatorUtils.FastCreateInstance(typeof(SingletonMessageFormatter<>).GetCachedGenericType(t));
            }

            return null;
        }
    }

    #region == ActorRefFormatter ==

    // IActorRef
    internal class ActorRefFormatter<T> : IMessagePackFormatter<T> where T : IActorRef
    {
        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            return MessagePackBinary.WriteString(ref bytes, offset, Serialization.SerializedActorPath(value));
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return default;
            }

            var path = MessagePackBinary.ReadString(bytes, offset, out readSize);

            var system = MsgPackSerializerHelper.LocalSystem.Value;
            if (system == null) { return default; }

            return (T)system.Provider.ResolveActorRef(path);
        }
    }

    #endregion

    #region == ActorPathFormatter ==

    // ActorPath
    internal class ActorPathFormatter<T> : IMessagePackFormatter<T> where T : ActorPath
    {
        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            return MessagePackBinary.WriteString(ref bytes, offset, value.ToSerializationFormat());
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var path = MessagePackBinary.ReadString(bytes, offset, out readSize);

            return ActorPath.TryParse(path, out var actorPath) ? (T)actorPath : null;
        }
    }

    #endregion

    #region == SingletonMessageFormatter ==

    internal class SingletonMessageFormatter<T> : IMessagePackFormatter<T> where T : class
    {
        const int c_totalSize = 3;
        const byte c_valueSize = 1;
        const byte c_empty = 1;
        private static readonly T s_instance;

        static SingletonMessageFormatter()
        {
            var thisType = typeof(T);
            var field = thisType.GetField("_instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = field.GetValue(null) as T; }
            if (s_instance != null) { return; }

            field = thisType.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = field.GetValue(null) as T; }
            if (s_instance != null) { return; }

            field = thisType.GetField("_singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = field.GetValue(null) as T; }
            if (s_instance != null) { return; }

            field = thisType.GetField("Singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = field.GetValue(null) as T; }
            if (s_instance != null) { return; }

            var property = thisType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (property != null) { s_instance = property.GetValue(null) as T; }
            if (s_instance != null) { return; }

            property = thisType.GetProperty("Singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (property != null) { s_instance = property.GetValue(null) as T; }
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            readSize = c_totalSize;
            return s_instance;
        }

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            MessagePackBinary.EnsureCapacity(ref bytes, offset, c_totalSize);

            bytes[offset] = MessagePackCode.Bin8;
            bytes[offset + 1] = c_valueSize;
            bytes[offset + 2] = c_empty;

            return c_totalSize;
        }
    }

    #endregion
}
