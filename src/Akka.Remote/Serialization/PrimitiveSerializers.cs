//-----------------------------------------------------------------------
// <copyright file="PrimitiveSerializers.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt.Text;

namespace Akka.Remote.Serialization
{
    public sealed class PrimitiveSerializers : SerializerWithStringManifest
    {
        private static readonly Encoding s_encodingUtf8 = StringHelper.UTF8NoBOM;
        private static readonly Encoding s_decodingUtf8 = Encoding.UTF8;

        #region manifests

        private const string StringManifest = "S";
        private static readonly byte[] StringManifestBytes;
        private const string Int32Manifest = "I";
        private static readonly byte[] Int32ManifestBytes;
        private const string Int64Manifest = "L";
        private static readonly byte[] Int64ManifestBytes;
        private static readonly Dictionary<Type, string> ManifestMap;

        static PrimitiveSerializers()
        {
            StringManifestBytes = StringHelper.UTF8NoBOM.GetBytes(StringManifest);
            Int32ManifestBytes = StringHelper.UTF8NoBOM.GetBytes(Int32Manifest);
            Int64ManifestBytes = StringHelper.UTF8NoBOM.GetBytes(Int64Manifest);
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(string), StringManifest},
                { typeof(int), Int32Manifest},
                { typeof(long), Int64Manifest},
            };
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSerializers" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public PrimitiveSerializers(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return string.Empty; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            return ThrowHelper.ThrowArgumentException_Serializer_D<string>(type);
        }

        /// <inheritdoc />
        public override object DeepCopy(object source) => source;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                //case ByteString bytes:
                //    return ProtobufUtil.GetBuffer(bytes);
                case string str:
                    return s_encodingUtf8.GetBytes(str);
                case int intValue:
                    return BitConverter.GetBytes(intValue);
                case long longValue:
                    return BitConverter.GetBytes(longValue);
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_S(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case StringManifest:
                    return s_decodingUtf8.GetString(bytes);
                case Int32Manifest:
                    return BitConverter.ToInt32(bytes, 0);
                case Int64Manifest:
                    return BitConverter.ToInt64(bytes, 0);
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_Primitive(manifest);
            }
        }

        /// <inheritdoc />
        public override string Manifest(object o)
        {
            switch (o)
            {
                case string _:
                    return StringManifest;
                case int _:
                    return Int32Manifest;
                case long _:
                    return Int64Manifest;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_D<string>(o);
            }
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object o, out byte[] manifest)
        {
            switch (o)
            {
                case string str:
                    manifest = StringManifestBytes;
                    return s_encodingUtf8.GetBytes(str);
                case int intValue:
                    manifest = Int32ManifestBytes;
                    return BitConverter.GetBytes(intValue);
                case long longValue:
                    manifest = Int64ManifestBytes;
                    return BitConverter.GetBytes(longValue);
                default:
                    manifest = null; return ThrowHelper.ThrowArgumentException_Serializer_D<byte[]>(o);
            }
        }
    }
}
