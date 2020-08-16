//-----------------------------------------------------------------------
// <copyright file="ReplicatorMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.DistributedData.Internal;
using Akka.Serialization;
using Akka.Util.Internal;
using MessagePack;
using DataEnvelope = Akka.DistributedData.Internal.DataEnvelope;
using DeltaPropagation = Akka.DistributedData.Internal.DeltaPropagation;
using Gossip = Akka.DistributedData.Internal.Gossip;
using OtherMessage = Akka.Serialization.Protocol.Payload;
using Read = Akka.DistributedData.Internal.Read;
using ReadResult = Akka.DistributedData.Internal.ReadResult;
using Status = Akka.DistributedData.Internal.Status;
using UniqueAddress = Akka.Cluster.UniqueAddress;
using Write = Akka.DistributedData.Internal.Write;

namespace Akka.DistributedData.Serialization
{
    public sealed class ReplicatorMessageSerializer : SerializerWithStringManifest, IRunnable
    {
        #region internal classes

        private sealed class SmallCache<TKey, TVal>
            where TKey : class
            where TVal : class
        {
            private readonly TimeSpan _ttl;
            private readonly Func<TKey, TVal> _getOrAddFactory;
            private readonly AtomicCounter _n = new AtomicCounter(0);
            private readonly int _mask;
            private readonly KeyValuePair<TKey, TVal>[] _elements;

            private DateTime _lastUsed;

            public SmallCache(int capacity, TimeSpan ttl, Func<TKey, TVal> getOrAddFactory)
            {
                _mask = capacity - 1;
                if ((capacity & _mask) != 0) throw new ArgumentException("Capacity must be power of 2 and less than or equal 32", nameof(capacity));
                if (capacity > 32) throw new ArgumentException("Capacity must be less than or equal 32", nameof(capacity));

                _ttl = ttl;
                _getOrAddFactory = getOrAddFactory;
                _elements = new KeyValuePair<TKey, TVal>[capacity];
                _lastUsed = DateTime.UtcNow;
            }

            public TVal this[TKey key]
            {
                get { return Get(key, _n.Current); }
                set { Add(key, value); }
            }

            /// <summary>
            /// Add value under specified key. Overrides existing entry.
            /// </summary>
            public void Add(TKey key, TVal value) => Add(new KeyValuePair<TKey, TVal>(key, value));

            /// <summary>
            /// Add an entry to the cache. Overrides existing entry.
            /// </summary>
            public void Add(KeyValuePair<TKey, TVal> entry)
            {
                var i = _n.IncrementAndGet();
                _elements[i & _mask] = entry;
                _lastUsed = DateTime.UtcNow;
            }

            public TVal GetOrAdd(TKey key)
            {
                var position = _n.Current;
                var c = Get(key, position);
                if (!ReferenceEquals(c, null)) return c;
                var b2 = _getOrAddFactory(key);
                if (position == _n.Current)
                {
                    // no change, add the new value
                    Add(key, b2);
                    return b2;
                }
                else
                {
                    // some other thread added, try one more time
                    // to reduce duplicates
                    var c2 = Get(key, _n.Current);
                    if (!ReferenceEquals(c2, null)) return c2;
                    else
                    {
                        Add(key, b2);
                        return b2;
                    }
                }
            }

            /// <summary>
            /// Remove all elements if the if cache has not been used within <see cref="_ttl"/>.
            /// </summary>
            public void Evict()
            {
                if (DateTime.UtcNow - _lastUsed > _ttl)
                {
                    _elements.Initialize();
                }
            }

            private TVal Get(TKey key, int startIndex)
            {
                var end = startIndex + _elements.Length;
                _lastUsed = DateTime.UtcNow;
                var i = startIndex;
                while (end - i == 0)
                {
                    var x = _elements[i & _mask];
                    if (x.Key != key) i++;
                    else return x.Value;
                }

                return null;
            }
        }

        #endregion

        #region manifests

