//-----------------------------------------------------------------------
// <copyright file="DistributedPubSubMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe.Internal;
using Akka.Serialization;
using MessagePack;
using AddressData = Akka.Serialization.Protocol.AddressData;
using Status = Akka.Cluster.Tools.PublishSubscribe.Internal.Status;

namespace Akka.Cluster.Tools.PublishSubscribe.Serialization
{
    /// <summary>
    /// Protobuf serializer of DistributedPubSubMediator messages.
    /// </summary>
    public class DistributedPubSubMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string StatusManifest = "A";
        private const string DeltaManifest = "B";
        private const string SendManifest = "C";
        private const string SendToAllManifest = "D";
        private const string PublishManifest = "E";
        private const string SendToOneSubscriberManifest = "F";

        private static readonly Dictionary<Type, string> ManifestMap;

        static DistributedPubSubMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                { typeof(Internal.Status), StatusManifest},
                { typeof(Internal.Delta), DeltaManifest},
                { typeof(Send), SendManifest},
                { typeof(SendToAll), SendToAllManifest},
                { typeof(Publish), PublishManifest},
                { typeof(SendToOneSubscriber), SendToOneSubscriberManifest},
            };
        }

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedPubSubMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public DistributedPubSubMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case Status status:
                    manifest = StatusManifest;
                    return StatusToProto(status);
                case Delta delta:
                    manifest = DeltaManifest;
                    return DeltaToProto(delta);
                case Send send:
                    manifest = SendManifest;
                    return SendToProto(send);
                case SendToAll sendToAll:
                    manifest = SendToAllManifest;
                    return SendToAllToProto(sendToAll);
                case Publish publish:
                    manifest = PublishManifest;
                    return PublishToProto(publish);
                case SendToOneSubscriber sub:
                    manifest = SendToOneSubscriberManifest;
                    return SendToOneSubscriberToProto(sub);
                default:
                    throw ThrowHelper.GetArgumentException_Manifest_DistributedPubSubMessage(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case StatusManifest:
                    return StatusFrom(bytes);
                case DeltaManifest:
                    return DeltaFrom(bytes);
                case SendManifest:
                    return SendFrom(bytes);
                case SendToAllManifest:
                    return SendToAllFrom(bytes);
                case PublishManifest:
                    return PublishFrom(bytes);
                case SendToOneSubscriberManifest:
                    return SendToOneSubscriberFrom(bytes);
                default:
                    throw ThrowHelper.GetSerializationException_Serializer_DistributedPubSubMessage(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            throw ThrowHelper.GetArgumentException_Manifest_ClusterClientMessage(type);
        }

        /// <inheritdoc />
        public override string Manifest(object o)
        {
            switch (o)
            {
                case Status _:
                    return StatusManifest;
                case Delta _:
                    return DeltaManifest;
                case Send _:
                    return SendManifest;
                case SendToAll _:
                    return SendToAllManifest;
                case Publish _:
                    return PublishManifest;
                case SendToOneSubscriber _:
                    return SendToOneSubscriberManifest;
                default:
                    throw ThrowHelper.GetArgumentException_Manifest_DistributedPubSubMessage(o);
            }
        }

        private static byte[] StatusToProto(Internal.Status status)
        {
            var protoVersions = status.Versions.Select(_ => new Protocol.Version(AddressToProto(_.Key), _.Value)).ToArray();
            var message = new Protocol.Status(protoVersions, status.IsReplyToStatus);

            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Internal.Status StatusFrom(in ReadOnlySpan<byte> bytes)
        {
            var statusProto = MessagePackSerializer.Deserialize<Protocol.Status>(bytes, s_defaultResolver);
            var versions = new Dictionary<Address, long>(AddressComparer.Instance);

            foreach (var protoVersion in statusProto.Versions)
            {
                versions.Add(AddressFrom(protoVersion.Address), protoVersion.Timestamp);
            }

            return new Internal.Status(versions, statusProto.ReplyToStatus);
        }

        private static byte[] DeltaToProto(Delta delta)
        {
            var protoBuckets = new List<Protocol.Bucket>(delta.Buckets.Length);
            foreach (var bucket in delta.Buckets)
            {
                var contents = new Dictionary<string, Protocol.ValueHolder>(bucket.Content.Count, StringComparer.Ordinal);
                foreach (var bucketContent in bucket.Content)
                {
                    var valueHolder = new Protocol.ValueHolder(
                        bucketContent.Value.Version,
                        Akka.Serialization.Serialization.SerializedActorPath(bucketContent.Value.Ref) // TODO: reuse the method from the core serializer
                    );
                    contents.Add(bucketContent.Key, valueHolder);
                }
                var protoBucket = new Protocol.Bucket(AddressToProto(bucket.Owner), bucket.Version, contents);

                protoBuckets.Add(protoBucket);
            }

            return MessagePackSerializer.Serialize(new Protocol.Delta(protoBuckets), s_defaultResolver);
        }

        private Delta DeltaFrom(in ReadOnlySpan<byte> bytes)
        {
            var deltaProto = MessagePackSerializer.Deserialize<Protocol.Delta>(bytes, s_defaultResolver);
            var buckets = new List<Bucket>();
            foreach (var protoBuckets in deltaProto.Buckets)
            {
                var content = new Dictionary<string, ValueHolder>(StringComparer.Ordinal);

                foreach (var protoBucketContent in protoBuckets.Content)
                {
                    var valueHolder = new ValueHolder(protoBucketContent.Value.Version, ResolveActorRef(protoBucketContent.Value.Ref));
                    content.Add(protoBucketContent.Key, valueHolder);
                }

                var bucket = new Bucket(AddressFrom(protoBuckets.Owner), protoBuckets.Version, content.ToImmutableDictionary(StringComparer.Ordinal));
                buckets.Add(bucket);
            }

            return new Delta(buckets.ToArray());
        }

        private byte[] SendToProto(Send send)
        {
            var protoMessage = new Protocol.Send(
                send.Path,
                send.LocalAffinity,
                _system.SerializeMessage(send.Message)
            );
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private Send SendFrom(in ReadOnlySpan<byte> bytes)
        {
            var sendProto = MessagePackSerializer.Deserialize<Protocol.Send>(bytes, s_defaultResolver);
            return new Send(sendProto.Path, _system.Deserialize(sendProto.Payload), sendProto.LocalAffinity);
        }

        private byte[] SendToAllToProto(SendToAll sendToAll)
        {
            var protoMessage = new Protocol.SendToAll(
                sendToAll.Path,
                sendToAll.ExcludeSelf,
                _system.SerializeMessage(sendToAll.Message)
            );
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private SendToAll SendToAllFrom(in ReadOnlySpan<byte> bytes)
        {
            var sendToAllProto = MessagePackSerializer.Deserialize<Protocol.SendToAll>(bytes, s_defaultResolver);
            return new SendToAll(sendToAllProto.Path, _system.Deserialize(sendToAllProto.Payload), sendToAllProto.AllButSelf);
        }

        private byte[] PublishToProto(Publish publish)
        {
            var protoMessage = new Protocol.Publish(
                publish.Topic,
                _system.SerializeMessage(publish.Message)
            );
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private Publish PublishFrom(in ReadOnlySpan<byte> bytes)
        {
            var publishProto = MessagePackSerializer.Deserialize<Protocol.Publish>(bytes, s_defaultResolver);
            return new Publish(publishProto.Topic, _system.Deserialize(publishProto.Payload));
        }

        private byte[] SendToOneSubscriberToProto(SendToOneSubscriber sendToOneSubscriber)
        {
            var protoMessage = new Protocol.SendToOneSubscriber(_system.SerializeMessage(sendToOneSubscriber.Message));
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private SendToOneSubscriber SendToOneSubscriberFrom(in ReadOnlySpan<byte> bytes)
        {
            var sendToOneSubscriberProto = MessagePackSerializer.Deserialize<Protocol.SendToOneSubscriber>(bytes, s_defaultResolver);
            return new SendToOneSubscriber(_system.Deserialize(sendToOneSubscriberProto.Payload));
        }

        //
        // Address
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AddressData AddressToProto(Address address)
        {
            return new AddressData(
                address.System,
                address.Host,
                (uint)(address.Port ?? 0),
                address.Protocol
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Address AddressFrom(in AddressData addressProto)
        {
            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Hostname,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        private IActorRef ResolveActorRef(string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return _system.Provider.ResolveActorRef(path);
        }
    }
}
