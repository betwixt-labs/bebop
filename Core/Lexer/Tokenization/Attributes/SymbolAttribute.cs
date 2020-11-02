using System;

namespace Core.Lexer.Tokenization.Attributes
{
    /// <summary>
    /// An attribute applied to <see cref="TokenKind"/> members to define symbol
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SymbolAttribute : Attribute 
    {
        /// <summary>
        /// Creates a new attribute instance
        /// </summary>
        /// <param name="value">the symbol such as '{' or '?'</param>
        public SymbolAttribute(char value)
        {
            Value = value.ToString();
        }
        /// <summary>
        /// The <see cref="char"/> that corresponds to the underlying <see cref="TokenKind"/>
        /// </summary>
        public string Value { get; }

        public override string ToString() => Value;
    }
}