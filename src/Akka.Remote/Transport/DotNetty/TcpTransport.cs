//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Util;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv.Native;
using MessagePack;

namespace Akka.Remote.Transport.DotNetty
{
    #region == class TcpHandlers ==

    internal abstract class TcpHandlers<TAssociationHandleFactory> : CommonHandlers
        where TAssociationHandleFactory : ITcpAssociationHandleFactory, new()
    {
        protected static readonly IFormatterResolver DefaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;

        private IHandleEventListener _listener;
        private TAssociationHandleFactory _associationHandleFactory;

        protected void NotifyListener(IHandleEvent msg) => _listener?.Notify(msg);

        protected TcpHandlers(DotNettyTransport transport, ILoggingAdapter log)
            : base(transport, log)
        {
            _associationHandleFactory = new TAssociationHandleFactory();
        }

        protected override void RegisterListener(IChannel channel, IHandleEventListener listener, object msg, IPEndPoint remoteAddress)
            => this._listener = listener;

        protected override AssociationHandle CreateHandle(IChannel channel, Address localAddress, Address remoteAddress)
            => _associationHandleFactory.Create(localAddress, remoteAddress, Transport, channel);

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            NotifyListener(new Disassociated(DisassociateInfo.Unknown));
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = (IByteBuffer)message;
            var unreadSpan = buf.UnreadSpan;
            if (!unreadSpan.IsEmpty)
            {
                var pdus = (List<object>)MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver);
                foreach (var raw in pdus)
                {
                    NotifyListener(new InboundPayload(raw));
                }
            }

