﻿//-----------------------------------------------------------------------
// <copyright file="ActorCell.Children.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Actor.Internal;
using Akka.Util;
using CuteAnt.Text;

namespace Akka.Actor
{
    public partial class ActorCell
    {
        private volatile IChildrenContainer _childrenContainerDoNotCallMeDirectly = EmptyChildrenContainer.Instance;
        private long _nextRandomNameDoNotCallMeDirectly = -1; // Interlocked.Increment automatically adds 1 to this value. Allows us to start from 0.
        private ImmutableDictionary<string, FunctionRef> _functionRefsDoNotCallMeDirectly = ImmutableDictionary<string, FunctionRef>.Empty;

        /// <summary>
        /// The child container collection, used to house information about all child actors.
        /// </summary>
        public IChildrenContainer ChildrenContainer
        {
            get { return _childrenContainerDoNotCallMeDirectly; }
        }

        private IReadOnlyCollection<IActorRef> Children
        {
            get { return ChildrenContainer.Children; }
        }

        private ImmutableDictionary<string, FunctionRef> FunctionRefs => Volatile.Read(ref _functionRefsDoNotCallMeDirectly);
        internal bool TryGetFunctionRef(string name, out FunctionRef functionRef) =>
            FunctionRefs.TryGetValue(name, out functionRef);

        internal bool TryGetFunctionRef(string name, int uid, out FunctionRef functionRef) =>
            FunctionRefs.TryGetValue(name, out functionRef) && (uid == ActorCell.UndefinedUid || uid == functionRef.Path.Uid);

        internal FunctionRef AddFunctionRef(Action<IActorRef, object> tell, string suffix = "")
        {
            return AddFunctionRef(new ActionHandler<IActorRef, object>(tell), suffix);
        }

        internal FunctionRef AddFunctionRef(IHandle<IActorRef, object> tell, string suffix = "")
        {
            var r = GetRandomActorName("$$");
            var n = string.IsNullOrEmpty(suffix) ? r : r + "-" + suffix;
            var childPath = new ChildActorPath(Self.Path, n, NewUid());
            var functionRef = new FunctionRef(childPath, SystemImpl.Provider, SystemImpl.EventStream, tell);

            return ImmutableInterlocked.GetOrAdd(ref _functionRefsDoNotCallMeDirectly, childPath.Name, functionRef);
        }

        internal bool RemoveFunctionRef(FunctionRef functionRef)
        {
            if (functionRef.Path.Parent != Self.Path)
            {
                AkkaThrowHelper.ThrowInvalidOperationException_Trying_to_remove_FunctionRef_from_wrong_ActorCell(functionRef);
            }

            var name = functionRef.Path.Name;
            if (ImmutableInterlocked.TryRemove(ref _functionRefsDoNotCallMeDirectly, name, out var fref))
            {
                fref.Stop();
                return true;
            }
            else return false;
        }

        protected void StopFunctionRefs()
        {
            var refs = Interlocked.Exchange(ref _functionRefsDoNotCallMeDirectly, ImmutableDictionary<string, FunctionRef>.Empty);
            foreach (var pair in refs)
            {
                pair.Value.Stop();
            }
        }