        private const string Get_Manifest = "A";
        private const string GetSuccessManifest = "B";
        private const string NotFoundManifest = "C";
        private const string GetFailureManifest = "D";
        private const string SubscribeManifest = "E";
        private const string UnsubscribeManifest = "F";
        private const string ChangedManifest = "G";
        private const string DataEnvelopeManifest = "H";
        private const string WriteManifest = "I";
        private const string WriteAckManifest = "J";
        private const string ReadManifest = "K";
        private const string ReadResultManifest = "L";
        private const string StatusManifest = "M";
        private const string GossipManifest = "N";
        private const string WriteNackManifest = "O";
        private const string DurableDataEnvelopeManifest = "P";
        private const string DeltaPropagationManifest = "Q";
        private const string DeltaNackManifest = "R";

        private static readonly IFormatterResolver s_defaultResolver;
        private static readonly Dictionary<Type, string> s_manifestMap;

        static ReplicatorMessageSerializer()
        {
            s_defaultResolver = MessagePackSerializer.DefaultResolver;
            s_manifestMap = new Dictionary<Type, string>()
            {
                { typeof(DataEnvelope), DataEnvelopeManifest },
                { typeof(Write), WriteManifest },
                { typeof(WriteAck), WriteAckManifest },
                { typeof(Read), ReadManifest },
                { typeof(ReadResult), ReadResultManifest },
                { typeof(DeltaPropagation), DeltaPropagationManifest },
                { typeof(Status), StatusManifest },
                { typeof(Get), Get_Manifest },
                { typeof(GetSuccess), GetSuccessManifest },
                { typeof(Durable.DurableDataEnvelope), DurableDataEnvelopeManifest },
                { typeof(Changed), ChangedManifest },
                { typeof(NotFound), NotFoundManifest },
                { typeof(GetFailure), GetFailureManifest },
                { typeof(Subscribe), SubscribeManifest },
                { typeof(Unsubscribe), UnsubscribeManifest },
                { typeof(Gossip), GossipManifest },
                { typeof(WriteNack), WriteNackManifest },
                { typeof(DeltaNack), DeltaNackManifest },
            };
        }

        #endregion

        private readonly SerializationSupport _ser;

        private readonly SmallCache<Read, byte[]> _readCache;
        private readonly SmallCache<Write, byte[]> _writeCache;
        private readonly byte[] _empty = Array.Empty<byte>();

        public ReplicatorMessageSerializer(Akka.Actor.ExtendedActorSystem system) : base(system)
        {
            _ser = new SerializationSupport(system);
            var cacheTtl = system.Settings.Config.GetTimeSpan("akka.cluster.distributed-data.serializer-cache-time-to-live");
            _readCache = new SmallCache<Read, byte[]>(4, cacheTtl, m => MessagePackSerializer.Serialize(ReadToProto(m), s_defaultResolver));
            _writeCache = new SmallCache<Write, byte[]>(4, cacheTtl, m => MessagePackSerializer.Serialize(WriteToProto(m), s_defaultResolver));

            system.Scheduler.Advanced.ScheduleRepeatedly(cacheTtl, new TimeSpan(cacheTtl.Ticks / 2), this);
        }

        void IRunnable.Run()
        {
            _readCache.Evict();
            _writeCache.Evict();
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            var manifestMap = s_manifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            throw GetSerializeException(type);
        }

