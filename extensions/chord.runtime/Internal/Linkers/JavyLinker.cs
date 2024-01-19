using Chord.Common;
using Chord.Runtime.Internal.Strings;
using Spectre.Console;
using Wasmtime;

namespace Chord.Runtime.Internal.Linkers;

/// <summary>
/// A specialized linker for Javy WebAssembly modules used in the Chord runtime.
/// It defines kernel functions specific to Javy and links them to the corresponding WebAssembly module.
/// </summary>
internal sealed class JavyLinker(Engine engine, Store store, StringMarshaler stringMarshaler, ExtensionRuntime runtime, ContributionType contributionType) : WasmLinker(engine, store, stringMarshaler, runtime, contributionType)
{
    private const string moduleName = "chord_javy";
    public override void DefineKernel()
    {

    }
}