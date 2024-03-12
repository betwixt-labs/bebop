using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text;
using Chord.Common;
using Chord.Runtime.Internal.Callers;
using Chord.Runtime.Internal.Linkers;
using Wasmtime;

namespace Chord.Runtime;

/// <summary>
/// Represents a loaded extension within the Chord runtime.
/// </summary>
public sealed class Extension : IDisposable
{
    private readonly Module _module;
    private readonly WasmLinker _linker;
    private readonly Store _store;
    private readonly WasmCaller _caller;
    private readonly ChordManifest _manifest;
    private readonly List<PackedFile> _packedFiles;

    private readonly FileStream _standardInput;
    private readonly FileStream _standardOutput;
    private readonly FileStream _standardError;

    /// <summary>
    /// Initializes a new instance of the <see cref="Extension"/> class.
    /// </summary>
    /// <param name="module">The WebAssembly module.</param>
    /// <param name="linker">The linker for the WebAssembly module.</param>
    /// <param name="store">The store containing module data.</param>
    /// <param name="caller">The caller for the WebAssembly module.</param>
    /// <param name="manifest">The manifest of the extension.</param>
    /// <param name="packedFiles">A list of packed files associated with the extension.</param>
    internal Extension(Module module, WasmLinker linker, Store store, WasmCaller caller, ChordManifest manifest, List<PackedFile> packedFiles, FileStream standardInput, FileStream standardOutput, FileStream standardError)
    {
        _module = module;
        _linker = linker;
        _store = store;
        _caller = caller;
        _manifest = manifest;
        _packedFiles = packedFiles;
        _standardInput = standardInput;
        _standardOutput = standardOutput;
        _standardError = standardError;
        // update the store caller
        _store.SetData(this);
    }

    /// <summary>
    /// Gets the manifest of the extension.
    /// </summary>
    public ChordManifest Manifest => _manifest;

    /// <summary>
    /// Gets the packed files associated with the extension.
    /// </summary>
    public ReadOnlyCollection<PackedFile> PackedFiles => new(_packedFiles);

    /// <summary>
    /// Gets the name of the extension.
    /// </summary>
    public string Name => _manifest.Name;

    /// <summary>
    /// Gets the version of the extension.
    /// </summary>
    public string Version => _manifest.Version.ToString();

    /// <summary>
    /// Gets the description of the extension.
    /// </summary>
    public string Description => _manifest.Description;

    /// <summary>
    /// Gets the type of contribution made by the extension.
    /// </summary>
    public ContributionType Type => _manifest.Contributions.Type;

    /// <summary>
    /// Gets the decorators defined in the extension.
    /// </summary>
    public ReadOnlyCollection<ChordDecorator> Decorators
    {
        get
        {
            if (_manifest.Contributions.Decorators is null)
            {
                return new([]);
            }
            return new(_manifest.Contributions.Decorators);
        }
    }

    public async ValueTask WriteStandardInput(string input, CancellationToken cancellationToken, Encoding? encoding = default)
    {
        encoding ??= Encoding.UTF8;
        if (_standardInput.CanWrite)
        {
            using var streamWriter = new StreamWriter(_standardInput, encoding, leaveOpen: true);
            var memory = new ReadOnlyMemory<char>(input.ToCharArray());
            await streamWriter.WriteAsync(memory, cancellationToken: cancellationToken);
            await streamWriter.FlushAsync();
        }
    }

    public async ValueTask ClearStandardInput(CancellationToken cancellationToken)
    {
        if (_standardInput.CanWrite)
        {
            await _standardInput.FlushAsync(cancellationToken);
            // truncate
            _standardInput.SetLength(0);
        }
    }

    public async ValueTask<string?> ReadStandardOutput(CancellationToken cancellationToken, Encoding? encoding = default)
    {
        encoding ??= Encoding.UTF8;
        if (_standardOutput.CanRead)
        {
            using var streamReader = new StreamReader(_standardOutput, encoding, leaveOpen: true);
            return await streamReader.ReadToEndAsync(cancellationToken);
        }
        return null;
    }

    public async ValueTask ClearStandardOutput(CancellationToken cancellationToken)
    {
        if (_standardOutput.CanWrite)
        {
            await _standardOutput.FlushAsync(cancellationToken);
            // truncate
            _standardOutput.SetLength(0);
        }
    }

    public async ValueTask<string?> ReadStandardError(CancellationToken cancellationToken, Encoding? encoding = default)
    {
        encoding ??= Encoding.UTF8;
        if (_standardError.CanRead)
        {
            using var streamReader = new StreamReader(_standardError, encoding, leaveOpen: true);
            return await streamReader.ReadToEndAsync(cancellationToken);
        }
        return null;
    }

    public async ValueTask ClearStandardError(CancellationToken cancellationToken)
    {
        if (_standardError.CanWrite)
        {
            await _standardError.FlushAsync(cancellationToken);
            // truncate
            _standardError.SetLength(0);
        }
    }

    /// <summary>
    /// Gets a simplified view of the contributions made by the extension, excluding decorators.
    /// </summary>
    public ChordContribution Contributions => _manifest.Contributions with { Decorators = null };

    /// <summary>
    /// Compiles a given context using the extension, if it is a generator type.
    /// </summary>
    /// <param name="context">The context to compile.</param>
    /// <returns>The result of the compilation.</returns>
    /// <exception cref="ExtensionRuntimeException">Thrown if the extension is not a generator type.</exception>
    public async ValueTask<string> ChordCompileAsync(string context, CancellationToken cancellationToken = default)
    {
        if (Type is not ContributionType.Generator)
        {
            throw new ExtensionRuntimeException("Attempted to call chord_compile on a non-generator extension.");
        }
        return await _caller.ChordCompileAsync(context, cancellationToken);
    }

    public void Dispose()
    {
        _module.Dispose();
        _linker.Dispose();
        _store.Dispose();
        _standardInput.Dispose();
        _standardOutput.Dispose();
        _standardError.Dispose();
        _packedFiles.Clear();
    }
}