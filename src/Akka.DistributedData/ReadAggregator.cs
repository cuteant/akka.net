﻿//-----------------------------------------------------------------------
// <copyright file="ReadAggregator.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.DistributedData.Internal;
using System;
using System.Collections.Immutable;
using Akka.Event;

namespace Akka.DistributedData
{
    internal class ReadAggregator : ReadWriteAggregator
    {
        internal static Props Props(IKey key, IReadConsistency consistency, object req, IImmutableSet<Address> nodes, IImmutableSet<Address> unreachable, DataEnvelope localValue, IActorRef replyTo) =>
            Actor.Props.Create(() => new ReadAggregator(key, consistency, req, nodes, unreachable, localValue, replyTo)).WithDeploy(Deploy.Local);

        private readonly IKey _key;
        private readonly IReadConsistency _consistency;
        private readonly object _req;
        private readonly IActorRef _replyTo;
        private readonly Read _read;

        private DataEnvelope _result;

        public ReadAggregator(IKey key, IReadConsistency consistency, object req, IImmutableSet<Address> nodes, IImmutableSet<Address> unreachable, DataEnvelope localValue, IActorRef replyTo)
            : base(nodes, unreachable, consistency.Timeout)
        {
            _key = key;
            _consistency = consistency;
            _req = req;
            _replyTo = replyTo;
            _result = localValue;
            _read = new Read(key.Id);
            DoneWhenRemainingSize = GetDoneWhenRemainingSize();
        }
        protected override int DoneWhenRemainingSize { get; }

        private int GetDoneWhenRemainingSize()
        {
            switch (_consistency)
            {
                case ReadFrom readFrom:
                    return Nodes.Count - (readFrom.N - 1);

                case ReadAll _:
                    return 0;

                case ReadMajority readMajority:
                    var ncount = Nodes.Count + 1;
                    var w = CalculateMajorityWithMinCapacity(readMajority.MinCapacity, ncount);
                    return ncount - w;

                case ReadLocal _:
                    throw new ArgumentException("ReadAggregator does not support ReadLocal");

                default:
                    throw new ArgumentException("Invalid consistency level");
            }
        }

        protected override void PreStart()
        {
            var debugEnabled = Log.IsDebugEnabled;
            foreach (var n in PrimaryNodes)
            {
                var replica = Replica(n);
                if (debugEnabled) Log.Debug("Sending {0} to primary replica {1}", _read, replica);
                replica.Tell(_read);
            }

            if (Remaining.Count == DoneWhenRemainingSize)
                Reply(true);
            else if (DoneWhenRemainingSize < 0 || Remaining.Count < DoneWhenRemainingSize)
                Reply(false);
        }

        protected override bool Receive(object message)
        {
            switch (message)
            {
                case ReadResult x:
                    if (x.Envelope != null)
                    {
                        _result = _result?.Merge(x.Envelope) ?? x.Envelope;
                    }

                    Remaining = Remaining.Remove(Sender.Path.Address);
                    var done = DoneWhenRemainingSize;
                    if (Log.IsDebugEnabled) Log.Debug("remaining: {0}, done when: {1}, current state: {2}", Remaining.Count, done, _result);
                    if (Remaining.Count == done) Reply(true);
                    return true;

                case SendToSecondary _:
                    foreach (var n in SecondaryNodes)
                    {
                        Replica(n).Tell(_read);
                    }
                    return true;

                case ReceiveTimeout _:
                    Reply(false);
                    return true;

                default:
                    return false;
            }
        }

        private void Reply(bool ok)
        {
            if (ok && _result != null)
            {
                Context.Parent.Tell(new ReadRepair(_key.Id, _result));
                Context.Become(WaitRepairAck(_result));
            }
            else if (ok && _result == null)
            {
                _replyTo.Tell(new NotFound(_key, _req), Context.Parent);
                Context.Stop(Self);
            }
            else
            {
                _replyTo.Tell(new GetFailure(_key, _req), Context.Parent);
                Context.Stop(Self);
            }
        }

        private Receive WaitRepairAck(DataEnvelope envelope)
        {
            bool ReceiveFunc(object msg)
            {
                switch (msg)
                {
                    case ReadRepairAck _:
                        var reply = envelope.Data is DeletedData
                            ? (object)new DataDeleted(_key, null)
                            : new GetSuccess(_key, _req, envelope.Data);
                        _replyTo.Tell(reply, Context.Parent);
                        Context.Stop(Self);
                        return true;

                    case ReadResult _:
                        Remaining = Remaining.Remove(Sender.Path.Address);
                        return true;

                    case SendToSecondary _:
                    case ReceiveTimeout _:
                        return true;

                    default:
                        return false;
                }
            }
            return ReceiveFunc;
        }
    }

    public interface IReadConsistency
    {
        TimeSpan Timeout { get; }
    }

    public sealed class ReadLocal : IReadConsistency
    {
        public static readonly ReadLocal Instance = new ReadLocal();

        public TimeSpan Timeout => TimeSpan.Zero;

        private ReadLocal() { }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj != null && obj is ReadLocal;

        /// <inheritdoc/>
        public override string ToString() => "ReadLocal";
        /// <inheritdoc/>
        public override int GetHashCode() => nameof(ReadLocal).GetHashCode();
    }

    public sealed class ReadFrom : IReadConsistency, IEquatable<ReadFrom>
    {
        public int N { get; }

        public TimeSpan Timeout { get; }

        public ReadFrom(int n, TimeSpan timeout)
        {
            N = n;
            Timeout = timeout;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ReadFrom readFrom && Equals(readFrom);

        /// <inheritdoc/>
        public bool Equals(ReadFrom other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return N == other.N && Timeout.Equals(other.Timeout);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (N * 397) ^ Timeout.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"ReadFrom({N}, timeout={Timeout})";
    }

    public sealed class ReadMajority : IReadConsistency, IEquatable<ReadMajority>
    {
        public TimeSpan Timeout { get; }
        public int MinCapacity { get; }

        public ReadMajority(TimeSpan timeout, int minCapacity = 0)
        {
            Timeout = timeout;
            MinCapacity = minCapacity;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ReadMajority readMajority && Equals(readMajority);
        }

        /// <inheritdoc/>
        public override string ToString() => $"ReadMajority(timeout={Timeout})";

        /// <inheritdoc/>
        public bool Equals(ReadMajority other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Timeout.Equals(other.Timeout) && MinCapacity == other.MinCapacity;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Timeout.GetHashCode() * 397) ^ MinCapacity;
            }
        }
    }

    public sealed class ReadAll : IReadConsistency, IEquatable<ReadAll>
    {
        public TimeSpan Timeout { get; }

        public ReadAll(TimeSpan timeout)
        {
            Timeout = timeout;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ReadAll readAll && Equals(readAll);
        }

        /// <inheritdoc/>
        public override string ToString() => $"ReadAll(timeout={Timeout})";

        /// <inheritdoc/>
        public bool Equals(ReadAll other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Timeout.Equals(other.Timeout);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Timeout.GetHashCode();
        }
    }
}
