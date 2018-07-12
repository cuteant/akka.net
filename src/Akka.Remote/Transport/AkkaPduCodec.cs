//-----------------------------------------------------------------------
// <copyright file="AkkaPduCodec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Remote.Serialization;
using Akka.Remote.Serialization.Protocol;
using MessagePack;
using SerializedMessage = Akka.Remote.Serialization.Protocol.Payload;

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

    #region == class Associate ==

    /// <summary>TBD</summary>
    internal sealed class Associate : IAkkaPdu
    {
        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        public Associate(HandshakeInfo info) => Info = info;

        /// <summary>TBD</summary>
        public HandshakeInfo Info { get; }
    }

    #endregion

    #region == class Disassociate ==

    /// <summary>TBD</summary>
    internal sealed class Disassociate : IAkkaPdu
    {
        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        public Disassociate(DisassociateInfo reason) => Reason = reason;

        /// <summary>TBD</summary>
        public DisassociateInfo Reason { get; }
    }

    #endregion

    #region == class Heartbeat ==

    /// <summary>INTERNAL API.
    ///
    /// Represents a heartbeat on the wire.
    /// </summary>
    internal sealed class Heartbeat : IAkkaPdu { }

    #endregion

    #region == class Payload ==

    /// <summary>TBD</summary>
    internal sealed class Payload : IAkkaPdu
    {
        /// <summary>TBD</summary>
        /// <param name="bytes">TBD</param>
        public Payload(byte[] bytes) => Bytes = bytes;

        /// <summary>TBD</summary>
        public byte[] Bytes { get; }
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
        public IInternalActorRef Recipient { get; }

        /// <summary>TBD</summary>
        public Address RecipientAddress { get; }

        /// <summary>TBD</summary>
        public SerializedMessage SerializedMessage { get; }

        /// <summary>TBD</summary>
        public IActorRef SenderOptional { get; }

        /// <summary>TBD</summary>
        public bool ReliableDeliveryEnabled => Seq != null;

        /// <summary>TBD</summary>
        public SeqNo Seq { get; }
    }

    #endregion

    #region == class AckAndMessage ==

    /// <summary>INTERNAL API</summary>
    internal sealed class AckAndMessage
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
        public Ack AckOption { get; }

        /// <summary>TBD</summary>
        public Message MessageOption { get; }
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
        /// <param name="raw">Encoded raw byte representation of an Akka PDU</param>
        /// <returns>Class representation of a PDU that can be used in a <see cref="PatternMatch"/>.</returns>
        public abstract IAkkaPdu DecodePdu(byte[] raw);

        /// <summary>Takes an <see cref="IAkkaPdu"/> representation of an Akka PDU and returns its encoded
        /// form as a <see cref="T:System.Byte{T}"/>.</summary>
        /// <param name="pdu">TBD</param>
        /// <returns>TBD</returns>
        public virtual byte[] EncodePdu(IAkkaPdu pdu)
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
        public abstract byte[] ConstructPayload(byte[] payload);

        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        /// <returns>TBD</returns>
        public abstract byte[] ConstructAssociate(HandshakeInfo info);

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        /// <returns>TBD</returns>
        public abstract byte[] ConstructDisassociate(DisassociateInfo reason);

        /// <summary>TBD</summary>
        /// <returns>TBD</returns>
        public abstract byte[] ConstructHeartbeat();

        /// <summary>TBD</summary>
        /// <param name="raw">TBD</param>
        /// <param name="provider">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <returns>TBD</returns>
        public abstract AckAndMessage DecodeMessage(byte[] raw, IRemoteActorRefProvider provider, Address localAddress);

        /// <summary>TBD</summary>
        /// <param name="localAddress">TBD</param>
        /// <param name="recipient">TBD</param>
        /// <param name="serializedMessage">TBD</param>
        /// <param name="senderOption">TBD</param>
        /// <param name="seqOption">TBD</param>
        /// <param name="ackOption">TBD</param>
        /// <returns>TBD</returns>
        public abstract byte[] ConstructMessage(Address localAddress, IActorRef recipient,
            SerializedMessage serializedMessage, IActorRef senderOption = null, SeqNo seqOption = null, Ack ackOption = null);

        /// <summary>TBD</summary>
        /// <param name="ack">TBD</param>
        /// <returns>TBD</returns>
        public abstract byte[] ConstructPureAck(Ack ack);
    }

    #endregion

    #region == class AkkaPduMessagePackCodec ==

    /// <summary>TBD</summary>
    internal sealed class AkkaPduMessagePackCodec : AkkaPduCodec
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>TBD</summary>
        /// <param name="raw">TBD</param>
        /// <exception cref="PduCodecException">
        /// This exception is thrown when the Akka PDU in the specified byte string, <paramref
        /// name="raw"/>, meets one of the following conditions: <ul><li>The PDU is neither a message
        /// or a control message.</li><li>The PDU is a control message with an invalid format.</li></ul>
        /// </exception>
        /// <returns>TBD</returns>
        public override IAkkaPdu DecodePdu(byte[] raw)
        {
            try
            {
                var pdu = MessagePackSerializer.Deserialize<AkkaProtocolMessage>(raw, s_defaultResolver);
                if (pdu.Instruction != null)
                {
                    return DecodeControlPdu(pdu.Instruction);
                }
                else if (pdu.Payload != null)
                {
                    return new Payload(pdu.Payload); // TODO HasPayload
                }
                else
                {
                    return ThrowHelper.ThrowPduCodecException_Decode();
                }
            }
            catch (FormatterNotRegisteredException ex) // InvalidProtocolBufferException
            {
                return ThrowHelper.ThrowPduCodecException_Decode(ex);
            }
        }

        /// <summary>TBD</summary>
        /// <param name="payload">TBD</param>
        /// <returns>TBD</returns>
        public override byte[] ConstructPayload(byte[] payload) => MessagePackSerializer.Serialize(new AkkaProtocolMessage(payload, null), s_defaultResolver);

        /// <summary>TBD</summary>
        /// <param name="info">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="info"/> contains an invalid address.
        /// </exception>
        /// <returns>TBD</returns>
        public override byte[] ConstructAssociate(HandshakeInfo info)
        {
            var handshakeInfo = new AkkaHandshakeInfo(SerializeAddress(info.Origin), (ulong)info.Uid);

            return ConstructControlMessagePdu(CommandType.Associate, handshakeInfo);
        }

        /// <summary>TBD</summary>
        /// <param name="reason">TBD</param>
        /// <returns>TBD</returns>
        public override byte[] ConstructDisassociate(DisassociateInfo reason)
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
        private static readonly byte[] HeartbeatPdu = ConstructControlMessagePdu(CommandType.Heartbeat, null);

        /// <summary>Creates a new Heartbeat message instance.</summary>
        /// <returns>The Heartbeat message.</returns>
        public override byte[] ConstructHeartbeat() => HeartbeatPdu;

        /// <summary>Indicated RemoteEnvelope.Seq is not defined (order is irrelevant)</summary>
        private const ulong SeqUndefined = ulong.MaxValue;

        /// <summary>TBD</summary>
        /// <param name="raw">TBD</param>
        /// <param name="provider">TBD</param>
        /// <param name="localAddress">TBD</param>
        /// <returns>TBD</returns>
        public override AckAndMessage DecodeMessage(byte[] raw, IRemoteActorRefProvider provider, Address localAddress)
        {
            var ackAndEnvelope = MessagePackSerializer.Deserialize<AckAndEnvelopeContainer>(raw, s_defaultResolver);

            Ack ackOption = null;

            if (ackAndEnvelope.Ack != null)
            {
                ackOption = new Ack(new SeqNo((long)ackAndEnvelope.Ack.CumulativeAck), ackAndEnvelope.Ack.Nacks.Select(x => new SeqNo((long)x)));
            }

            Message messageOption = null;

            if (ackAndEnvelope.Envelope != null)
            {
                var envelopeContainer = ackAndEnvelope.Envelope;
                if (envelopeContainer != null)
                {
                    var recipient = provider.ResolveActorRefWithLocalAddress(envelopeContainer.Recipient.Path, localAddress);
                    Address recipientAddress;
                    if (AddressCache != null)
                    {
                        recipientAddress = AddressCache.Cache.GetOrCompute(envelopeContainer.Recipient.Path);
                    }
                    else
                    {
                        ActorPath.TryParseAddress(envelopeContainer.Recipient.Path, out recipientAddress);
                    }

                    var serializedMessage = envelopeContainer.Message;
                    IActorRef senderOption = null;
                    if (envelopeContainer.Sender != null)
                    {
                        senderOption = provider.ResolveActorRefWithLocalAddress(envelopeContainer.Sender.Path, localAddress);
                    }
                    SeqNo seqOption = null;
                    if (envelopeContainer.Seq != SeqUndefined)
                    {
                        unchecked
                        {
                            seqOption = new SeqNo((long)envelopeContainer.Seq); //proto takes a ulong
                        }
                    }
                    messageOption = new Message(recipient, recipientAddress, serializedMessage, senderOption, seqOption);
                }
            }

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
        public override byte[] ConstructMessage(Address localAddress, IActorRef recipient, SerializedMessage serializedMessage,
            IActorRef senderOption = null, SeqNo seqOption = null, Ack ackOption = null)
        {
            var ackAndEnvelope = new AckAndEnvelopeContainer();
            var envelope = new RemoteEnvelope() { Recipient = SerializeActorRef(recipient.Path.Address, recipient) };
            if (senderOption != null && senderOption.Path != null) { envelope.Sender = SerializeActorRef(localAddress, senderOption); }
            if (seqOption != null) { envelope.Seq = (ulong)seqOption.RawValue; } else envelope.Seq = SeqUndefined;
            if (ackOption != null) { ackAndEnvelope.Ack = AckBuilder(ackOption); }
            envelope.Message = serializedMessage;
            ackAndEnvelope.Envelope = envelope;

            return MessagePackSerializer.Serialize(ackAndEnvelope, s_defaultResolver);
        }

        /// <summary>TBD</summary>
        /// <param name="ack">TBD</param>
        /// <returns>TBD</returns>
        public override byte[] ConstructPureAck(Ack ack)
            => MessagePackSerializer.Serialize(new AckAndEnvelopeContainer() { Ack = AckBuilder(ack) }, s_defaultResolver);

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

            return ThrowHelper.ThrowPduCodecException_Decode(controlPdu);
        }

        private static byte[] DISASSOCIATE => ConstructControlMessagePdu(CommandType.Disassociate);

        private static byte[] DISASSOCIATE_SHUTTING_DOWN => ConstructControlMessagePdu(CommandType.DisassociateShuttingDown);

        private static byte[] DISASSOCIATE_QUARANTINED => ConstructControlMessagePdu(CommandType.DisassociateQuarantined);

        private static byte[] ConstructControlMessagePdu(CommandType code, AkkaHandshakeInfo handshakeInfo = null)
        {
            var controlMessage = new AkkaControlMessage(code, handshakeInfo);

            return MessagePackSerializer.Serialize(new AkkaProtocolMessage(null, controlMessage), s_defaultResolver);
        }

        private static Address DecodeAddress(AddressData origin)
            => new Address(origin.Protocol, origin.System, origin.Hostname, (int)origin.Port);

        private static ActorRefData SerializeActorRef(Address defaultAddress, IActorRef actorRef)
        {
            return new ActorRefData(
                (!string.IsNullOrEmpty(actorRef.Path.Address.Host))
                    ? actorRef.Path.ToSerializationFormat()
                    : actorRef.Path.ToSerializationFormatWithAddress(defaultAddress));
        }

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