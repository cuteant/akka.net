﻿//-----------------------------------------------------------------------
// <copyright file="FanInShape.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Akka.Streams
{
    /// <summary>
    /// TBD
    /// </summary>
    /// <typeparam name="TOut">TBD</typeparam>
    public abstract class FanInShape<TOut> : Shape
    {
        #region internal classes

        /// <summary>
        /// TBD
        /// </summary>
        public interface IInit
        {
            /// <summary>
            /// TBD
            /// </summary>
            Outlet<TOut> Outlet { get; }
            /// <summary>
            /// TBD
            /// </summary>
            IEnumerable<Inlet> Inlets { get; }
            /// <summary>
            /// TBD
            /// </summary>
            string Name { get; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        public sealed class InitName : IInit
        {
            private readonly string _name;
            private readonly Outlet<TOut> _outlet;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="name">TBD</param>
            /// <exception cref="ArgumentException">TBD</exception>
            public InitName(string name)
            {
                if (string.IsNullOrEmpty(name)) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);

                _name = name;
                _outlet = new Outlet<TOut>(name + ".out");
            }

            /// <summary>
            /// TBD
            /// </summary>
            public Outlet<TOut> Outlet => _outlet;
            /// <summary>
            /// TBD
            /// </summary>
            public IEnumerable<Inlet> Inlets => Enumerable.Empty<Inlet>();
            /// <summary>
            /// TBD
            /// </summary>
            public string Name => _name;
        }

        /// <summary>
        /// TBD
        /// </summary>
        [Serializable]
        public sealed class InitPorts : IInit
        {
            private readonly Outlet<TOut> _outlet;
            private readonly IEnumerable<Inlet> _inlets;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="outlet">TBD</param>
            /// <param name="inlets">TBD</param>
            public InitPorts(Outlet<TOut> outlet, IEnumerable<Inlet> inlets)
            {
                if (null == outlet) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outlet); }
                if (null == inlets) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inlets); }
                _outlet = outlet;
                _inlets = inlets;
            }

            /// <summary>
            /// TBD
            /// </summary>
            public Outlet<TOut> Outlet => _outlet;
            /// <summary>
            /// TBD
            /// </summary>
            public IEnumerable<Inlet> Inlets => _inlets;
            /// <summary>
            /// TBD
            /// </summary>
            public string Name => "FanIn";
        }

        #endregion

        private ImmutableArray<Inlet> _inlets;
        private readonly IEnumerator<Inlet> _registered;
        private readonly string _name;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="outlet">TBD</param>
        /// <param name="registered">TBD</param>
        /// <param name="name">TBD</param>
        protected FanInShape(Outlet<TOut> outlet, IEnumerable<Inlet> registered, string name)
        {
            Out = outlet;
            Outlets = ImmutableArray.Create<Outlet>(outlet);
            _inlets = ImmutableArray<Inlet>.Empty;
            _name = name;

            _registered = registered.GetEnumerator();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="init">TBD</param>
        protected FanInShape(IInit init) : this(init.Outlet, init.Inlets, init.Name) { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="init">TBD</param>
        /// <returns>TBD</returns>
        protected abstract FanInShape<TOut> Construct(IInit init);

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="name">TBD</param>
        /// <returns>TBD</returns>
        protected Inlet<T> NewInlet<T>(string name)
        {
            var p = _registered.MoveNext() ? (Inlet<T>)_registered.Current : new Inlet<T>($"{_name}.{name}");
            _inlets = _inlets.Add(p);
            return p;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override ImmutableArray<Inlet> Inlets => _inlets;

        /// <summary>
        /// TBD
        /// </summary>
        public override ImmutableArray<Outlet> Outlets { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public Outlet<TOut> Out { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override Shape DeepCopy()
            => Construct(new InitPorts((Outlet<TOut>) Out.CarbonCopy(), _inlets.Select(i => i.CarbonCopy())));

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="inlets">TBD</param>
        /// <param name="outlets">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public sealed override Shape CopyFromPorts(ImmutableArray<Inlet> inlets, ImmutableArray<Outlet> outlets)
        {
            if (outlets.Length != 1) ThrowHelper.ThrowArgumentException_ProposedOutlets1(outlets);
            if (inlets.Length != Inlets.Length) ThrowHelper.ThrowArgumentException_ProposedInlets1(inlets);

            return Construct(new InitPorts((Outlet<TOut>)outlets[0], inlets));
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    /// <typeparam name="TIn">TBD</typeparam>
    /// <typeparam name="TOut">TBD</typeparam>
    public class UniformFanInShape<TIn, TOut> : FanInShape<TOut>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public readonly int N;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="n">TBD</param>
        /// <param name="init">TBD</param>
        public UniformFanInShape(int n, IInit init) : base(init)
        {
            N = n;
            Ins = Enumerable.Range(0, n).Select(i => NewInlet<TIn>($"in{i}")).ToImmutableList();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="n">TBD</param>
        public UniformFanInShape(int n) : this(n, new InitName("UniformFanIn"))
        {
            
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="n">TBD</param>
        /// <param name="name">TBD</param>
        public UniformFanInShape(int n, string name) : this(n, new InitName(name))
        {
            
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="outlet">TBD</param>
        /// <param name="inlets">TBD</param>
        public UniformFanInShape(Outlet<TOut> outlet, params Inlet<TIn>[] inlets)
            : this(inlets.Length, new InitPorts(outlet, inlets))
        {
            
        }

        /// <summary>
        /// TBD
        /// </summary>
        public IImmutableList<Inlet<TIn>> Ins { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="n">TBD</param>
        /// <returns>TBD</returns>
        public Inlet<TIn> In(int n) => Ins[n];

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="init">TBD</param>
        /// <returns>TBD</returns>
        protected override FanInShape<TOut> Construct(IInit init) => new UniformFanInShape<TIn, TOut>(N, init);
    }
}
