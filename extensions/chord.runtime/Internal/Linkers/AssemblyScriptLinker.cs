using Chord.Common;
using Chord.Runtime.Internal.Strings;
using Spectre.Console;
using Wasmtime;

namespace Chord.Runtime.Internal.Linkers;

/// <summary>
/// A specialized linker for AssemblyScript WebAssembly modules used in the Chord runtime.
/// It defines kernel functions specific to AssemblyScript and links them to the corresponding WebAssembly module.
/// </summary>
internal sealed class AssemblyScriptLinker(Engine engine, Store store, StringMarshaler stringMarshaler, ExtensionRuntime runtime, ContributionType contributionType) : WasmLinker(engine, store, stringMarshaler, runtime, contributionType)
{
    private const string moduleName = "chord_as";
    public override void DefineKernel()
    {
        Define(moduleName, "write_line", Function.FromCallback(_store, (int address) =>
         {
             var line = _stringMarshaler.ReadString(address);
             _runtime.StandardOut.MarkupLine(line);
         }));
        Define(moduleName, "write_error", Function.FromCallback(_store, (int address) =>
        {
            var line = _stringMarshaler.ReadString(address);
            _runtime.StandardError.MarkupLine(line);
        }));
        Define(moduleName, "get_bebopc_version", Function.FromCallback(_store, () =>
        {
            // don't dispose or assemblyscript will read garbage
            var wasmString = _stringMarshaler.CreateString(_runtime.EngineVersion);
            return wasmString.Address;
        }));
    }
}