using System;
using System.CommandLine;
using System.IO;
#if WASI_WASM_BUILD
using Core.Exceptions;
#endif
using Core.Generators;
using Core.Meta;

namespace Compiler;

public static class Helpers
{

    /// <summary>
    ///  Creates a uniquely named, zero-byte temporary file on disk and returns the full path of that file.
    /// </summary>
    /// <returns></returns>
    public static string GetTempFileName()
    {
#if WASI_WASM_BUILD
        const string tempDirectory = "/tmp";
        try
        {
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }
            var tempFileName = Path.Combine(tempDirectory, Path.GetRandomFileName());
            File.Create(tempFileName).Dispose();
            return tempFileName;
        }
        catch (Exception ex)
        {
            throw new CompilerException($"Could not create temporary file in {tempDirectory}.", ex);
        }
#else
        return Path.GetTempFileName();
#endif
    }

    /// <summary>
    /// Merges the results of a bebopc command line parse into the bebop.json config instance.
    /// </summary>
    /// <remarks>
    ///  When options are supplied on the command line, the corresponding bebop.json fields will be ignored.
    ///  </remarks>
    /// <param name="parseResults">The parsed commandline.</param>
    public static void MergeConfig(ParseResult parseResults, BebopConfig config)
    {
        if (parseResults.GetValue<string[]>(CliStrings.IncludeFlag) is { Length: > 0 } includes)
        {
            config.Includes = includes;
        }
        if (parseResults.GetValue<string[]>(CliStrings.ExcludeFlag) is { Length: > 0 } excludes)
        {
            config.Excludes = excludes;
        }
#if !WASI_WASM_BUILD
        if (parseResults.GetValue<string[]>(CliStrings.ExcludeDirectoriesFlag) is { Length: > 0 } watchExcludeDirectories)
        {
            config.WatchOptions.ExcludeDirectories = watchExcludeDirectories;
        }
        if (parseResults.GetValue<string[]>(CliStrings.ExcludeFilesFlag) is { Length: > 0 } watchExcludeFiles)
        {
            config.WatchOptions.ExcludeFiles = watchExcludeFiles;
        }
        if (parseResults.GetValue<bool>(CliStrings.PreserveWatchOutputFlag) is true)
        {
            config.WatchOptions.PreserveWatchOutput = true;
        }
#endif
        if (parseResults.GetValue<int[]>(CliStrings.NoWarnFlag) is { Length: > 0 } noWarn)
        {
            config.SupressedWarningCodes = noWarn;
        }
        if (parseResults.GetValue<bool>(CliStrings.NoEmitFlag) is true)
        {
            config.NoEmit = true;
        }
        if (parseResults.GetValue<GeneratorConfig[]>(CliStrings.GeneratorFlag) is { Length: > 0 } generators)
        {
            config.Generators = generators;
        }
    }

    public static int ProcessId
    {
        get
        {
            if (_processId == null)
            {
                using var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                _processId = thisProcess.Id;
            }
            return _processId.Value;
        }
    }
    private static int? _processId;
}
