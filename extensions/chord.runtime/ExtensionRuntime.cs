using System.Collections.ObjectModel;
using Chord.Common;
using Chord.Common.Wasm;
using Chord.Runtime.Internal.Callers;
using Chord.Runtime.Internal.Linkers;
using Chord.Runtime.Internal.Strings;
using Semver;
using Spectre.Console;
using Wasmtime;

namespace Chord.Runtime
{
    /// <summary>
    /// Represents a runtime for managing and executing Chord/WASM extensions.
    /// </summary>
    public sealed class ExtensionRuntime : IDisposable
    {
        private static readonly string[] _initMethods = ["_initialize", "_start"];
        private readonly Engine _engine;
        private readonly List<Extension> _extensions;
        private readonly SemVersion _engineVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionRuntime"/> class.
        /// </summary>
        /// <param name="engineVersion">The version of the engine (bebopc) that is hosting the runtime.</param>
        /// <param name="standardOut">Standard output console.</param>
        /// <param name="standardError">Standard error console.</param>
        public ExtensionRuntime(string engineVersion, IAnsiConsole standardOut, IAnsiConsole standardError)
        {
            _engine = new Engine(new Config()
                .WithReferenceTypes(false)
                .WithDebugInfo(true)
                .WithCraneliftDebugVerifier(true)
                .WithCompilerStrategy(CompilerStrategy.Auto)
                .WithBulkMemory(true)
                .WithMultiMemory(true)
                .WithSIMD(true)
                .WithWasmThreads(true)
                .WithOptimizationLevel(OptimizationLevel.None)
                .WithEpochInterruption(false)

            );
            _extensions = [];
            _engineVersion = SemVersion.Parse(engineVersion, SemVersionStyles.Strict);
            StandardOut = standardOut;
            StandardError = standardError;
        }

        /// <summary>
        /// Loads and initializes an extension by its name and version.
        /// </summary>
        /// <remarks>
        /// The extension must be installed in the default extension storage path.
        /// </remarks>
        /// <param name="extensionName">The name of the extension.</param>
        /// <param name="extensionVersion">The version of the extension.</param>
        /// <returns>The loaded extension.</returns>
        /// <exception cref="ExtensionException">Thrown when the extension cannot be loaded or initialized.</exception>
        /// <exception cref="ExtensionRuntimeException">Thrown when wasmtime fails.</exception>
        public Extension LoadExtension(string extensionName, string extensionVersion)
        {
            var modulePath = Path.Combine(StoragePath.BebopcData, extensionName, extensionVersion, "chord.wasm");
            if (!File.Exists(modulePath))
            {
                throw new ExtensionException($"Could not find extension {extensionName}@{extensionVersion} - is it installed?");
            }
            try
            {
                return LoadExtension(modulePath);
            }
            catch (WasmtimeException e)
            {
                throw new ExtensionException($"Could not load extension {extensionName}@{extensionVersion}", e);
            }
        }


        /// <summary>
        /// Loads and initializes an extension by its path.
        /// </summary>
        /// <param name="modulePath">The path to the extension.</param>
        /// <returns>The loaded extension.</returns>
        /// <exception cref="ExtensionException">Thrown when the extension cannot be loaded or initialized.</exception>
        /// <exception cref="ExtensionRuntimeException">Thrown when wasmtime fails.</exception>
        public Extension LoadExtension(string modulePath)
        {
            try
            {
                using var chordModule = WasmModule.FromFile(modulePath);
                var manifestSection = chordModule.CustomSections.FirstOrDefault(x => x.Name == "chord_manifest")
                    ?? throw new ExtensionException("Could not find 'chord_manifest' section in extension");

                var manifest = ChordManifest.FromBytes(manifestSection.Content);

                if (!_engineVersion.Satisfies(manifest.Engine.Bebopc))
                {
                    throw new ExtensionException($"Extension requires bebopc version '{manifest.Engine.Bebopc}' but runtime is '{_engineVersion}'");
                }

                var packedFiles = GetPackedFiles(manifest, chordModule);

                StringMarshaler marshaler = CreateMarshaler(manifest.Build.Compiler);

                var store = new Store(_engine);

                var standardOut = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
                var standardIn = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
                var standardErr = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.DeleteOnClose);


                store.SetWasiConfiguration(new WasiConfiguration().WithStandardInput(standardIn.Name).WithStandardOutput(standardOut.Name).WithStandardError(standardErr.Name));

                WasmLinker linker = CreateLinker(manifest.Build.Compiler, store, marshaler, manifest.Contributions.Type);
                linker.DefineKernel();
                var module = Module.FromFile(_engine, modulePath);

                var wasmInstance = InitializeWasmInstance(linker, store, module, marshaler, manifest.Build.Compiler);

                var wasmCaller = CreateCaller(manifest.Build.Compiler, wasmInstance, marshaler);
                var extension = new Extension(module, linker, store, wasmCaller, manifest, packedFiles, standardIn, standardOut, standardErr);
                if (manifest.Build.Compiler is WasmCompiler.Javy)
                {
                    wasmCaller.SetExtension(extension);
                }
                _extensions.Add(extension);
                return extension;
            }
            catch (WasmtimeException e)
            {
                throw new ExtensionRuntimeException($"Could not load extension @ {modulePath}", e);
            }
        }

