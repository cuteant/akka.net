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
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.Reflection;
using Google.Protobuf;
using AddressData = Akka.Remote.Serialization.Proto.Msg.AddressData;

namespace Akka.Cluster.Serialization
{
    public class ClusterMessageSerializer : Serializer
    {
        private readonly Dictionary<Type, Func<byte[], object>> _fromBinaryMap;

        public ClusterMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _fromBinaryMap = new Dictionary<Type, Func<byte[], object>>
            {
                [typeof(ClusterHeartbeatSender.Heartbeat)] = bytes => new ClusterHeartbeatSender.Heartbeat(AddressFrom(AddressData.Parser.ParseFrom(bytes))),
                [typeof(ClusterHeartbeatSender.HeartbeatRsp)] = bytes => new ClusterHeartbeatSender.HeartbeatRsp(UniqueAddressFrom(Proto.Msg.UniqueAddress.Parser.ParseFrom(bytes))),
                [typeof(GossipEnvelope)] = GossipEnvelopeFrom,
                [typeof(GossipStatus)] = GossipStatusFrom,
                [typeof(InternalClusterAction.Join)] = JoinFrom,
                [typeof(InternalClusterAction.Welcome)] = WelcomeFrom,
                [typeof(ClusterUserAction.Leave)] = bytes => new ClusterUserAction.Leave(AddressFrom(AddressData.Parser.ParseFrom(bytes))),
                [typeof(ClusterUserAction.Down)] = bytes => new ClusterUserAction.Down(AddressFrom(AddressData.Parser.ParseFrom(bytes))),
                [typeof(InternalClusterAction.InitJoin)] = bytes => InternalClusterAction.InitJoin.Instance,
                [typeof(InternalClusterAction.InitJoinAck)] = bytes => new InternalClusterAction.InitJoinAck(AddressFrom(AddressData.Parser.ParseFrom(bytes))),
                [typeof(InternalClusterAction.InitJoinNack)] = bytes => new InternalClusterAction.InitJoinNack(AddressFrom(AddressData.Parser.ParseFrom(bytes))),
                [typeof(InternalClusterAction.ExitingConfirmed)] = bytes => new InternalClusterAction.ExitingConfirmed(UniqueAddressFrom(Proto.Msg.UniqueAddress.Parser.ParseFrom(bytes))),
                [typeof(ClusterRouterPool)] = ClusterRouterPoolFrom
            };
        }

