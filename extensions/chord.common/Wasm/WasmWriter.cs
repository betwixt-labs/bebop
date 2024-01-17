using System.Text;
using Chord.Common.Extensions;
using Chord.Common.Wasm.Types;

namespace Chord.Common.Wasm;

/// <summary>
/// Writes WebAssembly (WASM) module data, allowing for the modification and addition of custom sections.
/// </summary>
internal sealed class WasmWriter : IDisposable
{
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;
    private readonly BinaryWriter _writer;
    private readonly WasmModule _module;

    /// <summary>
    /// Initializes a new instance of the WasmWriter class.
    /// </summary>
    /// <param name="module">The WASM module to be written.</param>
    /// <param name="inputStream">The input stream containing the original WASM data.</param>
    public WasmWriter(WasmModule module, Stream inputStream, Stream? outputStream = null)
    {
        _module = module ?? throw new ArgumentNullException(nameof(module));
        _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
        _inputStream.Seek(0, SeekOrigin.Begin);

        _outputStream = outputStream ?? new MemoryStream();
        _writer = new BinaryWriter(_outputStream, Encoding.UTF8, true);

        // Copy the first 8 bytes (magic number and version) from the input stream
        byte[] header = new byte[8];
        inputStream.Read(header, 0, 8);
        _writer.Write(header);
    }

    /// <summary>
    /// Processes sections from the input stream and writes them to the output stream, 
    /// including modified custom sections from the module.
    /// </summary>
    public void Write()
    {
        ProcessSections();
    }

    private void ProcessSections()
    {
        while (_inputStream.Position < _inputStream.Length)
        {
            SectionId sectionId = (SectionId)_inputStream.ReadByte();
            int sectionSize = _inputStream.ReadVarInt32();

            if (sectionId != SectionId.Custom) // Not a custom section
            {
                // Write section id and size
                _writer.Write((byte)sectionId);
                _writer.WriteVarInt32(sectionSize);

                byte[] sectionData = new byte[sectionSize];
                _inputStream.Read(sectionData, 0, sectionSize);
                _writer.Write(sectionData);
            }
            else
            {
                // Skip existing custom sections
                _inputStream.Seek(sectionSize, SeekOrigin.Current);
            }
        }

        // Append new custom sections from the module
        foreach (var customSection in _module.CustomSections)
        {
            WriteCustomSection(customSection);
        }
    }

    private void WriteCustomSection(CustomSection customSection)
    {
        _writer.Write((byte)SectionId.Custom); // Custom section id

        var nameLength = Encoding.UTF8.GetByteCount(customSection.Name);
        int sectionSize = CalculateVarInt32Size(nameLength) + nameLength + customSection.Content.Length;

        _writer.WriteVarInt32(sectionSize); // Section size
        WriteString(customSection.Name); // Section name
        _writer.Write(customSection.Content); // Section content
    }

    private void WriteString(string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        _writer.WriteVarInt32(bytes.Length);
        _writer.Write(bytes);
    }

    private static int CalculateVarInt32Size(int value)
    {
        int size = 0;
        do
        {
            value >>= 7;
            size++;
        } while (value != 0);
        return size;
    }

    /// <summary>
    /// Gets the serialized bytes from the output stream.
    /// </summary>
    /// <returns>The byte array containing the serialized WASM data.</returns>
    public byte[] GetBytes()
    {
        _writer.Flush();
        if (_outputStream is MemoryStream memoryStream)
        {
            // If the stream is already a MemoryStream, we can just get its buffer
            return memoryStream.ToArray();
        }
        else
        {
            // Otherwise, we read the stream into a MemoryStream first
            using var ms = new MemoryStream();
            _outputStream.CopyTo(ms);
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Disposes the underlying BinaryWriter and associated resources.
    /// </summary>
    public void Dispose()
    {
        _writer?.Dispose();
    }
}
