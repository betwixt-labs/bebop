using System;
using System.Collections.Generic;
using System.Reflection;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Attributes;

namespace Compiler.Lexer.Extensions
{
    public static class TokenizerExtensions
    {
        /// <summary>
        ///     A map of strings and their well-known token kind.
        /// </summary>
        private static readonly Dictionary<string, TokenKind> Keywords = PopulateKinds<string, KeywordAttribute>();

        /// <summary>
        ///     A map of chars and their well-known token kind.
        /// </summary>
        private static readonly Dictionary<char, TokenKind> Symbols = PopulateKinds<char, SymbolAttribute>();


        /// <summary>
        ///     Creates a dictionary using a source value type and an attribute that is applied to <see cref="TokenKind"/> fields.
        /// </summary>
        /// <typeparam name="TSource">
        ///     The type the key of the dictionary will be. Should match the parameter type of
        ///     <paramref name=":TAttribute"/>
        /// </typeparam>
        /// <typeparam name="TAttribute">The attribute that is defined on the desired group of <see cref="TokenKind"/> members.</typeparam>
        /// <returns></returns>
        private static Dictionary<TSource, TokenKind> PopulateKinds<TSource, TAttribute>() where TSource : notnull where TAttribute : Attribute
        {
            var dict = new Dictionary<TSource, TokenKind>();
            var enumNames = Enum.GetNames(typeof(TokenKind));
            foreach (var name in enumNames)
            {
                var attribute = typeof(TokenKind)?.GetField(name)
                    ?.GetCustomAttribute<TAttribute>()
                    ?.ToString();
                if (string.IsNullOrWhiteSpace(attribute))
                {
                    continue;
                }
                dict.Add((TSource) Convert.ChangeType(attribute, typeof(TSource)),
                    (TokenKind) Enum.Parse(typeof(TokenKind), name));
            }
            return dict;
        }

        /// <summary>
        ///     Checks if a string as represented in the <paramref name="keyword"/> parameter is a well-known token.
        /// </summary>
        /// <param name="keyword">The string to check.</param>
        /// <param name="kind">If token is well-known, this variable is the token kind.</param>
        /// <returns>true if the token could be identified, otherwise false.</returns>
        public static bool TryGetKeyword(string keyword, out TokenKind kind)
            => Keywords.TryGetValue(keyword, out kind);

        /// <summary>
        ///     Checks if a char as represented in the <paramref name="symbol"/> parameter is a well-known token.
        /// </summary>
        /// <param name="symbol">The char to check.</param>
        /// <param name="kind">If token is well-known, this variable is the token kind.</param>
        /// <returns>true if the token could be identified, otherwise false.</returns>
        public static bool TryGetSymbol(char symbol, out TokenKind kind)
            => Symbols.TryGetValue(symbol, out kind);
    }
}