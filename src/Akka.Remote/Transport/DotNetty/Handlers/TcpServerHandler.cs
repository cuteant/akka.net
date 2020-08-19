//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Akka.Event;
using Akka.Util;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using MessagePack;

namespace Akka.Remote.Transport.DotNetty
{
    internal sealed class TcpServerHandler : TcpServerHandler<TcpAssociationHandleFactory>
    {
        public TcpServerHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal sealed class PoolingTcpServerHandler : TcpServerHandler<PoolingTcpAssociationHandleFactory>
    {
        public PoolingTcpServerHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal sealed class TcpServerBatchWriterHandler : TcpServerHandler<TcpBatchWriterAssociationHandleFactory>
    {
        public TcpServerBatchWriterHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal sealed class PoolingTcpServerBatchWriterHandler : TcpServerHandler<PoolingTcpBatchWriterAssociationHandleFactory>
    {
        public PoolingTcpServerBatchWriterHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal sealed class TcpServerBatchMessagesHandler : TcpServerBatchMessagesHandlers<TcpBatchMessagesAssociationHandleFactory>
    {
        public TcpServerBatchMessagesHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal sealed class PoolingTcpServerBatchMessagesHandler : TcpServerBatchMessagesHandlers<PoolingTcpBatchMessagesAssociationHandleFactory>
    {
        public PoolingTcpServerBatchMessagesHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal abstract class TcpServerBatchMessagesHandlers<TAssociationHandleFactory> : TcpServerHandler<TAssociationHandleFactory>
        where TAssociationHandleFactory : ITcpAssociationHandleFactory, new()
    {
        public TcpServerBatchMessagesHandlers(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }

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

    internal abstract class TcpServerHandler<TAssociationHandleFactory> : TcpHandlers<TAssociationHandleFactory>
        where TAssociationHandleFactory : ITcpAssociationHandleFactory, new()
    {
        private readonly Task<IAssociationEventListener> _associationEventListener;

        public TcpServerHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log) => this._associationEventListener = associationEventListener;

        public override void ChannelActive(IChannelHandlerContext context)
        {
            InitInbound(context.Channel, (IPEndPoint)context.Channel.RemoteAddress, null);
            base.ChannelActive(context);
        }

        private void InitInbound(IChannel channel, IPEndPoint socketAddress, object msg)
        {
            // disable automatic reads
            channel.Configuration.IsAutoRead = false;

            _associationEventListener.Then(AfterSetupAssociationEventListenerAction,
                this, channel, socketAddress, msg, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static readonly Action<IAssociationEventListener, TcpServerHandler<TAssociationHandleFactory>, IChannel, IPEndPoint, object> AfterSetupAssociationEventListenerAction =
            (l, o, c, s, m) => AfterSetupAssociationEventListener(l, o, c, s, m);
        private static void AfterSetupAssociationEventListener(IAssociationEventListener listener,
            TcpServerHandler<TAssociationHandleFactory> owner, IChannel channel, IPEndPoint socketAddress, object msg)
        {
            var remoteAddress = DotNettyTransport.MapSocketToAddress(
                socketAddress: socketAddress,
                schemeIdentifier: owner.Transport.SchemeIdentifier,
                systemName: owner.Transport.System.Name);
            owner.Init(channel, socketAddress, remoteAddress, msg, out var handle);
            listener.Notify(new InboundAssociation(handle));
        }
    }
}