using System;
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
        private Dictionary<string, IDefinition> _definitions;
        private HashSet<(Token, Token)> _typeReferences;
        private uint _index;
        private string _package;
        private Token[] _tokens;

        /// <summary>
        /// Creates a new schema parser instance from a schema file on disk
        /// </summary>
        /// <param name="file">The path to the Pierogi schema file that will be parsed</param>
        public SchemaParser(string file)
        {
            _lexer = new SchemaLexer();
            _lexer.CreateFileHandle(file);
            _schemaPath = file;
        }

        /// <summary>
        /// Creates a new schema parser instance and loads the schema into memory
        /// </summary>
        /// <param name="file">The path to the Pierogi schema file that will be parsed</param>
        /// <param name="schema">The contents of the schema file.</param>
        public SchemaParser(string file, string schema)
        {
            _lexer = new SchemaLexer();
            _lexer.CreateMemoryHandle(schema);
            _schemaPath = file;
        }

        /// <summary>
        ///     Gets the <see cref="Token"/> at the current <see cref="_index"/>.
        /// </summary>
        private Token CurrentToken => _tokens[_index];

        /// <summary>
        /// Tokenize the input file, storing the result in <see cref="_tokens"/>.
        /// </summary>
        /// <returns></returns>
        private async Task Tokenize()
        {
            var collection = new List<Token>();
            await foreach (var token in _lexer.TokenStream())
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
        ///     Otherwise throw a <see cref="UnexpectedTokenException"/>
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private void Expect(TokenKind kind)
        {
            if (!Eat(kind))
            {
                throw new UnexpectedTokenException(kind, CurrentToken, _schemaPath);
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
            _definitions = new Dictionary<string, IDefinition>();
            _typeReferences = new HashSet<(Token, Token)>();
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
                    _ => throw new UnexpectedTokenException(TokenKind.Message, CurrentToken, _schemaPath)
                };
                DeclareAggregateType(CurrentToken, kind, isReadOnly);
            }
            foreach (var (typeToken, definitionToken) in _typeReferences)
            {
                if (!_definitions.ContainsKey(typeToken.Lexeme))
                {
                    throw new UnrecognizedTypeException(typeToken, definitionToken.Lexeme, _schemaPath);
                }
            }
            return new PierogiSchema(_schemaPath, _package, _definitions);
        }


        /// <summary>
        ///     Declares an aggregate data structure and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that names the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        /// <param name="isReadOnly"></param>
        private void DeclareAggregateType(Token definitionToken, AggregateKind kind, bool isReadOnly)
        {
            var fields = new List<IField>();
            Expect(TokenKind.Identifier);
            Expect(TokenKind.OpenBrace);
            var definitionEnd = CurrentToken.Span;
            while (!Eat(TokenKind.CloseBrace))
            {
                IType type = new ScalarType(BaseType.Int); // for enums
                DeprecatedAttribute? deprecatedAttribute = null;
                var value = 0;

                if (kind != AggregateKind.Enum)
                {
                    type = DetermineType(CurrentToken);
                    if (type is DefinedType)
                    {
                        _typeReferences.Add((CurrentToken, definitionToken));
                    }

                    Expect(TokenKind.Identifier);
                    while (Eat(TokenKind.OpenBracket))
                    {
                        Expect(TokenKind.CloseBracket);
                        type = new ArrayType(type);
                    }
                }

                var fieldName = CurrentToken.Lexeme;
                var fieldStart = CurrentToken.Span;
                Expect(TokenKind.Identifier);

                if (kind != AggregateKind.Struct)
                {
                    Expect(TokenKind.Eq);
                    value = int.Parse(CurrentToken.Lexeme);
                    Expect(TokenKind.Number);
                }
                if (Eat(TokenKind.OpenBracket))
                {
                    Expect(TokenKind.Deprecated);
                    var message = "";
                    if (Eat(TokenKind.OpenParenthesis))
                    {
                        message = CurrentToken.Lexeme;
                        Expect(TokenKind.StringExpandable);
                        Expect(TokenKind.CloseParenthesis);
                    }
                    Expect(TokenKind.CloseBracket);
                    deprecatedAttribute = new DeprecatedAttribute(message);
                }

                var fieldEnd = CurrentToken.Span;
                Expect(TokenKind.Semicolon);
                fields.Add(new Field(fieldName, type, fieldStart.Combine(fieldEnd), deprecatedAttribute, value));
                definitionEnd = CurrentToken.Span;
            }

            var name = definitionToken.Lexeme;
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);
            var definition = new Definition(name, isReadOnly, definitionSpan, kind, fields);
            _definitions.Add(name, definition);
        }

        /// <summary>
        ///     Attempts to determine the type for the <paramref name="currentToken"/>.
        /// </summary>
        /// <param name="currentToken">the token that reflects the type to derive a type from.</param>
        /// <returns>A type code or null if none was found.</returns>
        private IType DetermineType(Token currentToken)
        {
            if (currentToken.TryParseBaseType(out var baseType))
            {
                return new ScalarType(baseType.Value);
            }
            return new DefinedType(currentToken.Lexeme);
        }
    }
}