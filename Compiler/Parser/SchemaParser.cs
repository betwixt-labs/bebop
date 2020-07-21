using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Lexer;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta;
using Compiler.Meta.Interfaces;
using Compiler.Parser.Extensions;

namespace Compiler.Parser
{
    public class SchemaParser
    {
        private readonly SchemaLexer _lexer;
        private readonly string _schemaPath;
        private List<IDefinition> _definitions;
        private uint _index;
        private string _package;
        private Token[] _tokens;

        public SchemaParser(string file)
        {
            _lexer = new SchemaLexer();
            _lexer.CreateFileHandle(file);
            _schemaPath = file;
        }

        public SchemaParser(string file, string schema)
        {
            _lexer = new SchemaLexer();
            _lexer.CreateMemoryHandle(schema);
            _schemaPath = file;
        }

        /// <summary>
        ///     Gets the <see cref="Token"/> at the current <see name="_index"/>
        /// </summary>
        private Token CurrentToken => _tokens[_index];

        /// <summary>
        ///     Walks the contents of a schema and turns it's body into characters into <see cref="Token"/> instances
        /// </summary>
        /// <returns></returns>
        private async Task Tokenize()
        {
            var collection = new List<Token>();
            await foreach (var token in _lexer.NextToken())
            {
                collection.Add(token);
            }
            _tokens = collection.ToArray();
        }

        /// <summary>
        ///     Peeks a token at the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token PeekToken(uint index) => _tokens[index];

        /// <summary>
        ///     Sets the current token stream position to the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token Base(uint index)
        {
            _index = index;
            return _tokens[index];
        }

