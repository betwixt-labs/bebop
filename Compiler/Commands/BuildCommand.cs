using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Generators;
using Core.Logging;
using Core.Meta;

namespace Compiler.Commands;

public class BuildCommand : CliCommand
{
    public BuildCommand() : base(CliStrings.BuildCommand, "Build schemas into one or more target languages.")
    {
        SetAction(HandleCommandAsync);
    }

    private Task<int> HandleCommandAsync(ParseResult result, CancellationToken token)
    {
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag)!;
        config.Validate();
        var compiler = new BebopCompiler();
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
                    DiagnosticLogger.Instance.WriteDiagonstic(new CompilerException("No input files specified."));
                    return Task.FromResult(BebopCompiler.Err);
                }
                schema = BebopCompiler.ParseSchema(resolvedSchemas);
            }

             var (Warnings, Errors) = BebopCompiler.GetSchemaDiagnostics(schema, config.SupressedWarningCodes);
             DiagnosticLogger.Instance.WriteSpanDiagonstics([.. Warnings, .. Errors]);
             if (config.NoEmit)
             {
                 return Task.FromResult(Errors.Count != 0 ? BebopCompiler.Err : BebopCompiler.Ok);
             }
             if (config is { Generators.Length: <= 0 })
             {
                 DiagnosticLogger.Instance.WriteDiagonstic(new CompilerException("No code generators specified."));
                 return Task.FromResult(BebopCompiler.Err);
             }
             foreach (var generatorConfig in config.Generators)
             {
                BebopCompiler.Build(generatorConfig, schema, config);
             }
            return Task.FromResult(BebopCompiler.Ok);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Instance.WriteDiagonstic(ex);
            return Task.FromResult(BebopCompiler.Err);
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