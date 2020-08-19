//-----------------------------------------------------------------------
// <copyright file="TransportAdapters.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Event;
using Akka.Util;
using CuteAnt.Collections;
using CuteAnt.Reflection;

namespace Akka.Remote.Transport
{
    #region -- interface ITransportAdapterProvider --

    /// <summary>
    /// Interface for producing adapters that can wrap an underlying transport and augment it with
    /// additional behavior.
    /// </summary>
    public interface ITransportAdapterProvider
    {
        /// <summary>Create a transport adapter that wraps the underlying transport</summary>
        /// <param name="wrappedTransport">The transport that will be wrapped.</param>
        /// <param name="system">The actor system to which this transport belongs.</param>
        /// <returns>A transport wrapped with the new adapter.</returns>
        Transport Create(Transport wrappedTransport, ExtendedActorSystem system);
    }

    #endregion

    #region -- class TransportAdaptersExtension --

    /// <summary>INTERNAL API</summary>
    internal class TransportAdaptersExtension : ExtensionIdProvider<TransportAdapters>
    {
        /// <inheritdoc cref="ExtensionIdProvider{T}"/>
        public override TransportAdapters CreateExtension(ExtendedActorSystem system) => new TransportAdapters((ActorSystemImpl)system);

        #region Static methods

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public static TransportAdapters For(ActorSystem system) => system.WithExtension<TransportAdapters, TransportAdaptersExtension>();

        #endregion
    }

    #endregion

    #region == class TransportAdapters ==

    /// <summary>
    /// INTERNAL API
    ///
    /// Extension that allows us to look up transport adapters based upon the settings provided
    /// inside <see cref="RemoteSettings"/>
    /// </summary>
    internal sealed class TransportAdapters : IExtension
    {
        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        public TransportAdapters(ExtendedActorSystem system)
        {
            System = system;
            Settings = ((IRemoteActorRefProvider)system.Provider).RemoteSettings;
        }

        /// <summary>The ActorSystem</summary>
        public ActorSystem System { get; private set; }

        /// <summary>The Akka.Remote settings</summary>
        private readonly RemoteSettings Settings;

