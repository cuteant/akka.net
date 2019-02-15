using System;
using Akka.Actor;

namespace SimpleExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var system = ActorSystem.Create("SimpleExample");
            var supervisingActor = system.ActorOf(Props.Create<SupervisingActor>(), "supervising-actor");
            var ss = system.ActorOf<SupervisingActor>("s");
            supervisingActor.Tell("failChild");

            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }
    }
}
