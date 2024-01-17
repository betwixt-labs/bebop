using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Chord.Compiler;

internal static class Helpers
{

    public const string ManifestFileName = "chord.json";

    internal static bool TryReadFile(string path, [NotNullWhen(true)] out string? content)
    {
        try
        {
            content = File.ReadAllText(path);
            return true;
        }
        catch
        {
            content = null;
            return false;
        }
    }

    /// <summary>
    /// Searches for a chord.json file in the current directory or any parent directories.
    /// </summary>
    /// <returns>The fully qualified path to the manifest, or null if none was found.</returns>
    internal static string? LocateChordManifest()
    {
        try
        {
            var workingDirectory = Directory.GetCurrentDirectory();
            var configFile = Directory.GetFiles(workingDirectory, ManifestFileName).FirstOrDefault();
            while (string.IsNullOrWhiteSpace(configFile))
            {
                if (Directory.GetParent(workingDirectory) is not { Exists: true } parent)
                {
                    break;
                }
                workingDirectory = parent.FullName;
                if (parent.GetFiles(ManifestFileName)?.FirstOrDefault() is { Exists: true } file)
                {
                    configFile = file.FullName;
                }
            }
            return configFile;
        }
        catch
        {
            return null;
        }
    }
}