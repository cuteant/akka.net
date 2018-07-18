//-----------------------------------------------------------------------
// <copyright file="BackoffSupervisorBase.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.Pattern
{
    /// <summary>
    /// TBD
    /// </summary>
    public abstract class BackoffSupervisorBase : ActorBase
    {
        internal BackoffSupervisorBase(Props childProps, string childName, IBackoffReset reset)
        {
            ChildProps = childProps;
            ChildName = childName;
            Reset = reset;
        }

        protected Props ChildProps { get; }
        protected string ChildName { get; }
        protected IBackoffReset Reset { get; }
        protected IActorRef Child { get; set; } = null;
        protected int RestartCountN { get; set; } = 0;

        protected override void PreStart()
        {
            StartChild();
            base.PreStart();
        }

        private void StartChild()
        {
            if (Child == null)
            {
                Child = Context.Watch(Context.ActorOf(ChildProps, ChildName));
            }
        }

        protected bool HandleBackoff(object message)
        {
            switch (message)
            {
                case BackoffSupervisor.StartChild _:
                    StartChild();
                    if (Reset is AutoReset backoffReset)
                    {
                        Context.System.Scheduler.ScheduleTellOnce(backoffReset.ResetBackoff, Self,
                            new BackoffSupervisor.ResetRestartCount(RestartCountN), Self);
                    }
                    break;

                case BackoffSupervisor.Reset _:
                    if (Reset is ManualReset)
                    {
                        RestartCountN = 0;
                    }
                    else
                    {
                        Unhandled(message);
                    }
                    break;

                case BackoffSupervisor.ResetRestartCount restartCount:
                    if (restartCount.Current == RestartCountN)
                    {
                        RestartCountN = 0;
                    }
                    break;

                case BackoffSupervisor.GetRestartCount _:
                    Sender.Tell(new BackoffSupervisor.RestartCount(RestartCountN));
                    break;

                case BackoffSupervisor.GetCurrentChild _:
                    Sender.Tell(new BackoffSupervisor.CurrentChild(Child));
                    break;

                default:
                    if (Child != null)
                    {
                        if (Child.Equals(Sender))
                        {
                            // use the BackoffSupervisor as sender
                            Context.Parent.Tell(message);
                        }
                        else
                        {
                            Child.Forward(message);
                        }
                    }
                    else
                    {
                        Context.System.DeadLetters.Forward(message);
                    }
                    break;
            }

            return true;
        }
    }
}
