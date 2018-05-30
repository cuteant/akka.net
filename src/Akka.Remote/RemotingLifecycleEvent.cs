//-----------------------------------------------------------------------
// <copyright file="RemotingLifecycleEvent.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Remote
{
    /// <summary>Remote lifecycle events that are published to the <see cref="EventStream"/> when
    /// initialization / connect / disconnect events that occur during network operations</summary>
    public abstract class RemotingLifecycleEvent
    {
        /// <summary>Logs the level.</summary>
        /// <returns>LogLevel.</returns>
        public abstract LogLevel LogLevel();
    }

    /// <summary>TBD</summary>
    public abstract class AssociationEvent : RemotingLifecycleEvent
    {
        /// <summary>TBD</summary>
        [Key(5)]
        public abstract Address LocalAddress { get; protected set; }

        /// <summary>TBD</summary>
        [Key(6)]
        public abstract Address RemoteAddress { get; protected set; }

        /// <summary>TBD</summary>
        [Key(7)]
        public abstract bool IsInbound { get; protected set; }

        [IgnoreMember, IgnoreDataMember]
        protected string EventName;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            var networkDirection = IsInbound ? "<-" : "->";
            return string.Format("{0} [{1}] {2} {3}", EventName, LocalAddress, networkDirection, RemoteAddress);
        }
    }

    /// <summary>TBD</summary>
    [MessagePackObject]
    public sealed class AssociatedEvent : AssociationEvent
    {
        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override LogLevel LogLevel() => Event.LogLevel.DebugLevel;

        /// <summary>TBD</summary>
        [Key(0)]
        public override Address LocalAddress { get; protected set; }

        /// <summary>TBD</summary>
        [Key(1)]
        public override Address RemoteAddress { get; protected set; }

        /// <summary>TBD</summary>
        [Key(2)]
        public override bool IsInbound { get; protected set; }

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="inbound">TBD</param>
        [SerializationConstructor]
        public AssociatedEvent(Address localAddress, Address remoteAddress, bool inbound)
        {
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            IsInbound = inbound;
            EventName = "Associated";
        }
    }

    /// <summary>Event that is fired when a remote association to another <see cref="ActorSystem"/> is terminated.</summary>
    [MessagePackObject]
    public sealed class DisassociatedEvent : AssociationEvent
    {
        /// <inheritdoc/>
        public override LogLevel LogLevel() => Event.LogLevel.DebugLevel;

        /// <inheritdoc/>
        [Key(0)]
        public override Address LocalAddress { get; protected set; }

        /// <inheritdoc/>
        [Key(1)]
        public override Address RemoteAddress { get; protected set; }

        /// <inheritdoc/>
        [Key(2)]
        public override bool IsInbound { get; protected set; }

        /// <summary>Creates a new <see cref="DisassociatedEvent"/> instance.</summary>
        /// <param name="localAddress">The address of the current actor system.</param>
        /// <param name="remoteAddress">The address of the remote actor system.</param>
        /// <param name="inbound">
        /// <c>true</c> if this side of the connection as inbound, <c>false</c> if it was outbound.
        /// </param>
        [SerializationConstructor]
        public DisassociatedEvent(Address localAddress, Address remoteAddress, bool inbound)
        {
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            IsInbound = inbound;
            EventName = "Disassociated";
        }
    }

    /// <summary>TBD</summary>
    [MessagePackObject]
    public sealed class AssociationErrorEvent : AssociationEvent
    {
        /// <summary>TBD</summary>
        /// <param name="cause">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="inbound">TBD</param>
        /// <param name="level">TBD</param>
        [SerializationConstructor]
        public AssociationErrorEvent(Exception cause, Address localAddress, Address remoteAddress, bool inbound, LogLevel level)
        {
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            IsInbound = inbound;
            EventName = "AssociationError";
            _level = level;
            Cause = cause;
        }

        /// <summary>TBD</summary>
        [Key(0)]
        public Exception Cause { get; }

        /// <summary>TBD</summary>
        [Key(1)]
        public override Address LocalAddress { get; protected set; }

        /// <summary>TBD</summary>
        [Key(2)]
        public override Address RemoteAddress { get; protected set; }

        /// <summary>TBD</summary>
        [Key(3)]
        public override bool IsInbound { get; protected set; }

        [Key(4)]
        private readonly LogLevel _level;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override LogLevel LogLevel() => _level;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override string ToString() => $"{base.ToString()}: Error [{Cause.Message}] [{Cause.StackTrace}]";
    }

    /// <summary>TBD</summary>
    [MessagePackObject]
    public sealed class RemotingListenEvent : RemotingLifecycleEvent
    {
        /// <summary>TBD</summary>
        /// <param name="listenAddresses">TBD</param>
        [SerializationConstructor]
        public RemotingListenEvent(IList<Address> listenAddresses) => ListenAddresses = listenAddresses;

        /// <summary>TBD</summary>
        [Key(0)]
        public IList<Address> ListenAddresses { get; }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override LogLevel LogLevel() => Event.LogLevel.InfoLevel;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override string ToString() => $"Remoting now listens on addresses: [{ListenAddresses.Select(x => x.ToString()).Join(",")}]";
    }

    /// <summary>Event that is published when the remoting system terminates.</summary>
    [MessagePackObject]
    public sealed class RemotingShutdownEvent : RemotingLifecycleEvent
    {
        /// <inheritdoc/>
        public override LogLevel LogLevel() => Event.LogLevel.InfoLevel;

        /// <inheritdoc/>
        public override string ToString() => "Remoting shut down";
    }

    /// <summary>TBD</summary>
    [MessagePackObject]
    public sealed class RemotingErrorEvent : RemotingLifecycleEvent
    {
        /// <summary>TBD</summary>
        /// <param name="cause">TBD</param>
        [SerializationConstructor]
        public RemotingErrorEvent(Exception cause) => Cause = cause;

        /// <summary>TBD</summary>
        [Key(0)]
        public Exception Cause { get; }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override LogLevel LogLevel() => Event.LogLevel.ErrorLevel;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override string ToString() => $"Remoting error: [{Cause.Message}] [{Cause.StackTrace}]";
    }

    /// <summary>TBD</summary>
    [MessagePackObject]
    public sealed class QuarantinedEvent : RemotingLifecycleEvent
    {
        /// <summary>TBD</summary>
        /// <param name="address">TBD</param>
        /// <param name="uid">TBD</param>
        [SerializationConstructor]
        public QuarantinedEvent(Address address, int uid)
        {
            Uid = uid;
            Address = address;
        }

        /// <summary>TBD</summary>
        [Key(0)]
        public Address Address { get; }

        /// <summary>TBD</summary>
        [Key(1)]
        public int Uid { get; }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override LogLevel LogLevel() => Event.LogLevel.WarningLevel;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return
                string.Format(
                    "Association to [{0}] having UID [{1}] is irrecoverably failed. UID is now quarantined and all " +
                    "messages to this UID will be delivered to dead letters. Remote actorsystem must be restarted to recover " +
                    "from this situation.", Address, Uid);
        }
    }

    /// <summary>TBD</summary>
    [MessagePackObject]
    public sealed class ThisActorSystemQuarantinedEvent : RemotingLifecycleEvent
    {
        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="remoteAddress">TBD</param>
        [SerializationConstructor]
        public ThisActorSystemQuarantinedEvent(Address localAddress, Address remoteAddress)
        {
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
        }

        /// <summary>TBD</summary>
        [Key(0)]
        public Address LocalAddress { get; }

        /// <summary>TBD</summary>
        [Key(1)]
        public Address RemoteAddress { get; }

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override LogLevel LogLevel() => Event.LogLevel.WarningLevel;

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public override string ToString() => $"The remote system {RemoteAddress} has quarantined this system {LocalAddress}.";
    }

    /// <summary>
    /// INTERNAL API.
    ///
    /// Used for publishing remote lifecycle events to the <see cref="EventStream"/> of the provided
    /// <see cref="ActorSystem"/>.
    /// </summary>
    internal class EventPublisher
    {
        /// <summary>TBD</summary>
        public ActorSystem System { get; }

        /// <summary>TBD</summary>
        public ILoggingAdapter Log { get; }

        /// <summary>TBD</summary>
        public readonly LogLevel LogLevel;

        /// <summary>TBD</summary>
        /// <param name="system">TBD</param>
        /// <param name="log">TBD</param>
        /// <param name="logLevel">TBD</param>
        public EventPublisher(ActorSystem system, ILoggingAdapter log, LogLevel logLevel)
        {
            System = system;
            Log = log;
            LogLevel = logLevel;
        }

        /// <summary>TBD</summary>
        /// <param name="message">TBD</param>
        public void NotifyListeners(RemotingLifecycleEvent message)
        {
            System.EventStream.Publish(message);
            if (message.LogLevel() >= LogLevel) Log.Log(message.LogLevel(), message.ToString());
        }
    }
}