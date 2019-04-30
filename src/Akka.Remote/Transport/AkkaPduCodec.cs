//-----------------------------------------------------------------------
// <copyright file="AkkaPduCodec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Remote.Serialization;
using Akka.Serialization.Protocol;
using SerializedMessage = Akka.Serialization.Protocol.Payload;

namespace Akka.Remote.Transport
{
    #region == class PduCodecException ==

    /// <summary>INTERNAL API</summary>
    internal sealed class PduCodecException : AkkaException
    {
        /// <summary>Initializes a new instance of the <see cref="PduCodecException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public PduCodecException(string message, Exception cause = null) : base(message, cause) { }

#if SERIALIZATION
        /// <summary>Initializes a new instance of the <see cref="PduCodecException"/> class.</summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object data about the
        /// exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information about the source
        /// or destination.
        /// </param>
        private PduCodecException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    #endregion


    #region == interface IAkkaPdu ==

    /// <summary>Interface used to represent Akka PDUs (Protocol Data Unit)</summary>
    internal interface IAkkaPdu { }

    #endregion

    #region == struct Associate ==

    /// <summary>TBD</summary>
    internal readonly struct Associate : IAkkaPdu
    {
        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        public Associate(HandshakeInfo info) => Info = info;

        /// <summary>TBD</summary>
        public readonly HandshakeInfo Info;
    }

    #endregion

    #region == struct Disassociate ==

    /// <summary>TBD</summary>
    internal readonly struct Disassociate : IAkkaPdu
    {
        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        public Disassociate(DisassociateInfo reason) => Reason = reason;

        /// <summary>TBD</summary>
        public readonly DisassociateInfo Reason;
    }

    #endregion

    #region == struct Heartbeat ==

    /// <summary>INTERNAL API.
    ///
    /// Represents a heartbeat on the wire.
    /// </summary>
    internal readonly struct Heartbeat : IAkkaPdu { }

    #endregion

    #region == struct Payload ==

    /// <summary>TBD</summary>
    internal readonly struct Payload : IAkkaPdu
    {
        /// <summary>TBD</summary>
        /// <param name="bytes">TBD</param>
        public Payload(object bytes) => Bytes = bytes;

        /// <summary>TBD</summary>
        public readonly object Bytes;
    }

    #endregion

    #region == class Message ==

    /// <summary>TBD</summary>
    internal sealed class Message : IAkkaPdu, IHasSequenceNumber
    {
        /// <summary>TBD</summary>
        /// <param name="recipient">TBD</param>
        /// <param name="recipientAddress">TBD</param>
        /// <param name="serializedMessage">TBD</param>
        /// <param name="senderOptional">TBD</param>
        /// <param name="seq">TBD</param>
        public Message(IInternalActorRef recipient, Address recipientAddress, SerializedMessage serializedMessage, IActorRef senderOptional = null, SeqNo seq = null)
        {
            Seq = seq;
            SenderOptional = senderOptional;
            SerializedMessage = serializedMessage;
            RecipientAddress = recipientAddress;
            Recipient = recipient;
        }

        /// <summary>TBD</summary>
        public readonly IInternalActorRef Recipient;

        /// <summary>TBD</summary>
        public readonly Address RecipientAddress;

        /// <summary>TBD</summary>
        public readonly SerializedMessage SerializedMessage;

        /// <summary>TBD</summary>
        public readonly IActorRef SenderOptional;

        /// <summary>TBD</summary>
        public bool ReliableDeliveryEnabled => Seq != null;

        /// <summary>TBD</summary>
        public SeqNo Seq { get; }
    }

    #endregion

    #region == struct AckAndMessage ==

    /// <summary>INTERNAL API</summary>
    internal readonly struct AckAndMessage
    {
        /// <summary>TBD</summary>
        /// <param name="ackOption">TBD</param>
        /// <param name="messageOption">TBD</param>
        public AckAndMessage(Ack ackOption, Message messageOption)
        {
            MessageOption = messageOption;
            AckOption = ackOption;
        }

        /// <summary>TBD</summary>
        public readonly Ack AckOption;

        /// <summary>TBD</summary>
        public readonly Message MessageOption;
    }

    #endregion


    #region == class AkkaPduCodec ==

