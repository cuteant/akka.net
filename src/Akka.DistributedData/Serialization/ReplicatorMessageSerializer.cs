﻿//-----------------------------------------------------------------------
// <copyright file="ReplicatorMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Akka.DistributedData.Internal;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.Buffers;
using Hyperion;
using Serializer = Akka.Serialization.Serializer;

namespace Akka.DistributedData.Serialization
{
    public sealed class ReplicatorMessageSerializer : Serializer, IRunnable
    {
        #region internal classes

        private sealed class SmallCache<TKey, TVal>
            where TKey : class
            where TVal : class
        {
            private readonly TimeSpan ttl;
            private readonly Func<TKey, TVal> getOrAddFactory;
            private readonly AtomicCounter n = new AtomicCounter(0);
            private readonly int mask;
            private readonly KeyValuePair<TKey, TVal>[] elements;

            private DateTime lastUsed;

            public SmallCache(int capacity, TimeSpan ttl, Func<TKey, TVal> getOrAddFactory)
            {
                mask = capacity - 1;
                if ((capacity & mask) != 0) ThrowHelper.ThrowArgumentException_CapacityMustBe2_32();
                if (capacity > 32) ThrowHelper.ThrowArgumentException_CapacityMustBeLessThanOrEqual32();

                this.ttl = ttl;
                this.getOrAddFactory = getOrAddFactory;
                this.elements = new KeyValuePair<TKey, TVal>[capacity];
                this.lastUsed = DateTime.UtcNow;
            }

            public TVal this[TKey key]
            {
                get { return Get(key, n.Current); }
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
                var i = n.IncrementAndGet();
                elements[i & mask] = entry;
                lastUsed = DateTime.UtcNow;
            }

            public TVal GetOrAdd(TKey key)
            {
                var position = n.Current;
                var c = Get(key, position);
                if (!(c is null)) return c;
                var b2 = getOrAddFactory(key);
                if (position == n.Current)
                {
                    // no change, add the new value
                    Add(key, b2);
                    return b2;
                }
                else
                {
                    // some other thread added, try one more time
                    // to reduce duplicates
                    var c2 = Get(key, n.Current);
                    if (!(c2 is null)) return c2;
                    else
                    {
                        Add(key, b2);
                        return b2;
                    }
                }
            }

            /// <summary>
            /// Remove all elements if the if cache has not been used within <see cref="ttl"/>.
            /// </summary>
            public void Evict()
            {
                if (DateTime.UtcNow - lastUsed > ttl)
                {
                    elements.Initialize();
                }
            }

            private TVal Get(TKey key, int startIndex)
            {
                var end = startIndex + elements.Length;
                lastUsed = DateTime.UtcNow;
                var i = startIndex;
                while (end - i == 0)
                {
                    var x = elements[i & mask];
                    if (x.Key != key) i++;
                    else return x.Value;
                }

                return null;
            }
        }

        #endregion

        private static readonly ArrayPool<byte> s_sharedBuffer = BufferManager.Shared;
        public static readonly Type WriteAckType = typeof(WriteAck);

        private readonly SmallCache<Read, byte[]> _readCache;
        private readonly SmallCache<Write, byte[]> _writeCache;
        private readonly Hyperion.Serializer _serializer;
        private readonly byte[] _writeAckBytes;

        public ReplicatorMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _serializeFunc = Serialize;

            var cacheTtl = system.Settings.Config.GetTimeSpan("akka.cluster.distributed-data.serializer-cache-time-to-live");
            _readCache = new SmallCache<Read, byte[]>(4, cacheTtl, _serializeFunc);
            _writeCache = new SmallCache<Write, byte[]>(4, cacheTtl, _serializeFunc);

            var akkaSurrogate =
                Hyperion.Surrogate.Create<ISurrogated, ISurrogate>(
                    toSurrogate: from => from.ToSurrogate(system),
                    fromSurrogate: to => to.FromSurrogate(system));

            _serializer = new Hyperion.Serializer(new SerializerOptions(
                preserveObjectReferences: true,
                versionTolerance: true,
                surrogates: new[] { akkaSurrogate },
                knownTypes: new[]
                {
                    typeof(Get),
                    typeof(GetSuccess),
                    typeof(GetFailure),
                    typeof(NotFound),
                    typeof(Subscribe),
                    typeof(Unsubscribe),
                    typeof(Changed),
                    typeof(DataEnvelope),
                    typeof(Write),
                    typeof(WriteAck),
                    typeof(Read),
                    typeof(ReadResult),
                    typeof(Internal.Status),
                    typeof(Gossip)
                }));

            using (var stream = new MemoryStream())
            {
                _serializer.Serialize(WriteAck.Instance, stream);
                stream.Position = 0;
                _writeAckBytes = stream.ToArray();
            }

            system.Scheduler.Advanced.ScheduleRepeatedly(cacheTtl, new TimeSpan(cacheTtl.Ticks / 2), this);
        }

        void IRunnable.Run()
        {
            _readCache.Evict();
            _writeCache.Evict();
        }

        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case Write write:
                    return _writeCache.GetOrAdd(write);
                case Read read:
                    return _readCache.GetOrAdd(read);
                case WriteAck _:
                    return _writeAckBytes;
                default:
                    return Serialize(obj, _serializer);
            }
        }

        private readonly Func<object, byte[]> _serializeFunc;
        private byte[] Serialize(object obj) => Serialize(obj, _serializer);

        private const int c_initialBufferSize = 1024 * 80;
        private static byte[] Serialize(object obj, Hyperion.Serializer serializer)
        {
            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(c_initialBufferSize, s_sharedBuffer);

                serializer.Serialize(obj, outputStream);
                return outputStream.ToByteArray();
            }
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == WriteAckType) return WriteAck.Instance;
            using (var inputStream = new MemoryStream(bytes))
            {
                return _serializer.Deserialize(inputStream);
            }
        }
    }
}
