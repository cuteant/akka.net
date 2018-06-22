﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET40
using System.Linq;
#endif

namespace Akka.Util
{
    /// <summary>A double-ended queue (deque), which provides O(1) indexed access, O(1) removals from the front and back, 
    /// amortized O(1) insertions to the front and back, and O(N) insertions and removals anywhere else 
    /// (with the operations getting slower as the index approaches the middle).</summary>
    /// <typeparam name="T">The type of elements contained in the deque.</typeparam>
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
    [DebuggerTypeProxy(typeof(Deque<>.DebugView))]
    public class Deque<T> : IList<T>, IReadOnlyList<T>, System.Collections.IList
    {
        /// <summary>The default capacity.</summary>
        private const int DefaultCapacity = 8;

        /// <summary>The circular _buffer that holds the view.</summary>
        private T[] _buffer;

        /// <summary>The offset into <see cref="_buffer"/> where the view begins.</summary>
        private int _offset;

        /// <summary>Initializes a new instance of the <see cref="Deque&lt;T&gt;"/> class with the specified capacity.</summary>
        /// <param name="capacity">The initial capacity. Must be greater than <c>0</c>.</param>
        public Deque(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity may not be negative.");
            }
            _buffer = new T[capacity];
        }

        /// <summary>Initializes a new instance of the <see cref="Deque&lt;T&gt;"/> class with the elements from the specified collection.</summary>
        /// <param name="collection">The collection. May not be <c>null</c>.</param>
        public Deque(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            var source = CollectionHelpers.ReifyCollection(collection);
            var count = source.Count;
            if (count > 0)
            {
                _buffer = new T[count];
                DoInsertRange(0, source);
            }
            else
            {
                _buffer = new T[DefaultCapacity];
            }
        }

        /// <summary>Initializes a new instance of the <see cref="Deque&lt;T&gt;"/> class.</summary>
        public Deque()
          : this(DefaultCapacity)
        {
        }

        #region GenericListImplementations

        /// <summary>Gets a value indicating whether this list is read-only. This implementation always returns <c>false</c>.</summary>
        /// <returns>true if this list is read-only; otherwise, false.</returns>
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>Gets or sets the item at the specified index.</summary>
        /// <param name="index">The index of the item to get or set.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in this list.</exception>
        /// <exception cref="T:System.NotSupportedException">This property is set and the list is read-only.</exception>
        public T this[int index]
        {
            get
            {
                CheckExistingIndexArgument(_count, index);
                return DoGetItem(index);
            }

            set
            {
                CheckExistingIndexArgument(_count, index);
                DoSetItem(index, value);
            }
        }

        /// <summary>Inserts an item to this list at the specified index.</summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into this list.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is not a valid index in this list.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        public void Insert(int index, T item)
        {
            CheckNewIndexArgument(_count, index);
            DoInsert(index, item);
        }

        /// <summary>Removes the item at the specified index.</summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is not a valid index in this list.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        public void RemoveAt(int index)
        {
            CheckExistingIndexArgument(_count, index);
            DoRemoveAt(index);
        }

        /// <summary>Determines the index of a specific item in this list.</summary>
        /// <param name="item">The object to locate in this list.</param>
        /// <returns>The index of <paramref name="item"/> if found in this list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            int ret = 0;
            foreach (var sourceItem in this)
            {
                if (comparer.Equals(item, sourceItem)) { return ret; }
                ++ret;
            }