    /// <summary>INTERNAL API
    ///
    /// A codec that is able to convert Akka PDUs from and to <see cref="T:System.Byte{T}"/>
    /// </summary>
    internal abstract class AkkaPduCodec
    {
        protected readonly ActorSystem System;
        protected readonly AddressThreadLocalCache AddressCache;

        protected AkkaPduCodec(ActorSystem system)
        {
            System = system;
            AddressCache = AddressThreadLocalCache.For(system);
        }

        /// <summary>Return an <see cref="IAkkaPdu"/> instance that represents a PDU contained in the raw <see cref="T:System.Byte{T}"/>.</summary>
        /// <param name="pdu">Encoded raw byte representation of an Akka PDU</param>
        /// <returns>Class representation of a PDU that can be used in a <see cref="PatternMatch"/>.</returns>
        public abstract IAkkaPdu DecodePdu(AkkaProtocolMessage pdu);

        /// <summary>Takes an <see cref="IAkkaPdu"/> representation of an Akka PDU and returns its encoded
        /// form as a <see cref="T:System.Byte{T}"/>.</summary>
        /// <param name="pdu">TBD</param>
        /// <returns>TBD</returns>
        public virtual object EncodePdu(IAkkaPdu pdu)
        {
            switch (pdu)
            {
                case Payload p:
                    return ConstructPayload(p.Bytes);

                case Heartbeat _:
                    return ConstructHeartbeat();

                case Associate a:
                    return ConstructAssociate(a.Info);

                case Disassociate d:
                    return ConstructDisassociate(d.Reason);

                default:
                    return default; // unsupported message type
            }
        }

        /// <summary>TBD</summary>
        /// <param name="payload">TBD</param>
        /// <returns>TBD</returns>
        public abstract object ConstructPayload(object payload);

        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        /// <returns>TBD</returns>
        public abstract object ConstructAssociate(HandshakeInfo info);

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        /// <returns>TBD</returns>
        public abstract object ConstructDisassociate(DisassociateInfo reason);

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public abstract object ConstructHeartbeat();

        /// <summary>TBD</summary>
        /// <param name="ackAndEnvelope">TBD</param>
        /// <param name="provider">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <returns>TBD</returns>
        public abstract AckAndMessage DecodeMessage(AckAndEnvelopeContainer ackAndEnvelope, IRemoteActorRefProvider provider, Address localAddress);

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="recipient">TBD</param>
        /// <param name="serializedMessage">TBD</param>
        /// <param name="senderOption">TBD</param>
        /// <param name="seqOption">TBD</param>
        /// <param name="ackOption">TBD</param>
        /// <returns>TBD</returns>
        public abstract object ConstructMessage(Address localAddress, IActorRef recipient,
            in SerializedMessage serializedMessage, IActorRef senderOption = null, SeqNo seqOption = null, Ack ackOption = null);

        /// <summary>TBD</summary>
        /// <param name="ack">TBD</param>
        /// <returns>TBD</returns>
        public abstract object ConstructPureAck(Ack ack);
    }

    #endregion

    #region == class AkkaPduMessagePackCodec ==

    /// <summary>TBD</summary>
    internal sealed class AkkaPduMessagePackCodec : AkkaPduCodec
    {
        /// <summary>TBD</summary>
        /// <param name="pdu">TBD</param>
        /// <exception cref="PduCodecException">
        /// This exception is thrown when the Akka PDU in the specified byte string, <paramref
        /// name="pdu"/>, meets one of the following conditions: <ul><li>The PDU is neither a message
        /// or a control message.</li><li>The PDU is a control message with an invalid format.</li></ul>
        /// </exception>
        /// <returns>TBD</returns>
        public override IAkkaPdu DecodePdu(AkkaProtocolMessage pdu)
        {
            //try
            //{
            if (pdu.Instruction != null) { return DecodeControlPdu(pdu.Instruction); }

            if (pdu.Payload != null) { return new Payload(pdu.Payload); }

            throw ThrowHelper.GetPduCodecException_Decode();
            //}
            //catch (FormatterNotRegisteredException ex) // InvalidProtocolBufferException
            //{
            //    return ThrowHelper.ThrowPduCodecException_Decode(ex);
            //}
        }

