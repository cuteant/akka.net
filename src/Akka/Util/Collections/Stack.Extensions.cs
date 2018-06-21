using System;
using System.Runtime.CompilerServices;

namespace Akka.Util
{
    partial class StackX<T>
    {
        public bool IsEmpty => _size <= 0;
        public bool IsNotEmpty => _size > 0;

        public bool TryPopIf(Predicate<T> predicate, out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default;
                return false;
            }

            result = array[size];
            if (!predicate(result)) { return false; }

            _version++;
            _size = size;
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

        static class SR
        {
            internal const string InvalidOperation_EmptyStack = "Stack empty.";

            internal const string InvalidOperation_EnumFailedVersion = "Collection was modified; enumeration operation may not execute.";

            internal const string InvalidOperation_EnumNotStarted = "Enumeration has not started. Call MoveNext.";

            internal const string InvalidOperation_EnumEnded = "Enumeration already finished.";

            internal const string Argument_InvalidArrayType = "Target array type is not compatible with the type of items in the collection.";

            internal const string Argument_InvalidOffLen = "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.";

            internal const string ArgumentOutOfRange_Index = "Index was out of range. Must be non-negative and less than the size of the collection.";

            internal const string Arg_NonZeroLowerBound = "The lower bound of target array must be zero.";

            internal const string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";

            internal const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
        }
    }
}
