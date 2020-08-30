//-----------------------------------------------------------------------
// <copyright file="ActorCell.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Util;
using Assert = System.Diagnostics.Debug;

namespace Akka.Actor
{
    /// <summary>
    /// INTERNAL API.
    ///
    /// The hosting infrastructure for actors.
    /// </summary>
    public partial class ActorCell : IUntypedActorContext, ICell
    {
        /// <summary>NOTE! Only constructor and ClearActorFields is allowed to update this</summary>
        private IInternalActorRef _self;

        /// <summary>
        /// Constant placeholder value for actors without a defined unique identifier.
        /// </summary>
        public const int UndefinedUid = 0;

        private Props _props;

        private const int DefaultState = 0;
        private const int SuspendedState = 1;
        private const int SuspendedWaitForChildrenState = 2;

        private ActorBase _actor;
        private bool _actorHasBeenCleared;
        private Mailbox _mailboxDoNotCallMeDirectly;
        private readonly ActorSystemImpl _systemImpl;
        private readonly bool _serializeAllMessages;
        private readonly bool _serializeAllCreators;
        private ActorTaskScheduler _taskScheduler;

        // special system message stash, used when we aren't able to handle other system messages just yet
        private LatestFirstSystemMessageList _sysMsgStash = SystemMessageList.LNil;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="msg">TBD</param>
        protected void Stash(SystemMessage msg)
        {
            Assert.Assert(msg.Unlinked);
            _sysMsgStash = _sysMsgStash + msg;
        }

