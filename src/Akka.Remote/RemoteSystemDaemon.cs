﻿//-----------------------------------------------------------------------
// <copyright file="RemoteSystemDaemon.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Util;
using Akka.Util.Internal;
using Akka.Util.Internal.Collections;

namespace Akka.Remote
{
    /// <summary>INTERNAL API</summary>
    internal interface IDaemonMsg { }

    /// <summary>INTERNAL API</summary>
    internal sealed class DaemonMsgCreate : IDaemonMsg
    {
        /// <summary>Initializes a new instance of the <see cref="DaemonMsgCreate"/> class.</summary>
        /// <param name="props">The props.</param>
        /// <param name="deploy">The deploy.</param>
        /// <param name="path">The path.</param>
        /// <param name="supervisor">The supervisor.</param>
        public DaemonMsgCreate(Props props, Deploy deploy, string path, IActorRef supervisor)
        {
            Props = props;
            Deploy = deploy;
            Path = path;
            Supervisor = supervisor;
        }

        /// <summary>Gets the props.</summary>
        /// <value>The props.</value>
        public Props Props { get; private set; }

        /// <summary>Gets the deploy.</summary>
        /// <value>The deploy.</value>
        public Deploy Deploy { get; private set; }

        /// <summary>Gets the path.</summary>
        /// <value>The path.</value>
        public string Path { get; private set; }

        /// <summary>Gets the supervisor.</summary>
        /// <value>The supervisor.</value>
        public IActorRef Supervisor { get; private set; }
    }

    /// <summary>
    /// INTERNAL API
    ///
    /// Internal system "daemon" actor for remote internal communication.
    ///
    /// It acts as the brain of the remote that responds to system remote messages and executes
    /// actions accordingly.
    /// </summary>
    internal class RemoteSystemDaemon : VirtualPathContainer
    {
        private readonly ActorSystemImpl _system;
        private readonly Switch _terminating = new Switch(false);
        private readonly ConcurrentDictionary<IActorRef, IImmutableSet<IActorRef>> _parent2Children = new ConcurrentDictionary<IActorRef, IImmutableSet<IActorRef>>(ActorRefComparer.Instance);
        private readonly IActorRef _terminator;

        /// <summary>Initializes a new instance of the <see cref="RemoteSystemDaemon"/> class.</summary>
        /// <param name="system">The system.</param>
        /// <param name="path">The path.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="terminator">TBD</param>
        /// <param name="log">TBD</param>
        public RemoteSystemDaemon(ActorSystemImpl system, ActorPath path, IInternalActorRef parent, IActorRef terminator, ILoggingAdapter log)
            : base(system.Provider, path, parent, log)
        {
            _terminator = terminator;
            _system = system;
            AddressTerminatedTopic.Get(system).Subscribe(this);
        }

        private void TerminationHookDoneWhenNoChildren()
        {
            _terminating.WhileOn(() =>
            {
                if (!HasChildren)
                {
                    _terminator.Tell(TerminationHookDone.Instance, this);
                }
            });
        }

        /// <summary>Tells the internal.</summary>
        /// <param name="message">The message.</param>
        /// <param name="sender">The sender.</param>
        protected override void TellInternal(object message, IActorRef sender)
        {
            switch (message)
            {
                //note: RemoteDaemon does not handle ActorSelection messages - those are handled directly by the RemoteActorRefProvider.
                case IDaemonMsg _:
                    if (Log.IsDebugEnabled) Log.ReceivedCommandToRemoteSystemDaemonOn(message, Path);
                    if (message is DaemonMsgCreate daemon) HandleDaemonMsgCreate(daemon);
                    break;

                case ActorSelectionMessage sel:
                    var iter = sel.Elements.Iterator();

                    Tuple<IEnumerable<string>, object> Rec(IImmutableList<string> acc)
                    {
                        while (true)
                        {
                            if (iter.IsEmpty())
                                return Tuple.Create(acc.Reverse(), sel.Message);

                            // find child elements, and the message to send, which is a remaining ActorSelectionMessage
                            // in case of SelectChildPattern, otherwise the actual message of the selection
                            switch (iter.Next())
                            {
                                case SelectChildName n:
                                    acc = ImmutableList.Create(n.Name).AddRange(acc);
                                    continue;
                                case SelectParent p when acc.Count <= 0:
                                    continue;
                                case SelectParent p:
                                    acc = acc.Skip(1).ToImmutableList();
                                    continue;
                                case SelectChildPattern pat:
                                    return Tuple.Create<IEnumerable<string>, object>(acc.Reverse(), sel.Copy(elements: new[] { pat }.Concat(iter.ToVector()).ToArray()));
                                default: // compiler ceremony - should never be hit
                                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_Unknown_ActorSelPart);
                                    break;
                            }
                        }
                    }

                    var t = Rec(ImmutableList<string>.Empty);
                    var concatenatedChildNames = t.Item1;
                    var m = t.Item2;

                    var child = GetChild(concatenatedChildNames);
                    if (child.IsNobody())
                    {
                        var emptyRef = new EmptyLocalActorRef(_system.Provider,
                            Path / sel.Elements.Select(el => el.ToString()), _system.EventStream);
                        emptyRef.Tell(sel, sender);
                    }
                    else
                    {
                        child.Tell(m, sender);
                    }
                    break;

                //Remote ActorSystem on another process / machine has died.
                //Need to clean up any references to remote deployments here.
                case AddressTerminated addressTerminated:
                    //stop any remote actors that belong to this address
                    ForEachChild(@ref =>
                    {
                        if (@ref.Parent.Path.Address == addressTerminated.Address) _system.Stop(@ref);
                    });
                    break;

                case Identify identify:
                    sender.Tell(new ActorIdentity(identify.MessageId, this));
                    break;

                case TerminationHook _:
                    _terminating.SwitchOn(() =>
                    {
                        TerminationHookDoneWhenNoChildren();
                        ForEachChild(c => _system.Stop(c));
                    });
                    break;

                default:
                    break;
            }
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        public override void SendSystemMessage(ISystemMessage message)
        {
            if (message is DeathWatchNotification deathWatchNotification)
            {
                var parent = deathWatchNotification.Actor;
                switch (parent)
                {
                    case ActorRefWithCell child:
                        if (child.IsLocal)
                        {
                            _terminating.Locked(() =>
                            {
                                var name = child.Path.Elements.Drop(1).Join("/");
                                RemoveChild(name, child);
                                var childParent = child.Parent;
                                if (RemoveChildParentNeedsUnwatch(childParent, child))
                                {
                                    childParent.SendSystemMessage(new Unwatch(childParent, this));
                                }
                                TerminationHookDoneWhenNoChildren();
                            });
                        }
                        break;
                    case IActorRefScope parentWithScope when !parentWithScope.IsLocal:
                        _terminating.Locked(() =>
                        {
                            if (_parent2Children.TryRemove(parent, out var children))
                            {
                                foreach (var c in children)
                                {
                                    _system.Stop(c);
                                    var name = c.Path.Elements.Drop(1).Join("/");
                                    RemoveChild(name, c);
                                }
                                TerminationHookDoneWhenNoChildren();
                            }
                        });
                        break;
                    default:
                        break;
                }
            }
            else
            {
                base.SendSystemMessage(message);
            }
        }

