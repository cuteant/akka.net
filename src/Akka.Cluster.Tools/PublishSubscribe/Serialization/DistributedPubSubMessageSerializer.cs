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
using Akka.Remote.Serialization;
using Akka.Serialization;
using CuteAnt.Text;
using MessagePack;
using AddressData = Akka.Remote.Serialization.Protocol.AddressData;

namespace Akka.Cluster.Tools.PublishSubscribe.Serialization
{
    /// <summary>
    /// Protobuf serializer of DistributedPubSubMediator messages.
    /// </summary>
    public class DistributedPubSubMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string StatusManifest = "A";
        private static readonly byte[] StatusManifestBytes = StringHelper.UTF8NoBOM.GetBytes(StatusManifest);
        private const string DeltaManifest = "B";
        private static readonly byte[] DeltaManifestBytes = StringHelper.UTF8NoBOM.GetBytes(DeltaManifest);
        private const string SendManifest = "C";
        private static readonly byte[] SendManifestBytes = StringHelper.UTF8NoBOM.GetBytes(SendManifest);
        private const string SendToAllManifest = "D";
        private static readonly byte[] SendToAllManifestBytes = StringHelper.UTF8NoBOM.GetBytes(SendToAllManifest);
        private const string PublishManifest = "E";
        private static readonly byte[] PublishManifestBytes = StringHelper.UTF8NoBOM.GetBytes(PublishManifest);
        private const string SendToOneSubscriberManifest = "F";
        private static readonly byte[] SendToOneSubscriberManifestBytes = StringHelper.UTF8NoBOM.GetBytes(SendToOneSubscriberManifest);

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedPubSubMessageSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        public DistributedPubSubMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <summary>
        /// Serializes the given object into a byte array
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="obj"/> is of an unknown type.
        /// Acceptable types include:
        /// <see cref="Akka.Cluster.Tools.PublishSubscribe.Internal.Status"/> | <see cref="Akka.Cluster.Tools.PublishSubscribe.Internal.Delta"/> | <see cref="Send"/> | <see cref="SendToAll"/> | <see cref="Publish"/>
        /// </exception>
        /// <returns>A byte array containing the serialized object</returns>
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case Internal.Status status:
                    return StatusToProto(status);
                case Internal.Delta delta:
                    return DeltaToProto(delta);
                case Send send:
                    return SendToProto(send);
                case SendToAll sendToAll:
                    return SendToAllToProto(sendToAll);
                case Publish publish:
                    return PublishToProto(publish);
                case SendToOneSubscriber sub:
                    return SendToOneSubscriberToProto(sub);
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_DistributedPubSubMessage(obj);
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
                    return ThrowHelper.ThrowArgumentException_Serializer_DistributedPubSubMessage(manifest);
            }
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
                case Internal.Status _:
                    return StatusManifest;
                case Internal.Delta _:
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
                    return ThrowHelper.ThrowArgumentException_Manifest_DistributedPubSubMessage<string>(o);
            }
        }
        /// <inheritdoc />
        public override byte[] ManifestBytes(object o)
        {
            switch (o)
            {
                case Internal.Status _:
                    return StatusManifestBytes;
                case Internal.Delta _:
                    return DeltaManifestBytes;
                case Send _:
                    return SendManifestBytes;
                case SendToAll _:
                    return SendToAllManifestBytes;
                case Publish _:
                    return PublishManifestBytes;
                case SendToOneSubscriber _:
                    return SendToOneSubscriberManifestBytes;
                default:
                    return ThrowHelper.ThrowArgumentException_Manifest_DistributedPubSubMessage<byte[]>(o);
            }
        }

        private static byte[] StatusToProto(Internal.Status status)
        {
            var protoVersions = status.Versions.Select(_ => new Protocol.Version(AddressToProto(_.Key), _.Value)).ToArray();
            var message = new Protocol.Status(protoVersions, status.IsReplyToStatus);

            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Internal.Status StatusFrom(byte[] bytes)
        {
            var statusProto = MessagePackSerializer.Deserialize<Protocol.Status>(bytes, s_defaultResolver);
            var versions = new Dictionary<Address, long>();

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

        private Delta DeltaFrom(byte[] bytes)
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

                var bucket = new Bucket(AddressFrom(protoBuckets.Owner), protoBuckets.Version, content.ToImmutableDictionary());
                buckets.Add(bucket);
            }

            return new Delta(buckets.ToArray());
        }

        private byte[] SendToProto(Send send)
        {
            var protoMessage = new Protocol.Send(
                send.Path,
                send.LocalAffinity,
                WrappedPayloadSupport.PayloadToProto(system, send.Message)
            );
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private Send SendFrom(byte[] bytes)
        {
            var sendProto = MessagePackSerializer.Deserialize<Protocol.Send>(bytes, s_defaultResolver);
            return new Send(sendProto.Path, WrappedPayloadSupport.PayloadFrom(system, sendProto.Payload), sendProto.LocalAffinity);
        }

        private byte[] SendToAllToProto(SendToAll sendToAll)
        {
            var protoMessage = new Protocol.SendToAll(
                sendToAll.Path,
                sendToAll.ExcludeSelf,
                WrappedPayloadSupport.PayloadToProto(system, sendToAll.Message)
            );
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private SendToAll SendToAllFrom(byte[] bytes)
        {
            var sendToAllProto = MessagePackSerializer.Deserialize<Protocol.SendToAll>(bytes, s_defaultResolver);
            return new SendToAll(sendToAllProto.Path, WrappedPayloadSupport.PayloadFrom(system, sendToAllProto.Payload), sendToAllProto.AllButSelf);
        }

        private byte[] PublishToProto(Publish publish)
        {
            var protoMessage = new Protocol.Publish(
                publish.Topic,
                WrappedPayloadSupport.PayloadToProto(system, publish.Message)
            );
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private Publish PublishFrom(byte[] bytes)
        {
            var publishProto = MessagePackSerializer.Deserialize<Protocol.Publish>(bytes, s_defaultResolver);
            return new Publish(publishProto.Topic, WrappedPayloadSupport.PayloadFrom(system, publishProto.Payload));
        }

        private byte[] SendToOneSubscriberToProto(SendToOneSubscriber sendToOneSubscriber)
        {
            var protoMessage = new Protocol.SendToOneSubscriber(WrappedPayloadSupport.PayloadToProto(system, sendToOneSubscriber.Message));
            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        private SendToOneSubscriber SendToOneSubscriberFrom(byte[] bytes)
        {
            var sendToOneSubscriberProto = MessagePackSerializer.Deserialize<Protocol.SendToOneSubscriber>(bytes, s_defaultResolver);
            return new SendToOneSubscriber(WrappedPayloadSupport.PayloadFrom(system, sendToOneSubscriberProto.Payload));
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
        private static Address AddressFrom(AddressData addressProto)
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

            return system.Provider.ResolveActorRef(path);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static string GetObjectManifest(Serializer serializer, object obj)
        //{
        //    if (serializer is SerializerWithStringManifest manifestSerializer)
        //    {
        //        return manifestSerializer.Manifest(obj);
        //    }

        //    return obj.GetType().TypeQualifiedName();
        //}
    }
}