        /// <summary>
        ///     If the <see cref="CurrentToken"/> matches the specified <paramref name="kind"/>, advance the token stream
        ///     <see cref="_index"/> forward
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private bool Eat(TokenKind kind)
        {
            if (CurrentToken.Kind == kind)
            {
                _index++;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     If the <see cref="CurrentToken"/> matches the specified <paramref name="kind"/>, advance the token stream
        ///     <see cref="_index"/> forward.
        ///     Otherwise throw a <see cref="UnexpectedTypeException"/>
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private void Expect(TokenKind kind)
        {
            if (!Eat(kind))
            {
                throw FailFast.ExpectedTypeException(kind, CurrentToken, _schemaPath);
            }
        }


        /// <summary>
        ///     Evaluates a schema and parses it into a <see cref="ISchema"/> object
        /// </summary>
        /// <returns></returns>
        public async Task<ISchema> Evaluate()
        {
            await Tokenize();
            _index = 0;
            _definitions = new List<IDefinition>();
            _package = string.Empty;
            
            if (Eat(TokenKind.Package))
            {
                _package = CurrentToken.Lexeme;
                Expect(TokenKind.Identifier);
                Expect(TokenKind.Semicolon);
            }

            
            while (_index < _tokens.Length && !Eat(TokenKind.EndOfFile))
            {
                var isReadOnly = Eat(TokenKind.ReadOnly);
                var kind = CurrentToken switch
                {
                    _ when Eat(TokenKind.Enum) => AggregateKind.Enum,
                    _ when Eat(TokenKind.Struct) => AggregateKind.Struct,
                    _ when Eat(TokenKind.Message) => AggregateKind.Message,
                    _ => throw FailFast.ExpectedTypeException(TokenKind.Message, CurrentToken, _schemaPath)
                };
                if (isReadOnly && kind != AggregateKind.Struct)
                {
                    throw FailFast.UnsupportedException(TokenKind.ReadOnly,
                        TokenKind.Struct,
                        CurrentToken,
                        _schemaPath);
                }
                DeclareAggregateType(CurrentToken, kind, isReadOnly);
            }
            return new PierogiSchema(_schemaPath, _package, _definitions);
        }


        /// <summary>
        ///     Declares an aggregate data structure and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that begins the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        /// <param name="isReadOnly"></param>
        private void DeclareAggregateType(Token definitionToken, AggregateKind kind, bool isReadOnly)
        {
            var fields = new List<IField>();
            Expect(TokenKind.Identifier);
            Expect(TokenKind.OpenBrace);
            while (!Eat(TokenKind.CloseBrace))
            {
                var typeCode = 0;
                var isArray = false;
                DeprecatedAttribute? deprecatedAttribute = null;
                var value = 0;

                if (kind != AggregateKind.Enum)
                {
                    if (!CurrentToken.Lexeme.Equals(definitionToken.Lexeme))
                    {
                        var result = DetermineType(CurrentToken);
                        if (!result.HasValue)
                        {
                            throw FailFast.UndefinedTypeException(CurrentToken, definitionToken, _schemaPath);
                        }
                        typeCode = result.Value;
                    }
                    else
                    {
                        // there is a self-nested field, so let's define this definition and set the type code to the next index
                        typeCode = _definitions.Count;
                    }

                    Expect(TokenKind.Identifier);
                    if (Eat(TokenKind.OpenBracket))
                    {
                        Expect(TokenKind.CloseBracket);
                        isArray = true;
                    }
                }

                var fieldName = CurrentToken.Lexeme;

                var fieldLine = (uint) CurrentToken.Position.StartLine;
                var fieldCol = (uint) CurrentToken.Position.StartColumn;

                Expect(TokenKind.Identifier);

                if (kind != AggregateKind.Struct)
                {
                    Expect(TokenKind.Eq);
                    value = int.Parse(CurrentToken.Lexeme);
                    Expect(TokenKind.Number);
                }
                if (Eat(TokenKind.OpenBracket))
                {
                    if (kind != AggregateKind.Message)
                    {
                        throw FailFast.UnsupportedException(TokenKind.Deprecated,
                            TokenKind.Message,
                            CurrentToken,
                            _schemaPath);
                    }
                    Expect(TokenKind.Deprecated);
                    Expect(TokenKind.OpenParenthesis);
                    var message = CurrentToken.Lexeme;
                    Expect(TokenKind.StringExpandable);
                    Expect(TokenKind.CloseParenthesis);
                    Expect(TokenKind.CloseBracket);
                    deprecatedAttribute = new DeprecatedAttribute(message);
                }

                Expect(TokenKind.Semicolon);
                fields.Add(new Field(fieldName, typeCode, fieldLine, fieldCol, isArray, deprecatedAttribute, value));
            }

            if (!_definitions.Any(d
                => d.Name.Equals(definitionToken.Lexeme) && d.Column == definitionToken.Position.StartColumn &&
                d.Line == definitionToken.Position.StartLine))
            {
                _definitions.Add(new Definition(definitionToken.Lexeme, isReadOnly,
                    (uint) definitionToken.Position.StartLine,
                    (uint) definitionToken.Position.StartColumn,
                    kind,
                    fields));
            }
        }

        /// <summary>
        ///     Attempts to determine the type code for the <paramref name="currentToken"/>, defining dependency types where necessary
        /// </summary>
        /// <param name="currentToken">the token that reflects the type to derive a code from.</param>
        /// <returns>A type code or null if none was found.</returns>
        private int? DetermineType(Token currentToken)
        {
            if (currentToken.TryParseType(out var typeCode))
            {
                return typeCode;
            }
            typeCode = _definitions.FindIndex(definition => definition.Name.Equals(currentToken.Lexeme));
            if (typeCode != -1)
            {
                return typeCode;
            }
            var currentField = _index;
            AggregateKind? kind = null;
            var startIndex = _tokens.FindToken(t
                => t.Key.IsAggregateKind(out kind) &&
                PeekToken((uint) (t.Value + 1)).Lexeme.Equals(currentToken.Lexeme));
            if (startIndex == -1)
            {
                return null;
            }
            if (!kind.HasValue)
            {
                return null;
            }
            var rebase = Base((uint) startIndex + 1);
            Debug.Assert(kind != null, nameof(kind) + " != null");
            // ReSharper disable once PossibleInvalidOperationException
            DeclareAggregateType(rebase, kind.Value, PeekToken((uint) (startIndex - 1)).Kind == TokenKind.ReadOnly);
            Base(currentField);
            typeCode = _definitions.FindIndex(definition => definition.Name.Equals(CurrentToken.Lexeme));
            return typeCode == -1 ? null : typeCode;
        }
    }
}