        /// <summary>Handles the daemon MSG create.</summary>
        /// <param name="message">The message.</param>
        private void HandleDaemonMsgCreate(DaemonMsgCreate message)
        {
            var supervisor = (IInternalActorRef)message.Supervisor;
            var parent = supervisor;
            var props = message.Props;
            if (ActorPath.TryParse(message.Path, out var childPath))
            {
                var subPath = childPath.ElementsWithUid.Drop(1); //drop the /remote
                ActorPath p = Path / subPath;
                var s = subPath.Join("/");
                var i = s.IndexOf("#", StringComparison.Ordinal);
                var childName = i < 0 ? s : s.Substring(0, i); // extract the name without the UID
                var localProps = props; //.WithDeploy(new Deploy(Scope.Local));

                bool isTerminating = !_terminating.WhileOff(() =>
                {
                    IInternalActorRef actor = _system.Provider.ActorOf(_system, localProps, supervisor, p, false,
                    message.Deploy, true, false);

                    AddChild(childName, actor);
                    actor.SendSystemMessage(new Watch(actor, this));
                    actor.Start();
                    if (AddChildParentNeedsWatch(parent, actor))
                    {
                        //TODO: figure out why current transport is not set when this message is sent
                        parent.SendSystemMessage(new Watch(parent, this));
                    }
                });
                if (isTerminating)
                {
                    Log.SkippingToRemoteSystemDaemonOnWhileTerminating(message, p);
                }
            }
            else
            {
                if (Log.IsDebugEnabled) Log.RemotePathDoesNotMatchPathFromMessage(message);
            }
        }

        /// <summary>Find the longest matching path which we know about and return that <see
        /// cref="IActorRef"/> (or ask that <see cref="IActorRef"/> to continue searching if elements
        /// are left).</summary>
        /// <param name="name">The name.</param>
        /// <returns>ActorRef.</returns>
        public override IActorRef GetChild(IEnumerable<string> name)
        {
            var elements = name.ToArray();
            var path = elements.Join("/");
            var n = 0;
            while (true)
            {
                var nameAndUid = ActorCell.SplitNameAndUid(path);
                var child = GetChild(nameAndUid.Name);
                if (child == null)
                {
                    var last = path.LastIndexOf("/", StringComparison.Ordinal);
                    if (last == -1) { return Nobody.Instance; }
                    path = path.Substring(0, last);
                    n++;
                    continue;
                }
                if (nameAndUid.Uid != ActorCell.UndefinedUid && nameAndUid.Uid != child.Path.Uid) { return Nobody.Instance; }

                return n == 0 ? child : child.GetChild(elements.TakeRight(n));
            }
        }

        private IInternalActorRef GetChild(string name)
        {
            var nameAndUid = ActorCell.SplitNameAndUid(name);
            if (TryGetChild(nameAndUid.Name, out var child))
            {
                if (nameAndUid.Uid != ActorCell.UndefinedUid && nameAndUid.Uid != child.Path.Uid)
                {
                    return ActorRefs.Nobody;
                }
            }
            return child;
        }

        private bool AddChildParentNeedsWatch(IActorRef parent, IActorRef child)
        {
            const bool weDontHaveTailRecursion = true;
            while (weDontHaveTailRecursion)
            {
                if (_parent2Children.TryAdd(parent, ImmutableHashSet<IActorRef>.Empty.Add(child)))
                {
                    return true; //child was successfully added
                }

                if (_parent2Children.TryGetValue(parent, out var children))
                {
                    if (_parent2Children.TryUpdate(parent, children.Add(child), children))
                    {
                        return false; //child successfully added
                    }
                }
            }
        }

        private bool RemoveChildParentNeedsUnwatch(IActorRef parent, IActorRef child)
        {
            const bool weDontHaveTailRecursion = true;
            while (weDontHaveTailRecursion)
            {
                if (!_parent2Children.TryGetValue(parent, out var children))
                {
                    return false; //parent is missing, so child does not need to be removed
                }

                if (_parent2Children.TryUpdate(parent, children.Remove(child), children))
                {
                    return true; //child was removed
                }
            }
        }
    }
}