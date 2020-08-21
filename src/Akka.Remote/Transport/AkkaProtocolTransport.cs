//-----------------------------------------------------------------------
// <copyright file="AkkaProtocolTransport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Serialization.Protocol;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Remote.Transport
{
    #region == class ProtocolTransportAddressPair ==

    /// <summary>
    /// <para>
    /// This class represents a pairing of an <see cref="AkkaProtocolTransport"/> with its <see
    /// cref="Address"/> binding.
    /// </para>
    /// <para>
    /// This is the information that's used to allow external <see cref="ActorSystem"/> messages to
    /// address this system over the network.
    /// </para>
    /// </summary>
    internal sealed class ProtocolTransportAddressPair
    {
        /// <summary>Initializes a new instance of the <see cref="ProtocolTransportAddressPair"/> class.</summary>
        /// <param name="protocolTransport">The protocol transport to pair with the specified <paramref name="address"/>.</param>
        /// <param name="address">The address to pair with the specified <paramref name="protocolTransport"/>.</param>
        public ProtocolTransportAddressPair(AkkaProtocolTransport protocolTransport, Address address)
        {
            ProtocolTransport = protocolTransport;
            Address = address;
        }

        /// <summary>The protocol transport part of the pairing.</summary>
        public AkkaProtocolTransport ProtocolTransport { get; }

        /// <summary>The address part of the pairing.</summary>
        public Address Address { get; }
    }

    #endregion

    #region -- class AkkaProtocolException --

    /// <summary>This exception is thrown when an error occurred during the Akka protocol handshake.</summary>
    public sealed class AkkaProtocolException : AkkaException
    {
        /// <summary>Initializes a new instance of the <see cref="AkkaProtocolException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public AkkaProtocolException(string message, Exception cause = null) : base(message, cause) { }

#if SERIALIZATION
        /// <summary>Initializes a new instance of the <see cref="AkkaProtocolException"/> class.</summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the
        /// exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source
        /// or destination.</param>
        private AkkaProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    #endregion

    #region == class AkkaProtocolTransport ==

    /// <summary>Implementation of the Akka protocol as a (logical) <see cref="Transport"/> that wraps an
    /// underlying (physical) <see cref="Transport"/> instance.
    ///
    /// Features provided by this transport include:
    /// - Soft-state associations via the use of heartbeats and failure detectors
    /// - Transparent origin address handling
    ///
    /// This transport is loaded automatically by <see cref="Remoting"/> and will wrap all
    /// dynamically loaded transports.</summary>
    internal sealed class AkkaProtocolTransport : ActorTransportAdapter
    {
        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        /// <param name="system">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        public AkkaProtocolTransport(Transport wrappedTransport, ActorSystem system, AkkaProtocolSettings settings, AkkaPduCodec codec)
            : base(wrappedTransport, system)
        {
            Codec = codec;
            Settings = settings;
        }

        /// <summary>TBD</summary>
        public AkkaProtocolSettings Settings { get; }

        /// <summary>TBD</summary>
        internal AkkaPduCodec Codec { get; }

        private readonly SchemeAugmenter _schemeAugmenter = new SchemeAugmenter(RemoteSettings.AkkaScheme);

        /// <summary>TBD</summary>
        protected override SchemeAugmenter SchemeAugmenter => _schemeAugmenter;

        private string _managerName;

        /// <summary>TBD</summary>
        protected override string ManagerName
        {
            get
            {
                if (string.IsNullOrEmpty(_managerName))
                {
                    _managerName = $"akkaprotocolmanager.{WrappedTransport.SchemeIdentifier}.{UniqueId.GetAndIncrement()}";
                }

                return _managerName;
            }
        }

        private Props _managerProps;

        /// <summary>TBD</summary>
        protected override Props ManagerProps
        {
            get
            {
                return _managerProps ??
                       (_managerProps =
                           Props.Create(() => new AkkaProtocolManager(WrappedTransport, Settings))
                               .WithDeploy(Deploy.Local));
            }
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        public override Task<bool> ManagementCommand(object message) => WrappedTransport.ManagementCommand(message);

        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <returns>TBD</returns>
        public async Task<AkkaProtocolHandle> Associate(Address remoteAddress, int? refuseUid)
        {
            // Prepare a Task and pass its completion source to the manager
            var statusPromise = new TaskCompletionSource<AssociationHandle>();

            manager.Tell(new AssociateUnderlyingRefuseUid(SchemeAugmenter.RemoveScheme(remoteAddress), statusPromise, refuseUid));

            return (AkkaProtocolHandle)await statusPromise.Task.ConfigureAwait(false);
        }

        #region - Static properties -

        /// <summary>TBD</summary>
        public static readonly AtomicCounter UniqueId = new AtomicCounter(0);

        #endregion
    }

    #endregion

    #region == class AkkaProtocolManager ==

    /// <summary>TBD</summary>
    internal sealed class AkkaProtocolManager : ActorTransportAdapterManager
    {
        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        /// <param name="settings">TBD</param>
        public AkkaProtocolManager(Transport wrappedTransport, AkkaProtocolSettings settings)
        {
            _wrappedTransport = wrappedTransport;
            _settings = settings;
        }

        private readonly Transport _wrappedTransport;

        private readonly AkkaProtocolSettings _settings;

        /// <summary>The <see cref="AkkaProtocolTransport"/> does not handle recovery of associations, this
        /// task is implemented in the remoting itself. Hence the strategy <see cref="Directive.Stop"/>.</summary>
        private readonly SupervisorStrategy _supervisor = new OneForOneStrategy(exception => Directive.Stop);

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy() => _supervisor;

        #region - ActorBase / ActorTransportAdapterManager overrides -

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        protected override void Ready(object message)
        {
            switch (message)
            {
                case InboundAssociation ia: // need to create an Inbound ProtocolStateActor
                    var handle = ia.Association;
                    var stateActorLocalAddress = LocalAddress;
                    var stateActorAssociationListener = AssociationListener;
                    var stateActorSettings = _settings;
                    var failureDetector = CreateTransportFailureDetector();
                    Context.ActorOf(RARP.For(Context.System).ConfigureDispatcher(ProtocolStateActor.InboundProps(
                        new HandshakeInfo(stateActorLocalAddress, AddressUidExtension.Uid(Context.System)),
                        handle,
                        stateActorAssociationListener,
                        stateActorSettings,
                        new AkkaPduMessagePackCodec(Context.System),
                        failureDetector)), ActorNameFor(handle.RemoteAddress));
                    break;

                case AssociateUnderlying au: // need to create an Outbound ProtocolStateActor
                    CreateOutboundStateActor(au.RemoteAddress, au.StatusPromise, null);
                    break;

                case AssociateUnderlyingRefuseUid aur:
                    CreateOutboundStateActor(aur.RemoteAddress, aur.StatusCompletionSource, aur.RefuseUid);
                    break;

                default:
                    Unhandled(message);
                    break;
            }
        }

        #endregion

        #region * Actor creation methods *

        private string ActorNameFor(Address remoteAddress) => $"akkaProtocol-{AddressUrlEncoder.Encode(remoteAddress)}-{NextId()}";

        private void CreateOutboundStateActor(Address remoteAddress,
            TaskCompletionSource<AssociationHandle> statusPromise, int? refuseUid)
        {
            var stateActorLocalAddress = LocalAddress;
            var stateActorSettings = _settings;
            var stateActorWrappedTransport = _wrappedTransport;
            var failureDetector = CreateTransportFailureDetector();

            Context.ActorOf(RARP.For(Context.System).ConfigureDispatcher(ProtocolStateActor.OutboundProps(
                new HandshakeInfo(stateActorLocalAddress, AddressUidExtension.Uid(Context.System)),
                remoteAddress,
                statusPromise,
                stateActorWrappedTransport,
                stateActorSettings,
                new AkkaPduMessagePackCodec(Context.System), failureDetector, refuseUid)),
                ActorNameFor(remoteAddress));
        }

        private FailureDetector CreateTransportFailureDetector()
        {
            return Context.LoadFailureDetector(_settings.TransportFailureDetectorImplementationClass,
                _settings.TransportFailureDetectorConfig);
        }

        #endregion
    }

    #endregion

    #region == class AssociateUnderlyingRefuseUid ==

    /// <summary>TBD</summary>
    internal sealed class AssociateUnderlyingRefuseUid : INoSerializationVerificationNeeded
    {
        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusCompletionSource">TBD</param>
        /// <param name="refuseUid">TBD</param>
        public AssociateUnderlyingRefuseUid(Address remoteAddress, TaskCompletionSource<AssociationHandle> statusCompletionSource, int? refuseUid = null)
        {
            RefuseUid = refuseUid;
            StatusCompletionSource = statusCompletionSource;
            RemoteAddress = remoteAddress;
        }

        /// <summary>TBD</summary>
        public Address RemoteAddress { get; }

        /// <summary>TBD</summary>
        public TaskCompletionSource<AssociationHandle> StatusCompletionSource { get; }

        /// <summary>TBD</summary>
        public int? RefuseUid { get; }
    }

    #endregion

    #region == class HandshakeInfo ==

    /// <summary>TBD</summary>
    internal sealed class HandshakeInfo
    {
        /// <summary>TBD</summary>
        /// <param name="origin">TBD</param>
        /// <param name="uid">TBD</param>
        public HandshakeInfo(Address origin, int uid)
        {
            Origin = origin;
            Uid = uid;
        }

        /// <summary>TBD</summary>
        public Address Origin { get; }

        /// <summary>TBD</summary>
        public int Uid { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is HandshakeInfo handshakeInfo && Equals(handshakeInfo);
        }

        private bool Equals(HandshakeInfo other) => Equals(Origin, other.Origin) && Uid == other.Uid;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Origin is object ? Origin.GetHashCode() : 0) * 397) ^ Uid.GetHashCode();
            }
        }
    }

    #endregion

    #region == class AkkaProtocolHandle ==

    /// <summary>TBD</summary>
    internal sealed class AkkaProtocolHandle : AbstractTransportAdapterHandle
    {
        /// <summary>TBD</summary>
        /// <param name="originalLocalAddress">TBD</param>
        /// <param name="originalRemoteAddress">TBD</param>
        /// <param name="readHandlerCompletionSource">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="handshakeInfo">TBD</param>
        /// <param name="stateActor">TBD</param>
        /// <param name="codec">TBD</param>
        public AkkaProtocolHandle(Address originalLocalAddress, Address originalRemoteAddress,
            TaskCompletionSource<IHandleEventListener> readHandlerCompletionSource, AssociationHandle wrappedHandle,
            HandshakeInfo handshakeInfo, IActorRef stateActor, AkkaPduCodec codec)
            : base(originalLocalAddress, originalRemoteAddress, wrappedHandle, RemoteSettings.AkkaScheme)
        {
            HandshakeInfo = handshakeInfo;
            StateActor = stateActor;
            ReadHandlerSource = readHandlerCompletionSource;
            Codec = codec;
        }

        /// <summary>The current handshake information.</summary>
        public readonly HandshakeInfo HandshakeInfo;

        /// <summary>The <see cref="ProtocolStateActor"/> responsible for this association.</summary>
        public readonly IActorRef StateActor;

        /// <summary>The codec instance used for managing transport-specific commands.</summary>
        public readonly AkkaPduCodec Codec;

        /// <inheritdoc cref="AssociationHandle"/>
        public override bool Write(object payload) => WrappedHandle.Write(Codec.ConstructPayload(payload));

#pragma warning disable CS0672 // Member overrides obsolete member
        /// <inheritdoc cref="AssociationHandle"/>
        public override void Disassociate()
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            Disassociate(DisassociateInfo.Unknown);
        }

        /// <summary>Forces a disassociation of the current transport.</summary>
        /// <param name="info">The reason for disassociating.</param>
        public void Disassociate(DisassociateInfo info) => StateActor.Tell(new DisassociateUnderlying(info));

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AkkaProtocolHandle akkaProtocolHandle && Equals(akkaProtocolHandle);
        }

        /// <inheritdoc/>
        internal bool Equals(AkkaProtocolHandle other)
            => base.Equals(other) && Equals(HandshakeInfo, other.HandshakeInfo) && Equals(StateActor, other.StateActor);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (HandshakeInfo is object ? HandshakeInfo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StateActor is object ? StateActor.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    #endregion

    #region == enum AssociationState ==

    /// <summary>TBD</summary>
    internal enum AssociationState
    {
        /// <summary>TBD</summary>
        Closed = 0,

        /// <summary>TBD</summary>
        WaitHandshake = 1,

        /// <summary>TBD</summary>
        Open = 2
    }

    #endregion

    #region == class HeartbeatTimer ==

    /// <summary>TBD</summary>
    internal sealed class HeartbeatTimer : INoSerializationVerificationNeeded { }

    #endregion

    #region == class HandshakeTimer ==

    internal sealed class HandshakeTimer : INoSerializationVerificationNeeded { }

    #endregion

    #region == class HandleMsg ==

    /// <summary>TBD</summary>
    internal sealed class HandleMsg : INoSerializationVerificationNeeded
    {
        /// <summary>TBD</summary>
        /// <param name="handle">TBD</param>
        public HandleMsg(AssociationHandle handle) => Handle = handle;

        /// <summary>TBD</summary>
        public AssociationHandle Handle { get; }
    }

    #endregion

    #region == class HandleListenerRegistered ==

    /// <summary>TBD</summary>
    internal sealed class HandleListenerRegistered : INoSerializationVerificationNeeded
    {
        /// <summary>TBD</summary>
        /// <param name="listener">TBD</param>
        public HandleListenerRegistered(IHandleEventListener listener) => Listener = listener;

        /// <summary>TBD</summary>
        public IHandleEventListener Listener { get; }
    }

    #endregion


    #region == class ProtocolStateData ==

    /// <summary>TBD</summary>
    internal abstract class ProtocolStateData { }

    #endregion

    #region == class InitialProtocolStateData ==

    /// <summary>TBD</summary>
    internal abstract class InitialProtocolStateData : ProtocolStateData { }

    #endregion

    #region == class OutboundUnassociated ==

    /// <summary>Neither the underlying nor the provided transport is associated</summary>
    internal sealed class OutboundUnassociated : InitialProtocolStateData
    {
        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusCompletionSource">TBD</param>
        /// <param name="transport">TBD</param>
        public OutboundUnassociated(Address remoteAddress, TaskCompletionSource<AssociationHandle> statusCompletionSource, Transport transport)
        {
            Transport = transport;
            StatusCompletionSource = statusCompletionSource;
            RemoteAddress = remoteAddress;
        }

        /// <summary>TBD</summary>
        public Address RemoteAddress { get; }

        /// <summary>TBD</summary>
        public TaskCompletionSource<AssociationHandle> StatusCompletionSource { get; }

        /// <summary>TBD</summary>
        public Transport Transport { get; }
    }

    #endregion

    #region == class OutboundUnderlyingAssociated ==

    /// <summary>The underlying transport is associated, but the handshake of the Akka protocol is not yet finished.</summary>
    internal sealed class OutboundUnderlyingAssociated : ProtocolStateData
    {
        /// <summary>TBD</summary>
        /// <param name="statusCompletionSource">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        public OutboundUnderlyingAssociated(TaskCompletionSource<AssociationHandle> statusCompletionSource, AssociationHandle wrappedHandle)
        {
            WrappedHandle = wrappedHandle;
            StatusCompletionSource = statusCompletionSource;
        }

        /// <summary>TBD</summary>
        public TaskCompletionSource<AssociationHandle> StatusCompletionSource { get; }

        /// <summary>TBD</summary>
        public AssociationHandle WrappedHandle { get; }
    }

    #endregion

    #region == class InboundUnassociated ==

    /// <summary>The underlying transport is associated, but the handshake of the akka protocol is not yet finished</summary>
    internal sealed class InboundUnassociated : InitialProtocolStateData
    {
        /// <summary>TBD</summary>
        /// <param name="associationEventListener">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        public InboundUnassociated(IAssociationEventListener associationEventListener, AssociationHandle wrappedHandle)
        {
            WrappedHandle = wrappedHandle;
            AssociationEventListener = associationEventListener;
        }

        /// <summary>TBD</summary>
        public IAssociationEventListener AssociationEventListener { get; }

        /// <summary>TBD</summary>
        public AssociationHandle WrappedHandle { get; }
    }

    #endregion

    #region == class AssociatedWaitHandler ==

    /// <summary>The underlying transport is associated, but the handler for the handle has not been provided yet.</summary>
    internal sealed class AssociatedWaitHandler : ProtocolStateData
    {
        /// <summary>TBD</summary>
        /// <param name="handlerListener">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="queue">TBD</param>
        public AssociatedWaitHandler(Task<IHandleEventListener> handlerListener, AssociationHandle wrappedHandle, Queue<object> queue)
        {
            Queue = queue;
            WrappedHandle = wrappedHandle;
            HandlerListener = handlerListener;
        }

        /// <summary>TBD</summary>
        public Task<IHandleEventListener> HandlerListener { get; }

        /// <summary>TBD</summary>
        public AssociationHandle WrappedHandle { get; }

        /// <summary>TBD</summary>
        public Queue<object> Queue { get; }
    }

    #endregion

    #region == class ListenerReady ==

    /// <summary>System ready!</summary>
    internal sealed class ListenerReady : ProtocolStateData
    {
        /// <summary>TBD</summary>
        /// <param name="listener">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        public ListenerReady(IHandleEventListener listener, AssociationHandle wrappedHandle)
        {
            WrappedHandle = wrappedHandle;
            Listener = listener;
        }

        /// <summary>TBD</summary>
        public IHandleEventListener Listener { get; }

        /// <summary>TBD</summary>
        public AssociationHandle WrappedHandle { get; }
    }

    #endregion


    #region == class TimeoutReason ==

    /// <summary>Message sent when a <see cref="FailureDetector.IsAvailable"/> returns false, signaling a
    /// transport timeout.</summary>
    internal sealed class TimeoutReason
    {
        /// <summary>TBD</summary>
        /// <param name="errorMessage">TBD</param>
        public TimeoutReason(string errorMessage) => ErrorMessage = errorMessage;

        /// <summary>TBD</summary>
        public string ErrorMessage { get; }

        /// <inheritdoc/>
        public override string ToString() => $"Timeout: {ErrorMessage}";
    }

    #endregion

    #region == class ForbiddenUidReason ==

    /// <summary>TBD</summary>
    internal sealed class ForbiddenUidReason { }

    #endregion

    #region == class ProtocolStateActor ==

    /// <summary>INTERNAL API.</summary>
    internal sealed class ProtocolStateActor : FSM<AssociationState, ProtocolStateData>
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly InitialProtocolStateData _initialData;
        private readonly HandshakeInfo _localHandshakeInfo;
        private readonly int? _refuseUid;
        private readonly AkkaProtocolSettings _settings;
        private readonly Address _localAddress;
        private readonly AkkaPduCodec _codec;
        private readonly FailureDetector _failureDetector;

        private const string HandshakeTimerKey = "handshake-timer";

        /// <summary>Constructor for outbound ProtocolStateActors</summary>
        /// <param name="handshakeInfo">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusCompletionSource">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="failureDetector">TBD</param>
        /// <param name="refuseUid">TBD</param>
        public ProtocolStateActor(HandshakeInfo handshakeInfo, Address remoteAddress,
            TaskCompletionSource<AssociationHandle> statusCompletionSource, Transport transport,
            AkkaProtocolSettings settings, AkkaPduCodec codec, FailureDetector failureDetector, int? refuseUid = null)
            : this(
                new OutboundUnassociated(remoteAddress, statusCompletionSource, transport), handshakeInfo, settings, codec, failureDetector,
                refuseUid)
        {
        }

        /// <summary>Constructor for inbound ProtocolStateActors</summary>
        /// <param name="handshakeInfo">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="associationEventListener">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="failureDetector">TBD</param>
        public ProtocolStateActor(HandshakeInfo handshakeInfo, AssociationHandle wrappedHandle, IAssociationEventListener associationEventListener, AkkaProtocolSettings settings, AkkaPduCodec codec, FailureDetector failureDetector)
            : this(new InboundUnassociated(associationEventListener, wrappedHandle), handshakeInfo, settings, codec, failureDetector, refuseUid: null) { }

        /// <summary>Common constructor used by both the outbound and the inbound cases</summary>
        /// <param name="initialData">TBD</param>
        /// <param name="localHandshakeInfo">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="failureDetector">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <exception cref="AkkaProtocolException">
        /// This exception is thrown for a number of reasons that include the following:
        /// <dl><dt><b>when in the <see cref="AssociationState.WaitHandshake"/>
        /// state</b></dt><dd><dl><dt><b>the event is of type <see
        /// cref="HeartbeatTimer"/></b></dt><dd>This exception is thrown when there is no response
        /// from the remote system causing the timeout.</dd></dl></dd><dt><b>when in the <see
        /// cref="AssociationState.Open"/> state</b></dt><dd><dl><dt><b>the event is of type <see
        /// cref="InboundPayload"/></b></dt><dd>This exception is thrown when the message type could
        /// not be handled.</dd><dt><b>the event is of type <see
        /// cref="HeartbeatTimer"/></b></dt><dd>This exception is thrown when there is no response
        /// from the remote system causing the timeout.</dd><dt><b>the event is of type <see
        /// cref="DisassociateUnderlying"/></b></dt><dd>This exception is thrown when the message
        /// type could not be handled.</dd></dl></dd><dt><b>when the FSM is terminating <see
        /// cref="FSM{TState, TData}.OnTermination(Action{Akka.Actor.FSMBase.StopEvent{TState,
        /// TData}})"/></b></dt><dd><dl><dt><b>the event is of type <see
        /// cref="OutboundUnassociated"/></b></dt><dd>This exception is thrown when the transport
        /// disassociated before the handshake finished.</dd><dt><b>the event is of type <see
        /// cref="OutboundUnderlyingAssociated"/> with <see
        /// cref="Akka.Actor.FSMBase.StopEvent{TState, TData}.Reason "/> being <see
        /// cref="Akka.Actor.FSMBase.Failure"/></b></dt><dd>This exception is thrown when either a
        /// timeout occurs, the remote system is shutting down, this system has been quarantined, or
        /// the transport disassociated before handshake finished. </dd></dl></dd></dl>
        /// </exception>
        private ProtocolStateActor(InitialProtocolStateData initialData, HandshakeInfo localHandshakeInfo, AkkaProtocolSettings settings, AkkaPduCodec codec, FailureDetector failureDetector, int? refuseUid)
        {
            _initialData = initialData;
            _localHandshakeInfo = localHandshakeInfo;
            _settings = settings;
            _refuseUid = refuseUid;
            _localAddress = _localHandshakeInfo.Origin;
            _codec = codec;
            _failureDetector = failureDetector;
            InitializeFSM();
        }

        #region - FSM bindings -

        private void InitializeFSM()
        {
            When(AssociationState.Closed, e => HandleWhenClosed(e));

            //Transport layer events for outbound associations
            When(AssociationState.WaitHandshake, e => HandleWhenWaitHandshake(e));

            When(AssociationState.Open, e => HandleWhenOpen(e));

            OnTermination(e => HandleTermination(e));

            /*
             * Set the initial ProtocolStateActor state to CLOSED if OUTBOUND
             * Set the initial ProtocolStateActor state to WAITHANDSHAKE if INBOUND
             * */
            switch (_initialData)
            {
                case OutboundUnassociated d:
                    // attempt to open underlying transport to the remote address if using DotNetty,
                    // this is where the socket connection is opened.
                    d.Transport.Associate(d.RemoteAddress).Then(AfterConnectionIsOpenedFunc, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
                    StartWith(AssociationState.Closed, d);
                    break;
                case InboundUnassociated d:
                    // inbound transport is opened already inside the ProtocolStateManager therefore
                    // we just have to set ourselves as listener and wait for incoming handshake
                    // attempts from the client.
                    d.WrappedHandle.ReadHandlerSource.SetResult(new ActorHandleEventListener(Self));
                    StartWith(AssociationState.WaitHandshake, d);
                    break;
            }

            InitHandshakeTimer();
        }

        private State<AssociationState, ProtocolStateData> HandleWhenClosed(Event<ProtocolStateData> fsmEvent)
        {
            // Transport layer events for outbound associations
            switch (fsmEvent.FsmEvent)
            {
                case Status.Failure f:
                    if (fsmEvent.StateData is OutboundUnassociated ou)
                    {
                        ou.StatusCompletionSource.TrySetUnwrappedException(f.Cause);
                        return Stop();
                    }
                    return null;

                case HandleMsg h:
                    if (fsmEvent.StateData is OutboundUnassociated ou1)
                    {
                        /*
                         * Association has been established, but handshake is not yet complete.
                         * This actor, the outbound ProtocolStateActor, can now set itself as
                         * the read handler for the remainder of the handshake process.
                         */
                        var wrappedHandle = h.Handle;
                        var statusPromise = ou1.StatusCompletionSource;
                        wrappedHandle.ReadHandlerSource.TrySetResult(new ActorHandleEventListener(Self));
                        if (SendAssociate(wrappedHandle, _localHandshakeInfo))
                        {
                            _failureDetector.HeartBeat();
                            InitHeartbeatTimer();
                            // wait for reply from the inbound side of the connection (WaitHandshake)
                            return
                                GoTo(AssociationState.WaitHandshake)
                                    .Using(new OutboundUnderlyingAssociated(statusPromise, wrappedHandle));
                        }
                        else
                        {
                            // Underlying transport was busy -- Associate could not be sent
                            SetTimer("associate-retry", new HandleMsg(wrappedHandle),
                                RARP.For(Context.System).Provider
                                    .RemoteSettings.BackoffPeriod, repeat: false);
                            return Stay();
                        }
                    }
                    return Stay();

                case DisassociateUnderlying _:
                    return Stop();

                case HandshakeTimer _ when fsmEvent.StateData is OutboundUnassociated ou2:
                    var (timeoutExc, timeout) = GetHandshakeTimeoutError(_settings);
                    ou2.StatusCompletionSource.SetException(timeoutExc);
                    return Stop(timeout);

                default:
                    return Stay();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (TimeoutException timeoutExc, Failure timeout) GetHandshakeTimeoutError(AkkaProtocolSettings settings)
        {
            var errMsg = $"No response from remote for outbound association. Associate timed out after " +
                         $"[{settings.HandshakeTimeout.TotalMilliseconds} ms].";
            return (new TimeoutException(errMsg), new Failure(new TimeoutReason(errMsg)));
        }

        private State<AssociationState, ProtocolStateData> HandleWhenWaitHandshake(Event<ProtocolStateData> @event)
        {
            var fsmEvent = @event.FsmEvent;
            var evtStateData = @event.StateData;
            switch (fsmEvent)
            {
                case Disassociated d:
                    return Stop(new Failure(d.Info));

                case InboundPayload p when evtStateData is OutboundUnderlyingAssociated ola:
                    {
                        var pdu = DecodePdu(p.Payload);
                        /*
                         * This state is used for OutboundProtocolState actors when they receive
                         * a reply back from the inbound end of the association.
                         */
                        var wrappedHandle = ola.WrappedHandle;
                        var statusCompletionSource = ola.StatusCompletionSource;
                        switch (pdu)
                        {
                            case Associate a:
                                var handshakeInfo = a.Info;
                                if (_refuseUid.HasValue && _refuseUid == handshakeInfo.Uid) //refused UID
                                {
                                    SendDisassociate(wrappedHandle, DisassociateInfo.Quarantined);
                                    return Stop(new Failure(new ForbiddenUidReason()));
                                }
                                else //accepted UID
                                {
                                    _failureDetector.HeartBeat();
                                    CancelTimer(HandshakeTimerKey);
                                    return
                                        GoTo(AssociationState.Open)
                                            .Using(
                                                new AssociatedWaitHandler(
                                                    NotifyOutboundHandler(wrappedHandle, handshakeInfo,
                                                        statusCompletionSource), wrappedHandle,
                                                    new Queue<object>()));
                                }
                            case Disassociate d:
                                //After receiving Disassociate we MUST NOT send back a Disassociate (loop)
                                return Stop(new Failure(d.Reason));
                            default:
                                //Expect handshake to be finished, dropping connection
#if DEBUG
                                if (_log.IsDebugEnabled) _log.ExpectedMessageOfTypeAssociate(fsmEvent);
#endif

                                SendDisassociate(wrappedHandle, DisassociateInfo.Unknown);
                                return Stop();
                        }
                    }

                case HeartbeatTimer _ when evtStateData is OutboundUnderlyingAssociated oua:
                    return HandleTimers(oua.WrappedHandle);

                // Events for inbound associations
                case InboundPayload p when evtStateData is InboundUnassociated iu:
                    {
                        var pdu = DecodePdu(p.Payload);
                        /*
                         * This state is used by inbound protocol state actors
                         * when they receive an association attempt from the
                         * outbound side of the association.
                         */
                        var associationHandler = iu.AssociationEventListener;
                        var wrappedHandle = iu.WrappedHandle;
                        switch (pdu)
                        {
                            case Disassociate d:
                                // After receiving Disassociate we MUST NOT send back a Disassociate (loop)
                                return Stop(new Failure(d.Reason));
                            case Associate a:
                                // Incoming association -- implicitly ACK by a heartbeat
                                SendAssociate(wrappedHandle, _localHandshakeInfo);
                                _failureDetector.HeartBeat();
                                InitHeartbeatTimer();
                                CancelTimer(HandshakeTimerKey);
                                return GoTo(AssociationState.Open).Using(
                                    new AssociatedWaitHandler(
                                        NotifyInboundHandler(wrappedHandle, a.Info, associationHandler),
                                        wrappedHandle, new Queue<object>()));

                            // Got a stray message -- explicitly reset the association (force remote endpoint to reassociate)
                            default:
#if DEBUG
                                if (_log.IsDebugEnabled) _log.SendingDisassociateToBecauseUnexpectedMessageOfTypeWasReceivedUnassociated(wrappedHandle, fsmEvent);
#endif
                                SendDisassociate(wrappedHandle, DisassociateInfo.Unknown);
                                return Stop();
                        }
                    }

                case HandshakeTimer _ when evtStateData is OutboundUnderlyingAssociated oua:
#if DEBUG
                    if (_log.IsDebugEnabled) _log.SendingDisassociateToBecauseHandshakeTimedOutForOutboundAssociationAfter(oua, _settings);
#endif
                    SendDisassociate(oua.WrappedHandle, DisassociateInfo.Unknown);
                    return Stop(new Failure(GetTimeoutReasonForOutboundAssociation()));

                case HandshakeTimer _ when evtStateData is InboundUnassociated iu:
#if DEBUG
                    if (_log.IsDebugEnabled) _log.SendingDisassociateToBecauseHandshakeTimedOutForInboundAssociationAfter(iu, _settings);
#endif
                    SendDisassociate(iu.WrappedHandle, DisassociateInfo.Unknown);
                    return Stop(new Failure(GetTimeoutReasonForInboundAssociation()));

                case UnderlyingTransportError e:
                    PublishError(e);
                    return Stay();

                default:
                    return null;
            }
        }

        private State<AssociationState, ProtocolStateData> HandleWhenOpen(Event<ProtocolStateData> @event)
        {
            var fsmEvent = @event.FsmEvent;
            var evtStateData = @event.StateData;
            switch (fsmEvent)
            {
                case Disassociated d:
                    return Stop(new Failure(d.Info));

                case InboundPayload ip:
                    {
                        var pdu = DecodePdu(ip.Payload);
                        switch (pdu)
                        {
                            case Disassociate d:
                                return Stop(new Failure(d.Reason));
                            case Heartbeat _:
                                _failureDetector.HeartBeat();
                                return Stay();
                            case Payload p:
                                // use incoming ordinary message as alive sign
                                _failureDetector.HeartBeat();
                                switch (evtStateData)
                                {
                                    case AssociatedWaitHandler awh:
                                        var nQueue = new Queue<object>(awh.Queue);
                                        nQueue.Enqueue(p.Bytes);
                                        return
                                            Stay()
                                                .Using(new AssociatedWaitHandler(awh.HandlerListener, awh.WrappedHandle,
                                                    nQueue));
                                    case ListenerReady lr:
                                        lr.Listener.Notify(new InboundPayload(p.Bytes));
                                        return Stay();
                                    default:
                                        throw ThrowHelper.GetAkkaProtocolException_InboundPayload(fsmEvent);
                                }
                            default:
                                return Stay();
                        }
                    }
                case HeartbeatTimer _ when evtStateData is AssociatedWaitHandler awh:
                    return HandleTimers(awh.WrappedHandle);

                case HeartbeatTimer _ when evtStateData is ListenerReady lr:
                    return HandleTimers(lr.WrappedHandle);

                case DisassociateUnderlying dl:
                    {
                        AssociationHandle GetHandle(ProtocolStateData data)
                        {
                            switch (data)
                            {
                                case ListenerReady lr:
                                    return lr.WrappedHandle;
                                case AssociatedWaitHandler awh:
                                    return awh.WrappedHandle;
                                default:
                                    throw ThrowHelper.GetAkkaProtocolException_DisassociateUnderlying(fsmEvent);
                            }
                        }

                        var handle = GetHandle(evtStateData);

                        // No debug logging here as sending DisassociateUnderlying(Unknown) should have been logged from where
                        // it was sent
                        SendDisassociate(handle, dl.Info);
                        return Stop();
                    }
                case HandleListenerRegistered hlr when evtStateData is AssociatedWaitHandler awh:
                    foreach (var p in awh.Queue)
                    {
                        hlr.Listener.Notify(new InboundPayload(p));
                    }
                    return Stay().Using(new ListenerReady(hlr.Listener, awh.WrappedHandle));

                case UnderlyingTransportError e:
                    PublishError(e);
                    return Stay();

                default:
                    return null;
            }
        }

        private void HandleTermination(StopEvent<AssociationState, ProtocolStateData> @event)
        {
            var evtReason = @event.Reason;
            switch (@event.StateData)
            {
                case OutboundUnassociated ou:
                    ou.StatusCompletionSource.TrySetException(ThrowHelper.GetAkkaProtocolException_TransportDisassociatedBeforeHandshakeFinished(evtReason));
                    break;

                case OutboundUnderlyingAssociated oua:
                    oua.StatusCompletionSource.TrySetUnwrappedException(GetAkkaProtocolException(evtReason));
                    oua.WrappedHandle.Disassociate(DisassociationReason(evtReason), _log);
                    break;

                case AssociatedWaitHandler awh:
                    awh.HandlerListener.Then(AfterSetupHandlerListenerAction, CreateDisassociated(evtReason),
                        TaskContinuationOptions.ExecuteSynchronously);
                    awh.WrappedHandle.Disassociate(DisassociationReason(@event.Reason), _log);
                    break;

                case ListenerReady lr:
                    lr.Listener.Notify(CreateDisassociated(evtReason));
                    lr.WrappedHandle.Disassociate(DisassociationReason(@event.Reason), _log);
                    break;

                case InboundUnassociated iu:
                    iu.WrappedHandle.Disassociate(DisassociationReason(@event.Reason), _log);
                    break;

                default:
                    break;
            }
        }

        private static readonly Action<IHandleEventListener, Disassociated> AfterSetupHandlerListenerAction = (l, n) => AfterSetupHandlerListener(l, n);
        private static void AfterSetupHandlerListener(IHandleEventListener listener, Disassociated disassociateNotification)
        {
            listener.Notify(disassociateNotification);
        }

        private static readonly Func<AssociationHandle, HandleMsg> AfterConnectionIsOpenedFunc = h => AfterConnectionIsOpened(h);
        private static HandleMsg AfterConnectionIsOpened(AssociationHandle handle)
        {
            return new HandleMsg(handle);
        }

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        protected override void LogTermination(Reason reason)
        {
            if (reason is Failure failure)
            {
                switch (failure.Cause)
                {
                    case TimeoutReason timeoutReason:
                        _log.Error(timeoutReason.ErrorMessage);
                        break;
                    case DisassociateInfo _:
                    case ForbiddenUidReason _:
                    default:
                        // no logging
                        break;
                }
            }
            else
            {
                base.LogTermination(reason);
            }
        }

        #endregion

        #region - Actor methods -

        /// <summary>TBD</summary>
        protected override void PostStop()
        {
            CancelTimer("heartbeat-timer");
            base.PostStop(); //pass to OnTermination
        }

        #endregion

        #region * Internal protocol messaging methods *

        private static Disassociated CreateDisassociated(Reason reason)
        {
            // Invalidate exposed but still unfinished promise. The underlying association disappeared, so after
            // registration immediately signal a disassociate
            if (reason is Failure failure && failure.Cause is DisassociateInfo disassociateInfo)
            {
                return new Disassociated(disassociateInfo);
            }
            else
            {
                return new Disassociated(DisassociateInfo.Unknown);
            }
        }

        private static string DisassociationReason(Reason reason)
        {
            switch (reason)
            {
                case Normal _:
                    return "the ProtocolStateActor was stopped normally";
                case Shutdown _:
                    return "the ProtocolStateActor was shutdown";
                case Failure f:
                    return $"the ProtocolStateActor failed: {f.Cause}";
                default:
                    throw GetUnrecognizedShutdownReasonError(reason);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AkkaProtocolException GetUnrecognizedShutdownReasonError(Reason reason)
        {
            return new AkkaProtocolException($"Unrecognized shutdown reason: {reason}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AkkaProtocolException GetAkkaProtocolException(Reason reason)
        {
            if (reason is Failure f)
            {
                switch (f.Cause)
                {
                    case TimeoutReason timeout:
                        return new AkkaProtocolException(timeout.ErrorMessage);

                    case ForbiddenUidReason _:
                        return new AkkaProtocolException("The remote system has a UID that has been quarantined. Association aborted.");

                    case DisassociateInfo info:
                        return DisassociateException(info);

                    default:
                        return null;
                }
            }
            else
            {
                return new AkkaProtocolException("Transport disassociated before handshake finished");
            }
        }

        private static AkkaProtocolException DisassociateException(DisassociateInfo info)
        {
            switch (info)
            {
                case DisassociateInfo.Shutdown:
                    return new AkkaProtocolException("The remote system refused the association because it is shutting down.");

                case DisassociateInfo.Quarantined:
                    return new AkkaProtocolException("The remote system has quarantined this system. No further associations to the remote systems are possible until this system is restarted.");

                case DisassociateInfo.Unknown:
                default:
                    return new AkkaProtocolException("The remote system explicitly disassociated (reason unknown).");
            }
        }

        private State<AssociationState, ProtocolStateData> HandleTimers(AssociationHandle wrappedHandle)
        {
            if (_failureDetector.IsAvailable)
            {
                SendHeartBeat(wrappedHandle);
                return Stay();
            }
            else
            {
                var stateName = StateName;
#if DEBUG
                if (_log.IsDebugEnabled)
                {
                    _log.SendingDisassociateToBecauseFailureDetectorTriggeredInState(wrappedHandle, stateName);
                }
#endif

                // send disassociate just to be sure
                SendDisassociate(wrappedHandle, DisassociateInfo.Unknown);
                return Stop(new Failure(GetTimeoutReasonForHandleTimers(stateName)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TimeoutReason GetTimeoutReasonForHandleTimers(AssociationState stateName)
        {
            return new TimeoutReason($"No response from remote. Handshake timed out or transport failure detector triggered. (internal state was {stateName})");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private TimeoutReason GetTimeoutReasonForOutboundAssociation()
        {
            return new TimeoutReason(
                $"No response from remote for outbound association. Handshake timed out after [{_settings.HandshakeTimeout.TotalMilliseconds}] ms");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private TimeoutReason GetTimeoutReasonForInboundAssociation()
        {
            return new TimeoutReason(
                $"No response from remote for inbound association. Handshake timed out after [{_settings.HandshakeTimeout.TotalMilliseconds}] ms");
        }

        private void ListenForListenerRegistration(TaskCompletionSource<IHandleEventListener> readHandlerSource)
        {
            readHandlerSource.Task.Then(AfterSetupHandlerList1enerFunc, TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
        }

        private static readonly Func<IHandleEventListener, HandleListenerRegistered> AfterSetupHandlerList1enerFunc = l => AfterSetupHandlerListener(l);
        private static HandleListenerRegistered AfterSetupHandlerListener(IHandleEventListener listener)
        {
            return new HandleListenerRegistered(listener);
        }

        private Task<IHandleEventListener> NotifyOutboundHandler(AssociationHandle wrappedHandle,
            HandshakeInfo handshakeInfo, TaskCompletionSource<AssociationHandle> statusPromise)
        {
            var readHandlerPromise = new TaskCompletionSource<IHandleEventListener>();
            ListenForListenerRegistration(readHandlerPromise);

            statusPromise.SetResult(new AkkaProtocolHandle(_localAddress, wrappedHandle.RemoteAddress,
                readHandlerPromise, wrappedHandle, handshakeInfo, Self, _codec));

            return readHandlerPromise.Task;
        }

        private Task<IHandleEventListener> NotifyInboundHandler(AssociationHandle wrappedHandle,
            HandshakeInfo handshakeInfo, IAssociationEventListener associationEventListener)
        {
            var readHandlerPromise = new TaskCompletionSource<IHandleEventListener>();
            ListenForListenerRegistration(readHandlerPromise);

            associationEventListener.Notify(
                new InboundAssociation(
                    new AkkaProtocolHandle(_localAddress, handshakeInfo.Origin, readHandlerPromise, wrappedHandle, handshakeInfo, Self, _codec)));
            return readHandlerPromise.Task;
        }

        private IAkkaPdu DecodePdu(object pdu)
        {
            try
            {
                return _codec.DecodePdu((AkkaProtocolMessage)pdu);
            }
            catch (Exception ex)
            {
                throw ThrowHelper.GetAkkaProtocolException_DecodePdu(pdu, ex);
            }
        }

        private void InitHeartbeatTimer()
        {
            SetTimer("heartbeat-timer", new HeartbeatTimer(), _settings.TransportHeartBeatInterval, true);
        }

        private void InitHandshakeTimer()
        {
            SetTimer(HandshakeTimerKey, new HandshakeTimer(), _settings.HandshakeTimeout, true);
        }

        private bool SendAssociate(AssociationHandle wrappedHandle, HandshakeInfo info)
        {
            try
            {
                return wrappedHandle.Write(_codec.ConstructAssociate(info));
            }
            catch (Exception ex)
            {
                throw ThrowHelper.GetAkkaProtocolException_Associate(ex);
            }
        }

        private void SendDisassociate(AssociationHandle wrappedHandle, DisassociateInfo info)
        {
            try
            {
                wrappedHandle.Write(_codec.ConstructDisassociate(info));
            }
            catch (Exception ex)
            {
                ThrowHelper.ThrowAkkaProtocolException_Disassociate(ex);
            }
        }

        private bool SendHeartBeat(AssociationHandle wrappedHandle)
        {
            try
            {
                return wrappedHandle.Write(_codec.ConstructHeartbeat());
            }
            catch (Exception ex)
            {
                throw ThrowHelper.GetAkkaProtocolException_HeartBeat(ex);
            }
        }

        /// <summary>Publishes a transport error to the message stream</summary>
        private void PublishError(UnderlyingTransportError transportError)
        {
            _log.Error(transportError.Cause, transportError.Message);
        }

        #endregion

        #region - Static methods -

        /// <summary>
        /// <see cref="Props"/> used when creating OUTBOUND associations to remote endpoints.
        ///
        /// These <see cref="Props"/> create outbound <see cref="ProtocolStateActor"/> instances,
        /// which begin a state of
        /// </summary>
        /// <param name="handshakeInfo">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusCompletionSource">TBD</param>
        /// <param name="transport">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="failureDetector">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <returns>TBD</returns>
        public static Props OutboundProps(HandshakeInfo handshakeInfo, Address remoteAddress,
            TaskCompletionSource<AssociationHandle> statusCompletionSource,
            Transport transport, AkkaProtocolSettings settings, AkkaPduCodec codec, FailureDetector failureDetector, int? refuseUid = null)
        {
            return Props.Create(() => new ProtocolStateActor(handshakeInfo, remoteAddress, statusCompletionSource, transport, settings, codec, failureDetector, refuseUid));
        }

        /// <summary>TBD</summary>
        /// <param name="handshakeInfo">TBD</param>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="associationEventListener">TBD</param>
        /// <param name="settings">TBD</param>
        /// <param name="codec">TBD</param>
        /// <param name="failureDetector">TBD</param>
        /// <returns>TBD</returns>
        public static Props InboundProps(HandshakeInfo handshakeInfo, AssociationHandle wrappedHandle,
            IAssociationEventListener associationEventListener, AkkaProtocolSettings settings, AkkaPduCodec codec, FailureDetector failureDetector)
        {
            return Props.Create(() => new ProtocolStateActor(handshakeInfo, wrappedHandle, associationEventListener, settings, codec, failureDetector));
        }

        #endregion
    }

    #endregion
}