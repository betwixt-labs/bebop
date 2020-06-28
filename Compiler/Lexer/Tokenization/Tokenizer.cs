using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Compiler.IO.Interfaces;
using Compiler.Lexer.Extensions;
using Compiler.Lexer.Tokenization.Interfaces;
using Compiler.Lexer.Tokenization.Models;

namespace Compiler.Lexer.Tokenization
{
    public class Tokenizer : ITokenizer
    {
        private ISchemaReader _reader;
        /// <summary> The position of all newlines in the buffer, in ascending order. </summary>
        /// <remarks> Used for <see cref="UpdateTokenPosition"/></remarks>
        public List<int> Newlines { get; private set; }

        protected int TokenCount { get; private set; }

        protected Span CurrentTokenPosition { get; private set; }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        /// <summary>
        /// Assigns a reader for working on a schema
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        public void AssignReader<T>(T reader) where T : ISchemaReader
        {
            Newlines = new List<int>();
            _reader = reader;
        }

        /// <summary>
        /// Updates the current token line and column based on the <paramref name="currentPosition"/> provided
        /// </summary>
        /// <param name="currentPosition"></param>
        public void UpdateTokenPosition(int currentPosition)
        {
            var lastLine = 0;
            var b = Newlines.Count;

            if (b == 0 || currentPosition < Newlines[0])
            {
                CurrentTokenPosition = new Span(0, currentPosition);
                return;
            }

            var last = Newlines[b - 1];
            if (currentPosition > last)
            {
                CurrentTokenPosition = new Span(b, currentPosition - last - 1);
                return;
            }

            while (lastLine  < b)
            {
                var m = (lastLine + b) / 2;
                var v = Newlines[m];
                if (currentPosition <= v) b = m;
                else lastLine = m;
            }
            CurrentTokenPosition = new Span(lastLine + 1, currentPosition - Newlines[lastLine] - 1);
        }

        /// <summary>
        /// Yields back a a stream of tokens asynchronously 
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<Token> TokenStream()
        {
            CurrentTokenPosition = Span.Empty;
            TokenCount = 0;
            while (_reader != null && _reader.Peek() > 0)
            {

           
                var current = _reader.GetChar();
                var next = _reader.PeekChar();
               

                if (current.IsLineEnding(next))
                {
                    // when the schema is encoded using CRLF we need to skip ahead another byte
                    if (current.GetLineEndingType(next) == LineEndingType.CarriageReturn)
                    {
                        _reader.GetChar();
                    }
                    CurrentTokenPosition = CurrentTokenPosition.NewLine;
                    Newlines.Add(_reader.CurrentPosition - 1);
                    continue;
                }
                UpdateTokenPosition(_reader.CurrentPosition - 1);
                
                var scan = TryScan(current);
              
                if (scan.HasValue)
                {
                    // updates the tokens end column
                    CurrentTokenPosition = CurrentTokenPosition.SetEndColumn(scan.Value.Length);
                    yield return await Task.FromResult(scan.Value.UpdatePosition(CurrentTokenPosition));
                    TokenCount++;
                }
            }
            ++TokenCount;
            yield return new Token(TokenKind.EndOfFile, string.Empty, CurrentTokenPosition, TokenCount);
        }

        /// <summary>
        /// Tries to assign a token to the current <paramref name="surrogate"/>
        /// </summary>
        /// <param name="surrogate"></param>
        /// <returns></returns>
        public Token? TryScan(char surrogate) => surrogate switch
        {
            _ when IsSymbol(surrogate, out var s) => s,
            _ when IsIdentifier(surrogate, out var i) => i,
            _ when IsLiteral(surrogate, out var l) => l,
            _ when IsNumber(surrogate, out var n) => n,
            _ => null
        };


        /// <summary>
        /// Determines if a surrogate is a integral token
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
            while (_reader.PeekChar().IsDecimalDigit())
            {
                builder.Append(_reader.GetChar());
            }
            token = new Token(TokenKind.Number, builder.ToString(), CurrentTokenPosition, TokenCount);
            return true;
        }

        /// <summary>
        /// Determines if a surrogate is the beginning of a literal token
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsLiteral(char surrogate, out Token token)
        {
            token = default;
            return surrogate switch
            {
                _ when surrogate.IsSingleQuote() => ScanStringLiteral(out token),
                _ when surrogate.IsDoubleQuote() => ScanStringExpandable(out token),
                _ => false
            };
        }

        /// <summary>
        /// Reads a string that is wrapped in double quotes
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool ScanStringExpandable(out Token token)
        {
            token = default;
            var builder = new StringBuilder();
            var currentChar = _reader.GetChar();
            while (currentChar != '\0')
            {
                if (currentChar.IsDoubleQuote())
                {
                    if (!_reader.PeekChar().IsDoubleQuote())
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
            token = new Token(TokenKind.StringExpandable, builder.ToString(), CurrentTokenPosition, TokenCount);
            return true;
        }

        /// <summary>
        /// Reads a string that is wrapped in single quotes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool ScanStringLiteral(out Token token)
        {
            token = default;
            var builder = new StringBuilder();
            var currentChar = _reader.GetChar();
            while (currentChar != '\0')
            {
                if (currentChar.IsSingleQuote())
                {
                    if (!_reader.PeekChar().IsSingleQuote())
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
            token = new Token(TokenKind.StringLiteral, builder.ToString(), CurrentTokenPosition, TokenCount);
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
            if (surrogate == '/' && _reader.PeekChar() == '/')
            {
                return ReadLineComment(out token);
            }
            if (TokenizerExtensions.TryGetSymbol(surrogate, out var kind))
            {
                token = new Token(kind, surrogate.ToString(), CurrentTokenPosition, TokenCount);
                return true;
            }
            token = default;
            return false;
        }

        /// <summary>
        /// Parses a line comment beginning with a sequence of "//" 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool ReadLineComment(out Token token)
        {
            // skip the second 
            _reader.GetChar();
            var builder = new StringBuilder();
            while (_reader.Peek() > 0 && !_reader.PeekChar().IsLineEnding())
            {
                var current = _reader.GetChar();
                if (!current.IsWhitespace())
                {
                    builder.Append(current);
                }
            }
            token = new Token(TokenKind.LineComment, builder.ToString(), CurrentTokenPosition, TokenCount);
            return true;
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

            token = TokenizerExtensions.TryGetKeyword(lexeme, out var kind)
                ? new Token(kind, lexeme, CurrentTokenPosition, TokenCount)
                : new Token(TokenKind.Identifier, lexeme, CurrentTokenPosition, TokenCount);
            return true;
        }
    }
}