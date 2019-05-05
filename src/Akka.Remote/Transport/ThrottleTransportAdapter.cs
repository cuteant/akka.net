//-----------------------------------------------------------------------
// <copyright file="ThrottleTransportAdapter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util;
using Akka.Util.Internal;
using MessagePack;

/*
 * 注意：
 * Throttler 保留现只是为了单元测试，没有什么实际用处了
 */

namespace Akka.Remote.Transport
{
    #region -- class ThrottlerProvider --

    /// <summary>Used to provide throttling controls for remote <see cref="Transport"/> instances.</summary>
    public sealed class ThrottlerProvider : ITransportAdapterProvider
    {
        /// <inheritdoc cref="ITransportAdapterProvider"/>
        public Transport Create(Transport wrappedTransport, ExtendedActorSystem system)
            => new ThrottleTransportAdapter(wrappedTransport, system);
    }

    #endregion

    #region -- class ThrottleTransportAdapter --

    /// <summary>INTERNAL API
    ///
    /// The throttler transport adapter
    /// </summary>
    public class ThrottleTransportAdapter : ActorTransportAdapter
    {
        #region - Static methods and self-contained data types -

        /// <summary>TBD</summary>
        public const string Scheme = "trttl";

        /// <summary>TBD</summary>
        public static readonly AtomicCounter UniqueId = new AtomicCounter(0);

        /// <summary>TBD</summary>
        public enum Direction
        {
            /// <summary>TBD</summary>
            Send,

            /// <summary>TBD</summary>
            Receive,

            /// <summary>TBD</summary>
            Both
        }

        #endregion

        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        /// <param name="system">TBD</param>
        public ThrottleTransportAdapter(Transport wrappedTransport, ActorSystem system)
            : base(wrappedTransport, system) { }

        // ReSharper disable once InconsistentNaming
        private static readonly SchemeAugmenter _schemeAugmenter = new SchemeAugmenter(Scheme);

        /// <summary>TBD</summary>
        protected override SchemeAugmenter SchemeAugmenter => _schemeAugmenter;

        /// <summary>The name of the actor managing the throttler</summary>
        protected override string ManagerName
            => $"throttlermanager.${WrappedTransport.SchemeIdentifier}${UniqueId.GetAndIncrement()}";

        /// <summary>The props for starting the <see cref="ThrottlerManager"/></summary>
        protected override Props ManagerProps
        {
            get
            {
                var wt = WrappedTransport;
                return Props.Create(() => new ThrottlerManager(wt));
            }
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        public override Task<bool> ManagementCommand(object message)
        {
            switch (message)
            {
                case SetThrottle _:
                    return manager.Ask(message, AskTimeout).Then(AfterSetThrottleFunc);

                case ForceDisassociate _:
                case ForceDisassociateExplicitly _:
                    return manager
                        .Ask(message, AskTimeout)
                        .Then(AfterForceDisassociateFunc, TaskContinuationOptions.ExecuteSynchronously);

                default:
                    return WrappedTransport.ManagementCommand(message);
            }
        }

        private static readonly Func<object, bool> AfterSetThrottleFunc = AfterSetThrottle;
        private static bool AfterSetThrottle(object result)
        {
            return result is SetThrottleAck;
        }

        private static readonly Func<object, bool> AfterForceDisassociateFunc = AfterForceDisassociate;
        private static bool AfterForceDisassociate(object result)
        {
            return result is ForceDisassociateAck;
        }
    }

    #endregion

    #region == class ForceDisassociate ==

    /// <summary>Management command to force disassociation of an address</summary>
    internal sealed class ForceDisassociate
    {
        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        public ForceDisassociate(Address address) => Address = address;

        /// <summary>TBD</summary>
        public Address Address { get; }
    }

    #endregion

    #region == class ForceDisassociateExplicitly ==

    /// <summary>Management command to force disassociation of an address with an explicit error.</summary>
    internal sealed class ForceDisassociateExplicitly
    {
        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        /// <param name="reason">TBD</param>
        public ForceDisassociateExplicitly(Address address, DisassociateInfo reason)
        {
            Reason = reason;
            Address = address;
        }

        /// <summary>TBD</summary>
        public Address Address { get; }

        /// <summary>TBD</summary>
        public DisassociateInfo Reason { get; }
    }

    #endregion

    #region == class ForceDisassociateAck ==

    /// <summary>INTERNAL API</summary>
    internal sealed class ForceDisassociateAck
    {
        private ForceDisassociateAck() { }

        /// <summary>TBD</summary>
        public static readonly ForceDisassociateAck Instance = new ForceDisassociateAck();
    }

    #endregion

    #region == class ThrottlerManager ==

    /// <summary>INTERNAL API</summary>
    internal sealed class ThrottlerManager : ActorTransportAdapterManager
    {
        #region = Internal message classes =

        /// <summary>TBD</summary>
        internal sealed class Checkin : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="origin">TBD</param>
            /// <param name="handle">TBD</param>
            public Checkin(Address origin, ThrottlerHandle handle)
            {
                ThrottlerHandle = handle;
                Origin = origin;
            }

            /// <summary>TBD</summary>
            public Address Origin { get; }

            /// <summary>TBD</summary>
            public ThrottlerHandle ThrottlerHandle { get; }
        }

