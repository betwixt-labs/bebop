using System;
using System.Collections.Generic;
using System.Text;
namespace Chord.Compiler.Internal.Commands;

internal static class CommandParser
{

    private static readonly HashSet<string> _validCommands = ["build", "pack", "test", "publish", "install", "help"];

    internal static (string Command, Dictionary<string, object> Flags)? GetCommand(string[] args)
    {
        if (args.Length == 0 || !_validCommands.Contains(args[0].ToLowerInvariant()))
        {
            return null;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var flags = new Dictionary<string, object>();

        if (command == "install")
        {
            // Handle multiple extensions arguments for 'install' command
            var extensions = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith('-'))
                {
                    extensions.Add(args[i]);
                }
                else
                {
                    ParseFlag(args, ref i, flags);
                }
            }
            if (extensions.Count > 0)
            {
                flags["extensions"] = extensions;
            }
        }
        else
        {
            // Handle flags for other commands
            for (int i = 1; i < args.Length; i++)
            {
                ParseFlag(args, ref i, flags);
            }
        }

        return (command, flags);
    }

    private static void ParseFlag(string[] args, ref int i, Dictionary<string, object> flags)
    {
        var arg = args[i];
        if (arg.StartsWith('-'))
        {
            var flag = arg.TrimStart('-');
            string? value = null;

            // Check if the next argument is a value (not another flag)
            if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            {
                value = args[i + 1];
                i++; // Skip the next argument since it's the value for this flag
            }

            // If the flag has a value, store it; otherwise, store the flag as a boolean true
            if (string.IsNullOrEmpty(value))
            {
                flags[flag] = true;
            }
            else
            {
                flags[flag] = value;
            }
        }
    }

    internal static string GetHelpText()
    {
        var helpText = new StringBuilder();
        helpText.AppendLine("Usage: chordc [command] [options]");
        helpText.AppendLine();
        helpText.AppendLine("Commands:");
        helpText.AppendLine("  build     Build the chord extension");
        helpText.AppendLine("  pack      Package the chord extension");
        helpText.AppendLine("  test      Test the chord extension");
        helpText.AppendLine("  install  Install the chord extension");
        helpText.AppendLine("  help      Display help");
        helpText.AppendLine();
        //helpText.AppendLine("Options:");
        //helpText.AppendLine("  -h|--help  Show help information");
        return helpText.ToString();
    }

}