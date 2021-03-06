﻿// //-----------------------------------------------------------------------
// // <copyright file="ObjectExtensions.cs" company="Akka.NET Project">
// //     Copyright (C) 2009-2019 Lightbend Inc. <http://www.lightbend.com>
// //     Copyright (C) 2013-2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// // </copyright>
// //-----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Akka.Util.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Wraps object to the <see cref="Option{T}"/> monade
        /// </summary>
        [MethodImpl(InlineOptions.AggressiveOptimization)]
        public static Option<T> AsOption<T>(this T obj) => new Option<T>(obj);
    }
}