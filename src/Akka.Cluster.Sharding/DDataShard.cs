﻿//-----------------------------------------------------------------------
// <copyright file="DDataShard.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DistributedData;
using Akka.Event;
using Akka.Util;

namespace Akka.Cluster.Sharding
{
    using ShardId = String;
    using EntryId = String;
    using Msg = Object;

    /// <summary>
    /// This actor creates children entity actors on demand that it is told to be
    /// responsible for. It is used when `rememberEntities` is enabled and
    /// `state-store-mode=ddata`.
    /// </summary>
    internal sealed class DDataShard : ActorBase, IShard, IWithUnboundedStash
    {
        IActorContext IShard.Context => Context;
        IActorRef IShard.Self => Self;
        IActorRef IShard.Sender => Sender;
        void IShard.Unhandled(object message) => base.Unhandled(message);

        public string TypeName { get; }
        public string ShardId { get; }
        public Func<string, Props> EntityProps { get; }
        public ClusterShardingSettings Settings { get; }
        public ExtractEntityId ExtractEntityId { get; }
        public ExtractShardId ExtractShardId { get; }
        public object HandOffStopMessage { get; }
        ILoggingAdapter IShard.Log { get; } = Context.GetLogger();
        public IActorRef HandOffStopper { get; set; }
        public Shard.ShardState State { get; set; } = Shard.ShardState.Empty;
        public ImmutableDictionary<string, IActorRef> RefById { get; set; } = ImmutableDictionary<string, IActorRef>.Empty;
        public ImmutableDictionary<IActorRef, string> IdByRef { get; set; } = ImmutableDictionary<IActorRef, string>.Empty;
        public ImmutableDictionary<string, long> LastMessageTimestamp { get; set; }
        public ImmutableHashSet<IActorRef> Passivating { get; set; } = ImmutableHashSet<IActorRef>.Empty;
        public ImmutableDictionary<string, ImmutableList<Tuple<object, IActorRef>>> MessageBuffers { get; set; } = ImmutableDictionary<string, ImmutableList<Tuple<object, IActorRef>>>.Empty;
        public ICancelable PassivateIdleTask { get; }

        private EntityRecoveryStrategy RememberedEntitiesRecoveryStrategy { get; }
        public Cluster Cluster { get; } = Cluster.Get(Context.System);
        public ILoggingAdapter Log { get; } = Context.GetLogger();
        public IActorRef Replicator { get; }
        public int MajorityCap { get; }
        public IStash Stash { get; set; }

        private readonly IReadConsistency _readConsistency;
        private readonly IWriteConsistency _writeConsistency;
        private const int MaxUpdateAttempts = 3;

        // The default maximum-frame-size is 256 KiB with Artery.
        // When using entity identifiers with 36 character strings (e.g. UUID.randomUUID).
        // By splitting the elements over 5 keys we can support 10000 entities per shard.
        // The Gossip message size of 5 ORSet with 2000 ids is around 200 KiB.
        // This is by intention not configurable because it's important to have the same
        // configuration on each node.
        private const int NrOfKeys = 5;

        private readonly ImmutableArray<ORSetKey<EntryId>> _stateKeys;

        public DDataShard(
            string typeName,
            ShardId shardId,
            Func<string, Props> entityProps,
            ClusterShardingSettings settings,
            ExtractEntityId extractEntityId,
            ExtractShardId extractShardId,
            object handOffStopMessage,
            IActorRef replicator,
            int majorityCap)
        {
            TypeName = typeName;
            ShardId = shardId;
            EntityProps = entityProps;
            Settings = settings;
            ExtractEntityId = extractEntityId;
            ExtractShardId = extractShardId;
            HandOffStopMessage = handOffStopMessage;
            Replicator = replicator;
            MajorityCap = majorityCap;

            RememberedEntitiesRecoveryStrategy = Settings.TunningParameters.EntityRecoveryStrategy == "constant"
                ? EntityRecoveryStrategy.ConstantStrategy(
                    Context.System,
                    Settings.TunningParameters.EntityRecoveryConstantRateStrategyFrequency,
                    Settings.TunningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities)
                : EntityRecoveryStrategy.AllStrategy;

            var idleInterval = TimeSpan.FromTicks(Settings.PassivateIdleEntityAfter.Ticks / 2);
            PassivateIdleTask = Settings.PassivateIdleEntityAfter > TimeSpan.Zero
                ? Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(idleInterval, idleInterval, Self, Shard.PassivateIdleTick.Instance, Self)
                : null;

            _readConsistency = new ReadMajority(settings.TunningParameters.WaitingForStateTimeout, majorityCap);
            _writeConsistency = new WriteMajority(settings.TunningParameters.UpdatingStateTimeout, majorityCap);
            _stateKeys = Enumerable.Range(0, NrOfKeys).Select(i => new ORSetKey<EntryId>($"shard-{typeName}-{shardId}-{i}")).ToImmutableArray();

            GetState();
        }

