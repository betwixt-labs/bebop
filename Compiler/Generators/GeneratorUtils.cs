using System;
namespace Compiler.Generators
{
    public static class GeneratorUtils
    {
        /// <summary>
        /// Returns a loop variable name based on the provided loop <paramref name="depth"/>
        /// </summary>
        /// <param name="depth">The depth of the loop</param>
        /// <returns>for 0-3 an actual letter is returned, for anything greater the depth prefixed with "i" is returned.</returns>
        public static string LoopVariable(int depth)
        {
            return depth switch
            {
                0 => "i",
                1 => "j",
                2 => "k",
                3 => "l",
                _ => $"i{depth}",
            };
        }
    }
}
