using Core.Exceptions;
using Core.Lexer.Tokenization.Models;

namespace Core.Logging;

public partial class DiagnosticLogger
{
    internal const int FileNotFound = 404;
    internal const int Unknown = 1000;
    public record Diagnostic(Severity Severity, string Message, int ErrorCode, Span? Span) { }
}