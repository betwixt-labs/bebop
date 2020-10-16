using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Generators;
using Compiler.Generators.TypeScript;
using Compiler.Parser;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;
using vtortola.WebSockets.Rfc6455;

namespace Compiler
{
    class Program
    {
        static void Usage()
        {
            Console.Error.WriteLine("Usage:\n");
            Console.Error.WriteLine("    pierogic --server                       Run a WebSocket server on localhost.");
            Console.Error.WriteLine("    pierogic --lang ts [outdir] [schemas]   Compile schemas for TypeScript into outdir.");
            Console.Error.WriteLine("    pierogic --lang cs [outdir] [schemas]   Compile schemas for C# into outdir.");
            Console.Error.WriteLine("");
        }

        static async Task<int> Main(string[] args)
        {
            var generators = new Dictionary<string, IGenerator> {
                { "ts", new TypeScriptGenerator() }
            };

            switch (args.Length == 0 ? null : args[0])
            {
                case "--server":
                    await RunWebServer();
                    return 0;
                case "--lang" when args.Length >= 4: // at least one schema
                    var language = args[1];
                    var outputPath = args[2];
                    var schemaPaths = args.Skip(3);
                    if (!generators.ContainsKey(language))
                    {
                        Console.Error.WriteLine($"Unsupported language: {language}.");
                        Usage();
                        return 1;
                    }
                    var generator = generators[language];
                    await CompileSchemas(generator, outputPath, schemaPaths);
                    return 0;
                default:
                    Usage();
                    return 1;
            }
        }

        static void ReportError(string problem, string reason = "")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(problem);
            Console.ResetColor();
            Console.Error.WriteLine(string.IsNullOrWhiteSpace(reason) ? "" : " " + reason);
        }

        static async Task CompileSchemas(IGenerator generator, string outputPath, IEnumerable<string> schemaPaths)
        {
            generator.WriteAuxiliaryFiles(outputPath);
            foreach (var path in schemaPaths)
            {
                try
                {
                    var parser = new SchemaParser(path);
                    var schema = await parser.Evaluate();
                    schema.Validate();
                    var compiled = generator.Compile(schema);
                    await File.WriteAllTextAsync(Path.Join(outputPath, generator.OutputFileName(schema)), compiled);
                }
                catch (SpanException e)
                {
                    ReportError($"Error in {e.SourcePath} at {e.Span.StartColonString()}:", e.Message);
                }
                catch (FileNotFoundException)
                {
                    ReportError($"File {path} was not found.");
                }
                catch (Exception e)
                {
                    ReportError($"Error when processing {path}:", e.ToString());
                }
            }
        }

        static async Task RunWebServer()
        {
            var cancellation = new CancellationTokenSource();

            var bufferSize = 1024 * 8; // 8KiB
            var bufferPoolSize = 100 * bufferSize; // 800KiB pool

            var options = new WebSocketListenerOptions
            {
                SubProtocols = new[] { "text" },
                PingTimeout = TimeSpan.FromSeconds(5),
                NegotiationTimeout = TimeSpan.FromSeconds(5),
                PingMode = PingMode.Manual,
                ParallelNegotiations = 16,
                NegotiationQueueCapacity = 256,
                BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize)
            };
            options.Standards.RegisterRfc6455(factory =>
            {
                factory.MessageExtensions.RegisterDeflateCompression();
            });
            // configure tcp transport
            options.Transports.ConfigureTcp(tcp =>
            {
                tcp.BacklogSize = 100; // max pending connections waiting to be accepted
                tcp.ReceiveBufferSize = bufferSize;
                tcp.SendBufferSize = bufferSize;
            });

            // adding the WSS extension
            //var certificate = new X509Certificate2(File.ReadAllBytes("<PATH-TO-CERTIFICATE>"), "<PASSWORD>");
            // options.ConnectionExtensions.RegisterSecureConnection(certificate);

            var listenEndPoints = new Uri[] {
                new Uri("ws://localhost") // will listen both IPv4 and IPv6
            };

            // starting the server
            var server = new WebSocketListener(listenEndPoints, options);

            await server.StartAsync();

            Console.WriteLine("Echo Server listening: " + string.Join(", ", Array.ConvertAll(listenEndPoints, e => e.ToString())) + ".");
            Console.WriteLine("You can test echo server at http://www.websocket.org/echo.html.");

            var acceptingTask = AcceptWebSocketsAsync(server, cancellation.Token);

            Console.WriteLine("Press any key to stop.");
            Console.ReadKey(true);

            Console.WriteLine("Server stopping.");
            cancellation.Cancel();
            await server.StopAsync();
            await acceptingTask;

        }

        private static async Task AcceptWebSocketsAsync(WebSocketListener server, CancellationToken cancellation)
        {
            await Task.Yield();

            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    var webSocket = await server.AcceptWebSocketAsync(cancellation).ConfigureAwait(false);
                    if (webSocket == null)
                    {
                        if (cancellation.IsCancellationRequested || !server.IsStarted)
                            break; // stopped

                        continue; // retry
                    }

#pragma warning disable 4014
                    EchoAllIncomingMessagesAsync(webSocket, cancellation);
#pragma warning restore 4014
                }
                catch (OperationCanceledException)
                {
                    /* server is stopped */
                    break;
                }
                catch (Exception acceptError)
                {
                    //  Log.Error("An error occurred while accepting client.", acceptError);
                }
            }

            //  Log.Warning("Server has stopped accepting new clients.");
        }

        private static async Task EchoAllIncomingMessagesAsync(WebSocket webSocket, CancellationToken cancellation)
        {
            //  Log.Warning("Client '" + webSocket.RemoteEndpoint + "' connected.");
            // var sw = new Stopwatch();
            try
            {
                while (webSocket.IsConnected && !cancellation.IsCancellationRequested)
                {
                    try
                    {
                        var messageText = await webSocket.ReadStringAsync(cancellation).ConfigureAwait(false);
                        if (messageText == null)
                            break; // webSocket is disconnected

                        try
                        {
                            var parser = new SchemaParser("ErrorSchema", messageText);
                            var schema = await parser.Evaluate();
                            schema.Validate();
                            await webSocket.WriteStringAsync(new TypeScriptGenerator().Compile(schema), cancellationToken: cancellation);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            await webSocket.WriteStringAsync(ex.Message, cancellationToken: cancellation);
                        }

                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception readWriteError)
                    {
                        await webSocket.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                webSocket.Dispose();
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {

        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

        }
    }
}
