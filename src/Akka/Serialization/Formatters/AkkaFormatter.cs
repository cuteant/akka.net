using System;
using System.Reflection;
using Akka.Actor;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
{
    #region == ActorRefFormatter ==

    // IActorRef
    internal class ActorRefFormatter<T> : IMessagePackFormatter<T> where T : IActorRef
    {
        private const string c_nobody = "nobody";

        public void Serialize(ref MessagePackWriter writer, ref int idx, T value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var encodedPath = MessagePackBinary.GetEncodedStringBytes(value is Nobody ? c_nobody : Serialization.SerializedActorPath(value));
            writer.WriteRawBytes(encodedPath, ref idx);
        }

        public T Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var path = reader.ReadStringWithCache();
            if (null == path) { return default; }

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
        public void Serialize(ref MessagePackWriter writer, ref int idx, T value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var encodedPath = MessagePackBinary.GetEncodedStringBytes(value.ToSerializationFormat());
            writer.WriteRawBytes(encodedPath, ref idx);
        }

        public T Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var path = reader.ReadStringWithCache();
            if (null == path) { return null; }

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

        public T Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            reader.Advance(1);
            return s_instance;
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, T value, IFormatterResolver formatterResolver)
        {
            writer.WriteNil(ref idx);
        }
    }

    #endregion

    #region -- AkkaTypelessFormatter --

    public sealed class AkkaTypelessFormatter : TypelessFormatter
    {
        public new static readonly IMessagePackFormatter<object> Instance = new AkkaTypelessFormatter();

        protected override Type TranslateTypeName(Type actualType)
        {
            if (typeof(IActorRef).IsAssignableFrom(actualType))
            {
                return typeof(IActorRef);
            }
            return actualType;
        }
    }

    #endregion
}
