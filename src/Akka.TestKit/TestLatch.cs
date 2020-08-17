﻿//-----------------------------------------------------------------------
// <copyright file="TestLatch.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;
using Akka.Actor;
using MessagePack;

namespace Akka.TestKit
{
    /// <summary>
    /// <para>A count down latch that initially is closed. In order for it to become open <see cref="CountDown"/> must be called.
    /// By default one call is enough, but this can be changed by specifying the count in the constructor.</para>
    /// 
    /// <para>By default a timeout of 5 seconds is used.</para>
    /// <para>
    /// When created using <see cref="TestKitBase.CreateTestLatch">TestKit.CreateTestLatch</see> the default
    /// timeout from <see cref="TestKitSettings.DefaultTimeout"/> is used and all timeouts are dilated, i.e. multiplied by 
    /// <see cref="TestKitSettings.TestTimeFactor"/>
    /// </para>
    /// Timeouts will always throw an exception.
    /// </summary>
    [MessagePackObject]
    public class TestLatch
    {
        [Key(0)]
        private readonly Func<TimeSpan, TimeSpan> _dilate;
        [Key(1)]
        private readonly int _count;
        [IgnoreMember, IgnoreDataMember]
        private readonly CountdownEvent _latch;
        [Key(2)]
        private readonly TimeSpan _defaultTimeout;

        /// <summary>
        /// Obsolete. This field will be removed. <see cref="TestKitSettings.DefaultTimeout"/> is an alternative.
        /// </summary>
        [Obsolete("This field will be removed. TestKitSettings.DefaultTimeout is an alternative.")]
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestLatch"/> class with count = 1, i.e. the 
        /// instance will become open after one call to <see cref="CountDown"/>.
        /// The default timeout is set to 5 seconds.
        /// </summary>
        public TestLatch()
            : this(1, TimeSpan.FromSeconds(5))
        {
            //Intentionally left blank
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestLatch"/> class with the specified count, i.e
        /// number of times <see cref="CountDown"/> must be called to make this instance become open.
        /// The default timeout is set to 5 seconds.
        /// </summary>
        /// <param name="count">TBD</param>
        public TestLatch(int count)
            : this(count, TimeSpan.FromSeconds(5))
        {
            //Intentionally left blank
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestLatch"/> class with the specified count, i.e
        /// number of times <see cref="CountDown"/> must be called to make this instance become open.
        /// </summary>
        /// <param name="count">TBD</param>
        /// <param name="defaultTimeout">TBD</param>
        public TestLatch(int count, TimeSpan defaultTimeout)
        {
            _count = count;
            _latch = new CountdownEvent(count);
            _defaultTimeout = defaultTimeout;
        }

        /// <summary>
        /// Creates a TestLatch with the specified dilate function, timeout and count. 
        /// Intended to be used by TestKit.
        /// </summary>
        /// <param name="dilate">TBD</param>
        /// <param name="count">TBD</param>
        /// <param name="defaultTimeout">TBD</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SerializationConstructor]
        public TestLatch(Func<TimeSpan, TimeSpan> dilate, int count, TimeSpan defaultTimeout)
            :this(dilate, defaultTimeout,count)
        {
        }

        //This one exists to be available to inheritors
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="dilate">TBD</param>
        /// <param name="defaultTimeout">TBD</param>
        /// <param name="count">TBD</param>
        protected TestLatch(Func<TimeSpan, TimeSpan> dilate, TimeSpan defaultTimeout, int count)
            : this(count, defaultTimeout)
        {
            _dilate = dilate;
        }

        /// <summary>
        /// Gets a value indicating whether the latch is open.
        /// </summary>
        [IgnoreMember]
        public bool IsOpen
        {
            get { return _latch.CurrentCount == 0; }
        }

        /// <summary>
        /// Count down the latch.
        /// </summary>
        public void CountDown()
        {
            _latch.Signal();
        }

        /// <summary>
        /// Make this instance become open.
        /// </summary>
        public void Open()
        {
            while(!IsOpen) CountDown();
        }

        /// <summary>
        /// Reset this instance to the initial count, making it become closed.
        /// </summary>
        public void Reset()
        {
            _latch.Reset();
        }

        /// <summary>
        /// Expects the latch to become open within the specified timeout. If the timeout is reached, a
        /// <see cref="TimeoutException"/> is thrown.
        /// <para>
        /// If this instance has been created using <see cref="TestKitBase.CreateTestLatch">TestKit.CreateTestLatch</see> 
        /// <paramref name="timeout"/> is dilated, i.e. multiplied by <see cref="TestKitSettings.TestTimeFactor"/>
        /// </para>
        /// </summary>
        /// <param name="timeout">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when a too large timeout has been specified.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// This exception is thrown when the timeout is reached.
        /// </exception>
        public void Ready(TimeSpan timeout)
        {
            if(timeout == TimeSpan.MaxValue)
                throw new ArgumentException($"TestLatch does not support waiting for {timeout}");
            if(_dilate is object)
                timeout = _dilate(timeout);
            var opened = _latch.Wait(timeout);
            if(!opened)
                throw new TimeoutException($"Timeout of {timeout}");
        }

        /// <summary>
        /// Expects the latch to become open within the default timeout. If the timeout is reached, a
        /// <see cref="TimeoutException"/> is thrown.
        /// <para>If no timeout was specified when creating this instance, 5 seconds is used.</para>
        /// <para>If this instance has been created using <see cref="TestKitBase.CreateTestLatch">TestKit.CreateTestLatch</see> the default
        /// timeout from <see cref="TestKitSettings.DefaultTimeout"/> is used and dilated, i.e. multiplied by 
        /// <see cref="TestKitSettings.TestTimeFactor"/>
        /// </para>
        /// </summary>
        /// <exception cref="TimeoutException">
        /// This exception is thrown when the timeout is reached.
        /// </exception>
        public void Ready()
        {
            Ready(_defaultTimeout);
        }
    }
}
