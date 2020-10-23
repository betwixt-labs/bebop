using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compiler.Meta;

namespace Compiler.IO
{
    /// <summary>
    ///     A class for combining multiple schema files into a single source.
    /// </summary>
    public static class SchemaFuser
    {
        private static readonly Lager Log = Lager.CreateLogger("Fuser");

        public static bool TryCoalescing(string directory, out FileInfo? combinedSchema)
        {
            combinedSchema = null;
            var directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists)
            {
                Log.Error($"{directoryInfo.FullName} does not cannot be found.");
                return false;
            }
            var files = directoryInfo.GetFiles($"*.{ReservedWords.SchemaExt}", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Log.Error($"{directoryInfo.FullName} contains no schema files.");
                return false;
            }
            return TryCoalescing(files.Where(f => f.Exists).Select(f => f.FullName).ToList(),
                out combinedSchema);
        }

        public static bool TryCoalescing(List<string> filePaths, out FileInfo combinedSchema)
        {
          
            const int chunkSize = 2048;

            var tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), ReservedWords.CompilerName));
            Directory.CreateDirectory(tempDir);
            string tempFile = Path.Combine(tempDir, Path.GetFileName(Path.GetTempFileName()));

            using var tempFileStream = File.Create(tempFile, chunkSize, FileOptions.WriteThrough);

            Log.Info($"Coalescing {filePaths.Count} schemas...");
            for (int i = 0; i < filePaths.Count; i++)
            {
                var filePath = filePaths[i];
                using var input = File.OpenRead(filePath);
                var buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    tempFileStream.Write(buffer, 0, bytesRead);
                }
                Log.Info($"{filePath} {i + 1}/{filePaths.Count}");
            }

            tempFileStream.Flush();
            combinedSchema = new FileInfo(tempFile);
            return true;
        }
    }
}