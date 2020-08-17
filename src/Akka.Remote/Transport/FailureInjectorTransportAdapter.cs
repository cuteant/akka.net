//-----------------------------------------------------------------------
// <copyright file="FailureInjectorTransportAdapter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Util;
using CuteAnt.AsyncEx;
using MessagePack;

namespace Akka.Remote.Transport
{
    #region -- class FailureInjectorProvider --

    /// <summary>Provider implementation for creating <see cref="FailureInjectorTransportAdapter"/> instances.</summary>
    public sealed class FailureInjectorProvider : ITransportAdapterProvider
    {
        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public Transport Create(Transport wrappedTransport, ExtendedActorSystem system)
            => new FailureInjectorTransportAdapter(wrappedTransport, system);
    }

    #endregion

    #region -- class FailureInjectorException --

    /// <summary>This exception is used to indicate a simulated failure in an association.</summary>
    public sealed class FailureInjectorException : AkkaException
    {
        /// <summary>Initializes a new instance of the <see cref="FailureInjectorException"/> class.</summary>
        /// <param name="msg">The message that describes the error.</param>
        public FailureInjectorException(string msg) => Msg = msg;

#if SERIALIZATION
        /// <summary>Initializes a new instance of the <see cref="FailureInjectorException"/> class.</summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the
        /// exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source
        /// or destination.</param>
        private FailureInjectorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        /// <summary>Retrieves the message of the simulated failure.</summary>
        public string Msg { get; }
    }

    #endregion

    #region == class FailureInjectorTransportAdapter ==

    /// <summary>TBD</summary>
    internal sealed class FailureInjectorTransportAdapter : AbstractTransportAdapter, IAssociationEventListener
    {
        #region - Internal message classes -

        /// <summary>TBD</summary>
        public const string FailureInjectorSchemeIdentifier = "gremlin";

        /// <summary>TBD</summary>
        public interface IFailureInjectorCommand { }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class All
        {
            /// <summary>TBD</summary>
            /// <param name="mode">TBD</param>
            [SerializationConstructor]
            public All(IGremlinMode mode) => Mode = mode;

            /// <summary>TBD</summary>
            [Key(0)]
            public readonly IGremlinMode Mode;
        }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class One
        {
            /// <summary>TBD</summary>
            /// <param name="remoteAddress">TBD</param>
            /// <param name="mode">TBD</param>
            [SerializationConstructor]
            public One(Address remoteAddress, IGremlinMode mode)
            {
                Mode = mode;
                RemoteAddress = remoteAddress;
            }

            /// <summary>TBD</summary>
            [Key(0)]
            public readonly Address RemoteAddress;

            /// <summary>TBD</summary>
            [Key(1)]
            public readonly IGremlinMode Mode;
        }

        /// <summary>TBD</summary>
        public interface IGremlinMode { }

        /// <summary>TBD</summary>
        public sealed class PassThru : IGremlinMode, ISingletonMessage
        {
            private PassThru() { }

            /// <summary>TBD</summary>
            public static readonly PassThru Instance = new PassThru();
        }

        /// <summary>TBD</summary>
        [MessagePackObject]
        public sealed class Drop : IGremlinMode
        {
            /// <summary>TBD</summary>
            /// <param name="outboundDropP">TBD</param>
            /// <param name="inboundDropP">TBD</param>
            [SerializationConstructor]
            public Drop(double outboundDropP, double inboundDropP)
            {
                InboundDropP = inboundDropP;
                OutboundDropP = outboundDropP;
            }

            /// <summary>TBD</summary>
            [Key(0)]
            public readonly double OutboundDropP;

            /// <summary>TBD</summary>
            [Key(1)]
            public readonly double InboundDropP;
        }

        #endregion

        /// <summary>TBD</summary>
        public readonly ExtendedActorSystem ExtendedActorSystem;

        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        /// <param name="extendedActorSystem">TBD</param>
        public FailureInjectorTransportAdapter(Transport wrappedTransport, ExtendedActorSystem extendedActorSystem) : base(wrappedTransport)
        {
            ExtendedActorSystem = extendedActorSystem;
            _log = Logging.GetLogger(ExtendedActorSystem, this);
            _shouldDebugLog = ExtendedActorSystem.Settings.Config.GetBoolean("akka.remote.gremlin.debug", false);
        }

