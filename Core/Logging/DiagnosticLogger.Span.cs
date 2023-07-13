using System;
using Core.Exceptions;
using Core.Lexer.Tokenization.Models;

namespace Core.Logging;

public partial class DiagnosticLogger
{
    public static Range GetRangeFromSpan(string schemaContents, Span span)
    {
        var lines = schemaContents.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int start = 0;
        for (int i = 0; i < span.StartLine; i++)
        {
            start += lines[i].Length + 1; // Add 1 for the newline character
        }
        start += span.StartColumn;

        int end = 0;
        for (int i = 0; i < span.EndLine; i++)
        {
            end += lines[i].Length + 1;
        }
        end += span.EndColumn;

        return start..end;
    }
    
    private string FormatSpanError(SpanException ex) => FormatDiagnostic(new(ex.Severity, ex.Message, ex.ErrorCode, ex.Span));

}