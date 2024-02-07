using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.RegularExpressions;
using Core.Logging;
using Core.Meta;
using Spectre.Console;

namespace Compiler.Commands;

public partial class ConvertCommand : CliCommand
{

    public ConvertCommand() : base(CliStrings.ConvertCommand, "Convert schema files from one format to another.")
    {
        SetAction(HandleCommand);
    }

    private int HandleCommand(ParseResult result)
    {
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag)!;
        config.Validate();
        var schemas = config.ResolveIncludes();
        if (result.GetValue<string>(CliStrings.FromFlag) is "v2" && result.GetValue<string>(CliStrings.ToFlag) is "v3")
        {
            var isDryRun = result.GetValue<bool>(CliStrings.DryRunFlag);
            if (isDryRun)
            {
                DiagnosticLogger.Instance.Out.MarkupLine("[yellow]Dry run mode enabled. No files will be written.[/]");
            }
            foreach (var (schemaPath, originalSchema, transformedSchema) in TwoToThreeConverter.Convert(schemas))
            {
                ShowDiff(schemaPath, originalSchema, transformedSchema);
                if (!isDryRun)
                {
                    File.WriteAllText(schemaPath, transformedSchema);
                }
            }
            return 0;
        }
        DiagnosticLogger.Instance.Error.MarkupLine("[red]Only conversion from v2 to v3 is currently supported.[/]");
        return 1;
    }

    private static readonly string[] separators = ["\r\n", "\r", "\n"];

    internal static void ShowDiff(string schemaPath, string originalContent, string transformedSchema)
    {
        string[] originalLines = originalContent.Split(separators, StringSplitOptions.None);
        string[] transformedLines = transformedSchema.Split(separators, StringSplitOptions.None);

        var panelContent = new List<string>();
        int maxLines = Math.Max(originalLines.Length, transformedLines.Length);

        for (int i = 0; i < maxLines; i++)
        {
            string originalLine = i < originalLines.Length ? originalLines[i] : string.Empty;
            string modifiedLine = i < transformedLines.Length ? transformedLines[i] : string.Empty;

            if (originalLine != modifiedLine)
            {
                if (!string.IsNullOrEmpty(originalLine))
                {
                    panelContent.Add($"[yellow]- {originalLine.EscapeMarkup()}[/]");
                }
                if (!string.IsNullOrEmpty(modifiedLine))
                {
                    panelContent.Add($"[blue]+ {modifiedLine.EscapeMarkup()}[/]");
                }
            }
            else
            {
                panelContent.Add($"{originalLine.EscapeMarkup()}");
            }
        }




        // Render in a panel
        var panel = new Panel(string.Join(Environment.NewLine, panelContent))
        .Header($"[underline white][bold]{schemaPath}[/][/]")
            .Expand()

            .Border(BoxBorder.Heavy);



        DiagnosticLogger.Instance.Out.Write(panel);
    }


    internal static partial class TwoToThreeConverter
    {
        public static IEnumerable<(string SchemaPath, string OriginalSchema, string TransformedSchema)> Convert(IEnumerable<string> schemas)
        {

            foreach (var schemaPath in schemas)
            {

                var schema = File.ReadAllText(schemaPath);
                var wasModified = false;
                var transformedContent = AttributePattern().Replace(schema, match =>
                {
                    wasModified = true;
                    string identifier = match.Groups[1].Value;
                    string? value = match.Groups[2].Success ? match.Groups[2].Value : null;
                    // Handling for cases with and without value
                    return value != null ? $"@{identifier}({value})" : $"@{identifier}";
                });
                transformedContent = ReadOnlyPattern().Replace(transformedContent, match =>
                {
                    wasModified = true;
                    // Remove 'readonly' if present, otherwise append 'mut'
                    return match.Groups[1].Success ? $"struct {match.Groups[2].Value}" : $"mut struct {match.Groups[2].Value}";
                });
                if (wasModified)
                {
                    yield return (schemaPath, schema, transformedContent);
                }
            }
        }

        [GeneratedRegex(@"\[([\w]+)(?:\(([^,\]]*?)\))?\](?![\],])")]
        private static partial Regex AttributePattern();
        [GeneratedRegex(@"(?<!mut\s+)(readonly\s+)?struct\s+(\w+)")]
        private static partial Regex ReadOnlyPattern();
    }


}