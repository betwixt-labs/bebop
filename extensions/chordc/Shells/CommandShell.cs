using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Chord.Common;
using Chord.Compiler.Internal;
using Spectre.Console;
namespace Chord.Compiler.Shells;

internal static class CommandShell
{
    public static async Task<int> RunScriptAsync(BuildCommand buildCommand, string workingDirectory, CancellationToken cancellationToken)
    {

        if (buildCommand.Shell is not ScriptShell.None && ShellHelpers.IsShellOnHost(buildCommand.Shell) is not true)
        {
            throw new Exception($"The {buildCommand.Shell} shell is not available on this host.");
        }
        var shell = buildCommand.Shell is not ScriptShell.None ? buildCommand.Shell : ShellHelpers.GetDefaultShell();

        var contents = buildCommand.Script;
        var multiLines = contents.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Logger.Out.WriteLine($"Executing Script:");
        foreach (var line in multiLines)
        {
            // Bright Cyan color
            Logger.Out.MarkupLine("[cyan]{0}[/]", line);
        }

        string argFormat = ShellHelpers.GetScriptArgumentsFormat(shell);

        var tempDir = System.IO.Path.GetTempPath();
        var scriptFilePath = Path.Combine(tempDir, $"{Guid.NewGuid()}{ShellHelpers.GetScriptFileExtension(shell)}");
        // Format arg string with script path
        var arguments = string.Format(argFormat, scriptFilePath);
        contents = ShellHelpers.FixUpScriptContents(shell, contents);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Normalize Windows line endings
            contents = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
        File.WriteAllText(scriptFilePath, contents, new UTF8Encoding(false));

        var startInfo = new ProcessStartInfo
        {
            FileName = ShellHelpers.GetShellExecutable(shell),
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        // Inheriting all current environment variables
        foreach (var envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
        {
            if (envVar.Key is string key && envVar.Value is string value)
            {
                startInfo.EnvironmentVariables[key] = value;
            }
        }
        if (buildCommand.Env is { Count: > 0 })
        {
            foreach (var (key, value) in buildCommand.Env)
            {
                startInfo.EnvironmentVariables[key] = value;
            }
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        try
        {
            await Task.WhenAll(
                process.WaitForExitAsync(cancellationToken),
                ReadStreamAsync(process.StandardOutput, true, cancellationToken),
                ReadStreamAsync(process.StandardError, false, cancellationToken)
            );
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            // The task was cancelled, so we don't need to do anything here.
        }
        finally
        {
            File.Delete(scriptFilePath);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.Error.MarkupLine("[red]Script execution cancelled[/]");
        }
        if (process.ExitCode != 0)
        {
            Logger.Error.MarkupLine("[red]Script exited with non-zero exit code {0}[/]", process.ExitCode);
        }
        return process.ExitCode;
    }

    private static async Task ReadStreamAsync(StreamReader streamReader, bool isStandardOutput, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await streamReader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break; // End of stream
                }
                if (isStandardOutput)
                {
                    // Bright Green color
                    var escaped = Markup.Escape(line);
                    Logger.Out.MarkupLine("[white]{0}[/]", escaped);
                }
                else
                {
                    // Bright Red color
                    var escaped = Markup.Escape(line);
                    Logger.Error.MarkupLine("[red]{0}[/]", escaped);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }
}