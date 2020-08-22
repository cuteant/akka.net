//-----------------------------------------------------------------------
// <copyright file="EndpointManager.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Event;
using Akka.Remote.Transport;
using Akka.Util;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Remote
{
    /// <summary>INTERNAL API</summary>
    internal sealed class EndpointManager : ReceiveActor2, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
    {
        #region -- Policy definitions --

        /// <summary>Defines how we're going to treat incoming and outgoing connections for specific endpoints</summary>
        [Union(0, typeof(Pass))]
        [Union(1, typeof(Gated))]
        [Union(2, typeof(Quarantined))]
        [MessagePackObject]
        public abstract class EndpointPolicy
        {
            /// <summary>Indicates that the policy does not contain an active endpoint, but it is a tombstone
            /// of a previous failure.</summary>
            [Key(0)]
            public readonly bool IsTombstone;

            protected EndpointPolicy(bool isTombstone) => IsTombstone = isTombstone;
        }

        /// <summary>We will always accept a connection from this remote node.</summary>
        [MessagePackObject]
        public sealed class Pass : EndpointPolicy
        {
            /// <summary>TBD</summary>
            /// <param name="endpoint">TBD</param>
            /// <param name="uid">TBD</param>
            [SerializationConstructor]
            public Pass(IActorRef endpoint, int? uid)
                : base(false)
            {
                Uid = uid;
                Endpoint = endpoint;
            }

            /// <summary>The actor who owns the current endpoint.</summary>
            [Key(1)]
            public readonly IActorRef Endpoint;

            /// <summary>The endpoint UID, if it's currently known.</summary>
            [Key(2)]
            public readonly int? Uid;
        }

        /// <summary>A Gated node can't be connected to from this process for <see cref="TimeOfRelease"/>, but
        /// we may accept an inbound connection from it if the remote node recovers on its own.</summary>
        [MessagePackObject]
        public sealed class Gated : EndpointPolicy
        {
            [SerializationConstructor]
            public Gated(Deadline deadline)
                : base(true)
            {
                TimeOfRelease = deadline;
            }

            [Key(1)]
            public readonly Deadline TimeOfRelease;
        }

        /// <summary>We do not accept connection attempts for a quarantined node until it restarts and resets its UID.</summary>
        [MessagePackObject]
        public sealed class Quarantined : EndpointPolicy
        {
            [SerializationConstructor]
            public Quarantined(int uid, Deadline deadline)
                : base(true)
            {
                Uid = uid;
                Deadline = deadline;
            }

            [Key(1)]
            public readonly int Uid;

            [Key(2)]
            public readonly Deadline Deadline;
        }

        #endregion

        #region -- RemotingCommands and operations --

        /// <summary>Messages sent between <see cref="Remoting"/> and <see cref="EndpointManager"/></summary>
        public abstract class RemotingCommand : INoSerializationVerificationNeeded { }

        /// <summary>TBD</summary>
        public sealed class Listen : RemotingCommand
        {
            /// <summary>TBD</summary>
            /// <param name="addressesPromise">TBD</param>
            public Listen(TaskCompletionSource<IList<ProtocolTransportAddressPair>> addressesPromise)
                => AddressesPromise = addressesPromise;

            /// <summary>TBD</summary>
            public TaskCompletionSource<IList<ProtocolTransportAddressPair>> AddressesPromise { get; }
        }

        /// <summary>TBD</summary>
        public sealed class StartupFinished : RemotingCommand { }

        /// <summary>TBD</summary>
        public sealed class ShutdownAndFlush : RemotingCommand { }

        /// <summary>TBD</summary>
        public sealed class Send : RemotingCommand, IHasSequenceNumber
        {
            /// <summary>TBD</summary>
            /// <param name="message">TBD</param>
            /// <param name="recipient">TBD</param>
            /// <param name="senderOption">TBD</param>
            /// <param name="seqOpt">TBD</param>
            public Send(object message, RemoteActorRef recipient, IActorRef senderOption = null, SeqNo seqOpt = null)
            {
                Recipient = recipient;
                SenderOption = senderOption;
                Message = message;
                Seq = seqOpt;
            }

            /// <summary>TBD</summary>
            public readonly object Message;

            /// <summary>Can be null!</summary>
            public readonly IActorRef SenderOption;

            /// <summary>TBD</summary>
            public readonly RemoteActorRef Recipient;

            /// <summary>TBD</summary>
            /// <returns>TBD</returns>
            public override string ToString() => $"Remote message {SenderOption} -> {Recipient}";

            /// <summary>TBD</summary>
            public SeqNo Seq { get; }

            /// <summary>TBD</summary>
            /// <param name="opt">TBD</param>
            /// <returns>TBD</returns>
            public Send Copy(SeqNo opt) => new Send(Message, Recipient, SenderOption, opt);
        }

        /// <summary>TBD</summary>
        public sealed class Quarantine : RemotingCommand
        {
            /// <summary>TBD</summary>
            /// <param name="remoteAddress">TBD</param>
            /// <param name="uid">TBD</param>
            public Quarantine(Address remoteAddress, int? uid)
            {
                Uid = uid;
                RemoteAddress = remoteAddress;
            }

            /// <summary>TBD</summary>
            public readonly Address RemoteAddress;

            /// <summary>TBD</summary>
            public readonly int? Uid;
        }

        /// <summary>TBD</summary>
        public sealed class ManagementCommand : RemotingCommand
        {
            /// <summary>TBD</summary>
            /// <param name="cmd">TBD</param>
            public ManagementCommand(object cmd) => Cmd = cmd;

            /// <summary>TBD</summary>
            public readonly object Cmd;
        }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class ManagementCommandAck
        {
            /// <summary>TBD</summary>
            /// <param name="status">TBD</param>
            [SerializationConstructor]
            public ManagementCommandAck(bool status) => Status = status;

            [Key(0)]
            public readonly bool Status;
        }

        #endregion

        #region -- Messages internal to EndpointManager --

        /// <summary>TBD</summary>
        public sealed class Prune : INoSerializationVerificationNeeded { }

        /// <summary>TBD</summary>
        public sealed class ListensResult : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="addressesPromise">TBD</param>
            /// <param name="results">TBD</param>
            public ListensResult(TaskCompletionSource<IList<ProtocolTransportAddressPair>> addressesPromise, List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)> results)
            {
                Results = results;
                AddressesPromise = addressesPromise;
            }

            /// <summary>TBD</summary>
            public readonly TaskCompletionSource<IList<ProtocolTransportAddressPair>> AddressesPromise;

            /// <summary>TBD</summary>
            public readonly IList<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)> Results;
        }

        /// <summary>TBD</summary>
        public sealed class ListensFailure : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="addressesPromise">TBD</param>
            /// <param name="cause">TBD</param>
            public ListensFailure(TaskCompletionSource<IList<ProtocolTransportAddressPair>> addressesPromise, Exception cause)
            {
                Cause = cause;
                AddressesPromise = addressesPromise;
            }

            /// <summary>TBD</summary>
            public readonly TaskCompletionSource<IList<ProtocolTransportAddressPair>> AddressesPromise;

            /// <summary>TBD</summary>
            public readonly Exception Cause;
        }

        /// <summary>Helper class to store address pairs</summary>
        [MessagePackObject]
        public sealed class Link
        {
            /// <summary>TBD</summary>
            /// <param name="localAddress">TBD</param>
            /// <param name="remoteAddress">TBD</param>
            [SerializationConstructor]
            public Link(Address localAddress, Address remoteAddress)
            {
                RemoteAddress = remoteAddress;
                LocalAddress = localAddress;
            }

            [Key(0)]
            public readonly Address LocalAddress;
            [Key(1)]
            public readonly Address RemoteAddress;

            /// <summary>
            /// Overrode this to make sure that the <see cref="ReliableDeliverySupervisor"/> can
            /// correctly store <see cref="AckedReceiveBuffer{T}"/> data for each <see cref="Link"/>
            /// individually, since the HashCode is what Dictionary types use internally for equality
            /// checking by default.
            /// </summary>
            /// <returns>TBD</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (LocalAddress.GetHashCode() * 397) ^ RemoteAddress.GetHashCode();
                }
            }

            private bool Equals(Link other)
            {
                return LocalAddress.Equals(other.LocalAddress) && RemoteAddress.Equals(other.RemoteAddress);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Link other && Equals(other);
            }
        }

        /// <summary>TBD</summary>
        public sealed class ResendState : IEquatable<ResendState>
        {
            /// <summary>TBD</summary>
            /// <param name="uid">TBD</param>
            /// <param name="buffer">TBD</param>
            public ResendState(int uid, AckedReceiveBuffer<Message> buffer)
            {
                Buffer = buffer;
                Uid = uid;
            }

            /// <summary>TBD</summary>
            public readonly int Uid;

            /// <summary>TBD</summary>
            public readonly AckedReceiveBuffer<Message> Buffer;

            public bool Equals(ResendState other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Uid == other.Uid && Buffer.Equals(other.Buffer);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is ResendState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Uid * 397) ^ Buffer.GetHashCode();
                }
            }
        }

        #endregion

        #region @@ Constructros @@

        /// <summary>Creates a new <see cref="EndpointManager"/> instance.</summary>
        /// <param name="config">The HOCON configuration for the current <see cref="ActorSystem"/>.</param>
        /// <param name="log">The "remoting" logging source.</param>
        public EndpointManager(Config config, ILoggingAdapter log)
        {
            _conf = config;
            _settings = new RemoteSettings(_conf);
            _log = log;
            _eventPublisher = new EventPublisher(Context.System, log, Logging.LogLevelFor(_settings.RemoteLifecycleEventsLogLevel));

            _onlyDeciderStrategyFunc = e => OnlyDeciderStrategy(e);

            _acceptingPatterns = ConfigurePatterns(Accepting);
            _flushingPatterns = ConfigurePatterns(Flushing);
            Receiving();
        }

        #endregion

        #region @@ Private members @@

        private readonly PatternMatchBuilder _acceptingPatterns;
        private readonly PatternMatchBuilder _flushingPatterns;

        /// <summary>Mapping between addresses and endpoint actors. If passive connections are turned off,
        /// incoming connections will not be part of this map!</summary>
        private readonly EndpointRegistry _endpoints = new EndpointRegistry();

        private readonly RemoteSettings _settings;
        private readonly Config _conf;
        private readonly AtomicCounterLong _endpointId = new AtomicCounterLong(0L);
        private readonly ILoggingAdapter _log;
        private readonly EventPublisher _eventPublisher;

        /// <summary>Used to indicate when an abrupt shutdown occurs</summary>
        private bool _normalShutdown = false;

        /// <summary>Mapping between transports and the local addresses they listen to</summary>
        private Dictionary<Address, AkkaProtocolTransport> _transportMapping = new Dictionary<Address, AkkaProtocolTransport>(AddressComparer.Instance);

        private readonly ConcurrentDictionary<Link, ResendState> _receiveBuffers = new ConcurrentDictionary<Link, ResendState>();

        private bool RetryGateEnabled => _settings.RetryGateClosedFor > TimeSpan.Zero;

        private TimeSpan PruneInterval
        {
            get
            {
                //PruneInterval = 2x the RetryGateClosedFor value, if available
                if (RetryGateEnabled)
                {
                    return _settings.RetryGateClosedFor.Add(_settings.RetryGateClosedFor).Max(TimeSpan.FromSeconds(1)).Min(TimeSpan.FromSeconds(10));
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        private ICancelable _pruneTimeCancelable;

        /// <summary>Cancelable for terminating <see cref="Prune"/> operations.</summary>
        private ICancelable PruneTimerCancelleable
        {
            get
            {
                if (RetryGateEnabled && _pruneTimeCancelable is null)
                {
                    return _pruneTimeCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(PruneInterval, PruneInterval, Self, new Prune(), Self);
                }
                return _pruneTimeCancelable;
            }
        }

        private Dictionary<IActorRef, AkkaProtocolHandle> _pendingReadHandoffs = new Dictionary<IActorRef, AkkaProtocolHandle>(ActorRefComparer.Instance);
        private Dictionary<IActorRef, List<InboundAssociation>> _stashedInbound = new Dictionary<IActorRef, List<InboundAssociation>>(ActorRefComparer.Instance);

        #endregion

        #region ** HandleStashedInbound **

        private void HandleStashedInbound(IActorRef endpoint, bool writerIsIdle)
        {
            var stashed = _stashedInbound.GetOrElse(endpoint, new List<InboundAssociation>());
            _stashedInbound.Remove(endpoint);
            foreach (var ia in stashed)
            {
                HandleInboundAssociation(ia, writerIsIdle);
            }
        }

        #endregion

        #region ** KeepQuarantinedOr **

        private void KeepQuarantinedOr(Address remoteAddress, Action body)
        {
            var uid = _endpoints.RefuseUid(remoteAddress);
            if (uid.HasValue)
            {
                if (_log.IsInfoEnabled)
                {
                    _log.QuarantinedAddressIsStillUnreachable(remoteAddress);
                }
                // Restoring Quarantine marker overwritten by a Pass(endpoint, refuseUid) pair while
                // probing remote system.
                _endpoints.MarkAsQuarantined(remoteAddress, uid.Value, Deadline.Now + _settings.QuarantineDuration);
            }
            else
            {
                body();
            }
        }

        #endregion

        #region ++ ActorBase overrides ++

        #region + SupervisorStrategy +

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(_onlyDeciderStrategyFunc, false);
        }

        private readonly Func<Exception, Directive> _onlyDeciderStrategyFunc;
        private Directive OnlyDeciderStrategy(Exception ex)
        {
            Directive Hopeless(HopelessAssociation ha)
            {
                if (ha.Uid.HasValue)
                {
                    _log.AssociationToWithUidIsIrrecoverablyFailed(ha);
                    if (_settings.QuarantineDuration.HasValue && _settings.QuarantineDuration != TimeSpan.MaxValue)
                    {
                        // have a finite quarantine duration specified in settings.
                        // If we don't have one specified, don't bother quarantining - it's disabled.
                        _endpoints.MarkAsQuarantined(ha.RemoteAddress, ha.Uid.Value, Deadline.Now + _settings.QuarantineDuration);
                        _eventPublisher.NotifyListeners(new QuarantinedEvent(ha.RemoteAddress, ha.Uid.Value));
                    }

                    return Directive.Stop;
                }
                else // no UID found
                {
                    if (_log.IsWarningEnabled) _log.AssociationToWithUnknownUIDIsIrrecoverablyFailed(ha, _settings);
                    _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    return Directive.Stop;
                }
            }
            switch (ex)
            {
                case InvalidAssociation ia:
                    KeepQuarantinedOr(ia.RemoteAddress, () =>
                    {
                        if (_log.IsWarningEnabled) _log.TriedToAssociateWithUnreachableRemoteAddress(ia, _settings);
                        _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    });

                    if (ia.DisassociationInfo.HasValue && ia.DisassociationInfo == DisassociateInfo.Quarantined)
                        Context.System.EventStream.Publish(new ThisActorSystemQuarantinedEvent(ia.LocalAddress, ia.RemoteAddress));
                    return Directive.Stop;

                case ShutDownAssociation shutdown:
                    KeepQuarantinedOr(shutdown.RemoteAddress, () =>
                    {
#if DEBUG
                        if (_log.IsDebugEnabled) _log.RemoteSystemWithAddressHasShutDown(shutdown, _settings);
#endif
                        _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    });
                    return Directive.Stop;

                case HopelessAssociation h:
                    return Hopeless(h);

                case ActorInitializationException i when i.InnerException is HopelessAssociation h2:
                    return Hopeless(h2);

                case EndpointDisassociatedException _:
                case EndpointAssociationException _:
                    // no logging
                    _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    return Directive.Stop;

                default:
                    _log.LogErrorX(ex);
                    _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    return Directive.Stop;
            }
        }

        #endregion

        #region + PreStart +

        protected override void PreStart()
        {
            if (PruneTimerCancelleable is object)
            {
#if DEBUG
                if (_log.IsDebugEnabled) _log.StartingPruneTimerForEndpointManager();
#endif
            }

            base.PreStart();
        }

        #endregion

        #region + PostStop +

        protected override void PostStop()
        {
            if (PruneTimerCancelleable is object)
            {
                _pruneTimeCancelable.Cancel();
            }

            foreach (var h in _pendingReadHandoffs.Values)
            {
                h.Disassociate(DisassociateInfo.Shutdown);
            }

            if (!_normalShutdown)
            {
                // Remaining running endpoints are children, so they will clean up themselves.
                // We still need to clean up any remaining transports because handles might be in mailboxes, and for example
                // DotNetty is not part of the actor hierarchy, so its handles will not be cleaned up if no actor is taking
                // responsibility of them (because they are sitting in a mailbox).
                _log.RemotingSystemHasBeenTerminatedAbruptly();
                foreach (var t in _transportMapping.Values)
                {
                    t.Shutdown();
                }
            }
        }

        #endregion

        #region * Receiving *

        private void Receiving()
        {
            /*
            * the first command the EndpointManager receives.
            * instructs the EndpointManager to fire off its "Listens" command, which starts
            * up all inbound transports and binds them to specific addresses via configuration.
            * those results will then be piped back to Remoting, who waits for the results of
            * listen.AddressPromise.
            * */
            Receive<Listen>(e => HandleListen(e));

            Receive<ListensResult>(e => HandleListensResult(e));

            Receive<ListensFailure>(e => HandleListensFailure(e));

            // defer the inbound association until we can enter "Accepting" behavior

            Receive<InboundAssociation>(e => HandleInboundAssociationDefault(e));
            Receive<ManagementCommand>(e => HandleManagementCommand());
            Receive<StartupFinished>(e => HandleStartupFinished());
            Receive<ShutdownAndFlush>(e => HandleShutdownAndFlush());
        }

        private void HandleListen(Listen listen)
        {
            Listens.LinkOutcome(InvokeHandleListenFunc, listen,
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                   .PipeTo(Self);
        }

        private static readonly Func<Task<List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)>>, Listen, INoSerializationVerificationNeeded> InvokeHandleListenFunc =
            (t, l) => InvokeHandleListen(t, l);
        private static INoSerializationVerificationNeeded InvokeHandleListen(Task<List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)>> listens, Listen listen)
        {
            if (listens.IsSuccessfully())
            {
                return new ListensResult(listen.AddressesPromise, listens.Result);
            }
            else
            {
                return new ListensFailure(listen.AddressesPromise, listens.Exception);
            }
        }

        private void HandleListensResult(ListensResult listens)
        {
            _transportMapping = (from mapping in listens.Results
                                 group mapping by mapping.Item1.Address
                                       into g
                                 select new { address = g.Key, transports = g.ToList() }).Select(x =>
                                 {
                                     if (x.transports.Count > 1)
                                     {
                                         ThrowHelper.ThrowRemoteTransportException(x.address);
                                     }
                                     return new KeyValuePair<Address, AkkaProtocolTransport>(x.address,
                                         x.transports.Head().Item1.ProtocolTransport);
                                 }).ToDictionary(x => x.Key, v => v.Value, AddressComparer.Instance);

            //Register a listener to each transport and collect mapping to addresses
            var transportsAndAddresses = listens.Results.Select(x =>
            {
                x.Item2.SetResult(new ActorAssociationEventListener(Self));
                return x.Item1;
            }).ToList();

            listens.AddressesPromise.SetResult(transportsAndAddresses);
        }

        private void HandleListensFailure(ListensFailure failure)
        {
            failure.AddressesPromise.TrySetUnwrappedException(failure.Cause);
        }

        private void HandleInboundAssociationDefault(InboundAssociation ia)
        {
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(10), Self, ia, Self);
        }

        private void HandleManagementCommand()
        {
            Sender.Tell(new ManagementCommandAck(status: false));
        }

        private void HandleStartupFinished()
        {
            Become(_acceptingPatterns);
        }

        private void HandleShutdownAndFlush()
        {
            Sender.Tell(true);
            Context.Stop(Self);
        }

        #endregion

        #region + Accepting +

        /// <summary>Message-processing behavior when the <see cref="EndpointManager"/> is able to accept
        /// inbound association requests.</summary>
        private void Accepting()
        {
            Receive<ManagementCommand>(e => HandleManagementCommandAccepting(e));

            Receive<Quarantine>(e => HandleQuarantine(e));

            Receive<Send>(e => HandleSend(e));

            Receive<InboundAssociation>(e => HandleInboundAssociation(e));
            Receive<EndpointWriter.StoppedReading>(e => HandleEndpointWriterStoppedReading(e));

            Receive<Terminated>(e => HandleTerminated(e));

            Receive<EndpointWriter.TookOver>(e => HandleEndpointWriterTookOver(e));

            Receive<ReliableDeliverySupervisor.GotUid>(e => HandleGotUid(e));

            Receive<ReliableDeliverySupervisor.Idle>(e => HandleIdle(e));
            Receive<Prune>(e => HandlePrune(e));

            Receive<ShutdownAndFlush>(e => HandleShutdownAndFlushAccepting(e));
        }

        #region HandleManagementCommand

        private void HandleManagementCommandAccepting(ManagementCommand mc)
        {
            /*
            * applies a management command to all available transports.
            *
            * Useful for things like global restart
            */
            var sender = Sender;
            var allStatuses = _transportMapping.Values.Select(x => x.ManagementCommand(mc.Cmd));
            Task.WhenAll(allStatuses)
                .LinkOutcome(CheckManagementCommandFunc, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .PipeTo(sender);
        }

        private static readonly Func<Task<bool[]>, ManagementCommandAck> CheckManagementCommandFunc = t => CheckManagementCommand(t);
        private static ManagementCommandAck CheckManagementCommand(Task<bool[]> t)
        {
            return new ManagementCommandAck(t.Result.All(y => y));
        }

        #endregion

        #region HandleQuarantine

        private void HandleQuarantine(Quarantine quarantine)
        {
            //Stop writers
            var policy =
            (_endpoints.WritableEndpointWithPolicyFor(quarantine.RemoteAddress), quarantine.Uid);
            if (policy.Item1 is Pass pass && policy.Item2 == null)
            {
                var endpoint = pass.Endpoint;
                Context.Stop(endpoint);
                if (_log.IsWarningEnabled) _log.AssociationToWithUnknownUIDIsReportedAsQuarantined(quarantine.RemoteAddress, _settings);
                _endpoints.MarkAsFailed(endpoint, Deadline.Now + _settings.RetryGateClosedFor);
            }
            else if (policy.Item1 is Pass p && policy.Item2 != null)
            {
                var uidOption = p.Uid;
                var quarantineUid = policy.Item2;
                if (uidOption == quarantineUid)
                {
                    _endpoints.MarkAsQuarantined(quarantine.RemoteAddress, quarantineUid.Value, Deadline.Now + _settings.QuarantineDuration);
                    _eventPublisher.NotifyListeners(new QuarantinedEvent(quarantine.RemoteAddress, quarantineUid.Value));
                    Context.Stop(p.Endpoint);
                }
                // or it does not match with the UID to be quarantined
                else if (!uidOption.HasValue && _endpoints.RefuseUid(quarantine.RemoteAddress) != quarantineUid)
                {
                    // the quarantine uid may be got fresh by cluster gossip, so update refuseUid for late handle when the writer got uid
                    _endpoints.RegisterWritableEndpointRefuseUid(quarantine.RemoteAddress, quarantineUid.Value, Deadline.Now + _settings.QuarantineDuration);
                }
                else
                {
                    //the quarantine uid has lost the race with some failure, do nothing
                }
            }
            else if (policy.Item1 is Quarantined && policy.Item2 != null && policy.Item1.AsInstanceOf<Quarantined>().Uid == policy.Item2.Value)
            {
                // the UID to be quarantined already exists, do nothing
            }
            else if (policy.Item2 != null)
            {
                // the current state is gated or quarantined, and we know the UID, update
                _endpoints.MarkAsQuarantined(quarantine.RemoteAddress, policy.Item2.Value, Deadline.Now + _settings.QuarantineDuration);
                _eventPublisher.NotifyListeners(new QuarantinedEvent(quarantine.RemoteAddress, policy.Item2.Value));
            }
            else
            {
                // the current state is Gated, WasGated, or Quarantined and we don't know the UID, do nothing.
            }

            // Stop inbound read-only associations
            var readPolicy = (_endpoints.ReadOnlyEndpointFor(quarantine.RemoteAddress), quarantine.Uid);
            if (readPolicy.Item1?.Item1 != null && quarantine.Uid == null)
                Context.Stop(readPolicy.Item1.Value.Item1);
            else if (readPolicy.Item1?.Item1 != null && quarantine.Uid != null && readPolicy.Item1?.Item2 == quarantine.Uid) { Context.Stop(readPolicy.Item1.Value.Item1); }
            else { } // nothing to stop

            bool MatchesQuarantine(AkkaProtocolHandle handle)
            {

                return handle.RemoteAddress.Equals(quarantine.RemoteAddress) &&
                       quarantine.Uid == handle.HandshakeInfo.Uid;
            }

            // Stop all matching pending read handoffs
            _pendingReadHandoffs = _pendingReadHandoffs.Where(x =>
            {
                var drop = MatchesQuarantine(x.Value);
                // Side-effecting here
                if (drop)
                {
                    x.Value.Disassociate();
                    Context.Stop(x.Key);
                }
                return !drop;
            }).ToDictionary(key => key.Key, value => value.Value, ActorRefComparer.Instance);

            // Stop all matching stashed connections
            _stashedInbound = _stashedInbound.Select(x =>
            {
                var associations = x.Value.Where(assoc =>
                {
                    var handle = assoc.Association.AsInstanceOf<AkkaProtocolHandle>();
                    var drop = MatchesQuarantine(handle);
                    if (drop) { handle.Disassociate(); }
                    return !drop;
                }).ToList();
                return new KeyValuePair<IActorRef, List<InboundAssociation>>(x.Key, associations);
            }).ToDictionary(k => k.Key, v => v.Value, ActorRefComparer.Instance);
        }

        #endregion

        #region HandleSend

        private void HandleSend(Send send)
        {
            var recipient = send.Recipient;
            var recipientAddress = recipient.Path.Address;
            IActorRef CreateAndRegisterWritingEndpoint() => _endpoints.RegisterWritableEndpoint(recipientAddress,
                CreateEndpoint(recipientAddress, recipient.LocalAddressToUse, _transportMapping[recipient.LocalAddressToUse],
                    _settings, writing: true, handleOption: null), uid: null);

            // pattern match won't throw a NullReferenceException if one is returned by WritableEndpointWithPolicyFor
            var endpointPolicy = _endpoints.WritableEndpointWithPolicyFor(recipientAddress);
            switch (endpointPolicy)
            {
                case Pass pass:
                    pass.Endpoint.Tell(send);
                    break;
                case Gated gated:
                    if (gated.TimeOfRelease.IsOverdue)
                    {
                        CreateAndRegisterWritingEndpoint().Tell(send);
                    }
                    else
                    {
                        Context.System.DeadLetters.Tell(send);
                    }
                    break;
                case Quarantined _:
                    // timeOfRelease is only used for garbage collection reasons, therefore it is
                    // ignored here. We still have the Quarantined tombstone and we know what UID
                    // we don't want to accept, so use it.
                    CreateAndRegisterWritingEndpoint().Tell(send);
                    break;
                default:
                    CreateAndRegisterWritingEndpoint().Tell(send);
                    break;
            }
        }

        #endregion

        private void HandleEndpointWriterStoppedReading(EndpointWriter.StoppedReading endpoint)
        {
            AcceptPendingReader(endpoint.Writer);
        }

        private void HandleTerminated(Terminated terminated)
        {
            var actorRef = terminated.ActorRef;
            AcceptPendingReader(actorRef);
            _endpoints.UnregisterEndpoint(actorRef);
            HandleStashedInbound(actorRef, writerIsIdle: false);
        }

        private void HandleEndpointWriterTookOver(EndpointWriter.TookOver tookover)
        {
            RemovePendingReader(tookover.Writer, tookover.ProtocolHandle);
        }

        #region ReliableDeliverySupervisor.GotUid

        private void HandleGotUid(ReliableDeliverySupervisor.GotUid gotuid)
        {
            var remoteAddr = gotuid.RemoteAddress;
            var refuseUidOption = _endpoints.RefuseUid(remoteAddr);
            var uid = gotuid.Uid;
            var policy = _endpoints.WritableEndpointWithPolicyFor(remoteAddr);
            if (policy is Pass pass)
            {
                if (refuseUidOption == uid)
                {
                    _endpoints.MarkAsQuarantined(remoteAddr, uid,
                        Deadline.Now + _settings.QuarantineDuration);
                    _eventPublisher.NotifyListeners(new QuarantinedEvent(remoteAddr, uid));
                    Context.Stop(pass.Endpoint);
                }
                else
                {
                    _endpoints.RegisterWritableEndpointUid(remoteAddr, uid);
                }
                HandleStashedInbound(Sender, writerIsIdle: false);
            }
            else
            {
                // the GotUid might have lost the race with some failure
            }
        }

        #endregion

        private void HandleIdle(ReliableDeliverySupervisor.Idle idle)
        {
            HandleStashedInbound(Sender, writerIsIdle: true);
        }

        private void HandlePrune(Prune prune)
        {
            _endpoints.Prune();
        }

        #region HandleShutdownAndFlush

        private void HandleShutdownAndFlushAccepting(ShutdownAndFlush shutdown)
        {
            //Shutdown all endpoints and signal to Sender when ready (and whether all endpoints were shutdown gracefully)
            var sender = Sender;

            // The construction of the Task for shutdownStatus has to happen after the
            // flushStatus future has been finished so that endpoints are shut down before transports.
            var shutdownStatus = Task
                .WhenAll(_endpoints.AllEndpoints.Select(
                    x => x.GracefulStop(_settings.FlushWait, EndpointWriter.FlushAndStop.Instance)))
                .LinkOutcome(CheckGracefulStopFunc, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            shutdownStatus.Then(AfterGracefulStopFunc, _transportMapping).PipeTo(sender);

            foreach (var handoff in _pendingReadHandoffs.Values)
            {
                handoff.Disassociate(DisassociateInfo.Shutdown);
            }

            //Ignore all other writes
            _normalShutdown = true;
            Become(_flushingPatterns);
        }

        private static readonly Func<Task<bool[]>, bool> CheckGracefulStopFunc = t => CheckGracefulStop(t);
        private static bool CheckGracefulStop(Task<bool[]> result)
        {
            if (result.IsSuccessfully())
            {
                return result.Result.All(x => x);
            }
            if (result.Exception is object) { result.Exception.Handle(e => true); }
            return false;
        }

        private static readonly Func<bool, Dictionary<Address, AkkaProtocolTransport>, Task<bool>> AfterGracefulStopFunc = (s, t) => AfterGracefulStop(s, t);
        private static Task<bool> AfterGracefulStop(bool status, Dictionary<Address, AkkaProtocolTransport> transportMapping)
        {
            return Task.WhenAll(transportMapping.Values.Select(x => x.Shutdown()))
                       .LinkOutcome(CheckShutdownFunc, status, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private static readonly Func<Task<bool[]>, bool, bool> CheckShutdownFunc = (t, s) => CheckShutdown(t, s);
        private static bool CheckShutdown(Task<bool[]> result, bool shutdownStatus)
        {
            if (result.IsSuccessfully())
            {
                return result.Result.All(x => x) && shutdownStatus;
            }
            if (result.Exception is object) { result.Exception.Handle(e => true); }
            return false;
        }

        #endregion

        #endregion

        #region + Flushing +

        /// <summary>TBD</summary>
        private void Flushing()
        {
            Receive<Send>(e => HandleSendFlushing(e));
            Receive<InboundAssociation>(e => HandleInboundAssociationFlushing(e));
            Receive<Terminated>(PatternMatch<Terminated>.EmptyAction); // why should we care now?
        }

        private void HandleSendFlushing(Send send)
        {
            Context.System.DeadLetters.Tell(send);
        }

        private void HandleInboundAssociationFlushing(InboundAssociation ia)
        {
            ia.Association.AsInstanceOf<AkkaProtocolHandle>().Disassociate(DisassociateInfo.Shutdown);
        }

        #endregion

        #endregion

        #region ** Internal methods **

        #region * HandleInboundAssociation *

        private void HandleInboundAssociation(InboundAssociation ia) => HandleInboundAssociation(ia, false);
        private void HandleInboundAssociation(InboundAssociation ia, bool writerIsIdle)
        {
            var handle = ((AkkaProtocolHandle)ia.Association);
            var remoteAddr = handle.RemoteAddress;
            var readonlyEndpoint = _endpoints.ReadOnlyEndpointFor(remoteAddr);
            if (readonlyEndpoint is object)
            {
                var endpoint = readonlyEndpoint.Value.Item1;
                if (_pendingReadHandoffs.TryGetValue(endpoint, out var protocolHandle))
                {
                    protocolHandle.Disassociate("the existing readOnly association was replaced by a new incoming one", _log);
                }

                _pendingReadHandoffs[endpoint] = handle;
                endpoint.Tell(new EndpointWriter.TakeOver(handle, Self));
                var policy = _endpoints.WritableEndpointWithPolicyFor(remoteAddr);
                if (policy is Pass pass)
                {
                    pass.Endpoint.Tell(ReliableDeliverySupervisor.Ungate.Instance);
                }
            }
            else
            {
                var handshakeInfoUid = handle.HandshakeInfo.Uid;
                if (_endpoints.IsQuarantined(remoteAddr, handshakeInfoUid))
                {
                    handle.Disassociate(DisassociateInfo.Quarantined);
                }
                else
                {
                    var policy = _endpoints.WritableEndpointWithPolicyFor(remoteAddr);
                    if (policy is Pass pass)
                    {
                        var endpoint = pass.Endpoint;
                        var passUid = pass.Uid;
                        if (!passUid.HasValue) // pass, but UID is unknown
                        {
                            // Idle writer will never send a GotUid or a Terminated so we need to
                            // "provoke it" to get an unstash event
                            if (!writerIsIdle)
                            {
                                endpoint.Tell(ReliableDeliverySupervisor.IsIdle.Instance);

                                var stashedInboundForEp = _stashedInbound.GetOrElse(endpoint,
                                    new List<InboundAssociation>());
                                stashedInboundForEp.Add(ia);
                                _stashedInbound[endpoint] = stashedInboundForEp;
                            }
                            else
                            {
                                CreateAndRegisterEndpoint(handle);
                            }
                        }
                        else // pass with known UID
                        {
                            if (handshakeInfoUid == passUid)
                            {
                                _pendingReadHandoffs.GetOrElse(endpoint, null)?.Disassociate("the existing writable association was replaced by a new incoming one", _log);
                                _pendingReadHandoffs.AddOrSet(endpoint, handle);
                                endpoint.Tell(new EndpointWriter.StopReading(endpoint, Self));
                                endpoint.Tell(ReliableDeliverySupervisor.Ungate.Instance);
                            }
                            else
                            {
                                Context.Stop(endpoint);
                                _endpoints.UnregisterEndpoint(endpoint);
                                _pendingReadHandoffs.Remove(endpoint);
                                _endpoints.MarkAsQuarantined(handle.RemoteAddress, passUid.Value, Deadline.Now + _settings.QuarantineDuration);
                                CreateAndRegisterEndpoint(handle);
                            }
                        }
                    }
                    else
                    {
                        CreateAndRegisterEndpoint(handle);
                    }
                }
            }
        }

        #endregion

        #region * Listens *

        private Task<List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)>> _listens;

        private Task<List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)>> Listens
        {
            get
            {
                if (_listens is null)
                {
                    /*
                     * Constructs chains of adapters on top of each driven given in configuration. The result structure looks like the following:
                     *
                     *      AkkaProtocolTransport <-- Adapter <-- ... <-- Adapter <-- Driver
                     *
                     * The transports variable contains only the heads of each chains (the AkkaProtocolTransport instances)
                     */
                    var transports = new List<AkkaProtocolTransport>();
                    var actorSystem = Context.System;
                    foreach (var transportSettings in _settings.Transports)
                    {
                        var args = new object[] { actorSystem, transportSettings.Config };

                        // Loads the driver -- the bottom element of the chain
                        // The chain at this point:
                        //   Driver
                        Transport.Transport driver;
                        try
                        {
                            var driverType = TypeUtil.ResolveType(transportSettings.TransportClass);
                            if (driverType is null)
                            {
                                ThrowHelper.ThrowTypeLoadException(transportSettings);
                            }
                            if (!typeof(Transport.Transport).IsAssignableFrom(driverType))
                            {
                                ThrowHelper.ThrowTypeLoadException_Transport(transportSettings);
                            }

                            var constructorInfo = driverType.GetConstructor(new[] { typeof(ActorSystem), typeof(Config) });
                            if (constructorInfo is null)
                            {
                                ThrowHelper.ThrowTypeLoadException_ActorSystem(transportSettings);
                            }

                            // ReSharper disable once AssignNullToNotNullAttribute
                            driver = (Transport.Transport)Activator.CreateInstance(driverType, args);
                        }
                        catch (Exception ex)
                        {
                            var ei = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
                            var task = new Task<List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)>>(InvokeOnListenFailFunc, ei);
                            task.RunSynchronously();
                            _listens = task;
                            return _listens;
                        }

                        //Iteratively decorates the bottom level driver with a list of adapters
                        //The chain at this point:
                        //  Adapter <-- .. <-- Adapter <-- Driver
                        var wrappedTransport = transportSettings.Adapters
                            .Select(x => TransportAdaptersExtension.For(actorSystem).GetAdapterProvider(x))
                            .Aggregate(driver, (transport, provider) => provider.Create(transport, (ExtendedActorSystem)actorSystem));

                        //Apply AkkaProtocolTransport wrapper to the end of the chain
                        //The chain at this point:
                        // AkkaProtocolTransport <-- Adapter <-- .. <-- Adapter <-- Driver
                        transports.Add(new AkkaProtocolTransport(wrappedTransport, actorSystem, new AkkaProtocolSettings(_conf), new AkkaPduMessagePackCodec(actorSystem)));
                    }

                    // Collect all transports, listen addresses, and listener promises in one Task
                    var tasks = transports.Select(x => x.Listen().Then(AfterListenFunc, x, TaskContinuationOptions.ExecuteSynchronously));
                    _listens = Task.WhenAll(tasks).ContinueWith(transportResults => transportResults.Result.ToList(), TaskContinuationOptions.ExecuteSynchronously);
                }
                return _listens;
            }
        }

        private static readonly Func<object, List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)>> InvokeOnListenFailFunc =
            s => InvokeOnListenFail(s);
        private static List<(ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)> InvokeOnListenFail(object state)
        {
            ((System.Runtime.ExceptionServices.ExceptionDispatchInfo)state).Throw();
            return null;
        }

        private static readonly Func<(Address, TaskCompletionSource<IAssociationEventListener>), AkkaProtocolTransport, (ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>)> AfterListenFunc =
            (s, t) => AfterListen(s, t);
        private static (ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>) AfterListen(
            (Address, TaskCompletionSource<IAssociationEventListener>) result, AkkaProtocolTransport transport)
        {
            return (new ProtocolTransportAddressPair(transport, result.Item1), result.Item2);
        }

        #endregion

        #region * AcceptPendingReader *

        private void AcceptPendingReader(IActorRef takingOverFrom)
        {
            if (_pendingReadHandoffs.TryGetValue(takingOverFrom, out var handle))
            {
                _pendingReadHandoffs.Remove(takingOverFrom);
                var localAddr = handle.LocalAddress;
                var remoteAddr = handle.RemoteAddress;
                _eventPublisher.NotifyListeners(new AssociatedEvent(localAddr, remoteAddr, inbound: true));
                var endpoint = CreateEndpoint(remoteAddr, localAddr,
                    _transportMapping[localAddr], _settings, false, handle);
                _endpoints.RegisterReadOnlyEndpoint(remoteAddr, endpoint, handle.HandshakeInfo.Uid);
            }
        }

        #endregion

        #region * RemovePendingReader *

        private void RemovePendingReader(IActorRef takingOverFrom, AkkaProtocolHandle withHandle)
        {
            if (_pendingReadHandoffs.TryGetValue(takingOverFrom, out var handle) && handle.Equals(withHandle))
            {
                _pendingReadHandoffs.Remove(takingOverFrom);
            }
        }

        #endregion

        #region * CreateAndRegisterEndpoint *

        private void CreateAndRegisterEndpoint(AkkaProtocolHandle handle)
        {
            var localAddr = handle.LocalAddress;
            var remoteAddr = handle.RemoteAddress;
            var writing = _settings.UsePassiveConnections && !_endpoints.HasWritableEndpointFor(remoteAddr);
            _eventPublisher.NotifyListeners(new AssociatedEvent(localAddr, remoteAddr, true));
            var endpoint = CreateEndpoint(
                remoteAddr,
                localAddr,
                _transportMapping[localAddr],
                _settings,
                writing,
                handle);

            if (writing)
            {
                _endpoints.RegisterWritableEndpoint(remoteAddr, endpoint, handle.HandshakeInfo.Uid);
            }
            else
            {
                _endpoints.RegisterReadOnlyEndpoint(remoteAddr, endpoint, handle.HandshakeInfo.Uid);
                if (!_endpoints.HasWritableEndpointFor(remoteAddr))
                {
                    _endpoints.RemovePolicy(remoteAddr);
                }
            }
        }

        #endregion

        #region * CreateEndpoint *

        private IActorRef CreateEndpoint(
            Address remoteAddress,
            Address localAddress,
            AkkaProtocolTransport transport,
            RemoteSettings endpointSettings,
            bool writing,
            AkkaProtocolHandle handleOption = null)
        {
            System.Diagnostics.Debug.Assert(_transportMapping.ContainsKey(localAddress));
            // refuseUid is ignored for read-only endpoints since the UID of the remote system is
            // already known and has passed quarantine checks
            var refuseUid = _endpoints.RefuseUid(remoteAddress);

            IActorRef endpointActor;
            var context = Context;
            var actorSystem = context.System;
            if (writing)
            {
                endpointActor =
                    context.ActorOf(RARP.For(actorSystem)
                    .ConfigureDispatcher(
                        ReliableDeliverySupervisor.ReliableDeliverySupervisorProps(handleOption, localAddress,
                            remoteAddress, refuseUid, transport, endpointSettings, new AkkaPduMessagePackCodec(actorSystem),
                            _receiveBuffers, endpointSettings.Dispatcher)
                            .WithDeploy(Deploy.Local)),
                        $"reliableEndpointWriter-{AddressUrlEncoder.Encode(remoteAddress)}-{_endpointId.Next()}");
            }
            else
            {
                endpointActor =
                    context.ActorOf(RARP.For(actorSystem)
                    .ConfigureDispatcher(
                        EndpointWriter.EndpointWriterProps(handleOption, localAddress, remoteAddress, refuseUid,
                            transport, endpointSettings, new AkkaPduMessagePackCodec(actorSystem), _receiveBuffers,
                            reliableDeliverySupervisor: null)
                            .WithDeploy(Deploy.Local)),
                        $"endpointWriter-{AddressUrlEncoder.Encode(remoteAddress)}-{_endpointId.Next()}");
            }

            context.Watch(endpointActor);
            return endpointActor;
        }

        #endregion

        #endregion
    }
}