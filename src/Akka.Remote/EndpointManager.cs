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

namespace Akka.Remote
{
    /// <summary>INTERNAL API</summary>
    internal sealed class EndpointManager : ReceiveActor, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
    {
        #region -- Policy definitions --

        /// <summary>TBD</summary>
        public abstract class EndpointPolicy
        {
            /// <summary>Indicates that the policy does not contain an active endpoint, but it is a tombstone
            /// of a previous failure.</summary>
            public readonly bool IsTombstone;

            /// <summary>TBD</summary>
            /// <param name="isTombstone">TBD</param>
            protected EndpointPolicy(bool isTombstone) => IsTombstone = isTombstone;
        }

        /// <summary>We will always accept a connection from this remote node.</summary>
        public sealed class Pass : EndpointPolicy
        {
            /// <summary>TBD</summary>
            /// <param name="endpoint">TBD</param>
            /// <param name="uid">TBD</param>
            /// <param name="refuseUid">TBD</param>
            public Pass(IActorRef endpoint, int? uid, int? refuseUid)
                : base(false)
            {
                Uid = uid;
                Endpoint = endpoint;
                RefuseUid = refuseUid;
            }

            /// <summary>TBD</summary>
            public IActorRef Endpoint { get; }

            /// <summary>TBD</summary>
            public int? Uid { get; }

            /// <summary>TBD</summary>
            public int? RefuseUid { get; }
        }

        /// <summary>A Gated node can't be connected to from this process for <see cref="TimeOfRelease"/>, but
        /// we may accept an inbound connection from it if the remote node recovers on its own.</summary>
        public sealed class Gated : EndpointPolicy
        {
            /// <summary>TBD</summary>
            /// <param name="deadline">TBD</param>
            /// <param name="refuseUid">TBD</param>
            public Gated(Deadline deadline, int? refuseUid)
                : base(true)
            {
                TimeOfRelease = deadline;
                RefuseUid = refuseUid;
            }

            /// <summary>TBD</summary>
            public Deadline TimeOfRelease { get; }

            /// <summary>TBD</summary>
            public int? RefuseUid { get; }
        }

        /// <summary>Used to indicated that a node was <see cref="Gated"/> previously.</summary>
        public sealed class WasGated : EndpointPolicy
        {
            /// <summary>TBD</summary>
            /// <param name="refuseUid">TBD</param>
            public WasGated(int? refuseUid) : base(false) => RefuseUid = refuseUid;

            /// <summary>TBD</summary>
            public int? RefuseUid { get; }
        }

        /// <summary>We do not accept connection attempts for a quarantined node until it restarts and resets its UID.</summary>
        public sealed class Quarantined : EndpointPolicy
        {
            /// <summary>TBD</summary>
            /// <param name="uid">TBD</param>
            /// <param name="deadline">TBD</param>
            public Quarantined(int uid, Deadline deadline)
                : base(true)
            {
                Uid = uid;
                Deadline = deadline;
            }

            /// <summary>TBD</summary>
            public int Uid { get; }

            /// <summary>TBD</summary>
            public Deadline Deadline { get; }
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
            public object Message { get; }

            /// <summary>Can be null!</summary>
            public IActorRef SenderOption { get; }

            /// <summary>TBD</summary>
            public RemoteActorRef Recipient { get; }

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
            public Address RemoteAddress { get; }

            /// <summary>TBD</summary>
            public int? Uid { get; }
        }

        /// <summary>TBD</summary>
        public sealed class ManagementCommand : RemotingCommand
        {
            /// <summary>TBD</summary>
            /// <param name="cmd">TBD</param>
            public ManagementCommand(object cmd) => Cmd = cmd;

            /// <summary>TBD</summary>
            public object Cmd { get; }
        }

        /// <summary>TBD</summary>
        public sealed class ManagementCommandAck
        {
            /// <summary>TBD</summary>
            /// <param name="status">TBD</param>
            public ManagementCommandAck(bool status) => Status = status;

            /// <summary>TBD</summary>
            public bool Status { get; }
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
            public ListensResult(TaskCompletionSource<IList<ProtocolTransportAddressPair>> addressesPromise, List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>> results)
            {
                Results = results;
                AddressesPromise = addressesPromise;
            }

            /// <summary>TBD</summary>
            public TaskCompletionSource<IList<ProtocolTransportAddressPair>> AddressesPromise { get; }

