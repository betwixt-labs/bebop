using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
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
        var watcher = new SchemaWatcher(config.WorkingDirectory, config);
        return await watcher.StartAsync(token);
    }
}