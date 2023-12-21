using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Core.Exceptions;
using Core.Logging;
using Core.Meta;

namespace Compiler.Commands;

public class BuildCommand : CliCommand
{
    public BuildCommand() : base(CliStrings.BuildCommand, "Build schemas into one or more target languages.")
    {
        SetAction(HandleCommand);
    }

    private int HandleCommand(ParseResult result)
    {
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag)!;

        config.Validate();
        BebopSchema schema = default;
        string? tempFilePath = null;
        try
        {
            // if input is redirected, read from stdin, ignore includes.
            // this is a temporary flag as it appears there is a bug in wasi builds
            // that always returns true for Console.IsInputRedirected
            if (result.GetValue<bool>(CliStrings.StandardInputFlag))
            {
                // we should write the textual stream to a temp file and then parse it.
                tempFilePath = Helpers.GetTempFileName();
                using var fs = File.Open(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var standardInput = Console.OpenStandardInput();
                // dont use async as wasi currently has threading issues
                standardInput.CopyTo(fs);
                fs.Seek(0, SeekOrigin.Begin);
                schema = BebopCompiler.ParseSchema([tempFilePath]);
            }
            else
            {
                var resolvedSchemas = config.ResolveIncludes();
                if (!resolvedSchemas.Any())
                {
                    return DiagnosticLogger.Instance.WriteDiagonstic(new CompilerException("No input files specified."));
                }
                schema = BebopCompiler.ParseSchema(resolvedSchemas);
            }
            var isStandardOut = result.GetValue<bool>(CliStrings.StandardOutputFlag);
            var (Warnings, Errors) = BebopCompiler.GetSchemaDiagnostics(schema, config.SupressedWarningCodes);
            if (config.NoEmit || Errors.Count != 0)
            {
                DiagnosticLogger.Instance.WriteSpanDiagonstics([.. Warnings, .. Errors]);
                return Errors.Count != 0 ? BebopCompiler.Err : BebopCompiler.Ok;
            }
            // we only want to write the warnings if we are not writing results to stdout 
            // as we concat the outputs of all generators into one stream
            if (!isStandardOut && Warnings.Count != 0)
            {
                DiagnosticLogger.Instance.WriteSpanDiagonstics(Warnings);
            }
            if (config is { Generators.Length: <= 0 })
            {
                return DiagnosticLogger.Instance.WriteDiagonstic(new CompilerException("No code generators specified."));
            }
            var generatedFiles = new List<GeneratedFile>();
            foreach (var generatorConfig in config.Generators)
            {
                generatedFiles.Add(BebopCompiler.Build(generatorConfig, schema, config));
            }
            if (isStandardOut)
            {
                DiagnosticLogger.Instance.PrintCompilerOutput(new CompilerOutput(Warnings, Errors, [.. generatedFiles]));
            }
            else
            {
                BebopCompiler.EmitGeneratedFiles(generatedFiles, config);
            }
            return BebopCompiler.Ok;
        }
        catch (Exception ex)
        {
            return DiagnosticLogger.Instance.WriteDiagonstic(ex);
        }
        finally
        {
            if (tempFilePath is not null)
            {
                File.Delete(tempFilePath);
            }
        }
    }
}