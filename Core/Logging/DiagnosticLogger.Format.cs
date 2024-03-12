using System;
using System.Text.Json;
using Core.Exceptions;
using Core.Meta;

namespace Core.Logging;

public partial class DiagnosticLogger
{
    private string FormatDiagnostic(Diagnostic diagnostic)
    {
        var span = diagnostic.Span;
        var message = diagnostic.Message;
        if (diagnostic.Severity == Severity.Warning)
        {
            message += $" (To disable this warning, run bebopc with `--no-warn {diagnostic.ErrorCode}`)";
        }
        switch (_formatter)
        {
            case LogFormatter.MSBuild:
                var where = span == null ? ReservedWords.CompilerName : $"{span?.FileName}({span?.StartColonString(',')})";
                return $"{where} : {diagnostic.Severity.ToString().ToLowerInvariant()} BOP{diagnostic.ErrorCode}: {message}";
            case LogFormatter.JSON:
                return JsonSerializer.Serialize(diagnostic, JsonContext.Default.Diagnostic);
            case LogFormatter.Enhanced:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string FormatCompilerOutput(CompilerOutput output)
    {
        return JsonSerializer.Serialize(output, JsonContext.Default.CompilerOutput);
    }
}