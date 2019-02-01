//-----------------------------------------------------------------------
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
using AddressData = Akka.Remote.Serialization.Protocol.AddressData;

namespace Akka.Cluster.Serialization
{
    public class ClusterMessageSerializer : SerializerWithIntegerManifest
    {
        #region manifests

        static class _
        {
            internal const int HeartbeatManifest = 500;
            internal const int HeartbeatRspManifest = 501;
            internal const int GossipEnvelopeManifest = 502;
            internal const int GossipStatusManifest = 503;
            internal const int JoinManifest = 504;
            internal const int WelcomeManifest = 505;
            internal const int LeaveManifest = 506;
            internal const int DownManifest = 507;
            internal const int InitJoinManifest = 508;
            internal const int InitJoinAckManifest = 509;
            internal const int InitJoinNackManifest = 510;
            internal const int ExitingConfirmedManifest = 511;
            internal const int ClusterRouterPoolManifest = 512;
        }
        private static readonly Dictionary<Type, int> ManifestMap;

        static ClusterMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, int>
            {
                [typeof(ClusterHeartbeatSender.Heartbeat)] = _.HeartbeatManifest,
                [typeof(ClusterHeartbeatSender.HeartbeatRsp)] = _.HeartbeatRspManifest,
                [typeof(GossipEnvelope)] = _.GossipEnvelopeManifest,
                [typeof(GossipStatus)] = _.GossipStatusManifest,
                [typeof(InternalClusterAction.Join)] = _.JoinManifest,
                [typeof(InternalClusterAction.Welcome)] = _.WelcomeManifest,
                [typeof(ClusterUserAction.Leave)] = _.LeaveManifest,
                [typeof(ClusterUserAction.Down)] = _.DownManifest,
                [typeof(InternalClusterAction.InitJoin)] = _.InitJoinManifest,
                [typeof(InternalClusterAction.InitJoinAck)] = _.InitJoinAckManifest,
                [typeof(InternalClusterAction.InitJoinNack)] = _.InitJoinNackManifest,
                [typeof(InternalClusterAction.ExitingConfirmed)] = _.ExitingConfirmedManifest,
                [typeof(ClusterRouterPool)] = _.ClusterRouterPoolManifest
            };
        }

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        public ClusterMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out int manifest)
        {
            switch (obj)
            {
                case ClusterHeartbeatSender.Heartbeat heartbeat:
                    manifest = _.HeartbeatManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(heartbeat.From), s_defaultResolver);
                case ClusterHeartbeatSender.HeartbeatRsp heartbeatRsp:
                    manifest = _.HeartbeatRspManifest;
                    return MessagePackSerializer.Serialize(UniqueAddressToProto(heartbeatRsp.From), s_defaultResolver);
                case GossipEnvelope gossipEnvelope:
                    manifest = _.GossipEnvelopeManifest;
                    return GossipEnvelopeToProto(gossipEnvelope);
                case GossipStatus gossipStatus:
                    manifest = _.GossipStatusManifest;
                    return GossipStatusToProto(gossipStatus);
                case InternalClusterAction.Join @join:
                    manifest = _.JoinManifest;
                    return JoinToByteArray(@join);
                case InternalClusterAction.Welcome welcome:
                    manifest = _.WelcomeManifest;
                    return WelcomeMessageBuilder(welcome);
                case ClusterUserAction.Leave leave:
                    manifest = _.LeaveManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(leave.Address), s_defaultResolver);
                case ClusterUserAction.Down down:
                    manifest = _.DownManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(down.Address), s_defaultResolver);
                case InternalClusterAction.InitJoin _:
                    manifest = _.InitJoinManifest;
                    return CuteAnt.EmptyArray<byte>.Instance; // new Google.Protobuf.WellKnownTypes.Empty().ToByteArray();
                case InternalClusterAction.InitJoinAck initJoinAck:
                    manifest = _.InitJoinAckManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(initJoinAck.Address), s_defaultResolver);
                case InternalClusterAction.InitJoinNack initJoinNack:
                    manifest = _.InitJoinNackManifest;
                    return MessagePackSerializer.Serialize(AddressToProto(initJoinNack.Address), s_defaultResolver);
                case InternalClusterAction.ExitingConfirmed exitingConfirmed:
                    manifest = _.ExitingConfirmedManifest;
                    return MessagePackSerializer.Serialize(UniqueAddressToProto(exitingConfirmed.Address), s_defaultResolver);
                case ClusterRouterPool pool:
                    manifest = _.ClusterRouterPoolManifest;
                    return ClusterRouterPoolToByteArray(pool);
                default:
                    manifest = 0; return ThrowHelper.ThrowArgumentException_Serializer_ClusterMessage<byte[]>(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, int manifest)
        {
            switch (manifest)
            {
                case _.HeartbeatManifest:
                    return new ClusterHeartbeatSender.Heartbeat(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case _.HeartbeatRspManifest:
                    return new ClusterHeartbeatSender.HeartbeatRsp(UniqueAddressFrom(MessagePackSerializer.Deserialize<Protocol.UniqueAddress>(bytes, s_defaultResolver)));
                case _.GossipEnvelopeManifest:
                    return GossipEnvelopeFrom(bytes);
                case _.GossipStatusManifest:
                    return GossipStatusFrom(bytes);
                case _.JoinManifest:
                    return JoinFrom(bytes);
                case _.WelcomeManifest:
                    return WelcomeFrom(bytes);
                case _.LeaveManifest:
                    return new ClusterUserAction.Leave(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case _.DownManifest:
                    return new ClusterUserAction.Down(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case _.InitJoinManifest:
                    return InternalClusterAction.InitJoin.Instance;
                case _.InitJoinAckManifest:
                    return new InternalClusterAction.InitJoinAck(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case _.InitJoinNackManifest:
                    return new InternalClusterAction.InitJoinNack(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                case _.ExitingConfirmedManifest:
                    return new InternalClusterAction.ExitingConfirmed(UniqueAddressFrom(MessagePackSerializer.Deserialize<Protocol.UniqueAddress>(bytes, s_defaultResolver)));
                case _.ClusterRouterPoolManifest:
                    return ClusterRouterPoolFrom(bytes);
            }

            return ThrowHelper.ThrowArgumentException_Serializer_ClusterMessage(manifest);
        }

        /// <inheritdoc />
        protected override int GetManifest(Type type)
        {
            if (null == type) { return 0; }
            var manifestMap = ManifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            return ThrowHelper.ThrowArgumentException_Serializer_D<int>(type);
        }

        /// <inheritdoc />
        public override int Manifest(object obj)
        {
            switch (obj)
            {
                case ClusterHeartbeatSender.Heartbeat heartbeat:
                    return _.HeartbeatManifest;
                case ClusterHeartbeatSender.HeartbeatRsp heartbeatRsp:
                    return _.HeartbeatRspManifest;
                case GossipEnvelope gossipEnvelope:
                    return _.GossipEnvelopeManifest;
                case GossipStatus gossipStatus:
                    return _.GossipStatusManifest;
                case InternalClusterAction.Join @join:
                    return _.JoinManifest;
                case InternalClusterAction.Welcome welcome:
                    return _.WelcomeManifest;
                case ClusterUserAction.Leave leave:
                    return _.LeaveManifest;
                case ClusterUserAction.Down down:
                    return _.DownManifest;
                case InternalClusterAction.InitJoin _:
                    return _.InitJoinManifest;
                case InternalClusterAction.InitJoinAck initJoinAck:
                    return _.InitJoinAckManifest;
                case InternalClusterAction.InitJoinNack initJoinNack:
                    return _.InitJoinNackManifest;
                case InternalClusterAction.ExitingConfirmed exitingConfirmed:
                    return _.ExitingConfirmedManifest;
                case ClusterRouterPool pool:
                    return _.ClusterRouterPoolManifest;
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterMessage<int>(obj);
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

        private static InternalClusterAction.Join JoinFrom(byte[] bytes)
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

        private static InternalClusterAction.Welcome WelcomeFrom(byte[] bytes)
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

        private static GossipEnvelope GossipEnvelopeFrom(byte[] bytes)
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

        private static GossipStatus GossipStatusFrom(byte[] bytes)
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
                system.Serialize(clusterRouterPool.Local),
                ClusterRouterPoolSettingsToProto(clusterRouterPool.Settings)
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private ClusterRouterPool ClusterRouterPoolFrom(byte[] bytes)
        {
            var clusterRouterPool = MessagePackSerializer.Deserialize<Protocol.ClusterRouterPool>(bytes, s_defaultResolver);
            return new ClusterRouterPool(
                (Akka.Routing.Pool)system.Deserialize(clusterRouterPool.Pool),
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
                    member.RolesIndexes.Length > 0 ?
                        member.RolesIndexes.Select(x => roleMapping[x]).ToImmutableHashSet(StringComparer.Ordinal) :
                        ImmutableHashSet<string>.Empty);

            var members = gossip.Members.Select(MemberFromProto).ToImmutableSortedSet(Member.Ordering);
            var reachability = ReachabilityFromProto(gossip.Overview.ObserverReachability, addressMapping);
            var protoSeen = gossip.Overview.Seen;
            var seen = protoSeen == null || protoSeen.Length == 0 ?
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
            if (reachabilityProto == null || reachabilityProto.Count == 0)
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

            return ThrowHelper.ThrowArgumentException_UnknownInClusterMessage(value, unknown);
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
