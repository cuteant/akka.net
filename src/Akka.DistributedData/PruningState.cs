﻿//-----------------------------------------------------------------------
// <copyright file="PruningState.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster;
using System;
using System.Collections.Immutable;

namespace Akka.DistributedData
{
    internal sealed class PruningInitialized : IPruningState, IEquatable<PruningInitialized>
    {
        public UniqueAddress Owner { get; }
        public IImmutableSet<Address> Seen { get; }

        public PruningInitialized(UniqueAddress owner, params Address[] seen)
            : this(owner, seen.ToImmutableHashSet(AddressComparer.Instance)) { }

        public PruningInitialized(UniqueAddress owner, IImmutableSet<Address> seen)
        {
            Owner = owner;
            Seen = seen;
        }

        public IPruningState AddSeen(Address node) =>
            Seen.Contains(node) || Owner.Address == node
                ? this
                : new PruningInitialized(Owner, Seen.Add(node));
        /// <inheritdoc/>

        public IPruningState Merge(IPruningState other)
        {
            if (other is PruningPerformed) return other;

            var that = (PruningInitialized)other;
            if (Owner == that.Owner)
            {
                return new PruningInitialized(Owner, Seen.Union(that.Seen));
            }
            return Member.AddressOrdering.Compare(Owner.Address, that.Owner.Address) > 0 ? other : this;
        }

        public bool Equals(PruningInitialized other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Owner, other.Owner) && Seen.SetEquals(other.Seen);
        }

        public override bool Equals(object obj) => obj is PruningInitialized pruningInitialized && Equals(pruningInitialized);

        public override int GetHashCode()
        {
            unchecked
            {
                var seed = 17;
                if (Seen is object)
                {
                    foreach (var s in Seen)
                    {
                        seed *= s.GetHashCode();
                    }
                }

                if (Owner is object)
                {
                    seed = seed *= Owner.GetHashCode() ^ 397;
                }

                return seed;
            }
        }
    }

    internal sealed class PruningPerformed : IPruningState, IEquatable<PruningPerformed>
    {
        public PruningPerformed(DateTime obsoleteTime)
        {
            ObsoleteTime = obsoleteTime;
        }

        public DateTime ObsoleteTime { get; }

        public bool IsObsolete(DateTime currentTime) => ObsoleteTime <= currentTime;
        public IPruningState AddSeen(Address node) => this;

        public IPruningState Merge(IPruningState other)
        {
            if (other is PruningPerformed that)
            {
                return ObsoleteTime >= that.ObsoleteTime ? this : that;
            }
            else return this;
        }

        public bool Equals(PruningPerformed other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ObsoleteTime.Equals(other.ObsoleteTime);
        }

        public override bool Equals(object obj) => obj is PruningPerformed pruningPerformed && Equals(pruningPerformed);

        public override int GetHashCode() => ObsoleteTime.GetHashCode();
    }

    public interface IPruningState
    {
        IPruningState AddSeen(Address node);

        IPruningState Merge(IPruningState other);
    }
}
