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
using Akka.Remote.Serialization;
using Akka.Util.Internal;
using MessagePack;
using AddressData = Akka.Remote.Serialization.Protocol.AddressData;

namespace Akka.Cluster.Serialization
{
    public class ClusterMessageSerializer : SerializerWithTypeManifest
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        public ClusterMessageSerializer(ExtendedActorSystem system) : base(system) { }

        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case ClusterHeartbeatSender.Heartbeat heartbeat:
                    return MessagePackSerializer.Serialize(AddressToProto(heartbeat.From), s_defaultResolver);
                case ClusterHeartbeatSender.HeartbeatRsp heartbeatRsp:
                    return MessagePackSerializer.Serialize(UniqueAddressToProto(heartbeatRsp.From), s_defaultResolver);
                case GossipEnvelope gossipEnvelope:
                    return GossipEnvelopeToProto(gossipEnvelope);
                case GossipStatus gossipStatus:
                    return GossipStatusToProto(gossipStatus);
                case InternalClusterAction.Join @join:
                    return JoinToByteArray(@join);
                case InternalClusterAction.Welcome welcome:
                    return WelcomeMessageBuilder(welcome);
                case ClusterUserAction.Leave leave:
                    return MessagePackSerializer.Serialize(AddressToProto(leave.Address), s_defaultResolver);
                case ClusterUserAction.Down down:
                    return MessagePackSerializer.Serialize(AddressToProto(down.Address), s_defaultResolver);
                case InternalClusterAction.InitJoin _:
                    return CuteAnt.EmptyArray<byte>.Instance; // new Google.Protobuf.WellKnownTypes.Empty().ToByteArray();
                case InternalClusterAction.InitJoinAck initJoinAck:
                    return MessagePackSerializer.Serialize(AddressToProto(initJoinAck.Address), s_defaultResolver);
                case InternalClusterAction.InitJoinNack initJoinNack:
                    return MessagePackSerializer.Serialize(AddressToProto(initJoinNack.Address), s_defaultResolver);
                case InternalClusterAction.ExitingConfirmed exitingConfirmed:
                    return MessagePackSerializer.Serialize(UniqueAddressToProto(exitingConfirmed.Address), s_defaultResolver);
                case ClusterRouterPool pool:
                    return ClusterRouterPoolToByteArray(pool);
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_ClusterMessage(obj);
            }
        }

        static class _
        {
            internal const int Heartbeat = 0;
            internal const int HeartbeatRsp = 1;
            internal const int GossipEnvelope = 2;
            internal const int GossipStatus = 3;
            internal const int Join = 4;
            internal const int Welcome = 5;
            internal const int Leave = 6;
            internal const int Down = 7;
            internal const int InitJoin = 8;
            internal const int InitJoinAck = 9;
            internal const int InitJoinNack = 10;
            internal const int ExitingConfirmed = 11;
            internal const int ClusterRouterPool = 12;
        }
        private readonly Dictionary<Type, int> _fromBinaryMap = new Dictionary<Type, int>
        {
            [typeof(ClusterHeartbeatSender.Heartbeat)] = _.Heartbeat,
            [typeof(ClusterHeartbeatSender.HeartbeatRsp)] = _.HeartbeatRsp,
            [typeof(GossipEnvelope)] = _.GossipEnvelope,
            [typeof(GossipStatus)] = _.GossipStatus,
            [typeof(InternalClusterAction.Join)] = _.Join,
            [typeof(InternalClusterAction.Welcome)] = _.Welcome,
            [typeof(ClusterUserAction.Leave)] = _.Leave,
            [typeof(ClusterUserAction.Down)] = _.Down,
            [typeof(InternalClusterAction.InitJoin)] = _.InitJoin,
            [typeof(InternalClusterAction.InitJoinAck)] = _.InitJoinAck,
            [typeof(InternalClusterAction.InitJoinNack)] = _.InitJoinNack,
            [typeof(InternalClusterAction.ExitingConfirmed)] = _.ExitingConfirmed,
            [typeof(ClusterRouterPool)] = _.ClusterRouterPool
        };

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (_fromBinaryMap.TryGetValue(type, out var flag))
            {
                switch (flag)
                {
                    case _.Heartbeat:
                        return new ClusterHeartbeatSender.Heartbeat(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                    case _.HeartbeatRsp:
                        return new ClusterHeartbeatSender.HeartbeatRsp(UniqueAddressFrom(MessagePackSerializer.Deserialize<Protocol.UniqueAddress>(bytes, s_defaultResolver)));
                    case _.GossipEnvelope:
                        return GossipEnvelopeFrom(bytes);
                    case _.GossipStatus:
                        return GossipStatusFrom(bytes);
                    case _.Join:
                        return JoinFrom(bytes);
                    case _.Welcome:
                        return WelcomeFrom(bytes);
                    case _.Leave:
                        return new ClusterUserAction.Leave(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                    case _.Down:
                        return new ClusterUserAction.Down(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                    case _.InitJoin:
                        return InternalClusterAction.InitJoin.Instance;
                    case _.InitJoinAck:
                        return new InternalClusterAction.InitJoinAck(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                    case _.InitJoinNack:
                        return new InternalClusterAction.InitJoinNack(AddressFrom(MessagePackSerializer.Deserialize<AddressData>(bytes, s_defaultResolver)));
                    case _.ExitingConfirmed:
                        return new InternalClusterAction.ExitingConfirmed(UniqueAddressFrom(MessagePackSerializer.Deserialize<Protocol.UniqueAddress>(bytes, s_defaultResolver)));
                    case _.ClusterRouterPool:
                        return ClusterRouterPoolFrom(bytes);
                }
            }

            return ThrowHelper.ThrowArgumentException_Serializer_ClusterMessage(type);
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
            return new InternalClusterAction.Join(UniqueAddressFrom(join.Node), join.Roles.ToImmutableHashSet());
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
                WrappedPayloadSupport.PayloadToProto(system, clusterRouterPool.Local),
                ClusterRouterPoolSettingsToProto(clusterRouterPool.Settings)
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private ClusterRouterPool ClusterRouterPoolFrom(byte[] bytes)
        {
            var clusterRouterPool = MessagePackSerializer.Deserialize<Protocol.ClusterRouterPool>(bytes, s_defaultResolver);
            return new ClusterRouterPool(
                (Akka.Routing.Pool)WrappedPayloadSupport.PayloadFrom(system, clusterRouterPool.Pool),
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
            var allRoles = allMembers.Aggregate(ImmutableHashSet.Create<string>(), (set, member) => set.Union(member.Roles)).ToArray();
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
                    member.RolesIndexes.Select(x => roleMapping[x]).ToImmutableHashSet());

            var members = gossip.Members.Select(MemberFromProto).ToImmutableSortedSet(Member.Ordering);
            var reachability = ReachabilityFromProto(gossip.Overview.ObserverReachability, addressMapping);
            var seen = gossip.Overview.Seen.Select(x => addressMapping[x]).ToImmutableHashSet();
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

        private static Reachability ReachabilityFromProto(IEnumerable<Protocol.ObserverReachability> reachabilityProto, List<UniqueAddress> addressMapping)
        {
            var recordBuilder = ImmutableList.CreateBuilder<Reachability.Record>();
            var versionsBuilder = ImmutableDictionary.CreateBuilder<UniqueAddress, long>();
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
