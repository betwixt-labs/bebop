using System.Runtime.CompilerServices;

namespace Bebop.Runtime
{
    /// <summary>
    ///     Constant Bebop runtime values.
    /// </summary>
    public static class BebopConstants
    {
    #if AGGRESSIVE_OPTIMIZE
        /// <summary>
        ///     Identifies a method as being a "HotPath" and applies aggressive inlining and optimizations.
        /// </summary>
        public const MethodImplOptions HotPath =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    #else
        /// <summary>
        ///     Identifies a method as being a "HotPath" and applies aggressive inlining.
        /// </summary>
        public const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
    #endif
    }
}