            return -1;
        }

        /// <summary>Adds an item to the end of this list.</summary>
        /// <param name="item">The object to add to this list.</param>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        void ICollection<T>.Add(T item)
        {
            DoInsert(_count, item);
        }

        /// <summary>Determines whether this list contains a specific value.</summary>
        /// <param name="item">The object to locate in this list.</param>
        /// <returns>true if <paramref name="item"/> is found in this list; otherwise, false.</returns>
        bool ICollection<T>.Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var entry in this)
            {
                if (comparer.Equals(item, entry)) { return true; }
            }
            return false;
        }

        /// <summary>Copies the elements of this list to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from this slice. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        /// -or-
        /// The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
        public void /*ICollection<T>.*/CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            CheckRangeArguments(array.Length, arrayIndex, _count);
            CopyToArray(array, arrayIndex);
        }

        /// <summary>Copies the deque elemens into an array. The resulting array always has all the deque elements contiguously.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The optional index in the destination array at which to begin writing.</param>
        private void CopyToArray(Array array, int arrayIndex = 0)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (IsSplit)
            {
                // The existing buffer is split, so we have to copy it in parts
                int length = Capacity - _offset;
                Array.Copy(_buffer, _offset, array, arrayIndex, length);
                Array.Copy(_buffer, 0, array, arrayIndex + length, _count - length);
            }
            else
            {
                // The existing buffer is whole
                Array.Copy(_buffer, _offset, array, arrayIndex, _count);
            }
        }

        /// <summary>Removes the first occurrence of a specific object from this list.</summary>
        /// <param name="item">The object to remove from this list.</param>
        /// <returns>true if <paramref name="item"/> was successfully removed from this list; otherwise, false. This method also returns false if <paramref name="item"/> is not found in this list.</returns>
        /// <exception cref="T:System.NotSupportedException">
        /// This list is read-only.
        /// </exception>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1) { return false; }

            DoRemoveAt(index);
            return true;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            int count = _count;
            for (int i = 0; i != count; ++i)
            {
                yield return DoGetItem(i);
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ObjectListImplementations

        private static bool IsT(object value)
        {
            if (value is T) { return true; }
            if (value != null) { return false; }
            return default(T) == null;
        }

        int System.Collections.IList.Add(object value)
        {
            if (value == null && default(T) != null) throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            if (!IsT(value)) throw new ArgumentException("Value is of incorrect type.", nameof(value));

            AddToBack((T)value);
            return _count - 1;
        }

        bool System.Collections.IList.Contains(object value)
        {
            return IsT(value) ? ((ICollection<T>)this).Contains((T)value) : false;
        }

        int System.Collections.IList.IndexOf(object value)
        {
            return IsT(value) ? IndexOf((T)value) : -1;
        }

        void System.Collections.IList.Insert(int index, object value)
        {
            if (value == null && default(T) != null) { throw new ArgumentNullException("value", "Value cannot be null."); }
            if (!IsT(value)) { throw new ArgumentException("Value is of incorrect type.", "value"); }
            Insert(index, (T)value);
        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly
        {
            get { return false; }
        }

        void System.Collections.IList.Remove(object value)
        {
            if (IsT(value)) { Remove((T)value); }
        }

        object System.Collections.IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                if (value == null && default(T) != null) throw new ArgumentNullException(nameof(value), "Value cannot be null.");
                if (!IsT(value)) throw new ArgumentException("Value is of incorrect type.", nameof(value));
                this[index] = (T)value;
            }
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array), "Destination array cannot be null.");
            CheckRangeArguments(array.Length, index, _count);

            try
            {
                CopyToArray(array, index);
            }
            catch (ArrayTypeMismatchException ex)
            {
                throw new ArgumentException("Destination array is of incorrect type.", nameof(array), ex);
            }
            catch (RankException ex)
            {
                throw new ArgumentException("Destination array must be single dimensional.", nameof(array), ex);
            }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region GenericListHelpers

        /// <summary>Checks the <paramref name="index"/> argument to see if it refers to a valid insertion point in a source of a given length.</summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index to an insertion point for the source.</exception>
        private static void CheckNewIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index > sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid new index " + index + " for source length " + sourceLength);
            }
        }

        /// <summary>Checks the <paramref name="index"/> argument to see if it refers to an existing element in a source of a given length.</summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index to an existing element for the source.</exception>
        private static void CheckExistingIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index >= sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid existing index " + index + " for source length " + sourceLength);
            }
        }

        /// <summary>Checks the <paramref name="offset"/> and <paramref name="count"/> arguments for validity when applied to a source of a given length. Allows 0-element ranges, including a 0-element range at the end of the source.</summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="offset">The index into source at which the range begins.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <exception cref="ArgumentOutOfRangeException">Either <paramref name="offset"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The range [offset, offset + count) is not within the range [0, sourceLength).</exception>
        private static void CheckRangeArguments(int sourceLength, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset " + offset);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Invalid count " + count);
            }

            if (sourceLength - offset < count)
            {
                throw new ArgumentException("Invalid offset (" + offset + ") or count + (" + count + ") for source length " + sourceLength);
            }
        }

        #endregion

        /// <summary>Gets a value indicating whether this instance is empty.</summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count == 0; }
        }

        public bool NonEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count > 0; }
        }

        /// <summary>Gets a value indicating whether this instance is at full capacity.</summary>
        public bool IsFull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count == Capacity; }
        }

        /// <summary>Gets a value indicating whether the buffer is "split" (meaning the beginning of the view is at a later index in <see cref="_buffer"/> than the end).</summary>
        internal bool IsSplit
        {
            // Overflow-safe version of "(offset + Count) > Capacity"
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _offset > (Capacity - _count); }
        }

        /// <summary>Gets or sets the capacity for this deque. This value must always be greater than zero, and this property cannot be set to a value less than <see cref="Count"/>.</summary>
        /// <exception cref="InvalidOperationException"><c>Capacity</c> cannot be set to a value less than <see cref="Count"/>.</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _buffer.Length; }
            set
            {
                if (value < _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be set to a value less than Count");
                }

                if (value == _buffer.Length) { return; }

                // Create the new _buffer and copy our existing range.
                T[] newBuffer = new T[value];
                CopyToArray(newBuffer);

                // Set up to use the new _buffer.
                _buffer = newBuffer;
                _offset = 0;
            }
        }

        private int _count = 0;
        /// <summary>Gets the number of elements contained in this deque.</summary>
        /// <returns>The number of elements contained in this deque.</returns>
        public int Count => _count;

        /// <summary>Applies the offset to <paramref name="index"/>, resulting in a buffer index.</summary>
        /// <param name="index">The deque index.</param>
        /// <returns>The buffer index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DequeIndexToBufferIndex(int index)
        {
            return (index + _offset) % Capacity;
        }

        /// <summary>Gets an element at the specified view index.</summary>
        /// <param name="index">The zero-based view index of the element to get. This index is guaranteed to be valid.</param>
        /// <returns>The element at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T DoGetItem(int index)
        {
            return _buffer[DequeIndexToBufferIndex(index)];
        }

        /// <summary>Sets an element at the specified view index.</summary>
        /// <param name="index">The zero-based view index of the element to get. This index is guaranteed to be valid.</param>
        /// <param name="item">The element to store in the list.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetItem(int index, T item)
        {
            _buffer[DequeIndexToBufferIndex(index)] = item;
        }

        /// <summary>Inserts an element at the specified view index.</summary>
        /// <param name="index">The zero-based view index at which the element should be inserted. This index is guaranteed to be valid.</param>
        /// <param name="item">The element to store in the list.</param>
        private void DoInsert(int index, T item)
        {
            EnsureCapacityForOneElement();

            if (index == 0)
            {
                DoAddToFront(item);
                return;
            }
            else if (index == _count)
            {
                DoAddToBack(item);
                return;
            }

            DoInsertRange(index, new[] { item });
        }

        /// <summary>Removes an element at the specified view index.</summary>
        /// <param name="index">The zero-based view index of the element to remove. This index is guaranteed to be valid.</param>
        private void DoRemoveAt(int index)
        {
            if (index == 0)
            {
                DoRemoveFromFront();
                return;
            }
            else if (index == _count - 1)
            {
                DoRemoveFromBack();
                return;
            }

            DoRemoveRange(index, 1);
        }

        /// <summary>Increments <see cref="_offset"/> by <paramref name="value"/> using modulo-<see cref="Capacity"/> arithmetic.</summary>
        /// <param name="value">The value by which to increase <see cref="_offset"/>. May not be negative.</param>
        /// <returns>The value of <see cref="_offset"/> after it was incremented.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PostIncrement(int value)
        {
            _offset = (_offset + value) % Capacity;
        }

        /// <summary>Decrements <see cref="_offset"/> by <paramref name="value"/> using modulo-<see cref="Capacity"/> arithmetic.</summary>
        /// <param name="value">The value by which to reduce <see cref="_offset"/>. May not be negative or greater than <see cref="Capacity"/>.</param>
        /// <returns>The value of <see cref="_offset"/> before it was decremented.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PreDecrement(int value)
        {
            _offset -= value;
            if (_offset < 0) { _offset += Capacity; }
            return _offset;
        }

        /// <summary>Inserts a single element to the back of the view. <see cref="IsFull"/> must be false when this method is called.</summary>
        /// <param name="value">The element to insert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoAddToBack(T value)
        {
            _buffer[DequeIndexToBufferIndex(_count++)] = value;
        }

        /// <summary>Inserts a single element to the front of the view. <see cref="IsFull"/> must be false when this method is called.</summary>
        /// <param name="value">The element to insert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoAddToFront(T value)
        {
            _buffer[PreDecrement(1)] = value;
            ++_count;
        }

        /// <summary>Removes and returns the last element in the view. <see cref="IsEmpty"/> must be false when this method is called.</summary>
        /// <returns>The former last element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T DoRemoveFromBack()
        {
            var index = DequeIndexToBufferIndex(--_count);
            T ret = _buffer[index];
#if NETCOREAPP
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _buffer[index] = default;
            }
