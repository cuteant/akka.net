using System;
using CuteAnt.Collections;
using Serilog.Events;
using Serilog.Parsing;

namespace Akka.Logger.Serilog
{
    /// <summary>
    /// Taken directly from Serilog as the cache was internal.
    /// https://github.com/serilog/serilog/blob/master/src/Serilog/Core/Pipeline/MessageTemplateCache.cs
    /// </summary>
    internal class MessageTemplateCache
    {
        private readonly MessageTemplateParser _innerParser;
        private readonly CachedReadConcurrentDictionary<string, MessageTemplate> _templates = new CachedReadConcurrentDictionary<string, MessageTemplate>(StringComparer.Ordinal);

        const uint MaxCacheItems = 1000u;

        public MessageTemplateCache(MessageTemplateParser innerParser)
        {
            if (innerParser is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerParser); }
            _innerParser = innerParser;
        }

        public MessageTemplate Parse(string messageTemplate)
        {
            if (messageTemplate is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.messageTemplate); }

            if (_templates.TryGetValue(messageTemplate, out var result)) { return result; }

            result = _innerParser.Parse(messageTemplate);

            // Exceeding MaxCacheItems is *not* the sunny day scenario; all we're doing here is preventing out-of-memory
            // conditions when the library is used incorrectly. Correct use (templates, rather than
            // direct message strings) should barely, if ever, overflow this cache.

            if (MaxCacheItems >= (uint)_templates.Count)
            {
                _templates[messageTemplate] = result;
            }
            return result;
        }
    }
}
