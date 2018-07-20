﻿//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using CuteAnt.Collections;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv.Native;
using MessagePack;

namespace Akka.Remote.Transport.DotNetty
{
    #region == class TcpHandlers ==

    internal abstract class TcpHandlers : CommonHandlers
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;
        private IHandleEventListener _listener;

        protected void NotifyListener(IHandleEvent msg) => _listener?.Notify(msg);

        protected TcpHandlers(DotNettyTransport transport, ILoggingAdapter log)
            : base(transport, log) { }

        protected override void RegisterListener(IChannel channel, IHandleEventListener listener, object msg, IPEndPoint remoteAddress)
            => this._listener = listener;

        protected override AssociationHandle CreateHandle(IChannel channel, Address localAddress, Address remoteAddress)
            => new TcpAssociationHandle(localAddress, remoteAddress, Transport, channel);

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            NotifyListener(new Disassociated(DisassociateInfo.Unknown));
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buf = ((IByteBuffer)message);
            if (buf.ReadableBytes > 0)
            {
                // no need to copy the byte buffer contents; ByteString does that automatically
                var bytes = CopyFrom(buf.Array, buf.ArrayOffset + buf.ReaderIndex, buf.ReadableBytes);

#if DEBUG
                var pdus = (List<object>)MessagePackSerializer.Deserialize<object>(bytes, Akka.Serialization.MsgPackSerializerHelper.DefaultResolver);
#else
                var pdus = (List<object>)MessagePackSerializer.Deserialize<object>(bytes, s_defaultResolver);
#endif
                pdus.ForEach(raw => NotifyListener(new InboundPayload(raw)));

                //NotifyListener(new InboundPayload(bytes));
            }

            // decrease the reference count to 0 (releases buffer)
            ReferenceCountUtil.SafeRelease(message);
        }


        private
#if !NET451
            unsafe
#endif
            static byte[] CopyFrom(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
#if NET451
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
#else

            fixed (byte* pSrc = &buffer[offset])
            fixed (byte* pDst = &bytes[0])
            {
                Buffer.MemoryCopy(pSrc, pDst, bytes.Length, count);
            }
#endif
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
                                var channel = context.Channel;
                                Log.Info($"{se.Message} Channel [{channel.LocalAddress}->{channel.RemoteAddress}](Id={channel.Id})");
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
                                var channel = context.Channel;
                                Log.Info($"{exc.Description}. Channel [{channel.LocalAddress}->{channel.RemoteAddress}](Id={channel.Id})");
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

    internal sealed class TcpServerHandler : TcpHandlers
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
            channel.Configuration.AutoRead = false;

            _associationEventListener.ContinueWith(r =>
            {
                var listener = r.Result;
                var remoteAddress = DotNettyTransport.MapSocketToAddress(
                    socketAddress: socketAddress,
                    schemeIdentifier: Transport.SchemeIdentifier,
                    systemName: Transport.System.Name);
                Init(channel, socketAddress, remoteAddress, msg, out var handle);
                listener.Notify(new InboundAssociation(handle));
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }

    #endregion

    #region == class TcpClientHandler ==

    internal sealed class TcpClientHandler : TcpHandlers
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

    internal sealed class TcpAssociationHandle : AssociationHandle
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;

        private readonly IChannel _channel;

        private readonly int _batchSize;
        private readonly object _gate = new object();
        private int _status = TransportStatus.Idle;
        private readonly Deque<object> _deque = new Deque<object>(2 * 1024);

        private int _channelStatus = ChannelStatus.Unknow;
        private bool IsWritable
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
                        if (_channel.Active) // _channel.Open && _channel.IsWritable)
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
            }
        }

        public TcpAssociationHandle(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
            : base(localAddress, remoteAddress)
        {
            _channel = channel;
            _batchSize = transport.TransferBatchSize;
        }

        public override bool Write(object payload)
        {
            if (IsWritable)
            {
                lock (_gate)
                {
                    _deque.AddToBack(payload);
                }
                if (TransportStatus.Idle == Interlocked.CompareExchange(ref _status, TransportStatus.Busy, TransportStatus.Idle))
                {
                    Task.Run(ProcessMessagesAsync);
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

        private async Task<bool> ProcessMessagesAsync()
        {
            var batch = new List<object>(_batchSize);
            //var tasks = new List<Task>(_batchSize);
            while (true)
            {
                batch.Clear();
                //tasks.Clear();

                lock (_gate)
                {
                    if (!_deque.TryRemoveFromFront(batch, _batchSize))
                    {
                        Interlocked.Exchange(ref _status, TransportStatus.Idle);
                        return true;
                    }
                }

#if DEBUG
                var payload = MessagePackSerializer.Serialize<object>(batch, Akka.Serialization.MsgPackSerializerHelper.DefaultResolver);
#else
                var payload = MessagePackSerializer.Serialize<object>(batch, s_defaultResolver);
#endif
                if (_channel.Active) // _channel.Open && _channel.IsWritable
                {
                    await _channel.WriteAndFlushAsync(Unpooled.WrappedBuffer(payload));
                    //batch.ForEach(payload => tasks.Add(_channel.WriteAsync(Unpooled.WrappedBuffer(payload))));
                    //_channel.Flush();
                    //await Task.WhenAll(tasks).ConfigureAwait(false);

                    Interlocked.CompareExchange(ref _channelStatus, ChannelStatus.Open, ChannelStatus.Unknow);
                }
                else
                {
                    Interlocked.Exchange(ref _channelStatus, ChannelStatus.Closed);
                    Interlocked.Exchange(ref _status, TransportStatus.Idle);
                    return false;
                }
            }
        }

        static class TransportStatus
        {
            public const int Idle = 0;
            public const int Busy = 1;
        }

        static class ChannelStatus
        {
            public const int Unknow = 0;
            public const int Open = 1;
            public const int Closed = 2;
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
                var handler = (TcpClientHandler)associate.Pipeline.Last();
                return await handler.StatusFuture.ConfigureAwait(false);
            }
            catch (AggregateException e) when (e.InnerException is ConnectException cause)
            {
                throw HandleConnectException(remoteAddress, cause, e);
            }
            catch (AggregateException e) when (e.InnerException is ConnectTimeoutException cause)
            {
                throw new InvalidAssociationException(cause.Message);
            }
            catch (AggregateException e) when (e.InnerException is ChannelException cause)
            {
                throw new InvalidAssociationException(cause.InnerException?.Message ?? cause.Message);
            }
            catch (ConnectException exc)
            {
                throw HandleConnectException(remoteAddress, exc, null);
            }
            catch (ConnectTimeoutException exc)
            {
                throw new InvalidAssociationException(exc.Message);
            }
            catch (ChannelException exc)
            {
                throw new InvalidAssociationException(exc.InnerException?.Message ?? exc.Message);
            }
        }

        private static Exception HandleConnectException(Address remoteAddress, ConnectException cause, AggregateException e)
        {
            var socketException = cause?.InnerException as SocketException;

            if (socketException?.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return new InvalidAssociationException(socketException.Message + " " + remoteAddress);
            }

            return new InvalidAssociationException("Failed to associate with " + remoteAddress, e ?? (Exception)cause);
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