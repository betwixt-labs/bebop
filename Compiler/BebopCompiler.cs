using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Generators;
using Core.Logging;
using Core.Meta;
using Core.Parser;
using Core.Exceptions;

namespace Compiler;

public class BebopCompiler
{
    public const int Ok = 0;
    public const int Err = 1;

    public CommandLineFlags Flags { get; }

    public BebopCompiler(CommandLineFlags flags)
    {
        Flags = flags;
    }

    private async Task<BebopSchema> ParseAndValidateSchema(List<string> schemaPaths, string nameSpace)
    {
        var parser = new SchemaParser(schemaPaths, nameSpace);
        var schema = await parser.Parse();
        schema.Validate();
        return schema;
    }

    public async Task<int> CompileSchema(Func<BebopSchema, BaseGenerator> makeGenerator,
        string textualSchema, string outputFile, string nameSpace, TempoServices services, Version? langVersion)
    {
        var parser = new SchemaParser(textualSchema, nameSpace);
        var schema = await parser.Parse();
        schema.Validate();

        var diagonstics = GetSchemaDiagnostics(schema);
        if (diagonstics.Errors.Count > 0)
        {
            var errors = new CompilerOutput(diagonstics.Warnings, diagonstics.Errors, null);
            DiagnosticLogger.Instance.WriteCompilerOutput(errors);
            return Err;
        }
         var generator = makeGenerator(schema);
        var compiled = generator.Compile(langVersion, services: services, writeGeneratedNotice: Flags?.SkipGeneratedNotice ?? false, emitBinarySchema: Flags?.EmitBinarySchema ?? false);
        var auxiliary = generator.GetAuxiliaryFile();
        var generatedFile = new GeneratedFile(outputFile, compiled,  generator.Alias, auxiliary);
        var results = new CompilerOutput(diagonstics.Warnings, diagonstics.Errors, generatedFile);

        DiagnosticLogger.Instance.WriteCompilerOutput(results);
        return Ok;
    }

    public async Task<int> CompileSchema(Func<BebopSchema, BaseGenerator> makeGenerator,
        List<string> schemaPaths,
        FileInfo outputFile,
        string nameSpace, TempoServices services, Version? langVersion)
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
        var compiled = generator.Compile(langVersion, services: services, writeGeneratedNotice: Flags?.SkipGeneratedNotice ?? false, emitBinarySchema: Flags?.EmitBinarySchema ?? false);
        await File.WriteAllTextAsync(outputFile.FullName, compiled);
        return Ok;
    }

    private (List<SpanException> Warnings, List<SpanException> Errors) GetSchemaDiagnostics(BebopSchema schema)
    {
        var noWarn = Flags?.NoWarn ?? new List<string>();
        var loudWarnings = schema.Warnings.Where(x => !noWarn.Contains(x.ErrorCode.ToString())).ToList();
        return (loudWarnings, schema.Errors);
    }

    private async Task<int> ReportSchemaDiagnostics(BebopSchema schema)
    {
        var noWarn = Flags?.NoWarn ?? new List<string>();
        var loudWarnings = schema.Warnings.Where(x => !noWarn.Contains(x.ErrorCode.ToString()));
        var errors = loudWarnings.Concat(schema.Errors).ToList();
        DiagnosticLogger.Instance.WriteSpanDiagonstics(errors);
        return schema.Errors.Count > 0 ? Err : Ok;
    }

    public async Task<int> CheckSchema(string textualSchema)
    {
        var parser = new SchemaParser(textualSchema, "CheckNameSpace");
        var schema = await parser.Parse();
        schema.Validate();
        return await ReportSchemaDiagnostics(schema);
    }

    public async Task<int> CheckSchemas(List<string> schemaPaths)
    {
        var schema = await ParseAndValidateSchema(schemaPaths, "CheckNameSpace");
        return await ReportSchemaDiagnostics(schema);
    }
}