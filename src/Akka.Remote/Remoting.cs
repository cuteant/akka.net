//-----------------------------------------------------------------------
// <copyright file="Remoting.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Remote.Transport;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.AsyncEx;

namespace Akka.Remote
{
    #region == class AddressUrlEncoder ==

    /// <summary>INTERNAL API</summary>
    internal static class AddressUrlEncoder
    {
        /// <summary>URL-encodes an actor <see cref="Address"/>. Used when generating the names of some system
        /// remote actors.</summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public static string Encode(Address address) => WebUtility.UrlEncode(address.ToString());
    }

    #endregion

    #region == class RARP ==

    /// <summary>INTERNAL API
    ///
    /// (used for forcing all /system level remoting actors onto a dedicated dispatcher)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [Akka.Annotations.InternalApi]
    public sealed class RARP : ExtensionIdProvider<RARP>, IExtension // public for Akka.Tests.FsCheck
    {
        //this is why this extension is called "RARP"
        private readonly IRemoteActorRefProvider _provider;

        /// <summary>Used as part of the <see cref="ExtensionIdProvider{RARP}"/></summary>
        public RARP() { }

        private RARP(IRemoteActorRefProvider provider) => _provider = provider;

        /// <summary>TBD</summary>
        /// <param name="props">TBD</param>
        /// <returns>TBD</returns>
        public Props ConfigureDispatcher(Props props) => _provider.RemoteSettings.ConfigureDispatcher(props);

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public override RARP CreateExtension(ExtendedActorSystem system) => new RARP((IRemoteActorRefProvider)system.Provider);

        /// <summary>The underlying remote actor reference provider.</summary>
        public IRemoteActorRefProvider Provider => _provider;

        #region - Static methods -

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public static RARP For(ActorSystem system) => system.WithExtension<RARP, RARP>();

        #endregion
    }

    #endregion

    #region == interface IPriorityMessage ==

    /// <summary>
    /// INTERNAL API Messages marked with this interface will be sent before other messages when
    /// buffering is active. This means that these messages don't obey normal message ordering. It is
    /// used for failure detector heartbeat messages.
    /// </summary>
    internal interface IPriorityMessage { }

    #endregion

    #region == class Remoting ==

    /// <summary>INTERNAL API</summary>
    internal sealed class Remoting : RemoteTransport
    {
        private readonly ILoggingAdapter _log;
        private volatile IDictionary<string, HashSet<ProtocolTransportAddressPair>> _transportMapping;
        private volatile IActorRef _endpointManager;

        // This is effectively a write-once variable similar to a lazy val. The reason for not using
        // a lazy val is exception handling.
        private volatile HashSet<Address> _addresses;

        // This variable has the same semantics as the addresses variable, in the sense it is written
        // once, and emulates a lazy val
        private volatile Address _defaultAddress;

        private readonly IActorRef _transportSupervisor;
        private readonly EventPublisher _eventPublisher;

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <param name="provider">TBD</param>
        public Remoting(ExtendedActorSystem system, RemoteActorRefProvider provider)
            : base(system, provider)
        {
            _log = Logging.GetLogger(system, "remoting");
            _eventPublisher = new EventPublisher(system, _log, Logging.LogLevelFor(provider.RemoteSettings.RemoteLifecycleEventsLogLevel));
            _transportSupervisor = system.SystemActorOf(Props.Create<TransportSupervisor>(), "transports");
        }

        #region - RemoteTransport overrides -

        /// <summary>TBD</summary>
        public override ISet<Address> Addresses => _addresses;

        /// <summary>TBD</summary>
        public override Address DefaultAddress => _defaultAddress;

        #region - Start -

