// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Chord.Common;
using Chord.Common.Wasm;
using Chord.Common.Wasm.Types;
using Chord.Compiler;
using Chord.Compiler.Extensions;
using Chord.Compiler.Internal;
using Chord.Compiler.Internal.API;
using Chord.Compiler.Internal.Commands;
using Chord.Compiler.Internal.Utils;
using Chord.Compiler.Shells;
using Chord.Runtime;
using Errata;
using Spectre.Console;

string? manifestPath = null;
string? manifestContent = null;
ChordManifest? manifest = null;
string? authToken = Environment.GetEnvironmentVariable("CHORD_AUTH");

var parsed = CommandParser.GetCommand(args);
if (!parsed.HasValue || parsed.Value.Command is "help")
{
    Logger.Out.WriteLine(CommandParser.GetHelpText());
    return !parsed.HasValue ? 1 : 0;
}
var command = parsed.Value.Command;
var flags = parsed.Value.Flags;

try
{
    using var cts = new CancellationTokenSource();
    var runHandler = Run(cts);
    using var terminationHandler = new ProcessTerminationHandler(cts, runHandler, TimeSpan.FromSeconds(5));
    Task<int> firstCompletedTask = await Task.WhenAny(runHandler, terminationHandler.ProcessTerminationCompletionSource.Task);
    return await firstCompletedTask; // return the result or propagate the exception
}
catch (Exception ex)
{
    switch (ex)
    {
        case AggregateException agg:
            Logger.Error.WriteException(agg);
            return 1;
        default:
            Logger.Error.WriteException(ex);
            return 1;
    }
}

async Task<int> Run(CancellationTokenSource cts)
{
    switch (command)
    {
        case "install":
            return await InstallAsync(flags, cts.Token);
    }
    var (Manifest, ManifestPath, ManifestContent) = LoadManifest();
    manifest = Manifest;
    manifestPath = ManifestPath;
    manifestContent = ManifestContent;
    var workingDirectory = Path.GetDirectoryName(manifestPath);
    return command switch
    {
        "build" => await CommandShell.RunScriptAsync(manifest.Build, workingDirectory!, cts.Token),
        "test" => Test(workingDirectory!),
        "pack" => Pack(workingDirectory!),
        "publish" => await PublishAsync(workingDirectory!, cts.Token),
        _ => throw new NotImplementedException(command),
    };
}



async Task<int> InstallAsync(Dictionary<string, object> flags, CancellationToken token)
{
    if (!flags.TryGetValue("extensions", out var extensions))
    {
        Logger.Error.MarkupLine("[maroon]No extensions specified to install.[/]");
        return 1;
    }
    if (extensions is not List<string> extensionList || extensionList.Count == 0)
    {
        Logger.Error.MarkupLine("[maroon]Invalid or no extensions specified to install.[/]");
        return 1;
    }

    var forceDownload = false;
    if (flags.TryGetValue("force", out var forceFlag) && forceFlag is bool force)
    {
        forceDownload = force;
    }


    var distinctExtensions = extensionList.Select(e => e.Trim()).Distinct().ToList();

    using var client = new RegistryClient();
    return await AnsiConsole.Progress()
    .AutoClear(false)
            .StartAsync(async ctx =>
            {
                var progressTasks = new List<ProgressTask>();

                // Create a progress task for each extension
                foreach (var extension in distinctExtensions)
                {
                    var task = ctx.AddTask($"[green]Installing {extension}...[/]", maxValue: 100);
                    progressTasks.Add(task);
                }

                // Download each extension asynchronously
                var downloadTasks = distinctExtensions.Select((extension, index) =>
                {
                    var progressTask = progressTasks[index];
                    return client.DownloadChordAsync(extension, progressTask, forceDownload, token);
                }).ToList();

                // Wait for all downloads to complete
                var results = await Task.WhenAll(downloadTasks);

                if (results.Any(r => r is false))
                {
                    return 1;
                }
                return 0;
            });
}

