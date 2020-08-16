using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Configuration;
using Akka.Serialization.Protocol;
using Akka.Remote.Transport;
using Akka.Util;
using DotNetty.Transport.Channels;

namespace Akka.Remote
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        array,
        assembly,
        buffer,
        destination,
        key,
        obj,
        s,
        str,
        source,
        type,
        types,
        value,
        values,
        valueFactory,
        name,
        item,
        options,
        list,
        ts,
        other,
        pool,
        inner,
        policy,
        offset,
        count,
        path,
        typeInfo,
        method,
        qualifiedTypeName,
        fullName,
        feature,
        manager,
        directories,
        dirEnumArgs,
        asm,
        includedAssemblies,
        func,
        defaultFn,
        returnType,
        propertyInfo,
        parameterTypes,
        fieldInfo,
        memberInfo,
        attributeType,
        pi,
        fi,
        invoker,
        instanceType,
        target,
        member,
        typeName,
        predicate,
        assemblyPredicate,
        collection,
        capacity,
        match,
        index,
        length,
        startIndex,
        args,
        typeId,
        acceptableHeartbeatPause,
        heartbeatInterval,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
        InvalidOperation_Watcher,
        InvalidOperation_Unknown_ActorSelPart,

        ArgumentOutOfRange_AcceptableHeartbeatPause,
        ArgumentOutOfRange_HeartbeatInterval,
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IPAddress_Num()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("IPAddress.m_Numbers not found");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IPAddress_OnlyIpV6()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("Only AddressFamily.InterNetworkV6 can be converted to IPv4");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IPAddress_OnlyIpV4()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("Only AddressFamily.InterNetworkV4 can be converted to IPv6");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_D(object obj)
        {
            var type = obj as Type;
            var typeQualifiedName = type != null ? type.TypeQualifiedName() : obj?.GetType().TypeQualifiedName();
            return new ArgumentException($"Cannot deserialize object of type [{typeQualifiedName}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Endpoint_Reg(EndpointManager.Pass pass, IActorRef endpoint)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Attempting to overwrite existing endpoint {pass.Endpoint} with {endpoint}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SeqNo<T>(T msg, SeqNo maxSeq) where T : IHasSequenceNumber
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Sequence number must be monotonic. Received {msg.Seq} which is smaller than {maxSeq}", nameof(msg));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TransportAdapter_IsNoReg(string name)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"There is no registered transport adapter provider with name {name}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Pdu_InvalidAddr(Address address)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Address {address} could not be serialized: host or port missing");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Transport_AddrPortIsNull(Address address)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"address port must not be null: {address}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Transport_Initiate(string adapter, Exception ex)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot initiate transport adapter {adapter}", ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Serializer_DaemonMsg(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Can't serialize a non-DaemonMsgCreate message using DaemonMsgCreateSerializer [{obj?.GetType()}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Highest_SEQ_so_far_but_cumulative_ACK_is(Ack ack, SeqNo maxSeq)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(nameof(ack), $"Highest SEQ so far was {maxSeq} but cumulative ACK is {ack.CumulativeAck}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Unexpected_local_address_in_RemoteActorRef(RemoteActorRef actorRef)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unexpected local address in RemoteActorRef [{actorRef}]");
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_MaxSampleSize(int maxSampleSize)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(maxSampleSize), $"maxSampleSize must be >= 1, got {maxSampleSize}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_IntervalSum(long intervalSum)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(intervalSum), $"intervalSum must be >= 0, got {intervalSum}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_SquaredIntervalSum(long squaredIntervalSum)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(squaredIntervalSum), $"squaredIntervalSum must be >= 0, got {squaredIntervalSum}");
            }
        }

        #endregion

        #region -- SerializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static SerializationException GetSerializationException_Serializer_MiscFrom(string manifest)
        {
            return new SerializationException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(Akka.Remote.Serialization.MiscMessageSerializer)}]");
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
        {
            throw GetException();

            static NotSupportedException GetException()
            {
                return new NotSupportedException();
            }
        }

        #endregion

        #region -- TypeLoadException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTypeLoadException(RemoteSettings.TransportSettings transportSettings)
        {
            throw GetException();
            TypeLoadException GetException()
            {
                return new TypeLoadException($"Cannot instantiate transport [{transportSettings.TransportClass}]. Cannot find the type.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTypeLoadException_Transport(RemoteSettings.TransportSettings transportSettings)
        {
            throw GetException();
            TypeLoadException GetException()
            {
                return new TypeLoadException(
                    $"Cannot instantiate transport [{transportSettings.TransportClass}]. It does not implement [{typeof(Transport.Transport).FullName}].");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTypeLoadException_ActorSystem(RemoteSettings.TransportSettings transportSettings)
        {
            throw GetException();
            TypeLoadException GetException()
            {
                return new TypeLoadException(
                    $"Cannot instantiate transport [{transportSettings.TransportClass}]. " +
                    $"It has no public constructor with [{typeof(ActorSystem).FullName}] and [{typeof(Config).FullName}] parameters");
            }
        }

        #endregion

        #region -- InvalidMessageException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidMessageException()
        {
            throw GetException();

            static InvalidMessageException GetException()
            {
                return new InvalidMessageException("Message is null.");
            }
        }

        #endregion

        #region -- Akka Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowResendUnfulfillableException()
        {
            throw GetException();

            static ResendUnfulfillableException GetException()
            {
                return new ResendUnfulfillableException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowResendBufferCapacityReachedException(int capacity)
        {
            throw GetException();
            ResendBufferCapacityReachedException GetException()
            {
                return new ResendBufferCapacityReachedException(capacity);
            }
        }

        #endregion

        #region -- ConfigurationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException()
        {
            throw GetException();

            static ConfigurationException GetException()
            {
                return new ConfigurationException(@"No transports enabled under ""akka.remote.enabled-transports""");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException(IChannel channel)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Unknown local address type {channel.LocalAddress}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_FailureDetector_Load(string fqcn)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Could not create custom FailureDetector {fqcn}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_RequesteRemoteDeployment(ActorPath path)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"configuration requested remote deployment for local-only Props at {path}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_RemoteDeployer_ParseConfig(string remote)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"unparseable remote node name [{remote}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_ProblemWhileCreating(ActorPath path, Props props, Exception ex)
        {
            throw GetConfigurationException();
            ConfigurationException GetConfigurationException()
            {
                return new ConfigurationException(
                    $"Configuration problem while creating {path} with dispatcher [{props.Dispatcher}] and mailbox [{props.Mailbox}]", ex);
            }
        }

        #endregion

        #region -- ChannelException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowChannelException()
        {
            throw GetException();

            static ChannelException GetException()
            {
                return new ChannelException("Transport is not open");
            }
        }

        #endregion

        #region -- PduCodecException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static PduCodecException GetPduCodecException_Decode()
        {
            return new PduCodecException("Error decoding Akka PDU: Neither message nor control message were contained");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowPduCodecException_Decode(Exception ex)
        {
            throw GetException();
            PduCodecException GetException()
            {
                return new PduCodecException("Decoding PDU failed", ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static PduCodecException GetPduCodecException_Decode(AkkaControlMessage controlPdu)
        {
            return new PduCodecException($"Decoding of control PDU failed, invalid format, unexpected {controlPdu}");
        }

        #endregion

        #region -- AkkaProtocolException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AkkaProtocolException GetAkkaProtocolException_InboundPayload(object fsmEvent)
        {
            return new AkkaProtocolException($"Unhandled message in state Open(InboundPayload) with type {fsmEvent.GetType()}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AkkaProtocolException GetAkkaProtocolException_DisassociateUnderlying(object fsmEvent)
        {
            return new AkkaProtocolException($"Unhandled message in state Open(DisassociateUnderlying) with type {fsmEvent.GetType()}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AkkaProtocolException GetAkkaProtocolException_DecodePdu(object pdu, Exception ex)
        {
            return new AkkaProtocolException($"Error while decoding incoming Akka PDU of type {pdu.GetType()}", ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AkkaProtocolException GetAkkaProtocolException_Associate(Exception ex)
        {
            return new AkkaProtocolException("Error writing ASSOCIATE to transport", ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AkkaProtocolException GetAkkaProtocolException_TransportDisassociatedBeforeHandshakeFinished(FSMBase.Reason reason)
        {
            return reason is FSMBase.Failure
                ? new AkkaProtocolException(reason.ToString())
                : new AkkaProtocolException("Transport disassociated before handshake finished");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowAkkaProtocolException_Disassociate(Exception ex)
        {
            throw GetException();
            AkkaProtocolException GetException()
            {
                return new AkkaProtocolException("Error writing DISASSOCIATE to transport", ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AkkaProtocolException GetAkkaProtocolException_HeartBeat(Exception ex)
        {
            return new AkkaProtocolException("Error writing HEARTBEAT to transport", ex);
        }

        #endregion

        #region -- EndpointException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEndpointException_SerializeMessage()
        {
            throw GetException();

            static EndpointException GetException()
            {
                return new EndpointException("Internal error: No handle was present during serialization of outbound message.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEndpointException_WriteSend()
        {
            throw GetException();

            static EndpointException GetException()
            {
                return new EndpointException("Internal error: Endpoint is in state Writing, but no association handle is present.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static EndpointException GetEndpointException_DecodeMessageAndAck(Exception ex)
        {
            return new EndpointException("Error while decoding incoming Akka PDU", ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static EndpointException GetEndpointException_FailedToWriteMessageToTheTransport(Exception ex)
        {
            return new EndpointException("Failed to write message to the transport", ex);
        }

        #endregion

        #region -- EndpointDisassociatedException -- 

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static EndpointDisassociatedException GetEndpointDisassociatedException_Disassociated()
        {
            return new EndpointDisassociatedException("Disassociated");
        }

        #endregion

        #region -- RemoteTransportException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException_LocalAddressForRemote(Address remote)
        {
            throw GetException();
            RemoteTransportException GetException()
            {
                return new RemoteTransportException(
                    "No transport is responsible for address:[" + remote + "] although protocol [" + remote.Protocol +
                    "] is available." +
                    " Make sure at least one transport is configured to be responsible for the address.",
                    null);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException_LocalAddressForRemote(Address remote, IDictionary<string, HashSet<ProtocolTransportAddressPair>> transportMapping)
        {
            throw GetException();
            RemoteTransportException GetException()
            {
                return new RemoteTransportException(
                    "No transport is loaded for protocol: [" + remote.Protocol + "], available protocols: [" +
                    string.Join(",", transportMapping.Keys.Select(t => t.ToString())) + "]", null);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException_LocalAddressForRemote(Address remote, ProtocolTransportAddressPair[] responsibleTransports)
        {
            throw GetException();
            RemoteTransportException GetException()
            {
                return new RemoteTransportException(
                    "Multiple transports are available for [" + remote + ": " +
                    string.Join(",", responsibleTransports.Select(t => t.ToString())) + "] " +
                    "Remoting cannot decide which transport to use to reach the remote system. Change your configuration " +
                    "so that only one transport is responsible for the address.",
                    null);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException_EndpointManager()
        {
            throw GetException();

            static RemoteTransportException GetException()
            {
                return new RemoteTransportException("Attempted to send remote message but Remoting is not running.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException_EndpointManager(Address address, int? uid)
        {
            throw GetException();
            RemoteTransportException GetException()
            {
                return new RemoteTransportException($"Attempted to quarantine address {address} with uid {uid} but Remoting is not running");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException_EndpointManager_Cmd()
        {
            throw GetException();

            static RemoteTransportException GetException()
            {
                return new RemoteTransportException("Attempted to send management command but Remoting is not running.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRemoteTransportException(Address address)
        {
            throw GetException();
            RemoteTransportException GetException()
            {
                return new RemoteTransportException($"There are more than one transports listening on local address {address}");
            }
        }

        #endregion

        #region -- ActorInitializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ActorInitializationException GetActorInitializationException_RemoteDeploymentFailedFor(ActorPath path, Exception ex)
        {
            return new ActorInitializationException($"Remote deployment failed for [{path}]", ex);
        }

        #endregion
    }
}
