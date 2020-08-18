namespace Akka.Serialization
{
    using System;
    using System.Buffers;
    using Akka.Actor;
    using CuteAnt;
    using CuteAnt.Buffers;
    using SpanJson.Internal;

    public sealed class StringSerializer : Serializer
    {
        private const uint StackallocThreshold = 256u;
        private static readonly ArrayPool<byte> s_bufferPool = BufferManager.Shared;

        /// <summary>Initializes a new instance of the <see cref="PrimitiveSerializers" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public StringSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override int Identifier => 110;

        /// <inheritdoc />
        public override object DeepCopy(object source) => source;

        public override object FromBinary(byte[] bytes, Type type) => TextEncodings.Utf8.GetString(bytes);

        public override byte[] ToBinary(object obj)
        {
            var str = (string)obj;
            var bMaxLength = TextEncodings.Utf8.GetMaxByteCount(str.Length);
            if (0u >= (uint)bMaxLength) { return EmptyArray<byte>.Instance; }

            byte[] bBuffer = null;
            try
            {
                Span<byte> utf8Span = (uint)bMaxLength <= StackallocThreshold ?
                    stackalloc byte[bMaxLength] :
                    (bBuffer = s_bufferPool.Rent(bMaxLength));

                var written = TextEncodings.Utf8.GetBytes(str.AsSpan(), utf8Span);
                return utf8Span.Slice(0, written).ToArray();
            }
            finally
            {
                if (bBuffer is object) { s_bufferPool.Return(bBuffer); }
            }
        }
    }
}
