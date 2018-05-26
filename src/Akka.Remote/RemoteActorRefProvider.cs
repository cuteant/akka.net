//-----------------------------------------------------------------------
// <copyright file="RemoteActorRefProvider.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Annotations;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Remote.Configuration;
using Akka.Remote.Serialization;
using Akka.Util.Internal;

namespace Akka.Remote
{
    #region -- interface IRemoteActorRefProvider --

    /// <summary>INTERNAL API</summary>
    [InternalApi]
    public interface IRemoteActorRefProvider : IActorRefProvider
    {
        /// <summary>Remoting system daemon responsible for powering remote deployment capabilities.</summary>
        IInternalActorRef RemoteDaemon { get; }

        /// <summary>The remote death watcher.</summary>
        IActorRef RemoteWatcher { get; }

        /// <summary>The remote transport. Wraps all of the underlying physical network transports.</summary>
        RemoteTransport Transport { get; }

        /// <summary>The remoting settings</summary>
        RemoteSettings RemoteSettings { get; }

        /// <summary>Looks up local overrides for remote deployments</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        Deploy LookUpRemotes(IEnumerable<string> p);

        /// <summary>Determines if a particular network address is assigned to any of this <see
        /// cref="ActorSystem"/>'s transports.</summary>
        /// <param name="address">The address to check.</param>
        /// <returns><c>true</c> if the address is assigned to any bound transports; false otherwise.</returns>
        bool HasAddress(Address address);

        /// <summary>INTERNAL API.
        ///
        /// Called in deserialization of incoming remote messages where the correct local address is known.</summary>
        /// <param name="path">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <returns>TBD</returns>
        IInternalActorRef ResolveActorRefWithLocalAddress(string path, Address localAddress);

        /// <summary>INTERNAL API: this is used by the <see cref="ActorRefResolveCache"/> via the public <see
        /// cref="IActorRefProvider.ResolveActorRef(string)"/> method.</summary>
        /// <param name="path">The path of the actor we intend to resolve.</param>
        /// <returns>An <see cref="IActorRef"/> if a match was found. Otherwise nobody.</returns>
        IActorRef InternalResolveActorRef(string path);

        /// <summary>TBD</summary>
        /// <param name="actor">TBD</param>
        /// <param name="props">TBD</param>
        /// <param name="deploy">TBD</param>
        /// <param name="supervisor">TBD</param>
        void UseActorOnNode(RemoteActorRef actor, Props props, Deploy deploy, IInternalActorRef supervisor);

        /// <summary>Marks a remote system as out of sync and prevents reconnects until the quarantine timeout elapses.</summary>
        /// <param name="address">Address of the remote system to be quarantined</param>
        /// <param name="uid">
        /// UID of the remote system, if the uid is not defined it will not be a strong quarantine
        /// but the current endpoint writer will be stopped (dropping system messages) and the
        /// address will be gated
        /// </param>
        void Quarantine(Address address, int? uid);
    }

    #endregion

    /// <summary>INTERNAL API</summary>
    [InternalApi]
    public class RemoteActorRefProvider : IRemoteActorRefProvider
    {
        #region @@ Constructors @@

        /// <summary>Creates a new remote actor ref provider instance.</summary>
        /// <param name="systemName">Name of the actor system.</param>
        /// <param name="settings">The actor system settings.</param>
        /// <param name="eventStream">The <see cref="EventStream"/> instance used by this system.</param>
        public RemoteActorRefProvider(string systemName, Settings settings, EventStream eventStream)
        {
            settings.InjectTopLevelFallback(RemoteConfigFactory.Default());

            var remoteDeployer = new RemoteDeployer(settings);
            IInternalActorRef DeadLettersFactory(ActorPath path) => new RemoteDeadLetterActorRef(this, path, eventStream);
            _local = new LocalActorRefProvider(systemName, settings, eventStream, remoteDeployer, DeadLettersFactory);
            RemoteSettings = new RemoteSettings(settings.Config);
            Deployer = remoteDeployer;
            _log = _local.Log;
        }

        #endregion

        #region @@ Properties @@

        private readonly ILoggingAdapter _log;

        private readonly LocalActorRefProvider _local;
        private volatile Internals _internals;
        private ActorSystemImpl _system;

        private Internals RemoteInternals => _internals;

