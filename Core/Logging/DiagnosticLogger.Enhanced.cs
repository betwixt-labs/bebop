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
                if (diagnostic is { Severity: Severity.Warning, Span: not null })
                {
                    errataDiagnostic.WithLabel(new Label(fileName, GetRangeFromSpan(schemaSource, diagnostic.Span.Value), diagnostic.Message).WithColor(Color.Yellow));
                }
                else if (diagnostic is { Severity: Severity.Error, Span: not null })
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
        try
        {
            string code = Markup.Escape($"[BOP{errorCode}]");

            // Write error code and exception name
            _err.MarkupLine($"[maroon]Error {code}:[/] ");
            _err.MarkupLineInterpolated($"[white]{ex.Message}[/]");

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
            if (ex is { StackTrace: null } and { InnerException: not null })
            {
                _err.WriteLine();
                _err.MarkupLine("[maroon]Inner Exception:[/]");
                if (_traceEnabled)
                {
                    _err.WriteException(ex.InnerException);
                }
                else
                {
                    _err.MarkupLineInterpolated($"[white]{ex.InnerException.Message}[/]");

                }
            }
            if (_traceEnabled && !string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                // Write exception message
                _err.WriteException(ex);
            }
        }
        catch (Exception e)
        {
            _err.WriteLine($"{errorCode}:");
            _err.WriteException(ex);
            _err.WriteLine();
            _err.WriteLine("An error occurred while rendering the exception:");
            _err.WriteException(e);
            return;
        }
    }

}