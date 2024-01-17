using Chord.Runtime.Internal.Strings;
using Wasmtime;

namespace Chord.Runtime.Internal.Callers;

internal sealed class AssemblyScriptCaller : WasmCaller
{
    internal AssemblyScriptCaller(Instance wasmInstance, StringMarshaler stringMarshaler, ExtensionRuntime runtime) : base(wasmInstance, stringMarshaler, runtime)
    {
    }

    public override string ChordCompile(string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context, nameof(context));
        var compile = _instance.GetFunction<int, int>("chord_compile");
        if (compile == null)
        {
            throw new ExtensionRuntimeException("chord_compile function not found");
        }
        using var contextString = _stringMarshaler.CreateString(context);
        var returnAddress = compile.Invoke(contextString.Address);
        return _stringMarshaler.ReadString(returnAddress);
    }
}