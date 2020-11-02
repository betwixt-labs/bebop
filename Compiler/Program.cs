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
        private static readonly Lager Log = Lager.CreateLogger("Compiler");

        private static async Task<int> Main(string[] args)
        {
            if (!CommandLineFlags.TryParse(args, out var flags, out var message))
            {
                Log.Error(message);
                Console.WriteLine(flags.HelpText);
                return 1;
            }

            void WriteHelpText()
            {
                Console.WriteLine();
                Console.WriteLine(flags.HelpText);
            }

            if (flags.CheckSchemaFiles != null)
            {
                if (flags.CheckSchemaFiles.Count > 0)
                {
                    return await CheckSchemas(flags.CheckSchemaFiles);
                }
                else
                {
                    Log.Error("Must specify at least one schema file to check.");
                    return 1;
                }
            }

            if (flags.Version)
            {
                Console.WriteLine($"{ReservedWords.CompilerName} {ReservedWords.CompilerVersion}");
                return 0;
            }

            if (flags.Help)
            {
                WriteHelpText();
                return 0;
            }

            if (string.IsNullOrWhiteSpace(flags.Language))
            {
                Log.Error("No code generator was specified");
                WriteHelpText();
                return 1;
            }

            if (!GeneratorUtils.ImplementedGenerators.ContainsKey(flags.Language))
            {
                Log.Error($"\"{flags.Language}\" is not a recognized code generator");
                return 1;
            }

            if (flags.SchemaDirectory != null && flags.SchemaFiles != null)
            {
                Log.Error("Can't specify both an input directory and individual input files");
                return 1;
            }

            List<string> paths;

            if (flags.SchemaDirectory != null)
            {
                paths = new DirectoryInfo(flags.SchemaDirectory!).GetFiles($"*.{ReservedWords.SchemaExt}", SearchOption.AllDirectories).Select(f => f.FullName).ToList();
            }
            else if (flags.SchemaFiles != null)
            {
                paths = flags.SchemaFiles;
            }
            else
            {
                Log.Error("Specify one or more input files with --dir or --files.");
                return 1;
            }

            if (paths.Count == 0)
            {
                Log.Error("No input files were found at the specified target location.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(flags.OutputFile))
            {
                Log.Error("No output file was specified");
                return 1;
            }

            return await CompileSchema(GeneratorUtils.ImplementedGenerators[flags!.Language],
                paths, new FileInfo(flags.OutputFile), flags.Namespace ?? "");
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
            if (outputFile.Directory != null && !outputFile.Directory.Exists)
            {
                outputFile.Directory.Create();
                Log.Info($"Created output directory: {outputFile.Directory.FullName}");
            }
            if (outputFile.Exists)
            {
                File.Delete(outputFile.FullName);
                Log.Warn($"Deleted previously generated file: {outputFile.FullName}");
            }

            try
            {
                var schema = await ParseAndValidateSchemas(schemaPaths, nameSpace);
                var generator = makeGenerator(schema);
                generator.WriteAuxiliaryFiles(outputFile.DirectoryName ?? string.Empty);
                Log.Info("Auxiliary files written to output directory");
                var compiled = generator.Compile();
                await File.WriteAllTextAsync(outputFile.FullName, compiled);
                Log.Success($"Build complete -> \"{outputFile.FullName}\"");
                return 0;
            }
            catch (Exception e)
            {
                ReportError(e);
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
                ReportError(e);
                return 1;
            }
        }

        private static void ReportError(Exception exception)
        {
            switch (exception)
            {
                case SpanException e:
                    Log.Error($"Error in {e.Span.FileName} at {e.Span.StartColonString()}: ", e);
                    break;
                case FileNotFoundException e:
                    Log.Error($"File was not found.", e);
                    break;
                default:
                    Log.Error($"Error when processing schemas:", exception);
                    break;
            }
        }
    }
}
