using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.IO;
using Core.Lexer;
using Core.Lexer.Extensions;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;
using Core.Meta;
using Core.Meta.Attributes;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;
using Core.Parser.Extensions;

namespace Core.Parser
{
    public class SchemaParser
    {
        private readonly HashSet<TokenKind> _topLevelDefinitionKinds = new() { TokenKind.Enum, TokenKind.Struct, TokenKind.Message, TokenKind.Union };
        /// <summary>
        /// Tokens we can use to recover from practically anywhere.
        /// </summary>
        private readonly HashSet<TokenKind> _universalFollowKinds = new() { TokenKind.Enum, TokenKind.Struct, TokenKind.Message, TokenKind.Union, TokenKind.EndOfFile };
        private readonly Stack<List<Definition>> _scopes = new();
        private readonly Tokenizer _tokenizer;
        private readonly Dictionary<string, Definition> _definitions = new();
        /// <summary>
        /// A set of references to named types found in message/struct definitions:
        /// the left token is the type name, and the right token is the definition it's used in (used to report a helpful error).
        /// </summary>
        private readonly HashSet<(Token, Token)> _typeReferences = new();
        private int _index;
        private readonly string _nameSpace;
        private List<SpanException> _errors = new();
        private List<Token> _tokens => _tokenizer.Tokens;

        /// <summary>
        /// Add a definition to the current scope and the primary definitions map.
        /// </summary>
        /// <param name="name">Definition name.</param>
        /// <param name="definition"></param>
        private void AddDefinition(string name, Definition definition)
        {
            if (_definitions.ContainsKey(name)) return;
            _definitions.Add(name, definition);
            if (_scopes.Count > 0)
            {
                _scopes.Peek().Add(definition);
            }
        }

        /// <summary>
        /// Open a scope. All definitions added from this point will be added to the scope.
        /// </summary>
        private void StartScope() => _scopes.Push(new List<Definition>());

        /// <summary>
        /// Close the current scope, setting the parent of every enclosed definition to the provided Definition and adding the Definition at the end, in the next scope down.
        /// </summary>
        /// <param name="parent">Definition to take ownership of all enclosed definitions and push to the enclosing scope.</param>
        private void CloseScope(string name, Definition parent)
        {
            var scope = _scopes.Pop();
            foreach (var definition in scope)
            {
                definition.Parent = parent;
            }
            AddDefinition(name, parent);
        }

        private void CancelScope()
        {
            _scopes.Pop();
        }

        /// <summary>
        /// Creates a new schema parser instance from some schema files on disk.
        /// </summary>
        /// <param name="schemaPaths">The Bebop schema files that will be parsed</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(List<string> schemaPaths, string nameSpace)
        {
            _tokenizer = new Tokenizer(SchemaReader.FromSchemaPaths(schemaPaths));
            _nameSpace = nameSpace;
        }

        /// <summary>
        /// Creates a new schema parser instance and loads the schema into memory.
        /// </summary>
        /// <param name="textualSchema">A string representation of a schema.</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(string textualSchema, string nameSpace)
        {
            _tokenizer = new Tokenizer(SchemaReader.FromTextualSchema(textualSchema));
            _nameSpace = nameSpace;
        }

        /// <summary>
        ///     Gets the <see cref="Token"/> at the current <see cref="_index"/>.
        /// </summary>
        private Token CurrentToken => _tokens[_index];

        /// <summary>
        ///     Peeks a token at the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token PeekToken(int index) => _tokens[index];

