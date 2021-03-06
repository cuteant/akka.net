﻿//-----------------------------------------------------------------------
// <copyright file="AkkaLoggingHandler.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Util;
using CuteAnt.Pool;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using ILoggingAdapter = Akka.Event.ILoggingAdapter;

namespace Akka.Remote.Transport.DotNetty
{
    /// <summary>INTERNAL API
    ///
    /// Used for adding additional debug logging to the DotNetty transport.</summary>
    internal sealed class AkkaLoggingHandler : ChannelHandlerAdapter
    {
        private readonly ILoggingAdapter _log;

        public AkkaLoggingHandler(ILoggingAdapter log)
        {
            this._log = log;
        }

        public override void ChannelRegistered(IChannelHandlerContext ctx)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} registered", ctx.Channel);
            ctx.FireChannelRegistered();
        }

        public override void ChannelUnregistered(IChannelHandlerContext ctx)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} unregistered", ctx.Channel);
            ctx.FireChannelUnregistered();
        }

        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} active", ctx.Channel);
            ctx.FireChannelActive();
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} inactive", ctx.Channel);
            ctx.FireChannelInactive();
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
        {
            _log.Error(cause, "Channel {0} caught exception", ctx.Channel);
            ctx.FireExceptionCaught(cause);
        }

        public override void UserEventTriggered(IChannelHandlerContext ctx, object evt)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} triggered user event [{1}]", ctx.Channel, evt);
            ctx.FireUserEventTriggered(evt);
        }

        public override Task BindAsync(IChannelHandlerContext ctx, EndPoint localAddress)
        {
            if (_log.IsInfoEnabled) _log.Info("Channel {0} bind to address {1}", ctx.Channel, localAddress);
            return ctx.BindAsync(localAddress);
        }

        public override Task ConnectAsync(IChannelHandlerContext ctx, EndPoint remoteAddress, EndPoint localAddress)
        {
            if (_log.IsInfoEnabled) _log.Info("Channel {0} connect (remote: {1}, local: {2})", ctx.Channel, remoteAddress, localAddress);
            return ctx.ConnectAsync(remoteAddress, localAddress);
        }

        public override void Disconnect(IChannelHandlerContext ctx, IPromise promise)
        {
            if (_log.IsInfoEnabled) _log.Info("Channel {0} disconnect", ctx.Channel);
            ctx.DisconnectAsync(promise);
        }

        public override void Close(IChannelHandlerContext ctx, IPromise promise)
        {
            if (_log.IsInfoEnabled) _log.Info("Channel {0} close", ctx.Channel);
            ctx.CloseAsync(promise);
        }

        public override void Deregister(IChannelHandlerContext ctx, IPromise promise)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} deregister", ctx.Channel);
            ctx.DeregisterAsync(promise);
        }

        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            if (_log.IsDebugEnabled)
            {
                _log.Debug("Channel {0} received a message ({1}) of type [{2}]", ctx.Channel, message, message is null ? "NULL" : message.GetType().TypeQualifiedName());
            }
            ctx.FireChannelRead(message);
        }

        public override void Write(IChannelHandlerContext ctx, object message, IPromise promise)
        {
            if (_log.IsDebugEnabled)
            {
                _log.Debug("Channel {0} writing a message ({1}) of type [{2}]", ctx.Channel, message, message is null ? "NULL" : message.GetType().TypeQualifiedName());
            }
            ctx.WriteAsync(message, promise);
        }

        public override void Flush(IChannelHandlerContext ctx)
        {
            if (_log.IsDebugEnabled) _log.Debug("Channel {0} flushing", ctx.Channel);
            ctx.Flush();
        }

        internal static string Format(IChannelHandlerContext ctx, string eventName)
        {
            var chStr = ctx.Channel.ToString();
            var sb = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length);
            sb.Append(chStr).Append(' ').Append(eventName);

            return StringBuilderManager.ReturnAndFree(sb);
        }

        internal static string Format(IChannelHandlerContext ctx, string eventName, object arg)
        {
            switch (arg)
            {
                case IByteBuffer buffer:
                    return FormatByteBuffer(ctx, eventName, buffer);

                case IByteBufferHolder bufferHolder:
                    return FormatByteBufferHolder(ctx, eventName, bufferHolder);

                default:
                    return FormatSimple(ctx, eventName, arg);
            }
        }

        internal static string Format(IChannelHandlerContext ctx, string eventName, object firstArg, object secondArg)
        {
            if (secondArg is null)
            {
                return FormatSimple(ctx, eventName, firstArg);
            }
            string chStr = ctx.Channel.ToString();
            string arg1Str = firstArg.ToString();
            string arg2Str = secondArg.ToString();

            var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + arg1Str.Length + 2 + arg2Str.Length);
            buf.Append(chStr).Append(' ').Append(eventName).Append(": ")
               .Append(arg1Str).Append(", ").Append(arg2Str);
            return StringBuilderManager.ReturnAndFree(buf);
        }

        private static string FormatByteBuffer(IChannelHandlerContext ctx, string eventName, IByteBuffer msg)
        {
            string chStr = ctx.Channel.ToString();
            int length = msg.ReadableBytes;
            if (length == 0)
            {
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 4);
                buf.Append(chStr).Append(' ').Append(eventName).Append(": 0B");
                return StringBuilderManager.ReturnAndFree(buf);
            }
            else
            {
                int rows = length / 16 + (length % 15 == 0 ? 0 : 1) + 4;
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + 10 + 1 + 2 + rows * 80);

                buf.Append(chStr).Append(' ').Append(eventName).Append(": ").Append(length).Append('B').Append('\n');
                ByteBufferUtil.AppendPrettyHexDump(buf, msg);

                return StringBuilderManager.ReturnAndFree(buf);
            }
        }

        private static string FormatByteBufferHolder(IChannelHandlerContext ctx, string eventName, IByteBufferHolder msg)
        {
            string chStr = ctx.Channel.ToString();
            string msgStr = msg.ToString();
            IByteBuffer content = msg.Content;
            int length = content.ReadableBytes;
            if (length == 0)
            {
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length + 4);
                buf.Append(chStr).Append(' ').Append(eventName).Append(", ").Append(msgStr).Append(", 0B");
                return StringBuilderManager.ReturnAndFree(buf);
            }
            else
            {
                int rows = length / 16 + (length % 15 == 0 ? 0 : 1) + 4;
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length + 2 + 10 + 1 + 2 + rows * 80);

                buf.Append(chStr).Append(' ').Append(eventName).Append(": ")
                   .Append(msgStr).Append(", ").Append(length).Append('B').Append('\n');
                ByteBufferUtil.AppendPrettyHexDump(buf, content);

                return StringBuilderManager.ReturnAndFree(buf);
            }
        }

        private static string FormatSimple(IChannelHandlerContext ctx, string eventName, object msg)
        {
            string chStr = ctx.Channel.ToString();
            string msgStr = msg.ToString();
            var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length);
            return StringBuilderManager.ReturnAndFree(buf.Append(chStr).Append(' ').Append(eventName).Append(": ").Append(msgStr));
        }
    }
}