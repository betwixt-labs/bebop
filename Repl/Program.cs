using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


namespace Repl
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<Repl.App>("#app");
            await builder.Build().RunAsync();
        }
    }
}