        private Internals CreateInternals()
        {
            var internals =
                new Internals(new Remoting(_system, this), _system.Serialization,
                    new RemoteSystemDaemon(_system, RootPath / "remote", RootGuardian, _remotingTerminator, _log));
            _local.RegisterExtraName("remote", internals.RemoteDaemon);
            return internals;
        }

        /// <summary>Remoting system daemon responsible for powering remote deployment capabilities.</summary>
        public IInternalActorRef RemoteDaemon => RemoteInternals.RemoteDaemon;

        /// <summary>The remote transport. Wraps all of the underlying physical network transports.</summary>
        public RemoteTransport Transport => RemoteInternals.Transport;

        /// <summary>The remoting settings</summary>
        public RemoteSettings RemoteSettings { get; }

        /* these are only available after Init() is called */

        /// <inheritdoc/>
        public ActorPath RootPath => _local.RootPath;

        /// <inheritdoc/>
        public IInternalActorRef RootGuardian => _local.RootGuardian;

        /// <inheritdoc/>
        public LocalActorRef Guardian => _local.Guardian;

        /// <inheritdoc/>
        public LocalActorRef SystemGuardian => _local.SystemGuardian;

        /// <inheritdoc/>
        public IInternalActorRef TempContainer => _local.TempContainer;

        /// <inheritdoc/>
        public IActorRef DeadLetters => _local.DeadLetters;

        /// <inheritdoc/>
        public Deployer Deployer { get; protected set; }

        /// <inheritdoc/>
        public Address DefaultAddress => Transport.DefaultAddress;

        /// <inheritdoc/>
        public Settings Settings => _local.Settings;

        /// <inheritdoc/>
        public Task TerminationTask => _local.TerminationTask;

        private IInternalActorRef InternalDeadLetters => (IInternalActorRef)_local.DeadLetters;

        #endregion

        /// <inheritdoc/>
        public ActorPath TempPath() => _local.TempPath();

        /// <inheritdoc/>
        public void RegisterTempActor(IInternalActorRef actorRef, ActorPath path) => _local.RegisterTempActor(actorRef, path);

        /// <inheritdoc/>
        public void UnregisterTempActor(ActorPath path) => _local.UnregisterTempActor(path);

        private volatile IActorRef _remotingTerminator;
        private volatile IActorRef _remoteWatcher;

        private volatile ActorRefResolveThreadLocalCache _actorRefResolveThreadLocalCache;
        private volatile ActorPathThreadLocalCache _actorPathThreadLocalCache;

        /// <summary>The remote death watcher.</summary>
        public IActorRef RemoteWatcher => _remoteWatcher;

        private volatile IActorRef _remoteDeploymentWatcher;

        #region -- Init --

        /// <inheritdoc/>
        public virtual void Init(ActorSystemImpl system)
        {
            _system = system;

            _local.Init(system);

            _actorRefResolveThreadLocalCache = ActorRefResolveThreadLocalCache.For(system);
            _actorPathThreadLocalCache = ActorPathThreadLocalCache.For(system);

            _remotingTerminator =
                _system.SystemActorOf(
                    RemoteSettings.ConfigureDispatcher(Props.Create(() => new RemotingTerminator(_local.SystemGuardian))),
                    "remoting-terminator");

            _internals = CreateInternals();

            _remotingTerminator.Tell(RemoteInternals);

            Transport.Start();
            _remoteWatcher = CreateRemoteWatcher(system);
            _remoteDeploymentWatcher = CreateRemoteDeploymentWatcher(system);
        }

        #endregion

        #region ++ CreateRemoteWatcher ++

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        protected virtual IActorRef CreateRemoteWatcher(ActorSystemImpl system)
        {
            var failureDetector = CreateRemoteWatcherFailureDetector(system);
            return system.SystemActorOf(RemoteSettings.ConfigureDispatcher(
                Akka.Remote.RemoteWatcher.Props(
                    failureDetector,
                    RemoteSettings.WatchHeartBeatInterval,
                    RemoteSettings.WatchUnreachableReaperInterval,
                    RemoteSettings.WatchHeartbeatExpectedResponseAfter)), "remote-watcher");
        }

        #endregion

        #region ++ CreateRemoteDeploymentWatcher ++

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        protected virtual IActorRef CreateRemoteDeploymentWatcher(ActorSystemImpl system)
        {
            return system.SystemActorOf(RemoteSettings.ConfigureDispatcher(Props.Create<RemoteDeploymentWatcher>()),
                "remote-deployment-watcher");
        }

        #endregion

