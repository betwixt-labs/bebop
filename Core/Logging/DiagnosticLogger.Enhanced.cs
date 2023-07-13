using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Exceptions;
using Errata;
using Spectre.Console;

namespace Core.Logging;

public partial class DiagnosticLogger
{

    private void RenderEnhancedSpanErrors(List<SpanException> exs)
    {
        var report = new Report(new SchemaRepo());
        var groupedExceptions = exs.GroupBy(spanException => new
        {
            spanException.Span.FileName,
            spanException.ErrorCode,
            spanException.Severity,
            ExceptionType = spanException.GetType().Name
        }).ToList();
        foreach (var group in groupedExceptions)
        {
            string fileName = group.Key.FileName;
            var severity = group.Key.Severity;
            var schemaSource = File.ReadAllText(fileName);
            string errorCode = $"BOP{group.Key.ErrorCode}";
            var exceptionName = group.Key.ExceptionType;

            var errataDiagnostic = severity == Severity.Warning ? Errata.Diagnostic.Warning(exceptionName) : Errata.Diagnostic.Error(exceptionName);
            errataDiagnostic.WithCode(errorCode);
            foreach (var ex in group)
            {
                var diagnostic = new Diagnostic(ex.Severity, ex.Message, ex.ErrorCode, ex.Span);
                if (diagnostic.Severity == Severity.Warning)
                {
                    errataDiagnostic.WithLabel(new Label(fileName, GetRangeFromSpan(schemaSource, diagnostic.Span.Value), diagnostic.Message).WithColor(Color.Yellow));
                }
                else if (diagnostic.Severity == Severity.Error)
                {
                    errataDiagnostic.WithLabel(new Label(fileName, GetRangeFromSpan(schemaSource, diagnostic.Span.Value), diagnostic.Message).WithColor(Color.Red));
                }
            }
            report.AddDiagnostic(errataDiagnostic);
        }
        report.Render(_err);
    }

    public void WriteTable(Table table)
    {
        _out.Write(table);
    }

    private void RenderEnhancedException(Exception ex, int errorCode)
    {
        string code = Markup.Escape($"[BOP{errorCode}]");

        // Write error code and exception name
        _err.Markup($"[red bold]Error {code}:[/] ");
        _err.MarkupLine($"[white]{ex.Message}[/]");

        // Write file path if FileNotFoundException
        if (ex is FileNotFoundException fileNotFoundException)
        {
            var filePath = new TextPath(fileNotFoundException?.FileName ?? "[unknown]")
            {
                StemStyle = Style.Parse("white"),
                LeafStyle = Style.Parse("white")
            };
            _err.WriteLine();
            _err.Write("File: ");
            _err.Write(filePath);
            _err.WriteLine();
        }
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            // Write exception message
            _err.WriteException(ex);
        }
    }

}