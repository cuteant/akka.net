//-----------------------------------------------------------------------
// <copyright file="Endpoint.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Pattern;
using Akka.Remote.Transport;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.Collections;
using MessagePack;
using SerializedMessage = Akka.Serialization.Protocol.Payload;

namespace Akka.Remote
{
    #region == interface IInboundMessageDispatcher ==

    /// <summary>INTERNAL API</summary>
    // ReSharper disable once InconsistentNaming
    internal interface IInboundMessageDispatcher
    {
        /// <summary>TBD</summary>
        /// <param name="recipient">TBD</param>
        /// <param name="recipientAddress">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="senderOption">TBD</param>
        void Dispatch(IInternalActorRef recipient, Address recipientAddress, in SerializedMessage message, IActorRef senderOption = null);
    }

    #endregion

    #region == class DefaultMessageDispatcher ==

    /// <summary>INTERNAL API</summary>
    internal sealed class DefaultMessageDispatcher : IInboundMessageDispatcher
    {
        private readonly ActorSystem _system;
        private readonly IRemoteActorRefProvider _provider;
        private readonly ILoggingAdapter _log;
        private readonly IInternalActorRef _remoteDaemon;
        private readonly RemoteSettings _settings;

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <param name="provider">TBD</param>
        /// <param name="log">TBD</param>
        public DefaultMessageDispatcher(ActorSystem system, IRemoteActorRefProvider provider, ILoggingAdapter log)
        {
            _system = system;
            _provider = provider;
            _log = log;
            _remoteDaemon = provider.RemoteDaemon;
            _settings = provider.RemoteSettings;
        }

        #region - Dispatch -

        /// <summary>TBD</summary>
        /// <param name="recipient">TBD</param>
        /// <param name="recipientAddress">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="senderOption">TBD</param>
        public void Dispatch(IInternalActorRef recipient, Address recipientAddress, in SerializedMessage message, IActorRef senderOption = null)
        {
            var payload = _system.Deserialize(message);
            Type payloadClass = payload?.GetType();
            var sender = senderOption ?? _system.DeadLetters;
            var originalReceiver = recipient.Path;

            // message is intended for the RemoteDaemon, usually a command to create a remote actor
            if (recipient.Equals(_remoteDaemon))
            {
                if (_settings.UntrustedMode)
                {
                    _log.DroppingDaemonMessageInUntrustedMode();
                }
                else
                {
#if DEBUG
                    if (_settings.LogReceive)
                    {
                        _log.ReceivedDaemonMessage(payload, recipient, originalReceiver, sender);
                    }
#endif
                    _remoteDaemon.Tell(payload);
                }
            }
            else
            {
                switch (recipient)
                {
                    //message is intended for a local recipient
                    case ILocalRef _ when (recipient.IsLocal):
                    case RepointableActorRef _ when (recipient.IsLocal):
#if DEBUG
                        if (_settings.LogReceive)
                        {
                            _log.ReceivedLocalMessage(payload, recipient, originalReceiver, sender);
                        }
#endif
                        switch (payload)
                        {
                            case ActorSelectionMessage sel:
                                if (_settings.UntrustedMode
                                    && (!_settings.TrustedSelectionPaths.Contains(FormatActorPath(sel))
                                        || sel.Message is IPossiblyHarmful
                                        || !recipient.Equals(_provider.RootGuardian)))
                                {
                                    if (_log.IsDebugEnabled)
                                    {
                                        _log.OperatingInUntrustedMode1(sel);
                                    }
                                }
                                else
                                {
                                    //run the receive logic for ActorSelectionMessage here to make sure it is not stuck on busy user actor
                                    ActorSelection.DeliverSelection(recipient, sender, sel);
                                }
                                break;

                            case IPossiblyHarmful _ when _settings.UntrustedMode:
                                if (_log.IsDebugEnabled)
                                {
                                    _log.OperatingInUntrustedMode(payload);
                                }
                                break;

                            case ISystemMessage sysMsg:
                                recipient.SendSystemMessage(sysMsg);
                                break;

                            default:
                                recipient.Tell(payload, sender);
                                break;
                        }
                        break;

                    // message is intended for a remote-deployed recipient
                    case IRemoteRef _ when (!recipient.IsLocal && !_settings.UntrustedMode):
                    case RepointableActorRef _ when (!recipient.IsLocal && !_settings.UntrustedMode):
#if DEBUG
                        if (_settings.LogReceive)
                        {
                            _log.ReceivedRemoteMessage(payload, recipient, originalReceiver, sender);
                        }
#endif
                        if (_provider.Transport.Addresses.Contains(recipientAddress))
                        {
                            //if it was originally addressed to us but is in fact remote from our point of view (i.e. remote-deployed)
                            recipient.Tell(payload, sender);
                        }
                        else
                        {
                            _log.DroppingMsgForNonLocalRecipientArrivingAtInboundAddr(payloadClass, recipient, recipientAddress, _provider);
                        }
                        break;

                    default:
                        _log.DroppingMsgForNonLocalRecipientArrivingAtInboundAddr(payloadClass, recipient, recipientAddress, _provider);
                        break;
                }
            }
        }

        internal static string FormatActorPath(ActorSelectionMessage sel)
        {
            return sel.Elements.Join("/", "/", null);
        }

        #endregion
    }

    #endregion

    #region == Endpoint Exception Types ==

    /// <summary>INTERNAL API</summary>
    internal class EndpointException : AkkaException
    {
        /// <summary>Initializes a new instance of the <see cref="EndpointException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public EndpointException(string message, Exception cause = null) : base(message, cause) { }

#if SERIALIZATION
        /// <summary>Initializes a new instance of the <see cref="EndpointException"/> class.</summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object data about the
        /// exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information about the source
        /// or destination.
        /// </param>
        protected EndpointException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif
    }

    /// <summary>INTERNAL API</summary>
    internal interface IAssociationProblem { }

    /// <summary>INTERNAL API</summary>
    internal sealed class ShutDownAssociation : EndpointException, IAssociationProblem
    {
        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="cause">TBD</param>
        public ShutDownAssociation(string message, Address localAddress, Address remoteAddress, Exception cause = null)
            : base(message, cause)
        {
            RemoteAddress = remoteAddress;
            LocalAddress = localAddress;
        }
        private ShutDownAssociation(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>TBD</summary>
        public Address LocalAddress { get; }

        /// <summary>TBD</summary>
        public Address RemoteAddress { get; }
    }

    /// <summary>TBD</summary>
    internal sealed class InvalidAssociation : EndpointException, IAssociationProblem
    {
        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TbD</param>
        /// <param name="cause">TBD</param>
        /// <param name="disassociateInfo">TBD</param>
        public InvalidAssociation(string message, Address localAddress, Address remoteAddress, Exception cause = null, DisassociateInfo? disassociateInfo = null)
            : base(message, cause)
        {
            RemoteAddress = remoteAddress;
            LocalAddress = localAddress;
            DisassociationInfo = disassociateInfo;
        }
        private InvalidAssociation(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>TBD</summary>
        public Address LocalAddress { get; }

        /// <summary>TBD</summary>
        public Address RemoteAddress { get; }

        /// <summary>TBD</summary>
        public DisassociateInfo? DisassociationInfo { get; }
    }

    /// <summary>INTERNAL API</summary>
    internal sealed class HopelessAssociation : EndpointException, IAssociationProblem
    {
        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="uid">TBD</param>
        /// <param name="cause">TBD</param>
        public HopelessAssociation(Address localAddress, Address remoteAddress, int? uid = null, Exception cause = null)
            : base("Catastrophic association error.", cause)
        {
            RemoteAddress = remoteAddress;
            LocalAddress = localAddress;
            Uid = uid;
        }
        private HopelessAssociation(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>TBD</summary>
        public Address LocalAddress { get; private set; }

        /// <summary>TBD</summary>
        public Address RemoteAddress { get; private set; }

        /// <summary>TBD</summary>
        public int? Uid { get; private set; }
    }

    /// <summary>INTERNAL API</summary>
    internal sealed class EndpointDisassociatedException : EndpointException
    {
        /// <summary>Initializes a new instance of the <see cref="EndpointDisassociatedException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public EndpointDisassociatedException(string message) : base(message) { }
        private EndpointDisassociatedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>INTERNAL API</summary>
    internal sealed class EndpointAssociationException : EndpointException
    {
        /// <summary>Initializes a new instance of the <see cref="EndpointAssociationException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public EndpointAssociationException(string message) : base(message) { }

