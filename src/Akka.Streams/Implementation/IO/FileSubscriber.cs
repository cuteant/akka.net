﻿//-----------------------------------------------------------------------
// <copyright file="FileSubscriber.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams.Actors;
using Akka.Streams.IO;
using Akka.Util;

namespace Akka.Streams.Implementation.IO
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal class FileSubscriber : ActorSubscriber
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="f">TBD</param>
        /// <param name="completionPromise">TBD</param>
        /// <param name="bufferSize">TBD</param>
        /// <param name="startPosition">TBD</param>
        /// <param name="fileMode">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public static Props Props(FileInfo f, TaskCompletionSource<IOResult> completionPromise, int bufferSize, long startPosition, FileMode fileMode)
        {
            if (bufferSize <= 0) ThrowHelper.ThrowArgumentException_GreaterThanZero(ExceptionArgument.bufferSize, bufferSize);
            if (startPosition < 0) ThrowHelper.ThrowArgumentException_GreaterThanEqualZero(ExceptionArgument.startPosition, startPosition);

            return Actor.Props.Create(() => new FileSubscriber(f, completionPromise, bufferSize, startPosition, fileMode)).WithDeploy(Deploy.Local);
        }

        private readonly FileInfo _f;
        private readonly TaskCompletionSource<IOResult> _completionPromise;
        private readonly long _startPosition;
        private readonly FileMode _fileMode;
        private readonly ILoggingAdapter _log;
        private readonly WatermarkRequestStrategy _requestStrategy;
        private FileStream _chan;
        private long _bytesWritten;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="f">TBD</param>
        /// <param name="completionPromise">TBD</param>
        /// <param name="bufferSize">TBD</param>
        /// <param name="startPosition">TBD</param>
        /// <param name="fileMode">TBD</param>
        public FileSubscriber(FileInfo f, TaskCompletionSource<IOResult> completionPromise, int bufferSize, long startPosition, FileMode fileMode)
        {
            _f = f;
            _completionPromise = completionPromise;
            _startPosition = startPosition;
            _fileMode = fileMode;
            _log = Context.GetLogger();
            _requestStrategy = new WatermarkRequestStrategy(highWatermark: bufferSize);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override IRequestStrategy RequestStrategy => _requestStrategy;

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PreStart()
        {
            try
            {
                _chan = _f.Open(_fileMode, FileAccess.Write);
                if (_startPosition > 0)
                    _chan.Position = _startPosition;
                base.PreStart();
            }
            catch (Exception ex)
            {
                _completionPromise.TrySetResult(IOResult.Failed(_bytesWritten, ex));
                Cancel();
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            switch (message)
            {
                case OnNext next:
                    try
                    {
                        var byteString = (ByteString)next.Element;
                        var bytes = byteString.ToArray();
                        _chan.Write(bytes, 0, bytes.Length);
                        _bytesWritten += bytes.Length;
                    }
                    catch (Exception ex)
                    {
                        _completionPromise.TrySetResult(IOResult.Failed(_bytesWritten, ex));
                        Cancel();
                    }
                    return true;

                case OnError error:
                    _log.TearingDownFileSinkDueToUpstreamError(error, _f);
                    _completionPromise.TrySetResult(IOResult.Failed(_bytesWritten, error.Cause));
                    Context.Stop(Self);
                    return true;

                case OnComplete _:
                    try
                    {
                        _chan.Flush(true);
                    }
                    catch (Exception ex)
                    {
                        _completionPromise.TrySetResult(IOResult.Failed(_bytesWritten, ex));
                    }
                    Context.Stop(Self);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            try
            {
                _chan?.Dispose();
            }
            catch (Exception ex)
            {
                _completionPromise.TrySetResult(IOResult.Failed(_bytesWritten, ex));
            }

            _completionPromise.TrySetResult(IOResult.Success(_bytesWritten));
            base.PostStop();
        }
    }
}
