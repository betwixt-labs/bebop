using System;
using System.Collections.Generic;
using Core.Generators.CSharp;
using Core.Generators.Dart;
using Core.Generators.TypeScript;
using Core.Meta.Interfaces;

namespace Core.Generators
{
    public static class GeneratorUtils
    {

        /// <summary>
        /// A dictionary that contains generators.
        /// </summary>
        /// <remarks>
        /// Generators are keyed via their commandline alias.
        /// </remarks>
        public static Dictionary<string, Func<ISchema, Generator>> ImplementedGenerators  = new Dictionary<string, Func<ISchema, Generator>> {
            { "ts", s => new TypeScriptGenerator(s) },
            { "cs", s => new CSharpGenerator(s) },
            { "dart", s => new DartGenerator(s) },
        };
   
        /// <summary>
        /// Returns a loop variable name based on the provided loop <paramref name="depth"/>
        /// </summary>
        /// <param name="depth">The depth of the loop</param>
        /// <returns>for 0-3 an actual letter is returned, for anything greater the depth prefixed with "i" is returned.</returns>
        public static string LoopVariable(int depth)
        {
            return $"i{depth}";
        }
    }
}
