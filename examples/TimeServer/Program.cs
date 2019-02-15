//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Net;
using Akka.Actor;
using Akka.Event;
using Akka.IO;

namespace TimeServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("TimeServer"))
            {
                Console.Title = "Server";

                //var server = system.ActorOf<TimeServerActor>("time");
                var port = 9391;
                var actor = system.ActorOf(Props.Create(() => new TimeService(new IPEndPoint(IPAddress.Any, port))), "timer-service");

                Console.ReadLine();
                Console.WriteLine("Shutting down...");
                Console.WriteLine("Terminated");
            }
        }
        public class TimeService : ReceiveActor
        {
            private readonly IActorRef _manager = Context.System.Tcp();

            public TimeService(EndPoint endpoint)
            {
                _manager.Tell(new Tcp.Bind(Self, endpoint));

                // To behave as TCP listener, actor should be able to handle Tcp.Connected messages
                Receive<Tcp.Connected>(connected =>
                {
                    Console.WriteLine("Remote address {0} connected", connected.RemoteAddress);
                    Sender.Tell(new Tcp.Register(Context.ActorOf(Props.Create(() => new TimeServerActor(Sender)), "time")));
                });
            }
        }

        public class TimeServerActor : ReceiveActor2
        {
            private readonly ILoggingAdapter _log = Context.GetLogger();

            public TimeServerActor(IActorRef connection)
            {
                // we want to know when the connection dies (without using Tcp.ConnectionClosed)
                Context.Watch(connection);

                Receive<string>(Handle);
                ReceiveAny(_ => Console.WriteLine(Self.Path));
            }

            public void Handle(string message)
            {
                if (message.ToLowerInvariant() == "gettime")
                {
                    var time =DateTime.UtcNow.ToLongTimeString();
                    Sender.Tell(time, Self);
                }
                else
                {

                    _log.Error("Invalid command: {0}", message);
                    var invalid = "Unrecognized command";
                    Sender.Tell(invalid, Self);
                }
            }
        }
    }
}

