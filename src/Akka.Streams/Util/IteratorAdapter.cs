﻿//-----------------------------------------------------------------------
// <copyright file="IteratorAdapter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Akka.Streams.Util
{
    /// <summary>
    /// Interface matching Java's iterator semantics.
    /// Should only be needed in rare circumstances, where knowing whether there are
    /// more elements without consuming them makes the code easier to write.
    /// </summary>
    /// <typeparam name="T">TBD</typeparam>
    internal interface IIterator<out T>
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        bool HasNext();
        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        T Next();
    }

    /// <summary>
    /// TBD
    /// </summary>
    /// <typeparam name="T">TBD</typeparam>
    internal class IteratorAdapter<T> : IIterator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private bool? _hasNext;
        private Exception _exception;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="enumerator">TBD</param>
        public IteratorAdapter(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public bool HasNext()
        {
            if (_hasNext is null)
            {
                try
                {
                    _hasNext = _enumerator.MoveNext();
                    _exception = null;
                }
                catch (Exception e)
                {
                    // capture exception and throw it when Next() is called
                    _exception = e;
                    _hasNext = true;
                }
            }

            return _hasNext.Value;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="InvalidOperationException">TBD</exception>
        /// <returns>TBD</returns>
        public T Next()
        {
            if (!HasNext()) ThrowHelper.ThrowInvalidOperationException();
            if (_exception is object) ThrowHelper.ThrowAggregateException(_exception);

            _hasNext = null;
            _exception = null;

            return _enumerator.Current;
        }
    }
}
