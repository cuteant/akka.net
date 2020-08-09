//-----------------------------------------------------------------------
// <copyright file="BatchWriter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2019 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using DotNettyIRunnable = DotNetty.Common.Concurrency.IRunnable;

namespace Akka.Remote.Transport.DotNetty
{
    /// <summary>
    /// INTERNAL API.
    /// 
    /// Responsible for batching socket writes together into fewer sys calls to the socket.
    /// </summary>
    internal class BatchWriterHandler : ChannelHandlerAdapter
    {
        public readonly BatchWriterSettings Settings;

        internal bool CanSchedule { get; private set; } = true;

        public BatchWriterHandler(BatchWriterSettings settings)
        {
            Settings = settings;
        }

        private int _currentPendingWrites = 0;
        private long _currentPendingBytes;

        public bool HasPendingWrites => (uint)_currentPendingWrites > 0u;

        public override void HandlerAdded(IChannelHandlerContext context)
        {
            ScheduleFlush(context); // only schedule flush operations when batching is enabled

            base.HandlerAdded(context);
        }

        public override void Write(IChannelHandlerContext context, object message, IPromise promise)
        {
            /*
             * Need to add the write to the rest of the pipeline first before we
             * include it in the formula for determining whether or not we flush
             * right now. The reason being is that if we did this the other way around,
             * we could flush first before the write was in the "flushable" buffer and
             * this can lead to "dangling writes" that never actually get transmitted
             * across the network.
             */
            base.Write(context, message, promise);

            _currentPendingBytes += ((IByteBuffer)message).ReadableBytes;
            _currentPendingWrites++;
            if (_currentPendingWrites >= Settings.MaxPendingWrites
                || _currentPendingBytes >= Settings.MaxPendingBytes)
            {
                context.Flush();
                Reset();
            }
        }

        public override void Close(IChannelHandlerContext context, IPromise promise)
        {
            // flush any pending writes first
            context.Flush();
            CanSchedule = false;
            base.Close(context, promise);
        }

        private void ScheduleFlush(IChannelHandlerContext context)
        {
            // Schedule a recurring flush - only fires when there's writable data
            var task = new FlushTask(context, Settings.FlushInterval, this);
            context.Executor.Schedule(task, Settings.FlushInterval);
            //context.Executor.ScheduleWithFixedDelay(task, Settings.FlushInterval);
        }

        public void Reset()
        {
            _currentPendingWrites = 0;
            _currentPendingBytes = 0;
        }

        class FlushTask : DotNettyIRunnable
        {
            private readonly IChannelHandlerContext _context;
            private readonly TimeSpan _interval;
            private readonly BatchWriterHandler _writer;

            public FlushTask(IChannelHandlerContext context, TimeSpan interval, BatchWriterHandler writer)
            {
                _context = context;
                _interval = interval;
                _writer = writer;
            }

            public void Run()
            {
                if (_writer.HasPendingWrites)
                {
                    // execute a flush operation
                    _context.Flush();
                    _writer.Reset();
                }

                if (_writer.CanSchedule)
                {
                    _context.Executor.Schedule(this, _interval); // reschedule
                }
            }
        }
    }
}
