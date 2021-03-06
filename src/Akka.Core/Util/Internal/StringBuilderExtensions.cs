﻿//-----------------------------------------------------------------------
// <copyright file="StringBuilderExtensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.Util.Internal
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> values)
        {
            return AppendJoin(sb, separator, values, null);
        }

        public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> values, Action<StringBuilder, T, int> valueAppender)
        {
            if (values is null) return sb;
            if (separator is null) separator = "";
            if (valueAppender is null) valueAppender = DefaultAppendValue;

            using (var enumerator = values.GetEnumerator())
            {
                var index = 0;
                if (!enumerator.MoveNext())
                    return sb;

                // ReSharper disable CompareNonConstrainedGenericWithNull
                var current = enumerator.Current;
                if (current is object)
                // ReSharper restore CompareNonConstrainedGenericWithNull
                {
                    valueAppender(sb, current, index);
                }

                while (enumerator.MoveNext())
                {
                    index++;
                    sb.Append(separator);
                    // ReSharper disable CompareNonConstrainedGenericWithNull
                    current = enumerator.Current;
                    if (current is object)
                    // ReSharper restore CompareNonConstrainedGenericWithNull
                    {
                        valueAppender(sb, current, index);
                    }
                }
            }
            return sb;
        }

        private static void DefaultAppendValue<T>(StringBuilder sb, T value, int index)
        {
            var s = value.ToString();
            if (s is object)
                sb.Append(value);
        }
    }
}

