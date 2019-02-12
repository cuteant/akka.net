using System;
using System.Threading.Tasks;
using Akka.Tools.MatchHandler;
using Akka.Util.Internal;

namespace Akka
{
    /// <summary>TBD</summary>
    public sealed class PatternMatchBuilder : SimpleMatchBuilder<object> { }

    /// <summary>TBD</summary>
    /// <typeparam name="T"></typeparam>
    public static class PatternMatch<T>
    {
        /// <summary>TBD</summary>
        public static readonly Action<T> EmptyAction = Empty;

        /// <summary>TBD</summary>
        public static readonly Func<T, Task> AsyncEmptyAction = EmptyAsync;

        private static void Empty(T msg) { }

        private static Task EmptyAsync(T msg) => TaskEx.CompletedTask;
    }
}