        #region ++ CreateRemoteWatcherFailureDetector ++

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        protected DefaultFailureDetectorRegistry<Address> CreateRemoteWatcherFailureDetector(ActorSystem system)
        {
            return new DefaultFailureDetectorRegistry<Address>(() =>
                FailureDetectorLoader.Load(RemoteSettings.WatchFailureDetectorImplementationClass,
                RemoteSettings.WatchFailureDetectorConfig, _system));
        }

        #endregion

        #region -- ActorOf --

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <param name="props">TBD</param>
        /// <param name="supervisor">TBD</param>
        /// <param name="path">TBD</param>
        /// <param name="systemService">TBD</param>
        /// <param name="deploy">TBD</param>
        /// <param name="lookupDeploy">TBD</param>
        /// <param name="async">TBD</param>
        /// <exception cref="ActorInitializationException">This exception is thrown when the remote deployment 
        /// to the specified <paramref name="path"/> fails.</exception>
        /// <exception cref="ConfigurationException">This exception is thrown when either the scope of the deployment is local or the
        /// specified <paramref name="props"/> is invalid for deployment to the specified <paramref name="path"/>.</exception>
        /// <returns>TBD</returns>
        public IInternalActorRef ActorOf(ActorSystemImpl system, Props props, IInternalActorRef supervisor, ActorPath path, bool systemService, Deploy deploy, bool lookupDeploy, bool async)
        {
            if (systemService) { return LocalActorOf(system, props, supervisor, path, true, deploy, lookupDeploy, async); }

            /*
            * This needs to deal with "mangled" paths, which are created by remote
            * deployment, also in this method. The scheme is the following:
            *
            * Whenever a remote deployment is found, create a path on that remote
            * address below "remote", including the current system’s identification
            * as "sys@host:port" (typically; it will use whatever the remote
            * transport uses). This means that on a path up an actor tree each node
            * change introduces one layer or "remote/scheme/sys@host:port/" within the URI.
            *
            * Example:
            *
            * akka.tcp://sys@home:1234/remote/akka/sys@remote:6667/remote/akka/sys@other:3333/user/a/b/c
            *
            * means that the logical parent originates from "akka.tcp://sys@other:3333" with
            * one child (may be "a" or "b") being deployed on "akka.tcp://sys@remote:6667" and
            * finally either "b" or "c" being created on "akka.tcp://sys@home:1234", where
            * this whole thing actually resides. Thus, the logical path is
            * "/user/a/b/c" and the physical path contains all remote placement
            * information.
            *
            * Deployments are always looked up using the logical path, which is the
            * purpose of the lookupRemotes internal method.
            */

            var elements = path.Elements;
            Deploy configDeploy = null;
            if (lookupDeploy)
            {
                if (string.Equals(elements.Head(), "user", StringComparison.Ordinal))
                {
                    configDeploy = Deployer.Lookup(elements.Drop(1));
                }
                else if (string.Equals(elements.Head(), "remote", StringComparison.Ordinal))
                {
                    configDeploy = LookUpRemotes(elements);
                }
            }

            //merge all of the fallbacks together
            var deployment = new List<Deploy>() { deploy, configDeploy }.Where(x => x != null).Aggregate(Deploy.None, (deploy1, deploy2) => deploy2.WithFallback(deploy1));
            var propsDeploy = new List<Deploy>() { props.Deploy, deployment }.Where(x => x != null)
                .Aggregate(Deploy.None, (deploy1, deploy2) => deploy2.WithFallback(deploy1));

            //match for remote scope
            if (propsDeploy.Scope is RemoteScope)
            {
                var addr = propsDeploy.Scope.AsInstanceOf<RemoteScope>().Address;

                //Even if this actor is in RemoteScope, it might still be a local address
                if (HasAddress(addr))
                {
                    return LocalActorOf(system, props, supervisor, path, false, deployment, false, async);
                }

                //check for correct scope configuration
                if (props.Deploy.Scope is LocalScope)
                {
                    throw new ConfigurationException($"configuration requested remote deployment for local-only Props at {path}");
                }

                try
                {
                    try
                    {
                        // for consistency we check configuration of dispatcher and mailbox locally
                        var dispatcher = _system.Dispatchers.Lookup(props.Dispatcher);
                        var mailboxType = _system.Mailboxes.GetMailboxType(props, dispatcher.Configurator.Config);
                    }
                    catch (Exception ex)
                    {
                        throw new ConfigurationException(
                            $"Configuration problem while creating {path} with dispatcher [{props.Dispatcher}] and mailbox [{props.Mailbox}]", ex);
                    }
                    var localAddress = Transport.LocalAddressForRemote(addr);
                    var rpath = (new RootActorPath(addr) / "remote" / localAddress.Protocol / localAddress.HostPort() /
                                 path.Elements.ToArray()).
                        WithUid(path.Uid);
                    var remoteRef = new RemoteActorRef(Transport, localAddress, rpath, supervisor, props, deployment);
                    return remoteRef;
                }
                catch (Exception ex)
                {
                    throw new ActorInitializationException($"Remote deployment failed for [{path}]", ex);
                }
            }
            else
            {
                return LocalActorOf(system, props, supervisor, path, false, deployment, false, async);
            }
        }

