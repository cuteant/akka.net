﻿//-----------------------------------------------------------------------
// <copyright file="Reachability.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Cluster
{
    /// <summary>
    /// Immutable data structure that holds the reachability status of subject nodes as seen
    /// from observer nodes. Failure detector for the subject nodes exist on the
    /// observer nodes. Changes (reachable, unreachable, terminated) are only performed
    /// by observer nodes to its own records. Each change bumps the version number of the
    /// record, and thereby it is always possible to determine which record is newest 
    /// merging two instances.
    ///
    /// Aggregated status of a subject node is defined as (in this order):
    /// - Terminated if any observer node considers it as Terminated
    /// - Unreachable if any observer node considers it as Unreachable
    /// - Reachable otherwise, i.e. no observer node considers it as Unreachable
    /// </summary>
    [MessagePackObject]
    internal sealed class Reachability //TODO: ISerializable?
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Reachability Empty =
            new Reachability(ImmutableList.Create<Record>(), ImmutableDictionary.Create<UniqueAddress, long>());

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="records">TBD</param>
        /// <param name="versions">TBD</param>
        [SerializationConstructor]
        public Reachability(ImmutableList<Record> records, ImmutableDictionary<UniqueAddress, long> versions)
        {
            _cache = new Lazy<Cache>(() => new Cache(records));
            Versions = versions;
            Records = records;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [MessagePackObject]
        public sealed class Record
        {
            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public readonly UniqueAddress Observer;
            /// <summary>
            /// TBD
            /// </summary>
            [Key(1)]
            public readonly UniqueAddress Subject;
            /// <summary>
            /// TBD
            /// </summary>
            [Key(2)]
            public readonly ReachabilityStatus Status;
            /// <summary>
            /// TBD
            /// </summary>
            [Key(3)]
            public readonly long Version;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="observer">TBD</param>
            /// <param name="subject">TBD</param>
            /// <param name="status">TBD</param>
            /// <param name="version">TBD</param>
            [SerializationConstructor]
            public Record(UniqueAddress observer, UniqueAddress subject, ReachabilityStatus status, long version)
            {
                Observer = observer;
                Subject = subject;
                Status = status;
                Version = version;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (!(obj is Record other)) return false;
                return Version.Equals(other.Version) &&
                       Status == other.Status &&
                       Observer.Equals(other.Observer) &&
                       Subject.Equals(other.Subject);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Observer is object ? Observer.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Version.GetHashCode();
                    hashCode = (hashCode * 397) ^ Status.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Subject is object ? Subject.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public enum ReachabilityStatus
        {
            /// <summary>
            /// TBD
            /// </summary>
            Reachable,
            /// <summary>
            /// TBD
            /// </summary>
            Unreachable,
            /// <summary>
            /// TBD
            /// </summary>
            Terminated
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public readonly ImmutableList<Record> Records;

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public readonly ImmutableDictionary<UniqueAddress, long> Versions;

        /// <summary>
        /// TBD
        /// </summary>
        class Cache
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ImmutableDictionary<UniqueAddress, ImmutableDictionary<UniqueAddress, Record>> ObserverRowMap;

            /// <summary>
            /// TBD
            /// </summary>
            public readonly ImmutableHashSet<UniqueAddress> AllTerminated;

            /// <summary>
            /// TBD
            /// </summary>
            public readonly ImmutableHashSet<UniqueAddress> AllUnreachable;

            /// <summary>
            /// TBD
            /// </summary>
            public readonly ImmutableHashSet<UniqueAddress> AllUnreachableOrTerminated;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="records">TBD</param>
            public Cache(ImmutableList<Record> records)
            {
                if (records.IsEmpty)
                {
                    ObserverRowMap = ImmutableDictionary<UniqueAddress, ImmutableDictionary<UniqueAddress, Record>>.Empty;
                    AllTerminated = ImmutableHashSet<UniqueAddress>.Empty;
                    AllUnreachable = ImmutableHashSet<UniqueAddress>.Empty;
                }
                else
                {
                    var mapBuilder = new Dictionary<UniqueAddress, ImmutableDictionary<UniqueAddress, Record>>(UniqueAddressComparer.Instance);
                    var terminatedBuilder = ImmutableHashSet.CreateBuilder(UniqueAddressComparer.Instance);
                    var unreachableBuilder = ImmutableHashSet.CreateBuilder(UniqueAddressComparer.Instance);

                    foreach (var r in records)
                    {
                        ImmutableDictionary<UniqueAddress, Record> m = mapBuilder.TryGetValue(r.Observer, out m)
                            ? m.SetItem(r.Subject, r)
                            //TODO: Other collections take items for Create. Create unnecessary array here
                            : ImmutableDictionary.CreateRange(UniqueAddressComparer.Instance, new[] { new KeyValuePair<UniqueAddress, Record>(r.Subject, r) });


                        mapBuilder.AddOrSet(r.Observer, m);

                        if (r.Status == ReachabilityStatus.Unreachable) unreachableBuilder.Add(r.Subject);
                        else if (r.Status == ReachabilityStatus.Terminated) terminatedBuilder.Add(r.Subject);
                    }

                    ObserverRowMap = ImmutableDictionary.CreateRange(UniqueAddressComparer.Instance, mapBuilder);
                    AllTerminated = terminatedBuilder.ToImmutable();
                    AllUnreachable = unreachableBuilder.ToImmutable().Except(AllTerminated);
                }

                AllUnreachableOrTerminated = AllTerminated.IsEmpty
                    ? AllUnreachable
                    : AllUnreachable.Union(AllTerminated);
            }
        }

        [IgnoreMember]
        readonly Lazy<Cache> _cache;

        ImmutableDictionary<UniqueAddress, Record> ObserverRows(UniqueAddress observer)
        {
            _cache.Value.ObserverRowMap.TryGetValue(observer, out var observerRows);
            return observerRows;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public Reachability Unreachable(UniqueAddress observer, UniqueAddress subject)
        {
            return Change(observer, subject, ReachabilityStatus.Unreachable);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public Reachability Reachable(UniqueAddress observer, UniqueAddress subject)
        {
            return Change(observer, subject, ReachabilityStatus.Reachable);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public Reachability Terminated(UniqueAddress observer, UniqueAddress subject)
        {
            return Change(observer, subject, ReachabilityStatus.Terminated);
        }

        long CurrentVersion(UniqueAddress observer)
        {
            return Versions.TryGetValue(observer, out long version) ? version : 0;
        }

        long NextVersion(UniqueAddress observer)
        {
            return CurrentVersion(observer) + 1;
        }

        private Reachability Change(UniqueAddress observer, UniqueAddress subject, ReachabilityStatus status)
        {
            var v = NextVersion(observer);
            var newVersions = Versions.SetItem(observer, v);
            var newRecord = new Record(observer, subject, status, v);
            var oldObserverRows = ObserverRows(observer);
            if (oldObserverRows is null && status == ReachabilityStatus.Reachable) return this;
            if (oldObserverRows is null) return new Reachability(Records.Add(newRecord), newVersions);

            if (!oldObserverRows.TryGetValue(subject, out var oldRecord))
            {
                if (status == ReachabilityStatus.Reachable &&
                    oldObserverRows.Values.All(r => r.Status == ReachabilityStatus.Reachable))
                    return new Reachability(Records.FindAll(r => !r.Observer.Equals(observer)), newVersions);
                return new Reachability(Records.Add(newRecord), newVersions);
            }

            if (oldRecord.Status == ReachabilityStatus.Terminated || oldRecord.Status == status)
                return this;

            if (status == ReachabilityStatus.Reachable &&
                oldObserverRows.Values.All(r => r.Status == ReachabilityStatus.Reachable || r.Subject.Equals(subject)))
                return new Reachability(Records.FindAll(r => !r.Observer.Equals(observer)), newVersions);

            var newRecords = Records.SetItem(Records.IndexOf(oldRecord), newRecord);
            return new Reachability(newRecords, newVersions);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="allowed">TBD</param>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        public Reachability Merge(IEnumerable<UniqueAddress> allowed, Reachability other)
        {
            var recordBuilder = ImmutableList.CreateBuilder<Record>();
            //TODO: Size hint somehow?
            var newVersions = Versions;
            foreach (var observer in allowed)
            {
                var observerVersion1 = CurrentVersion(observer);
                var observerVersion2 = other.CurrentVersion(observer);

                var rows1 = ObserverRows(observer);
                var rows2 = other.ObserverRows(observer);

                if (rows1 is object && rows2 is object)
                {
                    var rows = observerVersion1 > observerVersion2 ? rows1 : rows2;
                    foreach (var record in rows.Values.Where(r => allowed.Contains(r.Subject)))
                        recordBuilder.Add(record);
                }
                if (rows1 is object && rows2 is null)
                {
                    if (observerVersion1 > observerVersion2)
                        foreach (var record in rows1.Values.Where(r => allowed.Contains(r.Subject)))
                            recordBuilder.Add(record);
                }
                if (rows1 is null && rows2 is object)
                {
                    if (observerVersion2 > observerVersion1)
                        foreach (var record in rows2.Values.Where(r => allowed.Contains(r.Subject)))
                            recordBuilder.Add(record);
                }

                if (observerVersion2 > observerVersion1)
                    newVersions = newVersions.SetItem(observer, observerVersion2);
            }

            newVersions = ImmutableDictionary.CreateRange(UniqueAddressComparer.Instance, newVersions.Where(p => allowed.Contains(p.Key)));

            return new Reachability(recordBuilder.ToImmutable(), newVersions);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="nodes">TBD</param>
        /// <returns>TBD</returns>
        public Reachability Remove(IEnumerable<UniqueAddress> nodes)
        {
            var nodesSet = nodes.ToImmutableHashSet(UniqueAddressComparer.Instance);
            var newRecords = Records.FindAll(r => !nodesSet.Contains(r.Observer) && !nodesSet.Contains(r.Subject));
            var newVersions = Versions.RemoveRange(nodes);
            return new Reachability(newRecords, newVersions);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="nodes">TBD</param>
        /// <returns>TBD</returns>
        public Reachability RemoveObservers(ImmutableHashSet<UniqueAddress> nodes)
        {
            if (0U >= (uint)nodes.Count)
            {
                return this;
            }
            else
            {
                var newRecords = Records.FindAll(r => !nodes.Contains(r.Observer));
                var newVersions = Versions.RemoveRange(nodes);
                return new Reachability(newRecords, newVersions);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public ReachabilityStatus Status(UniqueAddress observer, UniqueAddress subject)
        {
            var observerRows = ObserverRows(observer);
            if (observerRows is null) return ReachabilityStatus.Reachable;

            if (!observerRows.TryGetValue(subject, out var record))
                return ReachabilityStatus.Reachable;

            return record.Status;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public ReachabilityStatus Status(UniqueAddress node)
        {
            if (_cache.Value.AllTerminated.Contains(node)) return ReachabilityStatus.Terminated;
            if (_cache.Value.AllUnreachable.Contains(node)) return ReachabilityStatus.Unreachable;
            return ReachabilityStatus.Reachable;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="node">TBD</param>
        /// <returns>TBD</returns>
        public bool IsReachable(UniqueAddress node)
        {
            return IsAllReachable || !AllUnreachableOrTerminated.Contains(node);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <param name="subject">TBD</param>
        /// <returns>TBD</returns>
        public bool IsReachable(UniqueAddress observer, UniqueAddress subject)
        {
            return Status(observer, subject) == ReachabilityStatus.Reachable;
        }

        /*
         *  def isReachable(observer: UniqueAddress, subject: UniqueAddress): Boolean =
            status(observer, subject) == Reachable
         */

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public bool IsAllReachable
        {
            get { return Records.IsEmpty; }
        }

        /// <summary>
        /// Doesn't include terminated
        /// </summary>
        [IgnoreMember]
        public ImmutableHashSet<UniqueAddress> AllUnreachable
        {
            get { return _cache.Value.AllUnreachable; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public ImmutableHashSet<UniqueAddress> AllUnreachableOrTerminated
        {
            get { return _cache.Value.AllUnreachableOrTerminated; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <returns>TBD</returns>
        public ImmutableHashSet<UniqueAddress> AllUnreachableFrom(UniqueAddress observer)
        {
            var observerRows = ObserverRows(observer);
            if (observerRows is null) return ImmutableHashSet.Create<UniqueAddress>(UniqueAddressComparer.Instance);
            return
                ImmutableHashSet.CreateRange(UniqueAddressComparer.Instance,
                    observerRows.Where(p => p.Value.Status == ReachabilityStatus.Unreachable).Select(p => p.Key));
        }

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public ImmutableDictionary<UniqueAddress, ImmutableHashSet<UniqueAddress>> ObserversGroupedByUnreachable
        {
            get
            {
                var builder = new Dictionary<UniqueAddress, ImmutableHashSet<UniqueAddress>>(UniqueAddressComparer.Instance);

                var grouped = Records.GroupBy(p => p.Subject);
                foreach (var records in grouped)
                {
                    if (records.Any(r => r.Status == ReachabilityStatus.Unreachable))
                    {
                        builder.Add(records.Key, records.Where(r => r.Status == ReachabilityStatus.Unreachable)
                            .Select(r => r.Observer).ToImmutableHashSet(UniqueAddressComparer.Instance));
                    }
                }
                return builder.ToImmutableDictionary(UniqueAddressComparer.Instance);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [IgnoreMember]
        public ImmutableHashSet<UniqueAddress> AllObservers
        {
            get { return ImmutableHashSet.CreateRange(UniqueAddressComparer.Instance, Versions.Keys); }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="observer">TBD</param>
        /// <returns>TBD</returns>
        public ImmutableList<Record> RecordsFrom(UniqueAddress observer)
        {
            var rows = ObserverRows(observer);
            if (rows is null) return ImmutableList.Create<Record>();
            return rows.Values.ToImmutableList();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Versions.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is Reachability other)) return false;
            return Records.Count == other.Records.Count &&
                Versions.Equals(other.Versions) &&
                _cache.Value.ObserverRowMap.Equals(other._cache.Value.ObserverRowMap);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder("Reachability(");

            foreach (var observer in Versions.Keys)
            {
                var rows = ObserverRows(observer);
                if (rows is null) continue;

                builder.AppendJoin(", ", rows, (b, row, index) =>
                    b.AppendFormat("[{0} -> {1}: {2} [{3}] ({4})]",
                        observer.Address, row.Key, row.Value.Status, Status(row.Key), row.Value.Version));
            }

            return builder.Append(')').ToString();
        }
    }
}
