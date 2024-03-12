using System.Text;
using Chord.Common.Wasm.Types;

namespace Chord.Compiler.Internal;

public sealed record FunctionSignature(string Name, WebAssemblyValueType[] Parameters, WebAssemblyValueType[] Returns)
{
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('(');
        var nParams = Parameters.Length;
        for (var i = 0; i < nParams; i++)
        {
            builder.Append(Parameters[i].ToTypeString());
            if (i < nParams - 1)
            {
                builder.Append(',');
            }
        }
        builder.Append(") -> ");
        var nReturns = Returns.Length;
        if (nReturns == 0)
        {
            builder.Append("nil");
        }
        else
        {
            for (var i = 0; i < nReturns; i++)
            {
                builder.Append(Returns[i].ToTypeString());
                if (i < nReturns - 1)
                {
                    builder.Append(',');
                }
            }
        }
        return builder.ToString();
    }
}