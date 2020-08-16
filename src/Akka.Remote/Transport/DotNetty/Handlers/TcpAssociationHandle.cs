//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using MessagePack;

namespace Akka.Remote.Transport.DotNetty
{
    internal class TcpAssociationHandle : AssociationHandle
    {
        protected static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;

        protected readonly IChannel _channel;

        public TcpAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress)
        {
            _channel = channel;
        }

        public override bool Write(object msg)
        {
            if (_channel.IsActive)
            {
                _channel.WriteAndFlushAsync(Serialize(msg));
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static IByteBuffer Serialize(object obj)
        {
            var formatter = s_defaultResolver.GetFormatterWithVerify<object>();

            var idx = 0;
            var writer = new MessagePackWriter(true);
            formatter.Serialize(ref writer, ref idx, obj, s_defaultResolver);
            return Unpooled.WrappedBuffer(writer.ToArray(idx));
        }

        public override void Disassociate()
        {
            _channel.CloseAsync();
        }
    }

    internal class PoolingTcpAssociationHandle : TcpAssociationHandle
    {
        public PoolingTcpAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress, transport, channel)
        {
        }

        public override bool Write(object msg)
        {
            if (_channel.IsActive)
            {
                _channel.WriteAndFlushAsync(SerializeWithPool(msg));
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static IByteBuffer SerializeWithPool(object obj)
        {
            var formatter = s_defaultResolver.GetFormatterWithVerify<object>();

            var idx = 0;
            var writer = new MessagePackWriter(false);
            formatter.Serialize(ref writer, ref idx, obj, s_defaultResolver);
            writer.DiscardBuffer(out var arrayPool, out var buffer);
            return ArrayPooled.WrappedBuffer(arrayPool, buffer, 0, idx);
        }
    }

    internal class TcpBatchWriterAssociationHandle : TcpAssociationHandle
    {
        public TcpBatchWriterAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress, transport, channel)
        {
        }

        public override bool Write(object msg)
        {
            if (_channel.IsActive)
            {
                _channel.WriteAsync(Serialize(msg));
                return true;
            }
            return false;
        }

        public override void Disassociate()
        {
            _channel.Flush(); // flush before we close
            _channel.CloseAsync();
        }
    }

    internal class PoolingTcpBatchWriterAssociationHandle : PoolingTcpAssociationHandle
    {
        public PoolingTcpBatchWriterAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress, transport, channel)
        {
        }

        public override bool Write(object msg)
        {
            if (_channel.IsActive)
            {
                _channel.WriteAsync(SerializeWithPool(msg));
                return true;
            }
            return false;
        }

        public override void Disassociate()
        {
            _channel.Flush(); // flush before we close
            _channel.CloseAsync();
        }
    }

    internal class TcpBatchMessagesAssociationHandle : AssociationHandle
    {
        protected static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;

        protected readonly IChannel _channel;

        protected readonly int _batchSize;
        protected int _status = TransportStatus.Idle;
        protected readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();

