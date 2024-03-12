namespace Chord.Compiler.Internal.Utils;

internal static class IOUtil
{

    public static string ExeExtension
    {
        get
        {
#if OS_WINDOWS
                return ".exe";
#else
            return string.Empty;
#endif
        }
    }

    public static StringComparison FilePathStringComparison
    {
        get
        {
#if OS_LINUX
            return StringComparison.Ordinal;
#else
            return StringComparison.OrdinalIgnoreCase;
#endif
        }
    }

    /// <summary>
    /// Given a path and directory, return the path relative to the directory.  If the path is not
    /// under the directory the path is returned un modified.  Examples:
    /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src") -> @"project\foo.cpp"
    /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\specs") -> @"d:\src\project\foo.cpp"
    /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\proj") -> @"d:\src\project\foo.cpp"
    /// </summary>
    /// <remarks>Safe for remote paths.  Does not access the local disk.</remarks>
    /// <param name="path">Path to make relative.</param>
    /// <param name="folder">Folder to make it relative to.</param>
    /// <returns>Relative path.</returns>
    public static string MakeRelative(string path, string folder)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(path, nameof(path));
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));

        // Replace all Path.AltDirectorySeparatorChar with Path.DirectorySeparatorChar from both inputs
        path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        folder = folder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        // Check if the dir is a prefix of the path (if not, it isn't relative at all).
        if (!path.StartsWith(folder, FilePathStringComparison))
        {
            return path;
        }

        // Dir is a prefix of the path, if they are the same length then the relative path is empty.
        if (path.Length == folder.Length)
        {
            return string.Empty;
        }

        // If the dir ended in a '\\' (like d:\) or '/' (like user/bin/)  then we have a relative path.
        if (folder.Length > 0 && folder[folder.Length - 1] == Path.DirectorySeparatorChar)
        {
            return path.Substring(folder.Length);
        }
        // The next character needs to be a '\\' or they aren't really relative.
        else if (path[folder.Length] == Path.DirectorySeparatorChar)
        {
            return path.Substring(folder.Length + 1);
        }
        else
        {
            return path;
        }
    }

    public static string ResolvePath(string rootPath, string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath, nameof(rootPath));
        ArgumentException.ThrowIfNullOrEmpty(relativePath, nameof(relativePath));

        if (!Path.IsPathRooted(rootPath))
        {
            throw new ArgumentException($"{rootPath} should be a rooted path.");
        }

        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) > -1)
        {
            throw new InvalidOperationException($"{relativePath} contains invalid path characters.");
        }
        else if (Path.GetFileName(relativePath).IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
        {
            throw new InvalidOperationException($"{relativePath} contains invalid folder name characters.");
        }
        else if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"{relativePath} can not be a rooted path.");
        }
        else
        {
            rootPath = rootPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            relativePath = relativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Root the path
            relativePath = string.Concat(rootPath, Path.AltDirectorySeparatorChar, relativePath);

            // Collapse ".." directories with their parent, and skip "." directories.
            string[] split = relativePath.Split(new[] { Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var segments = new Stack<string>(split.Length);
            int skip = 0;
            for (int i = split.Length - 1; i >= 0; i--)
            {
                string segment = split[i];
                if (string.Equals(segment, ".", StringComparison.Ordinal))
                {
                    continue;
                }
                else if (string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    skip++;
                }
                else if (skip > 0)
                {
                    skip--;
                }
                else
                {
                    segments.Push(segment);
                }
            }

            if (skip > 0)
            {
                throw new InvalidOperationException($"The file path {relativePath} is invalid");
            }

#if OS_WINDOWS
                if (segments.Count > 1)
                {
                    return String.Join(Path.DirectorySeparatorChar, segments);
                }
                else
                {
                    return segments.Pop() + Path.DirectorySeparatorChar;
                }
#else
            return Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, segments);
#endif
        }
    }
}