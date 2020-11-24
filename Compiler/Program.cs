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
        private static Lager _log;
        private static CommandLineFlags? _flags;

        private static async Task WriteHelpText()
        {
            if (_flags is not null)
            {
                await Lager.StandardOut(string.Empty);
                await Lager.StandardOut(_flags.HelpText);
            }
        }

        private static async Task<int> Main(string[] args)
        {
            if (!CommandLineFlags.TryParse(args, out _flags, out var message))
            {
                await Lager.StandardError(message);
                await Lager.StandardOut(_flags.HelpText);
                return 1;
            }

            if (_flags.Version)
            {
                Console.WriteLine($"{ReservedWords.CompilerName} {ReservedWords.CompilerVersion}");
                return 0;
            }

            if (_flags.Help)
            {
                await WriteHelpText();
                return 0;
            }

            _log = Lager.CreateLogger(_flags.LogFormatter);


            if (_flags.CheckSchemaFiles is not null)
            {
                if (_flags.CheckSchemaFiles.Count > 0)
                {
                    return await CheckSchemas(_flags.CheckSchemaFiles);
                }
                await _log.Error(new CompilerException("No schemas specified in check."));
                return 1;
            }

            if (string.IsNullOrWhiteSpace(_flags.Language))
            {
                await _log.Error(new CompilerException("No code generator was specified."));
                return 1;
            }

            if (!GeneratorUtils.ImplementedGenerators.ContainsKey(_flags.Language))
            {
                await _log.Error(new CompilerException($"\"{_flags.Language}\" is not a recognized code generator"));
                return 1;
            }

            if (_flags.SchemaDirectory is not null && _flags.SchemaFiles is not null)
            {
                await _log.Error(
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
                await _log.Error(new CompilerException("Specify one or more input files with --dir or --files."));
                return 1;
            }

            if (paths.Count == 0)
            {
                await _log.Error(new CompilerException("No input files were found at the specified target location."));
                return 1;
            }

            if (string.IsNullOrWhiteSpace(_flags.OutputFile))
            {
                await _log.Error(new CompilerException("No output file was specified."));
                return 1;
            }

            return await CompileSchema(GeneratorUtils.ImplementedGenerators[_flags!.Language],
                paths, new FileInfo(_flags.OutputFile), _flags.Namespace ?? "");
        }

        private static async Task<ISchema> ParseAndValidateSchemas(List<string> schemaPaths, string nameSpace)
        {
            var parser = new SchemaParser(schemaPaths, nameSpace);
            var schema = await parser.Evaluate();
            schema.Validate();
            return schema;
        }

        private static async Task<int> CompileSchema(Func<ISchema, Generator> makeGenerator,
            List<string> schemaPaths,
            FileInfo outputFile,
            string nameSpace)
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
                var compiled = generator.Compile();
                await File.WriteAllTextAsync(outputFile.FullName, compiled);
                return 0;
            }
            catch (Exception e)
            {
                await ReportError(e);
            }
            return 1;
        }

        private static async Task<int> CheckSchemas(List<string> schemaPaths)
        {
            try
            {
                await ParseAndValidateSchemas(schemaPaths, "CheckNameSpace");
                return 0;
            }
            catch (Exception e)
            {
                await ReportError(e);
                return 1;
            }
        }

        private static async Task ReportError(Exception exception)
        {
            await _log.Error(exception);
        }
    }
}