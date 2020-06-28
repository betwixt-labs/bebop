using System;

namespace Compiler.Lexer.Tokenization.Models
{
    /// <summary>
    ///     Represents many of the various Pierogi schema tokens.
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
        public Span Position { get; }

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
        /// <param name="position">The absolute position of the token in the schema</param>
        /// <param name="index"></param>
        public Token(TokenKind kind, string lexeme, Span position, int index)
        {
            Kind = kind;
            Lexeme = lexeme;
            Position = position;
            Index = index;
            Length = (uint) (Lexeme?.Length ?? 0);
        }

        /// <summary>
        /// Updates the position of the current token
        /// </summary>
        /// <param name="position">the new span to assign the current token</param>
        public Token UpdatePosition(Span position) => new Token(Kind, Lexeme, position, Index);


        public override int GetHashCode() => HashCode.Combine((int) Kind, Lexeme, Position);

        /// <summary>Indicates whether the current <see cref="Token"/> is equal to another.</summary>
        /// <param name="other">The token to compare with the current token.</param>
        /// <returns>
        ///     <see langword="true"/> if the current token is equal to the <paramref name="other"/> parameter; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public bool Equals(Token other) => Kind == other.Kind &&
            string.Equals(Lexeme, other.Lexeme, StringComparison.Ordinal) && Position.Equals(other.Position);

        /// <summary>Indicates whether the current <see cref="Token"/> is equal to another.</summary>
        /// <param name="obj">The token to compare with the current token.</param>
        /// <returns>
        ///     <see langword="true"/> if the current token is equal to the <paramref name="obj"/> parameter; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj) => obj is Token other && Equals(other);

        public int CompareTo(Token other)
        {
            return Position.CompareTo(other.Position);
        }
    }
}