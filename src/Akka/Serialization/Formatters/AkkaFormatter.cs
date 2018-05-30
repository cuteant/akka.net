using System;
using System.Reflection;
using System.Text;
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

            if (value is Nobody) { return MessagePackBinary.WriteString(ref bytes, offset, c_nobody); }

            return MessagePackBinary.WriteString(ref bytes, offset, Serialization.SerializedActorPath(value));
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var path = MessagePackBinary.ReadString(bytes, offset, out readSize);

            if (string.Equals(c_nobody, path, StringComparison.Ordinal)) { return (T)(object)Nobody.Instance; }

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
        //const int c_totalSize = 3;
        const byte c_valueSize = 1;
        //const byte c_empty = 1;
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
            //readSize = c_totalSize;
            return s_instance;
        }

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
            //MessagePackBinary.EnsureCapacity(ref bytes, offset, c_totalSize);

            //bytes[offset] = MessagePackCode.Bin8;
            //bytes[offset + 1] = c_valueSize;
            //bytes[offset + 2] = c_empty;

            //return c_totalSize;
        }
    }

    #endregion

    #region -- AkkaTypelessFormatter --

    public sealed class AkkaTypelessFormatter : TypelessFormatter
    {
        public new static readonly IMessagePackFormatter<object> Instance = new AkkaTypelessFormatter();

        protected override byte[] TranslateTypeName(Type actualType, out Type expectedType)
        {
            //if (typeof(IInternalActorRef).GetTypeInfo().IsAssignableFrom(actualType.GetTypeInfo()))
            //{
            //    expectedType = typeof(IInternalActorRef);
            //}
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

    #region == ActorConfigFormatter ==

    //internal sealed class ActorConfigFormatter : IMessagePackFormatter<Config>
    //{
    //    internal static readonly ActorConfigFormatter Instnace = new ActorConfigFormatter();

    //    public int Serialize(ref byte[] bytes, int offset, Config value, IFormatterResolver formatterResolver)
    //    {
    //        if (value == null || value.IsEmpty) { return MessagePackBinary.WriteNil(ref bytes, offset); }

    //        return MessagePackBinary.WriteString(ref bytes, offset, value.Root.ToString());
    //    }

    //    public Config Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    //    {
    //        if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return Config.Empty; }

    //        var config = MessagePackBinary.ReadString(bytes, offset, out readSize);

    //        return ConfigurationFactory.ParseString(config);
    //    }
    //}

    #endregion

    #region -- WrappedPayloadFormatter --

    public sealed class WrappedPayloadFormatter : WrappedPayloadFormatter<object> { }

    public class WrappedPayloadFormatter<T> : IMessagePackFormatter<T>
    {
        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var startOffset = offset;

            var serializerId = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
            offset += readSize;
            var isSerializerWithStringManifest = MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
            offset += readSize;
            var hasManifest = MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
            offset += readSize;
            var manifest = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            offset += readSize;
            var hashCode = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
            offset += readSize;
            var message = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            offset += readSize;

            readSize = offset - startOffset;

            var system = formatterResolver.GetActorSystem();
            //if (system == null) { return default; }

            if (isSerializerWithStringManifest)
            {
                return (T)system.Serialization.Deserialize(message, serializerId, hasManifest ? Encoding.UTF8.GetString(manifest) : null);
            }
            else if (hasManifest)
            {
                return (T)system.Serialization.Deserialize(message, serializerId, manifest, hashCode);
            }
            else
            {
                return (T)system.Serialization.Deserialize(message, serializerId);
            }
        }

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var system = formatterResolver.GetActorSystem();
            //if (system == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var serializer = system.Serialization.FindSerializerFor(value);
            var isSerializerWithStringManifest = false;
            var hasManifest = false;
            byte[] manifest = null;
            var hashCode = 0;
            // get manifest
            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                isSerializerWithStringManifest = true;
                manifest = manifestSerializer.ManifestBytes(value);
                if (manifest != null) { hasManifest = true; }
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    hasManifest = true;
                    var typeKey = TypeSerializer.GetTypeKeyFromType(value.GetType());
                    hashCode = typeKey.HashCode;
                    manifest = typeKey.TypeName;
                }
            }

            var startOffset = offset;

            offset += MessagePackBinary.WriteInt32(ref bytes, offset, serializer.Identifier);
            offset += MessagePackBinary.WriteBoolean(ref bytes, offset, isSerializerWithStringManifest);
            offset += MessagePackBinary.WriteBoolean(ref bytes, offset, hasManifest);
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, manifest);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, hashCode);
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, serializer.ToBinary(value));

            return offset - startOffset;
        }
    }

    #endregion
}
