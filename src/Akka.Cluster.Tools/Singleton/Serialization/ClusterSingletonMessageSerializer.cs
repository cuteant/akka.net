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

namespace Akka.Cluster.Tools.Singleton.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ClusterSingletonMessageSerializer : SerializerWithStringManifest
    {
        #region manifest

        private const string HandOverToMeManifest = "A";
        private const string HandOverInProgressManifest = "B";
        private const string HandOverDoneManifest = "C";
        private const string TakeOverFromMeManifest = "D";

        private static readonly Dictionary<Type, string> ManifestMap;

        static ClusterSingletonMessageSerializer()
        {
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

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
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
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterSingletonMessage(obj);
            }
        }

        /// <inheritdoc />
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
                    throw ThrowHelper.GetSerializationException_Serializer_ClusterSingletonMessage(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (type is null) { return null; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            throw ThrowHelper.GetArgumentException_Manifest_ClusterClientMessage(type);
        }

        /// <inheritdoc />
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
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterSingletonMessage(o);
            }
        }
    }
}
