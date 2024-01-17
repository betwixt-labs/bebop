using System.Runtime.InteropServices;

namespace Chord.Common;

public static class StoragePath
{
    public static string BebopcData => FindDataDirectory("betwixt/bebopc");
    public static string FindDataDirectory(string appName)
    {
        string appDataDir;
        // Check for an override in environment variables
        var overrideDir = Environment.GetEnvironmentVariable("APP_DATA_DIR_OVERRIDE");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return Path.Combine(overrideDir, appName);
        }
        // Determine the path based on the OS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            appDataDir = Environment.GetEnvironmentVariable("APPDATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            appDataDir = "/var";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            appDataDir = "/var";
        }
        else if (OperatingSystem.IsWasi())
        {
            appDataDir = "/";
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }
        return Path.Combine(appDataDir, appName, "extensions");
    }
}