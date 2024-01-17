using Chord.Runtime.Internal.Strings;
using Wasmtime;

namespace Chord.Runtime.Internal.Callers;

internal sealed class TinyGoCaller : WasmCaller
{
    internal TinyGoCaller(Instance wasmInstance, StringMarshaler stringMarshaler, ExtensionRuntime runtime) : base(wasmInstance, stringMarshaler, runtime)
    {
    }

    public override string ChordCompile(string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context, nameof(context));
        var compile = _instance.GetAction<int, int, int>("chord_compile");
        if (compile == null)
        {
            throw new ExtensionRuntimeException("chord_compile function not found");
        }

        var contextString = _stringMarshaler.CreateString(context);
        var returnString = _stringMarshaler.CreateString();
        compile(returnString.Address, contextString.Address, contextString.Length);
        return returnString.Value;
    }
}