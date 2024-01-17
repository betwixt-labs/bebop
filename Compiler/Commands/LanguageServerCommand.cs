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
        Dictionary<string, string> extensions = [];
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag);
        if (config is not null)
        {
            extensions = config.Extensions;
        }
        using var host = CompilerHost.CompilerHostBuilder.Create().WithDefaultDecorators().WithExtensions(extensions).Build();
        await BebopLangServer.RunAsync(host, token);
#endif
        return BebopCompiler.Ok;
    }
}