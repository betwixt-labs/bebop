using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.IO;
using Core.IO.Interfaces;
using Core.Lexer.Extensions;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Extensions;

namespace Core.Lexer.Tokenization
{
    public class Tokenizer
    {
        private ISchemaReader _reader;

        public Tokenizer(ISchemaReader reader)
        {
            _reader = reader;
        }

        protected int TokenCount { get; private set; }

        protected Span TokenStart { get; private set; }

        private Token MakeToken(TokenKind kind, string lexeme)
        {
            var span = kind == TokenKind.EndOfFile ? _reader.LatestEofSpan() : TokenStart.Combine(_reader.CurrentSpan());
            return new Token(kind, lexeme, span, TokenCount++);
        }

        private List<Token> _tokens = new List<Token>();
        bool _newFilesToTokenize = true;

        public List<Token> Tokens
        {
            get
            {
                if (_newFilesToTokenize) _tokens.AddRange(GetPendingTokens());
                return _tokens;
            }
        }

        public async Task AddFile(string absolutePath)
        {
            if (await _reader.AddFile(absolutePath))
            {
                _newFilesToTokenize = true;
            }
        }


        /// <summary>
        /// Yields all pending tokens from the reader.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Token> GetPendingTokens()
        {
            while (true)
            {
                var current = GetCharSkippingTrivia();
                if (current == '\0') break;
                Token? scan = TryScan(current);
                if (!scan.HasValue)
                {
                    throw new UnrecognizedTokenException(current, TokenStart);
                }

                yield return scan.Value;
            }
            _newFilesToTokenize = false;
        }

        /// <summary>
        /// Skip over whitespace and comments, then return the first char of the next token.
        /// (This may be '\0' if the end of the token stream is reached.)
        /// </summary>
        /// <returns>The first char of the next token.</returns>
        public char GetCharSkippingTrivia()
        {
            var inLineComment = false;
            while (true)
            {
                var c = _reader.PeekChar();

                // Report end of token stream no matter what.
                if (c == '\0') return c;

                // Report (and skip over) a file separator no matter what.
                if (c == CharExtensions.FileSeparator) return _reader.GetChar();

                // Parse \r or \n or \r\n as a newline.
                var isNewLine = false;
                if (c == '\r')
                {
                    _reader.GetChar();
                    c = _reader.PeekChar();
                    isNewLine = true;
                }
                if (c == '\n')
                {
                    _reader.GetChar();
                    isNewLine = true;
                }
                if (isNewLine)
                {
                    inLineComment = false;
                    continue;
                }

                // Skip over non-newline whitespace.
                // While in a line comment, skip over anything that isn't a newline.
                if (c.IsWhitespace() || inLineComment)
                {
                    _reader.GetChar();
                    continue;
                }

                // This character starts the next token, unless it's the start of a line comment.
                TokenStart = _reader.CurrentSpan();
                c = _reader.GetChar();
                if (c == '/' && _reader.PeekChar() == '/')
                {
                    _reader.GetChar();
                    inLineComment = true;
                    continue;
                }
                return c;
            }
        }

        /// <summary>
        /// Tries to assign a token to the current <paramref name="surrogate"/>
        /// </summary>
        /// <param name="surrogate"></param>
        /// <returns></returns>
        public Token? TryScan(char surrogate) => surrogate switch
        {
            _ when surrogate == CharExtensions.FileSeparator => MakeToken(TokenKind.EndOfFile, ""),
            _ when IsBlockComment(surrogate, out var b) => b,
            _ when IsNumber(surrogate, out var n) => n,
            _ when IsSymbol(surrogate, out var s) => s,
            _ when IsIdentifier(surrogate, out var i) => i,
            _ when IsLiteral(surrogate, out var l) => l,
            _ => null
        };




        /// <summary>
        /// Determines if a surrogate leads into a block comment.
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsBlockComment(char surrogate, out Token token)
        {
            token = default;
            if (surrogate != '/' || _reader.PeekChar() != '*')
            {
                return false;
            }

            _reader.GetChar();
            var builder = new StringBuilder();
            var currentChar = _reader.GetChar();
            while (currentChar != '\0')
            {
                // we have reached the end of the block comment
                if (currentChar == '*' && _reader.PeekChar() == '/')
                {
                    _reader.GetChar();
                    break;
                }
                builder.Append(currentChar);
                currentChar = _reader.GetChar();
            }

            var cleanedDocumentation = new StringBuilder();

            foreach (var line in builder.ToString().GetLines())
            {
                var trimmedLine = line.Trim(' ', '*');
                cleanedDocumentation.AppendLine(trimmedLine);
            }


            token = MakeToken(TokenKind.BlockComment, cleanedDocumentation.ToString().TrimStart().TrimEnd());
            return true;
        }


        /// <summary>
        /// Determines if a surrogate starts a numeric token.
        /// A numeric token matches the regex: [0-9-][0-9A-Za-z_.]*
        /// This is a little "greedy": it includes things like GUID literals starting with a decimal digit, or "-inf".
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsNumber(char surrogate, out Token token)
        {
            token = default;
            if (!surrogate.IsDecimalDigit())
            {
                return false;
            }
            var builder = new StringBuilder();
            builder.Append(surrogate);

            char c;
            while ((c = _reader.PeekChar()).IsIdentifierFollow() || c == '.')
            {
                builder.Append(_reader.GetChar());
            }

            var tokenString = builder.ToString();
            token = MakeToken(TokenKind.Number, builder.ToString());
            return true;
        }

        /// <summary>
        /// Determines if a surrogate is the beginning of a string literal token
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsLiteral(char surrogate, out Token token)
        {
            token = default;
            return surrogate switch
            {
                '\'' => ScanString(out token, '\''),
                '"' => ScanString(out token, '\"'),
                _ => false
            };
        }
        /// <summary>
        /// Reads a string that is wrapped in single quotes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool ScanString(out Token token, char quote)
        {
            token = default;
            var builder = new StringBuilder();
            var currentChar = _reader.GetChar();
            while (currentChar != '\0')
            {
                if (currentChar == quote)
                {
                    // Quotes are escaped by doubling them. "Hello ""world""!" corresponds to: Hello "world"!
                    if (_reader.PeekChar() != quote)
                    {
                        break;
                    }
                    currentChar = _reader.GetChar();
                }
                builder.Append(currentChar);
                currentChar = _reader.GetChar();
            }
            if (currentChar == '\0')
            {
                // EOF
                return false;
            }
            token = MakeToken(TokenKind.String, builder.ToString());
            return true;
        }


        /// <summary>
        /// Determines if a surrogate is one that is defined with a <see cref="Attributes.SymbolAttribute"/>
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsSymbol(char surrogate, out Token token)
        {
            if (TokenizerExtensions.TryGetSymbol(surrogate, out var kind))
            {
                token = MakeToken(kind, surrogate.ToString());
                return true;
            }
            token = default;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsIdentifier(char surrogate, out Token token)
        {
            if (!surrogate.IsIdentifierStart())
            {
                token = default;
                return false;
            }

            var builder = new StringBuilder();
            builder.Append(surrogate);
            while (_reader.PeekChar().IsIdentifierFollow())
            {
                builder.Append(_reader.GetChar());
            }
            var lexeme = builder.ToString();

            token = MakeToken(TokenizerExtensions.TryGetKeyword(lexeme, out var kind) ? kind : TokenKind.Identifier, lexeme);
            return true;
        }
    }
}
