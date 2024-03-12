using System.Text;
using Chord.Common.Wasm;
using Wasmtime;

namespace Chord.Runtime.Internal.Strings;

internal abstract class StringMarshaler
{
    protected Instance? _wasmInstance;
    protected readonly WasmCompiler _compiler;
    public bool IsBound => _wasmInstance != null;

    public StringMarshaler(WasmCompiler compiler)
    {
        _compiler = compiler;
    }

    public abstract void Bind(Instance wasmInstance);

    public abstract WasmString CreateString(string value, Encoding? encoding = null);

    public abstract WasmString CreateString();

    public abstract void FreeString(WasmString value);
    public abstract string ReadString(int address, Encoding? encoding = null);
    public abstract string ReadString(int address, int size, Encoding? encoding = null);
    public abstract void WriteString(int address, string value, Encoding? encoding = null);
}