        private ILoggingAdapter _log;

        private Random Rng => ThreadLocalRandom.Current;

        private readonly bool _shouldDebugLog;
        private volatile IAssociationEventListener _upstreamListener = null;
        private readonly ConcurrentDictionary<Address, IGremlinMode> addressChaosTable = new ConcurrentDictionary<Address, IGremlinMode>(AddressComparer.Instance);
        private volatile IGremlinMode _allMode = PassThru.Instance;

        ///// <summary>TBD</summary>
        //private int MaximumOverhead = 0;

        #region - AbstractTransportAdapter members -

        // ReSharper disable once InconsistentNaming
        private static readonly SchemeAugmenter _augmenter = new SchemeAugmenter(FailureInjectorSchemeIdentifier);

        /// <summary>TBD</summary>
        protected override SchemeAugmenter SchemeAugmenter => _augmenter;

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        public override Task<bool> ManagementCommand(object message)
        {
            switch (message)
            {
                case All all:
                    Interlocked.Exchange(ref _allMode, all.Mode);
                    return TaskConstants.BooleanTrue;

                case One one:
                    // don't care about the protocol part - we are injected in the stack anyway!
                    addressChaosTable.AddOrUpdate(NakedAddress(one.RemoteAddress), address => one.Mode, (address, mode) => one.Mode);
                    return TaskConstants.BooleanTrue;

                default:
                    return WrappedTransport.ManagementCommand(message);
            }
        }

        #endregion

        #region - IAssociationEventListener members -