        /// <summary>Initializes a new instance of the <see cref="EndpointAssociationException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EndpointAssociationException(string message, Exception innerException) : base(message, innerException) { }
        private EndpointAssociationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>INTERNAL API</summary>
    internal sealed class OversizedPayloadException : EndpointException
    {
        /// <summary>Initializes a new instance of the <see cref="OversizedPayloadException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public OversizedPayloadException(string message) : base(message) { }
        private OversizedPayloadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    #endregion

    #region == class ReliableDeliverySupervisor ==

    /// <summary>INTERNAL API</summary>
    internal sealed class ReliableDeliverySupervisor : ReceiveActor2
    {
        #region - Internal message classes -

        /// <summary>TBD</summary>
        public class IsIdle : ISingletonMessage
        {
            /// <summary>TBD</summary>
            public static readonly IsIdle Instance = new IsIdle();

            private IsIdle() { }
        }

        /// <summary>TBD</summary>
        public class Idle : ISingletonMessage
        {
            /// <summary>TBD</summary>
            public static readonly Idle Instance = new Idle();

            private Idle() { }
        }

        /// <summary>TBD</summary>
        public class TooLongIdle : ISingletonMessage
        {
            /// <summary>TBD</summary>
            public static readonly TooLongIdle Instance = new TooLongIdle();

            private TooLongIdle() { }
        }

        #endregion

        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly Address _localAddress;
        private readonly Address _remoteAddress;
        private readonly int? _refuseUid;
        private readonly AkkaProtocolTransport _transport;
        private readonly RemoteSettings _settings;
        private readonly AkkaPduCodec _codec;
        private AkkaProtocolHandle _currentHandle;
        private readonly ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> _receiveBuffers;

        private PatternMatchBuilder _gatedPatterns;
        private PatternMatchBuilder _writerTerminatedGatedPatterns;
        private PatternMatchBuilder _earlyUngateRequestedGatedPatterns;
        private PatternMatchBuilder _flushWaitPatterns;
        private PatternMatchBuilder _idlePatterns;

        #region @ Constructors @

        /// <summary>TBD</summary>
        /// <param name="handleOrActive">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="receiveBuffers">TBD</param>
        public ReliableDeliverySupervisor(
            AkkaProtocolHandle handleOrActive,
            Address localAddress,
            Address remoteAddress,
            int? refuseUid,
            AkkaProtocolTransport transport,
            RemoteSettings settings,
            AkkaPduCodec codec,
            ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> receiveBuffers)
        {
            _localAddress = localAddress;
            _remoteAddress = remoteAddress;
            _refuseUid = refuseUid;
            _transport = transport;
            _settings = settings;
            _codec = codec;
            _currentHandle = handleOrActive;
            _receiveBuffers = receiveBuffers;

            _localOnlyDeciderFunc = LocalOnlyDecider;

            Reset(); // needs to be called at startup
            _writer = CreateWriter(); // need to create writer at startup
            Uid = handleOrActive != null ? (int?)handleOrActive.HandshakeInfo.Uid : null;
            UidConfirmed = Uid.HasValue;
            Receiving();
            _autoResendTimer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                _settings.SysResendTimeout, _settings.SysResendTimeout, Self, new AttemptSysMsgRedelivery(), Self);
        }

        #endregion

        private readonly ICancelable _autoResendTimer;

        /// <summary>TBD</summary>
        public int? Uid { get; set; }

        /// <summary>Processing of <see cref="Ack"/> s has to be delayed until the UID is discovered after a
        /// reconnect. Depending whether the UID matches the expected one, pending Acks can be
        /// processed or must be dropped. It is guaranteed that for any inbound connections (calling
        /// <see cref="CreateWriter"/>) the first message from that connection is <see
        /// cref="GotUid"/>, therefore it serves a separator.
        ///
        /// If we already have an inbound handle then UID is initially confirmed. (This actor is
        /// never restarted.)</summary>
        public bool UidConfirmed { get; private set; }

        private Deadline _bailoutAt = null;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(_localOnlyDeciderFunc);
        }

        private readonly Func<Exception, Directive> _localOnlyDeciderFunc;
        private Directive LocalOnlyDecider(Exception ex)
        {
            if (ex is IAssociationProblem) { return Directive.Escalate; }

            if (_log.IsWarningEnabled) _log.AssociationWithRemoteSystemHasFailed(_remoteAddress, _settings, ex);
            UidConfirmed = false; // Need confirmation of UID again
            if (_bufferWasInUse)
            {
                if ((_resendBuffer.Nacked.Count > 0 || _resendBuffer.NonAcked.Count > 0) && _bailoutAt == null)
                {
                    _bailoutAt = Deadline.Now + _settings.InitialSysMsgDeliveryTimeout;
                }
                if (_gatedPatterns == null)
                {
                    _gatedPatterns = ConfigurePatterns(() => Gated(writerTerminated: false, earlyUngateRequested: false));
                }
                Become(_gatedPatterns);
                _currentHandle = null;
                Context.Parent.Tell(new EndpointWriter.StoppedReading(Self));
                return Directive.Stop;
            }

            return Directive.Escalate;
        }

        private ICancelable _maxSilenceTimer = null;
        private AckedSendBuffer<EndpointManager.Send> _resendBuffer;
        private long _seqCounter;

        private bool _bufferWasInUse;

        private IActorRef _writer;

        #region * Reset *

        private void Reset()
        {
            _resendBuffer = new AckedSendBuffer<EndpointManager.Send>(_settings.SysMsgBufferSize);
            _seqCounter = 0L;
            _bailoutAt = null;
            _bufferWasInUse = false;
        }

        #endregion

        #region * NextSeq *

        private SeqNo NextSeq()
        {
            var tmp = _seqCounter;
            _seqCounter++;
            return new SeqNo(tmp);
        }

        #endregion

        #region - ActorBase methods and Behaviors -

        /// <summary>TBD</summary>
        protected override void PostStop()
        {
            // All remaining messages in the buffer has to be delivered to dead letters. It is
            // important to clear the sequence number otherwise deadLetters will ignore it to avoid
            // reporting system messages as dead letters while they are still possibly retransmitted.
            // Such a situation may arise when the EndpointWriter is shut down, and all of its
            // mailbox contents are delivered to dead letters. These messages should be ignored, as
            // they still live in resendBuffer and might be delivered to the remote system later.
            var deadLetters = Context.System.DeadLetters;
            foreach (var msg in _resendBuffer.Nacked.Concat(_resendBuffer.NonAcked))
            {
                deadLetters.Tell(msg.Copy(opt: null));
            }
            _receiveBuffers.TryRemove(new EndpointManager.Link(_localAddress, _remoteAddress), out _);
            _autoResendTimer.Cancel();
            _maxSilenceTimer?.Cancel();
        }

        /// <summary>N/A</summary>
        /// <param name="reason">N/A</param>
        /// <exception cref="IllegalActorStateException">
        /// This exception is thrown automatically since <see cref="ReliableDeliverySupervisor"/>
        /// must not be restarted.
        /// </exception>
        protected override void PostRestart(Exception reason)
        {
            throw new IllegalActorStateException("BUG: ReliableDeliverySupervisor has been attempted to be restarted. This must not happen.");
        }

        #region + Receiving +

        /// <summary>TBD</summary>
        /// <exception cref="HopelessAssociation">TBD</exception>
        private void Receiving()
        {
            Receive<EndpointWriter.FlushAndStop>(HandleEndpointWriterFlushAndStop);
            Receive<IsIdle>(PatternMatch<IsIdle>.EmptyAction); // Do not reply, we will Terminate soon, or send a GotUid
            Receive<EndpointManager.Send>(HandleSend);
            Receive<Ack>(HandleAck);
            Receive<AttemptSysMsgRedelivery>(HandleAttemptSysMsgRedelivery);
            Receive<Terminated>(HandleTerminated);
            Receive<GotUid>(HandleGotUid);
            Receive<EndpointWriter.StopReading>(HandleEndpointWriterStopReading);
        }

