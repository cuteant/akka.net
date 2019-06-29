using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using CuteAnt.Reflection;
using EventStore.ClientAPI;
using SpanJson;
using SpanJson.Internal;
using SpanJson.Resolvers;

namespace Akka.Extension.EventStore
{
    public class AkkaEventAdapter : EventAdapter<AkkaEventMetadata>
    {
        private readonly Akka.Serialization.Serialization _serialization;

        public AkkaEventAdapter(Akka.Serialization.Serialization serialization) => _serialization = serialization;

        public override EventData Adapt(object message, AkkaEventMetadata eventMeta)
        {
            //if (eventMeta == null) { ThrowArgumentNullException_EventMeta(); }

            if (null == eventMeta) { eventMeta = CreateEventMetadata(); }

            var payload = _serialization.ToExternalPayload(message);
            var msgType = payload.MessageType;
            eventMeta.ClrEventType = RuntimeTypeNameFormatter.Format(msgType);
            eventMeta.Identifier = payload.Identifier;
            eventMeta.Manifest = payload.Manifest;
            return new EventData(
                Guid.NewGuid(),
                StringMutator.ToCamelCaseWithCache(msgType.Name),
                payload.IsJson,
                payload.Message,
                JsonSerializer.Generic.Utf8.Serialize<AkkaEventMetadata, ExcludeNullsCamelCaseResolver<byte>>(eventMeta));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AkkaEventMetadata CreateEventMetadata() => new AkkaEventMetadata();

        public override object Adapt(byte[] eventData, AkkaEventMetadata eventMeta)
        {
            if (null == eventData) { ThrowArgumentNullException_EventData(); }

            var payload = new ExternalPayload(eventData, eventMeta.Identifier, eventMeta.ClrEventType, eventMeta.Manifest);
            return _serialization.Deserialize(payload);
        }

        public override IEventMetadata ToEventMetadata(Dictionary<string, object> context)
        {
            return new AkkaEventMetadata { Context = context };
        }

        public override IEventMetadata ToEventMetadata(byte[] metadata)
        {
            return JsonSerializer.Generic.Utf8.Deserialize<AkkaEventMetadata, ExcludeNullsCamelCaseResolver<byte>>(metadata);
        }

        #region ** ThrowHelper **

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNullException_EventMeta()
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException("eventMeta");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNullException_EventData()
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException("eventData");
            }
        }

        #endregion
    }
}
