using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Compiler.LangServer;
using Compiler.Options;

namespace Compiler.Commands;

public class LanguageServerCommand : CliCommand
{
    public LanguageServerCommand() : base("langserver", "Start the language server")
    {
        SetAction(HandleCommandAsync);
    }

    private async Task<int> HandleCommandAsync(ParseResult result, CancellationToken token)
    {
        await BebopLangServer.RunAsync(token);
        return BebopCompiler.Ok;
    }
}