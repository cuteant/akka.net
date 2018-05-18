//-----------------------------------------------------------------------
// <copyright file="JsonObjectParser.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Annotations;
using Akka.IO;
using Akka.Streams.Dsl;
using Akka.Streams.Util;

namespace Akka.Streams.Implementation
{
    /// <summary>
    /// INTERNAL API: Use <see cref="JsonFraming"/> instead
    /// 
    /// **Mutable** framing implementation that given any number of <see cref="ByteString"/> chunks, can emit JSON objects contained within them.
    /// Typically JSON objects are separated by new-lines or commas, however a top-level JSON Array can also be understood and chunked up
    /// into valid JSON objects by this framing implementation.
    /// 
    /// Leading whitespace between elements will be trimmed.
    /// </summary>
    [InternalApi]
    public class JsonObjectParser
    {
        private const byte SquareBraceStart = (byte)'[';
        private const byte SquareBraceEnd = (byte)']';
        private const byte CurlyBraceStart = (byte)'{';
        private const byte CurlyBraceEnd = (byte)'}';
        private const byte DoubleQuote = (byte)'"';
        private const byte Backslash = (byte)'\\';
        private const byte Comma = (byte)',';

        private const byte LineBreak = (byte)'\n';
        private const byte LineBreak2 = (byte)'\r';
        private const byte Tab = (byte)'\t';
        private const byte Space = (byte)' ';

        private static readonly byte[] Whitespace = { LineBreak, LineBreak2, Tab, Space };

        private static bool IsWhitespace(byte input) => Whitespace.Contains(input);

        private readonly int _maximumObjectLength;
        private ByteString _buffer = ByteString.Empty;
        private int _pos; // latest position of pointer while scanning for json object end
        private int _trimFront;
        private int _depth;
        private int _charsInObject;
        private bool _completedObject;
        private bool _inStringExpression;
        private bool _isStartOfEscapeSequence;
        private byte _lastInput = 0;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="maximumObjectLength">TBD</param>
        public JsonObjectParser(int maximumObjectLength = int.MaxValue)
        {
            _maximumObjectLength = maximumObjectLength;
        }

        private bool OutsideObject => _depth == 0;

        private bool InsideObject => !OutsideObject;

        /// <summary>
        /// Appends input ByteString to internal byte string buffer.
        /// Use <see cref="Poll"/> to extract contained JSON objects.
        /// </summary>
        /// <param name="input">TBD</param>
        public void Offer(ByteString input) => _buffer += input;

        /// <summary>
        /// TBD
        /// </summary>
        public bool IsEmpty => _buffer.IsEmpty;

        /// <summary>
        /// Attempt to locate next complete JSON object in buffered <see cref="ByteString"/> and returns it if found.
        /// May throw a <see cref="Framing.FramingException"/> if the contained JSON is invalid or max object size is exceeded.
        /// </summary>
        /// <exception cref="Framing.FramingException">TBD</exception>
        /// <returns>TBD</returns>
        public Option<ByteString> Poll()
        {
            var foundObject = SeekObject();
            if (!foundObject || _pos == -1 || _pos == 0)
                return Option<ByteString>.None;

            var emit = _buffer.Slice(0, _pos);
            var buffer = _buffer.Slice(_pos);
            _buffer = buffer.Compact();
            _pos = 0;

            var trimFront = _trimFront;
            _trimFront = 0;

            if (trimFront == 0)
                return emit;

            var trimmed = emit.Slice(trimFront);
            return trimmed.IsEmpty ? Option<ByteString>.None : trimmed;
        }

        /// <summary>
        /// Returns true if an entire valid JSON object was found, false otherwise
        /// </summary>
        private bool SeekObject()
        {
            _completedObject = false;
            var bufferSize = _buffer.Count;
            while (_pos != -1 && (_pos < bufferSize && _pos < _maximumObjectLength) && !_completedObject)
                Proceed(_buffer[_pos]);

            if (_pos >= _maximumObjectLength)
                throw new Framing.FramingException(
                    $"JSON element exceeded maximumObjectLength ({_maximumObjectLength} bytes)!");

            return _completedObject;
        }

        private void Proceed(byte input)
        {
            switch (input)
            {
                case SquareBraceStart when (OutsideObject):
                    // outer object is an array
                    _pos++;
                    _trimFront++;
                    break;
                case SquareBraceEnd when (OutsideObject):
                    // outer array completed!
                    _pos = -1;
                    break;
                case Comma when (OutsideObject):
                    // do nothing
                    _pos++;
                    _trimFront++;
                    break;
                case Backslash:
                    _isStartOfEscapeSequence = _lastInput != Backslash;
                    _pos++;
                    break;
                case DoubleQuote:
                    if (!_isStartOfEscapeSequence)
                    {
                        _inStringExpression = !_inStringExpression;
                    }
                    _isStartOfEscapeSequence = false;
                    _pos++;
                    break;
                case CurlyBraceStart when (!_inStringExpression):
                    _isStartOfEscapeSequence = false;
                    _depth++;
                    _pos++;
                    break;
                case CurlyBraceEnd when (!_inStringExpression):
                    _isStartOfEscapeSequence = false;
                    _depth--;
                    _pos++;
                    if (_depth == 0)
                    {
                        _charsInObject = 0;
                        _completedObject = true;
                    }
                    break;
                default:
                    if (IsWhitespace(input) && !_inStringExpression)
                    {
                        _pos++;
                        if (_depth == 0) { _trimFront++; }
                    }
                    else if (InsideObject)
                    {
                        _isStartOfEscapeSequence = false;
                        _pos++;
                    }
                    else
                    {
                        throw new Framing.FramingException($"Invalid JSON encountered at position {_pos} of {_buffer}");
                    }
                    break;
            }

            _lastInput = input;
        }
    }
}
