using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Generators;
using Compiler.IO;
using Compiler.Meta.Interfaces;
using Compiler.Parser;

namespace Compiler
{
    internal class Program
    {
        private static readonly Lager Log = Lager.CreateLogger("Compiler");

        private static async Task<int> Main(string[] args)
        {
            if (!CommandLineFlags.TryParse(args, out var flags, out var message))
            {
                Console.WriteLine(message);
                Console.WriteLine(flags.HelpText);
                return 1;
            }

            void WriteHelpText()
            {
                Console.WriteLine();
                Console.WriteLine(flags.HelpText);
            }

            if (flags.Version)
            {
                Console.WriteLine($"pierogic {Assembly.GetExecutingAssembly().GetName().Version}");
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

            if (string.IsNullOrWhiteSpace(flags.SchemaDirectory) && (flags.SchemaFiles?.Count ?? 0) == 0)
            {
                Log.Error("No schema sources specified");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(flags.OutputFile))
            {
                Log.Error("No output file was specified");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(flags.Namespace) && flags.Language == "cs")
            {
                Log.Error("No namespace was specified");
                return 1;
            }

            FileInfo? inputFile = null;

            if (!string.IsNullOrWhiteSpace(flags.SchemaDirectory) &&
                !SchemaFuser.TryCoalescing(flags.SchemaDirectory, out inputFile))
            {
                Log.Error($"Unable to coalesce schemas from {flags.SchemaDirectory}");
                return 1;
            }

            if (flags.SchemaFiles != null && flags.SchemaFiles.Count > 0 && !SchemaFuser.TryCoalescing(flags.SchemaFiles, out inputFile))
            {
                Log.Error($"Unable to coalesce schemas: {string.Join(",", flags.SchemaFiles)}");
                return 1;
            }


            if (!inputFile!.Exists)
            {
                Log.Error("Master source file not found after coalescing");
                return 1;
            }

            return await CompileSchemas(GeneratorUtils.ImplementedGenerators[flags!.Language],
                inputFile, new FileInfo(flags.OutputFile), flags.Namespace ?? "");
        }

        private static async Task<int> CompileSchemas(Func<ISchema, IGenerator> makeGenerator,
            FileInfo inputFile,
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
                var parser = new SchemaParser(inputFile, nameSpace);
                var schema = await parser.Evaluate();
                schema.Validate();
                var generator = makeGenerator(schema);
                generator.WriteAuxiliaryFiles(outputFile.DirectoryName ?? string.Empty);
                Log.Info("Auxiliary files written to output directory");
                var compiled = generator.Compile();
                await File.WriteAllTextAsync(outputFile.FullName, compiled);
                Log.Success($"Build complete -> \"{outputFile.FullName}\"");
                return 0;
            }
            catch (SpanException e)
            {
                Log.Error($"Error in {e.SourcePath} at {e.Span.StartColonString()}:", e);
            }
            catch (FileNotFoundException)
            {
                Log.Error($"File {inputFile.FullName} was not found.");
            }
            catch (Exception e)
            {
                Log.Error($"Error when processing {inputFile.FullName}:", e);
            }
            return 1;
        }
    }
}
