using System.Text.Json;
using Chord.Common.Wasm.Types;

namespace Chord.Common.Wasm;

/// <summary>
/// Represents a WebAssembly (WASM) module, providing functionality to manipulate custom sections.
/// </summary>
public sealed class WasmModule : IDisposable
{
    private readonly Stream _stream;
    private readonly List<CustomSection> _customSections;
    private readonly ImportSection? _importSection;
    private readonly ExportSection? _exportSection;

    private readonly TypeSection? _typeSection;
    private readonly FunctionSection? _functionSection;

    private WasmModule(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        using var parser = new WasmParser(stream);
        _customSections = [];
        foreach (var section in parser.ParseSections())
        {
            if (section is CustomSection customSection)
            {
                _customSections.Add(customSection);
            }
            else if (section is ImportSection importSection)
            {
                _importSection = importSection;
            }
            else if (section is ExportSection exportSection)
            {
                _exportSection = exportSection;
            }
            else if (section is TypeSection typeSection)
            {
                _typeSection = typeSection;
            }
            else if (section is FunctionSection functionSection)
            {
                _functionSection = functionSection;
            }
        }
        _stream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Gets a read-only list of custom sections in the WASM module.
    /// </summary>
    public IReadOnlyList<CustomSection> CustomSections => _customSections;

    /// <summary>
    /// Gets the import section of the WASM module, if any.
    /// </summary>
    public ImportSection? ImportSection => _importSection;

    /// <summary>
    /// Gets the export section of the WASM module, if any.
    /// </summary>
    public ExportSection? ExportSection => _exportSection;

    /// <summary>
    /// Gets the type section of the WASM module, if any.
    /// </summary>
    public TypeSection? TypeSection => _typeSection;

    /// <summary>
    /// Gets the function section of the WASM module, if any.
    /// </summary>
    public FunctionSection? FunctionSection => _functionSection;

    /// <summary>
    /// Adds a custom section to the module.
    /// </summary>
    /// <param name="section">The custom section to add.</param>
    public void AddCustomSection(CustomSection section)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));

        _customSections.Add(section);
    }

    /// <summary>
    /// Creates a WasmModule from a file path.
    /// </summary>
    /// <param name="filePath">Path to the WASM file.</param>
    /// <returns>A new WasmModule instance.</returns>
    public static WasmModule FromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        return new WasmModule(new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite));
    }

    /// <summary>
    /// Creates a WasmModule from a stream.
    /// </summary>
    /// <param name="stream">Stream containing the WASM data.</param>
    /// <returns>A new WasmModule instance.</returns>
    public static WasmModule FromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        return new WasmModule(stream);
    }

    /// <summary>
    /// Serializes the current state of the WASM module back to a byte array.
    /// </summary>
    /// <returns>The serialized WASM module as a byte array.</returns>
    public byte[] Serialize()
    {
        using var writer = new WasmWriter(this, _stream);
        writer.Write();
        return writer.GetBytes();
    }

    public void SerializeTo(Stream outputStream)
    {
        using var writer = new WasmWriter(this, _stream, outputStream);
        writer.Write();
        outputStream.Flush();
    }

    /// <summary>
    /// Disposes the underlying stream.
    /// </summary>
    public void Dispose()
    {
        _stream?.Dispose();
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonContext.Default.WasmModule);
    }

    public FunctionType? GetExportedFunctionType(Export export)
    {
        if (export.Kind != ExportKind.Function)
        {
            return null;
        }

        int functionIndex = (int)export.Index;
        int totalImports = ImportSection?.Imports.Count(func => func.Kind == ImportKind.Function) ?? 0;

        if (functionIndex < totalImports)
        {
            // Refers to an imported function
            int? typeIndex = (int?)ImportSection?.Imports[functionIndex].Index;
            return typeIndex.HasValue ? TypeSection?.Types[typeIndex.Value] : null;
        }
        else
        {
            // Refers to a locally defined function
            int localIndex = functionIndex - totalImports;
            return localIndex >= 0 && localIndex < FunctionSection?.TypeIndices.Length
                   ? TypeSection?.Types[(int)FunctionSection.TypeIndices[localIndex]]
                   : null;
        }
    }

    public FunctionType? GetImportedFunctionType(Import import)
    {
        if (import.Kind != ImportKind.Function)
        {
            return null;
        }

        // Directly use the Index from the import entry for a function
        int? typeIndex = (int?)import.Index;

        if (!typeIndex.HasValue || TypeSection == null)
        {
            return null;
        }

        // Ensure the typeIndex is within the bounds of the TypeSection.Types array
        if (typeIndex.Value >= 0 && typeIndex.Value < TypeSection.Types.Length)
        {
            return TypeSection.Types[typeIndex.Value];
        }

        return null;
    }
}