        public override bool IncludeManifest => true;

        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case ClusterHeartbeatSender.Heartbeat heartbeat:
                    return AddressToProto(heartbeat.From).ToArray();
                case ClusterHeartbeatSender.HeartbeatRsp heartbeatRsp:
                    return UniqueAddressToProto(heartbeatRsp.From).ToArray();
                case GossipEnvelope gossipEnvelope:
                    return GossipEnvelopeToProto(gossipEnvelope);
                case GossipStatus gossipStatus:
                    return GossipStatusToProto(gossipStatus);
                case InternalClusterAction.Join @join:
                    return JoinToByteArray(@join);
                case InternalClusterAction.Welcome welcome:
                    return WelcomeMessageBuilder(welcome);
                case ClusterUserAction.Leave leave:
                    return AddressToProto(leave.Address).ToArray();
                case ClusterUserAction.Down down:
                    return AddressToProto(down.Address).ToArray();
                case InternalClusterAction.InitJoin _:
                    return new Google.Protobuf.WellKnownTypes.Empty().ToArray();
                case InternalClusterAction.InitJoinAck initJoinAck:
                    return AddressToProto(initJoinAck.Address).ToArray();
                case InternalClusterAction.InitJoinNack initJoinNack:
                    return AddressToProto(initJoinNack.Address).ToArray();
                case InternalClusterAction.ExitingConfirmed exitingConfirmed:
                    return UniqueAddressToProto(exitingConfirmed.Address).ToArray();
                case ClusterRouterPool pool:
                    return ClusterRouterPoolToByteArray(pool);
                default:
                    throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{nameof(ClusterMessageSerializer)}]");
            }
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (_fromBinaryMap.TryGetValue(type, out var factory))
            {
                return factory(bytes);
            }

            throw new ArgumentException($"{nameof(ClusterMessageSerializer)} cannot deserialize object of type {type}");
        }

        //
        // Internal Cluster Action Messages
        //
        private static byte[] JoinToByteArray(InternalClusterAction.Join join)
        {
            var message = new Proto.Msg.Join
            {
                Node = UniqueAddressToProto(join.Node)
            };
            message.Roles.AddRange(join.Roles);
            return message.ToArray();
        }

        private static InternalClusterAction.Join JoinFrom(byte[] bytes)
        {
            var join = Proto.Msg.Join.Parser.ParseFrom(bytes);
            return new InternalClusterAction.Join(UniqueAddressFrom(join.Node), join.Roles.ToImmutableHashSet());
        }

        private static byte[] WelcomeMessageBuilder(InternalClusterAction.Welcome welcome)
        {
            var welcomeProto = new Proto.Msg.Welcome
            {
                From = UniqueAddressToProto(welcome.From),
                Gossip = GossipToProto(welcome.Gossip)
            };
            return welcomeProto.ToArray();
        }

        private static InternalClusterAction.Welcome WelcomeFrom(byte[] bytes)
        {
            var welcomeProto = Proto.Msg.Welcome.Parser.ParseFrom(bytes);
            return new InternalClusterAction.Welcome(UniqueAddressFrom(welcomeProto.From), GossipFrom(welcomeProto.Gossip));
        }

        //
        // Cluster Gossip Messages
        //
        private static byte[] GossipEnvelopeToProto(GossipEnvelope gossipEnvelope)
        {
            var message = new Proto.Msg.GossipEnvelope
            {
                From = UniqueAddressToProto(gossipEnvelope.From),
                To = UniqueAddressToProto(gossipEnvelope.To),
                SerializedGossip = ProtobufUtil.FromBytes(GossipToProto(gossipEnvelope.Gossip).ToArray())
            };

            return message.ToArray();
        }

        private static GossipEnvelope GossipEnvelopeFrom(byte[] bytes)
        {
            var gossipEnvelopeProto = Proto.Msg.GossipEnvelope.Parser.ParseFrom(bytes);

            return new GossipEnvelope(
                UniqueAddressFrom(gossipEnvelopeProto.From),
                UniqueAddressFrom(gossipEnvelopeProto.To),
                GossipFrom(Proto.Msg.Gossip.Parser.ParseFrom(gossipEnvelopeProto.SerializedGossip)));
        }

        private static byte[] GossipStatusToProto(GossipStatus gossipStatus)
        {
            var allHashes = gossipStatus.Version.Versions.Keys.Select(x => x.ToString()).ToList();
            var hashMapping = allHashes.ZipWithIndex();

            var message = new Proto.Msg.GossipStatus
            {
                From = UniqueAddressToProto(gossipStatus.From)
            };
            message.AllHashes.AddRange(allHashes);
            message.Version = VectorClockToProto(gossipStatus.Version, hashMapping);
            return message.ToArray();
        }

        private static GossipStatus GossipStatusFrom(byte[] bytes)
        {
            var gossipStatusProto = Proto.Msg.GossipStatus.Parser.ParseFrom(bytes);
            return new GossipStatus(UniqueAddressFrom(gossipStatusProto.From), VectorClockFrom(gossipStatusProto.Version, gossipStatusProto.AllHashes));
        }

        //
        // Cluster routing
        //

        private byte[] ClusterRouterPoolToByteArray(ClusterRouterPool clusterRouterPool)
        {
            var message = new Proto.Msg.ClusterRouterPool
            {
                Pool = PoolToProto(clusterRouterPool.Local),
                Settings = ClusterRouterPoolSettingsToProto(clusterRouterPool.Settings)
            };
            return message.ToArray();
        }

        private ClusterRouterPool ClusterRouterPoolFrom(byte[] bytes)
        {
            var clusterRouterPool = Proto.Msg.ClusterRouterPool.Parser.ParseFrom(bytes);
            return new ClusterRouterPool(PoolFrom(clusterRouterPool.Pool), ClusterRouterPoolSettingsFrom(clusterRouterPool.Settings));
        }

        private Proto.Msg.Pool PoolToProto(Akka.Routing.Pool pool)
        {
            var message = new Proto.Msg.Pool();
            var serializer = system.Serialization.FindSerializerFor(pool);
            message.SerializerId = (uint)serializer.Identifier;
            message.Data = serializer.ToByteString(pool);
            #region ## 苦竹 修改 ##
            //message.Manifest = GetObjectManifest(serializer, pool);
            if (serializer is SerializerWithStringManifest manifestSerializer)
            {
                message.IsSerializerWithStringManifest = true;
                var manifest = manifestSerializer.ManifestBytes(pool);
                if (manifest != null)
                {
                    message.HasManifest = true;
                    message.Manifest = ProtobufUtil.FromBytes(manifest);
                }
                else
                {
                    message.Manifest = ByteString.Empty;
                }
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    message.HasManifest = true;
                    var typeKey = TypeSerializer.GetTypeKeyFromType(typeof(Akka.Routing.Pool));
                    message.TypeHashCode = typeKey.HashCode;
                    message.Manifest = ProtobufUtil.FromBytes(typeKey.TypeName);
                }
            }
            #endregion
            return message;
        }

        private Akka.Routing.Pool PoolFrom(Proto.Msg.Pool poolProto)
        {
            if (poolProto.IsSerializerWithStringManifest)
            {
                return (Akka.Routing.Pool)system.Serialization.Deserialize(ProtobufUtil.GetBuffer(poolProto.Data), (int)poolProto.SerializerId, poolProto.HasManifest ? poolProto.Manifest.ToStringUtf8() : null);
            }
            else if (poolProto.HasManifest)
            {
                return (Akka.Routing.Pool)system.Serialization.Deserialize(ProtobufUtil.GetBuffer(poolProto.Data), (int)poolProto.SerializerId, ProtobufUtil.GetBuffer(poolProto.Manifest), poolProto.TypeHashCode);
            }
            else
            {
                return (Akka.Routing.Pool)system.Serialization.Deserialize(ProtobufUtil.GetBuffer(poolProto.Data), (int)poolProto.SerializerId);
            }
        }

        private static Proto.Msg.ClusterRouterPoolSettings ClusterRouterPoolSettingsToProto(ClusterRouterPoolSettings clusterRouterPoolSettings)
        {
            var message = new Proto.Msg.ClusterRouterPoolSettings
            {
                TotalInstances = (uint)clusterRouterPoolSettings.TotalInstances,
                MaxInstancesPerNode = (uint)clusterRouterPoolSettings.MaxInstancesPerNode,
                AllowLocalRoutees = clusterRouterPoolSettings.AllowLocalRoutees,
                UseRole = clusterRouterPoolSettings.UseRole ?? string.Empty
            };
            return message;
        }

        private static ClusterRouterPoolSettings ClusterRouterPoolSettingsFrom(Proto.Msg.ClusterRouterPoolSettings clusterRouterPoolSettingsProto)
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

        private static Proto.Msg.Gossip GossipToProto(Gossip gossip)
        {
            var allMembers = gossip.Members.ToList();
            var allAddresses = gossip.Members.Select(x => x.UniqueAddress).ToList();
            var addressMapping = allAddresses.ZipWithIndex();
            var allRoles = allMembers.Aggregate(ImmutableHashSet.Create<string>(), (set, member) => set.Union(member.Roles));
            var roleMapping = allRoles.ZipWithIndex();
            var allHashes = gossip.Version.Versions.Keys.Select(x => x.ToString()).ToList();
            var hashMapping = allHashes.ZipWithIndex();

            int MapUniqueAddress(UniqueAddress address) => MapWithErrorMessage(addressMapping, address, "address");

            Proto.Msg.Member MemberToProto(Member m)
            {
                var protoMember = new Proto.Msg.Member
                {
                    AddressIndex = MapUniqueAddress(m.UniqueAddress),
                    UpNumber = m.UpNumber,
                    Status = (Proto.Msg.Member.Types.MemberStatus)m.Status
                };
                protoMember.RolesIndexes.AddRange(m.Roles.Select(s => MapWithErrorMessage(roleMapping, s, "role")));
                return protoMember;
            }

            var reachabilityProto = ReachabilityToProto(gossip.Overview.Reachability, addressMapping);
            var membersProtos = gossip.Members.Select((Func<Member, Proto.Msg.Member>)MemberToProto);
            var seenProtos = gossip.Overview.Seen.Select((Func<UniqueAddress, int>)MapUniqueAddress);

            var overview = new Proto.Msg.GossipOverview();
            overview.Seen.AddRange(seenProtos);
            overview.ObserverReachability.AddRange(reachabilityProto);

            var message = new Proto.Msg.Gossip();
            message.AllAddresses.AddRange(allAddresses.Select(UniqueAddressToProto));
            message.AllRoles.AddRange(allRoles);
            message.AllHashes.AddRange(allHashes);
            message.Members.AddRange(membersProtos);
            message.Overview = overview;
            message.Version = VectorClockToProto(gossip.Version, hashMapping);
            return message;
        }

        private static Gossip GossipFrom(Proto.Msg.Gossip gossip)
        {
            var addressMapping = gossip.AllAddresses.Select(UniqueAddressFrom).ToList();
            var roleMapping = gossip.AllRoles.ToList();
            var hashMapping = gossip.AllHashes.ToList();

            Member MemberFromProto(Proto.Msg.Member member) =>
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

        private static IEnumerable<Proto.Msg.ObserverReachability> ReachabilityToProto(Reachability reachability, Dictionary<UniqueAddress, int> addressMapping)
        {
            var builderList = new List<Proto.Msg.ObserverReachability>();
            foreach (var version in reachability.Versions)
            {
                var subjectReachability = reachability.RecordsFrom(version.Key).Select(
                    r =>
                    {
                        var sr = new Proto.Msg.SubjectReachability
                        {
                            AddressIndex = MapWithErrorMessage(addressMapping, r.Subject, "address"),
                            Status = (Proto.Msg.SubjectReachability.Types.ReachabilityStatus)r.Status,
                            Version = r.Version
                        };
                        return sr;
                    });

                var observerReachability = new Proto.Msg.ObserverReachability
                {
                    AddressIndex = MapWithErrorMessage(addressMapping, version.Key, "address"),
                    Version = version.Value
                };
                observerReachability.SubjectReachability.AddRange(subjectReachability);
                builderList.Add(observerReachability);
            }
            return builderList;
        }

        private static Reachability ReachabilityFromProto(IEnumerable<Proto.Msg.ObserverReachability> reachabilityProto, List<UniqueAddress> addressMapping)
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

        private static Proto.Msg.VectorClock VectorClockToProto(VectorClock vectorClock, Dictionary<string, int> hashMapping)
        {
            var message = new Proto.Msg.VectorClock();

            foreach (var clock in vectorClock.Versions)
            {
                var version = new Proto.Msg.VectorClock.Types.Version
                {
                    HashIndex = MapWithErrorMessage(hashMapping, clock.Key.ToString(), "hash"),
                    Timestamp = clock.Value
                };
                message.Versions.Add(version);
            }
            message.Timestamp = 0L;

            return message;
        }

        private static VectorClock VectorClockFrom(Proto.Msg.VectorClock version, IList<string> hashMapping)
        {
            return VectorClock.Create(version.Versions.ToImmutableSortedDictionary(version1 =>
                    VectorClock.Node.FromHash(hashMapping[version1.HashIndex]), version1 => version1.Timestamp));
        }

        private static int MapWithErrorMessage<T>(Dictionary<T, int> map, T value, string unknown)
        {
            if (map.TryGetValue(value, out int mapIndex))
                return mapIndex;

            throw new ArgumentException($"Unknown {unknown} [{value}] in cluster message");
        }

        //
        // Address
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AddressData AddressToProto(Address address)
        {
            var message = new AddressData
            {
                System = address.System,
                Hostname = address.Host,
                Port = (uint)(address.Port ?? 0),
                Protocol = address.Protocol
            };
            return message;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Proto.Msg.UniqueAddress UniqueAddressToProto(UniqueAddress uniqueAddress)
        {
            var message = new Proto.Msg.UniqueAddress
            {
                Address = AddressToProto(uniqueAddress.Address),
                Uid = (uint)uniqueAddress.Uid
            };
            return message;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UniqueAddress UniqueAddressFrom(Proto.Msg.UniqueAddress uniqueAddressProto)
        {
            return new UniqueAddress(AddressFrom(uniqueAddressProto.Address), (int)uniqueAddressProto.Uid);
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
