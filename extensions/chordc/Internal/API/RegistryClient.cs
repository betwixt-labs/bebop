using System.Net.Http.Json;
using System.Security.Authentication;
using Chord.Common;
using Chord.Compiler.Internal.API.Responses;
using Spectre.Console;

namespace Chord.Compiler.Internal.API;

internal sealed partial class RegistryClient : IDisposable
{
    private const string RegistryUrl = "https://console.betwixtlabs.com";

    private readonly HttpClientHandler _httpClientHandler;
    private readonly HttpClient _httpClient;

    public RegistryClient(string? authToken = null)
    {
        _httpClientHandler = new HttpClientHandler
        {
            UseCookies = false,
            SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
        };

        _httpClient = new HttpClient(_httpClientHandler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "chordc/0.1.0");
        if (!string.IsNullOrEmpty(authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        }
    }

    public async Task<PublishResponse?> PublishAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
        // Create a linked token source for combined cancellation
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = linkedCts.Token;

        var response = await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var uploadTask = ctx.AddTask("[green]Publishing extension[/]", maxValue: fileStream.Length);

                using var contentStream = new ProgressableStreamContent(fileStream, (uploaded, total) =>
                {
                    uploadTask.Value = uploaded;
                }, linkedToken);


                contentStream.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/chord-package");

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{RegistryUrl}/api/extension/publish") { Content = contentStream };
                var response = await _httpClient.SendAsync(requestMessage, linkedToken);
                var publishResponse = await response.Content.ReadFromJsonAsync<PublishResponse>(
                        JsonContext.Default.PublishResponse, cancellationToken: linkedToken);
                return publishResponse;
            });
        return response;
    }



    public async Task<bool> DownloadChordAsync(string chord, ProgressTask progressTask, bool forceDownload = false, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateChord(chord, out string requestedChord, out string requestedVersion);

            var versions = await FetchRegistryCatalog(requestedChord, progressTask, cancellationToken);
            if (versions is null)
            {
                return false;
            }

            var registryVersion = FindRegistryVersion(versions, requestedVersion);
            if (registryVersion is null)
            {
                progressTask.Description = $"[maroon]{requestedChord}@{requestedVersion} is not published[/]";
                return false;
            }

            var destinationPath = GetDestinationPath(requestedChord, registryVersion.Version);
            if (File.Exists(destinationPath) && !forceDownload)
            {
                progressTask.Description = $"[yellow]Skipping [bold]{requestedChord}@{registryVersion.Version}[/][/] ";
                progressTask.Value = progressTask.MaxValue;
                return true;
            }
            var result = await DownloadChordAsync(requestedChord, registryVersion, destinationPath, progressTask, cancellationToken);
            if (result)
            {
                progressTask.Description = $":check_mark_button: [green][bold]{requestedChord}@{registryVersion.Version}[/] installed successfully.[/] ";
            }
            return result;
        }
        catch (Exception ex)
        {
            progressTask.Description = $":cross_mark: [maroon]unknown error when downloading chord.[/]";
            if (Logger.IsVerbose)
            {
                Logger.Error.WriteException(ex);
            }
            return false;
        }
    }

    public async Task<RegistryCatalog?> FetchRegistryCatalog(string chord, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        var versionsUrl = $"{ContentUrl}/{chord}/versions.json";

        try
        {
            return await _httpClient.GetFromJsonAsync(versionsUrl, JsonContext.Default.RegistryCatalog, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            HandleHttpRequestException(ex, $"error fetching version info for [bold]{chord}[/] ({ex.StatusCode})", progressTask);
            return null;
        }
    }

    private static void HandleHttpRequestException(HttpRequestException ex, string contextMessage, ProgressTask? progressTask = null)
    {
        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            if (progressTask is not null)
            {
                progressTask.Description = $":cross_mark: [maroon]{contextMessage}[/]";
            }
            else
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]{contextMessage}[/]");
            }
        }
        else
        {
            if (progressTask is not null)
            {
                progressTask.Description = $":cross_mark: [maroon]unknown error when {contextMessage}.[/]";
            }
            else
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]unknown error when {contextMessage}. [/]");
            }
            Logger.Error.WriteException(ex);
        }
    }

    public void Dispose()
    {
        _httpClientHandler.Dispose();
        _httpClient.Dispose();
    }
}