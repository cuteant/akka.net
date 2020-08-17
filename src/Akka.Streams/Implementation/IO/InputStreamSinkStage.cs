//-----------------------------------------------------------------------
// <copyright file="InputStreamSinkStage.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using Akka.IO;
using Akka.Pattern;
using Akka.Streams.Implementation.Stages;
using Akka.Streams.Stage;
using static Akka.Streams.Implementation.IO.InputStreamSinkStage;

namespace Akka.Streams.Implementation.IO
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal class InputStreamSinkStage : GraphStageWithMaterializedValue<SinkShape<ByteString>, Stream>
    {
        #region internal classes

        /// <summary>
        /// TBD
        /// </summary>
        internal interface IAdapterToStageMessage
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class ReadElementAcknowledgement : IAdapterToStageMessage, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly ReadElementAcknowledgement Instance = new ReadElementAcknowledgement();

            private ReadElementAcknowledgement()
            {

            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class Close : IAdapterToStageMessage, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Close Instance = new Close();

            private Close()
            {

            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal interface IStreamToAdapterMessage
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal readonly struct Data : IStreamToAdapterMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly ByteString Bytes;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="bytes">TBD</param>
            public Data(ByteString bytes)
            {
                Bytes = bytes;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal class Finished : IStreamToAdapterMessage, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Finished Instance = new Finished();

            private Finished()
            {

            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal class Initialized : IStreamToAdapterMessage, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Initialized Instance = new Initialized();

            private Initialized()
            {

            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal readonly struct Failed : IStreamToAdapterMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public readonly Exception Cause;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="cause">TBD</param>
            public Failed(Exception cause)
            {
                Cause = cause;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal interface IStageWithCallback
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="msg">TBD</param>
            void WakeUp(IAdapterToStageMessage msg);
        }

        private sealed class Logic : InGraphStageLogic, IStageWithCallback, IHandle<IAdapterToStageMessage>
        {
            private readonly InputStreamSinkStage _stage;
            private readonly IHandle<IAdapterToStageMessage> _callback;
            private bool _completionSignalled;

            public Logic(InputStreamSinkStage stage) : base(stage.Shape)
            {
                _stage = stage;
                _callback = GetAsyncCallback(this);

                SetHandler(stage._in, this);
            }

            void IHandle<IAdapterToStageMessage>.Handle(IAdapterToStageMessage message)
            {
                switch (message)
                {
                    case ReadElementAcknowledgement _:
                        SendPullIfAllowed();
                        break;
                    case Close _:
                        CompleteStage();
                        break;
                }
            }

            public override void OnPush()
            {
                //1 is buffer for Finished or Failed callback
                if (_stage._dataQueue.Count + 1 == _stage._dataQueue.BoundedCapacity) ThrowHelper.ThrowBufferOverflowException_Queue();

                _stage._dataQueue.Add(new Data(Grab(_stage._in)));
                if (_stage._dataQueue.BoundedCapacity - _stage._dataQueue.Count > 1)
                    SendPullIfAllowed();
            }

            public override void OnUpstreamFinish()
            {
                _stage._dataQueue.Add(Finished.Instance);
                _completionSignalled = true;
                CompleteStage();
            }

            public override void OnUpstreamFailure(Exception ex)
            {
                _stage._dataQueue.Add(new Failed(ex));
                _completionSignalled = true;
                FailStage(ex);
            }

            public override void PreStart()
            {
                _stage._dataQueue.Add(Initialized.Instance);
                Pull(_stage._in);
            }

            public override void PostStop()
            {
                if (!_completionSignalled)
                    _stage._dataQueue.Add(new Failed(new AbruptStageTerminationException(this)));
            }

            public void WakeUp(IAdapterToStageMessage msg) => _callback.Handle(msg);

            private void SendPullIfAllowed()
            {
                if (_stage._dataQueue.BoundedCapacity - _stage._dataQueue.Count > 1 && !HasBeenPulled(_stage._in))
                    Pull(_stage._in);
            }
        }

        #endregion

        private readonly Inlet<ByteString> _in = new Inlet<ByteString>("InputStreamSink.in");
        private readonly TimeSpan _readTimeout;
        private BlockingCollection<IStreamToAdapterMessage> _dataQueue;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="readTimeout">TBD</param>
        public InputStreamSinkStage(TimeSpan readTimeout)
        {
            _readTimeout = readTimeout;
            Shape = new SinkShape<ByteString>(_in);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override Attributes InitialAttributes => DefaultAttributes.InputStreamSink;

        /// <summary>
        /// TBD
        /// </summary>
        public override SinkShape<ByteString> Shape { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="inheritedAttributes">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the maximum size of the input buffer is less than or equal to zero.
        /// </exception>
        /// <returns>TBD</returns>
        public override ILogicAndMaterializedValue<Stream> CreateLogicAndMaterializedValue(
            Attributes inheritedAttributes)
        {
            var maxBuffer = inheritedAttributes.GetAttribute(new Attributes.InputBuffer(16, 16)).Max;
            if (maxBuffer <= 0) ThrowHelper.ThrowArgumentException_GreaterThanZero(ExceptionArgument.maxBuffer);

            _dataQueue = new BlockingCollection<IStreamToAdapterMessage>(maxBuffer + 2);

            var logic = new Logic(this);
            return new LogicAndMaterializedValue<Stream>(logic,
                new InputStreamAdapter(_dataQueue, logic, _readTimeout));
        }
    }

    /// <summary>
    /// INTERNAL API
    /// InputStreamAdapter that interacts with InputStreamSinkStage
    /// </summary>
    internal class InputStreamAdapter : Stream
    {
        #region not supported 

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="NotSupportedException">TBD</exception>
        public override void Flush() => ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R);

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="offset">TBD</param>
        /// <param name="origin">TBD</param>
        /// <exception cref="NotSupportedException">TBD</exception>
        /// <returns>TBD</returns>
        public override long Seek(long offset, SeekOrigin origin) { ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R); return 0L; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="value">TBD</param>
        /// <exception cref="NotSupportedException">TBD</exception>
        /// <returns>TBD</returns>
        public override void SetLength(long value) => ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R);

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="buffer">TBD</param>
        /// <param name="offset">TBD</param>
        /// <param name="count">TBD</param>
        /// <exception cref="NotSupportedException">TBD</exception>
        /// <returns>TBD</returns>
        public override void Write(byte[] buffer, int offset, int count) => ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R);

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="NotSupportedException">TBD</exception>
        public override long Length { get { ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R); return 0; } }

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="NotSupportedException">TBD</exception>
        public override long Position
        {
            get { ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R); return 0; }
            set => ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_Stream_Only_R);
        }

        #endregion


        private readonly BlockingCollection<IStreamToAdapterMessage> _sharedBuffer;
        private readonly IStageWithCallback _sendToStage;
        private readonly TimeSpan _readTimeout;
        private bool _isActive = true;
        private bool _isStageAlive = true;
        private bool _isInitialized;
        private ByteString _detachedChunk;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="sharedBuffer">TBD</param>
        /// <param name="sendToStage">TBD</param>
        /// <param name="readTimeout">TBD</param>
        public InputStreamAdapter(BlockingCollection<IStreamToAdapterMessage> sharedBuffer,
            IStageWithCallback sendToStage, TimeSpan readTimeout)
        {
            _sharedBuffer = sharedBuffer;
            _sendToStage = sendToStage;
            _readTimeout = readTimeout;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="disposing">TBD</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (ExecuteIfNotClosed())
            {
                // at this point Subscriber may be already terminated
                if (_isStageAlive)
                    _sendToStage.WakeUp(InputStreamSinkStage.Close.Instance);

                _isActive = false;
                //return NotUsed.Instance;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="IllegalStateException">
        /// This exception is thrown when an <see cref="Initialized"/> message is not the first message.
        /// </exception>
        /// <exception cref="IOException">
        /// This exception is thrown when a timeout occurs waiting on new data.
        /// </exception>
        /// <returns>TBD</returns>
        public sealed override int ReadByte()
        {
            var a = new byte[1];
            return Read(a, 0, 1) != 0 ? a[0] & 0xff : -1;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="buffer">TBD</param>
        /// <param name="offset">TBD</param>
        /// <param name="count">TBD</param>
        /// <exception cref="ArgumentException">TBD
        /// This exception is thrown for a number of reasons. These include:
        /// <ul>
        /// <li>the specified <paramref name="buffer"/> size is less than or equal to zero</li>
        /// <li>the specified <paramref name="buffer"/> size is less than the combination of <paramref name="offset"/> and <paramref name="count"/></li>
        /// <li>the specified <paramref name="offset"/> is less than zero</li>
        /// <li>the specified <paramref name="count"/> is less than or equal to zero</li>
        /// </ul>
        /// </exception>
        /// <exception cref="IllegalStateException">
        /// This exception is thrown when an <see cref="Initialized"/> message is not the first message.
        /// </exception>
        /// <exception cref="IOException">
        /// This exception is thrown when a timeout occurs waiting on new data.
        /// </exception>
        /// <returns>TBD</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (0u >= (uint)buffer.Length) ThrowHelper.ThrowArgumentException_GreaterThanZero(ExceptionArgument.buffer);
            if (offset < 0) ThrowHelper.ThrowArgumentException_GreaterThanEqualZero(ExceptionArgument.offset);
            if (count <= 0) ThrowHelper.ThrowArgumentException_GreaterThanZero(ExceptionArgument.count);
            if (offset + count > buffer.Length) ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_Offset_Count_Length);

            int ExecuteFunc()
            {
                if (!_isStageAlive) { return 0; }

                if (_detachedChunk is object) { return ReadBytes(buffer, offset, count); }

                var success = _sharedBuffer.TryTake(out var msg, _readTimeout);
                if (!success) { ThrowHelper.ThrowIOException_Timeout(); }

                switch (msg)
                {
                    case Data data:
                        _detachedChunk = data.Bytes;
                        return ReadBytes(buffer, offset, count);

                    case Finished _:
                        _isStageAlive = false;
                        return 0;

                    case Failed failed:
                        _isStageAlive = false;
                        throw failed.Cause;
                }
                ThrowHelper.ThrowIllegalStateException(ExceptionResource.IllegalState_init_mus_first); return 0;
            }

            ExecuteIfNotClosed();
            return ExecuteFunc();
        }

        private bool ExecuteIfNotClosed()
        {
            if (!_isActive) { ThrowHelper.ThrowIOException_SubscriberClosed(); }

            WaitIfNotInitialized();
            return true;
        }

        private void WaitIfNotInitialized()
        {
            if (_isInitialized) { return; }

            if (_sharedBuffer.TryTake(out var message, _readTimeout))
            {
                if (message is Initialized)
                {
                    _isInitialized = true;
                }
                else
                {
                    ThrowHelper.ThrowIllegalStateException(ExceptionResource.IllegalState_first_msg_init);
                }
            }
            else
            {
                ThrowHelper.ThrowIOException_Timeout(_readTimeout);
            }
        }

        private int ReadBytes(byte[] buffer, int offset, int count)
        {
            if (_detachedChunk is null || _detachedChunk.IsEmpty) ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_Chunk_be_pulled);

            var availableInChunk = _detachedChunk.Count;
            var readBytes = GetData(buffer, offset, count, 0);

            if (readBytes >= availableInChunk)
                _sendToStage.WakeUp(ReadElementAcknowledgement.Instance);

            return readBytes;
        }

        private int GetData(byte[] buffer, int offset, int count, int gotBytes)
        {
            var chunk = GrabDataChunk();
            if (chunk is null)
                return gotBytes;

            var size = chunk.Count;
            if (size <= count)
            {
                Array.Copy(chunk.ToArray(), 0, buffer, offset, size);
                _detachedChunk = null;
                if (size == count)
                    return gotBytes + size;

                return GetData(buffer, offset + size, count - size, gotBytes + size);
            }

            Array.Copy(chunk.ToArray(), 0, buffer, offset, count);
            _detachedChunk = chunk.Slice(count);
            return gotBytes + count;
        }

        private ByteString GrabDataChunk()
        {
            if (_detachedChunk is object)
                return _detachedChunk;

            var chunk = _sharedBuffer.Take();
            if (chunk is Data data)
            {
                _detachedChunk = data.Bytes;
                return _detachedChunk;
            }
            if (chunk is Finished)
                _isStageAlive = false;

            return null;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// TBD
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// TBD
        /// </summary>
        public override bool CanWrite => false;
    }
}
