using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Compiler.Commands;

public class WatchCommand : CliCommand
{
    public WatchCommand() : base("watch", "Watches the source code from schema files and rebuilds on changes.")
    {
        SetAction(HandleCommandAsync);
    }

     private async Task<int> HandleCommandAsync(ParseResult result, CancellationToken token)
    {
        Console.WriteLine("WatchCommand");
        var config = result.GetValue<BebopConfig>("--config");
        Console.WriteLine(config.Namespace);
        var generators = result.GetValue<Core.Generators.GeneratorConfig[]>("--generator");
        foreach (var generator in generators)
        {
            Console.WriteLine(generator);
            Console.WriteLine(generator.GetOptionRawValue("setting1"));
        }
        return 1;
    }
}