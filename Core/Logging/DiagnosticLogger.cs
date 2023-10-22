using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Exceptions;
using Spectre.Console;

namespace Core.Logging;

public partial class DiagnosticLogger
{
    private static DiagnosticLogger? _instance;
    private readonly LogFormatter _formatter;
    private readonly IAnsiConsole _out;
    private readonly IAnsiConsole _err;
    public IAnsiConsole Out => _out;
    public IAnsiConsole Error => _err;

    #region Static Methods
    private DiagnosticLogger(LogFormatter formatter)
    {
        _formatter = formatter;
        var isWasm = Environment.GetEnvironmentVariable("WASM") is not null;
        if (isWasm)
        {
            _out = VirtualTerminal.Create(Console.Out);
            _err = VirtualTerminal.Create(Console.Error);
        }
        else
        {
            _out = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Detect,
                ColorSystem = ColorSystemSupport.Detect,
                Out = new AnsiConsoleOutput(Console.Out),
            });
            _err = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Detect,
                ColorSystem = ColorSystemSupport.Detect,
                Out = new AnsiConsoleOutput(Console.Error),
            });
        }
    }

    public static void Initialize(LogFormatter formatter)
    {
        if (!Enum.IsDefined(typeof(LogFormatter), formatter))
        {
            throw new ArgumentOutOfRangeException(nameof(formatter),
                "Value should be defined in the Formatter enum.");
        }
        if (_instance is not null)
        {
            return;
        }
        _instance = new DiagnosticLogger(formatter);
    }

    public static DiagnosticLogger Instance
    {
        get
        {
            if (_instance is null)
            {
                throw new Exception("The diagonstic logger has not been initialized.");
            }
            return _instance;
        }
    }

    #endregion


    public void WriteDiagonstic(Exception exception)
    {
        switch (exception)
        {
            case SpanException span:
                WriteSpanDiagonstics(new List<SpanException>() { span });
                break;
            case FileNotFoundException file:
                WriteFileNotFoundDiagonstic(file);
                break;
            case CompilerException compiler:
                WriteCompilerDiagonstic(compiler);
                break;
            default:
                WriteBaseDiagonstic(exception);
                break;
        }
    }

    public void WriteSpanDiagonstics(List<SpanException> exceptions)
    {
        if (_formatter is LogFormatter.Enhanced)
        {
            RenderEnhancedSpanErrors(exceptions);
            return;
        }
        var messages = exceptions.Select(FormatSpanError);
        if (messages is null)
        {
            return;
        }
        var joined = _formatter switch
        {
            LogFormatter.JSON => "[" + string.Join(",\n", messages) + "]",
            _ => string.Join("\n", messages),
        };
        if (string.IsNullOrWhiteSpace(joined))
        {
            // Don't print a single blank line.
            return;
        }
        _err.WriteLine(joined);
    }

    private void WriteFileNotFoundDiagonstic(FileNotFoundException ex)
    {
        if (_formatter is LogFormatter.Enhanced)
        {
            RenderEnhancedException(ex, FileNotFound);
            return;
        }
        _err.WriteLine(FormatDiagnostic(new(Severity.Error, "Unable to open file: " + ex?.FileName, FileNotFound, null)));
    }

    private void WriteCompilerDiagonstic(CompilerException ex)
    {
        if (_formatter is LogFormatter.Enhanced)
        {
            RenderEnhancedException(ex, ex.ErrorCode);
            return;
        }
        _err.WriteLine(FormatDiagnostic(new(Severity.Error, ex.Message, ex.ErrorCode, null)));
    }

    private void WriteBaseDiagonstic(Exception ex)
    {
        if (_formatter is LogFormatter.Enhanced)
        {
            RenderEnhancedException(ex, Unknown);
            return;
        }
        _err.WriteLine(FormatDiagnostic(new(Severity.Error, ex.Message, Unknown, null)));
    }

    public void WriteLine(string message)
    {
        _out.WriteLine(message);
    }
    public void Write(string message)
    {
        _out.Write(message);
    }

    public void WriteError(string message)
    {
        _err.Write(message);
    }
    public void WriteErrorLine(string message)
    {
        _err.WriteLine(message);
    }
}
