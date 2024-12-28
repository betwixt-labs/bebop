using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.IO;
using Core.Lexer.Tokenization;
using Core.Meta;
using Core.Parser.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Compiler.LangServer
{
    public sealed class HoverHandler : IHoverHandler
    {
        private BufferManager _bufferManager;
        private BebopLangServerLogger _logger;
        private CompilerHost _compilerHost;

        private static readonly Hover _emptyHoverResult = new Hover() { Contents = new MarkedStringsOrMarkupContent(new MarkupContent()) };
        public HoverHandler(BufferManager bufferManager,
            BebopLangServerLogger logger,
            CompilerHost compilerHost)
        {
            _bufferManager = bufferManager;
            _logger = logger;
            _compilerHost = compilerHost;
        }


        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions() { DocumentSelector = TextDocumentSelector.ForLanguage("bebop") };
        }

        public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {

            var buffer = _bufferManager.GetBuffer(request.TextDocument.Uri);
            if (buffer?.Schema != null)
            {
                var position = request.Position;
                var schema = buffer.Schema.Value;
                var tokenizer = new Tokenizer(SchemaReader.FromTextualSchema(buffer.Text));
                var tokens = tokenizer.Tokens.ToArray();

                bool isDefinition([NotNullWhen(true)] out Core.Meta.Definition? definition)
                {
                    var token = tokens.FirstOrDefault(token => token.Span.StartLine == position.Line && token.Span.StartColumn <= position.Character && token.Span.EndColumn >= position.Character);
                    if (string.IsNullOrWhiteSpace(token.Lexeme))
                    {
                        definition = null;
                        return false;
                    }
                    if (schema.Definitions.TryGetValue(token.Lexeme, out definition))
                    {
                        return true;
                    }
                    return false;
                }
                bool isDecorator([NotNullWhen(true)] out Core.Meta.Decorators.DecoratorDefinition? decorator)
                {
                    var token = tokens.FirstOrDefault(token => token.Span.StartLine == position.Line && token.Span.StartColumn <= position.Character && token.Span.EndColumn >= position.Character);
                    if (string.IsNullOrWhiteSpace(token.Lexeme))
                    {
                        decorator = null;
                        return false;
                    }
                    foreach (var d in _compilerHost.Decorators)
                    {
                        if (d.Identifier == token.Lexeme)
                        {
                            decorator = d;
                            return true;
                        }
                    }
                    decorator = null;
                    return false;
                }
                if (isDefinition(out var definition))
                {
                    var stringBuilder = new System.Text.StringBuilder();
                    stringBuilder.AppendLine($"**{definition.Name}**");
                    stringBuilder.AppendLine();
                    if (string.IsNullOrWhiteSpace(definition.Documentation))
                    {
                        stringBuilder.AppendLine("No documentation available.");
                    }
                    else
                    {
                        stringBuilder.AppendLine(definition.Documentation);
                    }

                    return Task.FromResult<Hover?>(new Hover()
                    {
                        Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                        {
                            Kind = MarkupKind.Markdown,
                            Value = stringBuilder.ToString()
                        })
                    });
                }
                else if (isDecorator(out var decorator))
                {
                    var stringBuilder = new System.Text.StringBuilder();
                    stringBuilder.AppendLine($"**{decorator.Identifier}**");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(decorator.Description);
                    // create a markdown table showing the parameters

                    if (decorator.Parameters is not null)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine($"**Parameters**");
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine($"| Parameter | Type | Required | Default | Description |");
                        stringBuilder.AppendLine($"| --- | --- | --- | --- | --- |");
                        foreach (var parameter in decorator.Parameters)
                        {
                            stringBuilder.AppendLine($"| {parameter.Identifier} | {parameter.Type.ToTokenString()} | {parameter.IsRequired} | {parameter.DefaultValue ?? "N/A"} | {parameter.Description} |");
                        }
                    }
                    return Task.FromResult<Hover?>(new Hover()
                    {
                        Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                        {
                            Kind = MarkupKind.Markdown,
                            Value = stringBuilder.ToString()
                        })
                    });
                }
                return Task.FromResult<Hover?>(_emptyHoverResult);
            }
            else
            {
                _logger.LogInfo($"Hover request at {request.TextDocument.Uri}");
            }
            return Task.FromResult<Hover?>(_emptyHoverResult);
        }
    }
}
