using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Generators;
using Core.Logging;
using Core.Meta;
using Core.Meta.Interfaces;
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

            if (!CommandLineFlags.TryParse(Environment.GetCommandLineArgs(), out _flags, out var message))
            {
                await Log.Error(new CompilerException(message));
                return 1;
            }

          

            if (_flags.Version)
            {
                await Lager.StandardOut($"{ReservedWords.CompilerName} {VersionInfo.Informational}");
                return 0;
            }

            if (_flags.Help)
            {
                await WriteHelpText();
                return 0;
            }
            if (_flags.CheckSchemaFile is not null)
            {
                if (string.IsNullOrWhiteSpace(_flags.CheckSchemaFile))
                {
                    await Log.Error(new CompilerException("No textual schema was read from standard input."));
                    return 1;
                }
                return await CheckSchema(_flags.CheckSchemaFile);
            }
            if (_flags.CheckSchemaFiles is not null)
            {
                if (_flags.CheckSchemaFiles.Count > 0)
                {
                    return await CheckSchemas(_flags.CheckSchemaFiles);
                }
                await Log.Error(new CompilerException("No schemas specified in check."));
                return 1;
            }

            if (!_flags.GetParsedGenerators().Any())
            {
                await Log.Error(new CompilerException("No code generators were specified."));
                return 1;
            }

            if (_flags.SchemaDirectory is not null && _flags.SchemaFiles is not null)
            {
                await Log.Error(
                    new CompilerException("Can't specify both an input directory and individual input files"));
                return 1;
            }

            List<string> paths;

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
            else
            {
                await Log.Error(new CompilerException("Specify one or more input files with --dir or --files."));
                return 1;
            }
            if (paths.Count == 0)
            {
                await Log.Error(new CompilerException("No input files were found at the specified target location."));
                return 1;
            }

            foreach (var parsedGenerator in _flags.GetParsedGenerators())
            {
                if (!GeneratorUtils.ImplementedGenerators.ContainsKey(parsedGenerator.Alias))
                {
                    await Log.Error(new CompilerException($"'{parsedGenerator.Alias}' is not a recognized code generator"));
                    return 1;
                }
                if (string.IsNullOrWhiteSpace(parsedGenerator.OutputFile))
                {
                    await Log.Error(new CompilerException("No output file was specified."));
                    return 1;
                }
                var result = await CompileSchema(GeneratorUtils.ImplementedGenerators[parsedGenerator.Alias], paths, new FileInfo(parsedGenerator.OutputFile), _flags.Namespace ?? string.Empty, parsedGenerator.LangVersion);
                if (result != Ok)
                {
                    return result;
                }
            }
            return Ok;
        }

        private static async Task<int> CheckSchema(string textualSchema)
        {
            try
            {
                var parser = new SchemaParser(textualSchema, "CheckNameSpace");
                var schema = await parser.Parse();
                schema.Validate();
                return Ok;
            }
            catch (Exception e)
            {
                await ReportError(e);
                return Err;
            }
        }

        private static async Task<ISchema> ParseAndValidateSchemas(List<string> schemaPaths, string nameSpace)
        {
            var parser = new SchemaParser(schemaPaths, nameSpace);
            var schema = await parser.Parse();
            schema.Validate();
            return schema;
        }

        private static async Task<int> CompileSchema(Func<ISchema, BaseGenerator> makeGenerator,
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

            try
            {
                var schema = await ParseAndValidateSchemas(schemaPaths, nameSpace);
                var generator = makeGenerator(schema);
                generator.WriteAuxiliaryFiles(outputFile.DirectoryName ?? string.Empty);
                var compiled = generator.Compile(langVersion);
                await File.WriteAllTextAsync(outputFile.FullName, compiled);
                return Ok;
            }
            catch (Exception e)
            {
                await ReportError(e);
            }
            return Err;
        }

        private static async Task<int> CheckSchemas(List<string> schemaPaths)
        {
            try
            {
                await ParseAndValidateSchemas(schemaPaths, "CheckNameSpace");
                return Ok;
            }
            catch (Exception e)
            {
                await ReportError(e);
                return Err;
            }
        }

        private static async Task ReportError(Exception exception)
        {
            await Log.Error(exception);
        }
    }
}
