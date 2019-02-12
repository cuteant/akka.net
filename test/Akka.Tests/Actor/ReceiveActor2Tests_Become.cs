//-----------------------------------------------------------------------
// <copyright file="ReceiveActorTests_Become.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Xunit;

namespace Akka.Tests.Actor
{
    public partial class ReceiveActor2Tests
    {
        [Fact]
        public void Given_actor_When_it_calls_Become_Then_it_switches_handler()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeActor>("become");
            system.EventStream.Subscribe(TestActor, typeof(UnhandledMessage));

            //When
            actor.Tell("BECOME", TestActor);    //Switch to state2   
            actor.Tell("hello", TestActor);
            actor.Tell(4711, TestActor);
            //Then
            ExpectMsg((object)"string2:hello");
            ExpectMsg<UnhandledMessage>(m => ((int)m.Message) == 4711 && m.Recipient == actor);

            //When
            actor.Tell("BECOME", TestActor);    //Switch to state3
            actor.Tell("hello", TestActor);
            actor.Tell(4711, TestActor);
            //Then
            ExpectMsg((object)"string3:hello");
            ExpectMsg<UnhandledMessage>(m => ((int)m.Message) == 4711 && m.Recipient == actor);
        }
        [Fact]
        public void Given_actor_When_it_calls_Become_Then_it_switches_handler2()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeActor2>("become");
            system.EventStream.Subscribe(TestActor, typeof(UnhandledMessage));

            //When
            actor.Tell("BECOME", TestActor);    //Switch to state2   
            actor.Tell("hello", TestActor);
            actor.Tell(4711, TestActor);
            //Then
            ExpectMsg((object)"string2:hello");
            ExpectMsg<UnhandledMessage>(m => ((int)m.Message) == 4711 && m.Recipient == actor);