#else
            _buffer[index] = default;
#endif
            return ret;
        }

        /// <summary>Removes and returns the first element in the view. <see cref="IsEmpty"/> must be false when this method is called.</summary>
        /// <returns>The former first element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T DoRemoveFromFront()
        {
            --_count;
            var index = _offset; PostIncrement(1);
            var ret = _buffer[index];
#if NETCOREAPP
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _buffer[index] = default;
            }
#else
            _buffer[index] = default;
#endif
            return ret;
        }

        /// <summary>Inserts a range of elements into the view.</summary>
        /// <param name="index">The index into the view at which the elements are to be inserted.</param>
        /// <param name="collection">The elements to insert. The sum of <c>collection.Count</c> and <see cref="Count"/> must be less than or equal to <see cref="Capacity"/>.</param>
#if !NET40
        private void DoInsertRange(int index, IReadOnlyCollection<T> collection)
        {
            var collectionCount = collection.Count;
#else
        private void DoInsertRange(int index, IEnumerable<T> collection)
        {
            var collectionCount = collection.Count();
#endif
            // Make room in the existing list
            if (index < _count / 2)
            {
                // Inserting into the first half of the list

                // Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
                // This clears out the low "index" number of items, moving them "collectionCount" places down;
                //   after rotation, there will be a "collectionCount"-sized hole at "index".
                int copyCount = index;
                int writeIndex = Capacity - collectionCount;
                for (int j = 0; j != copyCount; ++j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];
                }

                // Rotate to the new view
                PreDecrement(collectionCount);
            }
            else
            {
                // Inserting into the second half of the list

                // Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
                int copyCount = _count - index;
                int writeIndex = index + collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(index + j)];
                }
            }

            // Copy new items into place
            int i = index;
            foreach (T item in collection)
            {
                _buffer[DequeIndexToBufferIndex(i)] = item;
                ++i;
            }

            // Adjust valid count
            _count += collectionCount;
        }

        /// <summary>Removes a range of elements from the view.</summary>
        /// <param name="index">The index into the view at which the range begins.</param>
        /// <param name="collectionCount">The number of elements in the range. This must be greater than 0 and less than or equal to <see cref="Count"/>.</param>
        private void DoRemoveRange(int index, int collectionCount)
        {
            if (index == 0)
            {
#if NETCOREAPP
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
#endif
                    var maxIndex = _offset + collectionCount;
                    for (var idx = _offset; idx < maxIndex; idx++)
                    {
                        _buffer[idx % Capacity] = default;
                    }
#if NETCOREAPP
                }
#endif
                // Removing from the beginning: rotate to the new view
                PostIncrement(collectionCount);
                _count -= collectionCount;
                return;
            }
            else if (index == _count - collectionCount)
            {
#if NETCOREAPP
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
#endif
                    var maxIndex = index + collectionCount;
                    for (var idx = index; idx < maxIndex; idx++)
                    {
                        _buffer[DequeIndexToBufferIndex(index)] = default;
                    }
#if NETCOREAPP
                }
