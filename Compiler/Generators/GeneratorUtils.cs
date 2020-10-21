using System;
using System.Collections.Generic;
using Compiler.Generators.CSharp;
using Compiler.Generators.TypeScript;

namespace Compiler.Generators
{
    public static class GeneratorUtils
    {

        /// <summary>
        /// A dictionary that contains generators.
        /// </summary>
        /// <remarks>
        /// Generators are keyed via their commandline alias.
        /// </remarks>
        public static Dictionary<string, IGenerator> ImplementedGenerators  = new Dictionary<string, IGenerator> {
            { "ts", new TypeScriptGenerator() },
            { "cs", new CSharpGenerator() }
        };
   
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
