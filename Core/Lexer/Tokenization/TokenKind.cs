using Core.Lexer.Tokenization.Attributes;

namespace Core.Lexer.Tokenization
{
    /// <summary>
    ///     An enumeration that identifies and describes various lexical tokens
    /// </summary>
    public enum TokenKind : ushort
    {
    #region Keywords

        /// <summary>
        ///     The 'enum' keyword which is used by the enum <see cref="Meta.AggregateKind"/>
        /// </summary>
        [Keyword("enum")]
        Enum,

        /// <summary>
        ///     The 'struct' keyword which is used by the structure <see cref="Meta.AggregateKind"/>
        /// </summary>
        [Keyword("struct")]
        Struct,

        /// <summary>
        ///     The 'message' keyword which is used by the message <see cref="Meta.AggregateKind"/>
        /// </summary>
        [Keyword("message")]
        Message,

        /// <summary>
        ///     The 'readonly' keyword which is reserved by the compiler
        /// </summary>
        [Keyword("readonly")]
        ReadOnly,

        /// <summary>
        ///     The 'deprecated' keyword which is reserved by the compiler
        /// </summary>
        [Keyword("deprecated")]
        Deprecated,

        /// <summary>
        ///     The 'opcode' keyword which is reserved by the compiler
        /// </summary>
        [Keyword("opcode")]
        Opcode,

        /// <summary>
        ///     The 'map' keyword which is reserved by the compiler
        /// </summary>
        [Keyword("map")]
        Map,

        /// <summary>
        ///     The 'array' keyword which is reserved by the compiler
        /// </summary>
        [Keyword("array")]
        Array,

        /// <summary>
        ///     The 'union' keyword which is reserved by the compiler
        /// </summary>
        [Keyword("union")]
        Union,

    #endregion


        #region Literals
        Identifier,

        /// <summary>
        ///     A single quoted string literal.
        /// </summary>
        StringLiteral,

        /// <summary>
        ///     A double quoted string literal.
        /// </summary>
        StringExpandable,

        /// <summary>
        ///     Any numerical literal token.
        /// </summary>
        Number,
        /// <summary>
        /// A token that represents a multi-line block comment
        /// </summary>
        BlockComment,
        EndOfFile,

    #endregion


    #region Symbols

        /// <summary>
        ///     <![CDATA[ ( ]]>
        /// </summary>
        [Symbol('(')]
        OpenParenthesis,

        /// <summary>
        ///     <![CDATA[ ) ]]>
        /// </summary>
        [Symbol(')')]
        CloseParenthesis,

        /// <summary>
        ///     <![CDATA[ < ]]>
        /// </summary>
        [Symbol('<')]
        OpenCaret,

        /// <summary>
        ///     <![CDATA[ > ]]>
        /// </summary>
        [Symbol('>')]
        CloseCaret,

        /// <summary>
        ///     <![CDATA[ { }]]>
        /// </summary>
        [Symbol('{')]
        OpenBrace,

        /// <summary>
        ///     <![CDATA[ }]]>
        /// </summary>
        [Symbol('}')]
        CloseBrace,

        /// <summary>
        ///     <![CDATA[ [ ]]>
        /// </summary>
        [Symbol('[')]
        OpenBracket,

        /// <summary>
        ///     <![CDATA[ ] ]]>
        /// </summary>
        [Symbol(']')]
        CloseBracket,

        /// <summary>
        ///     <![CDATA[ : ]]>
        /// </summary>
        [Symbol(':')]
        Colon,

        /// <summary>
        ///     <![CDATA[ ; ]]>
        /// </summary>
        [Symbol(';')]
        Semicolon,

        /// <summary>
        ///     <![CDATA[ , ]]>
        /// </summary>
        [Symbol(',')]
        Comma,

        /// <summary>
        ///     <![CDATA[ . ]]>
        /// </summary>
        [Symbol('.')]
        Dot,

        /// <summary>
        ///     <![CDATA[ ? ]]>
        /// </summary>
        [Symbol('?')]
        QuestionMark,

        /// <summary>
        ///     <![CDATA[ / ]]>
        /// </summary>
        [Symbol('/')]
        Slash,

        /// <summary>
        ///     The assignment operator '='.
        /// </summary>
        [Symbol('=')]
        Eq,
        /// <summary>
        ///      <![CDATA[ - ]]>
        /// </summary>
        [Symbol('-')]
        Hyphen
        #endregion
    }
}