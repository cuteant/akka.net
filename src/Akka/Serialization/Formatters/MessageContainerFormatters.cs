﻿using System.Collections.Generic;
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

        public ActorSelectionMessage Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SelectionEnvelope>();
            var selectionEnvelope = formatter.Deserialize(ref reader, DefaultResolver);

            var message = formatterResolver.Deserialize(selectionEnvelope.Payload);
            var elements = ParsePattern(selectionEnvelope.Pattern);
            return new ActorSelectionMessage(message, elements);
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, ActorSelectionMessage value, IFormatterResolver formatterResolver)
        {
            if (value is null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.SelectionEnvelope(
                formatterResolver.SerializeMessage(value.Message),
                GetPattern(value));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SelectionEnvelope>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
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
                    case SelectParent _:
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
