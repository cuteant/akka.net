using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Actor;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
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

            var message = formatterResolver.Deserialize(selectionEnvelope.Payload);
            var elements = ParsePattern(selectionEnvelope.Pattern);
            return new ActorSelectionMessage(message, elements);
        }

        public int Serialize(ref byte[] bytes, int offset, ActorSelectionMessage value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.SelectionEnvelope(
                formatterResolver.Serialize(value.Message),
                GetPattern(value));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SelectionEnvelope>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<Protocol.Selection> GetPattern(ActorSelectionMessage sel)
        {
            var pattern = new List<Protocol.Selection>(sel.Elements.Length);
            foreach (var element in sel.Elements)
            {
                switch (element)
                {
                    case SelectChildName childName:
                        pattern.Add(BuildPattern(childName.Name, Protocol.Selection.PatternType.ChildName));
                        break;
                    case SelectChildPattern childPattern:
                        pattern.Add(BuildPattern(childPattern.PatternStr, Protocol.Selection.PatternType.ChildPattern));
                        break;
                    case SelectParent parent:
                        pattern.Add(BuildPattern(null, Protocol.Selection.PatternType.Parent));
                        break;
                }
            }
            return pattern;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SelectionPathElement[] ParsePattern(List<Protocol.Selection> pattern)
        {
            var elements = new SelectionPathElement[pattern.Count];
            for (var i = 0; i < pattern.Count; i++)
            {
                var p = pattern[i];
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
            return elements;
        }

        private static Protocol.Selection BuildPattern(string matcher, Protocol.Selection.PatternType patternType)
        {
            return new Protocol.Selection(patternType, matcher);
        }
    }
}
