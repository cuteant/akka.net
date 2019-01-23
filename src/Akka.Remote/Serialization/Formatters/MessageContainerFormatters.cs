using System.Collections.Generic;
using Akka.Actor;
using Akka.Serialization;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Formatters
{

    public sealed class ActorSelectionMessageFormatter : IMessagePackFormatter<ActorSelectionMessage>
    {
        public static readonly IMessagePackFormatter<ActorSelectionMessage> Instance = new ActorSelectionMessageFormatter();

        private static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;

        private ActorSelectionMessageFormatter() { }

        public ActorSelectionMessage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SelectionEnvelope>();
            var selectionEnvelope = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var message = WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), selectionEnvelope.Payload);
            var elements = MessageContainerSerializer.ParsePattern(selectionEnvelope.Pattern);
            return new ActorSelectionMessage(message, elements);
        }

        public int Serialize(ref byte[] bytes, int offset, ActorSelectionMessage value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.SelectionEnvelope(
                WrappedPayloadSupport.PayloadToProto(formatterResolver.GetActorSystem(), value.Message),
                MessageContainerSerializer.GetPattern(value));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SelectionEnvelope>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }
}
