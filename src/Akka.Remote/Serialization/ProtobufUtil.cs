using System;
using System.Reflection;
using Akka.Actor;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Serialization
{
    public static class ProtobufUtil
    {
        private const string c_byteStringUnsafeTypeName = "Google.Protobuf.ByteString+Unsafe, Google.Protobuf";
        private const string c_getBufferMethodName = "GetBuffer";

        internal static readonly Type ByteStringUnsafeType;
        private static readonly MethodInfo s_getBufferMethodInfo;
        private static readonly MethodCaller<object, byte[]> s_getBufferMethodCaller;

        private const string c_attachBytesMethodName = "AttachBytes";
        private static readonly MethodInfo s_attachBytesMethodInfo;
        private static readonly MethodCaller<object, ByteString> s_attachBytesMethodCaller;

        static ProtobufUtil()
        {
            ByteStringUnsafeType = TypeUtils.ResolveType(c_byteStringUnsafeTypeName);
            s_getBufferMethodInfo = ByteStringUnsafeType.GetMethod(c_getBufferMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            s_getBufferMethodCaller = s_getBufferMethodInfo.MakeDelegateForCall<object, byte[]>();

            s_attachBytesMethodInfo = typeof(ByteString).GetMethod(c_attachBytesMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            s_attachBytesMethodCaller = s_attachBytesMethodInfo.MakeDelegateForCall<object, ByteString>();
        }

        /// <summary>Constructs a new ByteString from the given byte array. The array is
        /// *not* copied, and must not be modified after this constructor is called.</summary>
        public static ByteString FromBytes(byte[] bytes) => s_attachBytesMethodCaller(null, new[] { bytes });

        /// <summary>Provides direct, unrestricted access to the bytes contained in this instance.
        /// You must not modify or resize the byte array returned by this method.</summary>
        public static byte[] GetBuffer(ByteString bytes) => s_getBufferMethodCaller(null, new[] { bytes });

        /// <summary>Serializes the given object into a <see cref="ByteString"/>.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="obj">The object to serialize </param>
        /// <returns>A <see cref="ByteString"/> containing the serialized object</returns>
        public static ByteString ToByteString(this Serializer serializer, object obj)
        {
            var bytes = serializer.ToBinary(obj);
            return s_attachBytesMethodCaller(null, new[] { bytes });
        }

        /// <summary>Serializes the given object into a <see cref="ByteString"/> and uses the given address to decorate serialized ActorRef's.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="address">The address to use when serializing local ActorRef´s</param>
        /// <param name="obj">The object to serialize</param>
        /// <returns>TBD</returns>
        public static ByteString ToByteStringWithAddress(this Serializer serializer, Address address, object obj)
        {
            var bytes = serializer.ToBinaryWithAddress(address, obj);
            return s_attachBytesMethodCaller(null, new[] { bytes });
        }

        /// <summary>Deserializes a <see cref="ByteString"/> into an object of type <paramref name="type"/>.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="byteString">The <see cref="ByteString"/> containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public static object FromByteString(this Serializer serializer, ByteString byteString, Type type)
        {
            var bytes = s_getBufferMethodCaller(null, new[] { byteString });
            return serializer.FromBinary(bytes, type);
        }

        /// <summary>Deserializes a <see cref="ByteString"/> into an object.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="bytes">The <see cref="ByteString"/> containing the serialized object</param>
        /// <returns>The object contained in the <see cref="ByteString"/>.</returns>
        public static T FromByteString<T>(this Serializer serializer, ByteString bytes) => (T)FromByteString(serializer, bytes, typeof(T));
    }
}