#endif
                // Removing from the ending: trim the existing view
                _count -= collectionCount;
                return;
            }

            if ((index + (collectionCount / 2)) < _count / 2)
            {
                // Removing from first half of list

                // Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
                int copyCount = index;
                int writeIndex = collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                {
                    var idx = DequeIndexToBufferIndex(j);
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[idx];
#if NETCOREAPP
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        _buffer[idx] = default;
                    }
#else
                    _buffer[idx] = default;
#endif
                }

                // Rotate to new view
                PostIncrement(collectionCount);
            }
            else
            {
                // Removing from second half of list

                // Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
                int copyCount = _count - collectionCount - index;
                int readIndex = index + collectionCount;
                for (int j = 0; j != copyCount; ++j)
                {
                    var idx = DequeIndexToBufferIndex(readIndex + j);
                    _buffer[DequeIndexToBufferIndex(index + j)] = _buffer[idx];
#if NETCOREAPP
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        _buffer[idx] = default;
                    }
#else
                    _buffer[idx] = default;
#endif
                }
            }

            // Adjust valid count
            _count -= collectionCount;
        }

        /// <summary>Doubles the capacity if necessary to make room for one more element. When this method returns, <see cref="IsFull"/> is false.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacityForOneElement()
        {
            var capacity = _buffer.Length; // Capacity
            if (_count == capacity) // IsFull
            {
                Capacity = (capacity == 0) ? 1 : capacity * 2;
            }
        }

        /// <summary>Inserts a single element at the back of this deque.</summary>
        /// <param name="value">The element to insert.</param>
        public void AddToBack(T value)
        {
            EnsureCapacityForOneElement();
            DoAddToBack(value);
        }

        /// <summary>Inserts a single element at the front of this deque.</summary>
        /// <param name="value">The element to insert.</param>
        public void AddToFront(T value)
        {
            EnsureCapacityForOneElement();
            DoAddToFront(value);
        }

        /// <summary>Inserts a collection of elements into this deque.</summary>
        /// <param name="index">The index at which the collection is inserted.</param>
        /// <param name="collection">The collection of elements to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index to an insertion point for the source.</exception>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            CheckNewIndexArgument(_count, index);
            var source = CollectionHelpers.ReifyCollection(collection);
            int collectionCount = source.Count;

            // Overflow-safe check for "Count + collectionCount > Capacity"
            if (collectionCount > Capacity - _count)
            {
                Capacity = checked(_count + collectionCount);
            }

            if (collectionCount == 0) { return; }

            DoInsertRange(index, source);
        }

        /// <summary>Removes a range of elements from this deque.</summary>
        /// <param name="offset">The index into the deque at which the range begins.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Either <paramref name="offset"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The range [<paramref name="offset"/>, <paramref name="offset"/> + <paramref name="count"/>) is not within the range [0, <see cref="Count"/>).</exception>
        public void RemoveRange(int offset, int count)
        {
            if (count == 0) { return; }

            CheckRangeArguments(_count, offset, count);

            DoRemoveRange(offset, count);
        }

        public int RemoveAll(Predicate<T> match)
        {
            if (null == match) { throw new ArgumentNullException(nameof(match)); }

            if (IsEmpty) { return 0; }

            int freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _count && !match(_buffer[DequeIndexToBufferIndex(freeIndex)])) freeIndex++;
            if (freeIndex >= _count) return 0;

            int current = freeIndex + 1;
            while (current < _count)
            {
                // Find the first item which needs to be kept.
                while (current < _count && match(_buffer[DequeIndexToBufferIndex(current)])) current++;

                if (current < _count)
                {
                    var idx = DequeIndexToBufferIndex(current++);
                    // copy item to the free slot.
                    _buffer[DequeIndexToBufferIndex(freeIndex++)] = _buffer[idx];
#if NETCOREAPP
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        _buffer[idx] = default;
                    }
#else
                    _buffer[idx] = default;
#endif
                }
            }

            int result = _count - freeIndex;
            _count = freeIndex;
            return result;
        }

        public void ForEach(Action<T> action)
        {
            if (null == action) { throw new ArgumentNullException(nameof(action)); }
            if (IsEmpty) { return; }

            var idx = 0;
            while (idx < _count)
            {
                action(DoGetItem(idx));
                idx++;
            }
        }

        public void Reverse(Action<T> action)
        {
            if (null == action) { throw new ArgumentNullException(nameof(action)); }
            if (IsEmpty) { return; }

            var idx = _count - 1;
            while (idx >= 0)
            {
                action(DoGetItem(idx));
                idx--;
            }
        }

        public T PeekFromBack()
        {
            if (IsEmpty) { throw new InvalidOperationException("The deque is empty."); }

            return _buffer[DequeIndexToBufferIndex(_count - 1)];
        }

        public bool TryPeekFromBack(out T result)
        {
            if (IsEmpty) { result = default; return false; }

            result = _buffer[DequeIndexToBufferIndex(_count - 1)];
            return true;
        }

        /// <summary>Removes and returns the last element of this deque.</summary>
        /// <returns>The former last element.</returns>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        public T RemoveFromBack()
        {
            if (IsEmpty) { throw new InvalidOperationException("The deque is empty."); }

            return DoRemoveFromBack();
        }

        /// <summary>Removes and returns the last element of this deque.</summary>
        /// <param name="result">The former last element.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryRemoveFromBack(out T result)
        {
            if (IsEmpty) { result = default; return false; }

            result = DoRemoveFromBack();
            return true;
        }

        /// <summary>Removes and returns the last element of this deque.</summary>
        /// <param name="predicate">The predicate that must return true for the item to be dequeued.  If null, all items implicitly return true.</param>
        /// <param name="result">The former last element.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryRemoveFromBackIf(Predicate<T> predicate, out T result)
        {
            if (IsEmpty) { result = default; return false; }

            var size = _count - 1;
            var index = DequeIndexToBufferIndex(size);
            var item = _buffer[index];
            if (predicate(item))
            {
                result = item;
#if NETCOREAPP
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    _buffer[index] = default;
                }
#else
                _buffer[index] = default;
#endif
                _count = size;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public bool TryRemoveFromBackUntil(Predicate<T> predicate, out T result)
        {
            if (IsEmpty) { result = default; return false; }

            var found = false;
            T item = default;
            var idx = _count - 1;
            while (idx >= 0)
            {
                item = DoGetItem(idx);
                if (predicate(item))
                {
                    found = true;
                    break;
                }
                idx--;
            }

            if (found) { DoRemoveAt(idx); }

            result = item;
            return found;
        }

        public T PeekFromFront()
        {
            if (IsEmpty) { throw new InvalidOperationException("The deque is empty."); }
            return _buffer[_offset];
        }

        public bool TryPeekFromFront(out T result)
        {
            if (IsEmpty) { result = default; return false; }

            result = _buffer[_offset];
            return true;
        }

        /// <summary>Removes and returns the first element of this deque.</summary>
        /// <returns>The former first element.</returns>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        public T RemoveFromFront()
        {
            if (IsEmpty) { throw new InvalidOperationException("The deque is empty."); }

            return DoRemoveFromFront();
        }

        /// <summary>Removes and returns the first element of this deque.</summary>
        /// <param name="result">The former first element.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryRemoveFromFront(out T result)
        {
            if (IsEmpty) { result = default; return false; }

            result = DoRemoveFromFront();
            return true;
        }

        /// <summary>Removes and returns the first element of this deque.</summary>
        /// <param name="predicate">The predicate that must return true for the item to be dequeued.  If null, all items implicitly return true.</param>
        /// <param name="result">The former first element.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryRemoveFromFrontIf(Predicate<T> predicate, out T result)
        {
            if (IsEmpty) { result = default; return false; }

            var item = _buffer[_offset];
            if (predicate(item))
            {
#if NETCOREAPP
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    _buffer[_offset] = default;
                }
#else
                _buffer[_offset] = default;
#endif
                --_count;
                result = item;
                _offset += 1;
                _offset %= Capacity;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public bool TryRemoveFromFrontUntil(Predicate<T> predicate, out T result)
        {
            if (IsEmpty) { result = default; return false; }

            var found = false;
            T item = default;
            var idx = 0;
            while (idx < _count)
            {
                item = DoGetItem(idx);
                if (predicate(item))
                {
                    found = true;
                    break;
                }
                idx++;
            }

            if (found) { DoRemoveAt(idx); }

            result = item;
            return found;
        }

        /// <summary>Removes all items from this deque.</summary>
        public void Clear()
        {
            if (NonEmpty)
            {
#if NETCOREAPP
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
#endif
                    if (IsSplit)
                    {
                        // The existing buffer is split, so we have to copy it in parts
                        int length = Capacity - _offset;
                        Array.Clear(_buffer, _offset, length);
                        Array.Clear(_buffer, 0, _count - length);
                    }
                    else
                    {
                        // The existing buffer is whole
                        Array.Clear(_buffer, _offset, _count);
                    }
#if NETCOREAPP
                }
#endif
            }
            _offset = 0;
            _count = 0;
        }

        /// <summary>Creates and returns a new array containing the elements in this deque.</summary>
        public T[] ToArray()
        {
            var result = new T[_count];
            CopyTo(result, 0);
            return result;
        }

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly Deque<T> deque;

            public DebugView(Deque<T> deque)
            {
                this.deque = deque;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get { return deque.ToArray(); }
            }
        }
    }
}