            //When
            actor.Tell("BECOME", TestActor);    //Switch to state3
            actor.Tell("hello", TestActor);
            actor.Tell(4711, TestActor);
            //Then
            ExpectMsg((object)"string3:hello");
            ExpectMsg<UnhandledMessage>(m => ((int)m.Message) == 4711 && m.Recipient == actor);
        }

        [Fact]
        public void Given_actor_that_has_called_Become_When_it_calls_Unbecome_Then_it_switches_back_handler()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeActor>("become");
            actor.Tell("BECOME", TestActor);    //Switch to state2
            actor.Tell("BECOME", TestActor);    //Switch to state3

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);

            //Then
            ExpectMsg((object)"string2:hello");
        }
        [Fact]
        public void Given_actor_that_has_called_Become_When_it_calls_Unbecome_Then_it_switches_back_handler2()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeActor2>("become");
            actor.Tell("BECOME", TestActor);    //Switch to state2
            actor.Tell("BECOME", TestActor);    //Switch to state3

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);

            //Then
            ExpectMsg((object)"string2:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);

            //Then
            ExpectMsg((object)"string1:hello");
        }

        [Fact]
        public void Given_actor_that_has_called_Become_at_construction_time_When_it_calls_Unbecome_Then_it_switches_back_handler()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeDirectlyInConstructorActor>("become");

            //When
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string3:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string2:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string1:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //should still be in state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string1:hello");
        }

        [Fact]
        public void Given_actor_that_has_called_Become_at_construction_time_When_it_calls_Unbecome_Then_it_switches_back_handler2()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeDirectlyInConstructorActor2>("become");

            //When
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string3:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string2:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string1:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //should still be in state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string1:hello");
        }

        [Fact]
        public void Given_actor_that_has_called_Become_at_construction_time_When_it_calls_Unbecome_Then_it_switches_back_handler3()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeDirectlyInConstructorActor3>("become");

            //When
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string3:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string2:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string1:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //should still be in state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string1:hello");
        }

        [Fact]
        public void Given_actor_that_has_called_Become_at_construction_time_When_it_calls_Unbecome_Then_it_switches_back_handler4()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeDirectlyInConstructorActor4>("become");

            //When
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string3:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string5:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string6:hello");

            //When
            actor.Tell("BECOME", TestActor);  //should still be in state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string8:hello");
        }

        [Fact]
        public void Given_actor_that_has_called_Become_at_construction_time_When_it_calls_Unbecome_Then_it_switches_back_handler5()
        {
            //Given
            var system = ActorSystem.Create("test");
            var actor = system.ActorOf<BecomeDirectlyInConstructorActor5>("become");

            //When
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string3:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state2
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string5:hello");

            //When
            actor.Tell("UNBECOME", TestActor);  //Switch back to state1
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string6:hello");

            //When
            actor.Tell("BECOME", TestActor);  //should still be in state2
            actor.Tell("hello", TestActor);
            //Then
            ExpectMsg((object)"string8:hello");
        }

        private class BecomeActor : ReceiveActor2
        {
            public BecomeActor()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(State2));
                Receive<string>(s => Sender.Tell("string1:" + s, Self));
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
            }

            private void State2()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(State3));
                Receive<string>(s => Sender.Tell("string2:" + s, Self));
            }

            private void State3()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => Sender.Tell("string3:" + s, Self));
            }
        }

        private class BecomeActor2 : ReceiveActor2
        {
            private readonly PatternMatchBuilder _state2;
            private readonly PatternMatchBuilder _state3;

            public BecomeActor2()
            {
                _state2 = State2();
                _state3 = State3();
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(_state2));
                Receive<string>(s => Sender.Tell("string1:" + s, Self));
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
            }

            private PatternMatchBuilder State2()
            {
                var builder = new PatternMatchBuilder();
                builder.Match<string>(handler: __ => UnbecomeStacked(), s => s == "UNBECOME");
                builder.Match<string>(handler: _ => BecomeStacked(_state3), s => s == "BECOME");
                builder.Match<string>(handler: s => Sender.Tell("string2:" + s, Self));
                return builder;
            }

            private PatternMatchBuilder State3()
            {
                var builder = new PatternMatchBuilder();
                builder.Match<string>(handler: __ => UnbecomeStacked(), s => s == "UNBECOME");
                builder.Match<string>(handler: s => Sender.Tell("string3:" + s, Self));
                return builder;
            }
        }

        private class BecomeDirectlyInConstructorActor : ReceiveActor2
        {
            public BecomeDirectlyInConstructorActor()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(State2));
                Receive<string>(s => Sender.Tell("string1:" + s, Self));
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
                BecomeStacked(State2);
                BecomeStacked(State3);
            }

            private void State2()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(State3));
                Receive<string>(s => Sender.Tell("string2:" + s, Self));
            }

            private void State3()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => Sender.Tell("string3:" + s, Self));
            }
        }
        private class BecomeDirectlyInConstructorActor2 : ReceiveActor2
        {
            private readonly PatternMatchBuilder _state2;
            private readonly PatternMatchBuilder _state3;

            public BecomeDirectlyInConstructorActor2()
            {
                _state2 = ConfigurePatterns(State2);
                _state3 = ConfigurePatterns(State3);
                Receive();
                BecomeStacked(_state2);
                BecomeStacked(_state3);
            }

            private void Receive()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(_state2));
                Receive<string>(s => Sender.Tell("string1:" + s, Self));
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
            }

            private void State2()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => s == "BECOME", _ => BecomeStacked(_state3));
                Receive<string>(s => Sender.Tell("string2:" + s, Self));
            }

            private void State3()
            {
                Receive<string>(s => s == "UNBECOME", __ => UnbecomeStacked());
                Receive<string>(s => Sender.Tell("string3:" + s, Self));
            }
        }
        private class BecomeDirectlyInConstructorActor3 : ReceiveActor2
        {
            private readonly PatternMatchBuilder _state2;
            private readonly PatternMatchBuilder _state3;

            public BecomeDirectlyInConstructorActor3()
            {
                Receive();
                _state2 = Become(State2);
                _state3 = Become(State3);
            }

            private void Receive()
            {
                Receive<string>(s => s == "UNBECOME", __ => { });
                Receive<string>(s => s == "BECOME", _ => Become(_state2));
                Receive<string>(s => Sender.Tell("string1:" + s, Self));
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
            }

            private void State2()
            {
                Receive<string>(s => s == "UNBECOME", __ => Become(DefaultPatterns));
                Receive<string>(s => s == "BECOME", _ => Become(_state3));
                Receive<string>(s => Sender.Tell("string2:" + s, Self));
            }

            private void State3()
            {
                Receive<string>(s => s == "UNBECOME", __ => Become(_state2));
                Receive<string>(s => Sender.Tell("string3:" + s, Self));
            }
        }
        private class BecomeDirectlyInConstructorActor4 : ReceiveActor2
        {
            private readonly PatternMatchBuilder _state2;
            private readonly PatternMatchBuilder _state3;

            private int _counter = 0;

            public BecomeDirectlyInConstructorActor4()
            {
                Receive();
                _state2 = Become(State2);
                _state3 = Become(State3);
            }

            private void Receive()
            {
                Receive<string>(s => s == "UNBECOME", __ => { });
                Receive<string>(s => s == "BECOME", _ => Become(_state2));
                Receive<string>(s => { _counter++; Sender.Tell($"string{_counter}:" + s, Self); });
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
            }

            private void State2()
            {
                Receive<string>(s => s == "UNBECOME", __ => Become(DefaultPatterns));
                Receive<string>(s => s == "BECOME", _ => Become(_state3));
                Receive<string>(s => { _counter += 2; Sender.Tell($"string{_counter }:" + s, Self); });
            }

            private void State3()
            {
                Receive<string>(s => s == "UNBECOME", __ => Become(_state2));
                Receive<string>(s => { _counter += 3; Sender.Tell($"string{_counter}:" + s, Self); });
            }
        }
        private class BecomeDirectlyInConstructorActor5 : ReceiveActor2
        {
            private readonly PatternMatchBuilder _state2;
            private readonly PatternMatchBuilder _state3;

            public BecomeDirectlyInConstructorActor5()
            {
                int _counter = 0;

                _state2 = new PatternMatchBuilder();
                _state2.Match<string>(handler: __ => Become(DefaultPatterns), s => s == "UNBECOME");
                _state2.Match<string>(handler: _ => Become(_state3), s => s == "BECOME");
                _state2.Match<string>(handler: s => { _counter += 2; Sender.Tell($"string{_counter }:" + s, Self); });

                _state3 = new PatternMatchBuilder();
                _state3.Match<string>(handler: __ => Become(_state2), s => s == "UNBECOME");
                _state3.Match<string>(handler: s => { _counter += 3; Sender.Tell($"string{_counter}:" + s, Self); });

                Receive<string>(s => s == "UNBECOME", __ => { });
                Receive<string>(s => s == "BECOME", _ => Become(_state2));
                Receive<string>(s => { _counter++; Sender.Tell($"string{_counter}:" + s, Self); });
                Receive<int>(i => Sender.Tell("int1:" + i, Self));
                Become(_state2);
                Become(_state3);
            }
        }
    }
}

