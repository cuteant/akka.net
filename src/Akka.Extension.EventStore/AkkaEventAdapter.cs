using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using CuteAnt.Collections;
using CuteAnt.Reflection;
using EventStore.ClientAPI;
using Utf8Json;

namespace Akka.Extension.EventStore
{
    public class AkkaEventAdapter : EventAdapter<AkkaEventMetadata>
    {
        private static readonly IJsonFormatterResolver s_defaultResolver = Utf8JsonStandardResolver.CamelCase;

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
                s_camelCaseTypeNameCache.GetOrAdd(msgType.Name, s_toCamelCaseFunc),
                payload.IsJson,
                payload.Message,
                JsonSerializer.Serialize(eventMeta, s_defaultResolver));
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
            return JsonSerializer.Deserialize<AkkaEventMetadata>(metadata, s_defaultResolver);
        }

        #region ** ToCamelCase **

        private static readonly CachedReadConcurrentDictionary<string, string> s_camelCaseTypeNameCache =
            new CachedReadConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly Func<string, string> s_toCamelCaseFunc = ToCamelCase;
        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0])) { return s; }

            char[] chars = s.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i])) { break; }

                bool hasNext = (i + 1 < chars.Length);
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    if (char.IsSeparator(chars[i + 1]))
                    {
                        chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
                    }

                    break;
                }

                chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
            }

            return new string(chars);
        }

        #endregion

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
