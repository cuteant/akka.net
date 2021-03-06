﻿//-----------------------------------------------------------------------
// <copyright file="ClusterMessageSerializer.cs" company="Akka.NET Project">
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
using Akka.Cluster.Routing;
using Akka.Serialization;
using Akka.Util.Internal;
using MessagePack;
using AddressData = Akka.Serialization.Protocol.AddressData;

namespace Akka.Cluster.Serialization
{
    public class ClusterMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        static class Manifests
        {
            internal const string HeartbeatManifest = "HB";
            internal const string HeartbeatRspManifest = "HBR";
            internal const string GossipEnvelopeManifest = "GE";
            internal const string GossipStatusManifest = "GS";
            internal const string JoinManifest = "J";
            internal const string WelcomeManifest = "WC";
            internal const string LeaveManifest = "L";
            internal const string DownManifest = "D";
            internal const string InitJoinManifest = "IJ";
            internal const string InitJoinAckManifest = "IJA";
            internal const string InitJoinNackManifest = "IJN";
            internal const string ExitingConfirmedManifest = "EC";
            internal const string ClusterRouterPoolManifest = "CRP";
        }
        private static readonly Dictionary<Type, string> ManifestMap;

        static ClusterMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>
            {
                [typeof(ClusterHeartbeatSender.Heartbeat)] = Manifests.HeartbeatManifest,
                [typeof(ClusterHeartbeatSender.HeartbeatRsp)] = Manifests.HeartbeatRspManifest,
                [typeof(GossipEnvelope)] = Manifests.GossipEnvelopeManifest,
                [typeof(GossipStatus)] = Manifests.GossipStatusManifest,
                [typeof(InternalClusterAction.Join)] = Manifests.JoinManifest,
                [typeof(InternalClusterAction.Welcome)] = Manifests.WelcomeManifest,
                [typeof(ClusterUserAction.Leave)] = Manifests.LeaveManifest,
                [typeof(ClusterUserAction.Down)] = Manifests.DownManifest,
                [typeof(InternalClusterAction.InitJoin)] = Manifests.InitJoinManifest,
                [typeof(InternalClusterAction.InitJoinAck)] = Manifests.InitJoinAckManifest,
                [typeof(InternalClusterAction.InitJoinNack)] = Manifests.InitJoinNackManifest,
                [typeof(InternalClusterAction.ExitingConfirmed)] = Manifests.ExitingConfirmedManifest,
                [typeof(ClusterRouterPool)] = Manifests.ClusterRouterPoolManifest
            };
        }

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        public ClusterMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case ClusterHeartbeatSender.Heartbeat heartbeat:
                    manifest = Manifests.HeartbeatManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(heartbeat.From), s_defaultResolver);
                case ClusterHeartbeatSender.HeartbeatRsp heartbeatRsp:
                    manifest = Manifests.HeartbeatRspManifest;
                    return MessagePackSerializer.Serialize(UniqueAddressToProto(heartbeatRsp.From), s_defaultResolver);
                case GossipEnvelope gossipEnvelope:
                    manifest = Manifests.GossipEnvelopeManifest;
                    return GossipEnvelopeToProto(gossipEnvelope);
                case GossipStatus gossipStatus:
                    manifest = Manifests.GossipStatusManifest;
                    return GossipStatusToProto(gossipStatus);
                case InternalClusterAction.Join @join:
                    manifest = Manifests.JoinManifest;
                    return JoinToByteArray(@join);
                case InternalClusterAction.Welcome welcome:
                    manifest = Manifests.WelcomeManifest;
                    return WelcomeMessageBuilder(welcome);
                case ClusterUserAction.Leave leave:
                    manifest = Manifests.LeaveManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(leave.Address), s_defaultResolver);
                case ClusterUserAction.Down down:
                    manifest = Manifests.DownManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(down.Address), s_defaultResolver);
                case InternalClusterAction.InitJoin _:
                    manifest = Manifests.InitJoinManifest;
                    return CuteAnt.EmptyArray<byte>.Instance; // new Google.Protobuf.WellKnownTypes.Empty().ToByteArray();
                case InternalClusterAction.InitJoinAck initJoinAck:
                    manifest = Manifests.InitJoinAckManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(initJoinAck.Address), s_defaultResolver);
                case InternalClusterAction.InitJoinNack initJoinNack:
                    manifest = Manifests.InitJoinNackManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(initJoinNack.Address), s_defaultResolver);
                case InternalClusterAction.ExitingConfirmed exitingConfirmed:
                    manifest = Manifests.ExitingConfirmedManifest;
                    return MessagePackSerializer.Serialize(UniqueAddressToProto(exitingConfirmed.Address), s_defaultResolver);
                case ClusterRouterPool pool:
                    manifest = Manifests.ClusterRouterPoolManifest;
                    return ClusterRouterPoolToByteArray(pool);
                default:
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterMessage(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case Manifests.HeartbeatManifest:
                    return new ClusterHeartbeatSender.Heartbeat(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case Manifests.HeartbeatRspManifest:
                    return new ClusterHeartbeatSender.HeartbeatRsp(UniqueAddressFrom(MessagePackSerializer.Deserialize<Protocol.UniqueAddress>(bytes, s_defaultResolver)));
                case Manifests.GossipEnvelopeManifest:
                    return GossipEnvelopeFrom(bytes);
                case Manifests.GossipStatusManifest:
                    return GossipStatusFrom(bytes);
                case Manifests.JoinManifest:
                    return JoinFrom(bytes);
                case Manifests.WelcomeManifest:
                    return WelcomeFrom(bytes);
                case Manifests.LeaveManifest:
                    return new ClusterUserAction.Leave(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case Manifests.DownManifest:
                    return new ClusterUserAction.Down(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case Manifests.InitJoinManifest:
                    return InternalClusterAction.InitJoin.Instance;
                case Manifests.InitJoinAckManifest:
                    return new InternalClusterAction.InitJoinAck(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case Manifests.InitJoinNackManifest:
                    return new InternalClusterAction.InitJoinNack(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case Manifests.ExitingConfirmedManifest:
                    return new InternalClusterAction.ExitingConfirmed(UniqueAddressFrom(MessagePackSerializer.Deserialize<Protocol.UniqueAddress>(bytes, s_defaultResolver)));
                case Manifests.ClusterRouterPoolManifest:
                    return ClusterRouterPoolFrom(bytes);
            }

            throw ThrowHelper.GetArgumentException_Serializer_ClusterMessage(manifest);
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (type is null) { return null; }
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
                case ClusterHeartbeatSender.Heartbeat _:
                    return Manifests.HeartbeatManifest;
                case ClusterHeartbeatSender.HeartbeatRsp _:
                    return Manifests.HeartbeatRspManifest;
                case GossipEnvelope _:
                    return Manifests.GossipEnvelopeManifest;
                case GossipStatus _:
                    return Manifests.GossipStatusManifest;
                case InternalClusterAction.Join _:
                    return Manifests.JoinManifest;
                case InternalClusterAction.Welcome _:
                    return Manifests.WelcomeManifest;
                case ClusterUserAction.Leave _:
                    return Manifests.LeaveManifest;
                case ClusterUserAction.Down _:
                    return Manifests.DownManifest;
                case InternalClusterAction.InitJoin _:
                    return Manifests.InitJoinManifest;
                case InternalClusterAction.InitJoinAck _:
                    return Manifests.InitJoinAckManifest;
                case InternalClusterAction.InitJoinNack _:
                    return Manifests.InitJoinNackManifest;
                case InternalClusterAction.ExitingConfirmed _:
                    return Manifests.ExitingConfirmedManifest;
                case ClusterRouterPool _:
                    return Manifests.ClusterRouterPoolManifest;
                default:
                    throw ThrowHelper.GetArgumentException_Serializer_ClusterMessage(obj);
            }
        }

        //
        // Internal Cluster Action Messages
        //
        private static byte[] JoinToByteArray(InternalClusterAction.Join join)
        {
            var message = new Protocol.Join(
                UniqueAddressToProto(join.Node),
                join.Roles.ToArray()
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static InternalClusterAction.Join JoinFrom(in ReadOnlySpan<byte> bytes)
        {
            var join = MessagePackSerializer.Deserialize<Protocol.Join>(bytes, s_defaultResolver);
            return new InternalClusterAction.Join(UniqueAddressFrom(join.Node), join.Roles.ToImmutableHashSet(StringComparer.Ordinal));
        }

        private static byte[] WelcomeMessageBuilder(InternalClusterAction.Welcome welcome)
        {
            var welcomeProto = new Protocol.Welcome(
                UniqueAddressToProto(welcome.From),
                GossipToProto(welcome.Gossip)
            );
            return MessagePackSerializer.Serialize(welcomeProto, s_defaultResolver);
        }

        private static InternalClusterAction.Welcome WelcomeFrom(in ReadOnlySpan<byte> bytes)
        {
            var welcomeProto = MessagePackSerializer.Deserialize<Protocol.Welcome>(bytes, s_defaultResolver);
            return new InternalClusterAction.Welcome(UniqueAddressFrom(welcomeProto.From), GossipFrom(welcomeProto.Gossip));
        }

        //
        // Cluster Gossip Messages
        //
        private static byte[] GossipEnvelopeToProto(GossipEnvelope gossipEnvelope)
        {
            var message = new Protocol.GossipEnvelope(
                UniqueAddressToProto(gossipEnvelope.From),
                UniqueAddressToProto(gossipEnvelope.To),
                MessagePackSerializer.Serialize(GossipToProto(gossipEnvelope.Gossip), s_defaultResolver)
            );

            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static GossipEnvelope GossipEnvelopeFrom(in ReadOnlySpan<byte> bytes)
        {
            var gossipEnvelopeProto = MessagePackSerializer.Deserialize<Protocol.GossipEnvelope>(bytes, s_defaultResolver);

            return new GossipEnvelope(
                UniqueAddressFrom(gossipEnvelopeProto.From),
                UniqueAddressFrom(gossipEnvelopeProto.To),
                GossipFrom(MessagePackSerializer.Deserialize<Protocol.Gossip>(gossipEnvelopeProto.SerializedGossip, s_defaultResolver)));
        }

        private static byte[] GossipStatusToProto(GossipStatus gossipStatus)
        {
            var allHashes = gossipStatus.Version.Versions.Keys.Select(x => x.ToString()).ToArray();
            var hashMapping = allHashes.ZipWithIndex();

            var message = new Protocol.GossipStatus(
                UniqueAddressToProto(gossipStatus.From),
                allHashes,
                VectorClockToProto(gossipStatus.Version, hashMapping)
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static GossipStatus GossipStatusFrom(in ReadOnlySpan<byte> bytes)
        {
            var gossipStatusProto = MessagePackSerializer.Deserialize<Protocol.GossipStatus>(bytes, s_defaultResolver);
            return new GossipStatus(UniqueAddressFrom(gossipStatusProto.From), VectorClockFrom(gossipStatusProto.Version, gossipStatusProto.AllHashes));
        }

        //
        // Cluster routing
        //

        private byte[] ClusterRouterPoolToByteArray(ClusterRouterPool clusterRouterPool)
        {
            var message = new Protocol.ClusterRouterPool(
                _system.SerializeMessage(clusterRouterPool.Local),
                ClusterRouterPoolSettingsToProto(clusterRouterPool.Settings)
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private ClusterRouterPool ClusterRouterPoolFrom(in ReadOnlySpan<byte> bytes)
        {
            var clusterRouterPool = MessagePackSerializer.Deserialize<Protocol.ClusterRouterPool>(bytes, s_defaultResolver);
            return new ClusterRouterPool(
                (Akka.Routing.Pool)_system.Deserialize(clusterRouterPool.Pool),
                ClusterRouterPoolSettingsFrom(clusterRouterPool.Settings));
        }

        private static Protocol.ClusterRouterPoolSettings ClusterRouterPoolSettingsToProto(ClusterRouterPoolSettings clusterRouterPoolSettings)
        {
            return new Protocol.ClusterRouterPoolSettings(
                (uint)clusterRouterPoolSettings.TotalInstances,
                (uint)clusterRouterPoolSettings.MaxInstancesPerNode,
                clusterRouterPoolSettings.AllowLocalRoutees,
                clusterRouterPoolSettings.UseRole ?? string.Empty
            );
        }

        private static ClusterRouterPoolSettings ClusterRouterPoolSettingsFrom(Protocol.ClusterRouterPoolSettings clusterRouterPoolSettingsProto)
        {
            return new ClusterRouterPoolSettings(
                (int)clusterRouterPoolSettingsProto.TotalInstances,
                (int)clusterRouterPoolSettingsProto.MaxInstancesPerNode,
                clusterRouterPoolSettingsProto.AllowLocalRoutees,
                clusterRouterPoolSettingsProto.UseRole == string.Empty ? null : clusterRouterPoolSettingsProto.UseRole);
        }

        //
        // Gossip
        //

        private static Protocol.Gossip GossipToProto(Gossip gossip)
        {
            var allMembers = gossip.Members.ToArray();
            var allAddresses = gossip.Members.Select(x => x.UniqueAddress).ToArray();
            var addressMapping = allAddresses.ZipWithIndex();
            var allRoles = allMembers.Aggregate(ImmutableHashSet.Create<string>(StringComparer.Ordinal), (set, member) => set.Union(member.Roles)).ToArray();
            var roleMapping = allRoles.ZipWithIndex();
            var allHashes = gossip.Version.Versions.Keys.Select(x => x.ToString()).ToArray();
            var hashMapping = allHashes.ZipWithIndex();

            int MapUniqueAddress(UniqueAddress address) => MapWithErrorMessage(addressMapping, address, "address");

            Protocol.Member MemberToProto(Member m)
            {
                return new Protocol.Member(
                    MapUniqueAddress(m.UniqueAddress),
                    m.UpNumber,
                    (Protocol.MemberStatus)m.Status,
                    m.Roles.Select(s => MapWithErrorMessage(roleMapping, s, "role")).ToArray()
                );
            }

            var reachabilityProto = ReachabilityToProto(gossip.Overview.Reachability, addressMapping);
            var membersProtos = gossip.Members.Select(MemberToProto).ToArray();
            var seenProtos = gossip.Overview.Seen.Select(MapUniqueAddress).ToArray();

            var overview = new Protocol.GossipOverview(seenProtos, reachabilityProto);

            var message = new Protocol.Gossip();
            message.AllAddresses = allAddresses.Select(UniqueAddressToProto).ToArray();
            message.AllRoles = allRoles;
            message.AllHashes = allHashes;
            message.Members = membersProtos;
            message.Overview = overview;
            message.Version = VectorClockToProto(gossip.Version, hashMapping);
            return message;
        }

        private static Gossip GossipFrom(Protocol.Gossip gossip)
        {
            var addressMapping = gossip.AllAddresses.Select(UniqueAddressFrom).ToList();
            var roleMapping = gossip.AllRoles.ToList();
            var hashMapping = gossip.AllHashes.ToList();

            Member MemberFromProto(Protocol.Member member) =>
                Member.Create(
                    addressMapping[member.AddressIndex],
                    member.UpNumber,
                    (MemberStatus)member.Status,
                    0u < (uint)member.RolesIndexes.Length ?
                        member.RolesIndexes.Select(x => roleMapping[x]).ToImmutableHashSet(StringComparer.Ordinal) :
                        ImmutableHashSet<string>.Empty);

            var members = gossip.Members.Select(MemberFromProto).ToImmutableSortedSet(Member.Ordering);
            var reachability = ReachabilityFromProto(gossip.Overview.ObserverReachability, addressMapping);
            var protoSeen = gossip.Overview.Seen;
            var seen = protoSeen is null || 0u >= (uint)protoSeen.Length ?
                ImmutableHashSet<UniqueAddress>.Empty :
                protoSeen.Select(x => addressMapping[x]).ToImmutableHashSet(UniqueAddressComparer.Instance);
            var overview = new GossipOverview(seen, reachability);

            return new Gossip(members, overview, VectorClockFrom(gossip.Version, hashMapping));
        }

        private static List<Protocol.ObserverReachability> ReachabilityToProto(Reachability reachability, Dictionary<UniqueAddress, int> addressMapping)
        {
            var builderList = new List<Protocol.ObserverReachability>(reachability.Versions.Count);
            foreach (var version in reachability.Versions)
            {
                var subjectReachability = reachability.RecordsFrom(version.Key).Select(
                    r =>
                    {
                        var sr = new Protocol.SubjectReachability(
                            MapWithErrorMessage(addressMapping, r.Subject, "address"),
                            (Protocol.ReachabilityStatus)r.Status,
                            r.Version
                        );
                        return sr;
                    }).ToArray();

                var observerReachability = new Protocol.ObserverReachability(
                    MapWithErrorMessage(addressMapping, version.Key, "address"),
                    version.Value,
                    subjectReachability
                );
                builderList.Add(observerReachability);
            }
            return builderList;
        }

        private static Reachability ReachabilityFromProto(IList<Protocol.ObserverReachability> reachabilityProto, List<UniqueAddress> addressMapping)
        {
            if (reachabilityProto.IsNullOrEmpty())
            {
                return new Reachability(ImmutableList<Reachability.Record>.Empty, ImmutableDictionary<UniqueAddress, long>.Empty);
            }
            var recordBuilder = ImmutableList.CreateBuilder<Reachability.Record>();
            var versionsBuilder = ImmutableDictionary.CreateBuilder<UniqueAddress, long>(UniqueAddressComparer.Instance);
            foreach (var o in reachabilityProto)
            {
                var observer = addressMapping[o.AddressIndex];
                versionsBuilder.Add(observer, o.Version);
                foreach (var s in o.SubjectReachability)
                {
                    var subject = addressMapping[s.AddressIndex];
                    var record = new Reachability.Record(observer, subject, (Reachability.ReachabilityStatus)s.Status,
                        s.Version);
                    recordBuilder.Add(record);
                }
            }

            return new Reachability(recordBuilder.ToImmutable(), versionsBuilder.ToImmutable());
        }

        private static Protocol.VectorClock VectorClockToProto(VectorClock vectorClock, Dictionary<string, int> hashMapping)
        {
            var versions = vectorClock.Versions.Select(clock => new Protocol.Version(
                    MapWithErrorMessage(hashMapping, clock.Key.ToString(), "hash"),
                    clock.Value
                )).ToArray();
            return new Protocol.VectorClock(0L, versions);
        }

        private static VectorClock VectorClockFrom(Protocol.VectorClock version, IList<string> hashMapping)
        {
            return VectorClock.Create(version.Versions.ToImmutableSortedDictionary(version1 =>
                    VectorClock.Node.FromHash(hashMapping[version1.HashIndex]), version1 => version1.Timestamp));
        }

        private static int MapWithErrorMessage<T>(Dictionary<T, int> map, T value, string unknown)
        {
            if (map.TryGetValue(value, out int mapIndex)) { return mapIndex; }

            ThrowHelper.ThrowArgumentException_UnknownInClusterMessage(value, unknown); return 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Protocol.UniqueAddress UniqueAddressToProto(UniqueAddress uniqueAddress)
        {
            return new Protocol.UniqueAddress(AddressToProto(uniqueAddress.Address), (uint)uniqueAddress.Uid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UniqueAddress UniqueAddressFrom(Protocol.UniqueAddress uniqueAddressProto)
        {
            return new UniqueAddress(AddressFrom(uniqueAddressProto.Address), (int)uniqueAddressProto.Uid);
        }
    }
}
