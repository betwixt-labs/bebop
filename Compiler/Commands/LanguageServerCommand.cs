using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Compiler.LangServer;

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
        await BebopLangServer.RunAsync(token);
        #endif
        return BebopCompiler.Ok;
    }
}