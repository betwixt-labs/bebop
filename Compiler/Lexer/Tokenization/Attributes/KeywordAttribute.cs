using System;

namespace Compiler.Lexer.Tokenization.Attributes
{
    /// <summary>
    /// An attribute applied to <see cref="TokenKind"/> members to define a literal keyword
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class KeywordAttribute : Attribute 
    {
        /// <summary>
        /// Creates a new attribute instance
        /// </summary>
        /// <param name="value">the keyword such as 'enum' or 'struct'</param>
        public KeywordAttribute(string value)
        {
            Value = value;
        }
        /// <summary>
        /// The string literal that corresponds to the underlying <see cref="TokenKind"/>
        /// </summary>
        public string Value { get; }

        public override string ToString() => Value;
    }
}