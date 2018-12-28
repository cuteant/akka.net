//-----------------------------------------------------------------------
// <copyright file="Snapshot.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Akka.Persistence.Serialization
{
    /// <summary>
    /// Wrapper for snapshot data.
    /// </summary>
    public sealed class Snapshot
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="data">TBD</param>
        public Snapshot(object data)
        {
            Data = data;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public readonly object Data;

        /// <inheritdoc/>
        private bool Equals(Snapshot other)
        {
            return Equals(Data, other.Data);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Snapshot snapshot && Equals(snapshot);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0);
        }
    }
}
