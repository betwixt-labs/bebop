using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Core.Meta;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Repl
{
    public class Program
    {
        public static readonly string ExampleSchema = @"
enum FurnitureFamily {
    Bed = 0;
    Table = 1;
    Shoe = 2;
}

readonly struct Furniture {
    string name;
    uint32 price;
	FurnitureFamily family;
}

[opcode(""IKEA"")]
message RequestCatalog {
    FurnitureFamily family = 1;
	[deprecated(""Nobody react to what I'm about to say..."")]
	string secretTunnel = 2;
}

[opcode(0x31323334)]
readonly struct RequestResponse {
    Furniture[] availableFurniture;
}
";
        public static async Task Main(string[] args)
        {
            

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
    }
}
