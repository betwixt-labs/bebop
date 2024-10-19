using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.IO;
using Core.Lexer.Tokenization;
using Core.Meta;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Compiler.LangServer
{
    public sealed class SemanticTokenHandler : SemanticTokensHandlerBase
    {
        private readonly BufferManager _bufferManager;
        private readonly BebopLangServerLogger _logger;
        private readonly SemanticTokensLegend _legend;

        private readonly Dictionary<TokenKind, SemanticTokenType> _tokenTypes;
        private readonly Dictionary<string, SemanticTokenType> _identifiers;

        public SemanticTokenHandler(
            BufferManager bufferManager,
            BebopLangServerLogger logger,
            CompilerHost compilerHost)
        {
            _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _legend = new SemanticTokensLegend
            {
                TokenModifiers = new Container<SemanticTokenModifier>(SemanticTokenModifier.Defaults),
                TokenTypes = new Container<SemanticTokenType>(SemanticTokenType.Defaults),
            };

            _tokenTypes = new Dictionary<TokenKind, SemanticTokenType>
            {
                { TokenKind.Mut, SemanticTokenType.Keyword  },
                { TokenKind.Array, SemanticTokenType.Keyword  },
                { TokenKind.Enum, SemanticTokenType.Keyword  },
                { TokenKind.Map, SemanticTokenType.Keyword  },
                { TokenKind.Union, SemanticTokenType.Keyword  },
                { TokenKind.Struct, SemanticTokenType.Keyword  },
                { TokenKind.Message, SemanticTokenType.Keyword },
                { TokenKind.Service, SemanticTokenType.Keyword },
                { TokenKind.Stream, SemanticTokenType.Keyword },
                { TokenKind.Number, SemanticTokenType.Number },
                { TokenKind.Decorator, SemanticTokenType.Method }
            };

            _identifiers = new Dictionary<string, SemanticTokenType>
            {
                { "true", SemanticTokenType.Keyword },
                { "false", SemanticTokenType.Keyword },
                { "const", SemanticTokenType.Keyword },
                { "opcode", SemanticTokenType.Interface },
                { "inf", SemanticTokenType.Number },
                { "nan", SemanticTokenType.Number },
                { "bool", SemanticTokenType.Type },
                { "byte", SemanticTokenType.Type },
                { "int8", SemanticTokenType.Type }, { "uint8", SemanticTokenType.Type },
                { "int16", SemanticTokenType.Type }, { "uint16", SemanticTokenType.Type },
                { "int32", SemanticTokenType.Type }, { "uint32", SemanticTokenType.Type },
                { "int64", SemanticTokenType.Type }, { "uint64", SemanticTokenType.Type },
                { "float32", SemanticTokenType.Type }, { "float64", SemanticTokenType.Type },
                { "string", SemanticTokenType.Type },
                { "guid", SemanticTokenType.Type },
                { "date", SemanticTokenType.Type },
            };

            foreach (var decorator in compilerHost.Decorators)
            {
                _identifiers[decorator.Identifier] = SemanticTokenType.Method;
            }
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        {
            return new SemanticTokensRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("bebop"),
                Legend = _legend,
                Full = new SemanticTokensCapabilityRequestFull
                {
                    Delta = false,
                },
                Range = true
            };
        }

        protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SemanticTokensDocument(_legend));
        }

        protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = _bufferManager.GetBuffer(identifier.TextDocument.Uri);
                if (buffer == null)
                {
                    return Task.CompletedTask;
                }

                var tokenizer = new Tokenizer(SchemaReader.FromTextualSchema(buffer.Text));
                var index = 0;
                while (index < tokenizer.Tokens.Count)
                {
                    var token = tokenizer.Tokens[index];

                    if (token.Kind == TokenKind.Identifier)
                    {
                        var isDefinition = false;
                        if (buffer?.Schema != null)
                        {
                            if (buffer.Schema?.Definitions?.TryGetValue(token.Lexeme, out var definition) ?? false)
                            {
                                isDefinition = true;

                                _logger.LogInfo($"Found definition {definition.Name} at {token.Span.StartLine}:{token.Span.StartColumn}");
                                builder.Push(
                               new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                               {
                                   Start = new Position(token.Span.StartLine, token.Span.StartColumn),
                                   End = new Position(token.Span.EndLine, token.Span.EndColumn)
                               },
                               SemanticTokenType.Type as SemanticTokenType?);
                            }
                        }

                        if (!isDefinition)
                        {
                            var type = SemanticTokenType.Variable;
                            if (_identifiers.TryGetValue(token.Lexeme, out var identifierType))
                            {
                                type = identifierType;
                            }

                            builder.Push(
                                new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                                {
                                    Start = new Position(token.Span.StartLine, token.Span.StartColumn),
                                    End = new Position(token.Span.EndLine, token.Span.EndColumn)
                                },
                                type as SemanticTokenType?);
                        }
                    }
                    else
                    {
                        if (_tokenTypes.TryGetValue(token.Kind, out var item))
                        {
                            builder.Push(
                                new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                                {
                                    Start = new Position(token.Span.StartLine, token.Span.StartColumn),
                                    End = new Position(token.Span.EndLine, token.Span.EndColumn)
                                },
                                item as SemanticTokenType?);
                        }
                    }

                    index++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during tokenization");
            }

            return Task.CompletedTask;
        }
    }
}
