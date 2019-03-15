using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Akka.Actor;
using CuteAnt;
using CuteAnt.Buffers;
using CuteAnt.Text;

namespace Akka.Serialization
{
    public sealed class StringSerializer : Serializer
    {
        private static readonly Encoding s_encodingUtf8 = StringHelper.UTF8NoBOM;
        private static readonly Encoding s_decodingUtf8 = Encoding.UTF8;
        private static readonly ArrayPool<byte> s_bufferPool = BufferManager.Shared;

        /// <summary>Initializes a new instance of the <see cref="PrimitiveSerializers" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public StringSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override int Identifier => 110;

        /// <inheritdoc />
        public override object DeepCopy(object source) => source;

        public override object FromBinary(byte[] bytes, Type type)
        {
            return s_decodingUtf8.GetString(bytes);
        }

        public override byte[] ToBinary(object obj)
        {
            var str = (string)obj;
            var len = str.Length;
            if (0u >= (uint)len) { return EmptyArray<byte>.Instance; }

            var bufferSize = s_encodingUtf8.GetMaxByteCount(len);
            var buffer = s_bufferPool.Rent(bufferSize);
            try
            {
                var bytesCount = s_encodingUtf8.GetBytes(str, 0, len, buffer, 0);
                return CopyFrom(buffer, 0, bytesCount);
            }
            catch (Exception ex)
            {
                s_bufferPool.Return(buffer);
                throw ex;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private
#if !NET451
            unsafe
#endif
            static byte[] CopyFrom(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
#if NET451
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
#else

            fixed (byte* pSrc = &buffer[offset])
            fixed (byte* pDst = &bytes[0])
            {
                Buffer.MemoryCopy(pSrc, pDst, bytes.Length, count);
            }
#endif
            return bytes;
        }
    }
}
