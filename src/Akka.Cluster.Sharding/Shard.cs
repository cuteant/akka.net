//-----------------------------------------------------------------------
// <copyright file="Shard.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Coordination;
using Akka.Event;
using MessagePack;

namespace Akka.Cluster.Sharding
{
    using EntityId = String;
    using Msg = Object;
    using ShardId = String;

    internal interface IShard
    {
        IActorContext Context { get; }
        IActorRef Self { get; }
        IActorRef Sender { get; }
        string TypeName { get; }
        ShardId ShardId { get; }
        Func<string, Props> EntityProps { get; }
        ClusterShardingSettings Settings { get; }
        ExtractEntityId ExtractEntityId { get; }
        ExtractShardId ExtractShardId { get; }
        object HandOffStopMessage { get; }
        ILoggingAdapter Log { get; }
        IActorRef HandOffStopper { get; set; }
        Shard.ShardState State { get; set; }
        ImmutableDictionary<EntityId, IActorRef> RefById { get; set; }
        ImmutableDictionary<IActorRef, EntityId> IdByRef { get; set; }
        ImmutableDictionary<string, long> LastMessageTimestamp { get; set; }
        ImmutableHashSet<IActorRef> Passivating { get; set; }
        ImmutableDictionary<EntityId, ImmutableList<(Msg, IActorRef)>> MessageBuffers { get; set; }
        void Unhandled(object message);
        void ProcessChange<T>(T evt, Action<T> handler) where T : Shard.StateChange;
        void EntityTerminated(IActorRef tref);
        void DeliverTo(string id, object message, object payload, IActorRef sender);
        ICancelable PassivateIdleTask { get; }

        ITimerScheduler Timers { get; }
        Lease Lease { get; }
        TimeSpan LeaseRetryInterval { get; }
        /// <summary>
        /// Override to execute logic once the lease has been acquired
        /// Will be called on the actor thread
        /// </summary>
        void OnLeaseAcquired();
    }

    internal sealed class Shard : ActorBase, IShard, IWithTimers
    {
        #region internal classes

        /// <summary>
        /// Persistent state of the Shard.
        /// </summary>
        public class ShardState : IClusterShardingSerializable
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly ShardState Empty = new ShardState(ImmutableHashSet<string>.Empty);

            /// <summary>
            /// TBD
            /// </summary>
            public readonly IImmutableSet<EntityId> Entries;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="entries">TBD</param>
            public ShardState(IImmutableSet<EntityId> entries)
            {
                Entries = entries;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardState;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return Entries.SequenceEqual(other.Entries);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 13;

                    foreach (var v in Entries)
                    {
                        hashCode = (hashCode * 397) ^ (v?.GetHashCode() ?? 0);
                    }

                    return hashCode;
                }
            }

