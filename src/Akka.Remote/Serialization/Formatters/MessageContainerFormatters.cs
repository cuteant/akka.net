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

            var elements = new SelectionPathElement[selectionEnvelope.Pattern.Count];
            for (var i = 0; i < selectionEnvelope.Pattern.Count; i++)
            {
                var p = selectionEnvelope.Pattern[i];
                switch (p.Type)
                {
                    case Protocol.Selection.PatternType.ChildName:
                        elements[i] = new SelectChildName(p.Matcher);
                        break;
                    case Protocol.Selection.PatternType.ChildPattern:
                        elements[i] = new SelectChildPattern(p.Matcher);
                        break;
                    case Protocol.Selection.PatternType.Parent:
                        elements[i] = new SelectParent();
                        break;
                    case Protocol.Selection.PatternType.NoPatern:
                    default:
                        break;
                }
            }

            return new ActorSelectionMessage(message, elements);
        }

        public int Serialize(ref byte[] bytes, int offset, ActorSelectionMessage value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SelectionEnvelope>();

            var pattern = new List<Protocol.Selection>(value.Elements.Length);
            foreach (var element in value.Elements)
            {
                switch (element)
                {
                    case SelectChildName childName:
                        pattern.Add(MessageContainerSerializer.BuildPattern(childName.Name, Protocol.Selection.PatternType.ChildName));
                        break;
                    case SelectChildPattern childPattern:
                        pattern.Add(MessageContainerSerializer.BuildPattern(childPattern.PatternStr, Protocol.Selection.PatternType.ChildPattern));
                        break;
                    case SelectParent parent:
                        pattern.Add(MessageContainerSerializer.BuildPattern(null, Protocol.Selection.PatternType.Parent));
                        break;
                    default:
                        break;
                }
            }
            var protoMessage = new Protocol.SelectionEnvelope(
                WrappedPayloadSupport.PayloadToProto(formatterResolver.GetActorSystem(), value.Message), pattern);

            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }
}
