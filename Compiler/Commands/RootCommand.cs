using System;
using System.CommandLine;
using System.IO;
using Core.Logging;
using Core.Meta;
using Spectre.Console;
using Spectre.Console.Json;

namespace Compiler.Commands;

public class RootCommand
{

    public static int HandleCommand(ParseResult result)
    {
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag)!;
        if (result.GetValue<bool>(CliStrings.InitFlag) is true)
        {
            return InitProject();
        }
        if (result.GetValue<bool>(CliStrings.ListSchemasFlag) is true)
        {
            return ListSchemas(config);
        }
        if (result.GetValue<bool>(CliStrings.ShowConfigFlag) is true)
        {
            return ShowConfig(config);
        }
        return 0;
    }

    /// <summary>
    /// Shows the current configuration and stops processing.
    /// </summary>
    /// <param name="config">The bebop configuration object.</param>
    /// <returns>An integer representing the status of the operation.</returns>
    private static int ShowConfig(BebopConfig config)
    {
        var json = new JsonText(config.ToJson());

        DiagnosticLogger.Instance.Out.Write(new Panel(json)
        .Collapse()
        .RoundedBorder()
        .BorderColor(Color.Yellow));
        return 0;
    }

    /// <summary>
    /// Lists all schemas defined in the configuration.
    /// </summary>
    /// <param name="config">The bebop configuration object.</param>
    /// <returns>An integer representing the status of the operation.</returns>
    private static int ListSchemas(BebopConfig config)
    {
        foreach (var schema in config.ResolveIncludes())
        {
            DiagnosticLogger.Instance.WriteLine(schema);
        }
        return 0;
    }

    /// <summary>
    /// Initializes a new project with a default configuration.
    /// </summary>
    /// <param name="config">The bebop configuration object to initialize the project with.</param>
    /// <returns>An integer representing the status of the operation.</returns>
    private static int InitProject()
    {
        try
        {
            var workingDirectory = Directory.GetCurrentDirectory();
            var configPath = Path.GetFullPath(Path.Combine(workingDirectory, BebopConfig.ConfigFileName));
            if (File.Exists(configPath))
            {
                DiagnosticLogger.Instance.Error.MarkupLine($"[maroon]{BebopConfig.ConfigFileName} already exists in the current directory.[/]");
                return 1;
            }
            File.WriteAllText(configPath, BebopConfig.Default.ToJson());
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Instance.Error.WriteException(ex);
            return 1;
        }

        return 0;
    }
}