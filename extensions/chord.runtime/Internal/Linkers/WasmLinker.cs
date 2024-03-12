using Chord.Common;
using Chord.Runtime.Internal.Strings;
using Wasmtime;

namespace Chord.Runtime.Internal.Linkers;

/// <summary>
/// Represents an abstract base class for WebAssembly linkers used in the Chord runtime.
/// This class provides common functionalities for different types of Wasm linkers.
/// </summary>
internal abstract class WasmLinker : Linker
{
    private protected Store _store;
    protected readonly StringMarshaler _stringMarshaler;
    protected readonly ExtensionRuntime _runtime;
    protected readonly ContributionType _contributionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmLinker"/> class.
    /// </summary>
    /// <param name="engine">The WebAssembly engine used for module instantiation.</param>
    /// <param name="store">The store representing the WebAssembly runtime state.</param>
    /// <param name="stringMarshaler">The string marshaler for managing string operations in WebAssembly memory.</param>
    /// <param name="runtime">The runtime context in which the linker operates.</param>
    /// <param name="contributionType">The type of contribution the linker is intended to support.</param>
    protected WasmLinker(Engine engine, Store store, StringMarshaler stringMarshaler, ExtensionRuntime runtime, ContributionType contributionType) : base(engine)
    {
        _store = store;
        _stringMarshaler = stringMarshaler;
        _runtime = runtime;
        _contributionType = contributionType;
        DefineWasi();
    }

    /// <summary>
    /// Defines the kernel functions and environment for the WebAssembly modules.
    /// Each derived linker class should implement this method to set up the specific
    /// functions and environment needed for the module it supports.
    /// </summary>
    public abstract void DefineKernel();
}