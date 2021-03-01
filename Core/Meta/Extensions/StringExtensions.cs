using System;
using System.Linq;

namespace Core.Meta.Extensions
{
    public static class StringExtensions
    {

        private static readonly string[] _newLines = new[] {"\r\n", "\r", "\n"};
        /// <summary>
        /// Splits the specified <paramref name="value"/> based on line ending.
        /// </summary>
        /// <param name="value">The input string to split.</param>
        /// <returns>An array of each line in the string.</returns>
        public static string[] GetLines(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(_newLines, StringSplitOptions.None);
        }

        /// <summary>
        ///     Converts a string into Pascal case
        /// </summary>
        /// <param name="input">the input string we need to convert</param>
        /// <returns>a pascal converted string</returns>
        public static string ToPascalCase(this string input)
        {
            // If there are 0 or 1 characters, just return the string.
            if (input.Length < 2)
            {
                return input.ToUpper();
            }

            // Split the string into words.
            var words = input.Split(new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            return words.Aggregate("", (current, word) => current + word.Substring(0, 1).ToUpper() + word.Substring(1));
        }

        /// <summary>
        ///     Converts a string to a camelcase representation
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToCamelCase(this string str)
        {
            if (str.Length == 1)
            {
                return str;
            }

            var f = str.Substring(0, 1);
            var r = str.Substring(1);

            if (char.IsUpper(f[0]) && char.IsUpper(r[0]))
            {
                return str;
            }

            return f.ToLowerInvariant() + r;
        }

        public static bool TryParseUInt(this string str, out uint result)
        {
            if (uint.TryParse(str, out result))
            {
                return true;
            }
            if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    result = Convert.ToUInt32(str, 16);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}