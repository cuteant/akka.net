//-----------------------------------------------------------------------
// <copyright file="InternalTestActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Event;

namespace Akka.TestKit.Internal
{
    /// <summary>
    /// An actor that enqueues received messages to a <see cref="BlockingCollection{T}"/>.
    /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
    /// </summary>
    public class InternalTestActor : ActorBase
    {
        private readonly ITestActorQueue<MessageEnvelope> _queue;
        private TestKit.TestActor.Ignore _ignore;
        private AutoPilot _autoPilot;
        private DelegatingSupervisorStrategy _supervisorStrategy = new DelegatingSupervisorStrategy();

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="queue">TBD</param>
        public InternalTestActor(ITestActorQueue<MessageEnvelope> queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            global::System.Diagnostics.Debug.WriteLine("TestActor received " + message);
            var setIgnore = message as TestKit.TestActor.SetIgnore;
            if(setIgnore is object)
            {
                _ignore = setIgnore.Ignore;
                return true;
            }
            var watch = message as TestKit.TestActor.Watch;
            if(watch is object)
            {
                Context.Watch(watch.Actor);
                return true;
            }
            var unwatch = message as TestKit.TestActor.Unwatch;
            if(unwatch is object)
            {
                Context.Unwatch(unwatch.Actor);
                return true;
            }
            var setAutoPilot = message as TestKit.TestActor.SetAutoPilot;
            if(setAutoPilot is object)
            {
                _autoPilot = setAutoPilot.AutoPilot;
                return true;
            }

            var spawn = message as TestKit.TestActor.Spawn;
            if (spawn is object)
            {
                var actor = spawn.Apply(Context);
                if (spawn._supervisorStrategy.HasValue)
                {
                    _supervisorStrategy.Update(actor, spawn._supervisorStrategy.Value);
                }
                _queue.Enqueue(new RealMessageEnvelope(actor, Self));
                return true;
            }

            var actorRef = Sender;
            if(_autoPilot is object)
            {
                var newAutoPilot = _autoPilot.Run(actorRef, message);
                if(!(newAutoPilot is KeepRunning))
                    _autoPilot = newAutoPilot;
            }
            if(_ignore is null || !_ignore(message))
                _queue.Enqueue(new RealMessageEnvelope(message, actorRef));
            return true;
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            var self = Self;
            foreach(var messageEnvelope in _queue.ToList())
            {
                var messageSender = messageEnvelope.Sender;
                var message = messageEnvelope.Message;
                Context.System.DeadLetters.Tell(new DeadLetter(message, messageSender, self), messageSender);
            }
        }
    }
}
