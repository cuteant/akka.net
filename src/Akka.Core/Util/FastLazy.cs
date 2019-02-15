//-----------------------------------------------------------------------
// <copyright file="FastLazy.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Akka.Util
{
    /// <summary>
    /// A fast, atomic lazy that only allows a single publish operation to happen,
    /// but allows executions to occur concurrently.
    /// 
    /// Does not cache exceptions. Designed for use with <typeparamref name="T"/> types that are <see cref="IDisposable"/>
    /// or are otherwise considered to be expensive to allocate. 
    /// 
    /// Read the full explanation here: https://github.com/Aaronontheweb/FastAtomicLazy#rationale
    /// </summary>
    /// <typeparam name="T">TBD</typeparam>
    public sealed class FastLazy<T>
    {
        private readonly Func<T> _producer;
        private int _created = Constants.False;
        private int _creating = Constants.False;
        private T _createdValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastLazy{T}"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if the given <paramref name="producer"/> is undefined.
        /// </exception>
        public FastLazy(Func<T> producer)
        {
            if (producer == null) { AkkaThrowHelper.ThrowArgumentNullException_ProducerCannotBeNull(); }
            _producer = producer;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public bool IsValueCreated => IsValueCreatedInternal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValueCreatedInternal()
        {
            return Volatile.Read(ref _created) == Constants.True;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValueCreationInProgress()
        {
            return Volatile.Read(ref _creating) == Constants.True;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public T Value
        {
            get
            {
                if (IsValueCreatedInternal())
                    return _createdValue;
                if (!IsValueCreationInProgress())
                {
                    Interlocked.Exchange(ref _creating, Constants.True);
                    _createdValue = _producer();
                    Interlocked.Exchange(ref _created, Constants.True);
                }
                else
                {
                    SpinWait.SpinUntil(IsValueCreatedInternal);
                }
                return _createdValue;
            }
        }
    }


    /// <summary>
    /// A fast, atomic lazy that only allows a single publish operation to happen,
    /// but allows executions to occur concurrently.
    /// 
    /// Does not cache exceptions. Designed for use with <typeparamref name="T"/> types that are <see cref="IDisposable"/>
    /// or are otherwise considered to be expensive to allocate. 
    /// 
    /// Read the full explanation here: https://github.com/Aaronontheweb/FastAtomicLazy#rationale
    /// </summary>
    /// <typeparam name="S">State type</typeparam>
    /// <typeparam name="T">Value type</typeparam>
    public sealed class FastLazy<S, T>
    {
        private readonly Func<S, T> _producer;
        private int _created = Constants.False;
        private int _creating = Constants.False;
        private T _createdValue;
        private S _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastLazy{T}"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown if the given <paramref name="producer"/> or <paramref name="state"/> is undefined.
        /// </exception>
        public FastLazy(Func<S, T> producer, S state)
        {
            if (producer == null) AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.producer, AkkaExceptionResource.ArgumentNull_FastLazyProducer);
            if (state == null) AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.state, AkkaExceptionResource.ArgumentNull_FastLazyState);
            _producer = producer;
            _state = state;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public bool IsValueCreated => IsValueCreatedInternal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValueCreatedInternal()
        {
            return Volatile.Read(ref _created) == Constants.True;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValueCreationInProgress()
        {
            return Volatile.Read(ref _creating) == Constants.True;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public T Value
        {
            get
            {
                if (IsValueCreatedInternal())
                    return _createdValue;
                if (!IsValueCreationInProgress())
                {
                    Interlocked.Exchange(ref _creating, Constants.True);
                    _createdValue = _producer(_state);
                    Interlocked.Exchange(ref _created, Constants.True);
                    _state = default; // for reference types to make it suitable for gc
                }
                else
                {
                    SpinWait.SpinUntil(IsValueCreatedInternal);
                }
                return _createdValue;
            }
        }
    }
}
