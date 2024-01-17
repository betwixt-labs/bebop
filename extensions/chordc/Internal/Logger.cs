using Spectre.Console;

namespace Chord.Compiler.Internal;

internal static class Logger
{
    public static readonly IAnsiConsole Out;
    public static readonly IAnsiConsole Error;
    public static bool IsVerbose { get; set; }
    static Logger()
    {
        Out = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Out = new AnsiConsoleOutput(Console.Out),
        });
        Error = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Out = new AnsiConsoleOutput(Console.Error),
        });
        IsVerbose = false;
    }
}