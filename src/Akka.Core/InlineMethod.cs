using System.Runtime.CompilerServices;

namespace Akka
{
    /// <summary>Helper class for constants for inlining methods</summary>
    public static class InlineOptions
    {
        public const MethodImplOptions AggressiveInlining = MethodImplOptions.AggressiveInlining;

        /// <summary>Value for lining method</summary>
        public const MethodImplOptions AggressiveOptimization =
#if NETCOREAPP_3_0_GREATER
            MethodImplOptions.AggressiveOptimization;
#else
            MethodImplOptions.AggressiveInlining;
#endif
    }
}