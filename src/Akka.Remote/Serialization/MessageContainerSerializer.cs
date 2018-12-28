//-----------------------------------------------------------------------
// <copyright file="MessageContainerSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Serialization;
using MessagePack;

namespace Akka.Remote.Serialization
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
        public override bool IncludeManifest => false;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            var sel = obj as ActorSelectionMessage;
            if (null == sel) { ThrowHelper.ThrowArgumentException_Serializer_ActorSel(obj); }

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
                    default:
                        break;
                }
            }

            return MessagePackSerializer.Serialize(new Protocol.SelectionEnvelope(
                WrappedPayloadSupport.PayloadToProto(system, sel.Message), pattern), s_defaultResolver);
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            var selectionEnvelope = MessagePackSerializer.Deserialize<Protocol.SelectionEnvelope>(bytes, s_defaultResolver);
            var message = WrappedPayloadSupport.PayloadFrom(system, selectionEnvelope.Payload);

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

        internal static Protocol.Selection BuildPattern(string matcher, Protocol.Selection.PatternType patternType)
        {
            return new Protocol.Selection(patternType, matcher);
        }
    }
}