        /// <summary>TBD</summary>
        /// <param name="payload">TBD</param>
        /// <returns>TBD</returns>
        public override object ConstructPayload(object payload) => new AkkaProtocolMessage(payload, null);

        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="info"/> contains an invalid address.
        /// </exception>
        /// <returns>TBD</returns>
        public override object ConstructAssociate(HandshakeInfo info)
        {
            var handshakeInfo = new AkkaHandshakeInfo(SerializeAddress(info.Origin), (ulong)info.Uid);

            return ConstructControlMessagePdu(CommandType.Associate, handshakeInfo);
        }

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        /// <returns>TBD</returns>
        public override object ConstructDisassociate(DisassociateInfo reason)
        {
            switch (reason)
            {
                case DisassociateInfo.Quarantined:
                    return DISASSOCIATE_QUARANTINED;

                case DisassociateInfo.Shutdown:
                    return DISASSOCIATE_SHUTTING_DOWN;

                case DisassociateInfo.Unknown:
                default:
                    return DISASSOCIATE;
            }
        }

        /*
         * Since there's never any ActorSystem-specific information coded directly
         * into the heartbeat messages themselves (i.e. no handshake info,) there's no harm in caching in the
         * same heartbeat byte buffer and re-using it.
         */
        private static readonly AkkaProtocolMessage HeartbeatPdu = ConstructControlMessagePdu(CommandType.Heartbeat);

        /// <summary>Creates a new Heartbeat message instance.</summary>
        /// <returns>The Heartbeat message.</returns>
        public override object ConstructHeartbeat() => HeartbeatPdu;

        /// <summary>Indicated RemoteEnvelope.Seq is not defined (order is irrelevant)</summary>
        private const ulong SeqUndefined = ulong.MaxValue;

        /// <summary>TBD</summary>
        /// <param name="ackAndEnvelope">TBD</param>
        /// <param name="provider">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <returns>TBD</returns>
        public override AckAndMessage DecodeMessage(AckAndEnvelopeContainer ackAndEnvelope, IRemoteActorRefProvider provider, Address localAddress)
        {
            Ack ackOption = null;
            var aeAck = ackAndEnvelope.Ack;
            if (aeAck != null)
            {
                ackOption = new Ack(new SeqNo((long)aeAck.CumulativeAck), aeAck.Nacks.Select(x => new SeqNo((long)x)));
            }

            Message messageOption = null;
            //if (ackAndEnvelope.Envelope != null)
            //{
            var envelopeContainer = ackAndEnvelope.Envelope;
            if (envelopeContainer != null)
            {
                var recipientPath = envelopeContainer.Recipient.Path;
                var recipient = provider.ResolveActorRefWithLocalAddress(recipientPath, localAddress);
                Address recipientAddress;
                if (AddressCache != null)
                {
                    recipientAddress = AddressCache.Cache.GetOrCompute(recipientPath);
                }
                else
                {
                    ActorPath.TryParseAddress(recipientPath, out recipientAddress);
                }

                var serializedMessage = envelopeContainer.Message;
                IActorRef senderOption = null;
                var ecSender = envelopeContainer.Sender;
                if (ecSender != null)
                {
                    senderOption = provider.ResolveActorRefWithLocalAddress(ecSender.Path, localAddress);
                }
                SeqNo seqOption = null;
                var ecSeq = envelopeContainer.Seq;
                if (ecSeq != SeqUndefined)
                {
                    unchecked
                    {
                        seqOption = new SeqNo((long)ecSeq); //proto takes a ulong
                    }
                }
                messageOption = new Message(recipient, recipientAddress, serializedMessage, senderOption, seqOption);
            }
            //}

            return new AckAndMessage(ackOption, messageOption);
        }