            /// <summary>TBD</summary>
            public IList<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>> Results { get; }
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
            public TaskCompletionSource<IList<ProtocolTransportAddressPair>> AddressesPromise { get; }

            /// <summary>TBD</summary>
            public Exception Cause { get; }
        }

        /// <summary>Helper class to store address pairs</summary>
        public sealed class Link
        {
            /// <summary>TBD</summary>
            /// <param name="localAddress">TBD</param>
            /// <param name="remoteAddress">TBD</param>
            public Link(Address localAddress, Address remoteAddress)
            {
                RemoteAddress = remoteAddress;
                LocalAddress = localAddress;
            }

            /// <summary>TBD</summary>
            public Address LocalAddress { get; }

            /// <summary>TBD</summary>
            public Address RemoteAddress { get; }

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
                    var hash = 17;
                    hash = hash * 23 + (LocalAddress == null ? 0 : LocalAddress.GetHashCode());
                    hash = hash * 23 + (RemoteAddress == null ? 0 : RemoteAddress.GetHashCode());
                    return hash;
                }
            }
        }

        /// <summary>TBD</summary>
        public sealed class ResendState
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
            public int Uid { get; }

            /// <summary>TBD</summary>
            public AckedReceiveBuffer<Message> Buffer { get; }
        }

        #endregion

        #region @@ Constructros @@

        /// <summary>TBD</summary>
        /// <param name="config">TBD</param>
        /// <param name="log">TBD</param>
        public EndpointManager(Config config, ILoggingAdapter log)
        {
            _conf = config;
            _settings = new RemoteSettings(_conf);
            _log = log;
            _eventPublisher = new EventPublisher(Context.System, log, Logging.LogLevelFor(_settings.RemoteLifecycleEventsLogLevel));

            _onlyDeciderStrategy = OnlyDeciderStrategy;
            _handleInboundAssociationAction = HandleInboundAssociation;

            Receiving();
        }

        #endregion

        #region @@ Private members @@

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
                if (RetryGateEnabled && _pruneTimeCancelable == null)
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
            return new OneForOneStrategy(_onlyDeciderStrategy, false);
        }

        private readonly Func<Exception, Directive> _onlyDeciderStrategy;
        private Directive OnlyDeciderStrategy(Exception ex)
        {
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
                        if (_log.IsDebugEnabled) _log.RemoteSystemWithAddressHasShutDown(shutdown, _settings);
                        _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    });
                    return Directive.Stop;

                case HopelessAssociation hopeless:
                    if (hopeless.Uid.HasValue)
                    {
                        _log.AssociationToWithUidIsIrrecoverablyFailed(hopeless);
                        if (_settings.QuarantineDuration.HasValue)
                        {
                            _endpoints.MarkAsQuarantined(hopeless.RemoteAddress, hopeless.Uid.Value,
                           Deadline.Now + _settings.QuarantineDuration.Value);
                            _eventPublisher.NotifyListeners(new QuarantinedEvent(hopeless.RemoteAddress,
                                hopeless.Uid.Value));
                        }
                    }
                    else
                    {
                        if (_log.IsWarningEnabled) _log.AssociationToWithUnknownUIDIsIrrecoverablyFailed(hopeless, _settings);
                        _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    }
                    return Directive.Stop;

                default:
                    switch (ex)
                    {
                        case EndpointDisassociatedException _:
                        case EndpointAssociationException _:
                            // no logging
                            break;

                        default:
                            _log.LogErrorX(ex);
                            break;
                    }
                    _endpoints.MarkAsFailed(Sender, Deadline.Now + _settings.RetryGateClosedFor);
                    return Directive.Stop;
            }
        }

        #endregion

        #region + PreStart +

        /// <summary>TBD</summary>
        protected override void PreStart()
        {
            if (PruneTimerCancelleable != null)
            {
                if (_log.IsDebugEnabled) _log.StartingPruneTimerForEndpointManager();
            }

            base.PreStart();
        }

        #endregion

        #region + PostStop +

        /// <summary>TBD</summary>
        protected override void PostStop()
        {
            if (PruneTimerCancelleable != null)
            {
                _pruneTimeCancelable.Cancel();
            }

            foreach (var h in _pendingReadHandoffs.Values)
            {
                h.Disassociate(DisassociateInfo.Shutdown);
            }

            if (!_normalShutdown)
            {
                // Remaining running endpoints are children, so they will clean up themselves. We
                // still need to clean up any remaining transports because handles might be in
                // mailboxes, and for example Netty is not part of the actor hierarchy, so its
                // handles will not be cleaned up if no actor is taking responsibility of them
                // (because they are sitting in a mailbox).
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
            #region Listen

            /*
            * the first command the EndpointManager receives.
            * instructs the EndpointManager to fire off its "Listens" command, which starts
            * up all inbound transports and binds them to specific addresses via configuration.
            * those results will then be piped back to Remoting, who waits for the results of
            * listen.AddressPromise.
            * */
            Receive<Listen>(listen =>
            {
                Listens.LinkOutcome(InvokeHandleListenFunc, listen,
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                       .PipeTo(Self);
            });

            #endregion

            #region ListensResult

            Receive<ListensResult>(listens =>
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
            });

            #endregion

            Receive<ListensFailure>(failure => failure.AddressesPromise.TrySetUnwrappedException(failure.Cause));

            // defer the inbound association until we can enter "Accepting" behavior

            Receive<InboundAssociation>(
                ia => Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(10), Self, ia, Self));
            Receive<ManagementCommand>(mc => Sender.Tell(new ManagementCommandAck(status: false)));
            Receive<StartupFinished>(sf => Become(Accepting));
            Receive<ShutdownAndFlush>(sf =>
            {
                Sender.Tell(true);
                Context.Stop(Self);
            });
        }

        private static readonly Func<Task<List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>>>, Listen, INoSerializationVerificationNeeded> InvokeHandleListenFunc = InvokeHandleListen;
        private static INoSerializationVerificationNeeded InvokeHandleListen(Task<List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>>> listens, Listen listen)
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

        #endregion

        #region + Accepting +

        /// <summary>Message-processing behavior when the <see cref="EndpointManager"/> is able to accept
        /// inbound association requests.</summary>
        private void Accepting()
        {
            #region ManagementCommand

            Receive<ManagementCommand>(mc =>
            {
                /*
                * applies a management command to all available transports.
                *
                * Useful for things like global restart
                */
                var sender = Sender;
                var allStatuses = _transportMapping.Values.Select(x => x.ManagementCommand(mc.Cmd));
                Task.WhenAll(allStatuses)
                    .Then(CheckManagementCommandFunc, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                    .PipeTo(sender);
            });

            #endregion

            #region Quarantine

            Receive<Quarantine>(quarantine =>
            {
                var context = Context;
                var remoteAddr = quarantine.RemoteAddress;
                //Stop writers
                var policy = _endpoints.WritableEndpointWithPolicyFor(remoteAddr);
                var quarantineUid = quarantine.Uid;
                if (quarantineUid != null)
                {
                    switch (policy)
                    {
                        case Pass pass:
                            var uidOption = pass.Uid;
                            if (uidOption == quarantineUid)
                            {
                                _endpoints.MarkAsQuarantined(remoteAddr, quarantineUid.Value, Deadline.Now + _settings.QuarantineDuration);
                                _eventPublisher.NotifyListeners(new QuarantinedEvent(remoteAddr, quarantineUid.Value));
                                context.Stop(pass.Endpoint);
                            }
                            // or it does not match with the UID to be quarantined
                            else if (!uidOption.HasValue && pass.RefuseUid != quarantineUid)
                            {
                                // the quarantine uid may be got fresh by cluster gossip, so update refuseUid
                                // for late handle when the writer got uid
                                _endpoints.RegisterWritableEndpointRefuseUid(remoteAddr, quarantineUid.Value);
                            }
                            else
                            {
                                //the quarantine uid has lost the race with some failure, do nothing
                            }
                            break;

                        case WasGated wg:
                            if (wg.RefuseUid == quarantineUid)
                            {
                                _endpoints.RegisterWritableEndpointRefuseUid(remoteAddr, quarantineUid.Value);
                            }
                            break;

                        case Quarantined quarantined when quarantined.Uid == quarantineUid.Value:
                            // the UID to be quarantined already exists, do nothing
                            break;

                        default:
                            // the current state is gated or quarantined, and we know the UID, update
                            _endpoints.MarkAsQuarantined(remoteAddr, quarantineUid.Value, Deadline.Now + _settings.QuarantineDuration);
                            _eventPublisher.NotifyListeners(new QuarantinedEvent(remoteAddr, quarantineUid.Value));
                            break;
                    }
                }
                else
                {
                    if (policy is Pass pass)
                    {
                        var endpoint = pass.Endpoint;
                        context.Stop(endpoint);
                        if (_log.IsWarningEnabled) _log.AssociationToWithUnknownUIDIsReportedAsQuarantined(remoteAddr, _settings);
                        _endpoints.MarkAsFailed(endpoint, Deadline.Now + _settings.RetryGateClosedFor);
                    }
                    else
                    {
                        // the current state is Gated, WasGated, or Quarantined and we don't know the
                        // UID, do nothing.
                    }
                }

                // Stop inbound read-only associations
                var readPolicy = _endpoints.ReadOnlyEndpointFor(remoteAddr);
                if (readPolicy != null)
                {
                    var readPolicyActor = readPolicy.Item1;
                    if (readPolicyActor != null)
                    {
                        if (quarantineUid == null)
                        {
                            context.Stop(readPolicyActor);
                        }
                        else if (readPolicy.Item2 == quarantineUid)
                        {
                            context.Stop(readPolicyActor);
                        }
                        else { } // nothing to stop
                    }
                }
                bool MatchesQuarantine(AkkaProtocolHandle handle) => handle.RemoteAddress.Equals(remoteAddr) && quarantineUid == handle.HandshakeInfo.Uid;

                // Stop all matching pending read handoffs
                _pendingReadHandoffs = _pendingReadHandoffs.Where(x =>
                {
                    var drop = MatchesQuarantine(x.Value);
                    // Side-effecting here
                    if (drop)
                    {
                        x.Value.Disassociate();
                        context.Stop(x.Key);
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
            });

            #endregion

            #region Send

            Receive<Send>(send =>
            {
                var recipientAddress = send.Recipient.Path.Address;
                IActorRef CreateAndRegisterWritingEndpoint(int? refuseUid) => _endpoints.RegisterWritableEndpoint(recipientAddress, CreateEndpoint(recipientAddress, send.Recipient.LocalAddressToUse, _transportMapping[send.Recipient.LocalAddressToUse], _settings, writing: true, handleOption: null, refuseUid: refuseUid), uid: null, refuseUid: refuseUid);

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
                            CreateAndRegisterWritingEndpoint(gated.RefuseUid).Tell(send);
                        }
                        else
                        {
                            Context.System.DeadLetters.Tell(send);
                        }
                        break;
                    case WasGated wasGated:
                        CreateAndRegisterWritingEndpoint(wasGated.RefuseUid).Tell(send);
                        break;
                    case Quarantined quarantined:
                        // timeOfRelease is only used for garbage collection reasons, therefore it is
                        // ignored here. We still have the Quarantined tombstone and we know what UID
                        // we don't want to accept, so use it.
                        CreateAndRegisterWritingEndpoint(quarantined.Uid).Tell(send);
                        break;
                    default:
                        CreateAndRegisterWritingEndpoint(null).Tell(send);
                        break;
                }
            });

            #endregion

            Receive<InboundAssociation>(_handleInboundAssociationAction);
            Receive<EndpointWriter.StoppedReading>(endpoint => AcceptPendingReader(endpoint.Writer));

            #region Terminated

            Receive<Terminated>(terminated =>
            {
                var actorRef = terminated.ActorRef;
                AcceptPendingReader(actorRef);
                _endpoints.UnregisterEndpoint(actorRef);
                HandleStashedInbound(actorRef, writerIsIdle: false);
            });

            #endregion

            Receive<EndpointWriter.TookOver>(tookover => RemovePendingReader(tookover.Writer, tookover.ProtocolHandle));

            #region ReliableDeliverySupervisor.GotUid

            Receive<ReliableDeliverySupervisor.GotUid>(gotuid =>
            {
                var remoteAddr = gotuid.RemoteAddress;
                var uid = gotuid.Uid;
                var policy = _endpoints.WritableEndpointWithPolicyFor(remoteAddr);
                switch (policy)
                {
                    case Pass pass:
                        if (pass.RefuseUid == uid)
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
                        break;
                    case WasGated wg:
                        if (wg.RefuseUid == uid)
                        {
                            _endpoints.MarkAsQuarantined(remoteAddr, uid,
                                Deadline.Now + _settings.QuarantineDuration);
                            _eventPublisher.NotifyListeners(new QuarantinedEvent(remoteAddr, uid));
                        }
                        else
                        {
                            _endpoints.RegisterWritableEndpointUid(remoteAddr, uid);
                        }
                        HandleStashedInbound(Sender, writerIsIdle: false);
                        break;
                    default:
                        // the GotUid might have lost the race with some failure
                        break;
                }
            });

            #endregion

            Receive<ReliableDeliverySupervisor.Idle>(idle => HandleStashedInbound(Sender, writerIsIdle: true));
            Receive<Prune>(prune => _endpoints.Prune());

            #region ShutdownAndFlush

            Receive<ShutdownAndFlush>(shutdown =>
            {
                //Shutdown all endpoints and signal to Sender when ready (and whether all endpoints were shutdown gracefully)
                var sender = Sender;

                // The construction of the Task for shutdownStatus has to happen after the
                // flushStatus future has been finished so that endpoints are shut down before transports.
                var shutdownStatus = Task
                    .WhenAll(_endpoints.AllEndpoints.Select(
                        x => x.GracefulStop(_settings.FlushWait, EndpointWriter.FlushAndStop.Instance)))
                    .ContinueWith(CheckGracefulStopFunc, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                shutdownStatus.Then(AfterGracefulStopFunc, _transportMapping).PipeTo(sender);

                foreach (var handoff in _pendingReadHandoffs.Values)
                {
                    handoff.Disassociate(DisassociateInfo.Shutdown);
                }

                //Ignore all other writes
                _normalShutdown = true;
                Become(Flushing);
            });

            #endregion
        }

        private static readonly Func<bool[], ManagementCommandAck> CheckManagementCommandFunc = CheckManagementCommand;
        private static ManagementCommandAck CheckManagementCommand(bool[] result)
        {
            return new ManagementCommandAck(result.All(y => y));
        }

        private static readonly Func<Task<bool[]>, bool> CheckGracefulStopFunc = CheckGracefulStop;
        private static bool CheckGracefulStop(Task<bool[]> result)
        {
            if (result.IsSuccessfully())
            {
                return result.Result.All(x => x);
            }
            if (result.Exception != null) { result.Exception.Handle(e => true); }
            return false;
        }

        private static readonly Func<bool, Dictionary<Address, AkkaProtocolTransport>, Task<bool>> AfterGracefulStopFunc = AfterGracefulStop;
        private static Task<bool> AfterGracefulStop(bool status, Dictionary<Address, AkkaProtocolTransport> transportMapping)
        {
            return Task.WhenAll(transportMapping.Values.Select(x => x.Shutdown()))
                       .LinkOutcome(CheckShutdownFunc, status, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private static readonly Func<Task<bool[]>, bool, bool> CheckShutdownFunc = CheckShutdown;
        private static bool CheckShutdown(Task<bool[]> result, bool shutdownStatus)
        {
            if (result.IsSuccessfully())
            {
                return result.Result.All(x => x) && shutdownStatus;
            }
            if (result.Exception != null) { result.Exception.Handle(e => true); }
            return false;
        }

        #endregion

        #region + Flushing +

        /// <summary>TBD</summary>
        private void Flushing()
        {
            Receive<Send>(send => Context.System.DeadLetters.Tell(send));
            Receive<InboundAssociation>(
                     ia => ia.Association.AsInstanceOf<AkkaProtocolHandle>().Disassociate(DisassociateInfo.Shutdown));
            Receive<Terminated>(terminated => { }); // why should we care now?
        }

        #endregion

        #endregion

        #region ** Internal methods **

        #region * HandleInboundAssociation *

        private readonly Action<InboundAssociation> _handleInboundAssociationAction;
        private void HandleInboundAssociation(InboundAssociation ia) => HandleInboundAssociation(ia, false);
        private void HandleInboundAssociation(InboundAssociation ia, bool writerIsIdle)
        {
            var readonlyEndpoint = _endpoints.ReadOnlyEndpointFor(ia.Association.RemoteAddress);
            var handle = ((AkkaProtocolHandle)ia.Association);
            if (readonlyEndpoint != null)
            {
                var endpoint = readonlyEndpoint.Item1;
                if (_pendingReadHandoffs.TryGetValue(endpoint, out var protocolHandle))
                {
                    protocolHandle.Disassociate();
                }

                _pendingReadHandoffs.AddOrSet(endpoint, handle);
                endpoint.Tell(new EndpointWriter.TakeOver(handle, Self));
                var policy = _endpoints.WritableEndpointWithPolicyFor(handle.RemoteAddress);
                if (policy is Pass pass)
                {
                    pass.Endpoint.Tell(new ReliableDeliverySupervisor.Ungate());
                }
            }
            else
            {
                var remoteAddr = handle.RemoteAddress;
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
                        if (!passUid.HasValue)
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
                                CreateAndRegisterEndpoint(handle, _endpoints.RefuseUid(remoteAddr));
                            }
                        }
                        else // has a UID value
                        {
                            if (handshakeInfoUid == passUid)
                            {
                                _pendingReadHandoffs.GetOrElse(endpoint, null)?.Disassociate();
                                _pendingReadHandoffs.AddOrSet(endpoint, handle);
                                endpoint.Tell(new EndpointWriter.StopReading(endpoint, Self));
                                endpoint.Tell(new ReliableDeliverySupervisor.Ungate());
                            }
                            else
                            {
                                Context.Stop(endpoint);
                                _endpoints.UnregisterEndpoint(endpoint);
                                _pendingReadHandoffs.Remove(endpoint);
                                CreateAndRegisterEndpoint(handle, passUid);
                            }
                        }
                    }
                    else
                    {
                        CreateAndRegisterEndpoint(handle, _endpoints.RefuseUid(remoteAddr));
                    }
                }
            }
        }

        #endregion

        #region * Listens *

        private Task<List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>>> _listens;

        private Task<List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>>> Listens
        {
            get
            {
                if (_listens == null)
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
                            if (driverType == null)
                            {
                                ThrowHelper.ThrowTypeLoadException(transportSettings);
                            }
                            if (!typeof(Transport.Transport).IsAssignableFrom(driverType))
                            {
                                ThrowHelper.ThrowTypeLoadException_Transport(transportSettings);
                            }

                            var constructorInfo = driverType.GetConstructor(new[] { typeof(ActorSystem), typeof(Config) });
                            if (constructorInfo == null)
                            {
                                ThrowHelper.ThrowTypeLoadException_ActorSystem(transportSettings);
                            }

                            // ReSharper disable once AssignNullToNotNullAttribute
                            driver = (Transport.Transport)Activator.CreateInstance(driverType, args);
                        }
                        catch (Exception ex)
                        {
                            var ei = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
                            var task = new Task<List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>>>(InvokeOnListenFailFunc, ei);
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

        private static readonly Func<object, List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>>> InvokeOnListenFailFunc = InvokeOnListenFail;
        private static List<Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>> InvokeOnListenFail(object state)
        {
            ((System.Runtime.ExceptionServices.ExceptionDispatchInfo)state).Throw();
            return null;
        }

        private static readonly Func<Tuple<Address, TaskCompletionSource<IAssociationEventListener>>, AkkaProtocolTransport, Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>>> AfterListenFunc = AfterListen;
        private static Tuple<ProtocolTransportAddressPair, TaskCompletionSource<IAssociationEventListener>> AfterListen(
            Tuple<Address, TaskCompletionSource<IAssociationEventListener>> result, AkkaProtocolTransport transport)
        {
            return Tuple.Create(new ProtocolTransportAddressPair(transport, result.Item1), result.Item2);
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
                    _transportMapping[localAddr], _settings, false, handle, refuseUid: null);
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

        private void CreateAndRegisterEndpoint(AkkaProtocolHandle handle, int? refuseUid)
        {
            var localAddr = handle.LocalAddress;
            var remoteAddr = handle.RemoteAddress;
            var writing = _settings.UsePassiveConnections && !_endpoints.HasWriteableEndpointFor(remoteAddr);
            _eventPublisher.NotifyListeners(new AssociatedEvent(localAddr, remoteAddr, true));
            var endpoint = CreateEndpoint(
                remoteAddr,
                localAddr,
                _transportMapping[localAddr],
                _settings,
                writing,
                handle,
                refuseUid);

            if (writing)
            {
                _endpoints.RegisterWritableEndpoint(remoteAddr, endpoint, handle.HandshakeInfo.Uid, refuseUid);
            }
            else
            {
                _endpoints.RegisterReadOnlyEndpoint(remoteAddr, endpoint, handle.HandshakeInfo.Uid);
                if (!_endpoints.HasWriteableEndpointFor(remoteAddr))
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
            AkkaProtocolHandle handleOption = null,
            int? refuseUid = null)
        {
            System.Diagnostics.Debug.Assert(_transportMapping.ContainsKey(localAddress));
            // refuseUid is ignored for read-only endpoints since the UID of the remote system is
            // already known and has passed quarantine checks

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