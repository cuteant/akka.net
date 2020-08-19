//-----------------------------------------------------------------------
// <copyright file="GracefulStopSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Dispatch.SysMsg;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Actor
{
    /// <summary>
    /// Returns a <see cref="Task"/> that will be completed with success when existing messages
    /// of the target actor have been processed and the actor has been terminated.
    /// 
    /// Useful when you need to wait for termination or compose ordered termination of several actors,
    /// which should only be done outside of the <see cref="ActorSystem"/> as blocking inside <see cref="ActorBase"/> is discouraged.
    /// 
    /// <remarks><c>IMPORTANT:</c> the actor being terminated and its supervisor being informed of the availability of the deceased actor's name
    /// are two distinct operations, which do not obey any reliable ordering.</remarks>
    /// 
    /// If the target actor isn't terminated within the timeout the <see cref="Task"/> is completed with failure.
    /// 
    /// If you want to invoke specialized stopping logic on your target actor instead of <see cref="PoisonPill"/>, you can pass your stop command as a parameter:
    /// <code>
    ///     GracefulStop(someChild, timeout, MyStopGracefullyMessage).ContinueWith(r => {
    ///         // Do something after someChild starts being stopped.
    ///     });
    /// </code>
    /// </summary>
    public static class GracefulStopSupport
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="target">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <returns>TBD</returns>
        public static Task<bool> GracefulStop(this IActorRef target, TimeSpan timeout)
        {
            return GracefulStop(target, timeout, PoisonPill.Instance);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="target">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="stopMessage">TBD</param>
        /// <exception cref="TaskCanceledException">
        /// This exception is thrown if the underlying task is <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        /// <returns>TBD</returns>
        public static Task<bool> GracefulStop(this IActorRef target, TimeSpan timeout, object stopMessage)
        {
            var internalTarget = target.AsInstanceOf<IInternalActorRef>();

            var promiseRef = PromiseActorRef.Apply(internalTarget.Provider, timeout, target, stopMessage.GetType().Name);
            internalTarget.SendSystemMessage(new Watch(internalTarget, promiseRef));
            target.Tell(stopMessage, ActorRefs.NoSender);

            return promiseRef.Result.LinkOutcome(InvokeGracefulStopFunc, internalTarget, promiseRef, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static readonly Func<Task<object>, IInternalActorRef, PromiseActorRef, bool> InvokeGracefulStopFunc = (t, ar, pr) => InvokeGracefulStop(t, ar, pr);
        private static bool InvokeGracefulStop(Task<object> t, IInternalActorRef internalTarget, PromiseActorRef promiseRef)
        {
            if (t.IsSuccessfully())
            {
                switch (t.Result)
                {
                    case Terminated terminated:
                        return (terminated.ActorRef.Path.Equals(internalTarget.Path));
                    default:
                        internalTarget.SendSystemMessage(new Unwatch(internalTarget, promiseRef));
                        return false;
                }
            }
            else
            {
                internalTarget.SendSystemMessage(new Unwatch(internalTarget, promiseRef));
                return ThrowFailureAfterCompletion(t);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool ThrowFailureAfterCompletion(Task t)
        {
            if (t.IsCanceled)
                throw new TaskCanceledException();
            else
                throw t.Exception;
        }
    }
}

