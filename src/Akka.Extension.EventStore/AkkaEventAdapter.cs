using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Serialization.Protocol;
using CuteAnt.Reflection;
using EventStore.ClientAPI;
using SpanJson;
using SpanJson.Internal;

namespace Akka.Extension.EventStore
{
    public class AkkaEventAdapter : EventAdapter<AkkaEventMetadata>
    {
        private readonly Akka.Serialization.Serialization _serialization;

        public AkkaEventAdapter(Akka.Serialization.Serialization serialization) => _serialization = serialization;

        public override EventData Adapt(object message, AkkaEventMetadata eventMeta)
        {
            //if (eventMeta is null) { ThrowArgumentNullException_EventMeta(); }

            if (eventMeta is null) { eventMeta = CreateEventMetadata(); }

            var payload = _serialization.SerializeExternalMessageWithTransport(message);
            var msgType = payload.MessageType;
            eventMeta.ClrEventType = RuntimeTypeNameFormatter.Format(msgType);
            eventMeta.Identifier = payload.Identifier;
            eventMeta.Manifest = payload.Manifest;
            return new EventData(
                Guid.NewGuid(),
                StringMutator.ToCamelCaseWithCache(msgType.Name),
                payload.IsJson,
                payload.Message,
                JsonCamelCaseSerializer.Generic.Utf8.Serialize(eventMeta));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AkkaEventMetadata CreateEventMetadata() => new AkkaEventMetadata();

        public override object Adapt(byte[] eventData, AkkaEventMetadata eventMeta)
        {
            if (eventData is null) { ThrowArgumentNullException_EventData(); }

            var payload = new ExternalPayload(eventData, eventMeta.Identifier, eventMeta.ClrEventType, eventMeta.Manifest);
            return _serialization.Deserialize(payload);
        }

        public override IEventMetadata ToEventMetadata(Dictionary<string, object> context)
        {
            return new AkkaEventMetadata { Context = context };
        }

        public override IEventMetadata ToEventMetadata(byte[] metadata)
        {
            return JsonCamelCaseSerializer.Generic.Utf8.Deserialize<AkkaEventMetadata>(metadata);
        }

        #region ** ThrowHelper **

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNullException_EventMeta()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("eventMeta");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNullException_EventData()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("eventData");
            }
        }

        #endregion
    }
}
