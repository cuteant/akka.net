// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Event;
using Akka.Remote.Transport;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv.Native;

namespace Akka.Remote
{
    internal static class RemoteLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExpectedMessageOfTypeAssociate(this ILoggingAdapter logger, object fsmEvent)
        {
            logger.Debug("Expected message of type Associate; instead received {0}", fsmEvent.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingInbound(this ILoggingAdapter logger, object instance, Address remoteAddress, string debugMessage)
        {
            logger.Debug("Dropping inbound [{0}] for [{1}] {2}", instance.GetType(), remoteAddress, debugMessage);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingOutbound(this ILoggingAdapter logger, object instance, Address remoteAddress, string debugMessage)
        {
            logger.Debug("Dropping outbound [{0}] for [{1}] {2}", instance.GetType(), remoteAddress, debugMessage);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReWatch(this ILoggingAdapter logger, IInternalActorRef watcher, IInternalActorRef watchee)
        {
            logger.Debug("Re-watch [{0} -> {1}]", watcher.Path, watchee.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Watching(this ILoggingAdapter logger, IInternalActorRef watcher, IInternalActorRef watchee)
        {
            logger.Debug("Watching: [{0} -> {1}]", watcher.Path, watchee.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Unwatching(this ILoggingAdapter logger, IInternalActorRef watcher, IInternalActorRef watchee)
        {
            logger.Debug($"Unwatching: [{watcher.Path} -> {watchee.Path}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TriggerExtraExpectedHeartbeatFromAddr(this ILoggingAdapter logger, Address address)
        {
            logger.Debug("Trigger extra expected heartbeat from [{0}]", address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingHeartbeatToAddr(this ILoggingAdapter logger, Address a)
        {
            logger.Debug("Sending Heartbeat to [{0}]", a);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingFirstHeartbeatToAddr(this ILoggingAdapter logger, Address a)
        {
            logger.Debug("Sending first Heartbeat to [{0}]", a);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WatcheeTerminated(this ILoggingAdapter logger, IInternalActorRef watchee)
        {
            logger.Debug("Watchee terminated: [{0}]", watchee.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnwatchedLastWatcheeOfNode(this ILoggingAdapter logger, Address watcheeAddress)
        {
            logger.Debug("Unwatched last watchee of node: [{0}]", watcheeAddress);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CleanupSelfWatchOf(this ILoggingAdapter logger, IInternalActorRef watchee)
        {
            logger.Debug("Cleanup self watch of [{0}]", watchee.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedHeartbeatRspFrom(this ILoggingAdapter logger, Address from)
        {
            logger.Debug("Received heartbeat rsp from [{0}]", from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedFirstHeartbeatRspFrom(this ILoggingAdapter logger, Address from)
        {
            logger.Debug("Received first heartbeat rsp from [{0}]", from);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemotePathDoesNotMatchPathFromMessage(this ILoggingAdapter logger, DaemonMsgCreate message)
        {
            logger.Debug("remote path does not match path from message [{0}]", message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedCommandToRemoteSystemDaemonOn(this ILoggingAdapter logger, object message, ActorPath p)
        {
            logger.Debug("Received command [{0}] to RemoteSystemDaemon on [{1}]", message, p.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InstantiatingRemoteActor(this ILoggingAdapter logger, ActorPath rootPath, RemoteActorRef actor)
        {
            logger.Debug("[{0}] Instantiating Remote Actor [{1}]", rootPath, actor.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResolveOfUnknownPathFailed(this ILoggingAdapter logger, string path)
        {
            logger.Debug("resolve of unknown path [{0}] failed", path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingPruneTimerForEndpointManager(this ILoggingAdapter logger)
        {
            logger.Debug("Starting prune timer for endpoint manager...");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemoteSystemWithAddressHasShutDown(this ILoggingAdapter logger, ShutDownAssociation shutdown, RemoteSettings settings)
        {
            logger.Debug("Remote system with address [{0}] has shut down. Address is now gated for {1}ms, all messages to this address will be delivered to dead letters.",
                shutdown.RemoteAddress, settings.RetryGateClosedFor.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DrainedBufferWithMaxWriteCount(this ILoggingAdapter logger, int maxWriteCount,
            int fullBackoffCount, int smallBackoffCount, int noBackoffCount, long adaptiveBackoffNanos)
        {
            logger.Debug("Drained buffer with maxWriteCount: {0}, fullBackoffCount: {1}," +
                        "smallBackoffCount: {2}, noBackoffCount: {3}," +
                        "adaptiveBackoff: {4}", maxWriteCount, fullBackoffCount, smallBackoffCount, noBackoffCount, adaptiveBackoffNanos / 1000);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendRemoteMessage(this ILoggingAdapter logger, EndpointManager.Send send, ActorSystem system)
        {
            logger.Debug("RemoteMessage: {0} to [{1}]<+[{2}] from [{3}]", send.Message,
                send.Recipient, send.Recipient.Path, send.SenderOption ?? system.DeadLetters);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedRemoteMessage(this ILoggingAdapter logger, object payload, IInternalActorRef recipient,
            ActorPath originalReceiver, IActorRef sender)
        {
            var msgLog = string.Format("RemoteMessage: {0} to {1}<+{2} from {3}", payload, recipient, originalReceiver, sender);
            logger.Debug("received remote-destined message {0}", msgLog);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OperatingInUntrustedMode(this ILoggingAdapter logger, object payload)
        {
            logger.Debug("operating in UntrustedMode, dropping inbound IPossiblyHarmful message of type {0}", payload.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OperatingInUntrustedMode1(this ILoggingAdapter logger, ActorSelectionMessage sel)
        {
            logger.Debug("operating in UntrustedMode, dropping inbound actor selection to [{0}], allow it" +
                         "by adding the path to 'akka.remote.trusted-selection-paths' in configuration",
                         DefaultMessageDispatcher.FormatActorPath(sel));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedLocalMessage(this ILoggingAdapter logger, object payload, IInternalActorRef recipient, ActorPath originalReceiver, IActorRef sender)
        {
            var msgLog = $"RemoteMessage: {payload} to {recipient}<+{originalReceiver} from {sender}";
            logger.Debug("received local message [{0}]", msgLog);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedDaemonMessage(this ILoggingAdapter logger, object payload, IInternalActorRef recipient, ActorPath originalReceiver, IActorRef sender)
        {
            var msgLog = $"RemoteMessage: {payload} to {recipient}<+{originalReceiver} from {sender}";
            logger.Debug("received daemon message [{0}]", msgLog);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingDaemonMessageInUntrustedMode(this ILoggingAdapter logger)
        {
            logger.Debug("dropping daemon message in untrusted mode");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AssociationBetweenLocalAndRemoteWasDisassociatedBecause(this ILoggingAdapter logger, AssociationHandle associationHandle, string reason)
        {
            logger.Debug("Association between local [{0}] and remote [{1}] was disassociated because {2}", associationHandle.LocalAddress, associationHandle.RemoteAddress, reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingDisassociateToBecauseFailureDetectorTriggeredInState(this ILoggingAdapter logger, AssociationHandle wrappedHandle, AssociationState stateName)
        {
            logger.Debug("Sending disassociate to [{0}] because failure detector triggered in state [{1}]", wrappedHandle, stateName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingDisassociateToBecauseUnexpectedMessageOfTypeWasReceivedUnassociated(this ILoggingAdapter logger, AssociationHandle wrappedHandle, object fsmEvent)
        {
            logger.Debug("Sending disassociate to [{0}] because unexpected message of type [{1}] was received unassociated.", wrappedHandle, fsmEvent.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingDisassociateToBecauseHandshakeTimedOutForOutboundAssociationAfter(this ILoggingAdapter logger, OutboundUnderlyingAssociated oua, AkkaProtocolSettings settings)
        {
            logger.Debug("Sending disassociate to [{0}] because handshake timed out for outbound association after [{1}] ms.", oua.WrappedHandle, settings.HandshakeTimeout.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SendingDisassociateToBecauseHandshakeTimedOutForInboundAssociationAfter(this ILoggingAdapter logger, InboundUnassociated iu, AkkaProtocolSettings settings)
        {
            logger.Debug("Sending disassociate to [{0}] because handshake timed out for inbound association after [{1}] ms.", iu.WrappedHandle, settings.HandshakeTimeout.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingRemoting(this ILoggingAdapter logger)
        {
            logger.Info("Starting remoting");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemotingShutDown(this ILoggingAdapter logger)
        {
            logger.Info("Remoting shut down.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShuttingDownRemoteDaemon(this ILoggingAdapter logger)
        {
            logger.Info("Shutting down remote daemon.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemoteDaemonShutDown(this ILoggingAdapter logger)
        {
            logger.Info("Remote daemon shut down; proceeding with flushing remote transports.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemotingStartedListeningOnAddresses(this ILoggingAdapter logger, HashSet<Address> addresses)
        {
            logger.Info("Remoting started; listening on addresses : [{0}]", string.Join(",", addresses.Select(x => x.ToString())));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PayloadSizeForIsBytes(this ILoggingAdapter logger, Type type, long payloadBytes)
        {
            logger.Info("Payload size for [{0}] is [{1}] bytes", type.FullName, payloadBytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NewMaximumPayloadSizeForIsBytes(this ILoggingAdapter logger, Type type, long payloadBytes)
        {
            logger.Info("New maximum payload size for [{0}] is [{1}] bytes", type.FullName, payloadBytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void QuarantinedAddressIsStillUnreachable(this ILoggingAdapter logger, Address remoteAddress)
        {
            logger.Info($"Quarantined address [{remoteAddress}] is still unreachable or has not been restarted. Keeping it quarantined.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DotNettyExceptionCaught(this ILoggingAdapter logger, OperationException exc, IChannelHandlerContext context)
        {
            var channel = context.Channel;
            logger.Info($"{exc.Description}. Channel [{channel.LocalAddress}->{channel.RemoteAddress}](Id={channel.Id})");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DotNettyExceptionCaught(this ILoggingAdapter logger, SocketException se, IChannelHandlerContext context)
        {
            var channel = context.Channel;
            logger.Info($"{se.Message} Channel [{channel.LocalAddress}->{channel.RemoteAddress}](Id={channel.Id})");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShutdownFinishedButFlushingMightNotHaveBeenSuccessful(this ILoggingAdapter logger)
        {
            logger.Warning("Shutdown finished, but flushing might not have been successful and some messages might have been dropped. " +
                           "Increase akka.remote.flush-wait-on-shutdown to a larger value to avoid this.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemotingIsNotRunningIgnoringShutdownAttempt(this ILoggingAdapter logger)
        {
            logger.Warning("Remoting is not running. Ignoring shutdown attempt");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemotingWasAlreadyStartedIgnoringStartAttempt(this ILoggingAdapter logger)
        {
            logger.Warning("Remoting was already started. Ignoring start attempt.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DetectedUnreachable(this ILoggingAdapter logger, Address a)
        {
            logger.Warning("Detected unreachable: [{0}]", a);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileResolvingAddress(this ILoggingAdapter logger, ActorPath actorPath, Exception ex)
        {
            logger.Warning("Error while resolving address [{0}] due to [{1}]", actorPath.Address, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AssociationToWithUnknownUIDIsReportedAsQuarantined(this ILoggingAdapter logger, Address remoteAddr, RemoteSettings settings)
        {
            logger.Warning("Association to [{0}] with unknown UID is reported as quarantined, but " +
                        "address cannot be quarantined without knowing the UID, gating instead for {1} ms.",
                        remoteAddr, settings.RetryGateClosedFor.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AssociationToWithUnknownUIDIsIrrecoverablyFailed(this ILoggingAdapter logger, HopelessAssociation hopeless, RemoteSettings settings)
        {
            logger.Warning("Association to [{0}] with unknown UID is irrecoverably failed. Address cannot be quarantined without knowing the UID, gating instead for {1} ms.",
                hopeless.RemoteAddress, settings.RetryGateClosedFor.TotalMilliseconds);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TriedToAssociateWithUnreachableRemoteAddress(this ILoggingAdapter logger, InvalidAssociation ia, RemoteSettings settings)
        {
            var causedBy = ia.InnerException == null
                ? ""
                : $"Caused by: [{ia.InnerException}]";
            logger.Warning("Tried to associate with unreachable remote address [{0}]. Address is now gated for {1} ms, all messages to this address will be delivered to dead letters. Reason: [{2}] {3}",
                ia.RemoteAddress, settings.RetryGateClosedFor.TotalMilliseconds, ia.Message, causedBy);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BufferedMessagesInEndpointWriterFor(this ILoggingAdapter logger, int size, Address a)
        {
            logger.Warning("[{0}] buffered messages in EndpointWriter for [{1}]. You should probably implement flow control to avoid flooding the remote connection.", size, a);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AssociationWithRemoteSystemHasFailed(this ILoggingAdapter logger, Address remoteAddr, RemoteSettings settings, Exception ex)
        {
            logger.Warning("Association with remote system {0} has failed; address is now gated for {1} ms. Reason is: [{2}]", remoteAddr, settings.RetryGateClosedFor.TotalMilliseconds, ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailureInjectorTransportIsActiveOnThisSystem(this ILoggingAdapter logger)
        {
            logger.Warning("FailureInjectorTransport is active on this system. Gremlins might munch your packets.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToRemoveChannelFromConnectionGroup(this ILoggingAdapter logger, IChannel channel)
        {
            logger.Warning("Unable to REMOVE channel [{0}->{1}](Id={2}) from connection group. May not shut down cleanly.",
                channel.LocalAddress, channel.RemoteAddress, channel.Id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToAddChannelToConnectionGroup(this ILoggingAdapter logger, IChannel channel)
        {
            logger.Warning("Unable to ADD channel [{0}->{1}](Id={2}) to connection group. May not shut down cleanly.",
                channel.LocalAddress, channel.RemoteAddress, channel.Id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SkippingToRemoteSystemDaemonOnWhileTerminating(this ILoggingAdapter logger, DaemonMsgCreate message, ActorPath p)
        {
            logger.Error("Skipping [{0}] to RemoteSystemDaemon on [{1}] while terminating", message, p.Address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemotingSystemHasBeenTerminatedAbruptly(this ILoggingAdapter logger)
        {
            logger.Error("Remoting system has been terminated abruptly. Attempting to shut down transports");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogErrorX(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TransientAssociationErrorAassociationRemainsLive(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, "Transient association error (association remains live)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToPublishErrorEventToEventStream(this ILoggingAdapter logger, Exception ex)
        {
            logger.Error(ex, "Unable to publish error event to EventStream");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AssociationToWithUidIsIrrecoverablyFailed(this ILoggingAdapter logger, HopelessAssociation hopeless)
        {
            logger.Error(hopeless.InnerException ?? hopeless, "Association to [{0}] with UID [{1}] is irrecoverably failed. Quarantining address.",
                hopeless.RemoteAddress, hopeless.Uid);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingMsgForNonLocalRecipientArrivingAtInboundAddr(this ILoggingAdapter logger, Type payloadClass,
            IInternalActorRef recipient, Address recipientAddress, IRemoteActorRefProvider provider)
        {
            logger.Error(
                "Dropping message [{0}] for non-local recipient [{1}] arriving at [{2}] inbound addresses [{3}]",
                payloadClass, recipient, recipientAddress, string.Join(",", provider.Transport.Addresses));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToBindToEndPoint(this ILoggingAdapter logger, Exception ex, EndPoint listenAddress)
        {
            logger.Error(ex, "Failed to bind to {0}; shutting down DotNetty transport.", listenAddress);
        }
    }
}
