﻿//-----------------------------------------------------------------------
// <copyright file="MsgDecoder.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Remote.Transport;
using Akka.Util;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;

namespace Akka.Remote.TestKit
{
    internal class MsgDecoder : MessageToMessageDecoder<object>
    {
        private readonly ILogger _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<MsgDecoder>();

        public static Address Proto2Address(in Akka.Serialization.Protocol.AddressData addr)
        {
            return new Address(addr.Protocol, addr.System, addr.Hostname, (int)addr.Port);
        }

        public static ThrottleTransportAdapter.Direction Proto2Direction(Protocol.Direction dir)
        {
            switch (dir)
            {
                case Protocol.Direction.Send: return ThrottleTransportAdapter.Direction.Send;
                case Protocol.Direction.Receive: return ThrottleTransportAdapter.Direction.Receive;
                case Protocol.Direction.Both:
                default: return ThrottleTransportAdapter.Direction.Both;
            }
        }

        protected object Decode(object message)
        {
            _logger.LogDebug("Decoding {0}", message);

            var w = message as Protocol.Wrapper;
            if (w is object)
            {
                if (w.Hello is object)
                {
                    return new Hello(w.Hello.Name, Proto2Address(w.Hello.Address));
                }
                else if (w.Barrier is object)
                {
                    switch (w.Barrier.Op)
                    {
                        case Protocol.BarrierOp.Succeeded: return new BarrierResult(w.Barrier.Name, true);
                        case Protocol.BarrierOp.Failed: return new BarrierResult(w.Barrier.Name, false);
                        case Protocol.BarrierOp.Fail: return new FailBarrier(w.Barrier.Name);
                        case Protocol.BarrierOp.Enter:
                            return new EnterBarrier(w.Barrier.Name, w.Barrier.Timeout > 0 ? (TimeSpan?)TimeSpan.FromTicks(w.Barrier.Timeout) : null);
                    }
                }
                else if (w.Failure is object)
                {
                    var f = w.Failure;
                    switch (f.Failure)
                    {
                        case Protocol.FailType.Throttle:
                            return new ThrottleMsg(Proto2Address(f.Address), Proto2Direction(f.Direction), f.RateMBit);
                        case Protocol.FailType.Abort:
                            return new DisconnectMsg(Proto2Address(f.Address), true);
                        case Protocol.FailType.Disconnect:
                            return new DisconnectMsg(Proto2Address(f.Address), false);
                        case Protocol.FailType.Exit:
                            return new TerminateMsg(new Right<bool, int>(f.ExitValue));
                        case Protocol.FailType.Shutdown:
                            return new TerminateMsg(new Left<bool, int>(false));
                        case Protocol.FailType.ShutdownAbrupt:
                            return new TerminateMsg(new Left<bool, int>(true));
                    }
                }
                else if (w.Addr is object)
                {
                    var a = w.Addr;
                    if (a.Addr.System is object)
                        return new AddressReply(new RoleName(a.Node), Proto2Address(a.Addr));

                    return new GetAddress(new RoleName(a.Node));
                }
                else if (!string.IsNullOrEmpty(w.Done))
                {
                    return Done.Instance;
                }
                else
                {
                    throw new ArgumentException($"unknown message {message}");
                }
            }

            throw new ArgumentException($"wrong message {message}");
        }

        protected override void Decode(IChannelHandlerContext context, object message, List<object> output)
        {
            var o = Decode(message);
            _logger.LogDebug("Decoded {0}", o);
            output.Add(o);
        }
    }
}
