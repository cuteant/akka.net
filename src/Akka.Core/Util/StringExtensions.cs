using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CuteAnt.Text;

namespace Akka.Util
{
    public static class StringExtensions
    {
#if NET451
        public static string JoinWithPrefix(this IEnumerable<string> values, string separator, string prefix)
        {
            return Join(values, separator, prefix, null);
        }

        public static string JoinWithSuffix(this IEnumerable<string> values, string separator, string suffix)
        {
            return Join(values, separator, null, suffix);
        }

        public static string Join(this IEnumerable<string> values, string separator, string prefix, string suffix)
        {
            if (values is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.values); }

            StringBuilder result = StringBuilderCache.Acquire();
            result.Append(prefix);

            using (IEnumerator<string> en = values.GetEnumerator())
            {
                if (!en.MoveNext()) { goto FreeSb; }

                string firstValue = en.Current;

                if (!en.MoveNext())
                {
                    // Only one value available
                    result.Append(firstValue ?? string.Empty); goto FreeSb;
                }

                // Null separator and values are handled by the StringBuilder
                result.Append(firstValue);

                do
                {
                    result.Append(separator);
                    result.Append(en.Current);
                }
                while (en.MoveNext());
            }

        FreeSb:
            result.Append(suffix);
            return StringBuilderCache.GetStringAndRelease(result);
        }




        public static unsafe string JoinWithPrefix<T>(this IEnumerable<T> values, string separator, string prefix)
        {
            return Join(values, separator, prefix, null);
        }

        public static unsafe string JoinWithSuffix<T>(this IEnumerable<T> values, string separator, string suffix)
        {
            return Join(values, separator, null, suffix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Join<T>(this IEnumerable<T> values, string separator, string prefix, string suffix)
        {
            if (values is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.values); }

            StringBuilder result = StringBuilderCache.Acquire();
            result.Append(prefix);

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext()) { goto FreeSb; }

                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    result.Append(firstString ?? string.Empty); goto FreeSb;
                }

                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    result.Append(separator);
                    if (currentValue is object)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());
            }

        FreeSb:
            result.Append(suffix);
            return StringBuilderCache.GetStringAndRelease(result);
        }
#else
        public static unsafe string JoinWithPrefix(this IEnumerable<string> values, char separator, string prefix)
        {
            return Join(values, separator, prefix, null);
        }

        public static unsafe string JoinWithPrefix(this IEnumerable<string> values, string separator, string prefix)
        {
            return Join(values, separator, prefix, null);
        }

        public static unsafe string JoinWithSuffix(this IEnumerable<string> values, char separator, string suffix)
        {
            return Join(values, separator, null, suffix);
        }

        public static unsafe string JoinWithSuffix(this IEnumerable<string> values, string separator, string suffix)
        {
            return Join(values, separator, null, suffix);
        }

        public static unsafe string Join(this IEnumerable<string> values, char separator, string prefix, string suffix)
        {
            prefix = prefix ?? string.Empty;
            suffix = suffix ?? string.Empty;
            fixed (char* pPrefix = prefix)
            fixed (char* pSuffix = suffix)
                return JoinCore0(values, &separator, 1, pPrefix, prefix.Length, pSuffix, suffix.Length);
        }

        public static unsafe string Join(this IEnumerable<string> values, string separator, string prefix, string suffix)
        {
            separator = separator ?? string.Empty;
            prefix = prefix ?? string.Empty;
            suffix = suffix ?? string.Empty;
            fixed (char* pSeparator = separator)
            fixed (char* pPrefix = prefix)
            fixed (char* pSuffix = suffix)
                return JoinCore0(values, pSeparator, separator.Length, pPrefix, prefix.Length, pSuffix, suffix.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string JoinCore0(IEnumerable<string> values, char* separator, int separatorLength, char* prefix, int prefixLength, char* suffix, int suffixLength)
        {
            if (values is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.values); }

            StringBuilder result = StringBuilderCache.Acquire();
            result.Append(prefix, prefixLength);

            using (IEnumerator<string> en = values.GetEnumerator())
            {
                if (!en.MoveNext()) { goto FreeSb; }

                string firstValue = en.Current;

                if (!en.MoveNext())
                {
                    // Only one value available
                    result.Append(firstValue ?? string.Empty); goto FreeSb;
                }

                // Null separator and values are handled by the StringBuilder
                result.Append(firstValue);

                do
                {
                    result.Append(separator, separatorLength);
                    result.Append(en.Current);
                }
                while (en.MoveNext());
            }

        FreeSb:
            result.Append(suffix, suffixLength);
            return StringBuilderCache.GetStringAndRelease(result);
        }




        public static unsafe string JoinWithPrefix<T>(this IEnumerable<T> values, char separator, string prefix)
        {
            return Join(values, separator, prefix, null);
        }

        public static unsafe string JoinWithPrefix<T>(this IEnumerable<T> values, string separator, string prefix)
        {
            return Join(values, separator, prefix, null);
        }

        public static unsafe string JoinWithSuffix<T>(this IEnumerable<T> values, char separator, string suffix)
        {
            return Join(values, separator, null, suffix);
        }

        public static unsafe string JoinWithSuffix<T>(this IEnumerable<T> values, string separator, string suffix)
        {
            return Join(values, separator, null, suffix);
        }

        public static unsafe string Join<T>(this IEnumerable<T> values, char separator, string prefix, string suffix)
        {
            prefix = prefix ?? string.Empty;
            suffix = suffix ?? string.Empty;
            fixed (char* pPrefix = prefix)
            fixed (char* pSuffix = suffix)
                return JoinCore(&separator, 1, values, pPrefix, prefix.Length, pSuffix, suffix.Length);
        }

        public static unsafe string Join<T>(this IEnumerable<T> values, string separator, string prefix, string suffix)
        {
            separator = separator ?? string.Empty;
            prefix = prefix ?? string.Empty;
            suffix = suffix ?? string.Empty;
            fixed (char* pSeparator = separator)
            fixed (char* pPrefix = prefix)
            fixed (char* pSuffix = suffix)
                return JoinCore(pSeparator, separator.Length, values, pPrefix, prefix.Length, pSuffix, suffix.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string JoinCore<T>(char* separator, int separatorLength, IEnumerable<T> values, char* prefix, int prefixLength, char* suffix, int suffixLength)
        {
            if (values is null) { AkkaThrowHelper.ThrowArgumentNullException(AkkaExceptionArgument.values); }

            StringBuilder result = StringBuilderCache.Acquire();
            result.Append(prefix, prefixLength);

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext()) { goto FreeSb; }

                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    result.Append(firstString ?? string.Empty); goto FreeSb;
                }

                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    result.Append(separator, separatorLength);
                    if (currentValue is object)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());
            }

        FreeSb:
            result.Append(suffix, suffixLength);
            return StringBuilderCache.GetStringAndRelease(result);
        }
#endif
    }
}
