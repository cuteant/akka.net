﻿//-----------------------------------------------------------------------
// <copyright file="ReceiveActorTests_LifeCycle.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Xunit;

namespace Akka.Tests.Actor
{
    public partial class ReceiveActorSlimTests
    {
        [Fact]
        public void Given_actor_When_it_restarts_Then_uses_the_handler()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<CrashActor>("crash");

            //When
            actor.Tell("CRASH");

            //Then
            actor.Tell("hello", TestActor);
            ExpectMsg((object)"1:hello");
        }

        [Fact]
        public void Given_actor_that_has_replaced_its_initial_handler_When_it_restarts_Then_uses_the_initial_handler()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<CrashActor>("crash");
            actor.Tell("BECOME-DISCARD");

            //When
            actor.Tell("CRASH", TestActor);

            //Then
            actor.Tell("hello", TestActor);
            ExpectMsg((object)"1:hello");
        }


        [Fact]
        public void Given_actor_that_has_pushed_a_new_handler_When_it_restarts_Then_uses_the_initial_handler()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<CrashActor>("crash");
            actor.Tell("BECOME");

            //When
            actor.Tell("CRASH", TestActor);

            //Then
            actor.Tell("hello", TestActor);
            ExpectMsg((object)"1:hello");
        }

        private class CrashActor : ReceiveActorSlim
        {
            private readonly PatternMatchBuilder _builder2;

            public CrashActor()
            {
                _builder2 = new PatternMatchBuilder();
                _builder2.Match<string>(handler: s => { throw new Exception("Crash!"); }, s => s == "CRASH");
                _builder2.Match<string>(handler: s => Sender.Tell("2:" + s));

                Receive<string>(s => s == "CRASH", s => { throw new Exception("Crash!"); });
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(_builder2));
                Receive<string>(s => s == "BECOME-DISCARD", _ => BecomeStacked(_builder2));
                Receive<string>(s => Sender.Tell("1:" + s));
            }
        }
    }
}

