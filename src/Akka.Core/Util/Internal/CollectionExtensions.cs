using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CuteAnt;

namespace Akka.Util.Internal
{
    /// <summary>集合扩展</summary>
    public static class CollectionExtensions
    {
        #region -- ConvertAll --

        /// <summary>将当前集合中的元素转换为另一种类型，并返回包含转换后的元素的集合。</summary>
        /// <typeparam name="T">集合中元素类型</typeparam>
        /// <typeparam name="TResult">目标集合元素的类型</typeparam>
        /// <param name="items">原集合</param>
        /// <param name="transformation">将每个元素从一种类型转换为另一种类型的委托</param>
        /// <returns></returns>
        public static IReadOnlyList<TResult> ConvertAll<T, TResult>(this IEnumerable<T> items, Converter<T, TResult> transformation)
        {
            return items switch
            {
                null => EmptyArray<TResult>.Instance,

                T[] arr => arr.NonEmpty() ? Array.ConvertAll(arr, transformation) : EmptyArray<TResult>.Instance,

                List<T> list => list.NonEmpty() ? list.ConvertAll(transformation) : (IReadOnlyList<TResult>)EmptyArray<TResult>.Instance,

                _ => items.Select(_ => transformation(_)).ToList(),
            };
        }

        #endregion

        #region -- ToReadOnlyList --

        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> @this)
        {
            if (@this is IReadOnlyList<T> list) { return list; }
            return @this.ToList();
        }

        #endregion

        #region -- IsNullOrEmpty --

        /// <summary>Checks whether or not collection is null or empty. Assumes colleciton can be safely enumerated multiple times.</summary>
        /// <param name = "this"></param>
        /// <returns></returns>
        /// <remarks>
        /// Code taken from Castle Project's Castle.Core Library
        /// &lt;a href="http://www.castleproject.org/"&gt;Castle Project's Castle.Core Library&lt;/a&gt;
        /// </remarks>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsNullOrEmpty(this IEnumerable @this)
        {
            return @this is null || false == @this.GetEnumerator().MoveNext();
        }

        /// <summary>Checks whatever given collection object is null or has no item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsNullOrEmptyX(this ICollection source)
        {
            return source is null || 0u >= (uint)source.Count;
        }

        /// <summary>Checks whether or not collection is null or empty. Assumes colleciton can be safely enumerated multiple times.</summary>
        /// <param name = "this"></param>
        /// <returns></returns>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> @this)
        {
            return @this is null || @this.IsEmpty();
        }

        /// <summary>Checks whatever given collection object is null or has no item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsNullOrEmpty<T>(this ICollection<T> source)
        {
            return source is null || 0u >= (uint)source.Count;
        }

        /// <summary>Checks whatever given collection object is null or has no item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsNullOrEmptyR<T>(this IReadOnlyCollection<T> source)
        {
            return source is null || 0u >= (uint)source.Count;
        }

        #endregion

        #region -- IsEmpty --

        /// <summary>Checks whatever given collection object has no item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsEmptyX(this ICollection source)
        {
            return 0u >= (uint)source.Count;
        }

        /// <summary>Checks whether or not collection is empty. Assumes colleciton can be safely enumerated multiple times.</summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return source switch
            {
                ICollection<T> col => 0u >= (uint)col.Count,
                IReadOnlyCollection<T> col => 0u >= (uint)col.Count,
                _ => false == source.Any() ? true : false,
            };
        }

        /// <summary>Checks whatever given collection object has no item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsEmpty<T>(this ICollection<T> source)
        {
            return 0u >= (uint)source.Count;
        }

        /// <summary>Checks whatever given collection object has no item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool IsEmptyR<T>(this IReadOnlyCollection<T> source)
        {
            return 0u >= (uint)source.Count;
        }

        #endregion

        #region -- NonEmpty --

        /// <summary>Checks whatever given collection object has item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool NonEmptyX(this ICollection source)
        {
            return 0u < (uint)source.Count;
        }

        /// <summary>Checks whatever given collection object has item.</summary>
        public static bool NonEmpty<T>(this IEnumerable<T> source)
        {
            return source switch
            {
                ICollection<T> col => 0u < (uint)col.Count,
                IReadOnlyCollection<T> col => 0u < (uint)col.Count,
                _ => source.Any(),
            };
        }

        /// <summary>Checks whatever given collection object has item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool NonEmpty<T>(this ICollection<T> source)
        {
            return 0u < (uint)source.Count;
        }

        /// <summary>Checks whatever given collection object has item.</summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static bool NonEmptyR<T>(this IReadOnlyCollection<T> source)
        {
            return 0u < (uint)source.Count;
        }

        #endregion

        /// <summary>Adds an item to the collection if it's not already in the collection.</summary>
        /// <param name="source">Collection</param>
        /// <param name="item">Item to check and add</param>
        /// <typeparam name="T">Type of the items in the collection</typeparam>
        /// <returns>Returns True if added, returns False if not.</returns>
        public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
        {
            if (source is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.source); }

            if (source is ISet<T> sourceSet) { return sourceSet.Add(item); }

            if (source.Contains(item)) { return false; }

            source.Add(item);
            return true;
        }

        /// <summary>Generates a HashCode for the contents for the list. Order of items does not matter.</summary>
        /// <typeparam name="T">The type of object contained within the list.</typeparam>
        /// <param name="list">The list.</param>
        /// <returns>The generated HashCode.</returns>
        public static int GetContentsHashCode<T>(IList<T> list)
        {
            if (list is null) { return 0; }

            var result = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] is object)
                {
                    // simply add since order does not matter
                    result += list[i].GetHashCode();
                }
            }

            return result;
        }

        /// <summary>Determines if two lists are equivalent. Equivalent lists have the same number of items and each item is found 
        /// within the other regardless of respective position within each.</summary>
        /// <typeparam name="T">The type of object contained within the list.</typeparam>
        /// <param name="listA">The first list.</param>
        /// <param name="listB">The second list.</param>
        /// <param name="comparer"></param>
        /// <returns><c>True</c> if the two lists are equivalent.</returns>
        public static bool AreEquivalent<T>(IList<T> listA, IList<T> listB, IEqualityComparer<T> comparer = null)
        {
            if (listA is null && listB is null) { return true; }

            if (listA is null || listB is null) { return false; }

            if (listA.Count != listB.Count) { return false; }

            if (comparer is null) { comparer = EqualityComparer<T>.Default; }

            // copy contents to another list so that contents can be removed as they are found,
            // in order to consider duplicates
            var listBAvailableContents = listB.ToList();

            // order is not important, just make sure that each entry in A is also found in B
            for (var i = 0; i < listA.Count; i++)
            {
                var found = false;

                for (var j = 0; j < listBAvailableContents.Count; j++)
                {
                    if (comparer.Equals(listA[i], listBAvailableContents[j]))
                    {
                        found = true;
                        listBAvailableContents.RemoveAt(j);
                        break;
                    }
                }

                if (!found) { return false; }
            }

            return true;
        }
    }
}