int Test(string workingDirectory)
{
    return AnsiConsole.Status()
    .Start("Testing...", ctx =>
    {
        ctx.Spinner(Spinner.Known.Aesthetic);
        Logger.Out.MarkupLine(":magnifying_glass_tilted_right: [white]Testing extension binary[/]");
        var chordBinary = IOUtil.ResolvePath(workingDirectory, manifest.Bin);
        if (!File.Exists(chordBinary))
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Unable to find extension binary: {chordBinary}[/]");
            return 1;
        }
        using var module = WasmModule.FromFile(chordBinary);
        ctx.Status("Checking extension imports...");
        if (module.ImportSection is null || module.ImportSection is { Size: 0 })
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not import any functions[/]");
            return 1;
        }
        var wasiImports = module.ImportSection.Imports.Where(i => i.Module.Equals("wasi_snapshot_preview1"));
        if (!wasiImports.Any())
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not import any WASI functions[/]");
            return 1;
        }
        Logger.Out.MarkupLine("[green]WASI Imports:[/]");
        foreach (var import in wasiImports)
        {
            Logger.Out.MarkupLine($"[green]├──[/] [yellow]{import}[/]");
        }

        var compiler = manifest.Build.Compiler.ToCompilerString();
        var chordModule = $"chord_{compiler}";

        var chordImports = module.ImportSection.Imports.Where(i => i.Module.Equals(chordModule));
        if (!chordImports.Any())
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not import any Chord functions: {chordModule} [/]");
            return 1;
        }

        Logger.Out.MarkupLine("[green]Chord Imports:[/]");
        foreach (var import in chordImports)
        {
            Logger.Out.MarkupLine($"[green]├──[/] [yellow]{import}[/]");
            var functionType = module.GetImportedFunctionType(import);
            if (functionType is null)
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} imports {import.Field} but the function type is not found.[/]");
                return 1;
            }
            Logger.Out.MarkupLine($"[green]│   ├──[/] [yellow]signature:[/] [blue]{module.GetImportedFunctionType(import)}[/]");
            var expectedSignature = SignatureValidator.GetFunctionSignature(compiler, import.Field);
            if (expectedSignature is null)
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension imports {import.Field} but no expected function signature was found.[/]");
                return 1;
            }
            if (!SignatureValidator.ValidateSignature(functionType, expectedSignature))
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} imports {import.Field} with an invalid signature[/]");
                Logger.Error.MarkupLine($"[maroon]Found: {functionType}[/]");
                Logger.Error.MarkupLine($"[maroon]Expected: {expectedSignature}[/]");
                return 1;
            }
        }
        ctx.Status("Checking extension exports...");
        if (module.ExportSection is null || module.ExportSection is { Size: 0 })
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not export any functions[/]");
            return 1;
        }
        var hasStartOrInitialize = module.ExportSection.Exports.Any(e => (e.Name.Equals("_start") || e.Name.Equals("_initialize")) && e.Kind == ExportKind.Function);
        if (!hasStartOrInitialize)
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not export a _start or _initialize function[/]");
            return 1;
        }
        var chordExports = module.ExportSection.Exports.Where(e => e.Name.StartsWith("chord_"));
        if (!chordExports.Any())
        {
            Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not export any Chord functions[/]");
            return 1;
        }
        Logger.Out.MarkupLine("[green]Chord Exports:[/]");
        foreach (var export in chordExports)
        {
            Logger.Out.MarkupLine($"[green]├──[/] [yellow]{export}[/]");
            var functionType = module.GetExportedFunctionType(export);
            if (functionType is null)
            {
                Logger.Error.MarkupLine($"[maroon]Extension binary {chordBinary} exports {export.Name} but the function type is not found.[/]");
                return 1;
            }
            Logger.Out.MarkupLine($"[green]│   ├──[/] [yellow]signature:[/] [blue]{module.GetExportedFunctionType(export)}[/]");
            var expectedSignature = SignatureValidator.GetFunctionSignature(compiler, export.Name);
            if (expectedSignature is null)
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension exports {export.Name} but no expected function signature was found.[/]");
                return 1;
            }
            if (!SignatureValidator.ValidateSignature(functionType, expectedSignature))
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} exports {export.Name} with an invalid signature[/]");
                Logger.Error.MarkupLine($"[maroon]Found: {functionType}[/]");
                Logger.Error.MarkupLine($"[maroon]Expected: {expectedSignature}[/]");
                return 1;
            }

        }
        ctx.Status("Checking extension contributions...");
        // basic checks to ensure the extension binary is valid
        if (manifest.Contributions.Type is ContributionType.Generator)
        {
            // the only export a generator should have is chord_compile
            var chordCompile = chordExports.FirstOrDefault(e => e.Name.Equals("chord_compile"));
            if (chordCompile is null)
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} does not export a chord_compile function[/]");
                return 1;
            }
            if (chordCompile.Kind != ExportKind.Function)
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Extension binary {chordBinary} exports chord_compile as a {chordCompile.Kind} instead of a function[/]");
                return 1;
            }
            var functionType = module.GetExportedFunctionType(chordCompile);
            if (functionType is null)
            {
                Logger.Error.MarkupLine($"[maroon]Extension binary {chordBinary} exports chord_compile but the function type is not found.[/]");
                return 1;
            }
        }

        return 0;
    });
}

