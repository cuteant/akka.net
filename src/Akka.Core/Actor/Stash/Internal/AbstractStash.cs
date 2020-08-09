//-----------------------------------------------------------------------
// <copyright file="AbstractStash.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;
using Akka.Util;
using CuteAnt.Collections;

namespace Akka.Actor.Internal
{
    /// <summary>INTERNAL
    /// Abstract base class for stash support
    /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
    /// </summary>
    public abstract class AbstractStash : IStash
    {
        private Deque<Envelope> _theStash;
        private readonly ActorCell _actorCell;
        private readonly int _capacity;

        /// <summary>INTERNAL
        /// Abstract base class for stash support
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        /// <param name="context">TBD</param>
        /// <param name="capacity">TBD</param>
        /// <exception cref="NotSupportedException">This exception is thrown if the actor's mailbox isn't deque-based (e.g. <see cref="UnboundedDequeBasedMailbox"/>).</exception>
        protected AbstractStash(IActorContext context, int capacity = 100)
        {
            var actorCell = (ActorCell)context;
            Mailbox = actorCell.Mailbox.MessageQueue as IDequeBasedMessageQueueSemantics;
            if (Mailbox == null) { AkkaThrowHelper.ThrowNotSupportedException(actorCell); }
            _theStash = new Deque<Envelope>();
            _actorCell = actorCell;

            // TODO: capacity needs to come from dispatcher or mailbox config
            // https://github.com/akka/akka/blob/master/akka-actor/src/main/scala/akka/actor/Stash.scala#L126
            _capacity = capacity > 0 ? capacity : int.MaxValue;
        }

        private IDequeBasedMessageQueueSemantics Mailbox { get; }

        private int _currentEnvelopeId;

        /// <summary>
        /// Stashes the current message in the actor's state.
        /// </summary>
        /// <exception cref="IllegalActorStateException">This exception is thrown if we attempt to stash the same message more than once.</exception>
        /// <exception cref="StashOverflowException">
        /// This exception is thrown in the event that we're using a <see cref="BoundedMessageQueue"/>  for the <see cref="IStash"/> and we've exceeded capacity.
        /// </exception>
        public void Stash()
        {
            var currMsg = _actorCell.CurrentMessage;
            var sender = _actorCell.Sender;

            var currentEnvelopeId = _actorCell.CurrentEnvelopeId;
            if (currentEnvelopeId == _currentEnvelopeId)
            {
                AkkaThrowHelper.ThrowIllegalActorStateException_Stash(currMsg);
            }
            _currentEnvelopeId = currentEnvelopeId;

            if (_theStash.Count < _capacity)
            {
                _theStash.AddToBack(new Envelope(currMsg, sender));
            }
            else
            {
                AkkaThrowHelper.ThrowStashOverflowException_Stash(currMsg, _actorCell);
            }
        }

        /// <summary>
        /// Unstash the most recently stashed message (top of the message stack.)
        /// </summary>
        public void Unstash()
        {
            //if (_theStash.Count > 0)
            //{
            //    try
            //    {
            //        EnqueueFirst(_theStash.Head());
            //    }
            //    finally
            //    {
            //        _theStash.RemoveFirst();
            //    }
            //}
            if (_theStash.TryRemoveFromFront(out var item))
            {
                EnqueueFirst(item);
            }
        }

        /// <summary>
        /// Unstash all of the <see cref="Envelope"/>s in the Stash.
        /// </summary>
        public void UnstashAll()
        {
            UnstashAll(envelope => true);
        }

        /// <summary>
        /// Unstash all of the <see cref="Envelope"/>s in the Stash.
        /// </summary>
        /// <param name="predicate">A predicate function to determine which messages to select.</param>
        public void UnstashAll(Func<Envelope, bool> predicate)
        {
            //if(_theStash.Count > 0)
            //{
            try
            {
                //foreach (var item in _theStash.Reverse().Where(predicate))
                //{
                //    EnqueueFirst(item);
                //}
                _theStash.Reverse(item =>
                {
                    if (predicate(item)) { EnqueueFirst(item); }
                });
            }
            finally
            {
                //_theStash = new Deque<Envelope>();
                _theStash.Clear();
            }
            //}
        }

        /// <summary>
        /// Eliminates the contents of the <see cref="IStash"/>, and returns
        /// the previous contents of the messages.
        /// </summary>
        /// <returns>Previously stashed messages.</returns>
        public IEnumerable<Envelope> ClearStash()
        {
            if (_theStash.Count == 0) { return Enumerable.Empty<Envelope>(); }

            var stashed = _theStash;
            _theStash = new Deque<Envelope>();
            return stashed;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="envelopes">TBD</param>
        public void Prepend(IEnumerable<Envelope> envelopes)
        {
            // since we want to save the order of messages, but still prepending using AddFirst,
            // we must enumerate envelopes in reversed order
            foreach (var envelope in envelopes.Distinct().Reverse())
            {
                _theStash.AddToFront(envelope);
            }
        }

        /// <summary>
        /// Enqueues <paramref name="msg"/> at the first position in the mailbox. If the message contained in
        /// the envelope is a <see cref="Terminated"/> message, it will be ensured that it can be re-received
        /// by the actor.
        /// </summary>
        private void EnqueueFirst(in Envelope msg)
        {
            Mailbox.EnqueueFirst(msg);
            if (msg.Message is Terminated terminatedMessage)
            {
                _actorCell.TerminatedQueuedFor(terminatedMessage.ActorRef, Option<object>.None);
            }
        }
    }
}


