using System.Text;
using Chord.Common.Wasm;
using Wasmtime;

namespace Chord.Runtime.Internal.Strings;

internal sealed class TinyGoMarshaler : StringMarshaler
{
    private Memory? _memory;
    private Func<int, int>? _malloc;
    private Action<int>? _free;

    public TinyGoMarshaler() : base(WasmCompiler.TinyGo) { }

    public override void Bind(Instance wasmInstance)
    {
        _wasmInstance = wasmInstance;
        _memory = wasmInstance.GetMemory("memory") ?? throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        _malloc = wasmInstance.GetFunction<int, int>("malloc") ?? throw new ExtensionRuntimeException("Could not find malloc function");
        _free = wasmInstance.GetAction<int>("free") ?? throw new ExtensionRuntimeException("Could not find free function");
    }


    public override WasmString CreateString(string value, Encoding? encoding)
    {
        if (_malloc is null)
        {
            throw new ExtensionRuntimeException("Could not find malloc function. Was the marshaler bound?");
        }
        if (_memory is null)
        {
            throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        }
        encoding ??= Encoding.UTF8;
        int stringLength = encoding.GetByteCount(value);
        int stringAddress = _malloc.Invoke(stringLength);

        // Write the string bytes to the reserved memory.
        encoding.GetBytes(value, _memory.GetSpan(stringAddress, stringLength));

        return new WasmString(this, stringAddress, stringLength, value);
    }

    public override WasmString CreateString()
    {
        if (_malloc is null)
        {
            throw new ExtensionRuntimeException("Could not find malloc function. Was the marshaler bound?");
        }
        if (_memory is null)
        {
            throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        }
        // Allocate memory for the StringHeader
        int stringHeaderSize = sizeof(int) + sizeof(int); // Size of StringHeader (Data + Len)
        int stringHeaderAddress = _malloc.Invoke(stringHeaderSize);

        // Initially, the StringHeader's Data and Len fields are not set
        _memory.WriteInt32(stringHeaderAddress, 0); // Data pointer, initially 0
        _memory.WriteInt32(stringHeaderAddress + sizeof(int), 0); // Len, initially 0
        return new WasmString(this, stringHeaderAddress, 0);
    }

    /// <summary>
    /// Frees the memory occupied by a string in WebAssembly memory.
    /// </summary>
    /// <remarks>
    /// This method handles the memory layout specific to Go's StringSlice. It first reads the address of the string data 
    /// from the StringHeader and then frees the string data if it was allocated. Finally, it frees the StringHeader itself.
    /// </remarks>
    /// <param name="value">The WasmString object representing the string to be freed.</param>
    /// <exception cref="ExtensionRuntimeException">Thrown if the marshaler is not properly bound or if memory deallocation functions are not found.</exception>
    public override void FreeString(WasmString value)
    {
        if (_free is null)
        {
            throw new ExtensionRuntimeException("Could not find free function. Was the marshaler bound?");
        }
        if (_memory is null)
        {
            throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        }
        // Read the address of the string data from the StringHeader
        int stringDataAddress = _memory.ReadInt32(value.Address);

        // Free the string data if it was allocated
        if (stringDataAddress != 0)
        {
            // doesn't work at all, probably an incorrect assumption of how
            // Go strings are laid out in memory
            // try { _free.Invoke(stringDataAddress); } catch { }
        }
        // Free the StringHeader
        _free.Invoke(value.Address);
    }

    /// <summary>
    /// Reads a string from the specified address and size in WebAssembly memory.
    /// </summary>
    /// <remarks>
    /// This method directly reads the string data from the given memory address, assuming the string is stored 
    /// in a contiguous block of memory of the specified size. This is aligned with Go's memory layout for strings.
    /// </remarks>
    /// <param name="address">The memory address where the string data starts.</param>
    /// <param name="size">The size (in bytes) of the string data.</param>
    /// <param name="encoding">The encoding used for the string data. Defaults to UTF-8 if null.</param>
    /// <returns>The string read from memory.</returns>
    /// <exception cref="ExtensionRuntimeException">Thrown if the marshaler is not properly bound.</exception>
    public override string ReadString(int address, int size, Encoding? encoding = null)
    {
        if (_memory is null)
        {
            throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        }
        encoding ??= Encoding.UTF8;

        return encoding.GetString(_memory.GetSpan(address, size));
    }

