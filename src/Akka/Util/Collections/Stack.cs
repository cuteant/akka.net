﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: An array implementation of a generic stack.
**
**
=============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using CuteAnt;

namespace Akka.Util
{
    // A simple stack of objects.  Internally it is implemented as an array,
    // so Push can be O(n).  Pop is O(1).

    [DebuggerTypeProxy(typeof(StackDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial class StackX<T> : IEnumerable<T>,
        System.Collections.ICollection,
        IReadOnlyCollection<T>
    {
        private T[] _array; // Storage for stack elements. Do not rename (binary serialization)
        private int _size; // Number of items in the stack. Do not rename (binary serialization)
        private int _version; // Used to keep enumerator in sync w/ collection. Do not rename (binary serialization)
        [NonSerialized]
        private object _syncRoot;

        private const int DefaultCapacity = 4;

        public StackX()
        {
            _array = EmptyArray<T>.Instance; // Array.Empty<T>();
        }

        // Create a stack with a specific initial capacity.  The initial capacity
        // must be a non-negative number.
        public StackX(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            _array = new T[capacity];
        }

        // Fills a Stack with the contents of a particular collection.  The items are
        // pushed onto the stack in the same order they are read by the enumerator.
        public StackX(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            _array = EnumerableHelpers.ToArray(collection, out _size);
        }

        public int Count
        {
            get { return _size; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        // Removes all Objects from the Stack.
        public void Clear()
        {
#if NETCOREAPP
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            }
#else
            Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
#endif
            _size = 0;
            _version++;
        }

        public bool Contains(T item)
        {
            // Compare items using the default equality comparer

            // PERF: Internally Array.LastIndexOf calls
            // EqualityComparer<T>.Default.LastIndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.LastIndexOf.

            return _size != 0 && Array.LastIndexOf(_array, item, _size - 1) != -1;
        }

        // Copies the stack into an array.
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_Index);
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            Debug.Assert(array != _array);
            int srcIndex = 0;
            int dstIndex = arrayIndex + _size;
            while (srcIndex < _size)
            {
                array[--dstIndex] = _array[srcIndex++];
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_Index);
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            try
            {
                Array.Copy(_array, 0, array, arrayIndex, _size);
                Array.Reverse(array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
            }
        }

        // Returns an IEnumerator for this Stack.
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void TrimExcess()
        {
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_size < threshold)
            {
//#if DESKTOPCLR
                T[] newarray = new T[_size];
                Array.Copy(_array, 0, newarray, 0, _size);
                _array = newarray;
                _version++;
//#else
//                Array.Resize(ref _array, _size);
//                _version++;
//#endif
            }
        }

        // Returns the top object on the stack without removing it.  If the stack
        // is empty, Peek throws an InvalidOperationException.
        public T Peek()
        {
#if DESKTOPCLR
            if (_size == 0) { ThrowForEmptyStack(); }
            return _array[_size - 1];
#else
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            return array[size];
#endif
        }

        public bool TryPeek(out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default;
                return false;
            }
            result = array[size];
            return true;
        }

        // Pops an item from the top of the stack.  If the stack is empty, Pop
        // throws an InvalidOperationException.
        public T Pop()
        {
#if DESKTOPCLR1
            if (_size == 0) { ThrowForEmptyStack(); }
            _version++;
            T item = _array[--_size];
            _array[_size] = default;     // Free memory quicker.
            return item;
#else
            int size = _size - 1;
            T[] array = _array;
            
            // if (_size == 0) is equivalent to if (size == -1), and this case
            // is covered with (uint)size, thus allowing bounds check elimination 
            // https://github.com/dotnet/coreclr/pull/9773
            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }
            
            _version++;
            _size = size;
            T item = array[size];
#if NETCOREAPP
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array[size] = default;     // Free memory quicker.
            }
#else
            array[size] = default;     // Free memory quicker.
#endif
            return item;
#endif
        }

        public bool TryPop(out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default;
                return false;
            }

            _version++;
            _size = size;
            result = array[size];
#if NETCOREAPP
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array[size] = default;     // Free memory quicker.
            }
#else
            array[size] = default;     // Free memory quicker.
#endif
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(T item)
        {
#if DESKTOPCLR
            if (_size == _array.Length)
            {
                T[] newArray = new T[(_array.Length == 0) ? DefaultCapacity : 2 * _array.Length];
                Array.Copy(_array, 0, newArray, 0, _size);
                _array = newArray;
            }
            _array[_size++] = item;
            _version++;
#else
            int size = _size;
            T[] array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = item;
                _version++;
                _size = size + 1;
            }
            else
            {
                PushWithResize(item);
            }
#endif
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(T item)
        {
            Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_size] = item;
            _version++;
            _size++;
        }

        // Copies the Stack to an array, in the same order Pop would return the items.
        public T[] ToArray()
        {
            if (_size == 0)
                return EmptyArray<T>.Instance; // Array.Empty<T>();

            T[] objArray = new T[_size];
            int i = 0;
            while (i < _size)
            {
                objArray[i] = _array[_size - i - 1];
                i++;
            }
            return objArray;
        }

        private void ThrowForEmptyStack()
        {
            Debug.Assert(_size == 0);
            throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);
        }

        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "not an expected scenario")]
        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private readonly StackX<T> _stack;
            private readonly int _version;
            private int _index;
            private T _currentElement;

            internal Enumerator(StackX<T> stack)
            {
                _stack = stack;
                _version = stack._version;
                _index = -2;
                _currentElement = default(T);
            }

            public void Dispose()
            {
                _index = -1;
            }

            public bool MoveNext()
            {
                bool retval;
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                if (_index == -2)
                {  // First call to enumerator.
                    _index = _stack._size - 1;
                    retval = (_index >= 0);
                    if (retval)
                        _currentElement = _stack._array[_index];
                    return retval;
                }
                if (_index == -1)
                {  // End of enumeration.
                    return false;
                }

                retval = (--_index >= 0);
                if (retval)
                    _currentElement = _stack._array[_index];
                else
                    _currentElement = default(T);
                return retval;
            }

            public T Current
            {
                get
                {
                    if (_index < 0)
                        ThrowEnumerationNotStartedOrEnded();
                    return _currentElement;
                }
            }
            
            private void ThrowEnumerationNotStartedOrEnded()
            {
                Debug.Assert(_index == -1 || _index == -2);
                throw new InvalidOperationException(_index == -2 ? SR.InvalidOperation_EnumNotStarted : SR.InvalidOperation_EnumEnded);
            }
            
            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _index = -2;
                _currentElement = default(T);
            }
        }
    }
}
