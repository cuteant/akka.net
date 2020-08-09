//-----------------------------------------------------------------------
// <copyright file="MessageContainerSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Actor;
using MessagePack;

namespace Akka.Serialization
{
    /// <summary>
    /// This is a special <see cref="Serializer"/> that serializes and deserializes <see cref="ActorSelectionMessage"/> only.
    /// </summary>
    public sealed class MessageContainerSerializer : Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContainerSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public MessageContainerSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override object DeepCopy(object source)
        {
            if (null == source) return null;
            var sel = source as ActorSelectionMessage;
            if (null == sel) { AkkaThrowHelper.ThrowArgumentException_Serializer_ActorSel(source); }

            var pattern = GetPattern(sel);
            var payload = _system.SerializeMessage(sel.Message);

            var message = _system.Deserialize(payload);
            var elements = ParsePattern(pattern);

            return new ActorSelectionMessage(message, elements);
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            var sel = obj as ActorSelectionMessage;
            if (sel is null) { AkkaThrowHelper.ThrowArgumentException_Serializer_ActorSel(obj); }

            var protoMessage = new Protocol.SelectionEnvelope(
                _system.SerializeMessage(sel.Message),
                GetPattern(sel));

            return MessagePackSerializer.Serialize(protoMessage, s_defaultResolver);
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            var selectionEnvelope = MessagePackSerializer.Deserialize<Protocol.SelectionEnvelope>(bytes, s_defaultResolver);

            var message = _system.Deserialize(selectionEnvelope.Payload);
            var elements = ParsePattern(selectionEnvelope.Pattern);
            return new ActorSelectionMessage(message, elements);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<Protocol.Selection> GetPattern(ActorSelectionMessage sel)
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
        internal static SelectionPathElement[] ParsePattern(List<Protocol.Selection> pattern)
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

        internal static Protocol.Selection BuildPattern(string matcher, Protocol.Selection.PatternType patternType)
        {
            return new Protocol.Selection(patternType, matcher);
        }
    }
}
