using System.Text;
using Chord.Common.Wasm;
using Wasmtime;

namespace Chord.Runtime.Internal.Strings;

internal sealed class AssemblyScriptMarshaler : StringMarshaler
{
    private Memory? _memory;
    private Func<int, int, int>? _new;
    private Func<int, int>? _pin;
    private Action<int>? _unpin;
    private Action? _collect;

    public AssemblyScriptMarshaler() : base(WasmCompiler.AssemblyScript)
    {

    }

    public override void Bind(Instance wasmInstance)
    {
        _wasmInstance = wasmInstance;
        _memory = wasmInstance.GetMemory("memory") ?? throw new ExtensionRuntimeException("Could not find the 'memory' export. Was the marshaler bound?");
        _new = wasmInstance.GetFunction<int, int, int>("__new") ?? throw new ExtensionRuntimeException("Could not find __new function");
        _pin = wasmInstance.GetFunction<int, int>("__pin") ?? throw new ExtensionRuntimeException("Could not find __pin function");
        _unpin = wasmInstance.GetAction<int>("__unpin") ?? throw new ExtensionRuntimeException("Could not find __unpin function");
        _collect = wasmInstance.GetAction("__collect") ?? throw new ExtensionRuntimeException("Could not find __collect function");
    }

    /// <summary>
    /// Creates a string in the WebAssembly memory context and returns a WasmString representing it.
    /// </summary>
    /// <param name="value">The string value to be stored in WebAssembly memory.</param>
    /// <param name="encoding">The encoding to use. If null, Unicode (UTF-16) encoding will be used.</param>
    /// <returns>A WasmString instance representing the newly created string in WebAssembly memory.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided string value is null or whitespace.</exception>
    /// <exception cref="ExtensionRuntimeException">Thrown if the marshaler has not been properly bound with a WebAssembly instance.</exception>
    public override WasmString CreateString(string value, Encoding? encoding = null)
    {
        //https://www.assemblyscript.org/runtime.html#memory-layout
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (_new is null || _pin is null || _unpin is null || _collect is null || _memory is null)
            throw new ExtensionRuntimeException("Marshaler not bound");

        encoding ??= Encoding.Unicode;
        const int OBJECT_ID_STRING = 2;
        var stringLength = encoding.GetByteCount(value);
        var stringAddress = _new.Invoke(stringLength, OBJECT_ID_STRING);
        var pinned = _pin.Invoke(stringAddress);
        encoding.GetBytes(value, _memory.GetSpan(pinned, stringLength));
        _memory.WriteInt32(pinned - 4, stringLength);
        return new WasmString(this, pinned, stringLength, value);
    }

    public override WasmString CreateString()
    {
        throw new NotImplementedException();
    }

    public override void FreeString(WasmString value)
    {
        if (_unpin is null || _collect is null)
            throw new ExtensionRuntimeException("Marshaler not bound");
        _unpin.Invoke(value.Address);
        _collect.Invoke();
    }

    /// <summary>
    /// Not implemented. AssemblyScript strings never provide a length when marshaled.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="size"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override string ReadString(int address, int size, Encoding? encoding = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads a string from the specified address in WebAssembly memory using the AssemblyScript string layout.
    /// </summary>
    /// <remarks>
    /// In the AssemblyScript memory layout for strings, the string length is stored at an offset of -4 bytes 
    /// from the start address of the string. This method first reads the length of the string from this offset 
    /// and then retrieves the string data starting from the provided address.
    /// 
    /// It's important to note that AssemblyScript and other WebAssembly languages may have different 
    /// memory layouts for strings, so the correct handling of these layouts is crucial for accurately 
    /// reading string data from memory.
    /// </remarks>
    /// <param name="address">The memory address where the string starts in WebAssembly memory.</param>
    /// <param name="encoding">The encoding used for the string data. Defaults to Unicode (UTF-16) if null.</param>
    /// <returns>The string read from memory.</returns>
    /// <exception cref="ExtensionRuntimeException">Thrown if the marshaler is not properly bound to a WebAssembly instance.</exception>
    public override string ReadString(int address, Encoding? encoding = null)
    {
        if (_memory is null)
            throw new ExtensionRuntimeException("Marshaler not bound");
        encoding ??= Encoding.Unicode;
        // The byte length of the string is at offset -4 in AssemblyScript string layout.
        var length = _memory.ReadInt32(address - 4);
        return encoding.GetString(_memory.GetSpan(address, length));
    }

    public override void WriteString(int address, string value, Encoding? encoding = null)
    {
        throw new NotImplementedException();
    }
}