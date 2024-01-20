using System;
using System.CommandLine;
using System.Runtime.InteropServices;
using System.Linq;
using Compiler.Commands;
using Compiler.Options;
using Core.Logging;
using Core.Meta;
using System.Text.Json;
using Core.Exceptions;
using Compiler;

if (RuntimeInformation.OSArchitecture is Architecture.Wasm)
{
    Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
}
Console.OutputEncoding = System.Text.Encoding.UTF8;
DiagnosticLogger.Initialize(LogFormatter.Enhanced);

try
{
    var rootCommand = new CliRootCommand("The Bebop schema language compiler")
    {
        new DiagnosticFormatOption(),
        new BebopConfigOption(),
        new IncludeOption(),
        new ExcludeOption(),
        new InitOption(),
        new ListSchemaOption(),
        new ShowConfigOption(),
        new LocaleOption(),
        new TraceOption(),
        #if !WASI_WASM_BUILD
        new LanguageServerCommand(),
        #endif
        new BuildCommand()
        {
            new GeneratorOption(),
            new NoEmitOption(),
            new NoWarnOption(),
            new StandardInputOption(),
            new StandardOutputOption(),
        },
        #if !WASI_WASM_BUILD
        new WatchCommand()
        {
            new GeneratorOption(),
            new WatchExcludeDirectoriesOption(),
            new WatchExcludeFilesOption(),
            new NoEmitOption(),
            new NoWarnOption(),
            new PreserveWatchOutputOption(),
        },
        #endif
        new ConvertCommand()
        {
            new FromOption(),
            new ToOption(),
            new DryRunOption(),
        },
    };


    rootCommand.SetAction(RootCommand.HandleCommand);

    var results = rootCommand.Parse(args);



    results.Configuration.EnableDefaultExceptionHandler = false;
    results.Configuration.ProcessTerminationTimeout = null;
    if (results.GetValue<bool>(CliStrings.TraceFlag))
    {
        DiagnosticLogger.Instance.EnableTrace();
    }

    var formatter = results.GetValue<LogFormatter?>(CliStrings.DiagnosticFormatFlag);
    DiagnosticLogger.Instance.SetFormatter(formatter ?? LogFormatter.Enhanced);

    if (!results.Errors.Any())
    {
        var parsedConfig = results.GetValue<BebopConfig>(CliStrings.ConfigFlag);
        if (parsedConfig is not null)
        {
            Helpers.MergeConfig(results, parsedConfig);
        }
    }
    return await results.InvokeAsync();
}
catch (Exception e)
{
    switch (e)
    {
        case JsonException:
            return DiagnosticLogger.Instance.WriteDiagonstic(new CompilerException("error during JSON marshaling", e));
        default:
            return DiagnosticLogger.Instance.WriteDiagonstic(e);
    }
}