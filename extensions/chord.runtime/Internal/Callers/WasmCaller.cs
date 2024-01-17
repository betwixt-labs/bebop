using Chord.Common;
using Chord.Runtime.Internal.Strings;
using Wasmtime;

namespace Chord.Runtime.Internal.Callers;

/// <summary>
/// Represents an abstract base class for handling calls to WebAssembly modules within the Chord runtime.
/// This class provides common functionalities and serves as a base for specific WebAssembly calling implementations.
/// </summary>
internal abstract class WasmCaller
{
    protected readonly StringMarshaler _stringMarshaler;
    protected readonly Instance _instance;
    protected readonly ExtensionRuntime _runtime;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmCaller"/> class.
    /// </summary>
    /// <param name="wasmInstance">The WebAssembly instance associated with this caller.</param>
    /// <param name="stringMarshaler">The string marshaler for managing string operations in WebAssembly memory.</param>
    /// <param name="runtime">The runtime context in which the caller operates.</param>
    public WasmCaller(Instance wasmInstance, StringMarshaler stringMarshaler, ExtensionRuntime runtime)
    {
        _stringMarshaler = stringMarshaler;
        _instance = wasmInstance;
        _runtime = runtime;
    }

    /// <summary>
    /// Abstract method to compile a given context using the WebAssembly module.
    /// Implementing classes should define the specific logic for compilation based on the module and runtime context.
    /// </summary>
    /// <param name="context">The context to be compiled by the WebAssembly module.</param>
    /// <returns>The result of the compilation as a string.</returns>
    public abstract string ChordCompile(string context);
}