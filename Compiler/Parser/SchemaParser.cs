using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly Dictionary<string, IDefinition> _definitions = new Dictionary<string, IDefinition>();
        /// <summary>
        /// A set of references to named types found in message/struct definitions:
        /// the left token is the type name, and the right token is the definition it's used in (used to report a helpful error).
        /// </summary>
        private readonly HashSet<(Token, Token)> _typeReferences = new HashSet<(Token, Token)>();
        private uint _index;
        private readonly string _nameSpace;
        private Token[] _tokens = new Token[] { };
        private readonly string _schemaPath;

        /// <summary>
        /// Creates a new schema parser instance from a schema file on disk
        /// </summary>
        /// <param name="inputFile">The path to the Bebop schema file that will be parsed</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(FileInfo inputFile, string nameSpace)
        {
            _lexer = SchemaLexer.FromSchemaPath(inputFile.FullName);
            _nameSpace = nameSpace;
            _schemaPath = inputFile.FullName;
        }

        /// <summary>
        /// Creates a new schema parser instance and loads the schema into memory
        /// </summary>
        /// <param name="textualSchema">A string representation of a schema.</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(string textualSchema, string nameSpace)
        {
            _lexer = SchemaLexer.FromTextualSchema(textualSchema);
            _nameSpace = nameSpace;
            _schemaPath = string.Empty;
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

            // don't throw on block comment tokens
            if (CurrentToken.Kind == TokenKind.BlockComment)
            {
                ConsumeBlockComments();
            }
            if (!Eat(kind))
            {
                throw new UnexpectedTokenException(kind, CurrentToken, _schemaPath);
            }
        }

        /// <summary>
        /// Consume all sequential block comments.
        /// </summary>
        /// <returns>The content of the last block comment which usually proceeds a definition.</returns>
        private string ConsumeBlockComments()
        {

            var definitionDocumentation = string.Empty;
            if (CurrentToken.Kind == TokenKind.BlockComment)
            {
                while (CurrentToken.Kind == TokenKind.BlockComment)
                {
                    definitionDocumentation = CurrentToken.Lexeme;
                    Eat(TokenKind.BlockComment);
                }
            }
            return definitionDocumentation;
        }


        /// <summary>
        ///     Evaluates a schema and parses it into a <see cref="ISchema"/> object
        /// </summary>
        /// <returns></returns>
        public async Task<ISchema> Evaluate()
        {
            await Tokenize();
            _index = 0;
            _definitions.Clear();
            _typeReferences.Clear();


            while (_index < _tokens.Length && !Eat(TokenKind.EndOfFile))
            {

                var definitionDocumentation = ConsumeBlockComments();

                var isReadOnly = Eat(TokenKind.ReadOnly);
                var kind = CurrentToken switch
                {
                    _ when Eat(TokenKind.Enum) => AggregateKind.Enum,
                    _ when Eat(TokenKind.Struct) => AggregateKind.Struct,
                    _ when Eat(TokenKind.Message) => AggregateKind.Message,
                    _ => throw new UnexpectedTokenException(TokenKind.Message, CurrentToken, _schemaPath)
                };
                DeclareAggregateType(CurrentToken, kind, isReadOnly, definitionDocumentation);
            }
            foreach (var (typeToken, definitionToken) in _typeReferences)
            {
                if (!_definitions.ContainsKey(typeToken.Lexeme))
                {
                    throw new UnrecognizedTypeException(typeToken, definitionToken.Lexeme, _schemaPath);
                }
            }
            return new BebopSchema(_schemaPath, _nameSpace, _definitions);
        }


        /// <summary>
        ///     Declares an aggregate data structure and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that names the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        /// <param name="isReadOnly"></param>
        /// <param name="definitionDocumentation"></param>
        private void DeclareAggregateType(Token definitionToken, AggregateKind kind, bool isReadOnly, string definitionDocumentation)
        {
            var fields = new List<IField>();

            Expect(TokenKind.Identifier);
            Expect(TokenKind.OpenBrace);
            var definitionEnd = CurrentToken.Span;
            while (!Eat(TokenKind.CloseBrace))
            {
                DeprecatedAttribute? deprecatedAttribute = null;
                var value = 0;

                var fieldDocumentation = ConsumeBlockComments();
                // if we've reached the end of the definition after parsing documentation we need to exit.
                if (Eat(TokenKind.CloseBrace))
                {
                    break;
                }

                TypeBase type = kind == AggregateKind.Enum
                    ? new ScalarType(BaseType.UInt32, definitionToken.Span, definitionToken.Lexeme)
                    : ParseType(definitionToken);

                Console.WriteLine(CurrentToken.Lexeme);
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
                fields.Add(new Field(fieldName, type, fieldStart.Combine(fieldEnd), deprecatedAttribute, value, fieldDocumentation));
                definitionEnd = CurrentToken.Span;
            }

            var name = definitionToken.Lexeme;
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);
            var definition = new Definition(name, isReadOnly, definitionSpan, kind, fields, definitionDocumentation);
            if (_definitions.ContainsKey(name))
            {
                throw new MultipleDefinitionsException(definition, _schemaPath);
            }
            _definitions.Add(name, definition);
        }

        /// <summary>
        ///     Parse a type name.
        /// </summary>
        /// <param name="definitionToken">
        ///     The token acting as a representative for the definition we're in.
        ///     <para/>
        ///     This is used to track how DefinedTypes are referenced in definitions, and give useful error messages.
        /// </param>
        /// <returns>An IType object.</returns>
        private TypeBase ParseType(Token definitionToken)
        {
            TypeBase type;
            Span span = CurrentToken.Span;
            if (Eat(TokenKind.Map))
            {
                // Parse "map[k, v]".
                Expect(TokenKind.OpenBracket);
                var keyType = ParseType(definitionToken);
                if (!IsValidMapKeyType(keyType))
                {
                    throw new InvalidMapKeyTypeException(keyType, _schemaPath);
                }
                Expect(TokenKind.Comma);
                var valueType = ParseType(definitionToken);
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket);
                type = new MapType(keyType, valueType, span, $"map[{keyType.AsString}, {valueType.AsString}]");
            }
            else if (Eat(TokenKind.Array))
            {
                // Parse "array[v]".
                Expect(TokenKind.OpenBracket);
                var valueType = ParseType(definitionToken);
                Expect(TokenKind.CloseBracket);
                type = new ArrayType(valueType, span, $"array[{valueType.AsString}]");
            }
            else if (CurrentToken.TryParseBaseType(out var baseType))
            {
                type = new ScalarType(baseType!.Value, span, CurrentToken.Lexeme);
                // Consume the matched basetype name as an identifier:
                Expect(TokenKind.Identifier);
            }
            else
            {
                type = new DefinedType(CurrentToken.Lexeme, span, CurrentToken.Lexeme);
                _typeReferences.Add((CurrentToken, definitionToken));
                // Consume the type name as an identifier:
                Expect(TokenKind.Identifier);
            }

            // Parse any postfix "[]".
            while (Eat(TokenKind.OpenBracket))
            {
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket);
                type = new ArrayType(type, span, $"{type.AsString}[]");
            }

            return type;
        }

        private static bool IsValidMapKeyType(TypeBase keyType)
        {
            return keyType is ScalarType;
        }
    }
}
