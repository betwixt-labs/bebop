using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using OmnisharpLanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace Compiler.LangServer
{
    internal sealed class BebopLangServer
    {
        public static async Task RunAsync()
        {
            var server = await OmnisharpLanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(new LoggerFactory())
                    .AddDefaultLoggingProvider()
                    .WithServices(services =>
                    {
                        services.AddSingleton<BufferManager>();
                        services.AddSingleton<BebopLangServerLogger>();
                        services.AddSingleton<BebopDiagnosticPublisher>();
                    })
                    .WithHandler<CompletionHandler>()
                    .WithHandler<SemanticTokenHandler>()
                    .WithHandler<TextDocumentSyncHandler>());

            await server.WaitForExit;
        }
    }
}