    /// <summary>
    /// Reads a string from WebAssembly memory using a StringHeader at the specified address.
    /// </summary>
    /// <remarks>
    /// In Go's `StringSlice` layout, a StringHeader is used, which consists of a pointer to the string data and the length of the string.
    /// This method reads the StringHeader from the given address to obtain the memory address and length of the string,
    /// and then reads the string data from that memory address.
    /// It's important to handle this correctly to avoid reading incorrect memory regions, especially in the context of a managed
    /// environment like WebAssembly where memory access patterns are critical for system stability and security.
    /// </remarks>
    /// <param name="address">The memory address where the StringHeader is located.</param>
    /// <param name="encoding">The encoding used for the string data. Defaults to UTF-8 if null.</param>
    /// <returns>The string read from memory.</returns>
    public override string ReadString(int address, Encoding? encoding = null)
    {
        if (_memory is null)
        {
            throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        }
        // Use UTF-8 encoding by default if none is provided
        encoding ??= Encoding.UTF8;

        // Read the StringHeader from the given address
        // The StringHeader consists of two parts:
        // 1. The address of the string data (pointer)
        // 2. The length of the string
        var stringDataAddress = _memory.ReadInt32(address); // Read the data address
        var length = _memory.ReadInt32(address + sizeof(int)); // Read the length of the string

        // If the string data address is 0, return an empty string
        // This is to handle cases where the string might not be initialized
        if (stringDataAddress == 0 || length == 0)
        {
            return string.Empty;
        }

        // Read and return the string from the memory
        // The string is encoded in the specified encoding (UTF-8 by default)
        return encoding.GetString(_memory.GetSpan(stringDataAddress, length));
    }

    /// <summary>
    /// Writes a string to a specified address in WebAssembly memory.
    /// </summary>
    /// <remarks>
    /// In Go's memory layout for strings, the StringHeader consists of a pointer to the string data and its length.
    /// This method writes the string data to a newly allocated memory block and updates the StringHeader accordingly.
    /// </remarks>
    /// <param name="address">The memory address to write the string data to.</param>
    /// <param name="value">The string to write into memory.</param>
    /// <param name="encoding">The encoding to use for the string. Defaults to UTF-8 if null.</param>
    /// <exception cref="ExtensionRuntimeException">Thrown if the marshaler is not properly bound or necessary memory functions are not found.</exception>
    public override void WriteString(int address, string value, Encoding? encoding = null)
    {
        if (_memory is null)
        {
            throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        }
        if (_malloc is null)
        {
            throw new ExtensionRuntimeException("Could not find malloc function. Was the marshaler bound?");
        }
        if (_free is null)
        {
            throw new ExtensionRuntimeException("Could not find free function. Was the marshaler bound?");
        }
        // Use UTF-8 encoding by default if none is provided
        encoding ??= Encoding.UTF8;

        // Calculate the byte count of the string in the specified encoding
        var encodedLength = encoding.GetByteCount(value);

        // Read the address of the existing string data from the StringHeader
        var stringDataAddress = _memory.ReadInt32(address);

        // If there is existing string data, free it before writing new data
        if (stringDataAddress != 0)
        {
            //    _free.Invoke(stringDataAddress);
        }

        // Allocate new memory for the string data
        stringDataAddress = _malloc.Invoke(encodedLength);
        _memory.WriteInt32(address, stringDataAddress); // Update the StringHeader with the new data address

        // Write the string data to the allocated memory
        encoding.GetBytes(value, _memory.GetSpan(stringDataAddress, encodedLength));

        // Update the length in the StringHeader
        _memory.WriteInt32(address + sizeof(int), encodedLength);
    }
}