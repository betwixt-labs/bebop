using System;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            case LogFormatter.Structured:
                where = span == null ? "" : $"Issue located in '{span?.FileName}' at {span?.StartColonString()}: ";
                return $"[{DateTime.Now}][Compiler][{diagnostic.Severity}] {where}{message}";
            case LogFormatter.JSON:
                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
                return JsonSerializer.Serialize(diagnostic, options);
            case LogFormatter.Enhanced:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}