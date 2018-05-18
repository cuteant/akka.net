//-----------------------------------------------------------------------
// <copyright file="ActorRefSinkActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Akka.Streams.Actors;

namespace Akka.Streams.Implementation
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ActorRefSinkActor : ActorSubscriber
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="ref">TBD</param>
        /// <param name="highWatermark">TBD</param>
        /// <param name="onCompleteMessage">TBD</param>
        /// <returns>TBD</returns>
        public static Props Props(IActorRef @ref, int highWatermark, object onCompleteMessage)
            => Actor.Props.Create(() => new ActorRefSinkActor(@ref, highWatermark, onCompleteMessage));

        private ILoggingAdapter _log;

        /// <summary>
        /// TBD
        /// </summary>
        protected readonly int HighWatermark;
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly IActorRef Ref;
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly object OnCompleteMessage;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="ref">TBD</param>
        /// <param name="highWatermark">TBD</param>
        /// <param name="onCompleteMessage">TBD</param>
        public ActorRefSinkActor(IActorRef @ref, int highWatermark, object onCompleteMessage)
        {
            Ref = @ref;
            HighWatermark = highWatermark;
            OnCompleteMessage = onCompleteMessage;
            RequestStrategy = new WatermarkRequestStrategy(highWatermark);

            Context.Watch(Ref);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected ILoggingAdapter Log => _log ?? (_log = Context.GetLogger());

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            switch (message)
            {
                case OnNext onNext:
                    Ref.Tell(onNext.Element);
                    return true;
                case OnError onError:
                    Ref.Tell(new Status.Failure(onError.Cause));
                    Context.Stop(Self);
                    return true;
                case OnComplete _:
                    Ref.Tell(OnCompleteMessage);
                    Context.Stop(Self);
                    return true;
                case Terminated terminated when (terminated.ActorRef.Equals(Ref)):
                    Context.Stop(Self); // will cancel upstream
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override IRequestStrategy RequestStrategy { get; }
    }
}
