using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.Meta;

namespace Compiler.Commands;

public class WatchCommand : CliCommand
{
    public WatchCommand() : base(CliStrings.WatchCommand, "Watch input files.")
    {
        SetAction(HandleCommandAsync);
    }

    private async Task<int> HandleCommandAsync(ParseResult result, CancellationToken token)
    {
        var config = result.GetValue<BebopConfig>(CliStrings.ConfigFlag)!;
        config.Validate();
        using var host = CompilerHost.CompilerHostBuilder.Create(config.WorkingDirectory)
       .WithDefaults()
#if !WASI_WASM_BUILD
       .WithExtensions(config.Extensions)
#endif
       .Build();

        Helpers.WriteHostInfo(host);
        var compiler = new BebopCompiler(host);
        var watcher = new SchemaWatcher(config.WorkingDirectory, config, compiler);
        return await watcher.StartAsync(token);
    }
}