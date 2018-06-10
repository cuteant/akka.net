//-----------------------------------------------------------------------
// <copyright file="MsgEncoder.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Remote.Transport;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;

namespace Akka.Remote.TestKit
{
    internal class MsgEncoder : MessageToMessageEncoder<object>
    {
        private readonly ILogger _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<MsgEncoder>();

        private static Serialization.Protocol.AddressData AddressMessageBuilder(Address address)
        {
            var message = new Serialization.Protocol.AddressData(
                address.System,
                address.Host,
                (uint)(address.Port ?? 0),
                address.Protocol
            );
            return message;
        }

        public static Protocol.Direction Direction2Proto(ThrottleTransportAdapter.Direction dir)
        {
            switch (dir)
            {
                case ThrottleTransportAdapter.Direction.Send: return Protocol.Direction.Send;
                case ThrottleTransportAdapter.Direction.Receive: return Protocol.Direction.Receive;
                case ThrottleTransportAdapter.Direction.Both:
                default: return Protocol.Direction.Both;
            }
        }

        protected override void Encode(IChannelHandlerContext context, object message, List<object> output)
        {
            _logger.LogDebug("Encoding {0}", message);

            var wrapper = new Protocol.Wrapper();

            if (message is Hello)
            {
                var hello = (Hello)message;
                wrapper.Hello = new Protocol.Hello
                {
                    Name = hello.Name,
                    Address = AddressMessageBuilder(hello.Address)
                };
            }
            else if (message is EnterBarrier)
            {
                var enterBarrier = (EnterBarrier)message;
                wrapper.Barrier = new Protocol.EnterBarrier
                {
                    Name = enterBarrier.Name,
                    Timeout = enterBarrier.Timeout?.Ticks ?? 0,
                    Op = Protocol.BarrierOp.Enter,
                };
            }
            else if (message is BarrierResult)
            {
                var barrierResult = (BarrierResult)message;
                wrapper.Barrier = new Protocol.EnterBarrier
                {
                    Name = barrierResult.Name,
                    Op = barrierResult.Success
                        ? Protocol.BarrierOp.Succeeded
                        : Protocol.BarrierOp.Failed
                };
            }
            else if (message is FailBarrier)
            {
                var failBarrier = (FailBarrier)message;
                wrapper.Barrier = new Protocol.EnterBarrier
                {
                    Name = failBarrier.Name,
                    Op = Protocol.BarrierOp.Fail
                };
            }
            else if (message is ThrottleMsg)
            {
                var throttleMsg = (ThrottleMsg)message;
                wrapper.Failure = new Protocol.InjectFailure
                {
                    Address = AddressMessageBuilder(throttleMsg.Target),
                    Failure = Protocol.FailType.Throttle,
                    Direction = Direction2Proto(throttleMsg.Direction),
                    RateMBit = throttleMsg.RateMBit
                };
            }
            else if (message is DisconnectMsg)
            {
                var disconnectMsg = (DisconnectMsg)message;
                wrapper.Failure = new Protocol.InjectFailure
                {
                    Address = AddressMessageBuilder(disconnectMsg.Target),
                    Failure = disconnectMsg.Abort
                        ? Protocol.FailType.Abort
                        : Protocol.FailType.Disconnect
                };
            }
            else if (message is TerminateMsg)
            {
                var terminate = (TerminateMsg)message;
                if (terminate.ShutdownOrExit.IsRight)
                {
                    wrapper.Failure = new Protocol.InjectFailure()
                    {
                        Failure = Protocol.FailType.Exit,
                        ExitValue = terminate.ShutdownOrExit.ToRight().Value
                    };
                }
                else if (terminate.ShutdownOrExit.IsLeft && !terminate.ShutdownOrExit.ToLeft().Value)
                {
                    wrapper.Failure = new Protocol.InjectFailure()
                    {
                        Failure = Protocol.FailType.Shutdown
                    };
                }
                else
                {
                    wrapper.Failure = new Protocol.InjectFailure()
                    {
                        Failure = Protocol.FailType.ShutdownAbrupt
                    };
                }
            }
            else if (message is GetAddress)
            {
                var getAddress = (GetAddress)message;
                wrapper.Addr = new Protocol.AddressRequest { Node = getAddress.Node.Name };
            }
            else if (message is AddressReply)
            {
                var addressReply = (AddressReply)message;
                wrapper.Addr = new Protocol.AddressRequest
                {
                    Node = addressReply.Node.Name,
                    Addr = AddressMessageBuilder(addressReply.Addr)
                };
            }
            else if (message is Done)
            {
                wrapper.Done = " ";
            }

            output.Add(wrapper);
        }
    }
}
