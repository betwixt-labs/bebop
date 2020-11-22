using System.Runtime.CompilerServices;

namespace Bebop.Runtime
{
    public static class BebopConstants
    {
    #if AGGRESSIVE_OPTIMIZE
        public const MethodImplOptions HotPath =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    #else
        public const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
    #endif
    }
}