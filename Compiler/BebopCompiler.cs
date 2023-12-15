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

namespace Compiler;

public class BebopCompiler
{
    public const int Ok = 0;
    public const int Err = 1;

    public static BebopSchema ParseSchema(IEnumerable<string> schemaPaths)
    {
        var parser = new SchemaParser(schemaPaths);
        var schema = parser.Parse();
        schema.Validate();
        return schema;
    }

    public static void Build(GeneratorConfig generatorConfig, BebopSchema schema, BebopConfig config)
    {
        var (warnings, errors) = GetSchemaDiagnostics(schema, config.SupressedWarningCodes);
        var generator = GeneratorUtils.ImplementedGenerators[generatorConfig.Alias](schema, generatorConfig);
        var compiled = generator.Compile();
        var auxiliary = generator.GetAuxiliaryFile();
        if (string.Equals(generatorConfig.OutFile, "stdout", StringComparison.OrdinalIgnoreCase))
        {
            DiagnosticLogger.Instance.WriteCompilerOutput(new CompilerOutput(warnings, errors, new GeneratedFile("stdout", compiled, generator.Alias, auxiliary)));
            return;
        }
        var outFile = generatorConfig.OutFile;
        if (!Path.IsPathRooted(outFile))
        {
            outFile = Path.Combine(config.WorkingDirectory, outFile);
        }
        var outDirectory = Path.GetDirectoryName(outFile) ?? throw new CompilerException("Could not determine output directory.");
        if (!Directory.Exists(outDirectory))
        {
            Directory.CreateDirectory(outDirectory);
        }
        File.WriteAllText(outFile, compiled);
        if (auxiliary is not null)
        {
            File.WriteAllText(Path.Combine(outDirectory, auxiliary.Name), auxiliary.Contents);
        }
    }

    public static (List<SpanException> Warnings, List<SpanException> Errors) GetSchemaDiagnostics(BebopSchema schema, int[] supressWarningCodes)
    {
        var noWarn = supressWarningCodes;
        var loudWarnings = schema.Warnings.Where(x => !noWarn.Contains(x.ErrorCode)).ToList();
        return (loudWarnings, schema.Errors);
    }
}