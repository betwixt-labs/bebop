using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Core.Meta;
using Core.Exceptions;
using Core.Parser;
using Core.Generators;
using Core.Logging;
using System.IO;
using Core;

namespace Compiler;

public class BebopCompiler(CompilerHost Host)
{
    public const int Ok = 0;
    public const int Err = 1;


    public BebopSchema ParseSchema(IEnumerable<string> schemaPaths)
    {
        var parser = new SchemaParser(schemaPaths, Host);
        var schema = parser.Parse();
        schema.Validate();
        return schema;
    }

    public static void EmitGeneratedFiles(List<GeneratedFile> generatedFiles, BebopConfig config)
    {
        foreach (var generatedFile in generatedFiles)
        {
            var outFile = generatedFile.Name;

            // Normalize the path
            if (!Path.IsPathRooted(outFile))
            {
                outFile = Path.GetFullPath(Path.Combine(config.WorkingDirectory, outFile));
            }

            var outDirectory = Path.GetDirectoryName(outFile) ?? throw new CompilerException("Could not determine output directory.");
            if (!Directory.Exists(outDirectory))
            {
                Directory.CreateDirectory(outDirectory);
            }

            File.WriteAllText(outFile, generatedFile.Content);

            if (generatedFile.AuxiliaryFile is not null)
            {
                var auxiliaryOutFile = Path.GetFullPath(Path.Combine(outDirectory, generatedFile.AuxiliaryFile.Name));
                File.WriteAllBytes(auxiliaryOutFile, generatedFile.AuxiliaryFile.Content);
            }
        }
    }


    public GeneratedFile Build(GeneratorConfig generatorConfig, BebopSchema schema, BebopConfig config)
    {
        var (warnings, errors) = GetSchemaDiagnostics(schema, config.SupressedWarningCodes);
        if (!Host.TryGetGenerator(generatorConfig.Alias, out var generator))
        {
            throw new CompilerException($"Could not find generator with alias '{generatorConfig.Alias}'.");
        }

        var compiled = generator.Compile(schema, generatorConfig);
        var auxiliary = generator.GetAuxiliaryFile();
        return new GeneratedFile(generatorConfig.OutFile, compiled, generator.Alias, auxiliary);
    }

    public static (List<SpanException> Warnings, List<SpanException> Errors) GetSchemaDiagnostics(BebopSchema schema, int[] supressWarningCodes)
    {
        var noWarn = supressWarningCodes;
        var loudWarnings = schema.Warnings.Where(x => !noWarn.Contains(x.ErrorCode)).ToList();
        return (loudWarnings, schema.Errors);
    }
}