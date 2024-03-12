using System.Text.Json;
using Chord.Compiler.Internal;
using Errata;
using Spectre.Console;

namespace Chord.Compiler.Extensions;

internal static class ExceptionExtensions
{

    internal static void Render(this JsonException jsonException, ISourceRepository repository, string sourcePath)
    {
        if (jsonException.LineNumber is null || jsonException.BytePositionInLine is null)
        {
            Logger.Error.WriteException(jsonException);
            return;
        }
        var location = new Location((int)jsonException.LineNumber.Value + 1, (int)jsonException.BytePositionInLine.Value);
        var report = new Report(repository);
        var labelMessage = jsonException.InnerException?.Message ?? string.Empty;
        report.AddDiagnostic(
            Diagnostic.Error(jsonException.Message)
            .WithCode("CHORD0001")
            .WithLabel(new Label(sourcePath, location, labelMessage)));
        report.Render(Logger.Error);
    }
}