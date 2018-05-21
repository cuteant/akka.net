using System;
using System.Reflection;
using Akka.Actor;
using CuteAnt.Buffers;
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


        public static ByteBufferWrapper ToUnpooledByteBuffer(this IMessage message) => new ByteBufferWrapper(message.ToByteArray());

        public static ByteBufferWrapper ToUnpooledByteBuffer(this ByteString bytes) => new ByteBufferWrapper(GetBuffer(bytes));

        private const int c_initialBufferSize = 1024 * 64;
        /// <summary>Serializes the given message data.</summary>
        /// <param name="message">The message to write.</param>
        /// <param name="initialBufferSize">The initial buffer size.</param>
        /// <returns></returns>
        public static ByteBufferWrapper ToPooledByteBuffer(this IMessage message, int initialBufferSize = c_initialBufferSize)
        {
            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(initialBufferSize, BufferManager.Shared);
                message.WriteTo(outputStream);
                return new ByteBufferWrapper(outputStream.ToArraySegment());
            }
        }

        public
#if !NET451
            unsafe
#endif
            static ByteString ToByteString(this in ByteBufferWrapper byteBuffer)
        {
            if (byteBuffer.IsPooled)
            {
                var payload = byteBuffer.Payload;
                var bytes = new byte[payload.Count];
#if NET451
                Buffer.BlockCopy(payload.Array, payload.Offset, bytes, 0, payload.Count);
#else

                fixed (byte* pSrc = &payload.Array[payload.Offset])
                fixed (byte* pDst = &bytes[0])
                {
                    Buffer.MemoryCopy(pSrc, pDst, bytes.Length, payload.Count);
                }
#endif
                BufferManager.Shared.Return(payload.Array);
                return FromBytes(bytes);
            }
            else
            {
                return FromBytes(byteBuffer.Payload.Array);
            }
        }
    }

    public readonly struct ByteBufferWrapper
    {
        public readonly ArraySegment<byte> Payload;

        public readonly bool IsPooled;

        public readonly int Length;

        public ByteBufferWrapper(in ArraySegment<byte> payload)
        {
            Payload = payload;
            Length = payload.Count;
            IsPooled = true;
        }

        public ByteBufferWrapper(byte[] payload)
        {
            Payload = new ArraySegment<byte>(payload);
            Length = payload.Length;
            IsPooled = false;
        }
    }
}
