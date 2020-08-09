//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Akka.Actor;
using Akka.Event;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv.Native;
using MessagePack;

namespace Akka.Remote.Transport.DotNetty
{
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
                NotifyListener(new InboundPayload(MessagePackSerializer.Deserialize<object>(unreadSpan, DefaultResolver)));
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
}