        /// <summary>Start assumes that it cannot be followed by another Start() without having a Shutdown() first.</summary>
        /// <exception cref="ConfigurationException">This exception is thrown when no transports are enabled under the
        /// "akka.remote.enabled-transports" configuration setting.</exception>
        /// <exception cref="TaskCanceledException">This exception is thrown when startup is canceled due to a timeout.</exception>
        /// <exception cref="TimeoutException">This exception is thrown when startup times out.</exception>
        /// <exception cref="Exception">This exception is thrown when a general error occurs during startup.</exception>
        public override void Start()
        {
            if (_endpointManager is null)
            {
                if (_log.IsInfoEnabled) _log.StartingRemoting();
                Interlocked.Exchange(ref _endpointManager, System.SystemActorOf(RARP.For(System).ConfigureDispatcher(
                    Props.Create(() => new EndpointManager(System.Settings.Config, _log)).WithDeploy(Deploy.Local)),
                    EndpointManagerName));

                try
                {
                    var addressPromise = new TaskCompletionSource<IList<ProtocolTransportAddressPair>>();

                    // tells the EndpointManager to start all transports and bind them to listenable
                    // addresses, and then set the results of this promise to include them.
                    _endpointManager.Tell(new EndpointManager.Listen(addressPromise));

                    addressPromise.Task.Wait(Provider.RemoteSettings.StartupTimeout);
                    var akkaProtocolTransports = addressPromise.Task.Result;
                    if (akkaProtocolTransports.Count == 0)
                    {
                        ThrowHelper.ThrowConfigurationException();
                    }
                    //_addresses = new HashSet<Address>(akkaProtocolTransports.Select(a => a.Address), AddressComparer.Instance);

                    var tmp = akkaProtocolTransports.GroupBy(t => t.ProtocolTransport.SchemeIdentifier);
                    Interlocked.Exchange(ref _transportMapping, new Dictionary<string, HashSet<ProtocolTransportAddressPair>>(StringComparer.Ordinal));
                    foreach (var g in tmp)
                    {
                        var set = new HashSet<ProtocolTransportAddressPair>(g);
                        _transportMapping.Add(g.Key, set);
                    }

                    Interlocked.Exchange(ref _defaultAddress, akkaProtocolTransports.Head().Address);
                    Interlocked.Exchange(ref _addresses, new HashSet<Address>(akkaProtocolTransports.Select(x => x.Address), AddressComparer.Instance));

                    if (_log.IsInfoEnabled)
                    {
                        _log.RemotingStartedListeningOnAddresses(_addresses);
                    }

                    _endpointManager.Tell(new EndpointManager.StartupFinished());
                    _eventPublisher.NotifyListeners(new RemotingListenEvent(_addresses.ToList()));
                }
                catch (TaskCanceledException ex)
                {
                    NotifyError("Startup was canceled due to timeout", ex);
                    throw;
                }
                catch (TimeoutException ex)
                {
                    NotifyError("Startup timed out", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    NotifyError("Startup failed", ex);
                    throw;
                }
            }
            else
            {
                if (_log.IsWarningEnabled) _log.RemotingWasAlreadyStartedIgnoringStartAttempt();
            }
        }

        #endregion

        #region - Shutdown -

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override Task Shutdown()
        {
            if (_endpointManager is null)
            {
                if (_log.IsWarningEnabled) _log.RemotingIsNotRunningIgnoringShutdownAttempt();
                return TaskConstants.BooleanTrue;
            }
            else
            {
                var timeout = Provider.RemoteSettings.ShutdownTimeout;

                return _endpointManager
                    .Ask<bool>(new EndpointManager.ShutdownAndFlush(), timeout)
                    .LinkOutcome(ShutdownContinuationAction, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private static readonly Action<Task<bool>, Remoting> ShutdownContinuationAction = ShutdownContinuation;
        private static void ShutdownContinuation(Task<bool> result, Remoting owner)
        {
            void finalize()
            {
                owner._eventPublisher.NotifyListeners(new RemotingShutdownEvent());
                Interlocked.Exchange(ref owner._endpointManager, null);
            }

            if (result.IsSuccessfully()) //Shutdown was successful
            {
                if (!result.Result)
                {
                    var log = owner._log;
                    if (log.IsWarningEnabled) log.ShutdownFinishedButFlushingMightNotHaveBeenSuccessful();
                }
                finalize();
            }
            else
            {
                owner.NotifyError("Failure during shutdown of remoting", result.Exception);
                finalize();
            }
        }

        #endregion

        #region - Send -

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <param name="sender">TBD</param>
        /// <param name="recipient">TBD</param>
        /// <exception cref="RemoteTransportException">TBD</exception>
        public override void Send(object message, IActorRef sender, RemoteActorRef recipient)
        {
            if (_endpointManager is null) { ThrowHelper.ThrowRemoteTransportException_EndpointManager(); }

            _endpointManager.Tell(new EndpointManager.Send(message, recipient, sender), sender ?? ActorRefs.NoSender);
        }

        #endregion

        #region - ManagementCommand -

        /// <summary>TBD</summary>
        /// <param name="cmd">TBD</param>
        /// <exception cref="RemoteTransportException">TBD</exception>
        /// <returns>TBD</returns>
        public override Task<bool> ManagementCommand(object cmd)
        {
            if (_endpointManager is null) { ThrowHelper.ThrowRemoteTransportException_EndpointManager_Cmd(); }

            return _endpointManager
                .Ask<EndpointManager.ManagementCommandAck>(new EndpointManager.ManagementCommand(cmd), Provider.RemoteSettings.CommandAckTimeout)
                .ContinueWith(CheckManagementCommandFunc, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private static readonly Func<Task<EndpointManager.ManagementCommandAck>, bool> CheckManagementCommandFunc = CheckManagementCommand;
        private static bool CheckManagementCommand(Task<EndpointManager.ManagementCommandAck> result)
        {
            if (result.IsSuccessfully()) { return result.Result.Status; }
            return false;
        }

        #endregion

        #region - LocalAddressForRemote -

        /// <summary>TBD</summary>
        /// <param name="remote">TBD</param>
        /// <returns>TBD</returns>
        public override Address LocalAddressForRemote(Address remote) => LocalAddressForRemote(_transportMapping, remote);

        #endregion

        #region - Quarantine -

        /// <summary>Marks a remote system as out of sync and prevents reconnects until the quarantine timeout elapses.</summary>
        /// <param name="address">The address of the remote system to be quarantined</param>
        /// <param name="uid">
        /// The UID of the remote system; if the uid is not defined it will not be a strong
        /// quarantine but the current endpoint writer will be stopped (dropping system messages) and
        /// the address will be gated.
        /// </param>
        /// <exception cref="RemoteTransportException">
        /// This exception is thrown when trying to quarantine a system but remoting is not running.
        /// </exception>
        public override void Quarantine(Address address, int? uid)
        {
            if (_endpointManager is null) { ThrowHelper.ThrowRemoteTransportException_EndpointManager(address, uid); }

            _endpointManager.Tell(new EndpointManager.Quarantine(address, uid));
        }

        #endregion

        #endregion

        #region * Internal methods *

        private void NotifyError(string msg, Exception cause)
            => _eventPublisher.NotifyListeners(new RemotingErrorEvent(new RemoteTransportException(msg, cause)));

        #endregion

        #region = Static methods =

        /// <summary>TBD</summary>
        public const string EndpointManagerName = "endpointManager";

        /// <summary>TBD</summary>
        /// <param name="transportMapping">TBD</param>
        /// <param name="remote">TBD</param>
        /// <exception cref="RemoteTransportException">TBD</exception>
        /// <returns>TBD</returns>
        internal static Address LocalAddressForRemote(
            IDictionary<string, HashSet<ProtocolTransportAddressPair>> transportMapping, Address remote)
        {
            if (!transportMapping.TryGetValue(remote.Protocol, out var transports))
            {
                ThrowHelper.ThrowRemoteTransportException_LocalAddressForRemote(remote, transportMapping);
            }

            var responsibleTransports = transports.Where(t => t.ProtocolTransport.IsResponsibleFor(remote)).ToArray();
            if (0u >= (uint)responsibleTransports.Length)
            {
                ThrowHelper.ThrowRemoteTransportException_LocalAddressForRemote(remote);
            }

            if (1u < (uint)responsibleTransports.Length)
            {
                ThrowHelper.ThrowRemoteTransportException_LocalAddressForRemote(remote, responsibleTransports);
            }
            return responsibleTransports[0].Address; //.First().Address;
        }

        #endregion
    }

    #endregion

    #region == class RegisterTransportActor ==

    /// <summary>Message type used to provide both <see cref="Props"/> and a name for a new transport actor.</summary>
    internal sealed class RegisterTransportActor : INoSerializationVerificationNeeded
    {
        /// <summary>TBD</summary>
        /// <param name="props">TBD</param>
        /// <param name="name">TBD</param>
        public RegisterTransportActor(Props props, string name)
        {
            Props = props;
            Name = name;
        }

        /// <summary>TBD</summary>
        public Props Props { get; }

        /// <summary>TBD</summary>
        public string Name { get; }
    }

    #endregion

    #region == class TransportSupervisor ==

    /// <summary>Actor responsible for supervising the creation of all transport actors</summary>
    internal sealed class TransportSupervisor : ReceiveActor2
    {
        private readonly SupervisorStrategy _strategy = new OneForOneStrategy(exception => Directive.Restart);

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy() => _strategy;

        /// <summary>TBD</summary>
        public TransportSupervisor()
        {
            Receive<RegisterTransportActor>(HandleRegisterTransportActor);
        }

        private void HandleRegisterTransportActor(RegisterTransportActor r)
        {
            Sender.Tell(Context.ActorOf(RARP.For(Context.System).ConfigureDispatcher(r.Props.WithDeploy(Deploy.Local)), r.Name));
        }
    }

    #endregion
}