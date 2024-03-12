namespace Chord.Runtime.Internal.Strings;

/// <summary>
/// Represents a string stored in WebAssembly memory, managed by a specific StringMarshaler.
/// </summary>
internal sealed class WasmString : IDisposable
{
    private readonly StringMarshaler _marshaler;
    private readonly int _address;
    private readonly string? _value;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the WasmString class.
    /// </summary>
    /// <param name="marshaler">The StringMarshaler used to read and free the string from memory.</param>
    /// <param name="address">The address of the string in WebAssembly memory.</param>
    /// <param name="length">The length of the string in bytes.</param>
    /// <param name="value">The optional string value. If not provided, it will be lazy-loaded from memory.</param>
    internal WasmString(StringMarshaler marshaler, int address, int length, string? value = null)
    {
        _marshaler = marshaler;
        _address = address;
        _value = value;
        _length = length;
    }

    /// <summary>
    /// Gets the memory address of the string in WebAssembly memory.
    /// </summary>
    public int Address => _address;

    /// <summary>
    /// Gets the length of the string in bytes.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets the string value. If the value is not already set, it reads the string from WebAssembly memory.
    /// </summary>
    public string Value => _value is null ? _marshaler.ReadString(_address) : _value;

    /// <summary>
    /// Releases the resources used by the WasmString, specifically freeing the associated memory in WebAssembly.
    /// </summary>
    public void Dispose()
    {
        _marshaler.FreeString(this);
    }
}