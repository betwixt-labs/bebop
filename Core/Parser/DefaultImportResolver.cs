using System.IO;

namespace Core.Parser
{
    internal sealed class DefaultImportResolver : IImportResolver
    {
        public static DefaultImportResolver Shared { get; } = new DefaultImportResolver(); 

        public string GetPath(string currentFile, string relativeFile)
        {
            var currentFileDirectory = Path.GetDirectoryName(currentFile) ?? string.Empty;
            var combinedPath = Path.Combine(currentFileDirectory, relativeFile);
            return Path.GetFullPath(combinedPath);
        }
    }
}