        public void EntityTerminated(IActorRef tref) => this.BaseEntityTerminated(tref);
        public void DeliverTo(string id, object message, object payload, IActorRef sender) => this.BaseDeliverTo(id, message, payload, sender);

        protected override bool Receive(object message) => WaitingForState(ImmutableHashSet<int>.Empty)(message);

        private ORSetKey<EntryId> Key(EntryId entityId)
        {
            var i = Math.Abs(MurmurHash.StringHash(entityId)) % NrOfKeys;
            return _stateKeys[i];
        }

        private void GetState()
        {
            for (int i = 0; i < NrOfKeys; i++)
            {
                Replicator.Tell(Dsl.Get(_stateKeys[i], _readConsistency, i));
            }
        }
        private Receive WaitingForState(ImmutableHashSet<int> gotKeys) => message =>
        {
            void ReceiveOne(int i)
            {
                var newGotKeys = gotKeys.Add(i);
                if (newGotKeys.Count == NrOfKeys)
                    RecoveryCompleted();
                else
                    Context.Become(WaitingForState(newGotKeys));
            }

            switch (message)
            {
                case GetSuccess success:
                    var i = (int)success.Request;
                    var key = _stateKeys[i];
                    State = new Shard.ShardState(State.Entries.Union(success.Get(key).Elements));
                    ReceiveOne(i);
                    break;
                case GetFailure _:
                    Log.TheDDataShardWasUnableToGetAnInitialState(Settings);
                    Context.Stop(Self);
                    break;
                case NotFound notFound:
                    ReceiveOne((int)notFound.Request);
                    break;
                default: Stash.Stash(); break;
            }

            return true;
        };

        private void RecoveryCompleted()
        {
            RestartRememberedEntities();
            this.Initialized();
            if (Log.IsDebugEnabled) Log.DDataShardRecoveryCompletedShardWithEntities(ShardId, State.Entries.Count);
            Stash.UnstashAll();
            Context.Become(this.HandleCommand);
        }

        public void ProcessChange<T>(T evt, Action<T> handler) where T : Shard.StateChange
        {
            Context.BecomeStacked(WaitingForUpdate<T>(evt, handler));
            SendUpdate(evt, retryCount: 1);
        }

        private void SendUpdate(Shard.StateChange e, int retryCount)
        {
            Replicator.Tell(Dsl.Update(Key(e.EntityId), ORSet<EntryId>.Empty, _writeConsistency, Tuple.Create(e, retryCount),
                existing =>
                {
                    switch (e)
                    {
                        case Shard.EntityStarted started: return existing.Add(Cluster, started.EntityId);
                        case Shard.EntityStopped stopped: return existing.Remove(Cluster, stopped.EntityId);
                        default: ThrowHelper.ThrowNotSupportedException_DDataShardSendUpdateEventNotSupported(e); return null;
                    }
                }));
        }

        private Receive WaitingForUpdate<TEvent>(TEvent e, Action<TEvent> afterUpdateCallback) where TEvent : Shard.StateChange => message =>
        {
            switch (message)
            {
                case UpdateSuccess success when Equals(((Tuple<Shard.StateChange, int>)success.Request).Item1, e):
                    if (Log.IsDebugEnabled) Log.TheDDataShardStateWasSuccessfullyUpdatedWith(e);
                    Context.UnbecomeStacked();
                    afterUpdateCallback(e);
                    Stash.UnstashAll();
                    break;
                case UpdateTimeout timeout when Equals(((Tuple<Shard.StateChange, int>)timeout.Request).Item1, e):
                    var t = (Tuple<Shard.StateChange, int>)timeout.Request;
                    var retryCount = t.Item2;
                    if (retryCount == MaxUpdateAttempts)
                    {
                        // parent ShardRegion supervisor will notice that it terminated and will start it again, after backoff
                        Log.TheDDataShardWasUnableToUpdateState(MaxUpdateAttempts, Settings, e);
                        Context.Stop(Self);
                    }
                    else
                    {
                        Log.TheDDataShardWasUnableToUpdateState(retryCount, MaxUpdateAttempts, Settings, e);
                        SendUpdate(e, retryCount + 1);
                    }
                    break;
                case ModifyFailure failure when Equals(((Tuple<Shard.StateChange, int>)failure.Request).Item1, e):
                    Log.TheDDataShardWasUnableToUpdateStateWithError(failure, e);
                    ExceptionDispatchInfo.Capture(failure.Cause).Throw();
                    break;
                default: Stash.Stash(); break;
            }
            return true;
        };

        private void RestartRememberedEntities()
        {
            foreach (var scheduledRecovery in RememberedEntitiesRecoveryStrategy.RecoverEntities(State.Entries))
            {
                scheduledRecovery.LinkOutcome(AfterScheduledRecoveryFunc, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self, Self);
            }
        }

        private static readonly Func<Task<IImmutableSet<ShardId>>, Shard.RestartEntities> AfterScheduledRecoveryFunc = AfterScheduledRecovery;
        private static Shard.RestartEntities AfterScheduledRecovery(Task<IImmutableSet<ShardId>> t) => new Shard.RestartEntities(t.Result);
    }
}