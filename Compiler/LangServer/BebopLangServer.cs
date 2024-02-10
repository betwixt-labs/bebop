using System;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using OmnisharpLanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace Compiler.LangServer
{
    internal sealed class BebopLangServer
    {
        public static async Task RunAsync(CompilerHost host, CancellationToken cancellationToken)
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
                        services.AddSingleton<CompilerHost>(host);
                    })
                    .WithHandler<CompletionHandler>()
                    .WithHandler<SemanticTokenHandler>()
                    .WithHandler<HoverHandler>()
                    .WithHandler<TextDocumentSyncHandler>(), cancellationToken);


            await server.WaitForExit;

        }
    }
}