async Task<int> PublishAsync(string workingDirectory, CancellationToken cancellationToken = default)
{
    if (manifest.IsPrivate)
    {
        Logger.Error.MarkupLine($":stop_sign: [maroon]Cannot publish private extension {manifest.Name}[/]");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(authToken))
    {
        Logger.Error.MarkupLine($":stop_sign: [maroon]No authentication token found.[/] Please set the CHORD_AUTH environment variable to your authentication token.");
        return 1;
    }
    var buildResult = await CommandShell.RunScriptAsync(manifest.Build, workingDirectory, cancellationToken);
    if (buildResult != 0)
    {
        return buildResult;
    }
    var testResults = Test(workingDirectory);
    if (testResults != 0)
    {
        return testResults;
    }
    var packResults = Pack(workingDirectory);
    if (packResults != 0)
    {
        return packResults;
    }
    using var client = new RegistryClient(authToken);
    var archiveName = $"{manifest.Name}-{manifest.Version}.chord";
    var zipFilePath = Path.Combine(workingDirectory, archiveName);
    try
    {
        var publishResponse = await client.PublishAsync(zipFilePath, cancellationToken);
        if (publishResponse is null)
        {
            Logger.Error.MarkupLine($":stop_sign: [maroon]Error publishing extension:[/] no response from registry.");
            return 1;
        }
        if (publishResponse.Success)
        {
            Logger.Out.MarkupLine($":party_popper: [green]Extension published successfully![/]");
            return 0;
        }
        else
        {
            Logger.Error.MarkupLine($":stop_sign: [maroon]Error publishing extension:[/] {publishResponse.Message}");
            return 1;
        }
    }
    catch (Exception e)
    {
        Logger.Error.MarkupLine($":stop_sign: [maroon]Fatal error while publishing extension:[/] {e.Message}");
        Logger.Error.WriteException(e);
        return 1;
    }
}


