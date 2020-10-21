using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Generators;
using Compiler.Parser;

namespace Compiler
{
    class Program
    {
        
        static async Task<int> Main(string[] args)
        {
            if (!CommandLineFlags.TryParse(args, out var flags, out var message))
            {
                Console.WriteLine(message);
                Console.WriteLine(flags.HelpText);
                return 1;
            }

            if (flags.Version)
            {
                Console.WriteLine($"pierogic {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}");
                return 0;
            }

            if (flags.Help)
            {
                Console.WriteLine(flags.HelpText);
                return 0;
            }

            /*var generators = new Dictionary<string, IGenerator> {
                { "ts", new TypeScriptGenerator() },
                { "cs", new CSharpGenerator() }
            };

            switch (args.Length == 0 ? null : args[0])
            {
                case "--server":
                    await RunWebServer();
                    return 0;
                case "--lang" when args.Length >= 4: // at least one schema
                    var language = args[1];
                    var outputPath = args[2];
                    var schemaPaths = args.Skip(3);
                    if (!generators.ContainsKey(language))
                    {
                        Console.Error.WriteLine($"Unsupported language: {language}.");
                        Usage();
                        return 1;
                    }
                    var generator = generators[language];
                    await CompileSchemas(generator, outputPath, schemaPaths);
                    return 0;
                default:
                    Usage();
                    return 1;
            }*/
            return 1;
        }

        static void ReportError(string problem, string reason = "")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(problem);
            Console.ResetColor();
            Console.Error.WriteLine(string.IsNullOrWhiteSpace(reason) ? "" : " " + reason);
        }

        static async Task CompileSchemas(IGenerator generator, string outputPath, IEnumerable<string> schemaPaths)
        {
            generator.WriteAuxiliaryFiles(outputPath);
            foreach (var path in schemaPaths)
            {
                try
                {
                    var parser = new SchemaParser(path);
                    var schema = await parser.Evaluate();
                    schema.Validate();
                    var compiled = generator.Compile(schema);
                    await File.WriteAllTextAsync(Path.Join(outputPath, generator.OutputFileName(schema)), compiled);
                }
                catch (SpanException e)
                {
                    ReportError($"Error in {e.SourcePath} at {e.Span.StartColonString()}:", e.Message);
                }
                catch (FileNotFoundException)
                {
                    ReportError($"File {path} was not found.");
                }
                catch (Exception e)
                {
                    ReportError($"Error when processing {path}:", e.ToString());
                }
            }
        }
    }
}
