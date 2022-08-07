using System;
using Core.Exceptions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmnisharpDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;

namespace Compiler.LangServer
{
    public sealed class BebopDiagnostic
    {
        public SpanException Error { get; }
        public Severity Severity { get; }

        public BebopDiagnostic(SpanException diagnostic, Severity severity)
        {
            Error = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
            Severity = severity;
        }

        public OmnisharpDiagnostic ToOmnisharpDiagnostic()
        {
            return new OmnisharpDiagnostic
            {
                Code = Error.ErrorCode,
                Severity = Severity == Severity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                Message = Error.Message,
                Range = Error.Span.GetRange(),
            };
        }
    }
}