        public override string Manifest(object o)
        {
            switch (o)
            {
                case DataEnvelope _: return DataEnvelopeManifest;
                case Write _: return WriteManifest;
                case WriteAck _: return WriteAckManifest;
                case Read _: return ReadManifest;
                case ReadResult _: return ReadResultManifest;
                case DeltaPropagation _: return DeltaPropagationManifest;
                case Status _: return StatusManifest;
                case Get _: return Get_Manifest;
                case GetSuccess _: return GetSuccessManifest;
                case Durable.DurableDataEnvelope _: return DurableDataEnvelopeManifest;
                case Changed _: return ChangedManifest;
                case NotFound _: return NotFoundManifest;
                case GetFailure _: return GetFailureManifest;
                case Subscribe _: return SubscribeManifest;
                case Unsubscribe _: return UnsubscribeManifest;
                case Gossip _: return GossipManifest;
                case WriteNack _: return WriteNackManifest;
                case DeltaNack _: return DeltaNackManifest;

                default: throw GetSerializeException(o);
            }
        }

        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case DataEnvelope envelope:
                    manifest = DataEnvelopeManifest;
                    return MessagePackSerializer.Serialize(DataEnvelopeToProto(envelope), s_defaultResolver);
                case Write write:
                    manifest = WriteManifest;
                    return _writeCache.GetOrAdd(write);
                case WriteAck _:
                    manifest = WriteAckManifest;
                    return _empty;
                case Read read:
                    manifest = ReadManifest;
                    return _readCache.GetOrAdd(read);
                case ReadResult result:
                    manifest = ReadResultManifest;
                    return MessagePackSerializer.Serialize(ReadResultToProto(result), s_defaultResolver);
                case DeltaPropagation propagation:
                    manifest = DeltaPropagationManifest;
                    return MessagePackSerializer.Serialize(DeltaPropagationToProto(propagation), s_defaultResolver);
                case Status status:
                    manifest = StatusManifest;
                    return MessagePackSerializer.Serialize(StatusToProto(status), s_defaultResolver);
                case Get get:
                    manifest = Get_Manifest;
                    return MessagePackSerializer.Serialize(GetToProto(get), s_defaultResolver);
                case GetSuccess success:
                    manifest = GetSuccessManifest;
                    return MessagePackSerializer.Serialize(GetSuccessToProto(success), s_defaultResolver);
                case Durable.DurableDataEnvelope envelope:
                    manifest = DurableDataEnvelopeManifest;
                    return MessagePackSerializer.Serialize(DurableDataEnvelopeToProto(envelope), s_defaultResolver);
                case Changed changed:
                    manifest = ChangedManifest;
                    return MessagePackSerializer.Serialize(ChangedToProto(changed), s_defaultResolver);
                case NotFound found:
                    manifest = NotFoundManifest;
                    return MessagePackSerializer.Serialize(NotFoundToProto(found), s_defaultResolver);
                case GetFailure failure:
                    manifest = GetFailureManifest;
                    return MessagePackSerializer.Serialize(GetFailureToProto(failure), s_defaultResolver);
                case Subscribe subscribe:
                    manifest = SubscribeManifest;
                    return MessagePackSerializer.Serialize(SubscribeToProto(subscribe), s_defaultResolver);
                case Unsubscribe unsubscribe:
                    manifest = UnsubscribeManifest;
                    return MessagePackSerializer.Serialize(UnsubscribeToProto(unsubscribe), s_defaultResolver);
                case Gossip gossip:
                    manifest = GossipManifest;
                    return LZ4MessagePackSerializer.Serialize(GossipToProto(gossip), s_defaultResolver);
                case WriteNack _:
                    manifest = WriteNackManifest;
                    return _empty;
                case DeltaNack _:
                    manifest = DeltaNackManifest;
                    return _empty;