        private LatestFirstSystemMessageList UnstashAll()
        {
            var unstashed = _sysMsgStash;
            _sysMsgStash = SystemMessageList.LNil;
            return unstashed;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        /// <param name="self">TBD</param>
        /// <param name="props">TBD</param>
        /// <param name="dispatcher">TBD</param>
        /// <param name="parent">TBD</param>
        public ActorCell(ActorSystemImpl system, IInternalActorRef self, Props props, MessageDispatcher dispatcher, IInternalActorRef parent)
        {
            _self = self;
            _props = props;
            _systemImpl = system;
            Parent = parent;
            Dispatcher = dispatcher;
            _serializeAllMessages = system.Settings.SerializeAllMessages;
            _serializeAllCreators = system.Settings.SerializeAllCreators;

            _watchAction = ar => InvokeWatch(ar);
            _unwatchAction = ar => InvokeUnwatch(ar);
            _unwatchWatchedActorsAciton = w => InvokeUnwatchWatchedActors(w);
            _addWatcherAction = w => InvokeAddWatcher(w);
            _remWatcherAction = w => InvokeRemWatcher(w);
            _addressTerminated = a => InvokeAddressTerminated(a);
            _publishOnFaultRecreateAction = InvokePublishOnFaultRecreate;
            _invokeFailureAction = (c, e) => HandleInvokeFailure(c, e);
            _publishOnHandleInvokeFailureAction = (c, e) => InvokePublishOnHandleInvokeFailure(c, e);
            _pulishOnFinishTerminateAction = e => InvokePulishOnFinishTerminate(e);
            _publishOnFinishRecreateAction = (e, a, c) => InvokePublishOnFinishRecreate(e, a, c);
            _publishOnHandleChildTerminatedAction = e => InvokePublishOnHandleChildTerminated(e);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public object CurrentMessage { get; internal set; }
        /// <summary>
        /// TBD
        /// </summary>
        public Mailbox Mailbox => Volatile.Read(ref _mailboxDoNotCallMeDirectly);

        /// <summary>
        /// TBD
        /// </summary>
        public MessageDispatcher Dispatcher { get; private set; }
        /// <summary>
        /// TBD
        /// </summary>
        public bool IsLocal { get { return true; } }
        /// <summary>
        /// TBD
        /// </summary>
        protected ActorBase Actor { get { return _actor; } }
        /// <summary>
        /// TBD
        /// </summary>
        public bool IsTerminated => Mailbox.IsClosed();
        /// <summary>
        /// TBD
        /// </summary>
        internal static ActorCell Current
        {
            get { return InternalCurrentActorCellKeeper.Current; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ActorSystem System { get { return _systemImpl; } }
        /// <summary>
        /// TBD
        /// </summary>
        public ActorSystemImpl SystemImpl { get { return _systemImpl; } }
        /// <summary>
        /// TBD
        /// </summary>
        public Props Props { get { return _props; } }
        /// <summary>
        /// TBD
        /// </summary>
        public IActorRef Self { get { return _self; } }
        IActorRef IActorContext.Parent { get { return Parent; } }
        /// <summary>
        /// TBD
        /// </summary>
        public IInternalActorRef Parent { get; private set; }
        /// <summary>
        /// TBD
        /// </summary>
        public IActorRef Sender { get; private set; }
        /// <summary>
        /// TBD
        /// </summary>
        public bool HasMessages { get { return Mailbox.HasMessages; } }
        /// <summary>
        /// TBD
        /// </summary>
        public int NumberOfMessages { get { return Mailbox.NumberOfMessages; } }
        /// <summary>
        /// TBD
        /// </summary>
        internal bool ActorHasBeenCleared { get { return _actorHasBeenCleared; } }
        /// <summary>
        /// TBD
        /// </summary>
        internal static Props TerminatedProps { get; } = new TerminatedProps();

        /// <summary>
        /// TBD
        /// </summary>
        public ActorTaskScheduler TaskScheduler
        {
            get
            {
                var taskScheduler = Volatile.Read(ref _taskScheduler);

                if (taskScheduler is object)
                    return taskScheduler;

                taskScheduler = new ActorTaskScheduler(this);
                return Interlocked.CompareExchange(ref _taskScheduler, taskScheduler, null) ?? taskScheduler;
            }
        }

        /// <summary>
        /// Initialize this cell, i.e. set up mailboxes and supervision. The UID must be
        /// reasonably different from the previous UID of a possible actor with the same path,
        /// which can be achieved by using <see cref="ThreadLocalRandom"/>
        /// </summary>
        /// <param name="sendSupervise">TBD</param>
        /// <param name="mailboxType">TBD</param>
        public void Init(bool sendSupervise, MailboxType mailboxType)
        {
            /*
             * Create the mailbox and enqueue the Create() message to ensure that
             * this is processed before anything else.
             */
            var mailbox = Dispatcher.CreateMailbox(this, mailboxType);

            Create createMessage;
            /*
             * The mailboxType was calculated taking into account what the MailboxType
             * has promised to produce. If that was more than the default, then we need
             * to reverify here because the dispatcher may well have screwed it up.
             */
            // we need to delay the failure to the point of actor creation so we can handle
            // it properly in the normal way
            var actorClass = Props.Type;
            if (System.Mailboxes.ProducesMessageQueue(mailboxType.GetType()) && System.Mailboxes.HasRequiredType(actorClass))
            {
                var req = System.Mailboxes.GetRequiredType(actorClass);
                if (req.IsInstanceOfType(mailbox.MessageQueue)) createMessage = new Create(null); //success
                else
                {
                    var gotType = mailbox.MessageQueue is null ? "null" : mailbox.MessageQueue.GetType().FullName;
                    createMessage = new Create(new ActorInitializationException(Self, $"Actor [{Self}] requires mailbox type [{req}] got [{gotType}]"));
                }
            }
            else
            {
                createMessage = new Create(null);
            }

            SwapMailbox(mailbox);
            Mailbox.SetActor(this);

            //// ➡➡➡ NEVER SEND THE SAME SYSTEM MESSAGE OBJECT TO TWO ACTORS ⬅⬅⬅
            var self = Self;
            mailbox.SystemEnqueue(self, createMessage);

            if (sendSupervise)
            {
                Parent.SendSystemMessage(new Supervise(self, async: false));
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="TryGetChildStatsByName(string, out IChildStats)"/> instead.
        /// </summary>
        /// <param name="name">N/A</param>
        /// <returns>N/A</returns>
        [Obsolete("Use TryGetChildStatsByName [0.7.1]", true)]
        public IInternalActorRef GetChildByName(string name)   //TODO: Should return  Option[ChildStats]
        {
            return TryGetSingleChild(name, out var child) ? child : ActorRefs.Nobody;
        }

        IActorRef IActorContext.Child(string name)
        {
            return TryGetSingleChild(name, out var child) ? child : ActorRefs.Nobody;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <returns>TBD</returns>
        public ActorSelection ActorSelection(string path)
        {
            return ActorRefFactoryShared.ActorSelection(path, _systemImpl, Self);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <returns>TBD</returns>
        public ActorSelection ActorSelection(ActorPath path)
        {
            return ActorRefFactoryShared.ActorSelection(path, _systemImpl);
        }


        IEnumerable<IActorRef> IActorContext.GetChildren()
        {
            return GetChildren();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public IEnumerable<IInternalActorRef> GetChildren()
        {
            return ChildrenContainer.Children;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="receive">TBD</param>
        public void Become(Receive receive)
        {
            _state = _state.Become(receive);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="receive">TBD</param>
        public void BecomeStacked(Receive receive)
        {
            _state = _state.BecomeStacked(receive);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void UnbecomeStacked()
        {
            _state = _state.UnbecomeStacked();
        }

        void IUntypedActorContext.Become(UntypedReceive receive)
        {
            bool LocalHandleMessage(object m)
            {
                receive(m);
                return true;
            }
            Become(m => LocalHandleMessage(m));
        }

        void IUntypedActorContext.BecomeStacked(UntypedReceive receive)
        {
            bool LocalHandleMessage(object m)
            {
                receive(m);
                return true;
            }
            BecomeStacked(m => LocalHandleMessage(m));
        }

        private long NewUid()
        {
            // Note that this uid is also used as hashCode in ActorRef, so be careful
            // to not break hashing if you change the way uid is generated
            var uid = ThreadLocalRandom.Current.Next();
            while (uid == UndefinedUid)
                uid = ThreadLocalRandom.Current.Next();
            return uid;
        }

        private ActorBase NewActor()
        {
            PrepareForNewActor();
            ActorBase instance = null;
            //set the thread static context or things will break
            UseThreadContext(() =>
            {
                _state = _state.ClearBehaviorStack();
                instance = CreateNewActorInstance();
                //TODO: this overwrites any already initialized supervisor strategy
                //We should investigate what we can do to handle this better
                instance.SupervisorStrategyInternal = _props.SupervisorStrategy;
                //defaults to null - won't affect lazy instantiation unless explicitly set in props
            });
            return instance;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        protected virtual ActorBase CreateNewActorInstance()
        {
            var actor = _props.NewActor();

            // Apply default of custom behaviors to actor.
#pragma warning disable CS0618 // 类型或成员已过时
            var pipeline = _systemImpl.ActorPipelineResolver.ResolvePipeline(actor.GetType());
#pragma warning restore CS0618 // 类型或成员已过时
            pipeline.AfterActorIncarnated(actor, this);

            var initializableActor = actor as IInitializableActor;
            if (initializableActor is object)
            {
                initializableActor.Init();
            }
            return actor;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="action">TBD</param>
        public void UseThreadContext(Action action)
        {
            var tmp = InternalCurrentActorCellKeeper.Current;
            InternalCurrentActorCellKeeper.Current = this;
            try
            {
                action();
            }
            finally
            {
                //ensure we set back the old context
                InternalCurrentActorCellKeeper.Current = tmp;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="action">TBD</param>
        /// <param name="state"></param>
        public void UseThreadContext<T>(Action<T> action, T state)
        {
            var tmp = InternalCurrentActorCellKeeper.Current;
            InternalCurrentActorCellKeeper.Current = this;
            try
            {
                action(state);
            }
            finally
            {
                //ensure we set back the old context
                InternalCurrentActorCellKeeper.Current = tmp;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="action">TBD</param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void UseThreadContext<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            var tmp = InternalCurrentActorCellKeeper.Current;
            InternalCurrentActorCellKeeper.Current = this;
            try
            {
                action(arg1, arg2);
            }
            finally
            {
                //ensure we set back the old context
                InternalCurrentActorCellKeeper.Current = tmp;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="action">TBD</param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        public void UseThreadContext<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            var tmp = InternalCurrentActorCellKeeper.Current;
            InternalCurrentActorCellKeeper.Current = this;
            try
            {
                action(arg1, arg2, arg3);
            }
            finally
            {
                //ensure we set back the old context
                InternalCurrentActorCellKeeper.Current = tmp;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        public virtual void SendMessage(in Envelope message)
        {
            if (Mailbox is null)
            {
                return;
                //stackoverflow if this is the deadletters actorref
                //this._systemImpl.DeadLetters.Tell(new DeadLetter(message, sender, this.Self));
            }

            try
            {
                if (!_serializeAllMessages)
                {
                    Dispatcher.Dispatch(this, message);
                }
                else
                {
                    var messageToDispatch = SerializeAndDeserialize(message);
                    Dispatcher.Dispatch(this, messageToDispatch);
                }
            }
            catch (Exception e)
            {
                HandleSendMessageFailed(e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleSendMessageFailed(Exception e)
        {
            _systemImpl.EventStream.Publish(new Error(e, _self.Parent.ToString(), ActorType, "Swallowing exception during message send"));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="sender">TBD</param>
        /// <param name="message">TBD</param>
        public virtual void SendMessage(IActorRef sender, object message)
        {
            SendMessage(new Envelope(message, sender, System));
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected void ClearActorCell()
        {
            UnstashAll();
            _props = TerminatedProps;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actor">TBD</param>
        protected void ClearActor(ActorBase actor)
        {
            if (actor is object)
            {
                if (actor is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        _systemImpl.Log?.AnErrorOccurredWhileDisposingActor(actor, e);
                    }
                }

                ReleaseActor(actor);
                actor.Clear(_systemImpl.DeadLetters);
            }
            _actorHasBeenCleared = true;
            CurrentMessage = null;

            //TODO: semantics here? should all "_state" be cleared? or just behavior?
            _state = _state.ClearBehaviorStack();
        }

        private void ReleaseActor(ActorBase a)
        {
            _props.Release(a);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected void PrepareForNewActor()
        {
            _state = _state.ClearBehaviorStack();
            _actorHasBeenCleared = false;
        }
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actor">TBD</param>
        protected void SetActorFields(ActorBase actor)
        {
            actor?.Unclear();
        }
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="name">TBD</param>
        /// <returns>TBD</returns>
        public static NameAndUid SplitNameAndUid(string name)
        {
            var i = name.IndexOf('#');
            return i < 0
                ? new NameAndUid(name, UndefinedUid)
                : new NameAndUid(name.Substring(0, i), Int32.Parse(name.Substring(i + 1)));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public static IActorRef GetCurrentSelfOrNoSender()
        {
            var current = Current;
            return current is object ? current.Self : ActorRefs.NoSender;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public static IActorRef GetCurrentSenderOrNoSender()
        {
            var current = Current;
            return current is object ? current.Sender : ActorRefs.NoSender;
        }

        private Envelope SerializeAndDeserialize(in Envelope envelope)
        {
            DeadLetter deadLetter;
            var unwrapped = (deadLetter = envelope.Message as DeadLetter) is object ? deadLetter.Message : envelope.Message;

            if (unwrapped is INoSerializationVerificationNeeded)
            {
                return envelope;
            }
            var serializer = _systemImpl.Serialization.FindSerializerFor(unwrapped);
            var deserializedMsg = serializer.DeepCopy(unwrapped);
            if (deadLetter is object)
                return new Envelope(new DeadLetter(deserializedMsg, deadLetter.Sender, deadLetter.Recipient), envelope.Sender);
            return new Envelope(deserializedMsg, envelope.Sender);
        }
    }
}