        /// <summary>
        ///     Sets the current token stream position to the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token Base(int index)
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
        /// Like <see cref="Eat"/>, but matches an "identifier" token equal to the given keyword.
        /// For backwards compatibility, this is a little nicer than introducing a new keyword token that breaks old schemas (like "int32 import;").
        /// </summary>
        private bool EatPseudoKeyword(string keyword)
        {
            if (CurrentToken.Kind == TokenKind.Identifier && CurrentToken.Lexeme == keyword)
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
        private void Expect(TokenKind kind, string? hint = null)
        {
            // don't throw on block comment tokens
            ConsumeBlockComments();
            if (!Eat(kind))
            {
                throw new UnexpectedTokenException(kind, CurrentToken, hint);
            }
        }

        /// <summary>
        /// Expect a token of a certain kind. If not present, skip until it or one of a set of additional tokens is matched. Does NOT consume the token.
        /// </summary>
        /// <param name="kinds"></param>
        /// <param name="additionalTokens"></param>
        /// <param name="hint"></param>
        private void ExpectAndSkip(TokenKind kind, HashSet<TokenKind>? additionalTokens = null, string? hint = null)
        {
            ExpectAndSkip(new HashSet<TokenKind>() { kind }, additionalTokens, hint);
        }

        /// <summary>
        /// Expect a token of a certain set of kinds. If not present, skip until it or one of a set of additional tokens is matched. Does NOT consume the token.
        /// </summary>
        /// <param name="kinds"></param>
        /// <param name="additionalTokens"></param>
        /// <param name="hint"></param>
        private void ExpectAndSkip(HashSet<TokenKind> kinds, HashSet<TokenKind>? additionalTokens = null, string? hint = null)
        {
            additionalTokens ??= new();
            ConsumeBlockComments();
            if (kinds.Contains(CurrentToken.Kind)) return;
            _errors.Add(new UnexpectedTokenException(kinds, CurrentToken, hint));
            while (_index < _tokens.Count - 1 && !kinds.Contains(CurrentToken.Kind) && !additionalTokens.Contains(CurrentToken.Kind))
            {
                _index++;
            }
        }

        private void SkipAndSkipUntil(HashSet<TokenKind> kinds)
        {
            // Always advance by one.
            if (_index < _tokens.Count - 1)
            {
                _index++;
            }
            while (_index < _tokens.Count - 1 && !kinds.Contains(CurrentToken.Kind))
            {
                _index++;
            }
        }

        private string ExpectLexeme(TokenKind kind, string? hint = null)
        {
            var lexeme = CurrentToken.Lexeme;
            Expect(kind, hint);
            return lexeme;
        }

        /// <summary>
        /// Expect any string literal token, and return its contents.
        /// </summary>
        private string ExpectStringLiteral()
        {
            ConsumeBlockComments();
            if (CurrentToken.Kind == TokenKind.String)
            {
                return _tokens[_index++].Lexeme;
            }
            else
            {
                throw new UnexpectedTokenException(TokenKind.String, CurrentToken);
            }
        }

        /// <summary>
        /// Consume all sequential block comments.
        /// </summary>
        /// <returns>The content of the last block comment which usually proceeds a definition.</returns>
        private string ConsumeBlockComments()
        {

            var definitionDocumentation = string.Empty;
            while (CurrentToken.Kind == TokenKind.BlockComment)
            {
                definitionDocumentation = CurrentToken.Lexeme;
                _index++;
            }
            return definitionDocumentation;
        }


        /// <summary>
        ///     Parse the current input files into an <see cref="ISchema"/> object.
        /// </summary>
        /// <returns></returns>
        public async Task<ISchema> Parse()
        {
            _index = 0;
            _errors.Clear();
            _definitions.Clear();
            _typeReferences.Clear();


            while (_index < _tokens.Count)
            {
                if (Eat(TokenKind.EndOfFile)) continue;
                if (EatPseudoKeyword("import"))
                {
                    var currentFilePath = CurrentToken.Span.FileName;
                    
                    var currentFileDirectory = Path.GetDirectoryName(currentFilePath)!;
       
                    var pathToken = CurrentToken;
                    var relativePathFromCurrent = ExpectStringLiteral();
                    var combinedPath = Path.Combine(currentFileDirectory, relativePathFromCurrent);
                    try
                    {
                        await _tokenizer.AddFile(combinedPath);
                    }
                    catch (IOException)
                    {
                        throw File.Exists(combinedPath) ? new ImportFileReadException(pathToken) : new ImportFileNotFoundException(pathToken);
                    }
                }
                else
                {
                    try
                    {
                        ParseDefinition();
                    }
                    catch (SpanException e)
                    {
                        // Failsafe recovery: on thrown exception, trash scope stack and skip to end of file.
                        _errors.Add(e);
                        _scopes.Clear();
                        while (CurrentToken.Kind != TokenKind.EndOfFile)
                        {
                            _index++;
                        }
                    }
                }
            }
            return new BebopSchema(
                nameSpace: _nameSpace, 
                definitions: _definitions, 
                typeReferences: _typeReferences, 
                parsingErrors: _errors
            );
        }

        private Definition? ParseDefinition()
        {
            var definitionDocumentation = ConsumeBlockComments();

            if (EatPseudoKeyword("const"))
            {
                return ParseConstDefinition(definitionDocumentation);
            }
            BaseAttribute? opcodeAttribute = null;

            try
            {
                opcodeAttribute = EatAttribute(TokenKind.Opcode);
            }
            catch (SpanException e)
            {
                // If there's a syntax error in the attribute, we'll be skipping ahead to the next top level definition anyway.
                _errors.Add(e);
            }

            var isReadOnly = Eat(TokenKind.ReadOnly);

            ExpectAndSkip(_topLevelDefinitionKinds, _universalFollowKinds, hint: "Expecting a top-level definition.");
            if (CurrentToken.Kind == TokenKind.EndOfFile)
            {
                // We probably want to start over if we hit the end of the file.
                return null;
            }
            if (Eat(TokenKind.Union))
            {
                return ParseUnionDefinition(CurrentToken, definitionDocumentation, opcodeAttribute);
            }
            else
            {
                var kind = CurrentToken switch
                {
                    _ when Eat(TokenKind.Enum) => AggregateKind.Enum,
                    _ when Eat(TokenKind.Struct) => AggregateKind.Struct,
                    _ when Eat(TokenKind.Message) => AggregateKind.Message,
                    _ => throw new UnexpectedTokenException(TokenKind.Message, CurrentToken)
                };
                ExpectAndSkip(TokenKind.Identifier, _universalFollowKinds);
                if (CurrentToken.Kind != TokenKind.Identifier)
                {
                    // Uh oh we skipped ahead due to a missing identifier, get outta there
                    return null;
                }
                return ParseNonUnionDefinition(CurrentToken, kind, isReadOnly, definitionDocumentation, opcodeAttribute);
            }
        }

        private ConstDefinition ParseConstDefinition(string definitionDocumentation)
        {
            var definitionStart = CurrentToken.Span;
            
            TypeBase type;
            string name = "";
            Literal? value = new IntegerLiteral(new ScalarType(BaseType.UInt32, new Span(), ""), new Span(), "") ;
            try
            {
                type = ParseType(CurrentToken);
                name = ExpectLexeme(TokenKind.Identifier);
                Expect(TokenKind.Eq, hint: "A constant definition looks like: const uint32 pianoKeys = 88;");
                value = ParseLiteral(type);
            }
            catch (SpanException e)
            {
                _errors.Add(e);
            }
            ExpectAndSkip(TokenKind.Semicolon, hint: "A constant definition must end in a semicolon: const uint32 pianoKeys = 88;");
            Eat(TokenKind.Semicolon);
            var definitionSpan = definitionStart.Combine(CurrentToken.Span);
            var definition = new ConstDefinition(name, definitionSpan, definitionDocumentation, value);
            if (_definitions.ContainsKey(name))
            {
               _errors.Add(new DuplicateConstDefinitionException(definition));
            }
            else
            {
                AddDefinition(name, definition);
            }
            return definition;
        }

        private readonly Regex _reFloat = new(@"^(-?inf|nan|-?\d+(\.\d*)?(e-?\d+)?)$");
        private readonly Regex _reInteger = new(@"^-?(0[xX][0-9a-fA-F]+|\d+)$");
        private readonly Regex _reGuid = new(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");

        private (Span, string) ParseNumberLiteral()
        {
            var startToken = CurrentToken;
            var hyphen = "";
            if (Eat(TokenKind.Hyphen)) hyphen = "-";
            var numberToken = CurrentToken;
            if (!Eat(TokenKind.Number)) Expect(TokenKind.Identifier);
            var span = startToken.Span.Combine(numberToken.Span);
            return (span, hyphen + numberToken.Lexeme);
        }

        private Literal ParseLiteral(TypeBase type)
        {
            var token = CurrentToken;
            switch (type)
            {
                case ArrayType:
                    throw new UnsupportedConstTypeException("Array-typed constant definitions are not supported.", type.Span);
                case MapType:
                    throw new UnsupportedConstTypeException("Map-typed constant definitions are not supported.", type.Span);
                case DefinedType:
                    throw new UnsupportedConstTypeException("User-defined-type constant definitions are not supported.", type.Span);
                case ScalarType st when st.BaseType == BaseType.Date:
                    throw new UnsupportedConstTypeException("Date-type constant definitions are not supported.", type.Span);
                case ScalarType st when st.IsFloat:
                    var (floatSpan, floatLexeme) = ParseNumberLiteral();
                    if (!_reFloat.IsMatch(floatLexeme)) throw new InvalidLiteralException(token, st);
                    return new FloatLiteral(st, floatSpan, floatLexeme);
                case ScalarType st when st.IsInteger:
                    var (intSpan, intLexeme) = ParseNumberLiteral();
                    if (!_reInteger.IsMatch(intLexeme)) throw new InvalidLiteralException(token, st);
                    return new IntegerLiteral(st, intSpan, intLexeme);
                case ScalarType st when st.BaseType == BaseType.Bool:
                    Expect(TokenKind.Identifier);
                    return token.Lexeme switch
                    {
                        "true" => new BoolLiteral(st, token.Span, true),
                        "false" => new BoolLiteral(st, token.Span, false),
                        _ => throw new InvalidLiteralException(token, st),
                    };
                case ScalarType st when st.BaseType == BaseType.String:
                    ExpectStringLiteral();
                    return new StringLiteral(st, token.Span, token.Lexeme);
                case ScalarType st when st.BaseType == BaseType.Guid:
                    ExpectStringLiteral();
                    if (!_reGuid.IsMatch(token.Lexeme)) throw new InvalidLiteralException(token, st);
                    return new GuidLiteral(st, token.Span, Guid.Parse(token.Lexeme));
                default:
                    throw new UnsupportedConstTypeException($"Constant definitions for type {type.AsString} are not supported.", type.Span);
            }
        }

        /// <summary>
        /// Consumes all the tokens belonging to an attribute
        /// </summary>
        /// <param name="kind">The kind of attribute that will be eaten</param>
        /// <returns>An instance of the attribute.</returns>
        private BaseAttribute? EatAttribute(TokenKind kind)
        {
            if (Eat(TokenKind.OpenBracket))
            {
                Expect(kind);
                var value = string.Empty;
                var isNumber = false;
                if (Eat(TokenKind.OpenParenthesis))
                {
                    value = CurrentToken.Lexeme;
                    if (Eat(TokenKind.String)|| (kind == TokenKind.Opcode && Eat(TokenKind.Number)))
                    {
                        isNumber = PeekToken(_index - 1).Kind == TokenKind.Number;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(TokenKind.String, CurrentToken);
                    }
                    Expect(TokenKind.CloseParenthesis);
                }
                Expect(TokenKind.CloseBracket);
                return kind switch
                {
                    TokenKind.Deprecated => new DeprecatedAttribute(value),
                    TokenKind.Opcode => new OpcodeAttribute(value, isNumber),
                    _ => throw new UnexpectedTokenException(kind, CurrentToken)
                };
            }
            return null;
        }


        /// <summary>
        ///     Parses a non-union data structure and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that names the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        /// <param name="isReadOnly"></param>
        /// <param name="definitionDocumentation"></param>
        /// <param name="opcodeAttribute"></param>
        /// <returns>The parsed definition.</returns>
        private Definition? ParseNonUnionDefinition(Token definitionToken,
            AggregateKind kind,
            bool isReadOnly,
            string definitionDocumentation,
            BaseAttribute? opcodeAttribute)
        {
            
            var fields = new List<IField>();
            var kindName = kind switch { AggregateKind.Enum => "enum", AggregateKind.Struct => "struct", _ => "message" };
            var aKindName = kind switch { AggregateKind.Enum => "an enum", AggregateKind.Struct => "a struct", _ => "a message" };

            ExpectAndSkip(TokenKind.Identifier, new(_universalFollowKinds.Append(TokenKind.OpenBrace)), hint: $"Did you forget to specify a name for this {kindName}?");
            Eat(TokenKind.Identifier);
            ExpectAndSkip(TokenKind.OpenBrace, new(_universalFollowKinds.Append(TokenKind.CloseBrace)));
            if (!Eat(TokenKind.OpenBrace))
            {
                // Now we're in trouble. There was a top level definition/EOF before the next open brace. Eject!
                return null;
            }

            var definitionEnd = CurrentToken.Span;
            var messageFieldFollowKinds = new HashSet<TokenKind> { TokenKind.BlockComment, TokenKind.CloseBrace, TokenKind.OpenBracket, TokenKind.Number };
            var fieldFollowKinds = new HashSet<TokenKind>() { TokenKind.BlockComment, TokenKind.CloseBrace, TokenKind.OpenBracket, TokenKind.Number, TokenKind.Map, TokenKind.Array };
            // var fieldStoppingKinds = new HashSet<TokenKind>(_universalFollowKinds.Concat(fieldFollowKinds));
            var errored = false;
            StartScope();
            while (!Eat(TokenKind.CloseBrace))
            {
                //if (kind == AggregateKind.Message)
                //{
                //    ExpectAndSkip(messageFieldFollowKinds, _universalFollowKinds, hint: "Invalid field.");
                //}
                //ExpectAndSkip(fieldFollowKinds, _universalFollowKinds, hint: "Invalid field.");
                if (errored && !fieldFollowKinds.Contains(CurrentToken.Kind))
                {
                    // This token could not possibly be valid in any definition, skip the whole thing.
                    CancelScope();
                    return null;
                }
                errored = false;
                var value = 0u;

                var fieldDocumentation = ConsumeBlockComments();
                // if we've reached the end of the definition after parsing documentation we need to exit.
                if (Eat(TokenKind.CloseBrace))
                {
                    break;
                }

                var deprecatedAttribute = EatAttribute(TokenKind.Deprecated);

                if (kind == AggregateKind.Message)
                {
                    try
                    {
                        const string? indexHint = "Fields in a message must be explicitly indexed: message A { 1 -> string s; 2 -> bool b; }";
                        var indexLexeme = CurrentToken.Lexeme;
                        Expect(TokenKind.Number, hint: indexHint);
                        if (!indexLexeme.TryParseUInt(out value))
                        {
                            throw new UnexpectedTokenException(TokenKind.Number, CurrentToken, "Field index must be an unsigned integer.");
                        }
                        // Parse an arrow ("->").
                        Expect(TokenKind.Hyphen, hint: indexHint);
                        Expect(TokenKind.CloseCaret, hint: indexHint);
                    }
                    catch (SpanException e)
                    {
                        errored = true;
                        _errors.Add(e);
                        SkipAndSkipUntil(new(messageFieldFollowKinds.Concat(_universalFollowKinds)));
                        continue;
                    }
                }

                try
                {

                
                    // Parse a type name, if this isn't an enum:
                    TypeBase type = kind == AggregateKind.Enum
                        ? new ScalarType(BaseType.UInt32, definitionToken.Span, definitionToken.Lexeme)
                        : ParseType(definitionToken);

                    var fieldName = CurrentToken.Lexeme;
                    if (TokenizerExtensions.GetKeywords().Contains(fieldName))
                    {
                        throw new ReservedIdentifierException(fieldName, CurrentToken.Span);
                    }
                    if (fieldName.Equals(definitionToken.Lexeme, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new FieldNameException(CurrentToken);
                    }
                    var fieldStart = CurrentToken.Span;

                    Expect(TokenKind.Identifier);

                    if (kind == AggregateKind.Enum)
                    {
                        Expect(TokenKind.Eq, hint: "Every constant in an enum must have an explicit literal value.");
                        var valueLexeme = CurrentToken.Lexeme;
                        Expect(TokenKind.Number, hint: "An enum constant must have a literal unsigned integer value.");
                        if (!valueLexeme.TryParseUInt(out value))
                        {
                            throw new UnexpectedTokenException(TokenKind.Number, CurrentToken, "Enum constant must be an unsigned integer.");
                        }
                    }


                    var fieldEnd = CurrentToken.Span;
                    Expect(TokenKind.Semicolon, hint: CurrentToken.Kind == TokenKind.OpenBracket
                        ? "Try 'Type[] foo' instead of 'Type foo[]'."
                        : $"Elements in {aKindName} are delimited using semicolons.");
                    fields.Add(new Field(fieldName, type, fieldStart.Combine(fieldEnd), deprecatedAttribute, value, fieldDocumentation));
                    definitionEnd = CurrentToken.Span;
                }
                catch (SpanException e)
                {
                    errored = true;
                    _errors.Add(e);
                    SkipAndSkipUntil(new(fieldFollowKinds.Concat(_universalFollowKinds)));
                    continue;
                }
            }

            var name = definitionToken.Lexeme;
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);

            Definition definition = kind switch
            {
                AggregateKind.Enum => new EnumDefinition(name, definitionSpan, definitionDocumentation, fields),
                AggregateKind.Struct => new StructDefinition(name, definitionSpan, definitionDocumentation, opcodeAttribute, fields, isReadOnly),
                AggregateKind.Message => new MessageDefinition(name, definitionSpan, definitionDocumentation, opcodeAttribute, fields),
                _ => throw new InvalidOperationException("invalid kind when making definition"),
            };

            if (isReadOnly && definition is not StructDefinition)
            {
                _errors.Add(new InvalidReadOnlyException(definition));
            }
            if (opcodeAttribute != null && definition is not TopLevelDefinition)
            {
                _errors.Add(new InvalidOpcodeAttributeUsageException(definition));
            }

            if (_definitions.ContainsKey(name))
            {
                _errors.Add(new MultipleDefinitionsException(definition));
                CancelScope();
            }
            else
            {
                CloseScope(name, definition);
            }
            return definition;
        }

        /// <summary>
        ///     Parses a union definition and adds it to the <see cref="_definitions"/> collection.
        /// </summary>
        /// <param name="definitionToken">The token that names the union to define.</param>
        /// <param name="definitionDocumentation">The documentation above the union definition.</param>
        /// <param name="opcodeAttribute">The opcode attribute above the union definition, if any.</param>
        /// <returns>The parsed union definition.</returns>
        private Definition? ParseUnionDefinition(Token definitionToken,
            string definitionDocumentation,
            BaseAttribute? opcodeAttribute)
        {
            ExpectAndSkip(TokenKind.Identifier, new(_universalFollowKinds.Append(TokenKind.OpenBrace)), hint: $"Did you forget to specify a name for this union?");
            Eat(TokenKind.Identifier);
            ExpectAndSkip(TokenKind.OpenBrace, new(_universalFollowKinds.Append(TokenKind.CloseBrace)));
            if (!Eat(TokenKind.OpenBrace))
            {
                return null;
            }
            StartScope();
            var name = definitionToken.Lexeme;
            var branches = new List<UnionBranch>();
            var usedDiscriminators = new HashSet<uint>();

            
            var definitionEnd = CurrentToken.Span;
            var errored = false;
            var unionFieldFollowKinds = new HashSet<TokenKind>() { TokenKind.BlockComment, TokenKind.Number, TokenKind.CloseBrace };
            while (!Eat(TokenKind.CloseBrace))
            {
                if (errored && !unionFieldFollowKinds.Contains(CurrentToken.Kind))
                {
                    CancelScope();
                    return null;
                }
                errored = false;
                var documentation = ConsumeBlockComments();
                // if we've reached the end of the definition after parsing documentation we need to exit.
                if (Eat(TokenKind.CloseBrace))
                {
                    break;
                }

                const string? indexHint = "Branches in a union must be explicitly indexed: union U { 1 -> struct A{}  2 -> message B{} }";
                var indexToken = CurrentToken;
                var indexLexeme = indexToken.Lexeme;
                uint discriminator;
                try
                {
                    Expect(TokenKind.Number, hint: indexHint);
                    if (!indexLexeme.TryParseUInt(out discriminator))
                    {
                        throw new UnexpectedTokenException(TokenKind.Number, indexToken, "A union branch discriminator must be an unsigned integer.");
                    }
                    if (discriminator < 1 || discriminator > 255)
                    {
                        _errors.Add(new UnexpectedTokenException(TokenKind.Number, indexToken, "A union branch discriminator must be between 1 and 255."));
                    }
                    if (usedDiscriminators.Contains(discriminator))
                    {
                        _errors.Add(new DuplicateUnionDiscriminatorException(indexToken, name));
                    }
                    usedDiscriminators.Add(discriminator);
                    // Parse an arrow ("->").
                    Expect(TokenKind.Hyphen, hint: indexHint);
                    Expect(TokenKind.CloseCaret, hint: indexHint);
                }
                catch (SpanException e)
                {
                    _errors.Add(e);
                    errored = true;
                    SkipAndSkipUntil(new(unionFieldFollowKinds.Concat(_universalFollowKinds)));
                    continue;
                }
                var definition = ParseDefinition();
                if (definition is null)
                {
                    // Just escape out of there if there's a parsing error in one of the definitions.
                    CancelScope();
                    return null;
                }
                if (definition is not TopLevelDefinition td)
                {
                    _errors.Add(new InvalidUnionBranchException(definition));
                    return null;
                }
                // Parsing can continue if it's a nested union, but the rest of this method depends on it being a top level definition
                if (definition is UnionDefinition)
                {
                    _errors.Add(new InvalidUnionBranchException(definition));
                }
                td.DiscriminatorInParent = (byte)discriminator;
                Eat(TokenKind.Semicolon);
                definitionEnd = CurrentToken.Span;
                if (string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    definition.Documentation = documentation;
                }
                branches.Add(new UnionBranch((byte)discriminator, td));
            }
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);
            var unionDefinition = new UnionDefinition(name, definitionSpan, definitionDocumentation, opcodeAttribute, branches);
            CloseScope(name, unionDefinition);
            if (unionDefinition.Branches.Count == 0)
            {
                _errors.Add(new EmptyUnionException(unionDefinition));
                // throw new EmptyUnionException(unionDefinition);
            }
            return unionDefinition;
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
                var mapHint = "The syntax for map types is: map[KeyType, ValueType].";
                Expect(TokenKind.OpenBracket, hint: mapHint);
                var keyType = ParseType(definitionToken);
                if (!IsValidMapKeyType(keyType))
                {
                    // No problem parsing past this, just add it to the errors list
                    _errors.Add(new InvalidMapKeyTypeException(keyType));
                }
                Expect(TokenKind.Comma, hint: mapHint);
                var valueType = ParseType(definitionToken);
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket, hint: mapHint);
                type = new MapType(keyType, valueType, span, $"map[{keyType.AsString}, {valueType.AsString}]");
            }
            else if (Eat(TokenKind.Array))
            {
                // Parse "array[v]".
                var arrayHint = "The syntax for array types is either array[Type] or Type[].";
                Expect(TokenKind.OpenBracket, hint: arrayHint);
                var valueType = ParseType(definitionToken);
                Expect(TokenKind.CloseBracket, hint: arrayHint);
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
                Expect(TokenKind.CloseBracket, hint: "The syntax for array types is T[]. You can't specify a fixed size.");
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