                default: throw GetSerializeException(obj);
            }
        }

        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case DataEnvelopeManifest: return DataEnvelopeFromBinary(bytes);
                case WriteManifest: return WriteFromBinary(bytes);
                case WriteAckManifest: return WriteAck.Instance;
                case ReadManifest: return ReadFromBinary(bytes);
                case ReadResultManifest: return ReadResultFromBinary(bytes);
                case DeltaPropagationManifest: return DeltaPropagationFromBinary(bytes);
                case StatusManifest: return StatusFromBinary(bytes);
                case Get_Manifest: return GetFromBinary(bytes);
                case GetSuccessManifest: return GetSuccessFromBinary(bytes);
                case DurableDataEnvelopeManifest: return DurableDataEnvelopeFromBinary(bytes);
                case ChangedManifest: return ChangedFromBinary(bytes);
                case NotFoundManifest: return NotFoundFromBinary(bytes);
                case GetFailureManifest: return GetFailureFromBinary(bytes);
                case SubscribeManifest: return SubscribeFromBinary(bytes);
                case UnsubscribeManifest: return UnsubscribeFromBinary(bytes);
                case GossipManifest: return GossipFromLZ4Binary(bytes);
                case WriteNackManifest: return WriteNack.Instance;
                case DeltaNackManifest: return DeltaNack.Instance;

                default: throw GetDeserializationException(manifest);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetSerializeException(object o)
        {
            var type = o is Type t ? t : o.GetType();
            return new ArgumentException($"Can't serialize object of type [{type.FullName}] using [{nameof(ReplicatorMessageSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetDeserializationException(string manifest)
        {
            return new ArgumentException($"Unimplemented deserialization of message with manifest '{manifest}' using [{nameof(ReplicatorMessageSerializer)}]");
        }

        [MethodImpl(InlineOptions.AggressiveOptimization)]
        private OtherMessage OtherMessageToProto(object msg)
        {
            return _ser.OtherMessageToProto(msg);
        }


        private Protocol.Gossip GossipToProto(Gossip gossip)
        {
            List<Protocol.Gossip.Entry> entries = null;
            var updatedData = gossip.UpdatedData;
            if (updatedData is object)
            {
                entries = new List<Protocol.Gossip.Entry>(updatedData.Count);
                foreach (var entry in updatedData)
                {
                    entries.Add(new Protocol.Gossip.Entry(entry.Key, DataEnvelopeToProto(entry.Value)));
                }
            }

            bool hasToSystemUid = false;
            long toSystemUid = 0L;
            if (gossip.ToSystemUid.HasValue)
            {
                hasToSystemUid = true;
                toSystemUid = gossip.ToSystemUid.Value;
            }

            bool hasFromSystemUid = false;
            long fromSystemUid = 0L;
            if (gossip.FromSystemUid.HasValue)
            {
                hasFromSystemUid = true;
                fromSystemUid = gossip.FromSystemUid.Value;
            }

            return new Protocol.Gossip(gossip.SendBack, entries, hasToSystemUid, hasFromSystemUid, toSystemUid, fromSystemUid);
        }
        private Gossip GossipFromLZ4Binary(in ReadOnlySpan<byte> bytes)
        {
            var proto = LZ4MessagePackSerializer.Deserialize<Protocol.Gossip>(bytes, s_defaultResolver);
            var builder = ImmutableDictionary<string, DataEnvelope>.Empty.ToBuilder();
            var protoEntries = proto.Entries;
            if (protoEntries is object)
            {
                for (int idx = 0; idx < protoEntries.Count; idx++)
                {
                    Protocol.Gossip.Entry entry = protoEntries[idx];
                    builder.Add(entry.Key, DataEnvelopeFromProto(entry.Envelope));
                }
            }

            return new Gossip(
                builder.ToImmutable(),
                proto.SendBack,
                proto.HasToSystemUid ? (long?)proto.ToSystemUid : null,
                proto.HasFromSystemUid ? (long?)proto.FromSystemUid : null);
        }


        private Protocol.Unsubscribe UnsubscribeToProto(Unsubscribe unsubscribe)
        {
            return new Protocol.Unsubscribe(
                OtherMessageToProto(unsubscribe.Key),
                Akka.Serialization.Serialization.SerializedActorPath(unsubscribe.Subscriber)
            );
        }
        private Unsubscribe UnsubscribeFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Unsubscribe>(bytes, s_defaultResolver);
            var actorRef = _system.Provider.ResolveActorRef(proto.Ref);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            return new Unsubscribe(key, actorRef);
        }


        private Protocol.Subscribe SubscribeToProto(Subscribe msg)
        {
            return new Protocol.Subscribe(
                OtherMessageToProto(msg.Key),
                Akka.Serialization.Serialization.SerializedActorPath(msg.Subscriber)
            );
        }
        private Subscribe SubscribeFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Subscribe>(bytes, s_defaultResolver);
            var actorRef = _system.Provider.ResolveActorRef(proto.Ref);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            return new Subscribe(key, actorRef);
        }


        private Protocol.GetFailure GetFailureToProto(GetFailure msg)
        {
            return new Protocol.GetFailure(
                OtherMessageToProto(msg.Key),
                OtherMessageToProto(msg.Request)
            );
        }
        private GetFailure GetFailureFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.GetFailure>(bytes, s_defaultResolver);
            var request = _ser.OtherMessageFromProto(proto.Request);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            return new GetFailure(key, request);
        }


        private Protocol.NotFound NotFoundToProto(NotFound msg)
        {
            return new Protocol.NotFound(
                OtherMessageToProto(msg.Key),
                OtherMessageToProto(msg.Request)
            );
        }
        private NotFound NotFoundFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.NotFound>(bytes, s_defaultResolver);
            var request = _ser.OtherMessageFromProto(proto.Request);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            return new NotFound(key, request);
        }


        private Protocol.DurableDataEnvelope DurableDataEnvelopeToProto(Durable.DurableDataEnvelope msg)
        {
            var data = OtherMessageToProto(msg.Data);
            var pruning = msg.DataEnvelope.Pruning;

            List<Protocol.DataEnvelope.PruningEntry> pruningEntries = null;
            if (pruning is object)
            {
                pruningEntries = new List<Protocol.DataEnvelope.PruningEntry>(pruning.Count);
                // only keep the PruningPerformed entries
                foreach (var p in pruning)
                {
                    if (p.Value is PruningPerformed)
                    {
                        pruningEntries.Add(PruningToProto(p.Key, p.Value));
                    }
                }
            }
            return new Protocol.DurableDataEnvelope(data, pruningEntries);
        }
        private Durable.DurableDataEnvelope DurableDataEnvelopeFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.DurableDataEnvelope>(bytes, s_defaultResolver);
            var data = (IReplicatedData)_ser.OtherMessageFromProto(proto.Data);
            var pruning = PruningFromProto(proto.Pruning);

            return new Durable.DurableDataEnvelope(new DataEnvelope(data, pruning));
        }


        private Protocol.DataEnvelope.PruningEntry PruningToProto(UniqueAddress addr, IPruningState pruning)
        {
            var proto = new Protocol.DataEnvelope.PruningEntry
            {
                RemovedAddress = SerializationSupport.UniqueAddressToProto(addr)
            };
            switch (pruning)
            {
                case PruningPerformed performed:
                    proto.Performed = true;
                    proto.ObsoleteTime = performed.ObsoleteTime.Ticks;
                    break;
                case PruningInitialized init:
                    proto.Performed = false;
                    proto.OwnerAddress = SerializationSupport.UniqueAddressToProto(init.Owner);
                    var initSeen = init.Seen;
                    if (initSeen is object)
                    {
                        var seen = new List<Protocol.Address>(initSeen.Count);
                        foreach (var address in initSeen)
                        {
                            seen.Add(SerializationSupport.AddressToProto(address));
                        }
                        proto.Seen = seen;
                    }
                    break;
            }

            return proto;
        }
        private ImmutableDictionary<UniqueAddress, IPruningState> PruningFromProto(List<Protocol.DataEnvelope.PruningEntry> pruning)
        {
            if (pruning is null || 0u >= (uint)pruning.Count)
            {
                return ImmutableDictionary<UniqueAddress, IPruningState>.Empty;
            }

            var builder = ImmutableDictionary<UniqueAddress, IPruningState>.Empty.ToBuilder();

            foreach (var entry in pruning)
            {
                var removed = _ser.UniqueAddressFromProto(entry.RemovedAddress);
                if (entry.Performed)
                {
                    builder.Add(removed, new PruningPerformed(new DateTime(entry.ObsoleteTime)));
                }
                else
                {
                    Actor.Address[] seen = null;
                    if (entry.Seen is object)
                    {
                        seen = entry.Seen.Select(x => _ser.AddressFromProto(x)).ToArray();
                    }
                    builder.Add(removed, new PruningInitialized(_ser.UniqueAddressFromProto(entry.OwnerAddress), seen));
                }
            }

            return builder.ToImmutable();
        }


        private Protocol.Changed ChangedToProto(Changed msg)
        {
            return new Protocol.Changed(
                OtherMessageToProto(msg.Key),
                OtherMessageToProto(msg.Data)
            );
        }
        private Changed ChangedFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Changed>(bytes, s_defaultResolver);
            var data = _ser.OtherMessageFromProto(proto.Data);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            return new Changed(key, data);
        }


        private Protocol.GetSuccess GetSuccessToProto(GetSuccess msg)
        {
            return new Protocol.GetSuccess(
                OtherMessageToProto(msg.Key),
                OtherMessageToProto(msg.Data),
                OtherMessageToProto(msg.Request)
            );
        }
        private GetSuccess GetSuccessFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.GetSuccess>(bytes, s_defaultResolver);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            var data = (IReplicatedData)_ser.OtherMessageFromProto(proto.Data);
            var request = _ser.OtherMessageFromProto(proto.Request);

            return new GetSuccess(key, request, data);
        }


        private Protocol.Get GetToProto(Get msg)
        {
            var timeoutInMilis = msg.Consistency.Timeout.TotalMilliseconds;
            if (timeoutInMilis > 0XFFFFFFFFL)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_Timeouts_must_fit_in_a_32_bit_unsigned_int();
            }

            int consistencyValue = 1;
            int consistencyMinCap = 0;
            switch (msg.Consistency)
            {
                case ReadLocal _: consistencyValue = 1; break;
                case ReadFrom r: consistencyValue = r.N; break;
                case ReadMajority rm:
                    consistencyValue = 0;
                    consistencyMinCap = rm.MinCapacity;
                    break;
                case ReadAll _: consistencyValue = -1; break;
            }

            return new Protocol.Get(
                 OtherMessageToProto(msg.Key),
                 consistencyValue,
                 (uint)timeoutInMilis,
                 OtherMessageToProto(msg.Request),
                 consistencyMinCap
            );
        }
        private Get GetFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Get>(bytes, s_defaultResolver);
            var key = (IKey)_ser.OtherMessageFromProto(proto.Key);
            var request = _ser.OtherMessageFromProto(proto.Request);
            var timeout = new TimeSpan(proto.Timeout * TimeSpan.TicksPerMillisecond);
            IReadConsistency consistency;
            switch (proto.Consistency)
            {
                case 0: consistency = new ReadMajority(timeout, proto.ConsistencyMinCap); break;
                case -1: consistency = new ReadAll(timeout); break;
                case 1: consistency = ReadLocal.Instance; break;
                default: consistency = new ReadFrom(proto.Consistency, timeout); break;
            }
            return new Get(key, consistency, request);
        }


        private Protocol.Status StatusToProto(Status status)
        {
            List<Protocol.Status.Entry> entries = null;
            var digests = status.Digests;
            if (digests is object)
            {
                entries = new List<Protocol.Status.Entry>(digests.Count);
                foreach (var entry in digests)
                {
                    entries.Add(new Protocol.Status.Entry(entry.Key, entry.Value));
                }
            }

            bool hasToSystemUid = false;
            long toSystemUid = 0L;
            if (status.ToSystemUid.HasValue)
            {
                hasToSystemUid = true;
                toSystemUid = status.ToSystemUid.Value;
            }

            bool hasFromSystemUid = false;
            long fromSystemUid = 0L;
            if (status.FromSystemUid.HasValue)
            {
                hasFromSystemUid = true;
                fromSystemUid = status.FromSystemUid.Value;
            }
            return new Protocol.Status((uint)status.Chunk, (uint)status.TotalChunks, entries,
                hasToSystemUid, hasFromSystemUid, toSystemUid, fromSystemUid);
        }
        private Status StatusFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Status>(bytes, s_defaultResolver);
            var builder = ImmutableDictionary<string, byte[]>.Empty.ToBuilder();

            var protoEntries = proto.Entries;
            if (protoEntries is object)
            {
                for (int idx = 0; idx < protoEntries.Count; idx++)
                {
                    var entry = protoEntries[idx];
                    builder.Add(entry.Key, entry.Digest);
                }
            }

            return new Status(
                builder.ToImmutable(),
                (int)proto.Chunk,
                (int)proto.TotChunks,
                proto.HasToSystemUid ? (long?)proto.ToSystemUid : null,
                proto.HasFromSystemUid ? (long?)proto.FromSystemUid : null);
        }


        private Protocol.DeltaPropagation DeltaPropagationToProto(DeltaPropagation msg)
        {
            List<Protocol.DeltaPropagation.Entry> entries = null;
            var deltas = msg.Deltas;
            if (deltas is object)
            {
                entries = new List<Protocol.DeltaPropagation.Entry>(deltas.Count);
                foreach (var entry in msg.Deltas)
                {
                    var d = entry.Value;
                    var toSeqNr = d.ToSeqNr != d.FromSeqNr ? d.ToSeqNr : 0L;
                    var delta = new Protocol.DeltaPropagation.Entry
                    (
                         entry.Key,
                         DataEnvelopeToProto(d.DataEnvelope),
                         d.FromSeqNr,
                         toSeqNr
                    );
                    entries.Add(delta);
                }
            }

            return new Protocol.DeltaPropagation
            (
                SerializationSupport.UniqueAddressToProto(msg.FromNode),
                entries,
                msg.ShouldReply
            );
        }
        private DeltaPropagation DeltaPropagationFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.DeltaPropagation>(bytes, s_defaultResolver);
            var reply = proto.Reply;
            var fromNode = _ser.UniqueAddressFromProto(proto.FromNode);

            var builder = ImmutableDictionary<string, Delta>.Empty.ToBuilder();
            var protoEntries = proto.Entries;
            if (protoEntries is object)
            {
                for (int idx = 0; idx < protoEntries.Count; idx++)
                {
                    var entry = protoEntries[idx];
                    var fromSeqNr = entry.FromSeqNr;
                    var toSeqNr = 0ul >= (ulong)entry.ToSeqNr ? fromSeqNr : entry.ToSeqNr;
                    builder.Add(entry.Key, new Delta(DataEnvelopeFromProto(entry.Envelope), fromSeqNr, toSeqNr));
                }
            }

            return new DeltaPropagation(fromNode, reply, builder.ToImmutable());
        }


        private Protocol.ReadResult ReadResultToProto(ReadResult msg)
        {
            Protocol.DataEnvelope envelope = null;
            if (msg.Envelope != null)
            {
                envelope = DataEnvelopeToProto(msg.Envelope);
            }
            return new Protocol.ReadResult(envelope);
        }
        private ReadResult ReadResultFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ReadResult>(bytes, s_defaultResolver);
            var envelope = proto.Envelope is object
                ? DataEnvelopeFromProto(proto.Envelope)
                : null;
            return new ReadResult(envelope);
        }


        private Protocol.DataEnvelope DataEnvelopeToProto(DataEnvelope msg)
        {
            var proto = new Protocol.DataEnvelope
            {
                Data = OtherMessageToProto(msg.Data)
            };

            var msgPruning = msg.Pruning;
            if (msgPruning is object)
            {
                var pruning = new List<Protocol.DataEnvelope.PruningEntry>(msgPruning.Count);
                foreach (var entry in msgPruning)
                {
                    pruning.Add(PruningToProto(entry.Key, entry.Value));
                }
                proto.Pruning = pruning;
            }

            if (!msg.DeltaVersions.IsEmpty)
            {
                proto.DeltaVersions = SerializationSupport.VersionVectorToProto(msg.DeltaVersions);
            }

            return proto;
        }
        private DataEnvelope DataEnvelopeFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.DataEnvelope>(bytes, s_defaultResolver);
            return DataEnvelopeFromProto(proto);
        }
        private DataEnvelope DataEnvelopeFromProto(Protocol.DataEnvelope proto)
        {
            var data = (IReplicatedData)_ser.OtherMessageFromProto(proto.Data);
            var pruning = PruningFromProto(proto.Pruning);
            var vvector = _ser.VersionVectorFromProto(proto.DeltaVersions) ?? VersionVector.Empty;

            return new DataEnvelope(data, pruning, vvector);
        }


        private Protocol.Write WriteToProto(Write write)
        {
            return new Protocol.Write(write.Key, DataEnvelopeToProto(write.Envelope),
                write.FromNode is object ? SerializationSupport.UniqueAddressToProto(write.FromNode) : null);
        }
        private Write WriteFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Write>(bytes, s_defaultResolver);
            var fromNode = proto.FromNode != null ? _ser.UniqueAddressFromProto(proto.FromNode) : null;
            return new Write(proto.Key, DataEnvelopeFromProto(proto.Envelope), fromNode);
        }


        private Protocol.Read ReadToProto(Read read)
        {
            return new Protocol.Read(read.Key,
                read.FromNode is object ? SerializationSupport.UniqueAddressToProto(read.FromNode) : null);
        }
        private Read ReadFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.Read>(bytes, s_defaultResolver);
            var fromNode = proto.FromNode != null ? _ser.UniqueAddressFromProto(proto.FromNode) : null;
            return new Read(proto.Key, fromNode);
        }
    }
}