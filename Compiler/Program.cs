using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Generators;
using Compiler.IO;
using Compiler.Parser;

namespace Compiler
{
    internal class Program
    {
        private static Lager Log = Lager.CreateLogger("Compiler");

        private static async Task<int> Main(string[] args)
        {
            if (!CommandLineFlags.TryParse(args, out var flags, out var message))
            {
                Console.WriteLine(message);
                Console.WriteLine(flags.HelpText);
                return 1;
            }

            if (flags.Version)
            {
                Console.WriteLine($"pierogic {Assembly.GetExecutingAssembly().GetName().Version}");
                return 0;
            }

            if (flags.Help)
            {
                Console.WriteLine(flags.HelpText);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(flags.Language))
            {
                Console.WriteLine();
                return 1;
            }

            if (!GeneratorUtils.ImplementedGenerators.ContainsKey(flags.Language))
            {
                Console.WriteLine();
                return 1;
            }

            if (string.IsNullOrWhiteSpace(flags.SchemaDirectory) && flags.SchemaFiles.Count == 0)
            {
                Console.WriteLine();
                return 1;
            }

            if (string.IsNullOrWhiteSpace(flags.OutputFile))
            {
                Log.Error("No output file was specified.");
                return 1;
            }

            FileInfo? inputFile = null;

            if (!string.IsNullOrWhiteSpace(flags.SchemaDirectory) &&
                !SchemaFuser.TryCoalescing(flags.SchemaDirectory, out inputFile))
            {
                Log.Error($"Unable to coalesce schemas from {flags.SchemaDirectory}.");
                return 1;
            }

            if (flags?.SchemaFiles?.Count > 0 && !SchemaFuser.TryCoalescing(flags.SchemaFiles, out inputFile))
            {
                return 1;
            }

            if (inputFile == null)
            {
                return 1;
            }


            if (!inputFile.Exists)
            {
                return 1;
            }

            return await CompileSchemas(GeneratorUtils.ImplementedGenerators[flags.Language],
                inputFile, new FileInfo(flags.OutputFile), flags.Namespace);
        }

        private static async Task<int> CompileSchemas(IGenerator generator,
            FileInfo inputFile,
            FileInfo outputFile,
            string nameSpace)
        {
            if (outputFile.Directory != null && !outputFile.Directory.Exists)
            {
                outputFile.Directory.Create();
            }
            if (outputFile.Exists)
            {
                File.Delete(outputFile.FullName);
            }
            generator.WriteAuxiliaryFiles(outputFile.DirectoryName ?? string.Empty);

            try
            {
                var parser = new SchemaParser(inputFile, nameSpace);
                var schema = await parser.Evaluate();
                schema.Validate();
                var compiled = generator.Compile(schema);
                await File.WriteAllTextAsync(outputFile.FullName, compiled);
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