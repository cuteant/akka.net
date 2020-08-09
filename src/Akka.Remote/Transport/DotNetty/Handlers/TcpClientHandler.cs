//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using MessagePack;

namespace Akka.Remote.Transport.DotNetty
{
    internal interface ITcpClientHandler
    {
        Task<AssociationHandle> StatusFuture { get; }
    }

    internal sealed class TcpClientHandler : TcpClientHandler<TcpAssociationHandleFactory>
    {
        public TcpClientHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal sealed class PoolingTcpClientHandler : TcpClientHandler<PoolingTcpAssociationHandleFactory>
    {
        public PoolingTcpClientHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal sealed class TcpClientBatchWriterHandler : TcpClientHandler<TcpBatchWriterAssociationHandleFactory>
    {
        public TcpClientBatchWriterHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal sealed class PoolingTcpClientBatchWriterHandler : TcpClientHandler<PoolingTcpBatchWriterAssociationHandleFactory>
    {
        public PoolingTcpClientBatchWriterHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal sealed class TcpClientBatchMessagesHandler : TcpClientBatchMessagesHandlers<TcpBatchMessagesAssociationHandleFactory>
    {
        public TcpClientBatchMessagesHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal sealed class PoolingTcpClientBatchMessagesHandler : TcpClientBatchMessagesHandlers<PoolingTcpBatchMessagesAssociationHandleFactory>
    {
        public PoolingTcpClientBatchMessagesHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal abstract class TcpClientBatchMessagesHandlers<TAssociationHandleFactory> : TcpClientHandler<TAssociationHandleFactory>
        where TAssociationHandleFactory : ITcpAssociationHandleFactory, new()
    {
        public TcpClientBatchMessagesHandlers(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }

        public sealed override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = (IByteBuffer)message;
            var unreadSpan = buf.UnreadSpan;
            if (!unreadSpan.IsEmpty)
            {
                var pdus = (List<object>)MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver);
                for (int idx = 0; idx < pdus.Count; idx++)
                {
                    NotifyListener(new InboundPayload(pdus[idx]));
                }
            }

            // decrease the reference count to 0 (releases buffer)
            buf.Release(); //ReferenceCountUtil.SafeRelease(message);
        }
    }

    internal abstract class TcpClientHandler<TAssociationHandleFactory> : TcpHandlers<TAssociationHandleFactory>, ITcpClientHandler
        where TAssociationHandleFactory : ITcpAssociationHandleFactory, new()
    {
        private readonly TaskCompletionSource<AssociationHandle> _statusPromise = new TaskCompletionSource<AssociationHandle>();
        private readonly Address _remoteAddress;

        public Task<AssociationHandle> StatusFuture => _statusPromise.Task;

        public TcpClientHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log) => _remoteAddress = remoteAddress;

        public override void ChannelActive(IChannelHandlerContext context)
        {
            InitOutbound(context.Channel, (IPEndPoint)context.Channel.RemoteAddress, null);
            base.ChannelActive(context);
        }

        private void InitOutbound(IChannel channel, IPEndPoint socketAddress, object msg)
        {
            Init(channel, socketAddress, _remoteAddress, msg, out var handle);
            _statusPromise.TrySetResult(handle);
        }
    }
}