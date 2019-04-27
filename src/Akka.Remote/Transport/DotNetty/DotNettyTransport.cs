﻿//-----------------------------------------------------------------------
// <copyright file="DotNettyTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Util;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;

namespace Akka.Remote.Transport.DotNetty
{
    #region == class CommonHandlers ==

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
            channel.Configuration.AutoRead = true; // turn reads back on
        }
    }

    #endregion

    #region == class DotNettyTransportException ==

    internal sealed class DotNettyTransportException : RemoteTransportException
    {
        /// <summary>Initializes a new instance of the <see cref="DotNettyTransportException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public DotNettyTransportException(string message, Exception cause = null)
            : base(message, cause) { }

#if SERIALIZATION
        /// <summary>Initializes a new instance of the <see cref="DotNettyTransportException"/> class.</summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the
        /// exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source
        /// or destination.</param>
        private DotNettyTransportException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif
    }

    #endregion

    #region == class DotNettyTransport ==

    [Akka.Annotations.InternalApi]
    public abstract class DotNettyTransport : Transport // public for Akka.Tests.FsCheck
    {
        internal readonly ConcurrentSet<IChannel> ConnectionGroup;

        protected readonly TaskCompletionSource<IAssociationEventListener> AssociationListenerPromise;
        protected readonly ILoggingAdapter Log;
        protected volatile Address LocalAddress;
        protected internal volatile IChannel ServerChannel;

        private readonly IEventLoopGroup _serverBossGroup;
        private readonly IEventLoopGroup _serverWorkerGroup;
        private readonly IEventLoopGroup _clientWorkerGroup;

        protected DotNettyTransport(ActorSystem system, Config config)
        {
            System = system;
            Config = config;

            //if (system.Settings.Config.HasPath("akka.remote.helios.tcp"))
            //{
            //    var heliosFallbackConfig = system.Settings.Config.GetConfig("akka.remote.helios.tcp");
            //    config = heliosFallbackConfig.WithFallback(config);
            //}

            ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;

            Settings = DotNettyTransportSettings.Create(config);
            Log = Logging.GetLogger(System, GetType());
            if (Settings.EnableLibuv)
            {
                var dispatcher = new DispatcherEventLoopGroup();
                _serverBossGroup = dispatcher;
                _serverWorkerGroup = new WorkerEventLoopGroup(dispatcher, Settings.ServerSocketWorkerPoolSize);
                _clientWorkerGroup = new EventLoopGroup(Settings.ClientSocketWorkerPoolSize);
            }
            else
            {
                _serverBossGroup = new MultithreadEventLoopGroup(Settings.ServerSocketWorkerPoolSize);
                _serverWorkerGroup = new MultithreadEventLoopGroup(Settings.ServerSocketWorkerPoolSize);
                _clientWorkerGroup = new MultithreadEventLoopGroup(Settings.ClientSocketWorkerPoolSize);
            }
            ConnectionGroup = new ConcurrentSet<IChannel>();
            AssociationListenerPromise = new TaskCompletionSource<IAssociationEventListener>();

            SchemeIdentifier = (Settings.EnableSsl ? "ssl." : string.Empty) + Settings.TransportMode.ToString().ToLowerInvariant();
        }

        public DotNettyTransportSettings Settings { get; }
        public sealed override string SchemeIdentifier { get; protected set; }
        public override long MaximumPayloadBytes => Settings.MaxFrameSize;
        public override int TransferBatchSize => Settings.TransferBatchSize;
        private TransportMode InternalTransport => Settings.TransportMode;

        public sealed override bool IsResponsibleFor(Address remote) => true;

        protected async Task<IChannel> NewServer(EndPoint listenAddress)
        {
            if (InternalTransport != TransportMode.Tcp)
            {
                throw new NotImplementedException("Haven't implemented UDP transport at this time");
            }

            if (listenAddress is DnsEndPoint dns)
            {
                listenAddress = await DnsToIPEndpoint(dns).ConfigureAwait(false);
            }

            return await ServerFactory().BindAsync(listenAddress).ConfigureAwait(false);
        }

        public override async Task<Tuple<Address, TaskCompletionSource<IAssociationEventListener>>> Listen()
        {
            EndPoint listenAddress;
            if (IPAddress.TryParse(Settings.Hostname, out IPAddress ip))
            {
                listenAddress = new IPEndPoint(ip, Settings.Port);
            }
            else
            {
                listenAddress = new DnsEndPoint(Settings.Hostname, Settings.Port);
            }

            try
            {
                var newServerChannel = await NewServer(listenAddress).ConfigureAwait(false);

                // Block reads until a handler actor is registered no incoming connections will be
                // accepted until this value is reset it's possible that the first incoming
                // association might come in though
                newServerChannel.Configuration.AutoRead = false;
                ConnectionGroup.TryAdd(newServerChannel);
                Interlocked.Exchange(ref ServerChannel, newServerChannel);

                var addr = MapSocketToAddress(
                    socketAddress: (IPEndPoint)newServerChannel.LocalAddress,
                    schemeIdentifier: SchemeIdentifier,
                    systemName: System.Name,
                    hostName: Settings.PublicHostname,
                    publicPort: Settings.PublicPort);

                if (null == addr) { ThrowHelper.ThrowConfigurationException(newServerChannel); }
                Interlocked.Exchange(ref LocalAddress, addr);

                // resume accepting incoming connections
#pragma warning disable 4014 // we WANT this task to run without waiting
                AssociationListenerPromise.Task.LinkOutcome(InvokeResumeAcceptingIncomingConnsAction, newServerChannel,
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
#pragma warning restore 4014

                return Tuple.Create(addr, AssociationListenerPromise);
            }
            catch (Exception ex)
            {
                Log.FailedToBindToEndPoint(ex, listenAddress);
                try
                {
                    await Shutdown().ConfigureAwait(false);
                }
                catch
                {
                    // ignore errors occurring during shutdown
                }
                throw;
            }
        }

        private static readonly Action<Task<IAssociationEventListener>, IChannel> InvokeResumeAcceptingIncomingConnsAction = InvokeResumeAcceptingIncomingConns;
        private static void InvokeResumeAcceptingIncomingConns(Task<IAssociationEventListener> result, IChannel newServerChannel)
            => newServerChannel.Configuration.AutoRead = true;

        public override async Task<AssociationHandle> Associate(Address remoteAddress)
        {
            if (!ServerChannel.Open) { ThrowHelper.ThrowChannelException(); }

            return await AssociateInternal(remoteAddress).ConfigureAwait(false);
        }

        protected abstract Task<AssociationHandle> AssociateInternal(Address remoteAddress);

        public override async Task<bool> Shutdown()
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var channel in ConnectionGroup)
                {
                    tasks.Add(channel.CloseAsync());
                }
                var all = Task.WhenAll(tasks);
                await all.ConfigureAwait(false);

                var server = ServerChannel?.CloseAsync() ?? TaskUtil.Completed;
                await server.ConfigureAwait(false);

                return all.IsCompleted && server.IsCompleted;
            }
            finally
            {
                // free all of the connection objects we were holding onto
                ConnectionGroup.Clear();
#pragma warning disable 4014 // shutting down the worker groups can take up to 10 seconds each. Let that happen asnychronously.
                _clientWorkerGroup.ShutdownGracefullyAsync();
                _serverBossGroup.ShutdownGracefullyAsync();
                _serverWorkerGroup.ShutdownGracefullyAsync();
#pragma warning restore 4014
            }
        }

        protected Bootstrap ClientFactory(Address remoteAddress)
        {
            if (InternalTransport != TransportMode.Tcp)
                throw new NotSupportedException("Currently DotNetty client supports only TCP tranport mode.");

            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            var client = new Bootstrap()
                .Group(_clientWorkerGroup)
                .Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr)
                //.Option(ChannelOption.SoReuseport, Settings.TcpReusePort)
                .Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                .Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                //.Option(ChannelOption.SoLinger, Settings.TcpLinger)
                .Option(ChannelOption.ConnectTimeout, Settings.ConnectTimeout)
                .Option(ChannelOption.AutoRead, false)
                .Option(ChannelOption.Allocator, Settings.EnableBufferPooling ? (IByteBufferAllocator)PooledByteBufferAllocator.Default : UnpooledByteBufferAllocator.Default);
            if (Settings.EnableLibuv)
            {
                client.Channel<TcpChannel>();
            }
            else
            {
                client.ChannelFactory(() => Settings.EnforceIpFamily
                    ? new TcpSocketChannel(addressFamily)
                    : new TcpSocketChannel());
            }
            client.Handler(new ActionChannelInitializer<ISocketChannel>(channel => SetClientPipeline(channel, remoteAddress)));

            if (Settings.ReceiveBufferSize.HasValue) { client.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value); }
            if (Settings.SendBufferSize.HasValue) { client.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value); }
            if (Settings.WriteBufferHighWaterMark.HasValue) { client.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value); }
            if (Settings.WriteBufferLowWaterMark.HasValue) { client.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value); }

            return client;
        }

        protected async Task<IPEndPoint> DnsToIPEndpoint(DnsEndPoint dns)
        {
            IPEndPoint endpoint;
            //if (!Settings.EnforceIpFamily)
            //{
            //    endpoint = await ResolveNameAsync(dns).ConfigureAwait(false);
            //}
            //else
            //{
            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            endpoint = await ResolveNameAsync(dns, addressFamily).ConfigureAwait(false);
            //}
            return endpoint;
        }

        #region private methods

        private void SetInitialChannelPipeline(IChannel channel)
        {
            var pipeline = channel.Pipeline;

            if (Settings.LogTransport)
            {
                pipeline.AddLast("Logger", new AkkaLoggingHandler(Log));
            }

            if (InternalTransport == TransportMode.Tcp)
            {
                if (Settings.ByteOrder == ByteOrder.BigEndian)
                {
                    pipeline.AddLast("FrameDecoder", new LengthFieldBasedFrameDecoder2((int)MaximumPayloadBytes, 0, 4, 0, 4, true));
                    pipeline.AddLast("FrameEncoder", new LengthFieldPrepender2(4, 0, false));
                }
                else
                {
                    pipeline.AddLast("FrameDecoder", new LengthFieldBasedFrameDecoder(Settings.ByteOrder, (int)MaximumPayloadBytes, 0, 4, 0, 4, true));
                    //if (Settings.BackwardsCompatibilityModeEnabled)
                    //{
                    //    pipeline.AddLast("FrameEncoder", new HeliosBackwardsCompatabilityLengthFramePrepender(4, false));
                    //}
                    //else
                    //{
                    pipeline.AddLast("FrameEncoder", new LengthFieldPrepender(Settings.ByteOrder, 4, 0, false));
                    //}
                }
            }
        }

        private void SetClientPipeline(IChannel channel, Address remoteAddress)
        {
            if (Settings.EnableSsl)
            {
                var certificate = Settings.Ssl.Certificate;
                var host = certificate.GetNameInfo(X509NameType.DnsName, false);

                var tlsHandler = Settings.Ssl.SuppressValidation
                    ? new TlsHandler(new ClientTlsSettings(host) { ServerCertificateValidation = (cert, chain, errors) => true })
                    : TlsHandler.Client(host, certificate);

                channel.Pipeline.AddFirst("TlsHandler", tlsHandler);
            }

            SetInitialChannelPipeline(channel);
            var pipeline = channel.Pipeline;

            if (InternalTransport == TransportMode.Tcp)
            {
                if (TransferBatchSize > 1)
                {
                    var handler = !Settings.EnableMsgpackPooling ?
                        new TcpBatchClientHandler(this, Logging.GetLogger(System, typeof(TcpBatchClientHandler)), remoteAddress) :
                        (IChannelHandler)new TcpBatchClientHandlerWithPooling(this, Logging.GetLogger(System, typeof(TcpBatchClientHandlerWithPooling)), remoteAddress);
                    pipeline.AddLast("ClientHandler", handler);
                }
                else
                {
                    var handler = !Settings.EnableMsgpackPooling ?
                        new TcpClientHandler(this, Logging.GetLogger(System, typeof(TcpClientHandler)), remoteAddress) :
                        (IChannelHandler)new TcpClientHandlerWithPooling(this, Logging.GetLogger(System, typeof(TcpClientHandlerWithPooling)), remoteAddress);
                    pipeline.AddLast("ClientHandler", handler);
                }
            }
        }

        private void SetServerPipeline(IChannel channel)
        {
            if (Settings.EnableSsl)
            {
                channel.Pipeline.AddFirst("TlsHandler", TlsHandler.Server(Settings.Ssl.Certificate));
            }

            SetInitialChannelPipeline(channel);
            var pipeline = channel.Pipeline;

            if (Settings.TransportMode == TransportMode.Tcp)
            {
                if (TransferBatchSize > 1)
                {
                    var handler = !Settings.EnableMsgpackPooling ?
                        new TcpBatchServerHandler(this, Logging.GetLogger(System, typeof(TcpBatchServerHandler)), AssociationListenerPromise.Task) :
                        (IChannelHandler)new TcpBatchServerHandlerWithPooling(this, Logging.GetLogger(System, typeof(TcpBatchServerHandlerWithPooling)), AssociationListenerPromise.Task);
                    pipeline.AddLast("ServerHandler", handler);
                }
                else
                {
                    var handler = !Settings.EnableMsgpackPooling ?
                        new TcpServerHandler(this, Logging.GetLogger(System, typeof(TcpServerHandler)), AssociationListenerPromise.Task) :
                        (IChannelHandler)new TcpServerHandlerWithPooling(this, Logging.GetLogger(System, typeof(TcpServerHandlerWithPooling)), AssociationListenerPromise.Task);
                    pipeline.AddLast("ServerHandler", handler);
                }
            }
        }

        private ServerBootstrap ServerFactory()
        {
            if (InternalTransport != TransportMode.Tcp)
                throw new NotSupportedException("Currently DotNetty server supports only TCP tranport mode.");

            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            var server = new ServerBootstrap();
            server.Group(_serverBossGroup, _serverWorkerGroup);
            if (Settings.EnableLibuv)
            {
                server.Channel<TcpServerChannel>();
            }
            else
            {
                server.ChannelFactory(() => Settings.EnforceIpFamily
                    ? new TcpServerSocketChannel(addressFamily)
                    : new TcpServerSocketChannel());
            }
            if (Settings.EnableLibuv)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    server
                        .Option(ChannelOption.SoReuseport, Settings.TcpReusePort)
                        .ChildOption(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr);
                }
                else
                {
                    server.Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr);
                }
            }
            else
            {
                server.Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr);
            }

            server.Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                  .Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                  .Option(ChannelOption.AutoRead, false)
                  .Option(ChannelOption.SoBacklog, Settings.Backlog)
                  //.Option(ChannelOption.SoLinger, Settings.TcpLinger)

                  .Option(ChannelOption.Allocator, Settings.EnableBufferPooling ? (IByteBufferAllocator)PooledByteBufferAllocator.Default : UnpooledByteBufferAllocator.Default)
                  .ChildHandler(new ActionChannelInitializer<ISocketChannel>(SetServerPipeline));

            if (Settings.ReceiveBufferSize.HasValue) { server.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value); }
            if (Settings.SendBufferSize.HasValue) { server.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value); }

            if (Settings.WriteBufferHighWaterMark.HasValue) server.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value);
            if (Settings.WriteBufferLowWaterMark.HasValue) server.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value);

            return server;
        }

        private static async Task<IPEndPoint> ResolveNameAsync(DnsEndPoint address)
        {
            var resolved = await Dns.GetHostEntryAsync(address.Host).ConfigureAwait(false);
            //NOTE: for some reason while Helios takes first element from resolved address list
            // on the DotNetty side we need to take the last one in order to be compatible
            return new IPEndPoint(resolved.AddressList[resolved.AddressList.Length - 1], address.Port);
        }

        private static async Task<IPEndPoint> ResolveNameAsync(DnsEndPoint address, AddressFamily addressFamily)
        {
            var resolved = await Dns.GetHostEntryAsync(address.Host).ConfigureAwait(false);
            var found = resolved.AddressList.LastOrDefault(a => a.AddressFamily == addressFamily);
            if (found == null)
            {
                throw new KeyNotFoundException($"Couldn't resolve IP endpoint from provided DNS name '{address}' with address family of '{addressFamily}'");
            }

            return new IPEndPoint(found, address.Port);
        }

        #endregion

        #region static methods

        public static Address MapSocketToAddress(IPEndPoint socketAddress, string schemeIdentifier, string systemName, string hostName = null, int? publicPort = null)
        {
            return socketAddress == null
                ? null
                : new Address(schemeIdentifier, systemName, SafeMapHostName(hostName) ?? SafeMapIPv6(socketAddress.Address), publicPort ?? socketAddress.Port);
        }

        private static string SafeMapHostName(string hostName)
            => !string.IsNullOrEmpty(hostName) && IPAddress.TryParse(hostName, out var ip) ? SafeMapIPv6(ip) : hostName;

        private static string SafeMapIPv6(IPAddress ip)
            => ip.AddressFamily == AddressFamily.InterNetworkV6 ? "[" + ip + "]" : ip.ToString();

        public static EndPoint ToEndpoint(Address address)
        {
            if (!address.Port.HasValue) throw new ArgumentNullException(nameof(address), $"Address port must not be null: {address}");

            return IPAddress.TryParse(address.Host, out var ip)
                ? (EndPoint)new IPEndPoint(ip, address.Port.Value)
                : new DnsEndPoint(address.Host, address.Port.Value);
        }

        /// <summary>Maps an Akka.NET address to correlated <see cref="EndPoint"/>.</summary>
        /// <param name="address">Akka.NET fully qualified node address.</param>
        /// <exception cref="ArgumentException">Thrown if address port was not provided.</exception>
        /// <returns> <see cref="IPEndPoint"/> for IP-based addresses, <see cref="DnsEndPoint"/> for named addresses.</returns>
        public static EndPoint AddressToSocketAddress(Address address)
        {
            if (address.Port == null) ThrowHelper.ThrowArgumentException_Transport_AddrPortIsNull(address);
            EndPoint listenAddress;
            if (IPAddress.TryParse(address.Host, out var ip))
            {
                listenAddress = new IPEndPoint(ip, (int)address.Port);
            }
            else
            {
                // DNS resolution will be performed by the transport
                listenAddress = new DnsEndPoint(address.Host, (int)address.Port);
            }
            return listenAddress;
        }

        #endregion
    }

    #endregion

    #region == class HeliosBackwardsCompatabilityLengthFramePrepender ==

    //internal sealed class HeliosBackwardsCompatabilityLengthFramePrepender : LengthFieldPrepender
    //{
    //    private readonly List<object> _temporaryOutput = new List<object>(2);

    //    public HeliosBackwardsCompatabilityLengthFramePrepender(int lengthFieldLength, bool lengthFieldIncludesLengthFieldLength)
    //        : base(ByteOrder.LittleEndian, lengthFieldLength, 0, lengthFieldIncludesLengthFieldLength) { }

    //    protected override void Encode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
    //    {
    //        base.Encode(context, message, output);
    //        var lengthFrame = (IByteBuffer)_temporaryOutput[0];
    //        var combined = lengthFrame.WriteBytes(message);
    //        ReferenceCountUtil.SafeRelease(message, 1); // ready to release it - bytes have been copied
    //        output.Add(combined.Retain());
    //        _temporaryOutput.Clear();
    //    }
    //}

    #endregion
}