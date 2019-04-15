//-----------------------------------------------------------------------
// <copyright file="ByteArraySerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using Akka.Actor;

namespace Akka.Serialization
{
    /// <summary>
    /// This is a special <see cref="Serializer"/> that serializes and deserializes byte arrays only
    /// (just returns the byte array unchanged/uncopied).
    /// </summary>
    public class ByteArraySerializer : Serializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArraySerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public ByteArraySerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override object DeepCopy(object source)
        {
            if (null == source) { return null; }
            var bts = source as byte[];
            if (null == bts) { AkkaThrowHelper.ThrowNotSupportedException(AkkaExceptionResource.NotSupported_IsNotByteArray); }
            return CopyFrom(bts, 0, bts.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private
#if NET471
            unsafe
#endif
            static byte[] CopyFrom(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
#if NET451
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
#elif NET471
            fixed (byte* pSrc = &buffer[offset])
            fixed (byte* pDst = &bytes[0])
            {
                Buffer.MemoryCopy(pSrc, pDst, bytes.Length, count);
            }
#else
            Unsafe.CopyBlockUnaligned(ref bytes[0], ref buffer[offset], unchecked((uint)count));
#endif
            return bytes;
        }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize </param>
        /// <exception cref="NotSupportedException">
        /// This exception is thrown if the given <paramref name="obj"/> is not a byte array.
        /// </exception>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj) => (byte[])obj;

        /// <summary>
        /// Deserializes a byte array into an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, Type type) => bytes;
    }
}
