using System.Runtime.InteropServices;

namespace Chord.Compiler.Internal.Utils;

internal static class PathUtil
{
#if OS_WINDOWS
    public static readonly string PathVariable = "Path";
#else
    public static readonly string PathVariable = "PATH";
#endif

    public static string PrependPath(string path, string currentPath)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(path, nameof(path));
        if (string.IsNullOrEmpty(currentPath))
        {
            // Careful not to add a trailing separator if the PATH is empty.
            // On OSX/Linux, a trailing separator indicates that "current directory"
            // is added to the PATH, which is considered a security risk.
            return path;
        }

        // Not prepend path if it is already the first path in %PATH%
        if (currentPath.StartsWith(path + Path.PathSeparator, IOUtil.FilePathStringComparison))
        {
            return currentPath;
        }
        else
        {
            return path + Path.PathSeparator + currentPath;
        }
    }

    /// <summary>
    /// Determines if the given program is on the PATH.
    /// </summary>
    /// <param name="program">The name of the program</param>
    /// <returns>true if the program is on the PATH; otherwise false.</returns>
    public static bool IsProgramOnPath(string program)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            program += IOUtil.ExeExtension;
        }

        var path = Environment.GetEnvironmentVariable(PathVariable);
        if (path is null)
        {
            return false;
        }

        var paths = path.Split(Path.PathSeparator);
        foreach (var pathEntry in paths)
        {
            var fullPath = Path.Combine(pathEntry, program);
            if (File.Exists(fullPath))
            {
                return true;
            }
        }

        return false;
    }
}