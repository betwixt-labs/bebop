using System.Text;
using Chord.Common.Extensions;
using Chord.Common.Wasm.Types;

namespace Chord.Common.Wasm;

/// <summary>
/// Parses WebAssembly (WASM) binary files and extracts custom sections.
/// </summary>
internal sealed class WasmParser : IDisposable
{
    private readonly BinaryReader _reader;

    /// <summary>
    /// Initializes a new instance of the WasmParser class.
    /// </summary>
    /// <param name="stream">The input stream containing the WASM binary data.</param>
    public WasmParser(Stream stream)
    {
        _reader = new BinaryReader(stream ?? throw new ArgumentNullException(nameof(stream)), Encoding.UTF8, true);
    }

    /// <summary>
    /// Parses and returns sections from the WASM file.
    /// </summary>
    /// <returns>An enumerable of sections in the WASM file.</returns>
    public IEnumerable<Section> ParseSections()
    {
        ValidateWasmHeader();
        while (_reader.BaseStream.Position != _reader.BaseStream.Length)
        {
            var sectionId = (SectionId)_reader.ReadVarInt32();
            if (!sectionId.IsValid())
            {
                throw new InvalidDataException($"Invalid section id: {sectionId}");
            }

            int sectionSize = _reader.ReadVarInt32();
            long sectionStart = _reader.BaseStream.Position;

            if (sectionId is SectionId.Custom)
            {
                string name = ReadString();
                byte[] content = _reader.ReadBytes((int)(sectionSize - (_reader.BaseStream.Position - sectionStart)));
                yield return new CustomSection(name, content);
            }
            else if (sectionId is SectionId.Import)
            {
                var importCount = _reader.ReadVarUInt32();
                var imports = new Import[importCount];
                for (int i = 0; i < importCount; i++)
                {
                    var module = ReadString();
                    var field = ReadString();
                    var kind = (ImportKind)_reader.ReadVarUInt32();
                    var index = _reader.ReadVarUInt32();
                    imports[i] = new Import(module, field, kind, index);
                }
                yield return new ImportSection(imports);
            }
            else if (sectionId is SectionId.Export)
            {
                var exportCount = _reader.ReadVarUInt32();
                var exports = new Export[exportCount];
                for (int i = 0; i < exportCount; i++)
                {
                    var field = ReadString();
                    var kind = (ExportKind)_reader.ReadVarUInt32();
                    var index = _reader.ReadVarUInt32();

                    exports[i] = new Export(field, kind, index);
                }
                yield return new ExportSection(exports);
            }
            else if (sectionId is SectionId.Type)
            {
                var typeCount = _reader.ReadVarUInt32();
                var types = new FunctionType[typeCount];
                for (int i = 0; i < typeCount; i++)
                {
                    var form = (FunctionKind)_reader.ReadVarInt7();
                    if (form != FunctionKind.Function)
                    {
                        throw new InvalidDataException($"Invalid function type form: {form} vs {0x60}");
                    }
                    var paramCount = checked((int)_reader.ReadVarUInt32());
                    var parameters = new WebAssemblyValueType[paramCount];

                    for (int j = 0; j < paramCount; j++)
                    {
                        var paramType = (WebAssemblyValueType)_reader.ReadVarInt7();
                        parameters[j] = paramType;
                    }
                    var returnCount = checked((int)_reader.ReadVarUInt32());
                    var returns = new WebAssemblyValueType[returnCount];
                    for (int j = 0; j < returnCount; j++)
                    {
                        var returnType = (WebAssemblyValueType)_reader.ReadVarInt7();
                        returns[j] = returnType;
                    }
                    types[i] = new FunctionType(form, parameters, returns);
                }
                yield return new TypeSection(types);
            }
            else if (sectionId is SectionId.Function)
            {
                var count = _reader.ReadVarUInt32();
                var TypeIndices = new uint[count];
                for (int i = 0; i < count; i++)
                {
                    TypeIndices[i] = _reader.ReadVarUInt32();
                }
                yield return new FunctionSection(TypeIndices);
            }
            else
            {
                _reader.BaseStream.Seek(sectionSize, SeekOrigin.Current); // Skip unknown sections
            }
        }
    }

    private void ValidateWasmHeader()
    {
        int magicNumber = _reader.ReadInt32();
        if (magicNumber != 0x6D736100) // Equivalent to \0asm
        {
            throw new InvalidDataException("Invalid WASM file: Incorrect magic number.");
        }

        int version = _reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported WASM version: {version}");
        }
    }

    private string ReadString()
    {
        int length = _reader.ReadVarInt32();
        return Encoding.UTF8.GetString(_reader.ReadBytes(length));
    }

    /// <summary>
    /// Disposes the underlying BinaryReader and associated resources.
    /// </summary>
    public void Dispose()
    {
        _reader?.Dispose();
    }
}
