//-----------------------------------------------------------------------
// <copyright file="TcpTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using DotNetty.Transport.Channels;

namespace Akka.Remote.Transport.DotNetty
{
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

    internal sealed class PoolingTcpAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new PoolingTcpAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class TcpBatchWriterAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new TcpBatchWriterAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class PoolingTcpBatchWriterAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new PoolingTcpBatchWriterAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class TcpBatchMessagesAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new TcpBatchMessagesAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }

    internal sealed class PoolingTcpBatchMessagesAssociationHandleFactory : ITcpAssociationHandleFactory
    {
        public AssociationHandle Create(Address localAddress, Address remoteAddress, DotNettyTransport transport, IChannel channel)
        {
            return new PoolingTcpBatchMessagesAssociationHandle(localAddress, remoteAddress, transport, channel);
        }
    }
}