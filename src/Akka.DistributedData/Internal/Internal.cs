//-----------------------------------------------------------------------
// <copyright file="Internal.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace Akka.DistributedData.Internal
{
    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class GossipTick : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        internal static readonly GossipTick Instance = new GossipTick();
        private GossipTick() { }
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "GossipTick";
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal sealed class DeltaPropagationTick : ISingletonMessage
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static readonly DeltaPropagationTick Instance = new DeltaPropagationTick();

        private DeltaPropagationTick() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal class RemovedNodePruningTick : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        internal static readonly RemovedNodePruningTick Instance = new RemovedNodePruningTick();
        private RemovedNodePruningTick() { }
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "RemovedNodePruningTick";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal class ClockTick : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        internal static readonly ClockTick Instance = new ClockTick();
        private ClockTick() { }
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "ClockTick";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class Write : IReplicatorMessage, IEquatable<Write>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public DataEnvelope Envelope { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public UniqueAddress FromNode { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="key">TBD</param>
        /// <param name="envelope">TBD</param>
        /// <param name="fromNode">TBD</param>
        public Write(string key, DataEnvelope envelope, UniqueAddress fromNode = null)
        {
            Key = key;
            Envelope = envelope;
            FromNode = fromNode;
        }

        /// <inheritdoc/>
        public bool Equals(Write other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Key == other.Key && Equals(Envelope, other.Envelope) && Equals(FromNode, other.FromNode);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Write write && Equals(write);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key is object ? Key.GetHashCode() : 0) * 397) ^ (Envelope is object ? Envelope.GetHashCode() : 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"Write(key={Key}, envelope={Envelope})";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class WriteAck : IReplicatorMessage, IEquatable<WriteAck>//, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        internal static readonly WriteAck Instance = new WriteAck();

        private WriteAck() { }
        /// <inheritdoc/>
        public bool Equals(WriteAck other) => true;
        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is WriteAck;
        /// <inheritdoc/>
        public override int GetHashCode() => 1;
        /// <inheritdoc/>
        public override string ToString() => "WriteAck";
    }


    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class WriteNack : IReplicatorMessage, IEquatable<WriteNack> //, ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        internal static readonly WriteNack Instance = new WriteNack();

        private WriteNack() { }
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(WriteNack other) => true;
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj) => obj is WriteNack;
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode() => 1;
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "WriteNack";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class Read : IReplicatorMessage, IEquatable<Read>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public UniqueAddress FromNode { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="key">TBD</param>
        /// <param name="fromNode">TBD</param>
        public Read(string key, UniqueAddress fromNode = null)
        {
            Key = key;
            FromNode = fromNode;
        }

        /// <inheritdoc/>
        public bool Equals(Read other)
        {
            return other is object && Key == other.Key;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Read read && Equals(read);

        /// <inheritdoc/>
        public override int GetHashCode() => Key?.GetHashCode() ?? 0;

        /// <inheritdoc/>
        public override string ToString() => $"Read(key={Key})";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class ReadResult : IReplicatorMessage, IEquatable<ReadResult>, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        public DataEnvelope Envelope { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="envelope">TBD</param>
        public ReadResult(DataEnvelope envelope)
        {
            Envelope = envelope;
        }

        /// <inheritdoc/>
        public bool Equals(ReadResult other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Envelope, other.Envelope);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ReadResult readResult && Equals(readResult);

        /// <inheritdoc/>
        public override int GetHashCode() => Envelope?.GetHashCode() ?? 0;

        /// <inheritdoc/>
        public override string ToString() => $"ReadResult(envelope={Envelope})";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class ReadRepair : IEquatable<ReadRepair>, IReplicatorMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public DataEnvelope Envelope { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="key">TBD</param>
        /// <param name="envelope">TBD</param>
        public ReadRepair(string key, DataEnvelope envelope)
        {
            Key = key;
            Envelope = envelope;
        }

        /// <inheritdoc/>
        public bool Equals(ReadRepair other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Key, other.Key) && Equals(Envelope, other.Envelope);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ReadRepair readRepair && Equals(readRepair);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key?.GetHashCode() ?? 0) * 397) ^ (Envelope?.GetHashCode() ?? 0);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"ReadRepair(key={Key}, envelope={Envelope})";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class ReadRepairAck : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly ReadRepairAck Instance = new ReadRepairAck();

        private ReadRepairAck() { }

        /// <inheritdoc/>
        public override string ToString() => $"ReadRepairAck";
    }

    /// <summary>
    /// TBD
    /// </summary>
    public sealed class DataEnvelope : IEquatable<DataEnvelope>, IReplicatorMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static DataEnvelope DeletedEnvelope => new DataEnvelope(DeletedData.Instance);

        /// <summary>
        /// TBD
        /// </summary>
        public IReplicatedData Data { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public ImmutableDictionary<UniqueAddress, IPruningState> Pruning { get; }

        public VersionVector DeltaVersions { get; }

        /// <summary>
        /// The <see cref="DataEnvelope"/> wraps a data entry and carries state of the pruning process for the entry.
        /// </summary>
        /// <param name="data">TBD</param>
        /// <param name="pruning">TBD</param>
        /// <param name="deltaVersions"></param>
        internal DataEnvelope(IReplicatedData data, ImmutableDictionary<UniqueAddress, IPruningState> pruning = null, VersionVector deltaVersions = null)
        {
            Data = data;
            Pruning = pruning ?? ImmutableDictionary<UniqueAddress, IPruningState>.Empty;
            DeltaVersions = deltaVersions ?? VersionVector.Empty;
        }

        internal DataEnvelope WithData(IReplicatedData data) =>
            new DataEnvelope(data, Pruning, DeltaVersions);

        internal DataEnvelope WithPruning(ImmutableDictionary<UniqueAddress, IPruningState> pruning) =>
            new DataEnvelope(Data, pruning, DeltaVersions);

        internal DataEnvelope WithDeltaVersions(VersionVector deltaVersions) =>
            new DataEnvelope(Data, Pruning, deltaVersions);

        internal DataEnvelope WithoutDeltaVersions() =>
            DeltaVersions.IsEmpty
                ? this
                : new DataEnvelope(Data, Pruning);

        /// <summary>
        /// We only use the deltaVersions to track versions per node, not for ordering comparisons,
        /// so we can just remove the entry for the removed node.
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        private VersionVector CleanedDeltaVersions(UniqueAddress from) => DeltaVersions.PruningCleanup(from);

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="removedNode">TBD</param>
        /// <returns>TBD</returns>
        internal bool NeedPruningFrom(UniqueAddress removedNode)
        {
            return Data is IRemovedNodePruning r && r.NeedPruningFrom(removedNode);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="removed">TBD</param>
        /// <param name="owner">TBD</param>
        /// <returns>TBD</returns>
        internal DataEnvelope InitRemovedNodePruning(UniqueAddress removed, UniqueAddress owner) =>
            new DataEnvelope(Data, Pruning.SetItem(removed, new PruningInitialized(owner, ImmutableHashSet<Address>.Empty)));

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="from">TBD</param>
        /// <param name="pruningPerformed"></param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        internal DataEnvelope Prune(UniqueAddress from, PruningPerformed pruningPerformed)
        {
            if (Data is IRemovedNodePruning dataWithRemovedNodePruning)
            {
                if (!Pruning.TryGetValue(from, out var state))
                {
                    ThrowHelper.ThrowArgumentException_CantPruneSinceItsNotFoundInDataEnvelope(from);
                }

                if (state is PruningInitialized initialized)
                {
                    var prunedData = dataWithRemovedNodePruning.Prune(from, initialized.Owner);
                    return new DataEnvelope(data: prunedData, pruning: Pruning.SetItem(from, pruningPerformed), deltaVersions: CleanedDeltaVersions(from));
                }
            }
            return this;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        internal DataEnvelope Merge(DataEnvelope other)
        {
            if (other.Data is DeletedData) { return DeletedEnvelope; }

            var mergedPrunning = other.Pruning.ToBuilder();
            foreach (var entry in Pruning)
            {
                if (mergedPrunning.TryGetValue(entry.Key, out var state))
                    mergedPrunning[entry.Key] = entry.Value.Merge(state);
                else
                    mergedPrunning[entry.Key] = entry.Value;
            }

            var currentTime = DateTime.UtcNow;
            var filteredMergedPruning = mergedPrunning.Count == 0
                ? mergedPrunning.ToImmutable()
                : mergedPrunning
                    .Where(entry =>
                    {
                        var performed = entry.Value as PruningPerformed;
                        return !performed?.IsObsolete(currentTime) ?? true;
                    })
                    .ToImmutableDictionary(UniqueAddressComparer.Instance);

            // cleanup and merge DeltaVersions
            var removedNodes = filteredMergedPruning.Keys.ToArray();
            var cleanedDeltaVersions = removedNodes.Aggregate(DeltaVersions, (acc, node) => acc.PruningCleanup(node));
            var cleanedOtherDeltaVersions = removedNodes.Aggregate(other.DeltaVersions, (acc, node) => acc.PruningCleanup(node));
            var mergedDeltaVersions = cleanedDeltaVersions.Merge(cleanedOtherDeltaVersions);

            // cleanup both sides before merging, `merge(otherData: ReplicatedData)` will cleanup other.data
            return new DataEnvelope(
                    data: Cleaned(Data, filteredMergedPruning),
                    pruning: filteredMergedPruning,
                    deltaVersions: mergedDeltaVersions)
                .Merge(other.Data);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="otherData">TBD</param>
        /// <returns>TBD</returns>
        internal DataEnvelope Merge(IReplicatedData otherData)
        {
            if (otherData is DeletedData) return DeletedEnvelope;

            var cleanedData = Cleaned(otherData, Pruning);
            IReplicatedData mergedData;
            if (cleanedData is IReplicatedDelta d)
            {
                var delta = Data as IDeltaReplicatedData;
                if (delta is null)
                {
                    ThrowHelper.ThrowArgumentException_ExpectedIDeltaReplicatedDataButGot(Data);
                }

                mergedData = delta.MergeDelta(d);
            }
            else mergedData = Data.Merge(cleanedData);

            return new DataEnvelope(mergedData, Pruning, DeltaVersions);
        }

        private IReplicatedData Cleaned(IReplicatedData c, IImmutableDictionary<UniqueAddress, IPruningState> p) => p.Aggregate(c, (state, kvp) =>
        {
            if (c is IRemovedNodePruning pruning
                && kvp.Value is PruningPerformed
                && pruning.NeedPruningFrom(kvp.Key))
                return pruning.PruningCleanup(kvp.Key);
            return c;
        });

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        internal DataEnvelope AddSeen(Address node)
        {
            var changed = false;
            var newRemovedNodePruning = Pruning.Select(kvp =>
            {
                var newPruningState = kvp.Value.AddSeen(node);
                changed = !ReferenceEquals(newPruningState, kvp.Value) || changed;
                return new KeyValuePair<UniqueAddress, IPruningState>(kvp.Key, newPruningState);
            }).ToImmutableDictionary(UniqueAddressComparer.Instance);

            return changed ? new DataEnvelope(Data, newRemovedNodePruning) : this;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public bool Equals(DataEnvelope other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!Data.Equals(other.Data)) return false;
            if (Pruning.Count != other.Pruning.Count) return false;
            if (!DeltaVersions.Equals(other.DeltaVersions)) return false;

            foreach (var entry in Pruning)
            {
                if (!Equals(entry.Value, other.Pruning[entry.Key])) return false;
            }

            return true;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <returns>TBD</returns>
        public override bool Equals(object obj) => obj is DataEnvelope dataEnvelope && Equals(dataEnvelope);

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var seed = ((Data is object ? Data.GetHashCode() : 0) * 397)
                            ^ (DeltaVersions is object ? DeltaVersions.GetHashCode() : 0);

                foreach (var p in Pruning)
                {
                    seed *= p.Key.GetHashCode() ^ p.Value.GetHashCode();
                }

                return seed;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("{");
            if (Pruning is object)
                foreach (var entry in Pruning)
                {
                    sb.Append(entry.Key).Append("->").Append(entry.Value).Append(",");
                }
            sb.Append('}');

            return $"DataEnvelope(data={Data}, prunning={sb})";
        }
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// Placeholder used to represent deleted data that has not yet been pruned or is permanently tombstoned.
    /// </summary>
    /// <returns>TBD</returns>
    internal sealed class DeletedData : IReplicatedData<DeletedData>, IEquatable<DeletedData>, IReplicatedDataSerialization/*, ISingletonMessage // ReplicatedDataSerializer */
    {
        public static readonly DeletedData Instance = new DeletedData();

        private DeletedData() { }

        /// <inheritdoc cref="IReplicatedData{T}"/>
        public DeletedData Merge(DeletedData other) => this;

        /// <inheritdoc cref="IReplicatedData{T}"/>
        public IReplicatedData Merge(IReplicatedData other) => Merge((DeletedData)other);
        /// <inheritdoc/>
        public bool Equals(DeletedData other) => true;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DeletedData;

        /// <inheritdoc/>
        public override int GetHashCode() => 1;

        /// <inheritdoc/>
        public override string ToString() => "DeletedData";
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class Status : IReplicatorMessage, IEquatable<Status>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public IImmutableDictionary<string, byte[]> Digests { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public int Chunk { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public int TotalChunks { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public long? ToSystemUid { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public long? FromSystemUid { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="digests">TBD</param>
        /// <param name="chunk">TBD</param>
        /// <param name="totalChunks">TBD</param>
        /// <param name="toSystemUid">TBD</param>
        /// <param name="fromSystemUid">TBD</param>
        public Status(IImmutableDictionary<string, byte[]> digests, int chunk, int totalChunks, long? toSystemUid = null, long? fromSystemUid = null)
        {
            Digests = digests;
            Chunk = chunk;
            TotalChunks = totalChunks;
            ToSystemUid = toSystemUid;
            FromSystemUid = fromSystemUid;
        }

        /// <inheritdoc/>
        public bool Equals(Status other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.Chunk.Equals(Chunk)
                && other.TotalChunks.Equals(TotalChunks)
                && Digests.SequenceEqual(other.Digests, KVEqualityComparer.Instance)
                && ToSystemUid.Equals(other.ToSystemUid)
                && FromSystemUid.Equals(other.FromSystemUid);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Status status && Equals(status);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Digests is object ? Digests.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Chunk;
                hashCode = (hashCode * 397) ^ TotalChunks;
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder("{");
            if (Digests is object)
                foreach (var entry in Digests)
                {
                    sb.Append(entry.Key).Append("->").Append(entry.Value).Append(",");
                }
            sb.Append('}');

            return $"Status(chunk={Chunk}, totalChunks={TotalChunks}, digest={sb})";
        }

        private sealed class KVEqualityComparer : IEqualityComparer<KeyValuePair<string, byte[]>>
        {
            public static readonly KVEqualityComparer Instance = new KVEqualityComparer();

            private KVEqualityComparer() { }

            public bool Equals(KeyValuePair<string, byte[]> x, KeyValuePair<string, byte[]> y)
            {
                if (!string.Equals(x.Key, y.Key)) { return false; }
                if (ReferenceEquals(x.Value, y.Value)) { return true; }
                if (x.Value is null || y.Value is null) { return false; }
                return x.Value.AsSpan().SequenceEqual(y.Value);
            }

            public int GetHashCode(KeyValuePair<string, byte[]> obj)
            {
                return obj.Key.GetHashCode() ^ (obj.Value?.GetHashCode() ?? 0);
            }
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    internal sealed class Gossip : IReplicatorMessage, IEquatable<Gossip>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public IImmutableDictionary<string, DataEnvelope> UpdatedData { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public bool SendBack { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public long? ToSystemUid { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public long? FromSystemUid { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="updatedData">TBD</param>
        /// <param name="sendBack">TBD</param>
        /// <param name="toSystemUid">TBD</param>
        /// <param name="fromSystemUid">TBD</param>
        public Gossip(IImmutableDictionary<string, DataEnvelope> updatedData, bool sendBack, long? toSystemUid = null, long? fromSystemUid = null)
        {
            UpdatedData = updatedData;
            SendBack = sendBack;
            ToSystemUid = toSystemUid;
            FromSystemUid = fromSystemUid;
        }

        /// <inheritdoc/>
        public bool Equals(Gossip other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.SendBack.Equals(SendBack)
                && UpdatedData.SequenceEqual(other.UpdatedData)
                && ToSystemUid.Equals(other.ToSystemUid)
                && FromSystemUid.Equals(other.FromSystemUid);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Gossip gossip && Equals(gossip);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((UpdatedData is object ? UpdatedData.GetHashCode() : 0) * 397) ^ SendBack.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder("{");
            if (UpdatedData is object)
                foreach (var entry in UpdatedData)
                {
                    sb.Append(entry.Key).Append("->").Append(entry.Value).Append(",");
                }
            sb.Append('}');

            return $"Gossip(sendBack={SendBack}, updatedData={sb})";
        }
    }

    public sealed class Delta : IEquatable<Delta>
    {
        public readonly DataEnvelope DataEnvelope;
        public readonly long FromSeqNr;
        public readonly long ToSeqNr;

        public Delta(DataEnvelope dataEnvelope, long fromSeqNr, long toSeqNr)
        {
            DataEnvelope = dataEnvelope;
            FromSeqNr = fromSeqNr;
            ToSeqNr = toSeqNr;
        }

        public bool Equals(Delta other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(DataEnvelope, other.DataEnvelope) && FromSeqNr == other.FromSeqNr && ToSeqNr == other.ToSeqNr;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Delta delta && Equals(delta);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DataEnvelope.GetHashCode();
                hashCode = (hashCode * 397) ^ FromSeqNr.GetHashCode();
                hashCode = (hashCode * 397) ^ ToSeqNr.GetHashCode();
                return hashCode;
            }
        }
    }

    public sealed class DeltaPropagation : IReplicatorMessage, IEquatable<DeltaPropagation>
    {
        private sealed class NoDelta : IDeltaReplicatedData<IReplicatedData, IReplicatedDelta>, IRequireCausualDeliveryOfDeltas, ISingletonMessage
        {
            public static readonly NoDelta Instance = new NoDelta();
            private NoDelta() { }

            IReplicatedDelta IDeltaReplicatedData.Delta => Delta;
            public IReplicatedDelta Delta => null;
            public IDeltaReplicatedData Zero => this;

            IReplicatedData IReplicatedData<IReplicatedData>.Merge(IReplicatedData other) => Merge(other);
            public IReplicatedData Merge(IReplicatedData other) => this;
            IReplicatedData IDeltaReplicatedData<IReplicatedData, IReplicatedDelta>.MergeDelta(IReplicatedDelta delta) => MergeDelta(delta);
            public IReplicatedData ResetDelta() => this;
            public IReplicatedData MergeDelta(IReplicatedDelta delta) => this;
        }
        /// <summary>
        /// When a DeltaReplicatedData returns `null` from <see cref="Delta"/> it must still be
        /// treated as a delta that increase the version counter in <see cref="DeltaPropagationSelector"/>`.
        /// Otherwise a later delta might be applied before the full state gossip is received
        /// and thereby violating <see cref="IRequireCausualDeliveryOfDeltas"/>.
        /// 
        /// This is used as a placeholder for such `null` delta. It's filtered out
        /// in <see cref="DeltaPropagationSelector.CreateDeltaPropagation(ImmutableDictionary{string, Tuple{IReplicatedData, long, long}})"/>, i.e. never sent to the other replicas.
        /// </summary>
        public static readonly IReplicatedDelta NoDeltaPlaceholder = NoDelta.Instance;

        public readonly UniqueAddress FromNode;
        public readonly bool ShouldReply;
        public readonly ImmutableDictionary<string, Delta> Deltas;

        public DeltaPropagation(UniqueAddress fromNode, bool shouldReply, ImmutableDictionary<string, Delta> deltas)
        {
            FromNode = fromNode;
            ShouldReply = shouldReply;
            Deltas = deltas;
        }

        public bool Equals(DeltaPropagation other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(FromNode, other.FromNode) || !ShouldReply == other.ShouldReply || Deltas.Count != other.Deltas.Count)
                return false;

            foreach (var entry in Deltas)
            {
                if (!Equals(other.Deltas.GetValueOrDefault(entry.Key), entry.Value)) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DeltaPropagation deltaPropagation && Equals(deltaPropagation);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FromNode is object ? FromNode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ShouldReply.GetHashCode();
                hashCode = (hashCode * 397) ^ Deltas.GetHashCode();
                return hashCode;
            }
        }
    }

    public sealed class DeltaNack : IReplicatorMessage, IDeadLetterSuppression, IEquatable<DeltaNack>//, ISingletonMessage
    {
        public static readonly DeltaNack Instance = new DeltaNack();
        private DeltaNack() { }
        public bool Equals(DeltaNack other) => true;
        public override bool Equals(object obj) => obj is DeltaNack;
        public override int GetHashCode() => nameof(DeltaNack).GetHashCode();
    }
}
