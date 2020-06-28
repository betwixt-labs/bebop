using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler.Meta.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string into Pascal case
        /// </summary>
        /// <param name="input">the input string we need to convert</param>
        /// <returns>a pascal converted string</returns>
        public static string ToPascalCase(this string input)
        {
            // If there are 0 or 1 characters, just return the string.
            if (input == null) return null;
            if (input.Length < 2) return input.ToUpper();

            // Split the string into words.
            var words = input.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            return words.Aggregate("", (current, word) => current + (word.Substring(0, 1).ToUpper() + word.Substring(1)));
        }

        /// <summary>
        /// Converts a string to a camelcase representation
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToCamelCase(this string str)
        {
            if (str.Length == 1) return str;

            var f = str.Substring(0, 1);
            var r = str.Substring(1);

            if (char.IsUpper(f[0]) && char.IsUpper(r[0])) return str;

            return f.ToLowerInvariant() + r;
        }
    }
}
