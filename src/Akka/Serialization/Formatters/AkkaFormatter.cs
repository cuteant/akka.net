using System;
using System.Reflection;
using Akka.Actor;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
{
    #region == ActorRefFormatter ==

    // IActorRef
    internal class ActorRefFormatter<T> : IMessagePackFormatter<T> where T : IActorRef
    {
        private const string c_nobody = "nobody";

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            return MessagePackBinary.WriteString(ref bytes, offset, value is Nobody ? c_nobody : Serialization.SerializedActorPath(value));
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var path = MessagePackBinary.ReadString(bytes, offset, out readSize);

            if (string.Equals(path, c_nobody, StringComparison.Ordinal))
            {
                return (T)(object)Nobody.Instance;
            }

            var system = formatterResolver.GetActorSystem();
            //if (system == null) { return default; }

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
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            return MessagePackBinary.WriteString(ref bytes, offset, value.ToSerializationFormat());
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return null; }

            var path = MessagePackBinary.ReadString(bytes, offset, out readSize);

            return ActorPath.TryParse(path, out var actorPath) ? (T)actorPath : null;
        }
    }

    #endregion

    #region == SingletonMessageFormatter ==

    internal class SingletonMessageFormatter<T> : IMessagePackFormatter<T>
    {
        const byte c_valueSize = 1;
        private static readonly T s_instance;

        static SingletonMessageFormatter()
        {
            var thisType = typeof(T);
            var field = thisType.GetField("_instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = (T)field.GetValue(null); }
            if (s_instance != null) { return; }

            field = thisType.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = (T)field.GetValue(null); }
            if (s_instance != null) { return; }

            field = thisType.GetField("_singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = (T)field.GetValue(null); }
            if (s_instance != null) { return; }

            field = thisType.GetField("Singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field != null) { s_instance = (T)field.GetValue(null); }
            if (s_instance != null) { return; }

            var property = thisType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (property != null) { s_instance = (T)property.GetValue(null); }
            if (s_instance != null) { return; }

            property = thisType.GetProperty("Singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (property != null) { s_instance = (T)property.GetValue(null); }
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            readSize = c_valueSize;
            return s_instance;
        }

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }
    }

    #endregion

    #region -- AkkaTypelessFormatter --

    public sealed class AkkaTypelessFormatter : TypelessFormatter
    {
        public new static readonly IMessagePackFormatter<object> Instance = new AkkaTypelessFormatter();

        protected override byte[] TranslateTypeName(Type actualType, out Type expectedType)
        {
            if (typeof(IActorRef).GetTypeInfo().IsAssignableFrom(actualType.GetTypeInfo()))
            {
                expectedType = typeof(IActorRef);
            }
            else
            {
                expectedType = actualType;
            }
            return TypeSerializer.GetTypeKeyFromType(expectedType).TypeName;
        }
    }

    #endregion
}
