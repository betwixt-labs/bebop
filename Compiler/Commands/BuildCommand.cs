using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Compiler.Commands;

public class BuildCommand : CliCommand
{
    public BuildCommand() : base("build", "Builds the source code from schema files.")
    {
        SetAction(HandleCommandAsync);
    }

     private async Task<int> HandleCommandAsync(ParseResult result, CancellationToken token)
    {
        var config = result.GetValue<BebopConfig>("--config");
       
        if (Console.IsInputRedirected)
        {
            var input = await Console.In.ReadToEndAsync(token);
            Console.WriteLine("Input redirected: " + input);
        }
        else
        {
            Console.WriteLine("Input not redirected");
        }
        return 1;
    }
}