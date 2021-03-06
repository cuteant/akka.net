﻿//-----------------------------------------------------------------------
// <copyright file="PersistentShardCoordinator.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Cluster.Sharding
{
    using ShardId = String;

    /// <summary>
    /// Singleton coordinator that decides where shards should be allocated.
    /// </summary>
    internal sealed class PersistentShardCoordinator : PersistentActor, IShardCoordinator
    {
        #region State data type definition

        /// <summary>
        /// Persistent state of the event sourced PersistentShardCoordinator.
        /// </summary>
        public sealed class State : IClusterShardingSerializable
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly State Empty = new State();

            /// <summary>
            /// Region for each shard.
            /// </summary>
            public readonly IImmutableDictionary<ShardId, IActorRef> Shards;

            /// <summary>
            /// Shards for each region.
            /// </summary>
            public readonly IImmutableDictionary<IActorRef, IImmutableList<ShardId>> Regions;
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IImmutableSet<IActorRef> RegionProxies;
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IImmutableSet<ShardId> UnallocatedShards;

            public readonly bool RememberEntities;

            private State() : this(
                shards: ImmutableDictionary<ShardId, IActorRef>.Empty,
                regions: ImmutableDictionary<IActorRef, IImmutableList<ShardId>>.Empty,
                regionProxies: ImmutableHashSet<IActorRef>.Empty,
                unallocatedShards: ImmutableHashSet<ShardId>.Empty,
                rememberEntities: false)
            { }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shards">TBD</param>
            /// <param name="regions">TBD</param>
            /// <param name="regionProxies">TBD</param>
            /// <param name="unallocatedShards">TBD</param>
            /// <param name="rememberEntities">TBD</param>
            public State(
                IImmutableDictionary<ShardId, IActorRef> shards,
                IImmutableDictionary<IActorRef, IImmutableList<ShardId>> regions,
                IImmutableSet<IActorRef> regionProxies,
                IImmutableSet<ShardId> unallocatedShards,
                bool rememberEntities = false)
            {
                Shards = shards;
                Regions = regions;
                RegionProxies = regionProxies;
                UnallocatedShards = unallocatedShards;
                RememberEntities = rememberEntities;
            }

            public State WithRememberEntities(bool enabled)
            {
                if (enabled)
                    return Copy(rememberEntities: enabled);
                else
                    return Copy(unallocatedShards: ImmutableHashSet<ShardId>.Empty, rememberEntities: enabled);
            }

            public bool IsEmpty
            {
                get
                {
                    return Shards.IsEmptyR() && Regions.IsEmptyR() && 0U >= (uint)RegionProxies.Count;
                }
            }

            public IImmutableSet<ShardId> AllShards
            {
                get
                {
                    return Shards.Keys.Union(UnallocatedShards).ToImmutableHashSet(StringComparer.Ordinal);
                }
            }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="e">TBD</param>
            /// <exception cref="ArgumentException">TBD</exception>
            /// <returns>TBD</returns>
            public State Updated(IDomainEvent e)
            {
                switch (e)
                {
                    case ShardRegionRegistered message:
                        if (Regions.ContainsKey(message.Region))
                            ThrowHelper.ThrowArgumentException_RegionIsAlreadyRegistered(message);

                        return Copy(regions: Regions.SetItem(message.Region, ImmutableList<ShardId>.Empty));

                    case ShardRegionProxyRegistered message:
                        if (RegionProxies.Contains(message.RegionProxy))
                            ThrowHelper.ThrowArgumentException_RegionProxyIsAlreadyRegistered(message);

                        return Copy(regionProxies: RegionProxies.Add(message.RegionProxy));

                    case ShardRegionTerminated message:
                        {
                            if (!Regions.TryGetValue(message.Region, out var shardRegions))
                                ThrowHelper.ThrowArgumentException_TerminatedRegionNotRegistered(message);

                            var newUnallocatedShards = RememberEntities ? UnallocatedShards.Union(shardRegions) : UnallocatedShards;
                            return Copy(
                                regions: Regions.Remove(message.Region),
                                shards: Shards.RemoveRange(shardRegions),
                                unallocatedShards: newUnallocatedShards);
                        }

                    case ShardRegionProxyTerminated message:
                        if (!RegionProxies.Contains(message.RegionProxy))
                            ThrowHelper.ThrowArgumentException_TerminatedRegionProxyNotRegistered(message);

                        return Copy(regionProxies: RegionProxies.Remove(message.RegionProxy));

                    case ShardHomeAllocated message:
                        {
                            if (!Regions.TryGetValue(message.Region, out var shardRegions))
                                ThrowHelper.ThrowArgumentException_RegionNotRegistered(message);
                            if (Shards.ContainsKey(message.Shard))
                                ThrowHelper.ThrowArgumentException_ShardIsAlreadyAllocated(message);

                            var newUnallocatedShards = RememberEntities ? UnallocatedShards.Remove(message.Shard) : UnallocatedShards;
                            return Copy(
                                shards: Shards.SetItem(message.Shard, message.Region),
                                regions: Regions.SetItem(message.Region, shardRegions.Add(message.Shard)),
                                unallocatedShards: newUnallocatedShards);
                        }
                    case ShardHomeDeallocated message:
                        {
                            if (!Shards.TryGetValue(message.Shard, out var region))
                                ThrowHelper.ThrowArgumentException_ShardNotAllocated(message);
                            if (!Regions.TryGetValue(region, out var shardRegions))
                                ThrowHelper.ThrowArgumentException_RegionForShardNotRegistered(region, message);

                            var newUnallocatedShards = RememberEntities ? UnallocatedShards.Add(message.Shard) : UnallocatedShards;
                            return Copy(
                                shards: Shards.Remove(message.Shard),
                                regions: Regions.SetItem(region, shardRegions.Where(s => s != message.Shard).ToImmutableList()),
                                unallocatedShards: newUnallocatedShards);
                        }
                }

                return this;
            }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shards">TBD</param>
            /// <param name="regions">TBD</param>
            /// <param name="regionProxies">TBD</param>
            /// <param name="unallocatedShards">TBD</param>
            /// <param name="rememberEntities">TBD</param>
            /// <returns>TBD</returns>
            public State Copy(IImmutableDictionary<ShardId, IActorRef> shards = null,
                IImmutableDictionary<IActorRef, IImmutableList<ShardId>> regions = null,
                IImmutableSet<IActorRef> regionProxies = null,
                IImmutableSet<ShardId> unallocatedShards = null,
                bool? rememberEntities = null)
            {
                if (shards is null && regions is null && regionProxies is null && unallocatedShards is null && rememberEntities is null) return this;

                return new State(shards ?? Shards, regions ?? Regions, regionProxies ?? RegionProxies, unallocatedShards ?? UnallocatedShards, rememberEntities ?? RememberEntities);
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as State;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shards.SequenceEqual(other.Shards)
                    && Regions.Keys.SequenceEqual(other.Regions.Keys)
                    && RegionProxies.SequenceEqual(other.RegionProxies)
                    && UnallocatedShards.SequenceEqual(other.UnallocatedShards)
                    && RememberEntities.Equals(other.RememberEntities);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 13;

                    foreach (var v in Shards)
                    {
                        hashCode = (hashCode * 397) ^ (v.Key?.GetHashCode() ?? 0);
                    }

                    foreach (var v in Regions)
                    {
                        hashCode = (hashCode * 397) ^ (v.Key?.GetHashCode() ?? 0);
                    }

                    foreach (var v in RegionProxies)
                    {
                        hashCode = (hashCode * 397) ^ (v?.GetHashCode() ?? 0);
                    }

                    foreach (var v in UnallocatedShards)
                    {
                        hashCode = (hashCode * 397) ^ (v?.GetHashCode() ?? 0);
                    }

                    return hashCode;
                }
            }

            #endregion
        }

        #endregion

        #region Message types

        /// <summary>
        /// Messages sent to the coordinator.
        /// </summary>
        public interface ICoordinatorCommand : IClusterShardingSerializable { }

        /// <summary>
        /// Messages sent from the coordinator.
        /// </summary>
        public interface ICoordinatorMessage : IClusterShardingSerializable { }

        /// <summary>
        /// <see cref="Sharding.ShardRegion"/> registers to <see cref="PersistentShardCoordinator"/>, until it receives <see cref="RegisterAck"/>.
        /// </summary>
        public sealed class Register : ICoordinatorCommand, IDeadLetterSuppression
        {
            /// <summary>
            /// Reference to a shard region actor.
            /// </summary>
            public readonly IActorRef ShardRegion;

            /// <summary>
            /// Creates a new <see cref="Register"/> request for a given <paramref name="shardRegion"/>.
            /// </summary>
            /// <param name="shardRegion">TBD</param>
            public Register(IActorRef shardRegion)
            {
                ShardRegion = shardRegion;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as Register;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return ShardRegion.Equals(other.ShardRegion);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return ShardRegion?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"Register({ShardRegion})";

            #endregion
        }

        /// <summary>
        /// <see cref="ShardRegion"/> in proxy only mode registers to <see cref="PersistentShardCoordinator"/>, until it receives <see cref="RegisterAck"/>.
        /// </summary>
        public sealed class RegisterProxy : ICoordinatorCommand, IDeadLetterSuppression
        {
            /// <summary>
            /// Reference to a shard region proxy actor.
            /// </summary>
            public readonly IActorRef ShardRegionProxy;

            /// <summary>
            /// Creates a new <see cref="RegisterProxy"/> request for a given <paramref name="shardRegionProxy"/>.
            /// </summary>
            /// <param name="shardRegionProxy">TBD</param>
            public RegisterProxy(IActorRef shardRegionProxy)
            {
                ShardRegionProxy = shardRegionProxy;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as RegisterProxy;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return ShardRegionProxy.Equals(other.ShardRegionProxy);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return ShardRegionProxy?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"RegisterProxy({ShardRegionProxy})";

            #endregion
        }

        /// <summary>
        /// Acknowledgement from <see cref="PersistentShardCoordinator"/> that <see cref="Register"/> or <see cref="RegisterProxy"/> was successful.
        /// </summary>
        public sealed class RegisterAck : ICoordinatorMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef Coordinator;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="coordinator">TBD</param>
            public RegisterAck(IActorRef coordinator)
            {
                Coordinator = coordinator;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as RegisterAck;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Coordinator.Equals(other.Coordinator);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Coordinator?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"RegisterAck({Coordinator})";

            #endregion
        }

        /// <summary>
        /// <see cref="ShardRegion"/> requests the location of a shard by sending this message
        /// to the <see cref="PersistentShardCoordinator"/>.
        /// </summary>
        public sealed class GetShardHome : ICoordinatorCommand, IDeadLetterSuppression
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public GetShardHome(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as GetShardHome;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"GetShardHome({Shard})";

            #endregion
        }

        /// <summary>
        /// <see cref="PersistentShardCoordinator"/> replies with this message for <see cref="GetShardHome"/> requests.
        /// </summary>
        public sealed class ShardHome : ICoordinatorMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef Ref;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            /// <param name="ref">TBD</param>
            public ShardHome(string shard, IActorRef @ref)
            {
                Shard = shard;
                Ref = @ref;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardHome;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard)
                    && Ref.Equals(other.Ref);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Shard?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ (Ref?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardHome(shardId:{Shard}, ref:{Ref})";

            #endregion
        }

        /// <summary>
        /// <see cref="PersistentShardCoordinator"/> informs a <see cref="ShardRegion"/> that it is hosting this shard
        /// </summary>
        public sealed class HostShard : ICoordinatorMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public HostShard(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as HostShard;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"HostShard({Shard})";

            #endregion
        }

        /// <summary>
        /// <see cref="ShardRegion"/> replies with this message for <see cref="HostShard"/> requests which lead to it hosting the shard
        /// </summary>
        public sealed class ShardStarted : ICoordinatorMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public ShardStarted(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardStarted;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardStarted({Shard})";

            #endregion
        }

        /// <summary>
        /// <see cref="PersistentShardCoordinator" /> initiates rebalancing process by sending this message
        /// to all registered <see cref="ShardRegion" /> actors (including proxy only). They are
        /// supposed to discard their known location of the shard, i.e. start buffering
        /// incoming messages for the shard. They reply with <see cref="BeginHandOffAck" />.
        /// When all have replied the <see cref="PersistentShardCoordinator" /> continues by sending
        /// <see cref="HandOff" /> to the <see cref="ShardRegion" /> responsible for the shard.
        /// </summary>
        public sealed class BeginHandOff : ICoordinatorMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public BeginHandOff(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as BeginHandOff;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"BeginHandOff({Shard})";

            #endregion
        }

        /// <summary>
        /// Acknowledgement of <see cref="BeginHandOff"/>
        /// </summary>
        public sealed class BeginHandOffAck : ICoordinatorCommand
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public BeginHandOffAck(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as BeginHandOffAck;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"BeginHandOffAck({Shard})";

            #endregion
        }

        /// <summary>
        /// When all <see cref="ShardRegion"/> actors have acknowledged the <see cref="BeginHandOff"/> the
        /// <see cref="PersistentShardCoordinator"/> sends this message to the <see cref="ShardRegion"/> responsible for the
        /// shard. The <see cref="ShardRegion"/> is supposed to stop all entries in that shard and when
        /// all entries have terminated reply with <see cref="ShardStopped"/> to the <see cref="PersistentShardCoordinator"/>.
        /// </summary>
        public sealed class HandOff : ICoordinatorMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public HandOff(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as HandOff;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"HandOff({Shard})";

            #endregion
        }

        /// <summary>
        /// Reply to <see cref="HandOff"/> when all entries in the shard have been terminated.
        /// </summary>
        public sealed class ShardStopped : ICoordinatorCommand
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public ShardStopped(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardStopped;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardStopped({Shard})";

            #endregion
        }

        /// <summary>
        /// Result of <see cref="IShardAllocationStrategy.AllocateShard(IActorRef, ShardId, IImmutableDictionary{IActorRef, IImmutableList{ShardId}})"/> is piped to self with this message.
        /// </summary>
        [MessagePackObject]
        public sealed class AllocateShardResult
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly ShardId Shard;
            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public readonly IActorRef ShardRegion; // option
            /// <summary>
            /// TBD
            /// </summary>
            [Key(2)]
            public readonly IActorRef GetShardHomeSender;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            /// <param name="shardRegion">TBD</param>
            /// <param name="getShardHomeSender">TBD</param>
            [SerializationConstructor]
            public AllocateShardResult(string shard, IActorRef shardRegion, IActorRef getShardHomeSender)
            {
                Shard = shard;
                ShardRegion = shardRegion;
                GetShardHomeSender = getShardHomeSender;
            }

            /// <inheritdoc/>
            public override string ToString() => $"AllocateShardResult(shard:{Shard}, shardRegion:{ShardRegion}, sender:{GetShardHomeSender})";
        }

        /// <summary>
        /// Result of <see cref="IShardAllocationStrategy.Rebalance(IImmutableDictionary{IActorRef, IImmutableList{ShardId}}, IImmutableSet{ShardId})"/> is piped to self with this message.
        /// </summary>
        [MessagePackObject]
        public sealed class RebalanceResult
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly IImmutableSet<ShardId> Shards;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shards">TBD</param>
            public RebalanceResult(IImmutableSet<string> shards)
            {
                Shards = shards;
            }

            /// <inheritdoc/>
            public override string ToString() => $"RebalanceResult({string.Join(", ", Shards)})";
        }

        /// <summary>
        /// <see cref="Sharding.ShardRegion"/> requests full handoff to be able to shutdown gracefully.
        /// </summary>
        public sealed class GracefulShutdownRequest : ICoordinatorCommand
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef ShardRegion;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shardRegion">TBD</param>
            public GracefulShutdownRequest(IActorRef shardRegion)
            {
                ShardRegion = shardRegion;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as GracefulShutdownRequest;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return ShardRegion.Equals(other.ShardRegion);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return ShardRegion?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"GracefulShutdownRequest({ShardRegion})";

            #endregion
        }

        /// <summary>
        /// DomainEvents for the persistent state of the event sourced PersistentShardCoordinator
        /// </summary>
        public interface IDomainEvent : IClusterShardingSerializable { }

        /// <summary>
        /// TBD
        /// </summary>
        public class ShardRegionRegistered : IDomainEvent
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef Region;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="region">TBD</param>
            public ShardRegionRegistered(IActorRef region)
            {
                Region = region;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardRegionRegistered;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Region.Equals(other.Region);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Region?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardRegionRegistered({Region})";

            #endregion
        }

        /// <summary>
        /// TBD
        /// </summary>
        public class ShardRegionProxyRegistered : IDomainEvent
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef RegionProxy;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="regionProxy">TBD</param>
            public ShardRegionProxyRegistered(IActorRef regionProxy)
            {
                RegionProxy = regionProxy;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardRegionProxyRegistered;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return RegionProxy.Equals(other.RegionProxy);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return RegionProxy?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardRegionProxyRegistered({RegionProxy})";

            #endregion
        }

        /// <summary>
        /// TBD
        /// </summary>
        public class ShardRegionTerminated : IDomainEvent
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef Region;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="region">TBD</param>
            public ShardRegionTerminated(IActorRef region)
            {
                Region = region;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardRegionTerminated;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Region.Equals(other.Region);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Region?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardRegionTerminated({Region})";

            #endregion
        }

        /// <summary>
        /// TBD
        /// </summary>
        public class ShardRegionProxyTerminated : IDomainEvent
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef RegionProxy;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="regionProxy">TBD</param>
            public ShardRegionProxyTerminated(IActorRef regionProxy)
            {
                RegionProxy = regionProxy;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardRegionProxyTerminated;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return RegionProxy.Equals(other.RegionProxy);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return RegionProxy?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardRegionProxyTerminated({RegionProxy})";

            #endregion
        }

        /// <summary>
        /// TBD
        /// </summary>
        public class ShardHomeAllocated : IDomainEvent
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;
            /// <summary>
            /// TBD
            /// </summary>
            public readonly IActorRef Region;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            /// <param name="region">TBD</param>
            public ShardHomeAllocated(string shard, IActorRef region)
            {
                Shard = shard;
                Region = region;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardHomeAllocated;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard)
                    && Region.Equals(other.Region);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Shard?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ (Region?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardHomeAllocated(shard:{Shard}, region:{Region})";

            #endregion
        }

        /// <summary>
        /// TBD
        /// </summary>
        public class ShardHomeDeallocated : IDomainEvent
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ShardId Shard;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shard">TBD</param>
            public ShardHomeDeallocated(string shard)
            {
                Shard = shard;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardHomeDeallocated;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Shard.Equals(other.Shard);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return Shard?.GetHashCode() ?? 0;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => $"ShardHomeDeallocated({Shard})";

            #endregion
        }

        /// <summary>
        /// TBD
        /// </summary>
        public sealed class StateInitialized : ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly StateInitialized Instance = new StateInitialized();
            private StateInitialized() { }
        }

        #endregion

        /// <summary>
        /// Factory method for the <see cref="Actor.Props"/> of the <see cref="PersistentShardCoordinator"/> actor.
        /// </summary>
        /// <param name="typeName">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="allocationStrategy">TBD</param>
        /// <returns>TBD</returns>
        internal static Props Props(string typeName, ClusterShardingSettings settings, IShardAllocationStrategy allocationStrategy) =>
            Actor.Props.Create(() => new PersistentShardCoordinator(typeName, settings, allocationStrategy)).WithDeploy(Deploy.Local);

        public Cluster Cluster { get; } = Cluster.Get(Context.System);

        public readonly int MinMembers;

        private bool _allRegionsRegistered = false;

        /// <summary>
        /// TBD
        /// </summary>
        public readonly string TypeName;

        ILoggingAdapter IShardCoordinator.Log => this.Log;
        public ImmutableDictionary<string, ICancelable> UnAckedHostShards { get; set; } = ImmutableDictionary<string, ICancelable>.Empty;
        public ImmutableDictionary<string, ImmutableHashSet<IActorRef>> RebalanceInProgress { get; set; } = ImmutableDictionary<string, ImmutableHashSet<IActorRef>>.Empty;
        // regions that have requested handoff, for graceful shutdown
        public ImmutableHashSet<IActorRef> GracefullShutdownInProgress { get; set; } = ImmutableHashSet<IActorRef>.Empty;
        public ImmutableHashSet<IActorRef> AliveRegions { get; set; } = ImmutableHashSet<IActorRef>.Empty;
        public ImmutableHashSet<IActorRef> RegionTerminationInProgress { get; set; } = ImmutableHashSet<IActorRef>.Empty;
        public State CurrentState { get; set; }
        public ClusterShardingSettings Settings { get; }
        public IShardAllocationStrategy AllocationStrategy { get; }
        public ICancelable RebalanceTask { get; }

        IActorRef IShardCoordinator.Self => Self;
        IActorRef IShardCoordinator.Sender => Sender;
        IActorContext IShardCoordinator.Context => Context;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="typeName">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="allocationStrategy">TBD</param>
        public PersistentShardCoordinator(string typeName, ClusterShardingSettings settings, IShardAllocationStrategy allocationStrategy)
        {
            TypeName = typeName;
            Settings = settings;
            PersistenceId = Self.Path.ToStringWithoutAddress();

            //Log = Context.GetLogger();
            CurrentState = State.Empty.WithRememberEntities(settings.RememberEntities);

            AllocationStrategy = allocationStrategy;
            RemovalMargin = Cluster.DowningProvider.DownRemovalMargin;

            if (string.IsNullOrEmpty(settings.Role))
                MinMembers = Cluster.Settings.MinNrOfMembers;
            else
                MinMembers = Cluster.Settings.MinNrOfMembersOfRole.GetValueOrDefault(settings.Role, 1);

            JournalPluginId = Settings.JournalPluginId;
            SnapshotPluginId = Settings.SnapshotPluginId;

            RebalanceTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(Settings.TunningParameters.RebalanceInterval, Settings.TunningParameters.RebalanceInterval, Self, RebalanceTick.Instance, Self);

            Cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsEvents, new[] { typeof(ClusterEvent.ClusterShuttingDown) });
        }


        #region persistent part

        /// <summary>
        /// TBD
        /// </summary>
        public override String PersistenceId { get; }

        protected override void PostStop()
        {
            base.PostStop();
            this.Cancel();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool ReceiveRecover(object message)
        {
            switch (message)
            {
                case IDomainEvent evt:
#if DEBUG
                    if (Log.IsDebugEnabled) Log.ReceiveRecover(evt);
#endif

                    switch (evt)
                    {
                        case ShardRegionRegistered _:
                            CurrentState = CurrentState.Updated(evt);
                            return true;
                        case ShardRegionProxyRegistered _:
                            CurrentState = CurrentState.Updated(evt);
                            return true;
                        case ShardRegionTerminated regionTerminated:
                            if (CurrentState.Regions.ContainsKey(regionTerminated.Region))
                            {
                                CurrentState = CurrentState.Updated(evt);
                            }
#if DEBUG
                            else
                            {
                                if (Log.IsDebugEnabled) Log.ShardRegionTerminatedButRegionWasNotRegistered(regionTerminated);
                            }
#endif

                            return true;
                        case ShardRegionProxyTerminated proxyTerminated:
                            if (CurrentState.RegionProxies.Contains(proxyTerminated.RegionProxy))
                                CurrentState = CurrentState.Updated(evt);
                            return true;
                        case ShardHomeAllocated _:
                            CurrentState = CurrentState.Updated(evt);
                            return true;
                        case ShardHomeDeallocated _:
                            CurrentState = CurrentState.Updated(evt);
                            return true;
                    }
                    return false;
                case SnapshotOffer offer when offer.Snapshot is State state:
#if DEBUG
                    if (Log.IsDebugEnabled) Log.ReceiveRecoverSnapshotOffer(state);
#endif
                    CurrentState = state.WithRememberEntities(Settings.RememberEntities);
                    // Old versions of the state object may not have unallocatedShard set,
                    // thus it will be null.
                    if (state.UnallocatedShards is null)
                        CurrentState = CurrentState.Copy(unallocatedShards: ImmutableHashSet<ShardId>.Empty);

                    return true;

                case RecoveryCompleted _:
                    CurrentState = CurrentState.WithRememberEntities(Settings.RememberEntities);
                    this.WatchStateActors();
                    return true;
            }
            return false;
        }

        public bool HasAllRegionsRegistered()
        {
            // the check is only for startup, i.e. once all have registered we don't check more
            if (_allRegionsRegistered)
                return true;
            else
            {
                _allRegionsRegistered = AliveRegions.Count >= MinMembers;
                return _allRegionsRegistered;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool ReceiveCommand(object message)
        {
            return WaitingForStateInitialized(message);
        }

        private bool WaitingForStateInitialized(object message)
        {
            switch (message)
            {
                case Terminate _:
#if DEBUG
                    if (Log.IsDebugEnabled) { Log.Debug("Received termination message before state was initialized"); }
#endif
                    Context.Stop(Self);
                    return true;

                case StateInitialized _:
                    this.StateInitialized();
                    Context.Become(msg => this.Active(msg) || HandleSnapshotResult(msg));
                    return true;
            }

            if (this.ReceiveTerminated(message)) return true;
            else return HandleSnapshotResult(message);
        }


        private bool HandleSnapshotResult(object message)
        {
            switch (message)
            {
                case SaveSnapshotSuccess m:
#if DEBUG
                    if (Log.IsDebugEnabled) Log.PersistentSnapshotSavedSuccessfully();
#endif
                    InternalDeleteMessagesBeforeSnapshot(m, Settings.TunningParameters.KeepNrOfBatches, Settings.TunningParameters.SnapshotAfter);
                    break;

                case SaveSnapshotFailure m:
                    if (Log.IsWarningEnabled) Log.PersistentSnapshotFailure(m);
                    break;
                case DeleteMessagesSuccess m:
#if DEBUG
                    if (Log.IsDebugEnabled) Log.PersistentMessagesToDeletedSuccessfully(m);
#endif
                    DeleteSnapshots(new SnapshotSelectionCriteria(m.ToSequenceNr - 1));
                    break;
                case DeleteMessagesFailure m:
                    if (Log.IsWarningEnabled) Log.PersistentMessagesToDeletionFailure(m);
                    break;
                case DeleteSnapshotsSuccess m:
#if DEBUG
                    if (Log.IsDebugEnabled) Log.PersistentSnapshotsMatchingDeletedSuccessfully(m);
#endif
                    break;
                case DeleteSnapshotsFailure m:
                    if (Log.IsWarningEnabled) Log.PersistentSnapshotsMatchingDeletionFailure(m);
                    break;
                default:
                    return false;
            }
            return true;
        }

        public TimeSpan RemovalMargin { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="TEvent">TBD</typeparam>
        /// <param name="e">TBD</param>
        /// <param name="handler">TBD</param>
        /// <returns>TBD</returns>
        public void Update<TEvent>(TEvent e, Action<TEvent> handler) where TEvent : IDomainEvent
        {
            SaveSnapshotWhenNeeded();
            Persist(e, handler);
        }


        private void SaveSnapshotWhenNeeded()
        {
            if (LastSequenceNr % Settings.TunningParameters.SnapshotAfter == 0 && LastSequenceNr != 0)
            {
#if DEBUG
                if (Log.IsDebugEnabled) Log.SavingSnapshotSequenceNumber(SnapshotSequenceNr);
#endif
                SaveSnapshot(CurrentState);
            }
        }

        #endregion
    }
}