        /// <summary>TBD</summary>
        /// <param name="listenAddress">TBD</param>
        /// <param name="listenerTask">TBD</param>
        /// <returns>TBD</returns>
        protected override Task<IAssociationEventListener> InterceptListen(Address listenAddress, Task<IAssociationEventListener> listenerTask)
        {
            if (_log.IsWarningEnabled) _log.FailureInjectorTransportIsActiveOnThisSystem();
            listenerTask.Then(AfterSetupAssociationEventListenerAction, this,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            return Task.FromResult((IAssociationEventListener)this);
        }

        private static readonly Action<IAssociationEventListener, FailureInjectorTransportAdapter> AfterSetupAssociationEventListenerAction = AfterSetupAssociationEventListener;
        private static void AfterSetupAssociationEventListener(IAssociationEventListener listener, FailureInjectorTransportAdapter owner)
        {
            // Side effecting: As this class is not an actor, the only way to safely modify state
            // is through volatile vars. Listen is called only during the initialization of the
            // stack, and upstreamListener is not read before this finishes.
            Interlocked.Exchange(ref owner._upstreamListener, listener);
        }

        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="statusPromise">TBD</param>
        /// <exception cref="FailureInjectorException">TBD</exception>
        protected override void InterceptAssociate(Address remoteAddress, TaskCompletionSource<AssociationHandle> statusPromise)
        {
            // Association is simulated to be failed if there was either an inbound or outbound
            // message drop
            if (ShouldDropInbound(remoteAddress, new object(), "interceptAssociate") ||
                ShouldDropOutbound(remoteAddress, new object(), "interceptAssociate"))
            {
                statusPromise.TrySetException(
                    new FailureInjectorException($"Simulated failure of association to {remoteAddress}"));
            }
            else
            {
                WrappedTransport
                    .Associate(remoteAddress)
                    .Then(AfterConnectionIsOpenedAction, this, statusPromise, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private static readonly Action<AssociationHandle, FailureInjectorTransportAdapter, TaskCompletionSource<AssociationHandle>> AfterConnectionIsOpenedAction = AfterConnectionIsOpened;
        private static void AfterConnectionIsOpened(AssociationHandle handle, FailureInjectorTransportAdapter owner, TaskCompletionSource<AssociationHandle> statusPromise)
        {
            owner.addressChaosTable.AddOrUpdate(NakedAddress(handle.RemoteAddress), address => PassThru.Instance,
                (address, mode) => PassThru.Instance);
            statusPromise.SetResult(new FailureInjectorHandle(handle, owner));
        }

        /// <summary>TBD</summary>
        /// <param name="ev">TBD</param>
        public void Notify(IAssociationEvent ev)
        {
            if (ev is InboundAssociation inboundAssociation && ShouldDropInbound(inboundAssociation.Association.RemoteAddress, ev, "notify"))
            {
                //ignore
            }
            else
            {
                if (_upstreamListener is null)
                {
                }
                else
                {
                    _upstreamListener.Notify(InterceptInboundAssociation(ev));
                }
            }
        }

        #endregion

        #region - Internal methods -

        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="instance">TBD</param>
        /// <param name="debugMessage">TBD</param>
        /// <returns>TBD</returns>
        public bool ShouldDropInbound(Address remoteAddress, object instance, string debugMessage)
        {
            var mode = ChaosMode(remoteAddress);
            switch (mode)
            {
                case PassThru _:
                    return false;

                case Drop drop:
                    if (Rng.NextDouble() <= drop.InboundDropP)
                    {
                        if (_shouldDebugLog)
                        {
                            _log.DroppingInbound(instance, remoteAddress, debugMessage);
                        }

                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>TBD</summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="instance">TBD</param>
        /// <param name="debugMessage">TBD</param>
        /// <returns>TBD</returns>
        public bool ShouldDropOutbound(Address remoteAddress, object instance, string debugMessage)
        {
            var mode = ChaosMode(remoteAddress);
            switch (mode)
            {
                case PassThru _:
                    return false;

                case Drop drop:
                    if (Rng.NextDouble() <= drop.OutboundDropP)
                    {
                        if (_shouldDebugLog)
                        {
                            _log.DroppingOutbound(instance, remoteAddress, debugMessage);
                        }
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private IAssociationEvent InterceptInboundAssociation(IAssociationEvent ev)
        {
            if (ev is InboundAssociation inboundAssociation)
            {
                return new InboundAssociation(new FailureInjectorHandle(inboundAssociation.Association, this));
            }

            return ev;
        }

        private static Address NakedAddress(Address address)
        {
            return address.WithProtocol(string.Empty)
                          .WithSystem(string.Empty);
        }

        private IGremlinMode ChaosMode(Address remoteAddress)
        {
            if (addressChaosTable.TryGetValue(NakedAddress(remoteAddress), out var mode))
            {
                return mode;
            }

            return PassThru.Instance;
        }

        #endregion
    }

    #endregion

    #region == class FailureInjectorHandle ==

    /// <summary>INTERNAL API</summary>
    internal sealed class FailureInjectorHandle : AbstractTransportAdapterHandle, IHandleEventListener
    {
        private readonly FailureInjectorTransportAdapter _gremlinAdapter;
        private volatile IHandleEventListener _upstreamListener = null;

        /// <summary>TBD</summary>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="gremlinAdapter">TBD</param>
        public FailureInjectorHandle(AssociationHandle wrappedHandle, FailureInjectorTransportAdapter gremlinAdapter)
            : base(wrappedHandle, FailureInjectorTransportAdapter.FailureInjectorSchemeIdentifier)
        {
            _gremlinAdapter = gremlinAdapter;
            ReadHandlerSource.Task.Then(AfterSetupReadHandlerAction, this, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static readonly Action<IHandleEventListener, FailureInjectorHandle> AfterSetupReadHandlerAction = AfterSetupReadHandler;
        private static void AfterSetupReadHandler(IHandleEventListener listener, FailureInjectorHandle owner)
        {
            Interlocked.Exchange(ref owner._upstreamListener, listener);
            owner.WrappedHandle.ReadHandlerSource.SetResult(owner);
        }

        /// <summary>TBD</summary>
        /// <param name="payload">TBD</param>
        /// <returns>TBD</returns>
        public override bool Write(object payload)
        {
            if (!_gremlinAdapter.ShouldDropOutbound(WrappedHandle.RemoteAddress, payload, "handler.write"))
            {
                return WrappedHandle.Write(payload);
            }

            return true;
        }

        /// <summary>TBD</summary>
        public override void Disassociate() => WrappedHandle.Disassociate();

        #region - IHandleEventListener members -

        /// <summary>TBD</summary>
        /// <param name="ev">TBD</param>
        public void Notify(IHandleEvent ev)
        {
            if (!_gremlinAdapter.ShouldDropInbound(WrappedHandle.RemoteAddress, ev, "handler.notify"))
            {
                _upstreamListener.Notify(ev);
            }
        }

        #endregion
    }

    #endregion
}