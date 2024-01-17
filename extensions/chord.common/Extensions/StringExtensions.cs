namespace Chord.Common;

internal static class StringExtensions
{

    internal static bool IsLegalPath(this string path, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        // Check for invalid path characters
        var invalidPathChars = Path.GetInvalidPathChars();
        var invalidPathCharIndex = path.IndexOfAny(invalidPathChars);
        if (invalidPathCharIndex >= 0)
        {
            index = invalidPathCharIndex;
            return false;
        }
        return true;
    }

    internal static bool IsLegalFilePath(this string filePath, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        // Check for invalid path characters in the entire filePath
        if (!IsLegalPath(filePath, out index))
        {
            return false;
        }

        // Extract the file name from the path and check for invalid file name characters
        var fileName = Path.GetFileName(filePath);
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        var invalidFileNameCharIndex = fileName.IndexOfAny(invalidFileNameChars);
        if (invalidFileNameCharIndex >= 0)
        {
            // Adjust the index to be in the context of the full filePath, not just fileName
            index = filePath.LastIndexOf(fileName) + invalidFileNameCharIndex;
            return false;
        }
        return true;
    }

     public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest) {

        first = list.Count > 0 ? list[0] : default(T); // or throw
        rest = list.Skip(1).ToList();
    }

    public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest) {
        first = list.Count > 0 ? list[0] : default(T); // or throw
        second = list.Count > 1 ? list[1] : default(T); // or throw
        rest = list.Skip(2).ToList();
    }
}