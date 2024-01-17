using Chord.Common;
using Chord.Runtime.Internal.Strings;
using Spectre.Console;
using Wasmtime;

namespace Chord.Runtime.Internal.Linkers;

/// <summary>
/// A specialized linker for TinyGo WebAssembly modules used in the Chord runtime.
/// It defines kernel functions specific to TinyGo and links them to the corresponding WebAssembly module.
/// </summary>
internal sealed class TinyGoLinker(Engine engine, Store store, StringMarshaler stringMarshaler, ExtensionRuntime runtime, ContributionType contributionType) : WasmLinker(engine, store, stringMarshaler, runtime, contributionType)
{
    /// <summary>
    /// Defines kernel functions specific to TinyGo, linking them to the WebAssembly module.
    /// This includes functions for standard output and error, as well as getting version information.
    /// </summary>
    public override void DefineKernel()
    {
        Define("chord_tinygo", "write_line", Function.FromCallback(_store, (int address, int length) =>
        {
            var line = _stringMarshaler.ReadString(address, length);
            _runtime.StandardOut.MarkupLine(line);
        }));
        Define("chord_tinygo", "write_error", Function.FromCallback(_store, (int address, int length) =>
        {
            var line = _stringMarshaler.ReadString(address, length);
            _runtime.StandardError.MarkupLine(line);
        }));
        Define("chord_tinygo", "get_bebopc_version", Function.FromCallback(_store, (int address) =>
        {
            _stringMarshaler.WriteString(address, _runtime.EngineVersion);
        }));
    }
}