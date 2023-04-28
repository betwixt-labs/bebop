using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compiler.LangServer;
using Core.Exceptions;
using Core.Generators;
using Core.Logging;
using Core.Meta;
using Core.Parser;

namespace Compiler
{
    internal class Program
    {
        private const int Ok = 0;
        private const int Err = 1;
        private static readonly Lager Log = Lager.CreateLogger(LogFormatter.Structured);
        private static CommandLineFlags? _flags;

        private static async Task WriteHelpText()
        {
            if (_flags is not null)
            {
                await Lager.StandardOut(string.Empty);
                await Lager.StandardOut(_flags.HelpText);
            }
        }

        private static async Task<int> Main()
        {
            Log.Formatter = CommandLineFlags.FindLogFormatter(Environment.GetCommandLineArgs());

            try {

            if (!CommandLineFlags.TryParse(Environment.GetCommandLineArgs(), out _flags, out var message))
            {
                await Log.Error(new CompilerException(message));
                return Err;
            }

            if (_flags.Debug)
            {
                var process = Process.GetCurrentProcess().Id;
                await Lager.StandardOut($"Waiting for debugger to attach (PID={process})...");

                // Wait 5 minutes for a debugger to attach
                var timeoutToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;
                while (!Debugger.IsAttached)
                {
                    await Task.Delay(100, timeoutToken);
                }

                Debugger.Break();
            }

            if (_flags.Version)
            {
                await Lager.StandardOut($"{ReservedWords.CompilerName} {DotEnv.Generated.Environment.Version}");
                return Ok;
            }

            if (_flags.Help)
            {
                await WriteHelpText();
                return Ok;
            }

            if (_flags.LanguageServer)
            {
                await BebopLangServer.RunAsync();
                return Ok;
            }

            if (_flags.CheckSchemaFile is not null)
            {
                if (string.IsNullOrWhiteSpace(_flags.CheckSchemaFile))
                {
                    await Log.Error(new CompilerException("No textual schema was read from standard input."));
                    return Err;
                }
                return await CheckSchema(_flags.CheckSchemaFile);
            }

            List<string>? paths = null;

            if (_flags.SchemaDirectory is not null)
            {
                paths = new DirectoryInfo(_flags.SchemaDirectory!)
                    .GetFiles($"*.{ReservedWords.SchemaExt}", SearchOption.AllDirectories)
                    .Select(f => f.FullName)
                    .ToList();
            }
            else if (_flags.SchemaFiles is not null)
            {
                paths = _flags.SchemaFiles;
            }

            if (_flags.CheckSchemaFiles is not null)
            {
                if (_flags.CheckSchemaFiles.Count > 0)
                {
                    return await CheckSchemas(_flags.CheckSchemaFiles);
                }
                // Fall back to the paths defined by the config if none specified
                else if (paths is not null && paths.Count > 0)
                {
                    return await CheckSchemas(paths);
                }
                await Log.Error(new CompilerException("No schemas specified in check."));
                return Err;
            }

            if (!_flags.GetParsedGenerators().Any())
            {
                await Log.Error(new CompilerException("No code generators were specified."));
                return Err;
            }

            if (_flags.SchemaDirectory is not null && _flags.SchemaFiles is not null)
            {
                await Log.Error(
                    new CompilerException("Can't specify both an input directory and individual input files"));
                return Err;
            }

            if (paths is null)
            {
                await Log.Error(new CompilerException("Specify one or more input files with --dir or --files."));
                return Err;
            }
            if (paths.Count == 0)
            {
                await Log.Error(new CompilerException("No input files were found at the specified target location."));
                return Err;
            }

            // Everything below this point requires paths

            foreach (var parsedGenerator in _flags.GetParsedGenerators())
            {
                if (!GeneratorUtils.ImplementedGenerators.ContainsKey(parsedGenerator.Alias))
                {
                    await Log.Error(new CompilerException($"'{parsedGenerator.Alias}' is not a recognized code generator"));
                    return Err;
                }
                if (string.IsNullOrWhiteSpace(parsedGenerator.OutputFile))
                {
                    await Log.Error(new CompilerException("No output file was specified."));
                    return Err;
                }
                var result = await CompileSchema(GeneratorUtils.ImplementedGenerators[parsedGenerator.Alias], paths, new FileInfo(parsedGenerator.OutputFile), _flags.Namespace ?? string.Empty, parsedGenerator.LangVersion);
                if (result != Ok)
                {
                    return result;
                }
            }
            return Ok;

            }
            catch (Exception e)
            {
                await Log.Error(e);
                return Err;
            }
        }

        private static async Task<BebopSchema> ParseAndValidateSchema(List<string> schemaPaths, string nameSpace)
        {
            var parser = new SchemaParser(schemaPaths, nameSpace);
            var schema = await parser.Parse();
            schema.Validate();
            return schema;
        }

        private static async Task<int> CompileSchema(Func<BebopSchema, BaseGenerator> makeGenerator,
            List<string> schemaPaths,
            FileInfo outputFile,
            string nameSpace, Version? langVersion)
        {
            if (outputFile.Directory is not null && !outputFile.Directory.Exists)
            {
                outputFile.Directory.Create();
            }
            if (outputFile.Exists)
            {
                File.Delete(outputFile.FullName);
            }

            var schema = await ParseAndValidateSchema(schemaPaths, nameSpace);
            var result = await ReportSchemaDiagnostics(schema);
            if (result == Err) return Err;
            var generator = makeGenerator(schema);
            generator.WriteAuxiliaryFiles(outputFile.DirectoryName ?? string.Empty);
            var compiled = generator.Compile(langVersion, writeGeneratedNotice: !(_flags?.SkipGeneratedNotice ?? false));
            await File.WriteAllTextAsync(outputFile.FullName, compiled);
            return Ok;
        }

        private static async Task<int> CheckSchema(string textualSchema)
        {
            var parser = new SchemaParser(textualSchema, "CheckNameSpace");
            var schema = await parser.Parse();
            schema.Validate();
            return await ReportSchemaDiagnostics(schema);
        }

        private static async Task<int> CheckSchemas(List<string> schemaPaths)
        {
            var schema = await ParseAndValidateSchema(schemaPaths, "CheckNameSpace");
            return await ReportSchemaDiagnostics(schema);
        }

        private static async Task<int> ReportSchemaDiagnostics(BebopSchema schema)
        {
            var noWarn = _flags?.NoWarn ?? new List<string>();
            var loudWarnings = schema.Warnings.Where(x => !noWarn.Contains(x.ErrorCode.ToString()));
            var errors = loudWarnings.Concat(schema.Errors).ToList();
            await Log.WriteSpanErrors(errors);
            return schema.Errors.Count > 0 ? Err : Ok;
        }
    }
}