        /// <summary>TBD</summary>
        internal sealed class AssociateResult : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="associationHandle">TBD</param>
            /// <param name="statusPromise">TBD</param>
            public AssociateResult(AssociationHandle associationHandle, TaskCompletionSource<AssociationHandle> statusPromise)
            {
                StatusPromise = statusPromise;
                AssociationHandle = associationHandle;
            }

            /// <summary>TBD</summary>
            public AssociationHandle AssociationHandle { get; }

            /// <summary>TBD</summary>
            public TaskCompletionSource<AssociationHandle> StatusPromise { get; }
        }

        /// <summary>TBD</summary>
        internal sealed class ListenerAndMode : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="handleEventListener">TBD</param>
            /// <param name="mode">TBD</param>
            public ListenerAndMode(IHandleEventListener handleEventListener, ThrottleMode mode)
            {
                Mode = mode;
                HandleEventListener = handleEventListener;
            }

            /// <summary>TBD</summary>
            public IHandleEventListener HandleEventListener { get; }

            /// <summary>TBD</summary>
            public ThrottleMode Mode { get; }
        }

        /// <summary>TBD</summary>
        internal sealed class Handle : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="throttlerHandle">TBD</param>
            public Handle(ThrottlerHandle throttlerHandle) => ThrottlerHandle = throttlerHandle;

            /// <summary>TBD</summary>
            public ThrottlerHandle ThrottlerHandle { get; }
        }

        /// <summary>TBD</summary>
        internal sealed class Listener : INoSerializationVerificationNeeded
        {
            /// <summary>TBD</summary>
            /// <param name="handleEventListener">TBD</param>
            public Listener(IHandleEventListener handleEventListener) => HandleEventListener = handleEventListener;

            /// <summary>TBD</summary>
            public IHandleEventListener HandleEventListener { get; }
        }

        #endregion

        /// <summary>TBD</summary>
        private readonly Transport WrappedTransport;

        private Dictionary<Address, Tuple<ThrottleMode, ThrottleTransportAdapter.Direction>> _throttlingModes
            = new Dictionary<Address, Tuple<ThrottleMode, ThrottleTransportAdapter.Direction>>(AddressComparer.Instance);

        private List<Tuple<Address, ThrottlerHandle>> _handleTable = new List<Tuple<Address, ThrottlerHandle>>();

        /// <summary>TBD</summary>
        /// <param name="wrappedTransport">TBD</param>
        public ThrottlerManager(Transport wrappedTransport) => WrappedTransport = wrappedTransport;

        #region + Ready +

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        protected override void Ready(object message)
        {
            switch (message)
            {
                case InboundAssociation ia:
                    var wrappedHandle0 = WrapHandle(ia.Association, AssociationListener, true);
                    wrappedHandle0.ThrottlerActor.Tell(new Handle(wrappedHandle0));
                    break;

                case AssociateUnderlying ua:
                    // Slight modification of PipeTo, only success is sent, failure is propagated to
                    // a separate Task
                    var associateTask = WrappedTransport.Associate(ua.RemoteAddress);
                    var self = Self;
                    associateTask.LinkOutcome(AfterAssociateAction, self, ua, TaskContinuationOptions.ExecuteSynchronously);
                    break;

                case AssociateResult ar:  // Finished outbound association and got back the handle
                    var wrappedHandle1 = WrapHandle(ar.AssociationHandle, AssociationListener, false);
                    var naked1 = NakedAddress(ar.AssociationHandle.RemoteAddress);
                    var inMode = GetInboundMode(naked1);
                    wrappedHandle1.OutboundThrottleMode.Value = GetOutboundMode(naked1);
                    wrappedHandle1.ReadHandlerSource.Task.ContinueWith(tr => new ListenerAndMode(tr.Result, inMode), TaskContinuationOptions.ExecuteSynchronously)
                        .PipeTo(wrappedHandle1.ThrottlerActor);
                    _handleTable.Add(Tuple.Create(naked1, wrappedHandle1));
                    ar.StatusPromise.SetResult(wrappedHandle1);
                    break;

                case SetThrottle st:
                    var naked2 = NakedAddress(st.Address);
                    _throttlingModes[naked2] = new Tuple<ThrottleMode, ThrottleTransportAdapter.Direction>(st.Mode, st.Direction);
                    var ok = Task.FromResult(SetThrottleAck.Instance);
                    var modes = new List<Task<SetThrottleAck>>() { ok };
                    foreach (var handle in _handleTable)
                    {
                        if (handle.Item1 == naked2)
                        {
                            modes.Add(SetMode(handle.Item2, st.Mode, st.Direction));
                        }
                    }

                    var sender = Sender;
                    Task.WhenAll(modes).ContinueWith(tr =>
                    {
                        return SetThrottleAck.Instance;
                    }, TaskContinuationOptions.ExecuteSynchronously).PipeTo(sender);
                    break;

                case ForceDisassociate fd:
                    var naked3 = NakedAddress(fd.Address);
                    foreach (var handle in _handleTable)
                    {
                        if (handle.Item1 == naked3) { handle.Item2.Disassociate(); }
                    }

                    /*
                     * NOTE: Important difference between Akka.NET and Akka here.
                     * In canonical Akka, ThrottleHandlers are never removed from
                     * the _handleTable. The reason is because Ask-ing a terminated ActorRef
                     * doesn't cause any exceptions to be thrown upstream - it just times out
                     * and propagates a failed Future.
                     *
                     * In the CLR, a CancellationException gets thrown and causes all
                     * parent tasks chaining back to the EndPointManager to fail due
                     * to an Ask timeout.
                     *
                     * So in order to avoid this problem, we remove any disassociated handles
                     * from the _handleTable.
                     *
                     * Questions? Ask @Aaronontheweb
                     */
                    _handleTable.RemoveAll(tuple => tuple.Item1 == naked3);
                    Sender.Tell(ForceDisassociateAck.Instance);
                    break;

                case ForceDisassociateExplicitly fde:
                    var naked4 = NakedAddress(fde.Address);
                    foreach (var handle in _handleTable)
                    {
                        if (handle.Item1 == naked4)
                            handle.Item2.DisassociateWithFailure(fde.Reason);
                    }

                    /*
                     * NOTE: Important difference between Akka.NET and Akka here.
                     * In canonical Akka, ThrottleHandlers are never removed from
                     * the _handleTable. The reason is because Ask-ing a terminated ActorRef
                     * doesn't cause any exceptions to be thrown upstream - it just times out
                     * and propagates a failed Future.
                     *
                     * In the CLR, a CancellationException gets thrown and causes all
                     * parent tasks chaining back to the EndPointManager to fail due
                     * to an Ask timeout.
                     *
                     * So in order to avoid this problem, we remove any disassociated handles
                     * from the _handleTable.
                     *
                     * Questions? Ask @Aaronontheweb
                     */
                    _handleTable.RemoveAll(tuple => tuple.Item1 == naked4);
                    Sender.Tell(ForceDisassociateAck.Instance);
                    break;

                case Checkin chkin:
                    var naked5 = NakedAddress(chkin.Origin);
                    _handleTable.Add(new Tuple<Address, ThrottlerHandle>(naked5, chkin.ThrottlerHandle));
                    SetMode(naked5, chkin.ThrottlerHandle);
                    break;

                default:
                    break;
            }
        }

        private static readonly Action<Task<AssociationHandle>, IActorRef, AssociateUnderlying> AfterAssociateAction = AfterAssociate;
        private static void AfterAssociate(Task<AssociationHandle> tr, IActorRef self, AssociateUnderlying ua)
        {
            if (tr.IsSuccessfully())
            {
                self.Tell(new AssociateResult(tr.Result, ua.StatusPromise));
            }
            else
            {
                ua.StatusPromise.TrySetUnwrappedException(tr.Exception ?? new Exception("association failed"));
            }
        }

        #endregion

        #region * ThrottlerManager internal methods *

        private static Address NakedAddress(Address address)
        {
            return address.WithProtocol(string.Empty)
                          .WithSystem(string.Empty);
        }

        private ThrottleMode GetInboundMode(Address nakedAddress)
        {
            if (_throttlingModes.TryGetValue(nakedAddress, out var mode))
            {
                if (mode.Item2 == ThrottleTransportAdapter.Direction.Both || mode.Item2 == ThrottleTransportAdapter.Direction.Receive)
                {
                    return mode.Item1;
                }
            }

            return Unthrottled.Instance;
        }

        private ThrottleMode GetOutboundMode(Address nakedAddress)
        {
            if (_throttlingModes.TryGetValue(nakedAddress, out var mode))
            {
                if (mode.Item2 == ThrottleTransportAdapter.Direction.Both || mode.Item2 == ThrottleTransportAdapter.Direction.Send)
                {
                    return mode.Item1;
                }
            }

            return Unthrottled.Instance;
        }

        private Task<SetThrottleAck> SetMode(Address nakedAddress, ThrottlerHandle handle)
        {
            if (_throttlingModes.TryGetValue(nakedAddress, out var mode))
            {
                return SetMode(handle, mode.Item1, mode.Item2);
            }

            return SetMode(handle, Unthrottled.Instance, ThrottleTransportAdapter.Direction.Both);
        }

        private static Task<SetThrottleAck> SetMode(ThrottlerHandle handle, ThrottleMode mode,
            ThrottleTransportAdapter.Direction direction)
        {
            switch (direction)
            {
                case ThrottleTransportAdapter.Direction.Send:
                    handle.OutboundThrottleMode.Value = mode;
                    return Task.FromResult(SetThrottleAck.Instance);

                case ThrottleTransportAdapter.Direction.Receive:
                    return AskModeWithDeathCompletion(handle.ThrottlerActor, mode, ActorTransportAdapter.AskTimeout);

                case ThrottleTransportAdapter.Direction.Both:
                    handle.OutboundThrottleMode.Value = mode;
                    return AskModeWithDeathCompletion(handle.ThrottlerActor, mode, ActorTransportAdapter.AskTimeout);

                default:
                    return Task.FromResult(SetThrottleAck.Instance);
            }
            //if (direction == ThrottleTransportAdapter.Direction.Both ||
            //    direction == ThrottleTransportAdapter.Direction.Send)
            //{
            //    handle.OutboundThrottleMode.Value = mode;
            //}

            //if (direction == ThrottleTransportAdapter.Direction.Both ||
            //    direction == ThrottleTransportAdapter.Direction.Receive)
            //{
            //    return AskModeWithDeathCompletion(handle.ThrottlerActor, mode, ActorTransportAdapter.AskTimeout);
            //}
            //else
            //{
            //    return Task.FromResult(SetThrottleAck.Instance);
            //}
        }

        private static Task<SetThrottleAck> AskModeWithDeathCompletion(IActorRef target, ThrottleMode mode, TimeSpan timeout)
        {
            if (target.IsNobody()) { return Task.FromResult(SetThrottleAck.Instance); }

            var internalTarget = target.AsInstanceOf<IInternalActorRef>();
            var promiseRef = PromiseActorRef.Apply(internalTarget.Provider, timeout, target, mode.GetType().Name);
            internalTarget.SendSystemMessage(new Watch(internalTarget, promiseRef));
            target.Tell(mode, promiseRef);
            return promiseRef.Result.ContinueWith(tr =>
            {
                if (tr.Result is Terminated t && t.ActorRef.Path.Equals(target.Path))
                {
                    return SetThrottleAck.Instance;
                }
                internalTarget.SendSystemMessage(new Unwatch(internalTarget, promiseRef));
                return SetThrottleAck.Instance;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private ThrottlerHandle WrapHandle(AssociationHandle originalHandle, IAssociationEventListener listener, bool inbound)
        {
            var managerRef = Self;
            return new ThrottlerHandle(originalHandle, Context.ActorOf(
                RARP.For(Context.System).ConfigureDispatcher(
                Props.Create(() => new ThrottledAssociation(managerRef, listener, originalHandle, inbound)).WithDeploy(Deploy.Local)),
                "throttler" + NextId()));
        }

        #endregion
    }

    #endregion


    #region -- class ThrottleMode --

    /// <summary>The type of throttle being applied to a connection.</summary>
    public abstract class ThrottleMode : INoSerializationVerificationNeeded
    {
        /// <summary>TBD</summary>
        /// <param name="nanoTimeOfSend">TBD</param>
        /// <param name="tokens">TBD</param>
        /// <returns>TBD</returns>
        public abstract Tuple<ThrottleMode, bool> TryConsumeTokens(long nanoTimeOfSend, int tokens);

        /// <summary>TBD</summary>
        /// <param name="currentNanoTime">TBD</param>
        /// <param name="tokens">TBD</param>
        /// <returns>TBD</returns>
        public abstract TimeSpan TimeToAvailable(long currentNanoTime, int tokens);
    }

    #endregion

    #region -- class Blackhole --

    /// <summary>Signals that we're going to totally black out a connection</summary>
    public sealed class Blackhole : ThrottleMode
    {
        private Blackhole() { }

        /// <summary>The singleton instance</summary>
        public static readonly Blackhole Instance = new Blackhole();

        /// <inheritdoc/>
        public override Tuple<ThrottleMode, bool> TryConsumeTokens(long nanoTimeOfSend, int tokens)
            => Tuple.Create<ThrottleMode, bool>(this, false);

        /// <inheritdoc/>
        public override TimeSpan TimeToAvailable(long currentNanoTime, int tokens) => TimeSpan.Zero;
    }

    #endregion

    #region -- class Unthrottled --

    /// <summary>Unthrottles a previously throttled connection</summary>
    public sealed class Unthrottled : ThrottleMode
    {
        private Unthrottled() { }

        /// <summary>TBD</summary>
        public static readonly Unthrottled Instance = new Unthrottled();

        /// <inheritdoc/>
        public override Tuple<ThrottleMode, bool> TryConsumeTokens(long nanoTimeOfSend, int tokens)
            => Tuple.Create<ThrottleMode, bool>(this, true);

        /// <inheritdoc/>
        public override TimeSpan TimeToAvailable(long currentNanoTime, int tokens) => TimeSpan.Zero;
    }

    #endregion

    #region == class TokenBucket ==

    /// <summary>Applies token-bucket throttling to introduce latency to a connection</summary>
    internal sealed class TokenBucket : ThrottleMode
    {
        private readonly int _capacity;
        private readonly double _tokensPerSecond;
        private readonly long _nanoTimeOfLastSend;
        private readonly int _availableTokens;

        /// <summary>TBD</summary>
        /// <param name="capacity">TBD</param>
        /// <param name="tokensPerSecond">TBD</param>
        /// <param name="nanoTimeOfLastSend">TBD</param>
        /// <param name="availableTokens">TBD</param>
        public TokenBucket(int capacity, double tokensPerSecond, long nanoTimeOfLastSend, int availableTokens)
        {
            _capacity = capacity;
            _tokensPerSecond = tokensPerSecond;
            _nanoTimeOfLastSend = nanoTimeOfLastSend;
            _availableTokens = availableTokens;
        }

        private bool IsAvailable(long nanoTimeOfSend, int tokens)
        {
            if (tokens > _capacity && _availableTokens > 0)
            {
                return true; // Allow messages larger than capacity through, it will be recorded as negative tokens
            }

            return Math.Min(_availableTokens + TokensGenerated(nanoTimeOfSend), _capacity) >= tokens;
        }

        /// <inheritdoc/>
        public override Tuple<ThrottleMode, bool> TryConsumeTokens(long nanoTimeOfSend, int tokens)
        {
            if (IsAvailable(nanoTimeOfSend, tokens))
            {
                return Tuple.Create<ThrottleMode, bool>(Copy(
                    nanoTimeOfLastSend: nanoTimeOfSend,
                    availableTokens: Math.Min(_availableTokens - tokens + TokensGenerated(nanoTimeOfSend), _capacity))
                    , true);
            }
            return Tuple.Create<ThrottleMode, bool>(this, false);
        }

        /// <inheritdoc/>
        public override TimeSpan TimeToAvailable(long currentNanoTime, int tokens)
        {
            var needed = (tokens > _capacity ? 1 : tokens) - TokensGenerated(currentNanoTime);
            return TimeSpan.FromSeconds(needed / _tokensPerSecond);
        }

        private int TokensGenerated(long nanoTimeOfSend)
        {
            var milliSecondsSinceLastSend = ((nanoTimeOfSend - _nanoTimeOfLastSend).ToTicks() / TimeSpan.TicksPerMillisecond);
            var tokensGenerated = milliSecondsSinceLastSend * _tokensPerSecond / 1000;
            return Convert.ToInt32(tokensGenerated);
        }

        private TokenBucket Copy(int? capacity = null, double? tokensPerSecond = null, long? nanoTimeOfLastSend = null, int? availableTokens = null)
        {
            return new TokenBucket(
                capacity ?? _capacity,
                tokensPerSecond ?? _tokensPerSecond,
                nanoTimeOfLastSend ?? _nanoTimeOfLastSend,
                availableTokens ?? _availableTokens);
        }

        private bool Equals(TokenBucket other)
        {
            return _capacity == other._capacity
                && _tokensPerSecond.Equals(other._tokensPerSecond)
                && _nanoTimeOfLastSend == other._nanoTimeOfLastSend
                && _availableTokens == other._availableTokens;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TokenBucket tokenBucket && Equals(tokenBucket);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _capacity;
                hashCode = (hashCode * 397) ^ _tokensPerSecond.GetHashCode();
                hashCode = (hashCode * 397) ^ _nanoTimeOfLastSend.GetHashCode();
                hashCode = (hashCode * 397) ^ _availableTokens;
                return hashCode;
            }
        }

        /// <summary>Compares two specified <see cref="TokenBucket"/> for equality.</summary>
        /// <param name="left">The first <see cref="TokenBucket"/> used for comparison</param>
        /// <param name="right">The second <see cref="TokenBucket"/> used for comparison</param>
        /// <returns>
        /// <c>true</c> if both <see cref="TokenBucket">TokenBuckets</see> are equal; otherwise <c>false</c>
        /// </returns>
        public static bool operator ==(TokenBucket left, TokenBucket right) => Equals(left, right);

        /// <summary>Compares two specified <see cref="TokenBucket"/> for inequality.</summary>
        /// <param name="left">The first <see cref="TokenBucket"/> used for comparison</param>
        /// <param name="right">The second <see cref="TokenBucket"/> used for comparison</param>
        /// <returns>
        /// <c>true</c> if both <see cref="TokenBucket">TokenBuckets</see> are not equal; otherwise <c>false</c>
        /// </returns>
        public static bool operator !=(TokenBucket left, TokenBucket right) => !Equals(left, right);
    }

    #endregion


    #region == class SetThrottle ==

    /// <summary>Applies a throttle to the underlying conneciton</summary>
    public sealed class SetThrottle
    {
        private readonly Address _address;

        /// <summary>The address of the remote node we'll be throttling</summary>
        public Address Address => _address;

        private readonly ThrottleTransportAdapter.Direction _direction;

        /// <summary>The direction of the throttle</summary>
        public ThrottleTransportAdapter.Direction Direction => _direction;

        private readonly ThrottleMode _mode;

        /// <summary>The mode of the throttle</summary>
        public ThrottleMode Mode => _mode;

        /// <summary>Creates a new SetThrottle message.</summary>
        /// <param name="address">The address of the throttle.</param>
        /// <param name="direction">The direction of the throttle.</param>
        /// <param name="mode">The mode of the throttle.</param>
        public SetThrottle(Address address, ThrottleTransportAdapter.Direction direction, ThrottleMode mode)
        {
            _address = address;
            _direction = direction;
            _mode = mode;
        }

        private bool Equals(SetThrottle other)
            => Equals(_address, other._address) && _direction == other._direction && Equals(_mode, other._mode);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SetThrottle setThrottle && Equals(setThrottle);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_address != null ? _address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)_direction;
                hashCode = (hashCode * 397) ^ (_mode != null ? _mode.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>Compares two specified <see cref="SetThrottle"/> for equality.</summary>
        /// <param name="left">The first <see cref="SetThrottle"/> used for comparison</param>
        /// <param name="right">The second <see cref="SetThrottle"/> used for comparison</param>
        /// <returns>
        /// <c>true</c> if both <see cref="SetThrottle">SetThrottles</see> are equal; otherwise <c>false</c>
        /// </returns>
        public static bool operator ==(SetThrottle left, SetThrottle right) => Equals(left, right);

        /// <summary>Compares two specified <see cref="SetThrottle"/> for inequality.</summary>
        /// <param name="left">The first <see cref="SetThrottle"/> used for comparison</param>
        /// <param name="right">The second <see cref="SetThrottle"/> used for comparison</param>
        /// <returns>
        /// <c>true</c> if both <see cref="SetThrottle">SetThrottles</see> are not equal; otherwise <c>false</c>
        /// </returns>
        public static bool operator !=(SetThrottle left, SetThrottle right) => !Equals(left, right);
    }

    #endregion

    #region == class SetThrottleAck ==

    /// <summary>ACKs a throttle command</summary>
    public sealed class SetThrottleAck : ISingletonMessage
    {
        private SetThrottleAck() { }

        /// <summary>TBD</summary>
        public static readonly SetThrottleAck Instance = new SetThrottleAck();
    }

    #endregion

    #region == class ThrottlerHandle ==

    /// <summary>INTERNAL API</summary>
    internal sealed class ThrottlerHandle : AbstractTransportAdapterHandle
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;
        internal readonly IActorRef ThrottlerActor;

        internal AtomicReference<ThrottleMode> OutboundThrottleMode = new AtomicReference<ThrottleMode>(Unthrottled.Instance);

        /// <summary>TBD</summary>
        /// <param name="wrappedHandle">TBD</param>
        /// <param name="throttlerActor">TBD</param>
        public ThrottlerHandle(AssociationHandle wrappedHandle, IActorRef throttlerActor) : base(wrappedHandle, ThrottleTransportAdapter.Scheme)
        {
            ThrottlerActor = throttlerActor;
        }

        /// <inheritdoc/>
        public override bool Write(object payload)
        {
            var bts = MessagePackSerializer.Serialize<object>(payload, s_defaultResolver);
            var tokens = bts.Length;
            //need to declare recursive delegates first before they can self-reference
            //might want to consider making this consumer function strongly typed: http://blogs.msdn.com/b/wesdyer/archive/2007/02/02/anonymous-recursion-in-c.aspx
            bool TryConsume(ThrottleMode currentBucket)
            {
                var timeOfSend = MonotonicClock.GetNanos();
                var res = currentBucket.TryConsumeTokens(timeOfSend, tokens);
                var newBucket = res.Item1;
                var allow = res.Item2;
                if (allow)
                {
                    return OutboundThrottleMode.CompareAndSet(currentBucket, newBucket) || TryConsume(OutboundThrottleMode.Value);
                }
                return false;
            }

            var throttleMode = OutboundThrottleMode.Value;
            if (throttleMode is Blackhole) return true;

            var success = TryConsume(OutboundThrottleMode.Value);
            return success && WrappedHandle.Write(payload);
        }

        /// <inheritdoc/>
        public override void Disassociate() => ThrottlerActor.Tell(PoisonPill.Instance);

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        public void DisassociateWithFailure(DisassociateInfo reason) => ThrottlerActor.Tell(new ThrottledAssociation.FailWith(reason));
    }

    #endregion

    #region == class ThrottledAssociation ==

    /// <summary>INTERNAL API</summary>
    internal class ThrottledAssociation : FSM<ThrottledAssociation.ThrottlerState, ThrottledAssociation.IThrottlerData>, ILoggingFSM
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance;

        #region - ThrottledAssociation FSM state and data classes -

        private const string DequeueTimerName = "dequeue";

        /// <summary>TBD</summary>
        private sealed class Dequeue { }

        /// <summary>TBD</summary>
        public enum ThrottlerState
        {
            /*
             * STATES FOR INBOUND ASSOCIATIONS
             */
            /// <summary>Waiting for the <see cref="ThrottlerHandle"/> coupled with the throttler actor.</summary>
            WaitExposedHandle,

            /// <summary>Waiting for the ASSOCIATE message that contains the origin address of the remote endpoint.</summary>
            WaitOrigin,

            /// <summary>After origin is known and a Checkin message is sent to the manager, we must wait for
            /// the <see cref="ThrottleMode"/> for the address.</summary>
            WaitMode,

            /// <summary>After all information is known, the throttler must wait for the upstream listener to
            /// be able to forward messages.</summary>
            WaitUpstreamListener,

            /*
             * STATES FOR OUTBOUND ASSOCIATIONS
             */
            /// <summary>Waiting for the tuple containing the upstream listener and the <see cref="ThrottleMode"/>.</summary>
            WaitModeAndUpstreamListener,

            /// <summary>Fully initialized state</summary>
            Throttling
        }

        /// <summary>TBD</summary>
        internal interface IThrottlerData { }

        /// <summary>TBD</summary>
        internal class Uninitialized : IThrottlerData
        {
            private Uninitialized() { }

            /// <summary>TBD</summary>
            public static readonly Uninitialized Instance = new Uninitialized();
        }

        /// <summary>TBD</summary>
        internal sealed class ExposedHandle : IThrottlerData
        {
            /// <summary>TBD</summary>
            /// <param name="handle">TBD</param>
            public ExposedHandle(ThrottlerHandle handle) => Handle = handle;

            /// <summary>TBD</summary>
            public ThrottlerHandle Handle { get; }
        }

        /// <summary>TBD</summary>
        internal sealed class FailWith
        {
            /// <summary>TBD</summary>
            /// <param name="failReason">TBD</param>
            public FailWith(DisassociateInfo failReason) => FailReason = failReason;

            /// <summary>TBD</summary>
            public DisassociateInfo FailReason { get; }
        }

        #endregion

        /// <summary>TBD</summary>
        protected readonly IActorRef Manager;

        /// <summary>TBD</summary>
        protected readonly IAssociationEventListener AssociationHandler;

        /// <summary>TBD</summary>
        protected readonly AssociationHandle OriginalHandle;

        /// <summary>TBD</summary>
        protected readonly bool Inbound;

        /// <summary>TBD</summary>
        protected ThrottleMode InboundThrottleMode;

        /// <summary>TBD</summary>
        protected Queue<object> ThrottledMessages = new Queue<object>();

        /// <summary>TBD</summary>
        protected IHandleEventListener UpstreamListener;

        /// <summary>Used for decoding certain types of throttled messages on-the-fly</summary>
        private readonly AkkaPduMessagePackCodec _codec;

        /// <summary>TBD</summary>
        /// <param name="manager">TBD</param>
        /// <param name="associationHandler">TBD</param>
        /// <param name="originalHandle">TBD</param>
        /// <param name="inbound">TBD</param>
        public ThrottledAssociation(IActorRef manager, IAssociationEventListener associationHandler, AssociationHandle originalHandle, bool inbound)
        {
            _codec = new AkkaPduMessagePackCodec(Context.System);
            Manager = manager;
            AssociationHandler = associationHandler;
            OriginalHandle = originalHandle;
            Inbound = inbound;
            InitializeFSM();
        }

        private void InitializeFSM()
        {
            When(ThrottlerState.WaitExposedHandle, HandleWhenWaitExposedHandle);

            When(ThrottlerState.WaitOrigin, HandleWhenWaitOrigin);

            When(ThrottlerState.WaitMode, HandleWhenWaitMode);

            When(ThrottlerState.WaitUpstreamListener, HandleWhenWaitUpstreamListener);

            When(ThrottlerState.WaitModeAndUpstreamListener, HandleWhenWaitModeAndUpstreamListener);

            When(ThrottlerState.Throttling, HandleWhenThrottling);

            WhenUnhandled(HandleWhenUnhandled);

            if (Inbound)
            {
                StartWith(ThrottlerState.WaitExposedHandle, Uninitialized.Instance);
            }
            else
            {
                OriginalHandle.ReadHandlerSource.SetResult(new ActorHandleEventListener(Self));
                StartWith(ThrottlerState.WaitModeAndUpstreamListener, Uninitialized.Instance);
            }
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenWaitExposedHandle(Event<IThrottlerData> @event)
        {
            if (@event.FsmEvent is ThrottlerManager.Handle throttlerManagerHandle && @event.StateData is Uninitialized)
            {
                // register to downstream layer and wait for origin
                OriginalHandle.ReadHandlerSource.SetResult(new ActorHandleEventListener(Self));
                return
                    GoTo(ThrottlerState.WaitOrigin)
                        .Using(new ExposedHandle(throttlerManagerHandle.ThrottlerHandle));
            }
            return null;
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenWaitOrigin(Event<IThrottlerData> @event)
        {
            if (@event.FsmEvent is InboundPayload inboundPayload && @event.StateData is ExposedHandle exposedHandle)
            {
                var b = inboundPayload.Payload;
                ThrottledMessages.Enqueue(b);
                var origin = PeekOrigin(b);
                if (origin != null)
                {
                    Manager.Tell(new ThrottlerManager.Checkin(origin, exposedHandle.Handle));
                    return GoTo(ThrottlerState.WaitMode);
                }
                return Stay();
            }
            return null;
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenWaitMode(Event<IThrottlerData> @event)
        {
            switch (@event.FsmEvent)
            {
                case InboundPayload inboundPayload:
                    var b = inboundPayload.Payload;
                    ThrottledMessages.Enqueue(b);
                    return Stay();

                case ThrottleMode mode when @event.StateData is ExposedHandle exposedHandler:
                    var exposedHandle = exposedHandler.Handle;
                    InboundThrottleMode = mode;
                    try
                    {
                        if (mode is Blackhole)
                        {
                            ThrottledMessages.Clear();// = new Queue<byte[]>();
                            exposedHandle.Disassociate();
                            return Stop();
                        }
                        else
                        {
                            AssociationHandler.Notify(new InboundAssociation(exposedHandle));
                            var self = Self;
                            exposedHandle.ReadHandlerSource.Task.ContinueWith(
                                r => new ThrottlerManager.Listener(r.Result),
                                TaskContinuationOptions.ExecuteSynchronously)
                                .PipeTo(self);
                            return GoTo(ThrottlerState.WaitUpstreamListener);
                        }
                    }
                    finally
                    {
                        Sender.Tell(SetThrottleAck.Instance);
                    }

                default:
                    return null;
            }
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenWaitUpstreamListener(Event<IThrottlerData> @event)
        {
            switch (@event.FsmEvent)
            {
                case InboundPayload inboundPayload:
                    ThrottledMessages.Enqueue(inboundPayload.Payload);
                    return Stay();

                case ThrottlerManager.Listener throttlerManagerListener:
                    UpstreamListener = throttlerManagerListener.HandleEventListener;
                    Self.Tell(new Dequeue());
                    return GoTo(ThrottlerState.Throttling);

                default:
                    return null;
            }
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenWaitModeAndUpstreamListener(Event<IThrottlerData> @event)
        {
            switch (@event.FsmEvent)
            {
                case ThrottlerManager.ListenerAndMode listenerAndMode:
                    UpstreamListener = listenerAndMode.HandleEventListener;
                    InboundThrottleMode = listenerAndMode.Mode;
                    Self.Tell(new Dequeue());
                    return GoTo(ThrottlerState.Throttling);

                case InboundPayload inboundPayload:
                    ThrottledMessages.Enqueue(inboundPayload.Payload);
                    return Stay();

                default:
                    return null;
            }
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenThrottling(Event<IThrottlerData> @event)
        {
            switch (@event.FsmEvent)
            {
                case ThrottleMode mode:
                    InboundThrottleMode = mode;
                    if (mode is Blackhole) { ThrottledMessages.Clear(); /*= new Queue<byte[]>();*/ }
                    CancelTimer(DequeueTimerName);
                    if (ThrottledMessages.Count > 0)
                    {
                        var bts = MessagePackSerializer.Serialize<object>(ThrottledMessages.Peek(), s_defaultResolver);
                        ScheduleDequeue(InboundThrottleMode.TimeToAvailable(MonotonicClock.GetNanos(), bts.Length));
                    }
                    Sender.Tell(SetThrottleAck.Instance);
                    return Stay();

                case InboundPayload inboundPayload:
                    ForwardOrDelay(inboundPayload.Payload);
                    return Stay();

                case Dequeue _:
                    if (ThrottledMessages.Count > 0)
                    {
                        var payload = ThrottledMessages.Dequeue();
                        UpstreamListener.Notify(new InboundPayload(payload));
                        var bts = MessagePackSerializer.Serialize<object>(payload, s_defaultResolver);
                        InboundThrottleMode = InboundThrottleMode.TryConsumeTokens(MonotonicClock.GetNanos(), bts.Length).Item1;
                        if (ThrottledMessages.Count > 0)
                        {
                            bts = MessagePackSerializer.Serialize<object>(ThrottledMessages.Peek(), s_defaultResolver);
                            ScheduleDequeue(InboundThrottleMode.TimeToAvailable(MonotonicClock.GetNanos(), bts.Length));
                        }
                    }
                    return Stay();

                default:
                    return null;
            }
        }

        private State<ThrottlerState, IThrottlerData> HandleWhenUnhandled(Event<IThrottlerData> @event)
        {
            switch (@event.FsmEvent)
            {
                // we should always set the throttling mode
                case ThrottleMode throttleMode:
                    InboundThrottleMode = throttleMode;
                    Sender.Tell(SetThrottleAck.Instance);
                    return Stay();

                case Disassociated _:
                    return Stop(); // not notifying the upstream handler is intentional: we are relying on heartbeating

                case FailWith failWith:
                    var reason = failWith.FailReason;
                    if (UpstreamListener != null) UpstreamListener.Notify(new Disassociated(reason));
                    return Stop();

                default:
                    return null;
            }
        }

        /// <summary>This method captures ASSOCIATE packets and extracts the origin <see cref="Address"/>.</summary>
        /// <param name="b">Inbound <see cref="T:System.byte[]"/> received from network.</param>
        /// <returns></returns>
        private Address PeekOrigin(object b)
        {
            try
            {
                var pdu = _codec.DecodePdu((Akka.Serialization.Protocol.AkkaProtocolMessage)b);
                if (pdu is Associate associate)
                {
                    return associate.Info.Origin;
                }
                return null;
            }
            catch
            {
                // This layer should not care about malformed packets. Also, this also useful for
                // testing, because arbitrary payload could be passed in
                return null;
            }
        }

        private void ScheduleDequeue(TimeSpan delay)
        {
            if (InboundThrottleMode is Blackhole)
            {
                return; //do nothing
            }
            if (delay <= TimeSpan.Zero)
            {
                Self.Tell(new Dequeue());
            }
            else
            {
                SetTimer(DequeueTimerName, new Dequeue(), delay, repeat: false);
            }
        }

        private void ForwardOrDelay(object payload)
        {
            if (InboundThrottleMode is Blackhole)
            {
                // Do nothing
            }
            else
            {
                if (ThrottledMessages.Count <= 0)
                {
                    var bts = MessagePackSerializer.Serialize<object>(payload, s_defaultResolver);
                    var tokens = bts.Length;
                    var res = InboundThrottleMode.TryConsumeTokens(MonotonicClock.GetNanos(), tokens);
                    var newBucket = res.Item1;
                    var success = res.Item2;
                    if (success)
                    {
                        InboundThrottleMode = newBucket;
                        UpstreamListener.Notify(new InboundPayload(payload));
                    }
                    else
                    {
                        ThrottledMessages.Enqueue(payload);
                        ScheduleDequeue(InboundThrottleMode.TimeToAvailable(MonotonicClock.GetNanos(), tokens));
                    }
                }
                else
                {
                    ThrottledMessages.Enqueue(payload);
                }
            }
        }
    }

    #endregion
}