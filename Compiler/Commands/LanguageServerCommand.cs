using System.Collections.Generic;
using System.CommandLine;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Compiler.LangServer;
using Core;
using Core.Logging;
using Core.Meta;

namespace Compiler.Commands;

public class LanguageServerCommand : CliCommand
{
    public LanguageServerCommand() : base(CliStrings.LangServerCommand, "Start the language server")
    {
        Hidden = true;
        SetAction(HandleCommandAsync);
    }

    private async Task<int> HandleCommandAsync(ParseResult result, CancellationToken token)
    {
#if !WASI_WASM_BUILD
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag)!;
        var builder = CompilerHost.CompilerHostBuilder.Create(config.WorkingDirectory).WithDefaultDecorators();

#if !WIN_ARM64
        builder = builder.WithExtensions(config.Extensions);
#endif

        using var host = builder.Build();

        await BebopLangServer.RunAsync(host, token);
#endif

        return BebopCompiler.Ok;
    }
}