        private static Instance InitializeWasmInstance(WasmLinker linker, Store store, Module module, StringMarshaler marshaler, WasmCompiler compiler)
        {
            var wasmInstance = linker.Instantiate(store, module);
            marshaler.Bind(wasmInstance);
            // if the compiler is javy, calling _start breaks other methods
            if (compiler is WasmCompiler.Javy)
            {
                return wasmInstance;
            }
            bool methodInvoked = false;
            foreach (var methodName in _initMethods)
            {
                var method = wasmInstance.GetFunction(methodName);
                if (method != null)
                {
                    method.Invoke();
                    methodInvoked = true;
                    break;
                }
            }

            if (!methodInvoked)
            {
                throw new ExtensionException("No _initialize or _start method found");
            }
            return wasmInstance;
        }

        private static List<PackedFile> GetPackedFiles(ChordManifest manifest, WasmModule chordModule)
        {
            var packedFiles = new List<PackedFile>();
            if (manifest.Pack != null)
            {
                foreach (var (alias, pack) in manifest.Pack)
                {
                    var packedFileSection = chordModule.CustomSections.FirstOrDefault(x => x.Name == $"chord_{alias}")
                        ?? throw new ExtensionException($"Could not find expected packed file '{alias}' in extension");
                    packedFiles.Add(PackedFile.FromBytes(packedFileSection.Content));
                }
            }

            return packedFiles;
        }

        private static StringMarshaler CreateMarshaler(WasmCompiler compiler)
        {
            return compiler switch
            {
                WasmCompiler.TinyGo => new TinyGoMarshaler(),
                WasmCompiler.AssemblyScript => new AssemblyScriptMarshaler(),
                WasmCompiler.Javy => new JavyMarshaler(),
                _ => throw new ExtensionException("Unable to determine marshaler.")
            };
        }

        private WasmLinker CreateLinker(WasmCompiler compiler, Store store, StringMarshaler marshaler, ContributionType contributionType)
        {
            return compiler switch
            {
                WasmCompiler.TinyGo => new TinyGoLinker(_engine, store, marshaler, this, contributionType),
                WasmCompiler.AssemblyScript => new AssemblyScriptLinker(_engine, store, marshaler, this, contributionType),
                WasmCompiler.Javy => new JavyLinker(_engine, store, marshaler, this, contributionType),
                _ => throw new ExtensionException("Unable to determine linker.")
            };
        }

        private WasmCaller CreateCaller(WasmCompiler compiler, Instance wasmInstance, StringMarshaler marshaler)
        {
            return compiler switch
            {
                WasmCompiler.TinyGo => new TinyGoCaller(wasmInstance, marshaler, this),
                WasmCompiler.AssemblyScript => new AssemblyScriptCaller(wasmInstance, marshaler, this),
                WasmCompiler.Javy => new JavyCaller(wasmInstance, marshaler, this),
                _ => throw new ExtensionException("Unable to determine caller.")
            };
        }

        /// <summary>
        /// Gets the version of the engine (bebopc) hosting this runtime.
        /// </summary>
        public string EngineVersion => _engineVersion.ToString();
        /// <summary>
        /// Gets the standard output console.
        /// </summary>
        public IAnsiConsole StandardOut { get; }
        /// <summary>
        /// Gets the standard error console.
        /// </summary>
        public IAnsiConsole StandardError { get; }
        /// <summary>
        /// Gets the loaded extensions.
        /// </summary>
        public ReadOnlyCollection<Extension> Extensions => _extensions.AsReadOnly();

        /// <summary>
        /// Disposes of the runtime and all loaded extensions.
        /// </summary>
        public void Dispose()
        {
            _engine.Dispose();
            foreach (var extension in _extensions)
            {
                extension.Dispose();
            }
            _extensions.Clear();
        }
    }
}
