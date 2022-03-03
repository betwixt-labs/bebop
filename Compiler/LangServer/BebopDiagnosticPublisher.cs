using System;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmnisharpDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;

namespace Compiler.LangServer
{
    public sealed class BebopDiagnosticPublisher
    {
        private readonly ILanguageServerFacade _facade;
        private readonly BebopLangServerLogger _logger;

        public BebopDiagnosticPublisher(
            ILanguageServerFacade facade,
            BebopLangServerLogger logger)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ClearDiagnostics(DocumentUri uri, int? version)
        {
            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = uri,
                Version = version,
                Diagnostics = new Container<OmnisharpDiagnostic>(),
            });
        }

        public void PublishDiagnostics(DocumentUri uri, int? version, IEnumerable<BebopDiagnostic> diagnostics)
        {
            // Group diagnostics by their filename
            foreach (var group in diagnostics.GroupBy(x => x.Error.Span.FileName))
            {
                // If the schema is `unknown`, assume the diagnostics come from the currently opened file
                var noSchema = group.Key == "(unknown)";
                var groupUri = noSchema ? uri : DocumentUri.FromFileSystemPath(group.Key);
                var groupVersion = noSchema ? version : null;

                _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                {
                    Uri = groupUri,
                    Version = groupVersion,
                    Diagnostics = new Container<OmnisharpDiagnostic>(
                        group.Select(d => d.ToOmnisharpDiagnostic())),
                });
            }
        }
    }
}
