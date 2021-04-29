using System;

namespace Core.Lexer.Tokenization.Models
{
    /// <summary>
    ///     Represents many of the various Bebop schema tokens.
    /// </summary>
    public readonly struct Token : IEquatable<Token>, IComparable<Token>
    {
        /// <summary>
        ///     The specific kind of token.
        /// </summary>
        public TokenKind Kind { get; }

        /// <summary>
        ///     The text content of the token.
        /// </summary>
        public string Lexeme { get; }

        /// <summary>
        /// The position of the current token in the schema.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        /// The position of the token in the token stream.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The overall length of the token <see cref="Lexeme"/>
        /// </summary>
        public uint Length { get; }


        /// <summary>
        ///     Create a new lexical token instance
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> member that identifies the token.</param>
        /// <param name="lexeme">The text of the token as it appeared in the schema.</param>
        /// <param name="span">The absolute position of the token in the schema</param>
        /// <param name="index"></param>
        public Token(TokenKind kind, string lexeme, Span span, int index)
        {
            Kind = kind;
            Lexeme = lexeme;
            Index = index;
            Length = (uint) Lexeme.Length;
            Span = span;
        }

        public override int GetHashCode() => HashCode.Combine((int) Kind, Lexeme, Span);

        /// <summary>Indicates whether the current <see cref="Token"/> is equal to another.</summary>
        /// <param name="other">The token to compare with the current token.</param>
        /// <returns>
        ///     <see langword="true"/> if the current token is equal to the <paramref name="other"/> parameter; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public bool Equals(Token other) => Kind == other.Kind &&
            string.Equals(Lexeme, other.Lexeme, StringComparison.Ordinal) && Span.Equals(other.Span);

        /// <summary>Indicates whether the current <see cref="Token"/> is equal to another.</summary>
        /// <param name="obj">The token to compare with the current token.</param>
        /// <returns>
        ///     <see langword="true"/> if the current token is equal to the <paramref name="obj"/> parameter; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is Token other && Equals(other);

        public int CompareTo(Token other)
        {
            return Span.CompareTo(other.Span);
        }

        public override string ToString() => $"{Kind}({Lexeme})";
    }
}
