//-----------------------------------------------------------------------
// <copyright file="MiscMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Routing;
using Akka.Serialization;
using Akka.Serialization.Resolvers;
using CuteAnt;
using MessagePack;

namespace Akka.Remote.Serialization
{
    public sealed class MiscMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string RemoteWatcherHearthbeatManifest = "RWHB";
        private const string RemoteWatcherHearthbeatRspManifest = "RWHR";
        private const string RemoteRouterConfigManifest = "RORRC";

        private static readonly Dictionary<Type, string> ManifestMap;

        static MiscMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(RemoteWatcher.Heartbeat), RemoteWatcherHearthbeatManifest},
                { typeof(RemoteWatcher.HeartbeatRsp), RemoteWatcherHearthbeatRspManifest},
                { typeof(RemoteRouterConfig), RemoteRouterConfigManifest},
            };
        }

        #endregion

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        private readonly IFormatterResolver _defaultResolver;

        /// <summary>Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public MiscMessageSerializer(ExtendedActorSystem system)
            : this(system, HyperionSerializerSettings.Default) { }

        /// <summary>Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.</summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        /// <param name="config"></param>
        public MiscMessageSerializer(ExtendedActorSystem system, Config config)
            : this(system, HyperionSerializerSettings.Create(config)) { }

        /// <summary>Initializes a new instance of the <see cref="MiscMessageSerializer" /> class.</summary>
        public MiscMessageSerializer(ExtendedActorSystem system, HyperionSerializerSettings settings)
            : base(system)
        {
            var serializer = HyperionSerializerHelper.CreateSerializer(system, settings);
            _defaultResolver = new AkkaDefaultResolver(system, serializer);
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case RemoteWatcher.HeartbeatRsp heartbeatRsp:
                    manifest = RemoteWatcherHearthbeatRspManifest;
                    return MessagePackSerializer.Serialize(heartbeatRsp, _defaultResolver);

                case RemoteRouterConfig remoteRouterConfig:
                    manifest = RemoteRouterConfigManifest;
                    return MessagePackSerializer.Serialize(remoteRouterConfig, _defaultResolver);

                case RemoteWatcher.Heartbeat _:
                    manifest = RemoteWatcherHearthbeatManifest;
                    return EmptyBytes;

                default:
                    throw ThrowHelper.GetArgumentException_Serializer_D(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case RemoteWatcherHearthbeatManifest:
                    return RemoteWatcher.Heartbeat.Instance;
                case RemoteWatcherHearthbeatRspManifest:
                    return MessagePackSerializer.Deserialize<RemoteWatcher.HeartbeatRsp>(bytes, _defaultResolver);
                case RemoteRouterConfigManifest:
                    return MessagePackSerializer.Deserialize<RemoteRouterConfig>(bytes, _defaultResolver);

                default:
                    ThrowHelper.ThrowSerializationException_Serializer_MiscFrom(manifest); return null;
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            throw ThrowHelper.GetArgumentException_Serializer_D(type);
        }

        /// <inheritdoc />
        public override string Manifest(object obj)
        {
            switch (obj)
            {
                case RemoteWatcher.Heartbeat _:
                    return RemoteWatcherHearthbeatManifest;
                case RemoteWatcher.HeartbeatRsp _:
                    return RemoteWatcherHearthbeatRspManifest;
                case RemoteRouterConfig _:
                    return RemoteRouterConfigManifest;

                default:
                    throw ThrowHelper.GetArgumentException_Serializer_D(obj);
            }
        }
    }
}