            #endregion
        }
        #endregion

        #region messages

        /// <summary>
        /// TBD
        /// </summary>
        public interface IShardCommand { }

        /// <summary>
        /// TBD
        /// </summary>
        public interface IShardQuery { }


        /// <summary>
        /// When an remembering entries and the entity stops without issuing a <see cref="Passivate"/>,
        /// we restart it after a back off using this message.
        /// </summary>
        [MessagePackObject]
        public sealed class RestartEntity : IShardCommand
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly EntityId EntityId;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="entityId">TBD</param>
            [SerializationConstructor]
            public RestartEntity(string entityId)
            {
                EntityId = entityId;
            }
        }

        /// <summary>
        /// When initialising a shard with remember entities enabled the following message is used to restart
        /// batches of entity actors at a time.
        /// </summary>
        [MessagePackObject]
        public sealed class RestartEntities : IShardCommand
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly IImmutableSet<EntityId> Entries;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="entries">TBD</param>
            [SerializationConstructor]
            public RestartEntities(IImmutableSet<EntityId> entries)
            {
                Entries = entries;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        [Union(0, typeof(EntityStarted))]
        [Union(1, typeof(EntityStopped))]
        public abstract class StateChange : IClusterShardingSerializable
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly EntityId EntityId;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="entityId">TBD</param>
            protected StateChange(EntityId entityId)
            {
                EntityId = entityId;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as StateChange;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return EntityId.Equals(other.EntityId);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return EntityId?.GetHashCode() ?? 0;
                }
            }

            #endregion
        }

        /// <summary>
        /// <see cref="ShardState"/> change for starting an entity in this `Shard`
        /// </summary>
        [MessagePackObject]
        public sealed class EntityStarted : StateChange
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="entityId">TBD</param>
            [SerializationConstructor]
            public EntityStarted(string entityId) : base(entityId)
            {
            }
        }

        /// <summary>
        /// <see cref="ShardState"/> change for an entity which has terminated.
        /// </summary>
        [MessagePackObject]
        public sealed class EntityStopped : StateChange
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="entityId">TBD</param>
            [SerializationConstructor]
            public EntityStopped(string entityId) : base(entityId)
            {
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public sealed class GetCurrentShardState : IShardQuery, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly GetCurrentShardState Instance = new GetCurrentShardState();

            private GetCurrentShardState()
            {
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class CurrentShardState
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly string ShardId;
            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public readonly IImmutableSet<string> EntityIds;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shardId">TBD</param>
            /// <param name="entityIds">TBD</param>
            [SerializationConstructor]
            public CurrentShardState(string shardId, IImmutableSet<string> entityIds)
            {
                ShardId = shardId;
                EntityIds = entityIds;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public sealed class GetShardStats : IShardQuery, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly GetShardStats Instance = new GetShardStats();

            private GetShardStats()
            {
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class ShardStats
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly string ShardId;
            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public readonly int EntityCount;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="shardId">TBD</param>
            /// <param name="entityCount">TBD</param>
            [SerializationConstructor]
            public ShardStats(string shardId, int entityCount)
            {
                ShardId = shardId;
                EntityCount = entityCount;
            }

            #region Equals

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                var other = obj as ShardStats;

                if (other is null) return false;
                if (ReferenceEquals(other, this)) return true;

                return ShardId.Equals(other.ShardId)
                    && EntityCount.Equals(other.EntityCount);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = ShardId?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ EntityCount;
                    return hashCode;
                }
            }

            #endregion
        }

        //[Serializable]
        public sealed class PassivateIdleTick : INoSerializationVerificationNeeded, ISingletonMessage
        {
            public static readonly PassivateIdleTick Instance = new PassivateIdleTick();
            private PassivateIdleTick() { }
        }

        [Serializable]
        public sealed class LeaseAcquireResult : IDeadLetterSuppression, INoSerializationVerificationNeeded
        {
            public readonly bool Acquired;

            public readonly Exception Reason;

            public LeaseAcquireResult(bool acquired, Exception reason)
            {
                Acquired = acquired;
                Reason = reason;
            }
        }

        [Serializable]
        public sealed class LeaseLost : IDeadLetterSuppression, INoSerializationVerificationNeeded
        {
            public readonly Exception Reason;

            public LeaseLost(Exception reason)
            {
                Reason = reason;
            }
        }

        [Serializable]
        public sealed class LeaseRetry : IDeadLetterSuppression, INoSerializationVerificationNeeded
        {
            public static readonly LeaseRetry Instance = new LeaseRetry();
            private LeaseRetry() { }
        }

        #endregion

        IActorContext IShard.Context => Context;
        IActorRef IShard.Self => Self;
        IActorRef IShard.Sender => Sender;
        void IShard.Unhandled(object message) => base.Unhandled(message);

        public ILoggingAdapter Log { get; } = Context.GetLogger();
        public string TypeName { get; }
        public string ShardId { get; }
        public Func<string, Props> EntityProps { get; }
        public ClusterShardingSettings Settings { get; }
        public ExtractEntityId ExtractEntityId { get; }
        public ExtractShardId ExtractShardId { get; }
        public object HandOffStopMessage { get; }
        public IActorRef HandOffStopper { get; set; }
        public ShardState State { get; set; } = ShardState.Empty;
        public ImmutableDictionary<string, IActorRef> RefById { get; set; } = ImmutableDictionary<string, IActorRef>.Empty;
        public ImmutableDictionary<IActorRef, string> IdByRef { get; set; } = ImmutableDictionary<IActorRef, string>.Empty;
        public ImmutableDictionary<string, long> LastMessageTimestamp { get; set; } = ImmutableDictionary<string, long>.Empty;
        public ImmutableHashSet<IActorRef> Passivating { get; set; } = ImmutableHashSet<IActorRef>.Empty;
        public ImmutableDictionary<string, ImmutableList<(object, IActorRef)>> MessageBuffers { get; set; } = ImmutableDictionary<string, ImmutableList<(object, IActorRef)>>.Empty;
        public ICancelable PassivateIdleTask { get; }

        private EntityRecoveryStrategy RememberedEntitiesRecoveryStrategy { get; }

        public Lease Lease { get; }
        public TimeSpan LeaseRetryInterval { get; } = TimeSpan.FromSeconds(5); // won't be used
        public ITimerScheduler Timers { get; set; }

        public Shard(
            string typeName,
            string shardId,
            Func<string, Props> entityProps,
            ClusterShardingSettings settings,
            ExtractEntityId extractEntityId,
            ExtractShardId extractShardId,
            object handOffStopMessage)
        {
            TypeName = typeName;
            ShardId = shardId;
            EntityProps = entityProps;
            Settings = settings;
            ExtractEntityId = extractEntityId;
            ExtractShardId = extractShardId;
            HandOffStopMessage = handOffStopMessage;
            RememberedEntitiesRecoveryStrategy = Settings.TunningParameters.EntityRecoveryStrategy == "constant"
                ? EntityRecoveryStrategy.ConstantStrategy(
                    Context.System,
                    Settings.TunningParameters.EntityRecoveryConstantRateStrategyFrequency,
                    Settings.TunningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities)
                : EntityRecoveryStrategy.AllStrategy;

            var idleInterval = TimeSpan.FromTicks(Settings.PassivateIdleEntityAfter.Ticks / 2);
            PassivateIdleTask = Settings.ShouldPassivateIdleEntities
                ? Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(idleInterval, idleInterval, Self, PassivateIdleTick.Instance, Self)
                : null;

            if (settings.LeaseSettings != null)
            {
                Lease = LeaseProvider.Get(Context.System).GetLease(
                    $"{Context.System.Name}-shard-{typeName}-{shardId}",
                    settings.LeaseSettings.LeaseImplementation,
                    Cluster.Get(Context.System).SelfAddress.HostPort());

                LeaseRetryInterval = settings.LeaseSettings.LeaseRetryInterval;
            }
        }

        protected override void PreStart()
        {
            this.AcquireLeaseIfNeeded();
        }

        protected override void PostStop()
        {
            this.ReleaseLeaseIfNeeded();
            PassivateIdleTask?.Cancel();
            base.PostStop();
        }

        public void OnLeaseAcquired()
        {
#if DEBUG
            if (Log.IsDebugEnabled) { Log.Debug("Shard initialized"); }
#endif
            Context.Parent.Tell(new ShardInitialized(ShardId));
            Context.UnbecomeStacked();
        }

        protected override bool Receive(object message) => this.HandleCommand(message);
        public void ProcessChange<T>(T evt, Action<T> handler) where T : StateChange => this.BaseProcessChange(evt, handler);
        public void EntityTerminated(IActorRef tref) => this.BaseEntityTerminated(tref);
        public void DeliverTo(string id, object message, object payload, IActorRef sender) => this.BaseDeliverTo(id, message, payload, sender);
    }

    internal static class Shards
    {
        #region common shard methods

        private const string LeaseRetryTimer = "lease-retry";

        /// <summary>
        /// Will call onLeaseAcquired when completed, also when lease isn't used
        /// </summary>
        /// <typeparam name="TShard"></typeparam>
        /// <param name="shard"></param>
        public static void AcquireLeaseIfNeeded<TShard>(this TShard shard) where TShard : IShard
        {
            if (shard.Lease != null)
            {
                shard.TryGetLease(shard.Lease);
                shard.Context.Become(shard.AwaitingLease());
            }
            else
                shard.OnLeaseAcquired();
        }

        public static void ReleaseLeaseIfNeeded<TShard>(this TShard shard) where TShard : IShard
        {
            if (shard.Lease != null)
            {
                shard.Lease.Release().ContinueWith(r =>
                {
                    if (r.IsFaulted || r.IsCanceled)
                    {
                        shard.Log.Error(r.Exception,
                            "Failed to release lease of shard type [{0}] id [{1}]. Shard may not be able to run on another node until lease timeout occurs.", shard.TypeName, shard.ShardId);
                    }
                    else if (r.Result)
                    {
                        shard.Log.Info("Lease of shard type [{0}] id [{1}] released.", shard.TypeName, shard.ShardId);
                    }
                    else
                    {
                        shard.Log.Error(
                            "Failed to release lease of shard type [{0}] id [{1}]. Shard may not be able to run on another node until lease timeout occurs.", shard.TypeName, shard.ShardId);
                    }
                });
            }
        }

        public static void TryGetLease<TShard>(this TShard shard, Lease lease) where TShard : IShard
        {
            shard.Log.Info("Acquiring lease {0}", lease.Settings);

            var self = shard.Self;
            lease.Acquire(reason =>
            {
                self.Tell(new Shard.LeaseLost(reason));
            }).ContinueWith(r =>
            {
                if (r.IsFaulted || r.IsCanceled)
                    return new Shard.LeaseAcquireResult(false, r.Exception);
                return new Shard.LeaseAcquireResult(r.Result, null);
            }).PipeTo(shard.Self);
        }

        public static void HandleLeaseLost<TShard>(this TShard shard, Shard.LeaseLost message) where TShard : IShard
        {
            // The shard region will re-create this when it receives a message for this shard
            shard.Log.Error(message.Reason, "Shard type [{0}] id [{1}] lease lost.", shard.TypeName, shard.ShardId);
            // Stop entities ASAP rather than send termination message
            shard.Context.Stop(shard.Self);
        }

        /// <summary>
        /// Don't send back ShardInitialized so that messages are buffered in the ShardRegion
        /// while awaiting the lease
        /// </summary>
        /// <typeparam name="TShard"></typeparam>
        /// <param name="shard"></param>
        /// <returns></returns>
        public static Actor.Receive AwaitingLease<TShard>(this TShard shard) where TShard : IShard
        {
            bool AwaitingLease(object message)
            {
                switch (message)
                {
                    case Shard.LeaseAcquireResult lar when lar.Acquired:
#if DEBUG
                        if (shard.Log.IsDebugEnabled) { shard.Log.Debug("Acquired lease"); }
#endif
                        shard.OnLeaseAcquired();
                        return true;

                    case Shard.LeaseAcquireResult lar when !lar.Acquired && lar.Reason == null:
                        shard.Log.Error(
                              "Failed to get lease for shard type [{0}] id [{1}]. Retry in {2}",
                              shard.TypeName,
                              shard.ShardId,
                              shard.LeaseRetryInterval);
                        shard.Timers.StartSingleTimer(LeaseRetryTimer, Shard.LeaseRetry.Instance, shard.LeaseRetryInterval);
                        return true;

                    case Shard.LeaseAcquireResult lar when !lar.Acquired && lar.Reason != null:
                        shard.Log.Error(
                              lar.Reason,
                              "Failed to get lease for shard type [{0}] id [{1}]. Retry in {2}",
                              shard.TypeName,
                              shard.ShardId,
                              shard.LeaseRetryInterval);
                        shard.Timers.StartSingleTimer(LeaseRetryTimer, Shard.LeaseRetry.Instance, shard.LeaseRetryInterval);
                        return true;

                    case Shard.LeaseRetry _:
                        shard.TryGetLease(shard.Lease);
                        return true;

                    case Shard.LeaseLost ll:
                        shard.HandleLeaseLost(ll);
                        return true;
                }

                return false;
            }

            return AwaitingLease;
        }

        public static void BaseProcessChange<TShard, T>(this TShard shard, T evt, Action<T> handler)
            where TShard : IShard
            where T : Shard.StateChange
        {
#if DEBUG
            if (shard.Log.IsDebugEnabled) { shard.Log.Debug("Calling BaseProcessChange for {0} and event [{1}]", shard, evt); }
#endif
            handler(evt);
        }

        public static bool HandleCommand<TShard>(this TShard shard, object message) where TShard : IShard
        {
            switch (message)
            {
                case Terminated t:
                    shard.HandleTerminated(t.ActorRef);
                    return true;
                case PersistentShardCoordinator.ICoordinatorMessage cm:
                    shard.HandleCoordinatorMessage(cm);
                    return true;
                case Shard.IShardCommand sc:
                    shard.HandleShardCommand(sc);
                    return true;
                case ShardRegion.StartEntity se:
                    shard.HandleStartEntity(se);
                    return true;
                case ShardRegion.StartEntityAck sea:
                    shard.HandleStartEntityAck(sea);
                    return true;
                case IShardRegionCommand src:
                    shard.HandleShardRegionCommand(src);
                    return true;
                case Shard.IShardQuery sq:
                    shard.HandleShardRegionQuery(sq);
                    return true;
                case ShardRegion.RestartShard _:
                    return true;
                case Shard.PassivateIdleTick _:
                    shard.PassivateIdleEntities();
                    return true;

                case Shard.LeaseLost ll:
                    shard.HandleLeaseLost(ll);
                    return true;

                case var _ when shard.ExtractEntityId(message).HasValue:
                    shard.DeliverMessage(message, shard.Context.Sender);
                    return true;
            }
            return false;
        }

        internal static void HandleShardRegionQuery<TShard>(this TShard shard, Shard.IShardQuery query) where TShard : IShard
        {
            switch (query)
            {
                case Shard.GetCurrentShardState _:
                    shard.Context.Sender.Tell(new Shard.CurrentShardState(shard.ShardId, shard.RefById.Keys.ToImmutableHashSet(StringComparer.Ordinal)));
                    break;
                case Shard.GetShardStats _:
                    shard.Context.Sender.Tell(new Shard.ShardStats(shard.ShardId, shard.State.Entries.Count));
                    break;
            }
        }

        private static void HandleShardCommand<TShard>(this TShard shard, Shard.IShardCommand message) where TShard : IShard
        {
            switch (message)
            {
                case Shard.RestartEntity restartEntity:
                    shard.GetOrCreateEntity(restartEntity.EntityId);
                    break;
                case Shard.RestartEntities restartEntities:
                    shard.HandleRestartEntities(restartEntities.Entries);
                    break;
            }
        }

        private static void HandleStartEntity<TShard>(this TShard shard, ShardRegion.StartEntity start) where TShard : IShard
        {
            var shardLog = shard.Log;
            if (shardLog.IsDebugEnabled) { shardLog.GotRequestFromToStartEntityInShard(shard, start); }
            var requester = shard.Sender;
            shard.TouchLastMessageTimestamp(start.EntityId);

            if (shard.State.Entries.Contains(start.EntityId))
            {
                shard.GetOrCreateEntity(start.EntityId);
                requester.Tell(new ShardRegion.StartEntityAck(start.EntityId, shard.ShardId));
            }
            else
            {
                shard.ProcessChange(new Shard.EntityStarted(start.EntityId), e =>
                {
#if DEBUG
                    if (shardLog.IsDebugEnabled) { shardLog.Debug("Calling process change"); }
#endif
                    shard.GetOrCreateEntity(start.EntityId);
                    shard.SendMessageBuffer(e);
                    requester.Tell(new ShardRegion.StartEntityAck(start.EntityId, shard.ShardId));
                });
            };
        }

        private static void HandleStartEntityAck<TShard>(this TShard shard, ShardRegion.StartEntityAck ack) where TShard : IShard
        {
            if (ack.ShardId != shard.ShardId && shard.State.Entries.Contains(ack.EntityId))
            {
                var shardLog = shard.Log;
                if (shardLog.IsDebugEnabled) shardLog.EntityPreviouslyOwnedByShardStartedInShard(ack, shard);
                shard.ProcessChange(new Shard.EntityStopped(ack.EntityId), _ =>
                {
                    shard.State = new Shard.ShardState(shard.State.Entries.Remove(ack.EntityId));
                    shard.MessageBuffers = shard.MessageBuffers.Remove(ack.EntityId);
                });
            }
        }

        private static void HandleRestartEntities<TShard>(this TShard shard, IImmutableSet<EntityId> ids) where TShard : IShard
        {
            shard.Context.ActorOf(RememberEntityStarter.Props(shard.Context.Parent, shard.TypeName, shard.ShardId, ids, shard.Settings, shard.Sender));
        }

        private static void HandleShardRegionCommand<TShard>(this TShard shard, IShardRegionCommand message) where TShard : IShard
        {
            if (message is Passivate passivate)
                shard.Passivate(shard.Sender, passivate.StopMessage);
            else
                shard.Unhandled(message);
        }

        private static void HandleCoordinatorMessage<TShard>(this TShard shard, PersistentShardCoordinator.ICoordinatorMessage message) where TShard : IShard
        {
            switch (message)
            {
                case PersistentShardCoordinator.HandOff handOff when handOff.Shard == shard.ShardId:
                    shard.HandOff(shard.Sender);
                    break;
                case PersistentShardCoordinator.HandOff handOff:
                    var shardLog = shard.Log;
                    if (shardLog.IsWarningEnabled) shardLog.ShardCanNotHandOffForAnotherShard(shard, handOff);
                    break;
                default:
                    shard.Unhandled(message);
                    break;
            }
        }

        private static void HandOff<TShard>(this TShard shard, IActorRef replyTo) where TShard : IShard
        {
            var shardLog = shard.Log;
            if (shard.HandOffStopper != null)
            {
                shardLog.HandOffShardReceivedDuringExistingHandOff(shard);
            }
            else
            {
                if (shardLog.IsDebugEnabled) shardLog.HandOffShard(shard);

                if (!shard.IdByRef.IsEmpty)
                {
                    var entityHandOffTimeout = (shard.Settings.TunningParameters.HandOffTimeout - TimeSpan.FromSeconds(5));
                    if (entityHandOffTimeout < TimeSpan.FromSeconds(1))
                    {
                        entityHandOffTimeout = TimeSpan.FromSeconds(1);
                    }
#if DEBUG
                    if (shardLog.IsDebugEnabled)
                    {
                        shardLog.Debug("Starting HandOffStopper for shard [{0}] to terminate [{1}] entities.",
                            shard.ShardId, shard.IdByRef.Keys.Count());
                    }
#endif
                    shard.HandOffStopper = shard.Context.Watch(shard.Context.ActorOf(
                        ShardRegion.HandOffStopper.Props(shard.ShardId, replyTo, shard.IdByRef.Keys, shard.HandOffStopMessage, entityHandOffTimeout)));

                    //During hand off we only care about watching for termination of the hand off stopper
                    shard.Context.Become(message =>
                    {
                        if (message is Terminated terminated)
                        {
                            shard.HandleTerminated(terminated.ActorRef);
                            return true;
                        }
                        return false;
                    });
                }
                else
                {
                    replyTo.Tell(new PersistentShardCoordinator.ShardStopped(shard.ShardId));
                    shard.Context.Stop(shard.Context.Self);
                }
            }
        }

        private static void HandleTerminated<TShard>(this TShard shard, IActorRef terminatedRef) where TShard : IShard
        {
            if (Equals(shard.HandOffStopper, terminatedRef))
                shard.Context.Stop(shard.Context.Self);
            else if (shard.IdByRef.ContainsKey(terminatedRef) && shard.HandOffStopper == null)
                shard.EntityTerminated(terminatedRef);
        }

        private static void Passivate<TShard>(this TShard shard, IActorRef entity, object stopMessage) where TShard : IShard
        {
            var shardLog = shard.Log;
            if (shard.IdByRef.TryGetValue(entity, out var id))
            {
                if (!shard.MessageBuffers.ContainsKey(id))
                {
                    if (shardLog.IsDebugEnabled) shardLog.PassivatingStartedOnEntity(id);

                    shard.Passivating = shard.Passivating.Add(entity);
                    shard.MessageBuffers = shard.MessageBuffers.Add(id, ImmutableList<(object, IActorRef)>.Empty);

                    entity.Tell(stopMessage);
                }
                else
                {
                    if (shardLog.IsDebugEnabled) shardLog.PassivationAlreadyInProgress(entity);
                }
            }
            else
            {
                if (shardLog.IsDebugEnabled) shardLog.UnknownEntityNotSendingStopMessageBackToEntity(entity);
            }
        }

        public static void TouchLastMessageTimestamp<TShard>(this TShard shard, EntityId id) where TShard : IShard
        {
            if (shard.PassivateIdleTask is object)
            {
                shard.LastMessageTimestamp = shard.LastMessageTimestamp.SetItem(id, DateTime.Now.Ticks);
            }
        }

        private static void PassivateIdleEntities<TShard>(this TShard shard) where TShard : IShard
        {
            var idleEntitiesCount = 0;
            var deadline = DateTime.Now.Ticks - shard.Settings.PassivateIdleEntityAfter.Ticks;
            foreach (var pair in shard.LastMessageTimestamp)
            {
                if (pair.Value >= deadline) continue;
                Passivate(shard, shard.RefById[pair.Key], shard.HandOffStopMessage);
                idleEntitiesCount++;
            }

            var shardLog = shard.Log;
            if (shardLog.IsDebugEnabled) shardLog.PassivatingIdleEntities(idleEntitiesCount);
        }

        public static void PassivateCompleted<TShard>(this TShard shard, Shard.EntityStopped evt) where TShard : IShard
        {
            var shardLog = shard.Log;
            if (shardLog.IsDebugEnabled) shardLog.EntityStoppedAfterPassivation(evt);
            shard.State = new Shard.ShardState(shard.State.Entries.Remove(evt.EntityId));
            shard.MessageBuffers = shard.MessageBuffers.Remove(evt.EntityId);
        }

        public static void SendMessageBuffer<TShard>(this TShard shard, Shard.EntityStarted message) where TShard : IShard
        {
            var id = message.EntityId;

            // Get the buffered messages and remove the buffer
            if (shard.MessageBuffers.TryGetValue(id, out var buffer))
            {
                shard.MessageBuffers = shard.MessageBuffers.Remove(id);

                if (buffer.Count != 0)
                {
                    var shardLog = shard.Log;
                    if (shardLog.IsDebugEnabled) shardLog.SendingMessageBufferForEntity(id, buffer.Count);

                    shard.GetOrCreateEntity(id);

                    // Now there is no deliveryBuffer we can try to redeliver
                    // and as the child exists, the message will be directly forwarded
                    foreach (var pair in buffer)
                        shard.DeliverMessage(pair.Item1, pair.Item2);
                }
            }
        }

        internal static void DeliverMessage<TShard>(this TShard shard, object message, IActorRef sender) where TShard : IShard
        {
            var t = shard.ExtractEntityId(message);
            var id = t.Value.Item1;
            var payload = t.Value.Item2;

            var shardLog = shard.Log;
            if (string.IsNullOrEmpty(id))
            {
                if (shardLog.IsWarningEnabled) shardLog.IdMustNotBeEmptyDroppingMessage(message);
                shard.Context.System.DeadLetters.Tell(message);
            }
            else
            {
                if (payload is ShardRegion.StartEntity start)
                {
                    shard.HandleStartEntity(start);
                }
                else
                {
                    if (shard.MessageBuffers.TryGetValue(id, out var buffer))
                    {
                        if (shard.TotalBufferSize() >= shard.Settings.TunningParameters.BufferSize)
                        {
                            if (shardLog.IsWarningEnabled) shardLog.BufferIsFullDroppingMessageForEntity(id);
                            shard.Context.System.DeadLetters.Tell(message);
                        }
                        else
                        {
                            if (shardLog.IsDebugEnabled) shardLog.MessageForEntityBuffered(id);
                            shard.MessageBuffers = shard.MessageBuffers.SetItem(id, buffer.Add((message, sender)));
                        }
                    }
                    else
                    {
                        shard.DeliverTo(id, message, payload, sender);
                    }
                }
            }
        }

        public static void BaseEntityTerminated<TShard>(this TShard shard, IActorRef tref) where TShard : IShard
        {
            if (!shard.IdByRef.TryGetValue(tref, out var id)) { return; }
            shard.IdByRef = shard.IdByRef.Remove(tref);
            shard.RefById = shard.RefById.Remove(id);

            if (shard.PassivateIdleTask != null)
            {
                shard.LastMessageTimestamp = shard.LastMessageTimestamp.Remove(id);
            }

            if (shard.MessageBuffers.TryGetValue(id, out var buffer) && buffer.Count != 0)
            {
                var shardLog = shard.Log;
                if (shardLog.IsDebugEnabled) shardLog.StartingEntityAgainThereAreBufferedMessagesForIt(id);
                shard.SendMessageBuffer(new Shard.EntityStarted(id));
            }
            else
            {
                shard.ProcessChange(new Shard.EntityStopped(id), stopped => shard.PassivateCompleted(stopped));
            }

            shard.Passivating = shard.Passivating.Remove(tref);
        }

        internal static void BaseDeliverTo<TShard>(this TShard shard, string id, object message, object payload, IActorRef sender) where TShard : IShard
        {
            shard.TouchLastMessageTimestamp(id);
            shard.GetOrCreateEntity(id).Tell(payload, sender);
        }

        internal static IActorRef GetOrCreateEntity<TShard>(this TShard shard, string id, Action<IActorRef> onCreate = null) where TShard : IShard
        {
            var name = Uri.EscapeDataString(id);
            var child = shard.Context.Child(name).GetOrElse(() =>
            {
                var shardLog = shard.Log;
                if (shardLog.IsDebugEnabled) shardLog.StartingEntityInShard(id, shard);

                var a = shard.Context.Watch(shard.Context.ActorOf(shard.EntityProps(id), name));
                shard.IdByRef = shard.IdByRef.SetItem(a, id);
                shard.RefById = shard.RefById.SetItem(id, a);
                shard.TouchLastMessageTimestamp(id);
                shard.State = new Shard.ShardState(shard.State.Entries.Add(id));
                onCreate?.Invoke(a);
                return a;
            });

            return child;
        }

        internal static int TotalBufferSize<TShard>(this TShard shard) where TShard : IShard =>
            shard.MessageBuffers.Aggregate(0, (sum, entity) => sum + entity.Value.Count);

        #endregion

        public static Props Props(string typeName, ShardId shardId, Func<string, Props> entityProps, ClusterShardingSettings settings, ExtractEntityId extractEntityId, ExtractShardId extractShardId, object handOffStopMessage, IActorRef replicator, int majorityMinCap)
        {
            switch (settings.StateStoreMode)
            {
                case StateStoreMode.Persistence when settings.RememberEntities:
                    return Actor.Props.Create(() => new PersistentShard(typeName, shardId, entityProps, settings, extractEntityId, extractShardId, handOffStopMessage)).WithDeploy(Deploy.Local);
                case StateStoreMode.DData when settings.RememberEntities:
                    return Actor.Props.Create(() => new DDataShard(typeName, shardId, entityProps, settings, extractEntityId, extractShardId, handOffStopMessage, replicator, majorityMinCap)).WithDeploy(Deploy.Local);
                default:
                    return Actor.Props.Create(() => new Shard(typeName, shardId, entityProps, settings, extractEntityId, extractShardId, handOffStopMessage)).WithDeploy(Deploy.Local);
            }
        }
    }

    class RememberEntityStarter : ActorBase
    {
        private class Tick : INoSerializationVerificationNeeded, ISingletonMessage
        {
            public static readonly Tick Instance = new Tick();
            private Tick()
            {
            }
        }


        public static Props Props(
          IActorRef region,
          string typeName,
          ShardId shardId,
          IImmutableSet<EntityId> ids,
          ClusterShardingSettings settings,
          IActorRef requestor)
        {
            return Actor.Props.Create(() => new RememberEntityStarter(region, typeName, shardId, ids, settings, requestor));
        }


        private readonly IActorRef _region;
        private readonly string _typeName;
        private readonly ShardId _shardId;
        private readonly IImmutableSet<EntityId> _ids;
        private readonly ClusterShardingSettings _settings;
        private readonly IActorRef _requestor;

        private IImmutableSet<EntityId> _waitingForAck;
        private readonly ICancelable _tickTask;


        public RememberEntityStarter(
            IActorRef region,
            string typeName,
            ShardId shardId,
            IImmutableSet<EntityId> ids,
            ClusterShardingSettings settings,
            IActorRef requestor
            )
        {
            _region = region;
            _typeName = typeName;
            _shardId = shardId;
            _ids = ids;
            _settings = settings;
            _requestor = requestor;

            _waitingForAck = ids;

            SendStart(ids);

            var resendInterval = settings.TunningParameters.RetryInterval;
            _tickTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(resendInterval, resendInterval, Self, Tick.Instance, ActorRefs.NoSender);
        }

        private void SendStart(IImmutableSet<EntityId> ids)
        {
            foreach (var id in ids)
            {
                // these go through the region rather the directly to the shard
                // so that shard mapping changes are picked up
                _region.Tell(new ShardRegion.StartEntity(id));
            }
        }

        protected override bool Receive(object message)
        {
            switch (message)
            {
                case ShardRegion.StartEntityAck ack:
                    _waitingForAck = _waitingForAck.Remove(ack.EntityId);
                    // inform whoever requested the start that it happened
                    _requestor.Tell(ack);
                    if (_waitingForAck.Count == 0) Context.Stop(Self);
                    return true;
                case Tick _:
                    SendStart(_waitingForAck);
                    return true;
            }
            return false;
        }

        protected override void PostStop()
        {
            _tickTask.Cancel();
        }
    }
}
