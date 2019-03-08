﻿//-----------------------------------------------------------------------
// <copyright file="MultiValueDictionaryExtensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Akka.Actor;

namespace Akka.Persistence.Wings.Journal
{
    /// <summary>
    /// TBD
    /// </summary>
    internal static class MultiValueDictionaryExtensions
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="TKey">TBD</typeparam>
        /// <param name="dictionary">TBD</param>
        /// <param name="key">TBD</param>
        /// <param name="item">TBD</param>
        /// <returns>TBD</returns>
        public static void AddItem<TKey>(this Dictionary<TKey, HashSet<IActorRef>> dictionary, TKey key, IActorRef item)
        {
            if (!dictionary.TryGetValue(key, out var bucket))
            {
                bucket = new HashSet<IActorRef>(ActorRefComparer.Instance);
                dictionary.Add(key, bucket);
            }

            bucket.Add(item);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="TKey">TBD</typeparam>
        /// <typeparam name="TVal">TBD</typeparam>
        /// <param name="dictionary">TBD</param>
        /// <param name="key">TBD</param>
        /// <param name="item">TBD</param>
        /// <returns>TBD</returns>
        public static void RemoveItem<TKey, TVal>(this Dictionary<TKey, HashSet<TVal>> dictionary, TKey key, TVal item)
        {
            if (dictionary.TryGetValue(key, out var bucket))
                if (bucket.Remove(item) && bucket.Count == 0)
                    dictionary.Remove(key);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="TKey">TBD</typeparam>
        /// <typeparam name="TVal">TBD</typeparam>
        /// <param name="dictionary">TBD</param>
        /// <param name="item">TBD</param>
        /// <returns>TBD</returns>
        public static void RemoveItem<TKey, TVal>(this Dictionary<TKey, HashSet<TVal>> dictionary, TVal item)
        {
            foreach (var entry in dictionary)
            {
                entry.Value.Remove(item);
            }
        }
    }
}