        #endregion

        #region -- LookUpRemotes --

        /// <summary>Looks up local overrides for remote deployments</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Deploy LookUpRemotes(IEnumerable<string> p)
        {
            if (p == null || !p.Any()) { return Deploy.None; }
            if (string.Equals(p.Head(), "remote", StringComparison.Ordinal)) { return LookUpRemotes(p.Drop(3)); }
            if (string.Equals(p.Head(), "user", StringComparison.Ordinal)) { return Deployer.Lookup(p.Drop(1)); }
            return Deploy.None;
        }

        #endregion

        #region -- HasAddress --

        public bool HasAddress(Address address)
            => address == _local.RootPath.Address || address == RootPath.Address || Transport.Addresses.Any(a => a == address);

        #endregion

        #region - RootGuardianAt --

        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef RootGuardianAt(Address address)
        {
            if (HasAddress(address)) { return RootGuardian; }
            return CreateRemoteRef(new RootActorPath(address), Transport.LocalAddressForRemote(address));
        }

        #endregion

        #region ** LocalActorOf **

        private IInternalActorRef LocalActorOf(ActorSystemImpl system, Props props, IInternalActorRef supervisor,
            ActorPath path, bool systemService, Deploy deploy, bool lookupDeploy, bool async)
        {
            return _local.ActorOf(system, props, supervisor, path, systemService, deploy, lookupDeploy, async);
        }

        #endregion

        #region ** TryParseCachedPath **

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryParseCachedPath(string actorPath, out ActorPath path)
        {
            if (_actorPathThreadLocalCache != null)
            {
                path = _actorPathThreadLocalCache.Cache.GetOrCompute(actorPath);
                return path != null;
            }
            else // cache not initialized yet
            {
                return ActorPath.TryParse(actorPath, out path);
            }
        }

        #endregion

        #region -- ResolveActorRefWithLocalAddress --

        /// <summary>INTERNAL API.
        ///
        /// Called in deserialization of incoming remote messages where the correct local address is known.
        /// </summary>
        /// <param name="path">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <returns>TBD</returns>
        public IInternalActorRef ResolveActorRefWithLocalAddress(string path, Address localAddress)
        {
            if (TryParseCachedPath(path, out var actorPath))
            {
                //the actor's local address was already included in the ActorPath
                if (HasAddress(actorPath.Address))
                {
                    // HACK: needed to make ActorSelections work
                    if (string.Equals(actorPath.ToStringWithoutAddress(), "/", StringComparison.Ordinal))
                        return RootGuardian;
                    return _local.ResolveActorRef(RootGuardian, actorPath.ElementsWithUid);
                }

                return CreateRemoteRef(new RootActorPath(actorPath.Address) / actorPath.ElementsWithUid, localAddress);
            }
            if (_log.IsDebugEnabled) _log.Debug("resolve of unknown path [{0}] failed", path);
            return InternalDeadLetters;
        }

        #endregion

        #region ++ CreateRemoteRef ++

        /// <summary>Used to create <see cref="RemoteActorRef"/> instances upon deserialiation inside the
        /// Akka.Remote pipeline.</summary>
        /// <param name="actorPath">
        /// The remote path of the actor on its physical location on the network.
        /// </param>
        /// <param name="localAddress">The local path of the actor.</param>
        /// <returns>An <see cref="IInternalActorRef"/> instance.</returns>
        protected virtual IInternalActorRef CreateRemoteRef(ActorPath actorPath, Address localAddress)
            => new RemoteActorRef(Transport, localAddress, actorPath, ActorRefs.Nobody, Props.None, Deploy.None);

        #endregion

        #region -- ResolveActorRef --