        private AcknowledgementInfo AckBuilder(Ack ack)
        {
            return new AcknowledgementInfo((ulong)ack.CumulativeAck.RawValue, ack.Nacks.Select(_ => (ulong)_.RawValue).ToArray());
        }

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="recipient">TBD</param>
        /// <param name="serializedMessage">TBD</param>
        /// <param name="senderOption">TBD</param>
        /// <param name="seqOption">TBD</param>
        /// <param name="ackOption">TBD</param>
        /// <returns>TBD</returns>
        public override object ConstructMessage(Address localAddress, IActorRef recipient, in SerializedMessage serializedMessage,
            IActorRef senderOption = null, SeqNo seqOption = null, Ack ackOption = null)
        {
            var ackAndEnvelope = new AckAndEnvelopeContainer();
            var envelope = new RemoteEnvelope() { Recipient = SerializeActorRef(recipient.Path.Address, recipient) };
            if (senderOption != null && senderOption.Path != null) { envelope.Sender = ConvertActorRef(localAddress, senderOption); }
            if (seqOption != null) { envelope.Seq = (ulong)seqOption.RawValue; } else { envelope.Seq = SeqUndefined; }
            if (ackOption != null) { ackAndEnvelope.Ack = AckBuilder(ackOption); }
            envelope.Message = serializedMessage;
            ackAndEnvelope.Envelope = envelope;

            return ackAndEnvelope;
        }

        /// <summary>TBD</summary>
        /// <param name="ack">TBD</param>
        /// <returns>TBD</returns>
        public override object ConstructPureAck(Ack ack)
            => new AckAndEnvelopeContainer() { Ack = AckBuilder(ack) };

        #region * Internal methods *

        private static IAkkaPdu DecodeControlPdu(AkkaControlMessage controlPdu)
        {
            switch (controlPdu.CommandType)
            {
                case CommandType.Associate:
                    var handshakeInfo = controlPdu.HandshakeInfo;
                    if (handshakeInfo != null) // HasHandshakeInfo
                    {
                        return new Associate(new HandshakeInfo(DecodeAddress(handshakeInfo.Origin), (int)handshakeInfo.Uid));
                    }
                    break;

                case CommandType.Disassociate:
                    return new Disassociate(DisassociateInfo.Unknown);

                case CommandType.DisassociateQuarantined:
                    return new Disassociate(DisassociateInfo.Quarantined);

                case CommandType.DisassociateShuttingDown:
                    return new Disassociate(DisassociateInfo.Shutdown);

                case CommandType.Heartbeat:
                    return new Heartbeat();
            }

            throw ThrowHelper.GetPduCodecException_Decode(controlPdu);
        }

        private static readonly AkkaProtocolMessage DISASSOCIATE = ConstructControlMessagePdu(CommandType.Disassociate);

        private static readonly AkkaProtocolMessage DISASSOCIATE_SHUTTING_DOWN = ConstructControlMessagePdu(CommandType.DisassociateShuttingDown);

        private static readonly AkkaProtocolMessage DISASSOCIATE_QUARANTINED = ConstructControlMessagePdu(CommandType.DisassociateQuarantined);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AkkaProtocolMessage ConstructControlMessagePdu(CommandType code, AkkaHandshakeInfo handshakeInfo = null)
        {
            var controlMessage = new AkkaControlMessage(code, handshakeInfo);

            return new AkkaProtocolMessage(null, controlMessage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Address DecodeAddress(in AddressData origin)
            => new Address(origin.Protocol, origin.System, origin.Hostname, (int)origin.Port);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlyActorRefData SerializeActorRef(Address defaultAddress, IActorRef actorRef)
        {
            var path = actorRef.Path;
            return new ReadOnlyActorRefData(
                (!string.IsNullOrEmpty(path.Address.Host))
                    ? path.ToSerializationFormat()
                    : path.ToSerializationFormatWithAddress(defaultAddress));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ActorRefData ConvertActorRef(Address defaultAddress, IActorRef actorRef)
        {
            var path = actorRef.Path;
            return new ActorRefData(
                (!string.IsNullOrEmpty(path.Address.Host))
                    ? path.ToSerializationFormat()
                    : path.ToSerializationFormatWithAddress(defaultAddress));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AddressData SerializeAddress(Address address)
        {
            if (string.IsNullOrEmpty(address.Host) || !address.Port.HasValue)
            {
                ThrowHelper.ThrowArgumentException_Pdu_InvalidAddr(address);
            }

            return new AddressData(address.System, address.Host, (uint)address.Port.Value, address.Protocol);
        }

        #endregion

        public AkkaPduMessagePackCodec(ActorSystem system) : base(system) { }
    }

    #endregion
}