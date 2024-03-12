using System.Runtime.InteropServices;
using Chord.Common;
using Chord.Compiler.Internal.Utils;

namespace Chord.Compiler.Shells;

internal static class ShellHelpers
{
    private static readonly Dictionary<ScriptShell, string> _defaultArguments = new()
    {
        [ScriptShell.Cmd] = "/D /E:ON /V:OFF /S /C \"CALL \"{0}\"\"",
        [ScriptShell.Pwsh] = "-command \". '{0}'\"",
        [ScriptShell.Powershell] = "-command \". '{0}'\"",
        [ScriptShell.Bash] = "--noprofile --norc -e -o pipefail {0}",
        [ScriptShell.Sh] = "-e {0}",
        [ScriptShell.Python] = "{0}"
    };

    private static readonly Dictionary<ScriptShell, string> _extensions = new()
    {
        [ScriptShell.Cmd] = ".cmd",
        [ScriptShell.Pwsh] = ".ps1",
        [ScriptShell.Powershell] = ".ps1",
        [ScriptShell.Bash] = ".sh",
        [ScriptShell.Sh] = ".sh",
        [ScriptShell.Python] = ".py"
    };

    internal static string GetShellExecutable(ScriptShell shell)
    {
        var executable = shell switch
        {
            ScriptShell.Cmd => "cmd",
            ScriptShell.Pwsh => "pwsh",
            ScriptShell.Powershell => "powershell",
            ScriptShell.Bash => "bash",
            ScriptShell.Sh => "sh",
            ScriptShell.Python => "python3",
            _ => throw new ArgumentException($"Unknown shell {shell}")
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            executable += IOUtil.ExeExtension;
        }
        return executable;
    }

    internal static string GetScriptArgumentsFormat(ScriptShell shell)
    {
        if (_defaultArguments.TryGetValue(shell, out var argFormat))
        {
            return argFormat;
        }
        return "";
    }

    internal static string GetScriptFileExtension(ScriptShell shell)
    {
        if (_extensions.TryGetValue(shell, out var extension))
        {
            return extension;
        }
        return "";
    }

    internal static string FixUpScriptContents(ScriptShell shell, string contents)
    {
        switch (shell)
        {
            case ScriptShell.Cmd:
                // Note, use @echo off instead of using the /Q command line switch.
                // When /Q is used, echo can't be turned on.
                contents = $"@echo off{Environment.NewLine}{contents}";
                break;
            case ScriptShell.Pwsh:
            case ScriptShell.Powershell:
                var prepend = "$ErrorActionPreference = 'stop'";
                var append = @"if ((Test-Path -LiteralPath variable:\LASTEXITCODE)) { exit $LASTEXITCODE }";
                contents = $"{prepend}{Environment.NewLine}{contents}{Environment.NewLine}{append}";
                break;
        }
        return contents;
    }

    internal static (string shellCommand, string shellArgs) ParseShellOptionString(string shellOption)
    {
        var shellStringParts = shellOption.Split(" ", 2);
        if (shellStringParts.Length == 2)
        {
            return (shellCommand: shellStringParts[0], shellArgs: shellStringParts[1]);
        }
        else if (shellStringParts.Length == 1)
        {
            return (shellCommand: shellStringParts[0], shellArgs: "");
        }
        else
        {
            throw new ArgumentException($"Failed to parse COMMAND [..ARGS] from {shellOption}");
        }
    }

    internal static bool IsShellOnHost(ScriptShell shell) => shell switch
    {
        ScriptShell.Cmd => PathUtil.IsProgramOnPath("cmd"),
        ScriptShell.Pwsh => PathUtil.IsProgramOnPath("pwsh"),
        ScriptShell.Powershell => PathUtil.IsProgramOnPath("powershell"),
        ScriptShell.Bash => PathUtil.IsProgramOnPath("bash"),
        ScriptShell.Sh => PathUtil.IsProgramOnPath("sh"),
        ScriptShell.Python => PathUtil.IsProgramOnPath("python"),
        _ => throw new ArgumentException($"Unknown shell {shell}")
    };

    internal static ScriptShell GetDefaultShell()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (PathUtil.IsProgramOnPath("pwsh"))
            {
                return ScriptShell.Pwsh;
            }
            else if (PathUtil.IsProgramOnPath("powershell"))
            {
                return ScriptShell.Powershell;
            }
        }
        else
        {
            if (PathUtil.IsProgramOnPath("bash"))
            {
                return ScriptShell.Bash;
            }
            else if (PathUtil.IsProgramOnPath("sh"))
            {
                return ScriptShell.Sh;
            }
        }
        throw new PlatformNotSupportedException("Unable to find a supported shell.");
    }
}