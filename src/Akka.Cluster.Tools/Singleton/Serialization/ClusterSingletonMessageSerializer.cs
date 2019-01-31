﻿//-----------------------------------------------------------------------
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

namespace Akka.Cluster.Tools.Singleton.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterSingletonMessageSerializer : SerializerWithIntegerManifest
    {
        #region manifest

        private const int HandOverToMeManifest = 440;
        private const int HandOverInProgressManifest = 441;
        private const int HandOverDoneManifest = 442;
        private const int TakeOverFromMeManifest = 443;

        private static readonly Dictionary<Type, int> ManifestMap;

        static ClusterSingletonMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, int>
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out int manifest)
        {
            switch (obj)
            {
                case HandOverToMe _:
                    manifest = HandOverToMeManifest;
                    return EmptyBytes;
                case HandOverInProgress _:
                    manifest = HandOverInProgressManifest;
                    return EmptyBytes;
                case HandOverDone _:
                    manifest = HandOverDoneManifest;
                    return EmptyBytes;
                case TakeOverFromMe _:
                    manifest = TakeOverFromMeManifest;
                    return EmptyBytes;
                default:
                    manifest = 0; return ThrowHelper.ThrowArgumentException_Serializer_ClusterSingletonMessage<byte[]>(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, int manifest)
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
        protected override int GetManifest(Type type)
        {
            if (null == type) { return 0; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            return ThrowHelper.ThrowArgumentException_Manifest_ClusterClientMessage<int>(type);
        }

        /// <inheritdoc />
        public override int Manifest(object o)
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
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterSingletonMessage<int>(o);
            }
        }
    }
}