int Pack(string workingDirectory)
{
    var testResult = Test(workingDirectory);
    if (testResult != 0)
    {
        return testResult;
    }
    var binaryName = $"{manifest.Name}-{manifest.Version}.wasm";
    var archiveName = $"{manifest.Name}-{manifest.Version}.chord";
    var zipFilePath = Path.Combine(workingDirectory, archiveName);

    Logger.Out.MarkupLine(":package: [white]{0}[/]", archiveName);

    var encoding = new UTF8Encoding(false);
    var chordBinary = IOUtil.ResolvePath(workingDirectory, manifest.Bin);
    using var module = WasmModule.FromFile(chordBinary);

    // Function to log file details in a tree-like structure
    void LogFileDetails(string path, long size, int level = 0)
    {
        var indent = new string(' ', level * 4);
        var sizeInKb = size / 1024f;
        Logger.Out.MarkupLine($"{indent}[green]├──[/] [blue]{Path.GetFileName(path)}[/] ([yellow]{sizeInKb:F2} KB[/])");
    }

    // Log the .wasm file being the main container
    //  Logger.Out.MarkupLine("[green]Main .wasm Container:[/]");
    LogFileDetails(chordBinary, new FileInfo(chordBinary).Length);

    long totalSize = 0L; // Keep track of the total size of packed files
    var encodedManifest = new UTF8Encoding(false).GetBytes(manifestContent);
    totalSize += encodedManifest.Length;
    module.AddCustomSection(new("chord_manifest", encodedManifest));
    LogFileDetails(manifestPath, encodedManifest.Length, 1);

    // Adding custom sections to .wasm file
    if (manifest.Pack is { Count: > 0 })
    {
        foreach (var (alias, pack) in manifest.Pack)
        {
            var fullPath = IOUtil.ResolvePath(workingDirectory, pack.AuxilaryFile);
            if (!File.Exists(fullPath))
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Unable to find auxiliary file {fullPath}[/]");
                return 1;
            }

            var content = File.ReadAllBytes(fullPath);
            var fileInfo = new FileInfo(fullPath);
            totalSize += fileInfo.Length;

            // Log each file being packed into the .wasm
            LogFileDetails(fullPath, fileInfo.Length, 1);

            var packedFile = new PackedFile(Path.GetFileName(fullPath), content, alias);
            module.AddCustomSection(new($"chord_{alias}", packedFile.ToArray()));
        }
    }

    // Serialize .wasm file to a MemoryStream to get its new size
    using var memoryStream = new MemoryStream();
    module.SerializeTo(memoryStream);
    memoryStream.Seek(0, SeekOrigin.Begin);

    if (File.Exists(zipFilePath))
    {
        File.Delete(zipFilePath);
    }
    if (flags.ContainsKey("nozip"))
    {
        using var s = File.Create(Path.Combine(workingDirectory, "chord.wasm"));
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.CopyTo(s);
        return 0;
    }

    using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
    {
        var zipEntry = zipArchive.CreateEntry(binaryName);
        using (var entryStream = zipEntry.Open())
        {
            memoryStream.CopyTo(entryStream);
        }

        zipArchive.CreateEntryFromFile(manifestPath, Helpers.ManifestFileName);

        if (manifest.ReadMe is not null)
        {
            var readmePath = IOUtil.ResolvePath(workingDirectory, manifest.ReadMe);
            if (!File.Exists(readmePath))
            {
                Logger.Error.MarkupLine($":cross_mark: [maroon]Unable to find README file {readmePath}[/]");
                return 1;
            }
            zipArchive.CreateEntryFromFile(readmePath, "README.md");
            LogFileDetails(readmePath, new FileInfo(readmePath).Length, 0);
        }
    }

    // Report the final size of the .wasm and the ZIP archive
    Logger.Out.MarkupLine($"[green]Final Extension Size:[/] [yellow]{memoryStream.Length / 1024f:F2} KB[/]");
    Logger.Out.MarkupLine($"[green]Extension Archive Created at:[/] [blue]{zipFilePath}[/]");
    Logger.Out.MarkupLine($"[green]Extension Archive Size:[/] [yellow]{new FileInfo(zipFilePath).Length / 1024f:F2} KB[/]");
    return 0;
}

(ChordManifest Manifest, string ManifestPath, string ManifestContent) LoadManifest()
{
    //preamble nonsense
    var manifestPath = Helpers.LocateChordManifest();
    if (string.IsNullOrWhiteSpace(manifestPath))
    {
        Logger.Error.MarkupLine("[maroon]No chord.json file found.[/]");
        return default;
    }
    if (Helpers.TryReadFile(manifestPath, out var manifestContent) is false)
    {
        Logger.Error.MarkupLine($"[maroon]Unable to read contents of chord.json file at {manifestPath}[/]");
        return default;
    }
    var source = new Source(manifestPath, manifestContent);
    var sourceRepo = new InMemorySourceRepository();
    sourceRepo.Register(source);

    var workingDirectory = Path.GetDirectoryName(manifestPath);
    if (workingDirectory is null)
    {
        Logger.Error.MarkupLine($"[maroon]Unable to determine working directory from {manifestPath}[/]");
        return default;
    }
    try
    {
        return (ChordManifest.FromJson(manifestContent), manifestPath, manifestContent);
    }
    catch (JsonException ex)
    {
        ex.Render(sourceRepo, manifestPath);
        return default;
    }
}