using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Core.Lexer.Extensions;

namespace Core.Meta.Extensions
{
    public static class StringExtensions
    {
        private const char SnakeSeparator = '_';
        private const char KebabSeparator = '-';

        private static readonly string[] NewLines = { "\r\n", "\r", "\n" };

      
        /// <summary>
        ///     Splits the specified <paramref name="value"/> based on line ending.
        /// </summary>
        /// <param name="value">The input string to split.</param>
        /// <returns>An array of each line in the string.</returns>
        public static string[] GetLines(this string value) => string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(NewLines, StringSplitOptions.None);

        /// <summary>
        ///     Determines if the specified char array contains only uppercase characters.
        /// </summary>
        private static bool IsUpper(this Span<char> array)
        {
            foreach (var currentChar in array)
            {
                if (!char.IsUpper(currentChar) && currentChar is not (SnakeSeparator or KebabSeparator))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Converts the specified <paramref name="input"/> string into PascalCase.
        /// </summary>
        /// <param name="input">The input string that will be converted.</param>
        /// <returns>The mutated string.</returns>
        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }
            // DO capitalize both characters on two-character acronyms.
            if (input.Length <= 2)
            {
                return input.ToUpper();
            }
            // Remove invalid characters.
            var charArray = new Span<char>(input.ToCharArray());
            // Set first letter to uppercase
            if (char.IsLower(charArray[0]))
            {
                charArray[0] = char.ToUpperInvariant(charArray[0]);
            }

            // DO capitalize only the first character of acronyms with three or more characters, except the first word.
            // DO NOT capitalize any of the characters of any acronyms, whatever their length.
            if (charArray.IsUpper())
            {
                // Replace all characters following the first to lowercase when the entire string is uppercase (ABC -> Abc)
                for (var i = 1; i < charArray.Length; i++)
                {
                    charArray[i] = char.ToLowerInvariant(charArray[i]);
                }
            }

            for (var i = 1; i < charArray.Length; i++)
            {
                var currentChar = charArray[i];
                var lastChar = charArray.Peek(i is 1 ? 1 : i - 1);
                var nextChar = charArray.Peek(i + 1);

                if (currentChar.IsDecimalDigit() && char.IsLower(nextChar))
                {
                    charArray[i + 1] = char.ToUpperInvariant(nextChar);
                }
                else if (currentChar is SnakeSeparator or KebabSeparator)
                {
                    if (char.IsLower(nextChar))
                    {
                        charArray[i + 1] = char.ToUpperInvariant(nextChar);
                    }
                    if (char.IsUpper(lastChar))
                    {
                        charArray[i - 1] = char.ToLowerInvariant(lastChar);
                    }
                }
            }
            return new string(charArray.ToArray().Where(c => c is not (SnakeSeparator or KebabSeparator)).ToArray());
        }

        /// <summary>
        ///     Peeks a char at the specified <paramref name="index"/> from the provided <paramref name="array"/>
        /// </summary>
        private static char Peek(this Span<char> array, int index)
        {
            return index < array.Length && index >= 0 ? array[index] : default;
        }

        /// <summary>
        ///     Converts the specified <paramref name="input"/> string into camelCase.
        /// </summary>
        /// <param name="input">The input string that will be converted.</param>
        /// <returns>The mutated string.</returns>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }
            if (input.Length == 1)
            {
                return input;
            }
            // Pascal is a subset of camelCase. The first letter of Pascal is capital and first letter of the camel is small
            var converted = input.ToPascalCase();
            var f = converted[..1];
            var r = converted[1..];

            if (char.IsUpper(f[0]) && char.IsUpper(r[0]))
            {
                return input;
            }

            return f.ToLowerInvariant() + r;
        }

        /// <summary>
        ///     Converts the specified <paramref name="input"/> string into snake_case.
        /// </summary>
        /// <param name="input">The input string that will be converted.</param>
        /// <returns>The mutated string.</returns>
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }
            if (input.Length == 1)
            {
                return input;
            }
            // Remove invalid characters.
            var charArray = new Span<char>(input.ToCharArray());
            var builder = new StringBuilder();

            for (var i = 0; i < charArray.Length; i++)
            {
                var currentChar = charArray[i];
                var nextChar = charArray.Peek(i + 1);

                if (currentChar is SnakeSeparator or KebabSeparator)
                {
                    builder.Append(SnakeSeparator);
                }
                else if (char.IsLower(currentChar) && char.IsUpper(nextChar))
                {
                    builder.Append(char.ToLowerInvariant(currentChar));
                    builder.Append('_');
                }
                else if (char.IsUpper(currentChar))
                {
                    builder.Append(char.ToLowerInvariant(currentChar));
                }
                else
                {
                    builder.Append(currentChar);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        ///     Converts the specified <paramref name="input"/> string into kebab-case.
        /// </summary>
        /// <param name="input">The input string that will be converted.</param>
        /// <returns>The mutated string.</returns>
        public static string ToKebabCase(this string input) => input.ToSnakeCase().Replace(SnakeSeparator, KebabSeparator);

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
