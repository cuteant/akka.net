//-----------------------------------------------------------------------
// <copyright file="MatchBuilderSignature.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Util.Internal;

namespace Akka.Tools.MatchHandler
{
    /// <summary>
    /// This class contains the handled <see cref="Type">Types</see> and <see cref="HandlerKind">HandlerKinds</see> 
    /// that has been added to a <see cref="MatchBuilder"/>.
    /// Two signatures are equal if they contain the same <see cref="Type">Types</see> and <see cref="HandlerKind">HandlerKinds</see>
    /// in the same order.
    /// </summary>
    internal class MatchBuilderSignature : IEquatable<MatchBuilderSignature>
    {
        private readonly IReadOnlyList<object> _list;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="signature">TBD</param>
        public MatchBuilderSignature(IReadOnlyList<object> signature)
        {
            _list = signature;
        }

        /// <inheritdoc/>
        public bool Equals(MatchBuilderSignature other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ListsEqual(_list, other._list);
        }

        internal bool ListsEqual(MatchBuilderSignature other)
        {
            return ListsEqual(_list, other._list);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return ListsEqual(_list, ((MatchBuilderSignature)obj)._list);
        }

        // Two signatures are equal if they contain the same <see cref="Type">Types</see> and <see cref="HandlerKind">HandlerKinds</see>
        // in the same order.
        private static bool ListsEqual(IReadOnlyList<object> x, IReadOnlyList<object> y)
        {
            if (x is null) return y.IsNullOrEmptyR();
            var xCount = x.Count;
            if (y is null) return xCount == 0;
            if (xCount != y.Count) return false;
            for (var i = 0; i < xCount; i++)
            {
                if (!Equals(x[i], y[i])) return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (_list is null) return 0;
            var count = _list.Count;
            if (count == 0) return 0;
            var hashCode = _list[0].GetHashCode();
            for (var i = 1; i < count; i++)
            {
                hashCode = (hashCode * 397) ^ _list[i].GetHashCode();
            }
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var types = _list.Select(o => (o as Type)?.Name ?? o);
            return $"[{string.Join(", ", types)}]";
        }
    }

    internal class MatchBuilderSignatureComparer : IEqualityComparer<MatchBuilderSignature>
    {
        public static readonly MatchBuilderSignatureComparer Instance = new MatchBuilderSignatureComparer();
        private MatchBuilderSignatureComparer() { }

        public bool Equals(MatchBuilderSignature x, MatchBuilderSignature y)
        {
            if (ReferenceEquals(x, y)) { return true; }
            if (x is null || y is null) { return false; }
            return x.ListsEqual(y);
        }

        public int GetHashCode(MatchBuilderSignature obj) => obj.GetHashCode();
    }
}