            // decrease the reference count to 0 (releases buffer)
            buf.Release(); //ReferenceCountUtil.SafeRelease(message);
        }

        protected static byte[] CopyFrom(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
            MessagePackBinary.CopyMemory(buffer, offset, bytes, 0, count);
            return bytes;
        }

        /// <summary>TBD</summary>
        /// <param name="context">TBD</param>
        /// <param name="exception">TBD</param>
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            switch (exception)
            {
                case SocketException se:
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.Interrupted:
                        case SocketError.TimedOut:
                        case SocketError.OperationAborted:
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                            if (Log.IsInfoEnabled)
                            {
                                Log.DotNettyExceptionCaught(se, context);
                            }
                            NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
                            break;

                        default:
                            base.ExceptionCaught(context, exception);
                            NotifyListener(new Disassociated(DisassociateInfo.Unknown));
                            break;
                    }
                    break;

                // Libuv error handling: http://docs.libuv.org/en/v1.x/errors.html
                case ChannelException ce when (ce.InnerException is OperationException exc):
                    switch (exc.ErrorCode)
                    {
                        case ErrorCode.EINTR:        // interrupted system call
                        case ErrorCode.ENETDOWN:     // network is down
                        case ErrorCode.ENETUNREACH:  // network is unreachable
                        case ErrorCode.ENOTSOCK:     // socket operation on non-socket
                        case ErrorCode.ENOTSUP:      // operation not supported on socket
                        case ErrorCode.EPERM:        // operation not permitted
                        case ErrorCode.ETIMEDOUT:    // connection timed out
                        case ErrorCode.ECANCELED:    // operation canceled
                        case ErrorCode.ECONNABORTED: // software caused connection abort
                        case ErrorCode.ECONNRESET:   // connection reset by peer
                            if (Log.IsInfoEnabled)
                            {
                                Log.DotNettyExceptionCaught(exc, context);
                            }
                            NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
                            break;

                        default:
                            base.ExceptionCaught(context, exception);
                            NotifyListener(new Disassociated(DisassociateInfo.Unknown));
                            break;
                    }
                    break;

                default:
                    base.ExceptionCaught(context, exception);
                    NotifyListener(new Disassociated(DisassociateInfo.Unknown));
                    break;
            }
            #region ## 屏蔽 ##
            //switch (exception)
            //{
            //    case SocketException se when (se.SocketErrorCode == SocketError.Interrupted):
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("A blocking socket call was canceled. Channel [{0}->{1}](Id={2})",
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;

            //    case SocketException se when (se.SocketErrorCode == SocketError.TimedOut):
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("The connection attempt timed out, or the connected host has failed to respond. Channel [{0}->{1}](Id={2})",
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;

            //    case SocketException se when (se.SocketErrorCode == SocketError.OperationAborted):
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("Socket read operation aborted. Connection is about to be closed. Channel [{0}->{1}](Id={2})",
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;

            //    case SocketException se when (se.SocketErrorCode == SocketError.ConnectionAborted):
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("The connection was aborted by the .NET Framework or the underlying socket provider. Channel [{0}->{1}](Id={2})",
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;

            //    case SocketException se when (se.SocketErrorCode == SocketError.ConnectionReset):
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("Connection was reset by the remote peer. Channel [{0}->{1}](Id={2})",
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;

            //    // Libuv error handling: http://docs.libuv.org/en/v1.x/errors.html
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.EINTR): // interrupted system call
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ENETDOWN): // network is down
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ENETUNREACH): // network is unreachable
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ENOTSOCK): // socket operation on non-socket
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ENOTSUP): // operation not supported on socket
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.EPERM): // operation not permitted
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ETIMEDOUT): // connection timed out
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ECANCELED): // operation canceled
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ECONNABORTED): // software caused connection abort
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;
            //    case ChannelException ce when ((ce.InnerException as OperationException)?.ErrorCode == ErrorCode.ECONNRESET): // connection reset by peer
            //        if (Log.IsInfoEnabled)
            //        {
            //            Log.Info("{0}. Channel [{1}->{2}](Id={3})", (ce.InnerException as OperationException).Description,
            //                context.Channel.LocalAddress, context.Channel.RemoteAddress, context.Channel.Id);
            //        }

            //        NotifyListener(new Disassociated(DisassociateInfo.Shutdown));
            //        break;

            //    default:
            //        base.ExceptionCaught(context, exception);
            //        NotifyListener(new Disassociated(DisassociateInfo.Unknown));
            //        break;
            //}
            #endregion

            context.CloseAsync(); // close the channel
        }
    }

    #endregion

    #region == class TcpServerHandler ==

    internal sealed class TcpServerHandler : TcpServerHandler<TcpAssociationHandleFactory>
    {
        public TcpServerHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = (IByteBuffer)message;
            var unreadSpan = buf.UnreadSpan;
            if (!unreadSpan.IsEmpty)
            {
                NotifyListener(new InboundPayload(MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver)));
            }

            // decrease the reference count to 0 (releases buffer)
            buf.Release(); //ReferenceCountUtil.SafeRelease(message);
        }
    }

    internal sealed class TcpServerHandlerWithPooling : TcpServerHandler<TcpAssociationHandleWithPoolingFactory>
    {
        public TcpServerHandlerWithPooling(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = (IByteBuffer)message;
            var unreadSpan = buf.UnreadSpan;
            if (!unreadSpan.IsEmpty)
            {
                NotifyListener(new InboundPayload(MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver)));
            }

            // decrease the reference count to 0 (releases buffer)
            buf.Release(); //ReferenceCountUtil.SafeRelease(message);
        }
    }

    internal sealed class TcpBatchServerHandler : TcpServerHandler<TcpBatchAssociationHandleFactory>
    {
        public TcpBatchServerHandler(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
    }

    internal sealed class TcpBatchServerHandlerWithPooling : TcpServerHandler<TcpBatchAssociationHandleWithPoolingFactory>
    {
        public TcpBatchServerHandlerWithPooling(DotNettyTransport transport, ILoggingAdapter log, Task<IAssociationEventListener> associationEventListener)
            : base(transport, log, associationEventListener) { }
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

        private static readonly Action<IAssociationEventListener, TcpServerHandler<TAssociationHandleFactory>, IChannel, IPEndPoint, object> AfterSetupAssociationEventListenerAction = AfterSetupAssociationEventListener;
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

    #endregion

    #region == class TcpClientHandler ==

    internal interface ITcpClientHandler
    {
        Task<AssociationHandle> StatusFuture { get; }
    }

    internal class TcpClientHandler : TcpClientHandler<TcpAssociationHandleFactory>
    {
        public TcpClientHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = (IByteBuffer)message;
            var unreadSpan = buf.UnreadSpan;
            if (!unreadSpan.IsEmpty)
            {
                NotifyListener(new InboundPayload(MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver)));
            }

            // decrease the reference count to 0 (releases buffer)
            buf.Release(); //ReferenceCountUtil.SafeRelease(message);
        }
    }

    internal class TcpClientHandlerWithPooling : TcpClientHandler<TcpAssociationHandleWithPoolingFactory>
    {
        public TcpClientHandlerWithPooling(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = (IByteBuffer)message;
            var unreadSpan = buf.UnreadSpan;
            if (!unreadSpan.IsEmpty)
            {
                NotifyListener(new InboundPayload(MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver)));
            }

            // decrease the reference count to 0 (releases buffer)
            buf.Release(); //ReferenceCountUtil.SafeRelease(message);
        }
    }

    internal sealed class TcpBatchClientHandler : TcpClientHandler<TcpBatchAssociationHandleFactory>
    {
        public TcpBatchClientHandler(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
    }

    internal sealed class TcpBatchClientHandlerWithPooling : TcpClientHandler<TcpBatchAssociationHandleWithPoolingFactory>
    {
        public TcpBatchClientHandlerWithPooling(DotNettyTransport transport, ILoggingAdapter log, Address remoteAddress)
            : base(transport, log, remoteAddress) { }
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

    #endregion

    #region == class TcpAssociationHandle ==

    internal class TcpBatchAssociationHandle : AssociationHandle
    {
        protected static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;

        protected readonly IChannel _channel;

        protected readonly int _batchSize;
        protected int _status = TransportStatus.Idle;
        protected readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();

        protected int _channelStatus = ChannelStatus.Unknow;
        protected bool IsWritable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (Volatile.Read(ref _channelStatus))
                {
                    case ChannelStatus.Open:
                        return true;
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
            if (_channel.IsActive) // _channel.Open && _channel.IsWritable)
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

        public TcpBatchAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress)
        {
            _channel = channel;
            _batchSize = transport.TransferBatchSize;
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

        public override void Disassociate()
        {
            Interlocked.Exchange(ref _channelStatus, ChannelStatus.Closed);
            _channel.CloseAsync();
        }

        static readonly Func<object, Task<bool>> s_processMessagesFunc = s => ProcessMessagesAsync(s);
        private static async Task<bool> ProcessMessagesAsync(object state)
        {
            var associationHandle = (TcpBatchAssociationHandle)state;

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

                if (channel.IsActive) // _channel.Open && _channel.IsWritable
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
            public const int Closed = 2;
        }
    }

    internal sealed class TcpBatchAssociationHandleWithPooling : TcpBatchAssociationHandle
    {
        public TcpBatchAssociationHandleWithPooling(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
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
            var associationHandle = (TcpBatchAssociationHandleWithPooling)state;

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

                if (channel.IsActive) // _channel.Open && _channel.IsWritable
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
            if (_channel.IsActive) // _channel.Open && _channel.IsWritable
            {
                _channel.WriteAndFlushAsync(Serialize(msg));
                return true;
            }
            return false;
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

        public override void Disassociate()
        {
            _channel.CloseAsync();
        }
    }

    internal sealed class TcpAssociationHandleWithPooling : TcpAssociationHandle
    {
        public TcpAssociationHandleWithPooling(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress, transport, channel)
        {
        }

        public override bool Write(object msg)
        {
            if (_channel.IsActive) // _channel.Open && _channel.IsWritable
            {
                _channel.WriteAndFlushAsync(Serialize(msg));
                return true;
            }
            return false;
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

    #endregion

    #region -- TcpAssociationHandleFactory --

    internal interface ITcpAssociationHandleFactory
    {
        AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel);
    }

    internal sealed class TcpAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new TcpAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class TcpAssociationHandleWithPoolingFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new TcpAssociationHandleWithPooling(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class TcpBatchAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new TcpBatchAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class TcpBatchAssociationHandleWithPoolingFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new TcpBatchAssociationHandleWithPooling(localAddress, remoteAddress, transport, channel);
        }
    }

    #endregion

    #region == class TcpTransport ==

    internal sealed class TcpTransport : DotNettyTransport
    {
        public TcpTransport(ActorSystem system, Config config)
            : base(system, config) { }

        protected override async Task<AssociationHandle> AssociateInternal(Address remoteAddress)
        {
            try
            {
                var clientBootstrap = ClientFactory(remoteAddress);
                var socketAddress = AddressToSocketAddress(remoteAddress);
                socketAddress = await MapEndpointAsync(socketAddress).ConfigureAwait(false);
                var associate = await clientBootstrap.ConnectAsync(socketAddress).ConfigureAwait(false);
                var handler = (ITcpClientHandler)associate.Pipeline.Last();
                return await handler.StatusFuture.ConfigureAwait(false);
            }
            catch (AggregateException e) when (e.InnerException is ConnectException cause)
            {
                throw HandleConnectException(remoteAddress, cause, e);
            }
            catch (AggregateException e) when (e.InnerException is ConnectTimeoutException cause)
            {
                throw HandleConnectTimeoutException(cause);
            }
            catch (AggregateException e) when (e.InnerException is ChannelException cause)
            {
                throw HandleChannelException(cause);
            }
            catch (ConnectException exc)
            {
                throw HandleConnectException(remoteAddress, exc, null);
            }
            catch (ConnectTimeoutException exc)
            {
                throw HandleConnectTimeoutException(exc);
            }
            catch (ChannelException exc)
            {
                throw HandleChannelException(exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidAssociationException HandleConnectException(Address remoteAddress, ConnectException cause, AggregateException e)
        {
            var socketException = cause?.InnerException as SocketException;

            if (socketException?.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return new InvalidAssociationException(socketException.Message + " " + remoteAddress);
            }

            return new InvalidAssociationException("Failed to associate with " + remoteAddress, e ?? (Exception)cause);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidAssociationException HandleChannelException(ChannelException exc)
        {
            return new InvalidAssociationException(exc.InnerException?.Message ?? exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidAssociationException HandleConnectTimeoutException(ConnectTimeoutException exc)
        {
            return new InvalidAssociationException(exc.Message);
        }

        private async Task<IPEndPoint> MapEndpointAsync(EndPoint socketAddress)
        {
            IPEndPoint ipEndPoint;

            if (socketAddress is DnsEndPoint dns)
            {
                ipEndPoint = await DnsToIPEndpoint(dns).ConfigureAwait(false);
            }
            else
            {
                ipEndPoint = (IPEndPoint)socketAddress;
            }

            if (ipEndPoint.Address.Equals(IPAddress.Any) || ipEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                // client hack
                return ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Loopback, ipEndPoint.Port)
                    : new IPEndPoint(IPAddress.Loopback, ipEndPoint.Port);
            }
            return ipEndPoint;
        }
    }

    #endregion
}