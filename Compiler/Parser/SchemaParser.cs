using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private List<IDefinition> _definitions;
        private uint _index;
        private string _package;
        private Token[] _tokens;
        private readonly string _schemaPath;

        public SchemaParser(string file)
        {
          
            _lexer = new SchemaLexer();
            _lexer.CreateFileHandle(file);
            _schemaPath = file;
        }

        private async Task Tokenize()
        {
            var collection = new List<Token>();
            await foreach (var token in _lexer.NextToken())
            {
                collection.Add(token);
            }
            _tokens = collection.ToArray();
        }

        private Token CurrentToken => _tokens[_index];

        private Token PeekToken(uint index) => _tokens[index];

        private Token RebaseToken(uint index)
        {
            _index = index;
            return _tokens[index];
        }


        private bool Eat(TokenKind kind)
        {
            if (CurrentToken.Kind == kind)
            {
                _index++;
                return true;
            }
            return false;
        }

        private void Expect(TokenKind kind)
        {
            if (!Eat(kind))
            {
                throw ExpectedTypeException(kind);
            }
        }

        private UnexpectedTypeException ExpectedTypeException(TokenKind expected)
        {
            return new UnexpectedTypeException(expected.ToString(), $"{CurrentToken.Lexeme} ({CurrentToken.Kind})", CurrentToken.Position.StartLine, CurrentToken.Position.StartColumn, _schemaPath);
        }
        private TypeUndefinedException UndefinedTypeException(in Token definitionToken)
        {
            return new TypeUndefinedException(CurrentToken.ToString(), $"{definitionToken.Lexeme} ({definitionToken.Kind})", CurrentToken.Position.StartLine, CurrentToken.Position.StartColumn, _schemaPath);
        }

        private UnsupportedOperationException UnsupportedException(TokenKind declaration, TokenKind required)
        {
            return new UnsupportedOperationException($"The \"{declaration}\" declaration may only be applied to \"{required}\" fields", CurrentToken.Position.StartLine, CurrentToken.Position.StartColumn, _schemaPath);
        }

        

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
                var kind = CurrentToken switch
                {
                    _ when Eat(TokenKind.Enum) => AggregateKind.Enum,
                    _ when Eat(TokenKind.Struct) => AggregateKind.Struct,
                    _ when Eat(TokenKind.Message) => AggregateKind.Message,
                    _ => throw ExpectedTypeException(TokenKind.Message)
                };
                DefineAggregate(CurrentToken, kind);
            }
            return new PierogiSchema(_package, _definitions);
        }


        /// <summary>
        /// Defines an aggregate type and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that begins the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        private void DefineAggregate(Token definitionToken, AggregateKind kind)
        {
            var fields = new List<IField>();
            Expect(TokenKind.Identifier);
            Expect(TokenKind.OpenBrace);
            while (!Eat(TokenKind.CloseBrace))
            {
                var typeCode = 0;
                var isArray = false;
                var isDeprecated = false;
                uint value = 0;
               
                if (kind != AggregateKind.Enum)
                {
                    typeCode = GetTypeCode(CurrentToken);
                    if (typeCode == -1)
                    {
                        throw UndefinedTypeException(definitionToken);
                    }

                    Expect(TokenKind.Identifier);
                    if (Eat(TokenKind.OpenBracket))
                    {
                        Expect(TokenKind.CloseBracket);
                        isArray = true;
                    }
                }

                var fieldName = CurrentToken.Lexeme;

                var fieldLine = (uint)CurrentToken.Position.StartLine;
                var fieldCol = (uint)CurrentToken.Position.StartColumn;

                Expect(TokenKind.Identifier);

                if (kind != AggregateKind.Struct)
                {
                    Expect(TokenKind.Eq);
                    value = uint.Parse(CurrentToken.Lexeme);
                    Expect(TokenKind.Number);
                }
                if (Eat(TokenKind.OpenBracket))
                {
                    if (kind != AggregateKind.Message)
                    {
                        throw UnsupportedException(TokenKind.Deprecated, TokenKind.Message);
                    }
                    Expect(TokenKind.Deprecated);
                    Expect(TokenKind.CloseBracket);
                    isDeprecated = true;
                    
                }

                Expect(TokenKind.Semicolon);
                fields.Add(new Field(fieldName, typeCode, fieldLine, fieldCol, isArray, isDeprecated, value));
            }

            if (!_definitions.Any(d
                => d.Name.Equals(definitionToken.Lexeme) && d.Column == definitionToken.Position.StartColumn &&
                d.Line == definitionToken.Position.StartLine))
            {
                _definitions.Add(new Definition(definitionToken.Lexeme,
                    (uint)definitionToken.Position.StartLine,
                    (uint)definitionToken.Position.StartColumn,
                    kind,
                    fields));
            }

        }
        /// <summary>
        /// Searches for a type code, defining dependency types where necessary 
        /// </summary>
        /// <param name="currentToken">the token that reflects the type to derive a code from.</param>
        /// <returns>A type code or -1 if none was found.</returns>
        private int GetTypeCode(Token currentToken)
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
            var startIndex = _tokens.FindToken(t => t.Key.IsAggregateKind(out kind) && PeekToken((uint)(t.Value + 1)).Lexeme.Equals(currentToken.Lexeme));
            if (startIndex == -1)
            {
                return -1;
            }
            if (!kind.HasValue)
            {
                return -1;
            }
            var rebase = RebaseToken((uint)startIndex + 1);
            Debug.Assert(kind != null, nameof(kind) + " != null");
            DefineAggregate(rebase, kind.Value);
            RebaseToken(currentField);
            return _definitions.FindIndex(definition => definition.Name.Equals(CurrentToken.Lexeme));
        }
    }
}