        /// <summary>Resolves a deserialized path into an <see cref="IActorRef"/></summary>
        /// <param name="path">The path of the actor we are attempting to resolve.</param>
        /// <returns>A local <see cref="IActorRef"/> if it exists, <see cref="ActorRefs.Nobody"/> otherwise.</returns>
        public IActorRef ResolveActorRef(string path)
        {
            // using thread local LRU cache, which will call InternalRresolveActorRef if the value is
            // not cached
            if (_actorRefResolveThreadLocalCache == null)
            {
                return InternalResolveActorRef(path); // cache not initialized yet
            }
            return _actorRefResolveThreadLocalCache.Cache.GetOrCompute(path);
        }

        #endregion

        #region -- InternalResolveActorRef --

        /// <summary>INTERNAL API: this is used by the <see cref="ActorRefResolveCache"/> via the public <see
        /// cref="ResolveActorRef(string)"/> method.</summary>
        /// <param name="path">The path of the actor we intend to resolve.</param>
        /// <returns>An <see cref="IActorRef"/> if a match was found. Otherwise nobody.</returns>
        public IActorRef InternalResolveActorRef(string path)
        {
            if (path == String.Empty) { return ActorRefs.NoSender; }

            if (ActorPath.TryParse(path, out ActorPath actorPath)) { return ResolveActorRef(actorPath); }

            if (_log.IsDebugEnabled) _log.Debug("resolve of unknown path [{0}] failed", path);
            return DeadLetters;
        }

        #endregion

        #region -- ResolveActorRef --

        /// <summary>TBD</summary>
        /// <param name="actorPath">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef ResolveActorRef(ActorPath actorPath)
        {
            if (HasAddress(actorPath.Address))
            {
                return _local.ResolveActorRef(RootGuardian, actorPath.ElementsWithUid);
            }
            try
            {
                return CreateRemoteRef(actorPath, Transport.LocalAddressForRemote(actorPath.Address));
            }
            catch (Exception ex)
            {
                _log.Warning("Error while resolving address [{0}] due to [{1}]", actorPath.Address, ex.Message);
                return new EmptyLocalActorRef(this, RootPath, _local.EventStream);
            }
        }

        #endregion

