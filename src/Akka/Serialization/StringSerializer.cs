namespace Akka.Serialization
{
    using System;
    using System.Buffers;
    using System.Runtime.InteropServices;
    using Akka.Actor;
    using CuteAnt;
    using CuteAnt.Buffers;
    using MessagePack;
    using MessagePack.Internal;

    public sealed class StringSerializer : Serializer
    {
        private static readonly ArrayPool<byte> s_bufferPool = BufferManager.Shared;

        /// <summary>Initializes a new instance of the <see cref="PrimitiveSerializers" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public StringSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override int Identifier => 110;

        /// <inheritdoc />
        public override object DeepCopy(object source) => source;

        public override object FromBinary(byte[] bytes, Type type) => EncodingUtils.ToString(bytes);

        public override byte[] ToBinary(object obj)
        {
            var str = (string)obj;
            var maxSize = EncodingUtils.Utf8MaxBytes(str);
            if (0u >= (uint)maxSize) { return EmptyArray<byte>.Instance; }

            var buffer = s_bufferPool.Rent(maxSize);
            try
            {
                var utf16Source = MemoryMarshal.AsBytes(str.AsSpan());
                EncodingUtils.ToUtf8(ref MemoryMarshal.GetReference(utf16Source), utf16Source.Length,
                    ref buffer[0], maxSize, out _, out int written);
                var utf8Bytes = new byte[written];
                MessagePackBinary.CopyMemory(buffer, 0, utf8Bytes, 0, written);
                return utf8Bytes;
            }
            catch (Exception ex)
            {
                s_bufferPool.Return(buffer);
                throw ex;
            }
        }
    }
}
