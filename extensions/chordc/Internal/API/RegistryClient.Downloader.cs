using Chord.Common;
using Chord.Compiler.Internal.API.Responses;
using Spectre.Console;

namespace Chord.Compiler.Internal.API;

internal sealed partial class RegistryClient
{
    private const string ContentUrl = "https://betwixtusercontent.com/chord";

    private static void ValidateChord(string chord, out string requestedChord, out string requestedVersion)
    {
        if (string.IsNullOrWhiteSpace(chord))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(chord));
        }

        // Split the chord by '@', but only at the last occurrence to ensure versions with '@' in names are handled.
        int atIndex = chord.LastIndexOf('@');

        if (atIndex > 0)
        {
            requestedChord = chord[..atIndex];
            requestedVersion = chord[(atIndex + 1)..];
        }
        else
        {
            requestedChord = chord;
            requestedVersion = "latest"; // Default to 'latest' if no version is specified.
        }

        // Ensure requestedChord is not null or whitespace, even after trimming the '@' symbol.
        if (string.IsNullOrWhiteSpace(requestedChord))
        {
            throw new InvalidDataException("Chord ID cannot be null or whitespace.");
        }
    }


    private static RegistryVersion? FindRegistryVersion(RegistryCatalog versions, string requestedVersion)
    {


        return requestedVersion == "latest"
            ? versions.Versions.FirstOrDefault(v => v.Version == versions.Latest)
            : versions.Versions.FirstOrDefault(v => v.Version == requestedVersion);
    }

    private static string GetDestinationPath(string chord, string version)
    {
        string? scope = null;
        string name = chord;

        // Check if 'chord' contains a scope (i.e., starts with '@')
        if (chord.StartsWith("@"))
        {
            var parts = chord.Split('/', 2); // Split into scope and name
            if (parts.Length == 2)
            {
                scope = parts[0]; // '@betwixt'
                name = parts[1];  // 'example'
            }
            else
            {
                throw new InvalidOperationException("Invalid chord format. Expected '@scope/name'.");
            }
        }

        // Construct the full path
        var basePath = StoragePath.BebopcData;
        var destinationPath = scope != null
            ? Path.Combine(basePath, scope, name, version, "chord.wasm")  // With scope
            : Path.Combine(basePath, name, version, "chord.wasm");         // Without scope

        // Ensure the directory exists
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrEmpty(destinationDirectory))
        {
            throw new InvalidOperationException("Destination directory cannot be null or empty.");
        }

        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        return destinationPath;
    }

    private async Task DownloadFileAsync(string url, string destinationPath, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var buffer = new byte[8192];

        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await CopyContentAsync(contentStream, fileStream, buffer, progressTask, totalBytes, cancellationToken);
    }

    private static async Task CopyContentAsync(Stream source, Stream destination, byte[] buffer, ProgressTask progressTask, long totalBytes, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0L;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            progressTask.Value = (double)totalBytesRead / totalBytes * 100;
        }
    }

    private async Task<bool> DownloadChordAsync(string chord, RegistryVersion version, string destinationPath, ProgressTask progressTask, CancellationToken cancellationToken)
    {
        var chordUrl = $"{ContentUrl}/{chord}/{version.Version}/chord.wasm";
        try
        {
            await DownloadFileAsync(chordUrl, destinationPath, progressTask, cancellationToken);
            return true;
        }
        catch (HttpRequestException ex)
        {
            HandleHttpRequestException(ex, $"fetching chord [bold]{chord}[/] version [bold]{version.Version}[/]");
            return false;
        }
    }
}