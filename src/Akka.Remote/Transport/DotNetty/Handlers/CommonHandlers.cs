//-----------------------------------------------------------------------
// <copyright file="DotNettyTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Util;
using DotNetty.Transport.Channels;

namespace Akka.Remote.Transport.DotNetty
{
    internal abstract class CommonHandlers : ChannelHandlerAdapter
    {
        protected readonly DotNettyTransport Transport;
        protected readonly ILoggingAdapter Log;

        protected CommonHandlers(DotNettyTransport transport, ILoggingAdapter log)
        {
            Transport = transport;
            Log = log;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            var channel = context.Channel;
            if (!Transport.ConnectionGroup.TryAdd(channel))
            {
                if (Log.IsWarningEnabled) Log.UnableToAddChannelToConnectionGroup(channel);
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);

            var channel = context.Channel;
            if (!Transport.ConnectionGroup.TryRemove(channel))
            {
                if (Log.IsWarningEnabled) Log.UnableToRemoveChannelFromConnectionGroup(channel);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);

            var channel = context.Channel;
            Log.Error(exception, "Error caught channel [{0}->{1}](Id={2})", channel.LocalAddress, channel.RemoteAddress, channel.Id);
        }

        protected abstract AssociationHandle CreateHandle(IChannel channel, Address localAddress, Address remoteAddress);

        protected abstract void RegisterListener(IChannel channel, IHandleEventListener listener, object msg, IPEndPoint remoteAddress);

        protected void Init(IChannel channel, IPEndPoint remoteSocketAddress, Address remoteAddress, object msg, out AssociationHandle op)
        {
            var localAddress = DotNettyTransport.MapSocketToAddress((IPEndPoint)channel.LocalAddress, Transport.SchemeIdentifier, Transport.System.Name, Transport.Settings.Hostname);

            if (localAddress != null)
            {
                var handle = CreateHandle(channel, localAddress, remoteAddress);
                handle.ReadHandlerSource.Task.Then(AfterSetupReadHandlerAction, this, channel, remoteSocketAddress, msg,
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted);
                op = handle;
            }
            else
            {
                op = null;
                channel.CloseAsync();
            }
        }

        private static readonly Action<IHandleEventListener, CommonHandlers, IChannel, IPEndPoint, object> AfterSetupReadHandlerAction = AfterSetupReadHandler;
        private static void AfterSetupReadHandler(IHandleEventListener listener, CommonHandlers owner, IChannel channel, IPEndPoint remoteSocketAddress, object msg)
        {
            owner.RegisterListener(channel, listener, msg, remoteSocketAddress);
            channel.Configuration.IsAutoRead = true; // turn reads back on
        }
    }
}