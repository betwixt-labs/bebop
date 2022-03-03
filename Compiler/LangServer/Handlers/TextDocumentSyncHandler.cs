using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Meta;
using Core.Parser;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Compiler.LangServer
{
    public sealed class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        private readonly ILanguageServerFacade _router;
        private readonly BufferManager _bufferManager;
        private readonly BebopDiagnosticPublisher _publisher;
        private readonly BebopLangServerLogger _logger;
        private readonly DocumentSelector _documentSelector;

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public TextDocumentSyncHandler(
            ILanguageServerFacade router,
            BufferManager bufferManager,
            BebopDiagnosticPublisher publisher,
            BebopLangServerLogger logger)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentSelector = new DocumentSelector(new DocumentFilter()
            {
                Pattern = "**/*.bop",
            });
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "bop");
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSyncRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                Change = TextDocumentSyncKind.Full,
            };
        }

        public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Opening document: {request.TextDocument.Uri}");

            await UpdateBufferAsync(request.TextDocument.Uri, request.TextDocument.Text, request.TextDocument.Version);
            return Unit.Value;
        }

        public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            var text = request.ContentChanges.FirstOrDefault()?.Text ?? string.Empty;
            await UpdateBufferAsync(request.TextDocument.Uri, text, request.TextDocument.Version);

            return Unit.Value;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving document: {request.TextDocument.Uri}");
            return Unit.Task;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Closing document: {request.TextDocument.Uri}");

            if (_bufferManager.RemoveBuffer(request.TextDocument.Uri, out var version, out var imports))
            {
                if (imports != null)
                {
                    // Clear diagnostics for all imports.
                    foreach (var import in imports)
                    {
                        var uri = DocumentUri.FromFileSystemPath(import);
                        if (!_bufferManager.IsOpened(uri))
                        {
                            _router.Window.LogInfo($"Clearing implicit diagnostics: {uri}");
                            _publisher.ClearDiagnostics(uri, null);
                        }
                    }
                }

                // Clear diagnostics for the file.
                _publisher.ClearDiagnostics(request.TextDocument.Uri, version);
            }
            else
            {
                _router.Window.LogError($"Could not remove buffer: {request.TextDocument.Uri}");
            }

            return Unit.Task;
        }

        private async Task UpdateBufferAsync(DocumentUri uri, string text, int? version)
        {
            try
            {
                var schema = await ParseSchemaAsync(uri, text, version);
                _bufferManager.UpdateBuffer(uri, new Buffer(schema, text, version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);

                // Update the buffer without a schema
                _logger.LogInfo($"Updating buffer (without parsed schema) for document: {uri} ({version})");
                _bufferManager.UpdateBuffer(uri, new Buffer(null, text, version));
            }
        }

        private async Task<BebopSchema> ParseSchemaAsync(DocumentUri uri, string text, int? version)
        {
            var (schema, errors) = await ParseSchemaAsync(uri, text);

            // TODO: Don't count indirect errors here.
            // If there only is indirect errors (from an import),
            // Clear the diagnostics for this file.
            // For now, clear the diagnostics before submitting.
            if (errors.Count > 0)
            {
                _publisher.ClearDiagnostics(uri, version);
                _publisher.PublishDiagnostics(uri, version, errors);
            }
            else
            {
                _publisher.ClearDiagnostics(uri, version);
            }

            return schema;
        }

        private async Task<(BebopSchema, List<BebopDiagnostic>)> ParseSchemaAsync(DocumentUri uri, string text)
        {
            var diagnostics = new List<BebopDiagnostic>();

            try
            {
                var parser = new SchemaParser(text, DocumentUri.GetFileSystemPath(uri) ?? string.Empty)
                {
                    ImportResolver = new BebopLangServerImportResolver(uri, _logger)
                };

                var schema = await parser.Parse();

                // Perform validation
                PerformValidation(ref schema);

                // Add errors and warnings
                diagnostics.AddRange(schema.Errors.Select(d => new BebopDiagnostic(d, Severity.Error)));
                diagnostics.AddRange(schema.Warnings.Select(d => new BebopDiagnostic(d, Severity.Warning)));

                return (schema, diagnostics);
            }
            catch (SpanException ex)
            {
                // Add the error as a diagnostic
                diagnostics.Add(new BebopDiagnostic(ex, Severity.Error));
            }

            // Return an empty schema
            return new(default, diagnostics);
        }

        private void PerformValidation(ref BebopSchema schema)
        {
            try
            {
                schema.Validate();
            }
            catch (Exception ex)
            {
                _router.Window.LogWarning($"Unhandled validation error: {ex.Message}");
            }
        }
    }
}