        private Dictionary<string, ITransportAdapterProvider> _adaptersTable;
        private Dictionary<string, ITransportAdapterProvider> AdaptersTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _adaptersTable ?? GetAdaptersTable(); }
        }

        private Dictionary<string, ITransportAdapterProvider> GetAdaptersTable()
        {
            if (_adaptersTable is object) return _adaptersTable;
            _adaptersTable = new Dictionary<string, ITransportAdapterProvider>(StringComparer.Ordinal);
            foreach (var adapter in Settings.Adapters)
            {
                try
                {
                    var adapterTypeName = TypeUtils.ResolveType(adapter.Value);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var newAdapter = ActivatorUtils.FastCreateInstance<ITransportAdapterProvider>(adapterTypeName);
                    _adaptersTable.Add(adapter.Key, newAdapter);
                }
                catch (Exception ex)
                {
                    ThrowHelper.ThrowArgumentException_Transport_Initiate(adapter.Value, ex);
                }
            }

            return _adaptersTable;
        }

        /// <summary>TBD</summary>
        /// <param name="name">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public ITransportAdapterProvider GetAdapterProvider(string name)
        {
            if (!AdaptersTable.TryGetValue(name, out var provider))
            {
                ThrowHelper.ThrowArgumentException_TransportAdapter_IsNoReg(name);
            }
            return provider;
        }
    }

    #endregion

    #region -- class SchemeAugmenter --

    /// <summary>Used to augment the protocol scheme of transports when enabled.</summary>
    public sealed class SchemeAugmenter
    {
        /// <summary>Creates a new <see cref="SchemeAugmenter"/> instance.</summary>
        /// <param name="addedSchemeIdentifier">
        /// The new identifier that will be added to the front of the pipeline.
        /// </param>
        public SchemeAugmenter(string addedSchemeIdentifier)
        {
            AddedSchemeIdentifier = addedSchemeIdentifier;
        }

        /// <summary>
        /// The scheme that will be added to the front of the protocol. I.E. if using a TLS
        /// augmentor, the this field might read "ssl" and the full scheme of addresses generated
        /// using this transport might read "akka.tcp.ssl", the latter part being added by this augmenter.
        /// </summary>
        public readonly string AddedSchemeIdentifier;

        /// <summary>TBD</summary>
        /// <param name="originalScheme">TBD</param>
        /// <returns>TBD</returns>
        public string AugmentScheme(string originalScheme) => $"{AddedSchemeIdentifier}.{originalScheme}";

        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public Address AugmentScheme(Address address)
        {
            var protocol = AugmentScheme(address.Protocol);
            return address.WithProtocol(protocol);
        }

        /// <summary>TBD</summary>
        /// <param name="scheme">TBD</param>
        /// <returns>TBD</returns>
        public string RemoveScheme(string scheme)
        {
            if (scheme.StartsWith($"{AddedSchemeIdentifier}.", StringComparison.Ordinal))
            {
                return scheme.Remove(0, AddedSchemeIdentifier.Length + 1);
            }

            return scheme;
        }

        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public Address RemoveScheme(Address address)
        {
            var protocol = RemoveScheme(address.Protocol);
            return address.WithProtocol(protocol);
        }
    }

    #endregion

    #region -- class AbstractTransportAdapter --

    /// <summary>An adapter that wraps a transport and provides interception capabilities</summary>
    public abstract class AbstractTransportAdapter : Transport
    {
        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        protected AbstractTransportAdapter(Transport wrappedTransport)
        {
            WrappedTransport = wrappedTransport;
        }

        /// <summary>TBD</summary>
        protected Transport WrappedTransport;

        /// <summary>TBD</summary>
        protected abstract SchemeAugmenter SchemeAugmenter { get; }

        /// <summary>TBD</summary>
        public override string SchemeIdentifier => SchemeAugmenter.AugmentScheme(WrappedTransport.SchemeIdentifier);

        /// <summary>TBD</summary>
        public override long MaximumPayloadBytes => WrappedTransport.MaximumPayloadBytes;

        /// <summary>TBD</summary>
        /// <param name="listenAddress">TBD</param>
        /// <param name="listenerTask">TBD</param>
        /// <returns>TBD</returns>
        protected abstract Task<IAssociationEventListener> InterceptListen(Address listenAddress, Task<IAssociationEventListener> listenerTask);

        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusPromise">TBD</param>
        protected abstract void InterceptAssociate(Address remoteAddress, TaskCompletionSource<AssociationHandle> statusPromise);

        /// <summary>TBD</summary>
        /// <param name="remote">TBD</param>
        /// <returns>TBD</returns>
        public override bool IsResponsibleFor(Address remote) => WrappedTransport.IsResponsibleFor(remote);

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override Task<(Address, TaskCompletionSource<IAssociationEventListener>)> Listen()
        {
            var upstreamListenerPromise = new TaskCompletionSource<IAssociationEventListener>();
            return WrappedTransport.Listen().Then(AfterListenFunc, this, upstreamListenerPromise, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static readonly
            Func<(Address, TaskCompletionSource<IAssociationEventListener>), AbstractTransportAdapter, TaskCompletionSource<IAssociationEventListener>, Task<(Address, TaskCompletionSource<IAssociationEventListener>)>>
            AfterListenFunc = AfterListen;
        private static async Task<(Address, TaskCompletionSource<IAssociationEventListener>)> AfterListen(
            (Address, TaskCompletionSource<IAssociationEventListener>) result,
            AbstractTransportAdapter owner, TaskCompletionSource<IAssociationEventListener> upstreamListenerPromise)
        {
            var listenAddress = result.Item1;
            var listenerPromise = result.Item2;
            listenerPromise.TrySetResult(await owner.InterceptListen(listenAddress, upstreamListenerPromise.Task).ConfigureAwait(false));
            return (owner.SchemeAugmenter.AugmentScheme(listenAddress), upstreamListenerPromise);
        }

        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <returns>TBD</returns>
        public override Task<AssociationHandle> Associate(Address remoteAddress)
        {
            var statusPromise = new TaskCompletionSource<AssociationHandle>();
            InterceptAssociate(SchemeAugmenter.RemoveScheme(remoteAddress), statusPromise);
            return statusPromise.Task;
        }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override Task<bool> Shutdown() => WrappedTransport.Shutdown();
    }

    #endregion

    #region == class AbstractTransportAdapterHandle ==

    /// <summary>TBD</summary>
    internal abstract class AbstractTransportAdapterHandle : AssociationHandle
    {
        /// <summary>TBD</summary>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="addedSchemeIdentifier">TBD</param>
        protected AbstractTransportAdapterHandle(AssociationHandle wrappedHandle, string addedSchemeIdentifier)
            : this(wrappedHandle.LocalAddress, wrappedHandle.RemoteAddress, wrappedHandle, addedSchemeIdentifier) { }

        /// <summary>TBD</summary>
        /// <param name="originalLocalAddress">TBD</param>
        /// <param name="originalRemoteAddress">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="addedSchemeIdentifier">TBD</param>
        protected AbstractTransportAdapterHandle(Address originalLocalAddress, Address originalRemoteAddress, AssociationHandle wrappedHandle, string addedSchemeIdentifier) : base(originalLocalAddress, originalRemoteAddress)
        {
            WrappedHandle = wrappedHandle;
            OriginalRemoteAddress = originalRemoteAddress;
            OriginalLocalAddress = originalLocalAddress;
            SchemeAugmenter = new SchemeAugmenter(addedSchemeIdentifier);
            RemoteAddress = SchemeAugmenter.AugmentScheme(OriginalRemoteAddress);
            LocalAddress = SchemeAugmenter.AugmentScheme(OriginalLocalAddress);
        }

        /// <summary>TBD</summary>
        public Address OriginalLocalAddress { get; private set; }

        /// <summary>TBD</summary>
        public Address OriginalRemoteAddress { get; private set; }

        /// <summary>TBD</summary>
        public AssociationHandle WrappedHandle { get; private set; }

        /// <summary>TBD</summary>
        protected SchemeAugmenter SchemeAugmenter { get; private set; }

        /// <summary>TBD</summary>
        /// <param name="other">TBD</param>
        /// <returns>TBD</returns>
        protected bool Equals(AbstractTransportAdapterHandle other)
            => Equals(OriginalLocalAddress, other.OriginalLocalAddress) && Equals(OriginalRemoteAddress, other.OriginalRemoteAddress) && Equals(WrappedHandle, other.WrappedHandle);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AbstractTransportAdapterHandle handle && Equals(handle);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode() + (OriginalLocalAddress is object ? OriginalLocalAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OriginalRemoteAddress is object ? OriginalRemoteAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (WrappedHandle is object ? WrappedHandle.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    #endregion


    #region == TransportOperation ==

    /// <summary>Marker interface for all transport operations</summary>
    internal abstract class TransportOperation : INoSerializationVerificationNeeded
    {
        /// <summary>TBD</summary>
        public static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(5);
    }

    #endregion

    #region == class ListenerRegistered ==

    /// <summary>TBD</summary>
    internal sealed class ListenerRegistered : TransportOperation
    {
        /// <summary>TBD</summary>
        /// <param name="listener">TBD</param>
        public ListenerRegistered(IAssociationEventListener listener) => Listener = listener;

        /// <summary>TBD</summary>
        public IAssociationEventListener Listener { get; }
    }

    #endregion

    #region == class AssociateUnderlying ==

    /// <summary>TBD</summary>
    internal sealed class AssociateUnderlying : TransportOperation
    {
        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusPromise">TBD</param>
        public AssociateUnderlying(Address remoteAddress, TaskCompletionSource<AssociationHandle> statusPromise)
        {
            RemoteAddress = remoteAddress;
            StatusPromise = statusPromise;
        }

        /// <summary>TBD</summary>
        public Address RemoteAddress { get; }

        /// <summary>TBD</summary>
        public TaskCompletionSource<AssociationHandle> StatusPromise { get; }
    }

    #endregion

    #region == class ListenUnderlying ==

    /// <summary>TBD</summary>
    internal sealed class ListenUnderlying : TransportOperation
    {
        /// <summary>TBD</summary>
        /// <param name="listenAddress">TBD</param>
        /// <param name="upstreamListener">TBD</param>
        public ListenUnderlying(Address listenAddress, Task<IAssociationEventListener> upstreamListener)
        {
            UpstreamListener = upstreamListener;
            ListenAddress = listenAddress;
        }

        /// <summary>TBD</summary>
        public Address ListenAddress { get; }

        /// <summary>TBD</summary>
        public Task<IAssociationEventListener> UpstreamListener { get; }
    }

    #endregion

    #region == class DisassociateUnderlying ==

    /// <summary>TBD</summary>
    internal sealed class DisassociateUnderlying : TransportOperation, IDeadLetterSuppression
    {
        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        public DisassociateUnderlying(DisassociateInfo info = DisassociateInfo.Unknown) => Info = info;

        /// <summary>TBD</summary>
        public DisassociateInfo Info { get; }
    }

    #endregion


    #region -- class ActorTransportAdapter --

    /// <summary>Actor-based transport adapter</summary>
    public abstract class ActorTransportAdapter : AbstractTransportAdapter
    {
        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        /// <param name="system">TBD</param>
        protected ActorTransportAdapter(Transport wrappedTransport, ActorSystem system)
            : base(wrappedTransport) => System = system;

        /// <summary>TBD</summary>
        protected abstract string ManagerName { get; }

        /// <summary>TBD</summary>
        protected abstract Props ManagerProps { get; }

        /// <summary>TBD</summary>
        public static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(5);

        /// <summary>TBD</summary>
        protected volatile IActorRef manager;

        private Task<IActorRef> RegisterManager()
            => System.ActorSelection("/system/transports").Ask<IActorRef>(new RegisterTransportActor(ManagerProps, ManagerName));

        /// <inheritdoc/>
        protected override Task<IAssociationEventListener> InterceptListen(Address listenAddress, Task<IAssociationEventListener> listenerTask)
        {
            return RegisterManager().Then(AfterRegisterManagerFunc, this, listenAddress, listenerTask, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static readonly Func<IActorRef, ActorTransportAdapter, Address, Task<IAssociationEventListener>, IAssociationEventListener> AfterRegisterManagerFunc =
            (m, o, la, t) => AfterRegisterManager(m, o, la, t);
        private static IAssociationEventListener AfterRegisterManager(IActorRef manager, ActorTransportAdapter owner, Address listenAddress, Task<IAssociationEventListener> listenerTask)
        {
            Interlocked.Exchange(ref owner.manager, manager);
            manager.Tell(new ListenUnderlying(listenAddress, listenerTask));
            return (IAssociationEventListener)new ActorAssociationEventListener(manager);
        }

        /// <inheritdoc/>
        protected override void InterceptAssociate(Address remoteAddress, TaskCompletionSource<AssociationHandle> statusPromise)
            => manager.Tell(new AssociateUnderlying(remoteAddress, statusPromise));

        /// <inheritdoc/>
        public override Task<bool> Shutdown()
        {
            var stopTask = manager.GracefulStop((RARP.For(System).Provider).RemoteSettings.FlushWait);
            var transportStopTask = WrappedTransport.Shutdown();
            return Task.WhenAll(stopTask, transportStopTask).ContinueWith(IsShutdownSuccessFunc, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static readonly Func<Task<bool[]>, bool> IsShutdownSuccessFunc = t => IsShutdownSuccess(t);
        private static bool IsShutdownSuccess(Task<bool[]> x) => x.IsSuccessfully();
    }

    #endregion

    #region == class ActorTransportAdapterManager ==

    /// <summary>TBD</summary>
    internal abstract class ActorTransportAdapterManager : UntypedActor
    {
        /// <summary>Lightweight Stash implementation</summary>
        protected QueueX<object> DelayedEvents = new QueueX<object>();

        /// <summary>TBD</summary>
        protected IAssociationEventListener AssociationListener;

        /// <summary>TBD</summary>
        protected Address LocalAddress;

        /// <summary>TBD</summary>
        protected long UniqueId = 0L;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        protected long NextId() => Interlocked.Increment(ref UniqueId);

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        protected override void OnReceive(object message)
        {
            var self = Self;
            switch (message)
            {
                case ListenUnderlying listen:
                    LocalAddress = listen.ListenAddress;
                    listen.UpstreamListener.Then(
                        AfterUpstreamListenerRegisteredAction, self,
                        TaskContinuationOptions.ExecuteSynchronously);
                    break;

                case ListenerRegistered listener:
                    AssociationListener = listener.Listener;
                    foreach (var dEvent in DelayedEvents)
                    {
                        self.Tell(dEvent, ActorRefs.NoSender);
                    }
                    DelayedEvents.Clear();
                    Context.Become(Ready);
                    break;

                default:
                    DelayedEvents.Enqueue(message);
                    break;
            }
        }

        private static readonly Action<IAssociationEventListener, IActorRef> AfterUpstreamListenerRegisteredAction = (l, c) => AfterUpstreamListenerRegistered(l, c);
        private static void AfterUpstreamListenerRegistered(IAssociationEventListener listener, IActorRef capturedSelf)
        {
            capturedSelf.Tell(new ListenerRegistered(listener));
        }

        /// <summary>Method to be implemented for child classes - processes messages once the transport is
        /// ready to send / receive.</summary>
        /// <param name="message">TBD</param>
        protected abstract void Ready(object message);
    }

    #endregion
}