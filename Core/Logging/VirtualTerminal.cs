
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
namespace Core.Logging;
public sealed class VirtualTerminal : IAnsiConsole
{
    private readonly IAnsiConsole _owner;

    public Profile Profile => _owner.Profile;
    public IAnsiConsoleCursor Cursor => _owner.Cursor;
    public IAnsiConsoleInput Input { get; }
    public TextWriter Output { get; }
    public IExclusivityMode ExclusivityMode => _owner.ExclusivityMode;
    public RenderPipeline Pipeline => _owner.Pipeline;

    private VirtualTerminal(IAnsiConsole owner, TextWriter output)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Input = new VirtualConsoleInput();
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public static VirtualTerminal Create(TextWriter consoleWriter, int width = 80, int height = 24)
    {
        var consoleOutput = new ConsoleRedirectWriter(consoleWriter);
        return new VirtualTerminal(AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Out = new VirtualConsoleOutput(consoleOutput, width, height),
            Interactive = InteractionSupport.No,
        }), consoleOutput);
    }

    public void Clear(bool home)
    {
        _owner.Clear();
    }

    public void Write(IRenderable renderable)
    {
        _owner.Write(renderable);
    }
}

public sealed class VirtualConsoleInput : IAnsiConsoleInput
{
    public bool IsKeyAvailable()
    {
        return false;
    }

    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        return null;
    }

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        return Task.FromResult<ConsoleKeyInfo?>(null);
    }
}

public sealed class VirtualConsoleOutput : IAnsiConsoleOutput
{
    public TextWriter Writer { get; }
    public bool IsTerminal { get; }
    public int Width { get; }
    public int Height { get; }

    public VirtualConsoleOutput(TextWriter writer, int width, int height)
    {
        Writer = writer;
        IsTerminal = true;
        Width = width;
        Height = height;
    }

    public void SetEncoding(Encoding encoding)
    {

    }
}

public class ConsoleRedirectWriter : TextWriter
{
    private readonly TextWriter _originalWriter;
    public ConsoleRedirectWriter(TextWriter originalWriter)
    {
        _originalWriter = originalWriter;
    }
    public override void Write(char value)
    {
        // Write the character to the original writer
        _originalWriter.Write(value);
    }

    public override void Write(string? value)
    {
        // Write the string to the original writer
        _originalWriter.Write(value);
    }

    public override void WriteLine(string? value)
    {
        // Write the string followed by a line break to the original writer
        _originalWriter.WriteLine(value);
    }

    public override Encoding Encoding => Encoding.UTF8;
}