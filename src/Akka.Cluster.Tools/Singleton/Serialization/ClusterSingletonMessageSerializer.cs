//-----------------------------------------------------------------------
// <copyright file="ClusterSingletonMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Serialization;
using CuteAnt;
using CuteAnt.Text;

namespace Akka.Cluster.Tools.Singleton.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterSingletonMessageSerializer : SerializerWithStringManifest
    {
        #region manifest

        private const string HandOverToMeManifest = "A";
        private static readonly byte[] HandOverToMeManifestBytes;
        private const string HandOverInProgressManifest = "B";
        private static readonly byte[] HandOverInProgressManifestBytes;
        private const string HandOverDoneManifest = "C";
        private static readonly byte[] HandOverDoneManifestBytes;
        private const string TakeOverFromMeManifest = "D";
        private static readonly byte[] TakeOverFromMeManifestBytes;

        private static readonly Dictionary<Type, string> ManifestMap;

        static ClusterSingletonMessageSerializer()
        {
            HandOverToMeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HandOverToMeManifest);
            HandOverInProgressManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HandOverInProgressManifest);
            HandOverDoneManifestBytes = StringHelper.UTF8NoBOM.GetBytes(HandOverDoneManifest);
            TakeOverFromMeManifestBytes = StringHelper.UTF8NoBOM.GetBytes(TakeOverFromMeManifest);

            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(HandOverToMe), HandOverToMeManifest},
                { typeof(HandOverInProgress), HandOverInProgressManifest},
                { typeof(HandOverDone), HandOverDoneManifest},
                { typeof(TakeOverFromMe), TakeOverFromMeManifest},
            };
        }

        #endregion

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterSingletonMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public ClusterSingletonMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <exception cref="System.ArgumentException">
        /// This exception is thrown when the specified <paramref name="obj"/> is of an unknown type.
        /// Acceptable values include: <see cref="HandOverToMe"/> | <see cref="HandOverInProgress"/> | <see cref="HandOverDone"/> | <see cref="TakeOverFromMe"/>
        /// </exception>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case HandOverToMe _:
                case HandOverInProgress _:
                case HandOverDone _:
                case TakeOverFromMe _:
                    return EmptyBytes;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterSingletonMessage<byte[]>(obj);
            }
        }

        /// <summary>
        /// Deserializes a byte array into an object using an optional <paramref name="manifest" /> (type hint).
        /// </summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="manifest">The type hint used to deserialize the object contained in the array.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="bytes"/>cannot be deserialized using the specified <paramref name="manifest"/>.
        /// </exception>
        /// <returns>The object contained in the array</returns>
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case HandOverToMeManifest:
                    return HandOverToMe.Instance;
                case HandOverInProgressManifest:
                    return HandOverInProgress.Instance;
                case HandOverDoneManifest:
                    return HandOverDone.Instance;
                case TakeOverFromMeManifest:
                    return TakeOverFromMe.Instance;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterSingletonMessage(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return string.Empty; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            return ThrowHelper.ThrowArgumentException_Manifest_ClusterClientMessage<string>(type);
        }

        /// <summary>
        /// Returns the manifest (type hint) that will be provided in the <see cref="FromBinary(System.Byte[],System.String)" /> method.
        /// <note>
        /// This method returns <see cref="String.Empty" /> if a manifest is not needed.
        /// </note>
        /// </summary>
        /// <param name="o">The object for which the manifest is needed.</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="o"/> does not have an associated manifest.
        /// </exception>
        /// <returns>The manifest needed for the deserialization of the specified <paramref name="o" />.</returns>
        public override string Manifest(object o)
        {
            switch (o)
            {
                case HandOverToMe _:
                    return HandOverToMeManifest;
                case HandOverInProgress _:
                    return HandOverInProgressManifest;
                case HandOverDone _:
                    return HandOverDoneManifest;
                case TakeOverFromMe _:
                    return TakeOverFromMeManifest;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterSingletonMessage<string>(o);
            }
        }
        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out byte[] manifest)
        {
            switch (obj)
            {
                case HandOverToMe _:
                    manifest = HandOverToMeManifestBytes;
                    return EmptyBytes;
                case HandOverInProgress _:
                    manifest = HandOverInProgressManifestBytes;
                    return EmptyBytes;
                case HandOverDone _:
                    manifest = HandOverDoneManifestBytes;
                    return EmptyBytes;
                case TakeOverFromMe _:
                    manifest = TakeOverFromMeManifestBytes;
                    return EmptyBytes;
                default:
                    manifest = null; return ThrowHelper.ThrowArgumentException_Serializer_ClusterSingletonMessage<byte[]>(obj);
            }
        }
    }
}