        protected int _channelStatus = ChannelStatus.Unknow;
        protected bool IsWritable
        {
            [MethodImpl(InlineOptions.AggressiveOptimization)]
            get
            {
                switch (Volatile.Read(ref _channelStatus))
                {
                    case ChannelStatus.Open:
                        return true;
                    case ChannelStatus.Closing:
                        return false;
                    case ChannelStatus.Closed:
                        return false;
                    default:
                        return CheckLastChannelStatus();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool CheckLastChannelStatus()
        {
            if (_channel.IsActive)
            {
                Interlocked.Exchange(ref _channelStatus, ChannelStatus.Open);
                return true;
            }
            else
            {
                Interlocked.Exchange(ref _channelStatus, ChannelStatus.Unknow);
                return false;
            }
        }

        public TcpBatchMessagesAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress)
        {
            _channel = channel;
            _batchSize = transport.Settings.BatchWriterSettings.TransferBatchSize;
        }

        public override bool Write(object payload)
        {
            if (IsWritable)
            {
                _queue.Enqueue(payload);
                if (TransportStatus.Idle >= (uint)Interlocked.CompareExchange(ref _status, TransportStatus.Busy, TransportStatus.Idle))
                {
                    Task.Factory.StartNew(s_processMessagesFunc, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                return true;
            }
            return false;
        }

        public override void Disassociate()
        {
            Interlocked.Exchange(ref _channelStatus, ChannelStatus.Closing);
            var spinner = new SpinWait();
            while ((uint)Volatile.Read(ref _channelStatus) < ChannelStatus.Closed)
            {
                spinner.SpinOnce();
            }
            _channel.CloseAsync();
        }

        static readonly Func<object, Task<bool>> s_processMessagesFunc = s => ProcessMessagesAsync(s);
        private static async Task<bool> ProcessMessagesAsync(object state)
        {
            var associationHandle = (TcpBatchMessagesAssociationHandle)state;

            var queue = associationHandle._queue;
            var batchSize = associationHandle._batchSize;
            var channel = associationHandle._channel;

            try
            {
                var batch = new List<object>(batchSize);
                while (true)
                {
                    batch.Clear();

                    while (queue.TryDequeue(out var msg))
                    {
                        batch.Add(msg);
                        if ((uint)batch.Count >= (uint)batchSize) { break; }
                    }

                    if (channel.IsActive)
                    {
                        await channel.WriteAndFlushAsync(Serialize(batch));

                        //Interlocked.CompareExchange(ref associationHandle._channelStatus, ChannelStatus.Open, ChannelStatus.Unknow);
                    }
                    else
                    {
                        Interlocked.Exchange(ref associationHandle._channelStatus, ChannelStatus.Closed);
                        Interlocked.Exchange(ref associationHandle._status, TransportStatus.Idle);
                        return false;
                    }

                    if (queue.IsEmpty)
                    {
                        Interlocked.Exchange(ref associationHandle._status, TransportStatus.Idle);
                        return true;
                    }
                }
            }
            finally
            {
                if (ChannelStatus.Closing == Volatile.Read(ref associationHandle._channelStatus))
                {
                    Interlocked.Exchange(ref associationHandle._channelStatus, ChannelStatus.Closed);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IByteBuffer Serialize(object obj)
        {
            var formatter = s_defaultResolver.GetFormatterWithVerify<object>();

            var idx = 0;
            var writer = new MessagePackWriter(true);
            formatter.Serialize(ref writer, ref idx, obj, s_defaultResolver);
            return Unpooled.WrappedBuffer(writer.ToArray(idx));
        }

        protected static class TransportStatus
        {
            public const int Idle = 0;
            public const int Busy = 1;
        }

        protected static class ChannelStatus
        {
            public const int Unknow = 0;
            public const int Open = 1;
            public const int Closing = 2;
            public const int Closed = 3;
        }
    }

    internal sealed class PoolingTcpBatchMessagesAssociationHandle : TcpBatchMessagesAssociationHandle
    {
        public PoolingTcpBatchMessagesAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress, transport, channel)
        {
        }

        public override bool Write(object payload)
        {
            if (IsWritable)
            {
                _queue.Enqueue(payload);
                if (TransportStatus.Idle == Interlocked.CompareExchange(ref _status, TransportStatus.Busy, TransportStatus.Idle))
                {
                    Task.Factory.StartNew(s_processMessagesFunc, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                return true;
            }
            return false;
        }


        static readonly Func<object, Task<bool>> s_processMessagesFunc = s => ProcessMessagesAsync(s);
        private static async Task<bool> ProcessMessagesAsync(object state)
        {
            var associationHandle = (PoolingTcpBatchMessagesAssociationHandle)state;

            var queue = associationHandle._queue;
            var batchSize = associationHandle._batchSize;
            var channel = associationHandle._channel;

            var batch = new List<object>(batchSize);
            while (true)
            {
                batch.Clear();

                while (queue.TryDequeue(out var msg))
                {
                    batch.Add(msg);
                    if (batch.Count >= batchSize) { break; }
                }

                if (channel.IsActive)
                {
                    await channel.WriteAndFlushAsync(Serialize(batch));

                    //Interlocked.CompareExchange(ref associationHandle._channelStatus, ChannelStatus.Open, ChannelStatus.Unknow);
                }
                else
                {
                    Interlocked.Exchange(ref associationHandle._channelStatus, ChannelStatus.Closed);
                    Interlocked.Exchange(ref associationHandle._status, TransportStatus.Idle);
                    return false;
                }

                if (queue.IsEmpty)
                {
                    Interlocked.Exchange(ref associationHandle._status, TransportStatus.Idle);
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IByteBuffer Serialize(object obj)
        {
            var formatter = s_defaultResolver.GetFormatterWithVerify<object>();

            var idx = 0;
            var writer = new MessagePackWriter(false);
            formatter.Serialize(ref writer, ref idx, obj, s_defaultResolver);
            writer.DiscardBuffer(out var arrayPool, out var buffer);
            return ArrayPooled.WrappedBuffer(arrayPool, buffer, 0, idx);
        }
    }
}