        #region -- GetExternalAddressFor --

        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public Address GetExternalAddressFor(Address address)
        {
            if (HasAddress(address)) { return _local.RootPath.Address; }
            if (!string.IsNullOrEmpty(address.Host) && address.Port.HasValue)
            {
                try
                {
                    return Transport.LocalAddressForRemote(address);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        #endregion

        #region -- UseActorOnNode --

        /// <summary>TBD</summary>
        /// <param name="actor">TBD</param>
        /// <param name="props">TBD</param>
        /// <param name="deploy">TBD</param>
        /// <param name="supervisor">TBD</param>
        public void UseActorOnNode(RemoteActorRef actor, Props props, Deploy deploy, IInternalActorRef supervisor)
        {
            if (_log.IsDebugEnabled) _log.Debug("[{0}] Instantiating Remote Actor [{1}]", RootPath, actor.Path);
            IActorRef remoteNode = ResolveActorRef(new RootActorPath(actor.Path.Address) / "remote");
            remoteNode.Tell(new DaemonMsgCreate(props, deploy, actor.Path.ToSerializationFormat(), supervisor));
            _remoteDeploymentWatcher.Tell(new RemoteDeploymentWatcher.WatchRemote(actor, supervisor));
        }

        #endregion

        #region -- Quarantine --

        /// <summary>Marks a remote system as out of sync and prevents reconnects until the quarantine timeout elapses.</summary>
        /// <param name="address">Address of the remote system to be quarantined</param>
        /// <param name="uid">UID of the remote system, if the uid is not defined it will not be a strong quarantine
        /// but the current endpoint writer will be stopped (dropping system messages) and the
        /// address will be gated.</param>
        public void Quarantine(Address address, int? uid)
        {
            Transport.Quarantine(address, uid);
        }

        #endregion

        #region ** class Internals **

        /// <summary>All of the private internals used by <see cref="RemoteActorRefProvider"/>, namely its
        /// transport registry, remote serializers, and the <see cref="RemoteDaemon"/> instance.</summary>
        private class Internals : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="transport">TBD</param>
            /// <param name="serialization">TBD</param>
            /// <param name="remoteDaemon">TBD</param>
            public Internals(RemoteTransport transport, Akka.Serialization.Serialization serialization, IInternalActorRef remoteDaemon)
            {
                Transport = transport;
                Serialization = serialization;
                RemoteDaemon = remoteDaemon;
            }

            /// <summary>TBD</summary>
            public RemoteTransport Transport { get; }

            /// <summary>TBD</summary>
            public Akka.Serialization.Serialization Serialization { get; }

            /// <summary>TBD</summary>
            public IInternalActorRef RemoteDaemon { get; }
        }

        #endregion

        #region ** RemotingTerminator **

        /// <summary>Describes the FSM states of the <see cref="RemotingTerminator"/></summary>
        private enum TerminatorState
        {
            /// <summary>TBD</summary>
            Uninitialized,

            /// <summary>TBD</summary>
            Idle,

            /// <summary>TBD</summary>
            WaitDaemonShutdown,

            /// <summary>TBD</summary>
            WaitTransportShutdown,

            /// <summary>TBD</summary>
            Finished
        }

        /// <summary>Responsible for shutting down the <see cref="RemoteDaemon"/> and all transports when the
        /// <see cref="ActorSystem"/> is being shutdown.</summary>
        private sealed class RemotingTerminator : FSM<TerminatorState, Internals>, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
        {
            private readonly IActorRef _systemGuardian;
            private readonly ILoggingAdapter _log;

            public RemotingTerminator(IActorRef systemGuardian)
            {
                _systemGuardian = systemGuardian;
                _log = Context.GetLogger();
                InitFSM();
            }

            private void InitFSM()
            {
                When(TerminatorState.Uninitialized, @event =>
                {
                    if (@event.FsmEvent is Internals internals)
                    {
                        _systemGuardian.Tell(RegisterTerminationHook.Instance);
                        return GoTo(TerminatorState.Idle).Using(internals);
                    }
                    return null;
                });

                When(TerminatorState.Idle, @event =>
                {
                    if (@event.StateData != null && @event.FsmEvent is TerminationHook)
                    {
                        if (_log.IsInfoEnabled) _log.Info("Shutting down remote daemon.");
                        @event.StateData.RemoteDaemon.Tell(TerminationHook.Instance);
                        return GoTo(TerminatorState.WaitDaemonShutdown);
                    }
                    return null;
                });

                // TODO: state timeout
                When(TerminatorState.WaitDaemonShutdown, @event =>
                {
                    if (@event.StateData != null && @event.FsmEvent is TerminationHookDone)
                    {
                        if (_log.IsInfoEnabled) _log.Info("Remote daemon shut down; proceeding with flushing remote transports.");
                        @event.StateData.Transport.Shutdown()
                            .ContinueWith(t => TransportShutdown.Instance,
                                TaskContinuationOptions.ExecuteSynchronously)
                            .PipeTo(Self);
                        return GoTo(TerminatorState.WaitTransportShutdown);
                    }

                    return null;
                });

                When(TerminatorState.WaitTransportShutdown, @event =>
                {
                    if (_log.IsInfoEnabled) _log.Info("Remoting shut down.");
                    _systemGuardian.Tell(TerminationHookDone.Instance);
                    return Stop();
                });

                StartWith(TerminatorState.Uninitialized, null);
            }

            public sealed class TransportShutdown : ISingletonMessage
            {
                private TransportShutdown() { }

                public static readonly TransportShutdown Instance = new TransportShutdown();

                public override string ToString() => "<TransportShutdown>";
            }
        }

        #endregion

        #region ** class RemoteDeadLetterActorRef **

        private sealed class RemoteDeadLetterActorRef : DeadLetterActorRef
        {
            public RemoteDeadLetterActorRef(IActorRefProvider provider, ActorPath actorPath, EventStream eventStream)
                : base(provider, actorPath, eventStream) { }

            protected override void TellInternal(object message, IActorRef sender)
            {
                if (message is EndpointManager.Send send)
                {
                    if (send.Seq == null)
                    {
                        base.TellInternal(send.Message, send.SenderOption ?? ActorRefs.NoSender);
                    }
                }
                else if (message is DeadLetter deadLetter && deadLetter.Message is EndpointManager.Send deadSend)
                {
                    if (deadSend.Seq == null)
                    {
                        base.TellInternal(deadSend.Message, deadSend.SenderOption ?? ActorRefs.NoSender);
                    }
                }
                else
                {
                    base.TellInternal(message, sender);
                }
            }
        }

        #endregion
    }
}