        private void HandleEndpointWriterFlushAndStop(EndpointWriter.FlushAndStop flush)
        {
            //Trying to serve until our last breath
            ResendAll();
            _writer.Tell(EndpointWriter.FlushAndStop.Instance);
            if (_flushWaitPatterns == null) { _flushWaitPatterns = ConfigurePatterns(FlushWait); }
            Become(_flushWaitPatterns);
        }

        private void HandleAck(Ack ack)
        {
            // If we are not sure about the UID just ignore the ack. Ignoring is fine.
            if (UidConfirmed)
            {
                try
                {
                    _resendBuffer = _resendBuffer.Acknowledge(ack);
                }
                catch (Exception ex)
                {
                    ThrowHopelessAssociation(ack, ex);
                }

                ResendNacked();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowHopelessAssociation(Ack ack, Exception ex)
        {
            throw GetHopelessAssociation();
            HopelessAssociation GetHopelessAssociation()
            {
                return new HopelessAssociation(_localAddress, _remoteAddress, Uid,
                    new IllegalStateException($"Error encountered while processing system message acknowledgement buffer: {_resendBuffer} ack: {ack}", ex));
            }
        }

        private void HandleAttemptSysMsgRedelivery(AttemptSysMsgRedelivery sysmsg)
        {
            if (UidConfirmed) ResendAll();
        }

        private void HandleTerminated(Terminated terminated)
        {
            _currentHandle = null;
            Context.Parent.Tell(new EndpointWriter.StoppedReading(Self));
            if (_resendBuffer.NonAcked.Count > 0 || _resendBuffer.Nacked.Count > 0)
                Context.System.Scheduler.ScheduleTellOnce(_settings.SysResendTimeout, Self,
                    new AttemptSysMsgRedelivery(), Self);
            GoToIdle();
        }

        private void HandleGotUid(GotUid g)
        {
            _bailoutAt = null;
            Context.Parent.Tell(g);
            //New system that has the same address as the old - need to start from fresh state
            UidConfirmed = true;
            if (Uid.HasValue && Uid.Value != g.Uid) Reset();
            Uid = g.Uid;
            ResendAll();
        }

        private void HandleEndpointWriterStopReading(EndpointWriter.StopReading stopped)
        {
            _writer.Forward(stopped); //forward the request
        }

        #endregion

        private void GoToIdle()
        {
            if (_bufferWasInUse && _maxSilenceTimer == null)
            {
                _maxSilenceTimer =
                    Context.System.Scheduler.ScheduleTellOnceCancelable(_settings.QuarantineSilentSystemTimeout, Self,
                        TooLongIdle.Instance, Self);
            }

            if (_idlePatterns == null) { _idlePatterns = ConfigurePatterns(IdleBehavior); }
            Become(_idlePatterns);
        }

        private void GoToActive()
        {
            _maxSilenceTimer?.Cancel();
            _maxSilenceTimer = null;
            Become(DefaultPatterns);
        }

        #region + Gated +

        /// <summary>TBD</summary>
        /// <param name="writerTerminated">TBD</param>
        /// <param name="earlyUngateRequested">TBD</param>
        /// <exception cref="HopelessAssociation">TBD</exception>
        private void Gated(bool writerTerminated, bool earlyUngateRequested)
        {
            void LocalHandleTerminated(Terminated terminated)
            {
                if (!writerTerminated)
                {
                    if (earlyUngateRequested)
                    {
                        Self.Tell(new Ungate());
                    }
                    else
                    {
                        Context.System.Scheduler.ScheduleTellOnce(_settings.RetryGateClosedFor, Self, new Ungate(), Self);
                    }
                }

                if (null == _writerTerminatedGatedPatterns) { _writerTerminatedGatedPatterns = ConfigurePatterns(() => Gated(true, earlyUngateRequested)); }
                Become(_writerTerminatedGatedPatterns);
            }
            Receive<Terminated>(LocalHandleTerminated);
            Receive<IsIdle>(HandleIsIdle);
            void LocalHandleUngate(Ungate ungate)
            {
                if (!writerTerminated)
                {
                    // Ungate was sent from EndpointManager, but we must wait for Terminated first.
                    if (_earlyUngateRequestedGatedPatterns == null) { _earlyUngateRequestedGatedPatterns = ConfigurePatterns(() => Gated(false, true)); }
                    Become(_earlyUngateRequestedGatedPatterns);
                }
                else if (_resendBuffer.NonAcked.Count > 0 || _resendBuffer.Nacked.Count > 0)
                {
                    // If we talk to a system we have not talked to before (or has given up talking
                    // to in the past) stop system delivery attempts after the specified time. This
                    // act will drop the pending system messages and gate the remote address at the
                    // EndpointManager level stopping this actor. In case the remote system becomes
                    // reachable again it will be immediately quarantined due to out-of-sync system
                    // message buffer and becomes quarantined. In other words, this action is safe.
                    if (_bailoutAt != null && _bailoutAt.IsOverdue)
                    {
                        void ThrowHopelessAssociation()
                        {
                            throw GetHopelessAssociation();
                        }
                        HopelessAssociation GetHopelessAssociation()
                        {
                            return new HopelessAssociation(_localAddress, _remoteAddress, Uid,
                                new TimeoutException("Delivery of system messages timed out and they were dropped"));
                        }
                        ThrowHopelessAssociation();
                    }

                    _writer = CreateWriter();
                    //Resending will be triggered by the incoming GotUid message after the connection finished
                    GoToActive();
                }
                else
                {
                    GoToIdle();
                }
            }
            Receive<Ungate>(LocalHandleUngate);
            Receive<AttemptSysMsgRedelivery>(PatternMatch<AttemptSysMsgRedelivery>.EmptyAction); // Ignore
            Receive<EndpointManager.Send>(send => send.Message is ISystemMessage, send => TryBuffer(send.Copy(NextSeq())));
            Receive<EndpointManager.Send>(send => Context.System.DeadLetters.Tell(send));
            Receive<EndpointWriter.FlushAndStop>(flush => Context.Stop(Self));
            Receive<EndpointWriter.StopReading>(HandleEndpointWriterStopReadingGated);
        }

        private void HandleEndpointWriterStopReadingGated(EndpointWriter.StopReading stop)
        {
            stop.ReplyTo.Tell(new EndpointWriter.StoppedReading(stop.Writer));
            Sender.Tell(new EndpointWriter.StoppedReading(stop.Writer));
        }

        #endregion

        #region + IdleBehavior +

        /// <summary>TBD</summary>
        private void IdleBehavior()
        {
            Receive<IsIdle>(HandleIsIdle);
            Receive<EndpointManager.Send>(HandleEndpointManagerSendIdle);

            Receive<AttemptSysMsgRedelivery>(HandleAttemptSysMsgRedeliveryIdle);
            Receive<TooLongIdle>(HandleTooLongIdle);
            Receive<EndpointWriter.FlushAndStop>(HandleEndpointWriterFlushAndStopIdle);
            Receive<EndpointWriter.StopReading>(HandleEndpointWriterStopReadingIdle);
        }

        private void HandleIsIdle(IsIdle idle)
        {
            Sender.Tell(Idle.Instance);
        }

        private void HandleEndpointManagerSendIdle(EndpointManager.Send send)
        {
            _writer = CreateWriter();
            //Resending will be triggered by the incoming GotUid message after the connection finished
            HandleSend(send);
            GoToActive();
        }

        private void HandleAttemptSysMsgRedeliveryIdle(AttemptSysMsgRedelivery sys)
        {
            if (_resendBuffer.Nacked.Count > 0 || _resendBuffer.NonAcked.Count > 0)
            {
                _writer = CreateWriter();
                //Resending will be triggered by the incoming GotUid message after the connection finished
                GoToActive();
            }
        }

        private void HandleEndpointWriterFlushAndStopIdle(EndpointWriter.FlushAndStop stop)
        {
            Context.Stop(Self);
        }

        private void HandleEndpointWriterStopReadingIdle(EndpointWriter.StopReading stop)
        {
            stop.ReplyTo.Tell(new EndpointWriter.StoppedReading(stop.Writer));
        }

        #endregion

        #region + FlushWait +

        /// <summary>TBD</summary>
        private void FlushWait()
        {
            Receive<IsIdle>(PatternMatch<IsIdle>.EmptyAction); // Do not reply, we will Terminate soon, which will do the inbound connection unstashing
            Receive<Terminated>(HandleTerminatedFlushWait);
            ReceiveAny(PatternMatch<object>.EmptyAction); // ignore
        }

        private void HandleTerminatedFlushWait(Terminated terminated)
        {
            //Clear buffer to prevent sending system messages to dead letters -- at this point we are shutting down and
            //don't know if they were properly delivered or not
            _resendBuffer = new AckedSendBuffer<EndpointManager.Send>(0);
            Context.Stop(Self);
        }

        #endregion

        #endregion

        #region - Static methods and Internal Message Types -

        /// <summary>TBD</summary>
        public class AttemptSysMsgRedelivery { }

        /// <summary>TBD</summary>
        public class Ungate { }

        /// <summary>TBD</summary>
        public sealed class GotUid
        {
            /// <summary>TBD</summary>
            /// <param name="uid">TBD</param>
            /// <param name="remoteAddress">TBD</param>
            public GotUid(int uid, Address remoteAddress)
            {
                Uid = uid;
                RemoteAddress = remoteAddress;
            }

            /// <summary>TBD</summary>
            public int Uid { get; }

            /// <summary>TBD</summary>
            public Address RemoteAddress { get; }
        }

        /// <summary>TBD</summary>
        /// <param name="handleOrActive">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="receiveBuffers">TBD</param>
        /// <param name="dispatcher">TBD</param>
        /// <returns>TBD</returns>
        public static Props ReliableDeliverySupervisorProps(
                    AkkaProtocolHandle handleOrActive,
                    Address localAddress,
                    Address remoteAddress,
                    int? refuseUid,
                    AkkaProtocolTransport transport,
                    RemoteSettings settings,
                    AkkaPduCodec codec,
                    ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> receiveBuffers,
                    string dispatcher)
        {
            return
                Props.Create(
                    () =>
                        new ReliableDeliverySupervisor(handleOrActive, localAddress, remoteAddress, refuseUid, transport,
                            settings, codec, receiveBuffers))
                    .WithDispatcher(dispatcher);
        }

        #endregion

        #region * HandleTooLongIdle *

        // Extracted this method to solve a compiler issue with `Receive<TooLongIdle>`
        private void HandleTooLongIdle(TooLongIdle idle)
        {
            throw GetHopelessAssociation();
            HopelessAssociation GetHopelessAssociation()
            {
                return new HopelessAssociation(_localAddress, _remoteAddress, Uid,
                    new TimeoutException("Delivery of system messages timed out and they were dropped"));
            }
        }

        #endregion

        #region * HandleSend *

        private void HandleSend(EndpointManager.Send send)
        {
            if (send.Message is ISystemMessage)
            {
                var sequencedSend = send.Copy(NextSeq());
                TryBuffer(sequencedSend);
                // If we have not confirmed the remote UID we cannot transfer the system message at
                // this point just buffer it. GotUid will kick ResendAll() causing the messages to be
                // properly written. Flow control by not sending more when we already have many outstanding.
                if (UidConfirmed && _resendBuffer.NonAcked.Count <= _settings.SysResendLimit)
                {
                    _writer.Tell(sequencedSend);
                }
            }
            else
            {
                _writer.Tell(send);
            }
        }

        #endregion

        #region * ResendNacked *

        private void ResendNacked()
        {
            _resendBuffer.Nacked.ForEach(nacked => _writer.Tell(nacked));
        }

        #endregion

        #region * ResendAll *

        private void ResendAll()
        {
            ResendNacked();
            _resendBuffer.NonAcked.Take(_settings.SysResendLimit).ForEach(nonacked => _writer.Tell(nonacked));
        }

        #endregion

        #region * TryBuffer *

        private void TryBuffer(EndpointManager.Send s)
        {
            try
            {
                _resendBuffer = _resendBuffer.Buffer(s);
                _bufferWasInUse = true;
            }
            catch (Exception ex)
            {
                ThrowHopelessAssociation(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowHopelessAssociation(Exception ex)
        {
            throw GetHopelessAssociation();

            HopelessAssociation GetHopelessAssociation()
            {
                return new HopelessAssociation(_localAddress, _remoteAddress, Uid, ex);
            }
        }

        #endregion

        #region * Writer create *

        private IActorRef CreateWriter()
        {
            var context = Context;
            var actorSystem = context.System;
            var writer =
                context.ActorOf(RARP.For(actorSystem)
                    .ConfigureDispatcher(
                        EndpointWriter.EndpointWriterProps(_currentHandle, _localAddress, _remoteAddress, _refuseUid, _transport,
                            _settings, new AkkaPduMessagePackCodec(actorSystem), _receiveBuffers, Self)
                            .WithDeploy(Deploy.Local)),
                    "endpointWriter");
            context.Watch(writer);
            return writer;
        }

        #endregion
    }

    #endregion

    #region == class EndpointActor ==

    /// <summary>Abstract base class for <see cref="EndpointReader"/> classes</summary>
    internal abstract class EndpointActor : ReceiveActor2
    {
        /// <summary>TBD</summary>
        protected readonly Address LocalAddress;

        /// <summary>TBD</summary>
        protected Address RemoteAddress;

        /// <summary>TBD</summary>
        protected RemoteSettings Settings;

        /// <summary>TBD</summary>
        protected AkkaProtocolTransport Transport;

        private readonly ILoggingAdapter _log = Context.GetLogger();

        /// <summary>TBD</summary>
        protected readonly EventPublisher EventPublisher;

        /// <summary>TBD</summary>
        protected bool Inbound { get; set; }

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        protected EndpointActor(Address localAddress, Address remoteAddress, AkkaProtocolTransport transport,
            RemoteSettings settings)
        {
            EventPublisher = new EventPublisher(Context.System, _log, Logging.LogLevelFor(settings.RemoteLifecycleEventsLogLevel));
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            Transport = transport;
            Settings = settings;
        }

        #region + Event publishing methods +

        /// <summary>TBD</summary>
        /// <param name="ex">TBD</param>
        /// <param name="level">TBD</param>
        protected void PublishError(Exception ex, LogLevel level)
            => TryPublish(new AssociationErrorEvent(ex, LocalAddress, RemoteAddress, Inbound, level));

        /// <summary>TBD</summary>
        protected void PublishDisassociated()
            => TryPublish(new DisassociatedEvent(LocalAddress, RemoteAddress, Inbound));

        private void TryPublish(RemotingLifecycleEvent ev)
        {
            try
            {
                EventPublisher.NotifyListeners(ev);
            }
            catch (Exception ex)
            {
                _log.UnableToPublishErrorEventToEventStream(ex);
            }
        }

        #endregion
    }

    #endregion

    #region == class EndpointWriter ==

    /// <summary>INTERNAL API</summary>
    internal sealed class EndpointWriter : EndpointActor
    {
        #region @ Constructors @

        /// <summary>TBD</summary>
        /// <param name="handleOrActive">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="receiveBuffers">TBD</param>
        /// <param name="reliableDeliverySupervisor">TBD</param>
        public EndpointWriter(
            AkkaProtocolHandle handleOrActive,
            Address localAddress,
            Address remoteAddress,
            int? refuseUid,
            AkkaProtocolTransport transport,
            RemoteSettings settings,
            AkkaPduCodec codec,
            ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> receiveBuffers,
            IActorRef reliableDeliverySupervisor = null)
            : base(localAddress, remoteAddress, transport, settings)
        {
            _refuseUid = refuseUid;
            _codec = codec;
            _reliableDeliverySupervisor = reliableDeliverySupervisor;
            _system = Context.System;
            _provider = RARP.For(_system).Provider;
            _msgDispatcher = new DefaultMessageDispatcher(_system, _provider, _log);
            _receiveBuffers = receiveBuffers;
            Inbound = handleOrActive != null;
            _ackDeadline = NewAckDeadline();
            _handle = handleOrActive;
            _remoteMetrics = RemoteMetricsExtension.Create(_system.AsInstanceOf<ExtendedActorSystem>());

            _localOnlyDeciderFunc = LocalOnlyDecider;
            _writeSendFunc = new Predicate<EndpointManager.Send>(WriteSend);
            _sendSuccessFunc = new Predicate<object>(SendSuccess);

            if (_handle == null)
            {
                Initializing();
            }
            else
            {
                defaultPatternUseWriting = true;
                Writing();
            }
        }

        #endregion

        #region @ Fields @

        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly int? _refuseUid;
        private readonly AkkaPduCodec _codec;
        private readonly IActorRef _reliableDeliverySupervisor;
        private readonly ActorSystem _system;
        private readonly IRemoteActorRefProvider _provider;
        private readonly ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> _receiveBuffers;
        private DisassociateInfo _stopReason = DisassociateInfo.Unknown;

        private IActorRef _reader;
        private readonly AtomicCounter _readerId = new AtomicCounter(0);
        private readonly IInboundMessageDispatcher _msgDispatcher;

        private Ack _lastAck = null;
        private Deadline _ackDeadline;
        private AkkaProtocolHandle _handle;

        private ICancelable _ackIdleTimerCancelable;

        // Use an internal buffer instead of Stash for efficiency stash/unstashAll is slow when many
        // messages are stashed
        // IMPORTANT: sender is not stored, so .Sender and forward must not be used in EndpointWriter
        private readonly Deque<object> _buffer = new Deque<object>();

        //buffer for IPriorityMessages - ensures that heartbeats get delivered before user-defined messages
        private readonly Deque<EndpointManager.Send> _prioBuffer = new Deque<EndpointManager.Send>();

        private long _largeBufferLogTimestamp = MonotonicClock.GetNanos();

        private readonly IRemoteMetrics _remoteMetrics;

        #endregion

        #region * Patterns *

        private PatternMatchBuilder _handoffPatterns;

        private PatternMatchBuilder _bufferingPatterns;
        private PatternMatchBuilder BufferingPatterns
        {
            get
            {
                if (null == _bufferingPatterns) { _bufferingPatterns = ConfigurePatterns(Buffering); }
                return _bufferingPatterns;
            }
        }

        private bool defaultPatternUseWriting;
        private PatternMatchBuilder _writingPatterns;
        private PatternMatchBuilder WritingPatterns
        {
            get
            {
                if (defaultPatternUseWriting) { return DefaultPatterns; }
                if (null == _writingPatterns) { _writingPatterns = ConfigurePatterns(Writing); }
                return _writingPatterns;
            }
        }

        #endregion

        #region - ActorBase methods -

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(_localOnlyDeciderFunc);
        }

        private readonly Func<Exception, Directive> _localOnlyDeciderFunc;
        private Directive LocalOnlyDecider(Exception ex)
        {
            PublishAndThrow(ex, LogLevel.ErrorLevel, false);
            return Directive.Escalate;
        }

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        /// <exception cref="IllegalActorStateException">TBD</exception>
        protected override void PostRestart(Exception reason)
            => throw new IllegalActorStateException("EndpointWriter must not be restarted");

        /// <summary>TBD</summary>
        protected override void PreStart()
        {
            if (_handle == null)
            {
                AssociateAsync().PipeTo(Self);
            }
            else
            {
                _reader = StartReadEndpoint(_handle);
            }

            var ackIdleInterval = new TimeSpan(Settings.SysMsgAckTimeout.Ticks / 2);
            _ackIdleTimerCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(ackIdleInterval, ackIdleInterval, Self, AckIdleCheckTimer.Instance, Self);
        }

        private async Task<object> AssociateAsync()
        {
            try
            {
                return new Handle(await Transport.Associate(RemoteAddress, _refuseUid).ConfigureAwait(false));
            }
            catch (Exception e)
            {
                return new Status.Failure(e.InnerException ?? e);
            }
        }

        /// <summary>TBD</summary>
        protected override void PostStop()
        {
            _ackIdleTimerCancelable.CancelIfNotNull();

            while (_prioBuffer.TryRemoveFromFront(out var msg))
            {
                _system.DeadLetters.Tell(msg);
            }

            while (_buffer.TryRemoveFromFront(out var msg))
            {
                _system.DeadLetters.Tell(msg);
            }

            if (_handle != null) _handle.Disassociate(_stopReason);
            EventPublisher.NotifyListeners(new DisassociatedEvent(LocalAddress, RemoteAddress, Inbound));
        }

        #endregion

        #region - Receives -

        private void Initializing()
        {
            Receive<EndpointManager.Send>(HandleEndpointManagerSend);
            Receive<Status.Failure>(HandleStatusFailure);
            Receive<Handle>(HandleHandle);
        }

        private void Buffering()
        {
            Receive<EndpointManager.Send>(HandleEndpointManagerSend);
            Receive<BackoffTimer>(HandleBackoffTimer);
            Receive<FlushAndStop>(HandleFlushAndStopBuffering);
            Receive<FlushAndStopTimeout>(HandleFlushAndStopTimeout);
        }

        private void Writing()
        {
            Receive<EndpointManager.Send>(HandSendWriting);
            Receive<FlushAndStop>(HandleFlushAndStopWriting);
            Receive<AckIdleCheckTimer>(HandleAckIdleCheckTimer);
        }

        private void Handoff()
        {
            Receive<Terminated>(HandleTerminated);
            Receive<EndpointManager.Send>(HandleEndpointManagerSend);
        }

        private void HandSendWriting(EndpointManager.Send s)
        {
            if (!WriteSend(s))
            {
                if (s.Seq == null) EnqueueInBuffer(s);
                ScheduleBackoffTimer();
                Become(BufferingPatterns);
            }
        }

        private void HandleAckIdleCheckTimer(AckIdleCheckTimer ack)
        {
            if (_ackDeadline.IsOverdue)
            {
                TrySendPureAck();
            }
        }

        private void HandleFlushAndStopWriting(FlushAndStop flush)
        {
            DoFlushAndStop();
        }

        private void HandleFlushAndStopTimeout(FlushAndStopTimeout timeout)
        {
            // enough, ready to flush
            DoFlushAndStop();
        }

        private void HandleFlushAndStopBuffering(FlushAndStop stop)
        {
            _buffer.AddToBack(stop); //Flushing is postponed after the pending writes
            Context.System.Scheduler.ScheduleTellOnce(Settings.FlushWait, Self, FlushAndStopTimeout.Instance, Self);
        }

        private void HandleBackoffTimer(BackoffTimer backoff)
        {
            SendBufferedMessages();
        }

        private void HandleHandle(Handle handle)
        {
            // Assert handle == None?
            Context.Parent.Tell(
                new ReliableDeliverySupervisor.GotUid((int)handle.ProtocolHandle.HandshakeInfo.Uid, RemoteAddress));
            _handle = handle.ProtocolHandle;
            _reader = StartReadEndpoint(_handle);
            EventPublisher.NotifyListeners(new AssociatedEvent(LocalAddress, RemoteAddress, Inbound));
            BecomeWritingOrSendBufferedMessages();
        }

        private void HandleTerminated(Terminated terminated)
        {
            _reader = StartReadEndpoint(_handle);
            BecomeWritingOrSendBufferedMessages();
        }

        private void HandleEndpointManagerSend(EndpointManager.Send send)
        {
            EnqueueInBuffer(send);
        }

        private void HandleStatusFailure(Status.Failure failure)
        {
            if (failure.Cause is InvalidAssociationException)
            {
                if (failure.Cause.InnerException == null)
                {
                    PublishAndThrow(new InvalidAssociation(failure.Cause.Message, LocalAddress, RemoteAddress), LogLevel.WarningLevel);
                }
            }

            PublishAndThrow(new InvalidAssociation($"Association failed with {RemoteAddress}", LocalAddress, RemoteAddress, failure.Cause), LogLevel.WarningLevel);
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        protected override void Unhandled(object message)
        {
            switch (message)
            {
                case Terminated t:
                    if (_reader == null || t.ActorRef.Equals(_reader))
                    {
                        PublishAndThrow(ThrowHelper.GetEndpointDisassociatedException_Disassociated(), LogLevel.DebugLevel);
                    }
                    break;

                case StopReading stop:
                    if (_reader != null)
                    {
                        _reader.Tell(stop, stop.ReplyTo);
                    }
                    else
                    {
                        // initializing, buffer and take care of it later when buffer is sent
                        EnqueueInBuffer(message);
                    }
                    break;

                case TakeOver takeover:
                    // Shutdown old reader
                    _handle.Disassociate();
                    _handle = takeover.ProtocolHandle;
                    takeover.ReplyTo.Tell(new TookOver(Self, _handle));
                    if (null == _handoffPatterns) { _handoffPatterns = ConfigurePatterns(Handoff); }
                    Become(_handoffPatterns);
                    break;

                case FlushAndStop _:
                    _stopReason = DisassociateInfo.Shutdown;
                    Context.Stop(Self);
                    break;

                case OutboundAck ack:
                    _lastAck = ack.Ack;
                    if (_ackDeadline.IsOverdue) { TrySendPureAck(); }
                    break;

                case AckIdleCheckTimer _:
                case FlushAndStopTimeout _:
                case BackoffTimer _:
                    //ignore
                    break;

                default:
                    base.Unhandled(message);
                    break;
            }
        }

        #endregion

        #region - Internal methods -

        private Deadline NewAckDeadline() => Deadline.Now + Settings.SysMsgAckTimeout;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PublishAndThrow(Exception reason, LogLevel level, bool needToThrow = true)
        {
            switch (reason)
            {
                case EndpointDisassociatedException _:
                    PublishDisassociated();
                    break;
                case ShutDownAssociation _:
                    // don't log an error for planned shutdowns
                    break;
                default:
                    PublishError(reason, level);
                    break;
            }
            if (needToThrow)
            {
                throw reason;
            }
        }

        private IActorRef StartReadEndpoint(AkkaProtocolHandle handle)
        {
            var newReader =
                Context.ActorOf(RARP.For(Context.System)
                    .ConfigureDispatcher(
                        EndpointReader.ReaderProps(LocalAddress, RemoteAddress, Transport, Settings, _codec, _msgDispatcher,
                            Inbound, (int)handle.HandshakeInfo.Uid, _receiveBuffers, _reliableDeliverySupervisor)
                            .WithDeploy(Deploy.Local)),
                    $"endpointReader-{AddressUrlEncoder.Encode(RemoteAddress)}-{_readerId.Next()}");
            Context.Watch(newReader);
            handle.ReadHandlerSource.SetResult(new ActorHandleEventListener(newReader));
            return newReader;
        }

        /// <summary>Serializes the outbound message going onto the wire.</summary>
        /// <param name="msg">The C# object we intend to serialize.</param>
        /// <returns>The Akka.NET envelope containing the serialized message and addressing information.</returns>
        /// <remarks>Differs from JVM implementation due to Scala implicits.</remarks>
        private SerializedMessage SerializeMessage(object msg)
        {
            if (_handle == null)
            {
                ThrowHelper.ThrowEndpointException_SerializeMessage();
            }
            return MessageSerializer.Serialize(_system, _handle.LocalAddress, msg);
        }

        private int _writeCount = 0;
        private int _maxWriteCount = MaxWriteCount;
        private long _adaptiveBackoffNanos = 1000000L; // 1 ms
        private bool _fullBackoff = false;

        // FIXME remove these counters when tuning/testing is completed
        private int _fullBackoffCount = 1;

        private int _smallBackoffCount = 0;
        private int _noBackoffCount = 0;

        private void AdjustAdaptiveBackup()
        {
            _maxWriteCount = Math.Max(_writeCount, _maxWriteCount);
            if (_writeCount <= SendBufferBatchSize)
            {
                _fullBackoff = true;
                _adaptiveBackoffNanos = Math.Min(Convert.ToInt64(_adaptiveBackoffNanos * 1.2), MaxAdaptiveBackoffNanos);
            }
            else if (_writeCount >= _maxWriteCount * 0.6)
            {
                _adaptiveBackoffNanos = Math.Max(Convert.ToInt64(_adaptiveBackoffNanos * 0.9), MinAdaptiveBackoffNanos);
            }
            else if (_writeCount <= _maxWriteCount * 0.2)
            {
                _adaptiveBackoffNanos = Math.Min(Convert.ToInt64(_adaptiveBackoffNanos * 1.1), MaxAdaptiveBackoffNanos);
            }
            _writeCount = 0;
        }

        private void ScheduleBackoffTimer()
        {
            if (_fullBackoff)
            {
                _fullBackoffCount += 1;
                _fullBackoff = false;
                Context.System.Scheduler.ScheduleTellOnce(Settings.BackoffPeriod, Self, BackoffTimer.Instance, Self, null);
            }
            else
            {
                _smallBackoffCount += 1;
                var backoffDeadlineNanoTime = TimeSpan.FromTicks(_adaptiveBackoffNanos.ToTicks());

                Context.System.Scheduler.ScheduleTellOnce(backoffDeadlineNanoTime, Self, BackoffTimer.Instance, Self);
            }
        }

        private void DoFlushAndStop()
        {
            //Try to send last Ack message
            TrySendPureAck();
            _stopReason = DisassociateInfo.Shutdown;
            Context.Stop(Self);
        }

        private void TrySendPureAck()
        {
            if (_handle != null && _lastAck != null)
            {
                if (_handle.Write(_codec.ConstructPureAck(_lastAck)))
                {
                    _ackDeadline = NewAckDeadline();
                    _lastAck = null;
                }
            }
        }

        private void EnqueueInBuffer(object message)
        {
            var send = message as EndpointManager.Send;
            switch (send.Message)
            {
                case IPriorityMessage _:
                case ActorSelectionMessage selMsg when selMsg.Message is IPriorityMessage:
                    _prioBuffer.AddToBack(send);
                    break;

                default:
                    _buffer.AddToBack(message);
                    break;
            }
        }

        private void BecomeWritingOrSendBufferedMessages()
        {
            if (_buffer.IsEmpty)
            {
                Become(WritingPatterns);
            }
            else
            {
                Become(BufferingPatterns);
                SendBufferedMessages();
            }
        }

        private readonly Predicate<EndpointManager.Send> _writeSendFunc;
        private bool WriteSend(EndpointManager.Send send)
        {
            try
            {
                if (_handle == null)
                {
                    ThrowHelper.ThrowEndpointException_WriteSend();
                }

#if DEBUG
                if (_provider.RemoteSettings.LogSend)
                {
                    _log.SendRemoteMessage(send, _system);
                }
#endif

                var recipient = send.Recipient;
                var pdu = _codec.ConstructMessage(recipient.LocalAddressToUse, recipient,
                    this.SerializeMessage(send.Message), send.SenderOption, send.Seq, _lastAck);

#if DEBUG
                // Only for RemoteMetricsSpec
                if (send.Message is byte[] testMsg)
                {
                    _remoteMetrics.LogPayloadBytes(send.Message, testMsg.Length);
                }
#endif
                //_remoteMetrics.LogPayloadBytes(send.Message, pdu.Length);

                //if (pdu.Length > Transport.MaximumPayloadBytes)
                //{
                //    var reason = new OversizedPayloadException(
                //        string.Format("Discarding oversized payload sent to {0}: max allowed size {1} bytes, actual size of encoded {2} was {3} bytes.",
                //            send.Recipient,
                //            Transport.MaximumPayloadBytes,
                //            send.Message.GetType(),
                //            pdu.Length));
                //    _log.Error(reason, "Transient association error (association remains live)");
                //    return true;
                //}
                //else
                //{
                var ok = _handle.Write(pdu);

                if (ok)
                {
                    _ackDeadline = NewAckDeadline();
                    _lastAck = null;
                    return true;
                }
                //}
                return false;
            }
            catch (SerializationException ex)
            {
                _log.TransientAssociationErrorAassociationRemainsLive(ex);
                return true;
            }
            catch (EndpointException ex)
            {
                PublishAndThrow(ex, LogLevel.ErrorLevel);
            }
            catch (Exception ex)
            {
                PublishAndThrow(ThrowHelper.GetEndpointException_FailedToWriteMessageToTheTransport(ex),
                    LogLevel.ErrorLevel);
            }

            return false;
        }

        private readonly Predicate<object> _sendSuccessFunc;
        private bool SendSuccess(object msg)
        {
            switch (msg)
            {
                case EndpointManager.Send s:
                    return WriteSend(s);

                case FlushAndStop _:
                    DoFlushAndStop();
                    return false;

                case StopReading stop:
                    _reader?.Tell(stop, stop.ReplyTo);
                    return true;

                default:
                    return true;
            }
        }

        private void SendBufferedMessages()
        {
            bool WriteLoop(int count)
            {
                if (count > 0 && _buffer.NonEmpty)
                {
                    if (_buffer.TryRemoveFromFrontIf(_sendSuccessFunc, out _))
                    {
                        _writeCount += 1;
                        return WriteLoop(count - 1);
                    }
                    return false;
                }

                return true;
            }

            bool WritePrioLoop()
            {
                if (_prioBuffer.IsEmpty) { return true; }
                if (_prioBuffer.TryRemoveFromFrontIf(_writeSendFunc, out _))
                {
                    return WritePrioLoop();
                }
                return false;
            }

            var ok = WritePrioLoop() && WriteLoop(SendBufferBatchSize);
            if (_buffer.IsEmpty && _prioBuffer.IsEmpty)
            {
                // FIXME remove this when testing/tuning is completed
                if (_log.IsDebugEnabled)
                {
                    _log.DrainedBufferWithMaxWriteCount(_maxWriteCount, _fullBackoffCount, _smallBackoffCount, _noBackoffCount, _adaptiveBackoffNanos);
                }
                _fullBackoffCount = 1;
                _smallBackoffCount = 0;
                _noBackoffCount = 0;
                _writeCount = 0;
                _maxWriteCount = MaxWriteCount;
                Become(WritingPatterns);
            }
            else if (ok)
            {
                _noBackoffCount += 1;
                Self.Tell(BackoffTimer.Instance);
            }
            else
            {
                var size = _buffer.Count;
                if (size > Settings.LogBufferSizeExceeding)
                {
                    var now = MonotonicClock.GetNanos();
                    if (now - _largeBufferLogTimestamp >= LogBufferSizeInterval)
                    {
                        if (_log.IsWarningEnabled) _log.BufferedMessagesInEndpointWriterFor(size, RemoteAddress);
                        _largeBufferLogTimestamp = now;
                    }
                }
            }

            AdjustAdaptiveBackup();
            ScheduleBackoffTimer();
        }

        #endregion

        #region - Static methods and Internal messages -

        // These settings are not configurable because wrong configuration will break the auto-tuning
        private const int SendBufferBatchSize = 5;

        private const long MinAdaptiveBackoffNanos = 300000L; // 0.3 ms
        private const long MaxAdaptiveBackoffNanos = 2000000L; // 2 ms
        private const long LogBufferSizeInterval = 5000000000L; // 5 s, in nanoseconds
        private const int MaxWriteCount = 50;

        /// <summary>TBD</summary>
        /// <param name="handleOrActive">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="receiveBuffers">TBD</param>
        /// <param name="reliableDeliverySupervisor">TBD</param>
        /// <returns>TBD</returns>
        public static Props EndpointWriterProps(AkkaProtocolHandle handleOrActive, Address localAddress,
                    Address remoteAddress, int? refuseUid, AkkaProtocolTransport transport, RemoteSettings settings,
                    AkkaPduCodec codec, ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> receiveBuffers, IActorRef reliableDeliverySupervisor = null)
        {
            return Props.Create(
                () =>
                    new EndpointWriter(handleOrActive, localAddress, remoteAddress, refuseUid, transport, settings,
                        codec, receiveBuffers, reliableDeliverySupervisor));
        }

        /// <summary>This message signals that the current association maintained by the local <see
        /// cref="EndpointWriter"/> and <see cref="EndpointReader"/> is to be overridden by a new
        /// inbound association. This is needed to avoid parallel inbound associations from the same
        /// remote endpoint: when a parallel inbound association is detected, the old one is removed
        /// and the new one is used instead.</summary>
        public sealed class TakeOver : INoSerializationVerificationNeeded
        {
            /// <summary>Create a new TakeOver command</summary>
            /// <param name="protocolHandle">The handle of the new association</param>
            /// <param name="replyTo">TBD</param>
            public TakeOver(AkkaProtocolHandle protocolHandle, IActorRef replyTo)
            {
                ProtocolHandle = protocolHandle;
                ReplyTo = replyTo;
            }

            /// <summary>TBD</summary>
            public AkkaProtocolHandle ProtocolHandle { get; }

            /// <summary>TBD</summary>
            public IActorRef ReplyTo { get; }
        }

        /// <summary>TBD</summary>
        public sealed class TookOver : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="writer">TBD</param>
            /// <param name="protocolHandle">TBD</param>
            public TookOver(IActorRef writer, AkkaProtocolHandle protocolHandle)
            {
                ProtocolHandle = protocolHandle;
                Writer = writer;
            }

            /// <summary>TBD</summary>
            public IActorRef Writer { get; }

            /// <summary>TBD</summary>
            public AkkaProtocolHandle ProtocolHandle { get; }
        }

        /// <summary>TBD</summary>
        public sealed class BackoffTimer : ISingletonMessage
        {
            private BackoffTimer() { }

            /// <summary>TBD</summary>
            public static readonly BackoffTimer Instance = new BackoffTimer();
        }

        /// <summary>TBD</summary>
        public sealed class FlushAndStop : ISingletonMessage
        {
            private FlushAndStop() { }

            /// <summary>TBD</summary>
            public static readonly FlushAndStop Instance = new FlushAndStop();
        }

        /// <summary>TBD</summary>
        public sealed class AckIdleCheckTimer : ISingletonMessage
        {
            private AckIdleCheckTimer() { }

            /// <summary>TBD</summary>
            public static readonly AckIdleCheckTimer Instance = new AckIdleCheckTimer();
        }

        private sealed class FlushAndStopTimeout : ISingletonMessage
        {
            private FlushAndStopTimeout() { }

            public static readonly FlushAndStopTimeout Instance = new FlushAndStopTimeout();
        }

        /// <summary>TBD</summary>
        public sealed class Handle : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="protocolHandle">TBD</param>
            public Handle(AkkaProtocolHandle protocolHandle) => ProtocolHandle = protocolHandle;

            /// <summary>TBD</summary>
            public AkkaProtocolHandle ProtocolHandle { get; }
        }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class StopReading
        {
            /// <summary>TBD</summary>
            /// <param name="writer">TBD</param>
            /// <param name="replyTo">TBD</param>
            [SerializationConstructor]
            public StopReading(IActorRef writer, IActorRef replyTo)
            {
                Writer = writer;
                ReplyTo = replyTo;
            }

            /// <summary>TBD</summary>
            [Key(0)]
            public readonly IActorRef Writer;

            /// <summary>TBD</summary>
            [Key(1)]
            public readonly IActorRef ReplyTo;
        }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class StoppedReading
        {
            /// <summary>TBD</summary>
            /// <param name="writer">TBD</param>
            [SerializationConstructor]
            public StoppedReading(IActorRef writer) => Writer = writer;

            /// <summary>TBD</summary>
            [Key(0)]
            public readonly IActorRef Writer;
        }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class OutboundAck
        {
            /// <summary>TBD</summary>
            /// <param name="ack">TBD</param>
            [SerializationConstructor]
            public OutboundAck(Ack ack) => Ack = ack;

            /// <summary>TBD</summary>
            [Key(0)]
            public readonly Ack Ack;
        }

        private const string AckIdleTimerName = "AckIdleTimer";

        #endregion
    }

    #endregion

    #region == class EndpointReader ==

    /// <summary>INTERNAL API</summary>
    internal sealed class EndpointReader : EndpointActor
    {
        #region @ Constructors @

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="msgDispatch">TBD</param>
        /// <param name="inbound">TBD</param>
        /// <param name="uid">TBD</param>
        /// <param name="receiveBuffers">TBD</param>
        /// <param name="reliableDeliverySupervisor">TBD</param>
        public EndpointReader(
            Address localAddress,
            Address remoteAddress,
            AkkaProtocolTransport transport,
            RemoteSettings settings,
            AkkaPduCodec codec,
            IInboundMessageDispatcher msgDispatch,
            bool inbound,
            int uid,
            ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> receiveBuffers,
            IActorRef reliableDeliverySupervisor = null)
            : base(localAddress, remoteAddress, transport, settings)
        {
            _receiveBuffers = receiveBuffers;
            _msgDispatch = msgDispatch;
            Inbound = inbound;
            _uid = uid;
            _reliableDeliverySupervisor = reliableDeliverySupervisor;
            _codec = codec;
            _provider = RARP.For(Context.System).Provider;
            Reading();
        }

        #endregion

        #region @ Fields @

        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly AkkaPduCodec _codec;
        private readonly IActorRef _reliableDeliverySupervisor;
        private readonly ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> _receiveBuffers;
        private readonly int _uid;
        private readonly IInboundMessageDispatcher _msgDispatch;

        private readonly IRemoteActorRefProvider _provider;
        private AckedReceiveBuffer<Message> _ackedReceiveBuffer = new AckedReceiveBuffer<Message>();

        private PatternMatchBuilder _notReadingPatterns;

        #endregion

        #region + ActorBase overrides +

        /// <summary>TBD</summary>
        protected override void PreStart()
        {
            if (_receiveBuffers.TryGetValue(new EndpointManager.Link(LocalAddress, RemoteAddress), out var resendState))
            {
                _ackedReceiveBuffer = resendState.Buffer;
                DeliverAndAck();
            }
        }

        /// <summary>TBD</summary>
        protected override void PostStop() => SaveState();

        #region Reading

        private void Reading()
        {
            Receive<Disassociated>(HandleDisassociated);
            Receive<InboundPayload>(HandleInboundPayload);
            Receive<EndpointWriter.StopReading>(HandleStopReading);
        }

        private void HandleInboundPayload(InboundPayload inbound)
        {
            var payload = inbound.Payload;
            //if (payload.Length > Transport.MaximumPayloadBytes)
            //{
            //    var reason = new OversizedPayloadException(
            //        $"Discarding oversized payload received: max allowed size {Transport.MaximumPayloadBytes} bytes, actual size {payload.Length} bytes.");
            //    _log.Error(reason, "Transient error while reading from association (association remains live)");
            //}
            //else
            //{
            var ackAndMessage = TryDecodeMessageAndAck(payload);
            var ackOption = ackAndMessage.AckOption;
            if (ackOption != null && _reliableDeliverySupervisor != null)
            {
                _reliableDeliverySupervisor.Tell(ackOption);
            }
            var messageOption = ackAndMessage.MessageOption;
            if (messageOption != null)
            {
                if (messageOption.ReliableDeliveryEnabled)
                {
                    _ackedReceiveBuffer = _ackedReceiveBuffer.Receive(messageOption);
                    DeliverAndAck();
                }
                else
                {
                    _msgDispatch.Dispatch(messageOption.Recipient,
                        messageOption.RecipientAddress,
                        messageOption.SerializedMessage,
                        messageOption.SenderOptional);
                }
            }
            //}
        }

        private void HandleStopReading(EndpointWriter.StopReading stop)
        {
            SaveState();
            if (null == _notReadingPatterns) { _notReadingPatterns = ConfigurePatterns(NotReading); }
            Become(_notReadingPatterns);
            stop.ReplyTo.Tell(new EndpointWriter.StoppedReading(stop.Writer));
        }

        #endregion

        #region NotReading

        private void NotReading()
        {
            Receive<Disassociated>(HandleDisassociated);
            Receive<EndpointWriter.StopReading>(HandleStopReadingNotReading);
            Receive<InboundPayload>(HandleInboundPayloadNotReading);
            ReceiveAny(PatternMatch<object>.EmptyAction); // ignore
        }

        private void HandleStopReadingNotReading(EndpointWriter.StopReading stop)
        {
            stop.ReplyTo.Tell(new EndpointWriter.StoppedReading(stop.Writer));
        }

        private void HandleInboundPayloadNotReading(InboundPayload payload)
        {
            var ackAndMessage = TryDecodeMessageAndAck(payload.Payload);
            var ackOption = ackAndMessage.AckOption;
            if (ackOption != null && _reliableDeliverySupervisor != null)
            {
                _reliableDeliverySupervisor.Tell(ackOption);
            }
        }

        #endregion

        #endregion

        #region * Lifecycle event handlers *

        private void SaveState()
        {
            var key = new EndpointManager.Link(LocalAddress, RemoteAddress);
            _receiveBuffers.TryGetValue(key, out var previousValue);
            UpdateSavedState(key, previousValue);
        }

        private EndpointManager.ResendState Merge(EndpointManager.ResendState current, EndpointManager.ResendState oldState)
        {
            if (current.Uid == oldState.Uid)
            {
                return new EndpointManager.ResendState(_uid, oldState.Buffer.MergeFrom(current.Buffer));
            }

            return current;
        }

        private void UpdateSavedState(EndpointManager.Link key, EndpointManager.ResendState expectedState)
        {
            while (true)
            {
                if (expectedState == null)
                {
                    if (_receiveBuffers.ContainsKey(key))
                    {
                        var updatedValue = new EndpointManager.ResendState(_uid, _ackedReceiveBuffer);
                        _receiveBuffers.AddOrUpdate(key, updatedValue, (link, state) => updatedValue);
                        expectedState = updatedValue;
                        continue;
                    }
                }
                else
                {
                    if (_receiveBuffers.TryGetValue(key, out var resendState) && resendState.Equals(expectedState))
                    {
                        _receiveBuffers[key] = Merge(new EndpointManager.ResendState(_uid, _ackedReceiveBuffer), expectedState);
                    }
                    else
                    {
                        _receiveBuffers.TryGetValue(key, out var previousValue);
                        expectedState = previousValue;
                        continue;
                    }
                }
                break;
            }
        }

        private void HandleDisassociated(Disassociated disassociated)
        {
            switch (disassociated.Info)
            {
                case DisassociateInfo.Quarantined:
                    ThrowInvalidAssociation(); break;
                case DisassociateInfo.Shutdown:
                    ThrowShutDownAssociation(); break;
                case DisassociateInfo.Unknown:
                default:
                    Context.Stop(Self); break;
            }

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowInvalidAssociation()
        {
            throw GetInvalidAssociation();
            InvalidAssociation GetInvalidAssociation()
            {
                return new InvalidAssociation("The remote system has quarantined this system. No further associations " +
                                              "to the remote system are possible until this system is restarted.", LocalAddress, RemoteAddress, disassociateInfo: DisassociateInfo.Quarantined);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowShutDownAssociation()
        {
            throw GetShutDownAssociation();
            ShutDownAssociation GetShutDownAssociation()
            {
                return new ShutDownAssociation($"The remote system terminated the association because it is shutting down. Shut down address: {RemoteAddress}", LocalAddress, RemoteAddress); ;
            }
        }

        private void DeliverAndAck()
        {
            var deliverable = _ackedReceiveBuffer.ExtractDeliverable;
            _ackedReceiveBuffer = deliverable.Buffer;

            // Notify writer that some messages can be acked
            Context.Parent.Tell(new EndpointWriter.OutboundAck(deliverable.Ack));
            deliverable.Deliverables.ForEach(msg => _msgDispatch.Dispatch(msg.Recipient, msg.RecipientAddress, msg.SerializedMessage, msg.SenderOptional));
        }

        private AckAndMessage TryDecodeMessageAndAck(object pdu)
        {
            try
            {
                return _codec.DecodeMessage((Akka.Serialization.Protocol.AckAndEnvelopeContainer)pdu, _provider, LocalAddress);
            }
            catch (Exception ex)
            {
                ThrowHelper.ThrowEndpointException_DecodeMessageAndAck(ex); return default;
            }
        }

        #endregion

        #region - Static members -

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="dispatcher">TBD</param>
        /// <param name="inbound">TBD</param>
        /// <param name="uid">TBD</param>
        /// <param name="receiveBuffers">TBD</param>
        /// <param name="reliableDeliverySupervisor">TBD</param>
        /// <returns>TBD</returns>
        public static Props ReaderProps(
                    Address localAddress,
                    Address remoteAddress,
                    AkkaProtocolTransport transport,
                    RemoteSettings settings,
                    AkkaPduCodec codec,
                    IInboundMessageDispatcher dispatcher,
                    bool inbound,
                    int uid,
                    ConcurrentDictionary<EndpointManager.Link, EndpointManager.ResendState> receiveBuffers,
                    IActorRef reliableDeliverySupervisor = null)
        {
            return
                Props.Create(
                    () =>
                        new EndpointReader(localAddress, remoteAddress, transport, settings, codec, dispatcher, inbound,
                            uid, receiveBuffers, reliableDeliverySupervisor))
                            .WithDispatcher(settings.Dispatcher);
        }

        #endregion
    }

    #endregion
}