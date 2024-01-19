using System.Text;
using Chord.Common.Wasm;
using Wasmtime;

namespace Chord.Runtime.Internal.Strings;

internal sealed class JavyMarshaler : StringMarshaler
{
    public JavyMarshaler() : base(WasmCompiler.Javy)
    {
    }

    public override void Bind(Instance wasmInstance)
    {
        //NOOP
    }

    public override WasmString CreateString(string value, Encoding? encoding = null)
    {
        throw new NotImplementedException();
    }

    public override WasmString CreateString()
    {
        throw new NotImplementedException();
    }

    public override void FreeString(WasmString value)
    {
        throw new NotImplementedException();
    }

    public override string ReadString(int address, Encoding? encoding = null)
    {
        throw new NotImplementedException();
    }

    public override string ReadString(int address, int size, Encoding? encoding = null)
    {
        throw new NotImplementedException();
    }

    public override void WriteString(int address, string value, Encoding? encoding = null)
    {
        throw new NotImplementedException();
    }
}