        /// <summary>
        /// Attaches a child to the current <see cref="ActorCell"/>.
        /// 
        /// This method is used in the process of starting actors.
        /// </summary>
        /// <param name="props">The <see cref="Props"/> this child actor will use.</param>
        /// <param name="isSystemService">If <c>true</c>, then this actor is a system actor and activates a special initialization path.</param>
        /// <param name="name">The name of the actor being started. Can be <c>null</c>, and if it is we will automatically 
        /// generate a random name for this actor.</param>
        /// <exception cref="InvalidActorNameException">
        /// This exception is thrown if the given <paramref name="name"/> is an invalid actor name.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if a pre-creation serialization occurred.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if the actor tries to create a child while it is terminating or is terminated.
        /// </exception>
        /// <returns>A reference to the initialized child actor.</returns>
        public virtual IActorRef AttachChild(Props props, bool isSystemService, string name = null)
        {
            return MakeChild(props, name is null ? GetRandomActorName() : CheckName(name), true, isSystemService);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="props">TBD</param>
        /// <param name="name">TBD</param>
        /// <exception cref="InvalidActorNameException">
        /// This exception is thrown if the given <paramref name="name"/> is an invalid actor name.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// This exception is thrown if a pre-creation serialization occurred.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is thrown if the actor tries to create a child while it is terminating or is terminated.
        /// </exception>
        /// <returns>TBD</returns>
        public virtual IActorRef ActorOf(Props props, string name = null)
        {
            return ActorOf(props, name, false, false);
        }

        private IActorRef ActorOf(Props props, string name, bool isAsync, bool isSystemService)
        {
            if (name is null)
                name = GetRandomActorName();
            else
                CheckName(name);

            return MakeChild(props, name, isAsync, isSystemService);
        }

        private string GetRandomActorName(string prefix = "$")
        {
            var id = Interlocked.Increment(ref _nextRandomNameDoNotCallMeDirectly);
            var sb = StringBuilderCache.Acquire().Append(prefix);
            return id.Base64Encode(sb).ToString();
        }

        /// <summary>
        ///     Stops the specified child.
        /// </summary>
        /// <param name="child">The child.</param>
        public void Stop(IActorRef child)
        {
            if (ChildrenContainer.TryGetByRef(child, out _))
            {
                var repointableActorRef = child as RepointableActorRef;
                if (repointableActorRef is null || repointableActorRef.IsStarted)
                {
                    while (true)
                    {
                        var oldChildren = ChildrenContainer;
                        var newChildren = oldChildren.ShallDie(child);

                        if (SwapChildrenRefs(oldChildren, newChildren)) break;
                    }
                }
            }
            ((IInternalActorRef)child).Stop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SwapChildrenRefs(IChildrenContainer oldChildren, IChildrenContainer newChildren)
        {
            return ReferenceEquals(Interlocked.CompareExchange(ref _childrenContainerDoNotCallMeDirectly, newChildren, oldChildren), oldChildren);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="name">TBD</param>
        public void ReserveChild(string name)
        {
            while (true)
            {
                var oldChildren = ChildrenContainer;
                var newChildren = oldChildren.Reserve(name);

                if (SwapChildrenRefs(oldChildren, newChildren)) break;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="name">TBD</param>
        protected void UnreserveChild(string name)
        {
            while (true)
            {
                var oldChildren = ChildrenContainer;
                var newChildren = oldChildren.Unreserve(name);

                if (SwapChildrenRefs(oldChildren, newChildren)) break;
            }
        }

        /// <summary>
        /// This should only be used privately or when creating the root actor. 
        /// </summary>
        /// <param name="actor">TBD</param>
        /// <returns>TBD</returns>
        public ChildRestartStats InitChild(IInternalActorRef actor)
        {
            var name = actor.Path.Name;
            while (true)
            {
                var cc = ChildrenContainer;
                if (cc.TryGetByName(name, out var old))
                {
                    switch (old)
                    {
                        case ChildRestartStats restartStats:
                            return restartStats;
                        case ChildNameReserved _:
                            var crs = new ChildRestartStats(actor);
                            if (SwapChildrenRefs(cc, cc.Add(name, crs)))
                                return crs;
                            break;
                    }
                }
                else return null;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="reason">TBD</param>
        /// <returns>TBD</returns>
        protected bool SetChildrenTerminationReason(SuspendReason reason)
        {
            while (true)
            {
                if (ChildrenContainer is TerminatingChildrenContainer c)
                {
                    var n = c.CreateCopyWithReason(reason);
                    if (SwapChildrenRefs(c, n)) return true;
                }
                else return false;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected void SetTerminated()
        {
            Interlocked.Exchange(ref _childrenContainerDoNotCallMeDirectly, TerminatedChildrenContainer.Instance);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected bool IsNormal { get { return ChildrenContainer.IsNormal; } }
        /// <summary>
        /// TBD
        /// </summary>
        protected bool IsTerminating { get { return ChildrenContainer.IsTerminating; } }

        private bool IsWaitingForChildren  // This is called isWaitingForChildrenOrNull in AkkaJVM but is used like if returned a bool
        {
            get
            {
                return ChildrenContainer is TerminatingChildrenContainer terminating && terminating.Reason is SuspendReason.IWaitingForChildren;
            }
        }

        /// <summary>
        ///     Suspends the children.
        /// </summary>
        private void SuspendChildren(List<IActorRef> exceptFor = null)
        {
            if (exceptFor is null)
            {
                foreach (var stats in ChildrenContainer.Stats)
                {
                    var child = stats.Child;
                    child.Suspend();
                }
            }
            else
            {
                foreach (var stats in ChildrenContainer.Stats)
                {
                    var child = stats.Child;
                    if (!exceptFor.Contains(child))
                        child.Suspend();
                }
            }
        }

        /// <summary>
        ///     Resumes the children.
        /// </summary>
        private void ResumeChildren(Exception causedByFailure, IActorRef perpetrator)
        {
            foreach (var stats in ChildrenContainer.Stats)
            {
                var child = stats.Child;
                var cause = (perpetrator is object && child.Equals(perpetrator)) ? causedByFailure : null;
                child.Resume(cause);
            }
        }

        /// <summary>
        /// Tries to get the stats for the child with the specified name. The stats can be either <see cref="ChildNameReserved"/> 
        /// indicating that only a name has been reserved for the child, or a <see cref="ChildRestartStats"/> for a child that 
        /// has been initialized/created.
        /// </summary>
        /// <param name="name">TBD</param>
        /// <param name="child">TBD</param>
        /// <returns>TBD</returns>
        public bool TryGetChildStatsByName(string name, out IChildStats child)   //This is called getChildByName in Akka JVM
        {
            return ChildrenContainer.TryGetByName(name, out child);
        }

        /// <summary>
        /// Tries to get the stats for the child with the specified name. This ignores children for whom only names have been reserved.
        /// </summary>
        private bool TryGetChildRestartStatsByName(string name, out ChildRestartStats child)
        {
            if (ChildrenContainer.TryGetByName(name, out var stats))
            {
                child = stats as ChildRestartStats;
                if (child is object)
                    return true;
            }
            child = null;
            return false;
        }

        /// <summary>
        /// Tries to get the stats for the specified child.
        /// <remarks>Since the child exists <see cref="ChildRestartStats"/> is the only valid <see cref="IChildStats"/>.</remarks>
        /// </summary>
        /// <param name="actor">TBD</param>
        /// <param name="child">TBD</param>
        /// <returns>TBD</returns>
        protected bool TryGetChildStatsByRef(IActorRef actor, out ChildRestartStats child)   //This is called getChildByRef in Akka JVM
        {
            return ChildrenContainer.TryGetByRef(actor, out child);
        }

        // In Akka JVM there is a getAllChildStats here. Use ChildrenRefs.Stats instead

        /// <summary>
        /// Obsolete. Use <see cref="TryGetSingleChild(string, out IInternalActorRef)"/> instead.
        /// </summary>
        /// <param name="name">N/A</param>
        /// <returns>N/A</returns>
        [Obsolete("Use TryGetSingleChild [0.7.1]")]
        public IInternalActorRef GetSingleChild(string name)
        {
            return TryGetSingleChild(name, out var child) ? child : ActorRefs.Nobody;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="name">TBD</param>
        /// <param name="child">TBD</param>
        /// <returns>TBD</returns>
        public bool TryGetSingleChild(string name, out IInternalActorRef child)
        {
            if (name.IndexOf('#') < 0)
            {
                // optimization for the non-uid case
                if (TryGetChildRestartStatsByName(name, out var stats))
                {
                    child = stats.Child;
                    return true;
                }
                else if (TryGetFunctionRef(name, out var functionRef))
                {
                    child = functionRef;
                    return true;
                }
            }
            else
            {
                var nameAndUid = SplitNameAndUid(name);
                if (TryGetChildRestartStatsByName(nameAndUid.Name, out var stats))
                {
                    var uid = nameAndUid.Uid;
                    if (uid == ActorCell.UndefinedUid || uid == stats.Uid)
                    {
                        child = stats.Child;
                        return true;
                    }
                }
                else if (TryGetFunctionRef(nameAndUid.Name, nameAndUid.Uid, out var functionRef))
                {
                    child = functionRef;
                    return true;
                }
            }
            child = ActorRefs.Nobody;
            return false;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="child">TBD</param>
        /// <returns>TBD</returns>
        protected SuspendReason RemoveChildAndGetStateChange(IActorRef child)
        {
            if (ChildrenContainer is TerminatingChildrenContainer terminating)
            {
                var n = RemoveChild(child);
                if (!(n is TerminatingChildrenContainer))
                    return terminating.Reason;
                else
                    return null;
            }

            RemoveChild(child);
            return null;
        }

        private IChildrenContainer RemoveChild(IActorRef child)
        {
            while (true)
            {
                var oldChildren = ChildrenContainer;
                var newChildren = oldChildren.Remove(child);

                if (SwapChildrenRefs(oldChildren, newChildren)) return newChildren;
            }
        }

        private static string CheckName(string name)
        {
            if (name is null) AkkaThrowHelper.ThrowInvalidActorNameException(AkkaExceptionResource.InvalidActorName_Null);
            if (0u >= (uint)name.Length) AkkaThrowHelper.ThrowInvalidActorNameException(AkkaExceptionResource.InvalidActorName_Empty);
            if (!ActorPath.IsValidPathElement(name))
            {
                AkkaThrowHelper.ThrowInvalidActorNameException_Path(name);
            }
            return name;
        }

        private IInternalActorRef MakeChild(Props props, string name, bool async, bool systemService)
        {
            if (_serializeAllCreators && !systemService && !(props.Deploy.Scope is LocalScope))
            {
                CheckPropsArguments(props, name);
            }

            // In case we are currently terminating, fail external attachChild requests
            // (internal calls cannot happen anyway because we are suspended)
            if (ChildrenContainer.IsTerminating)
            {
                AkkaThrowHelper.ThrowInvalidOperationException(AkkaExceptionResource.InvalidOperation_ActorCell_IsTerminating);
            }

            // this name will either be unreserved or overwritten with a real child below
            ReserveChild(name);
            IInternalActorRef actor;
            try
            {
                var childPath = new ChildActorPath(Self.Path, name, NewUid());
                actor = _systemImpl.Provider.ActorOf(_systemImpl, props, _self, childPath,
                    systemService: systemService, deploy: null, lookupDeploy: true, async: async);
            }
            catch
            {
                //if actor creation failed, unreserve the name
                UnreserveChild(name);
                throw;
            }

            if (Mailbox is object && IsFailed)
            {
                for (var i = 1; i <= Mailbox.SuspendCount(); i++)
                {
                    actor.Suspend();
                }
            }

            //replace the reservation with the real actor
            InitChild(actor);
            actor.Start();
            return actor;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CheckPropsArguments(Props props, string name)
        {
            var oldInfo = Serialization.Serialization.CurrentTransportInformation;
            try
            {
                if (oldInfo is null)
                {
                    Serialization.Serialization.CurrentTransportInformation =
                        SystemImpl.Provider.SerializationInformation;
                }

                var ser = _systemImpl.Serialization;
                var propsArguments = props.Arguments;
                if (propsArguments is object)
                {
                    for (int idx = 0; idx < propsArguments.Length; idx++)
                    {
                        object argument = propsArguments[idx];
                        if (argument is object && !(argument is INoSerializationVerificationNeeded))
                        {
                            var serializer = ser.FindSerializerFor(argument);
                            var deserializedArgu = serializer.DeepCopy(argument);
                            if (deserializedArgu is null) { AkkaThrowHelper.ThrowArgumentException_ActorCellMakeChild(_self, name); }
                        }
                    }
                }
            }
            finally
            {
                Serialization.Serialization.CurrentTransportInformation = oldInfo;
            }
        }
    }
}

