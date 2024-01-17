namespace Chord.Common.Wasm;

public enum WasmCompiler
{
    None,
    AssemblyScript,
    TinyGo,
}

public static class WasmCompilerExtensions
{
    public static string ToCompilerString(this WasmCompiler compiler)
    {
        return compiler switch
        {
            WasmCompiler.None => "none",
            WasmCompiler.AssemblyScript => "as",
            WasmCompiler.TinyGo => "tinygo",
            _ => throw new ArgumentOutOfRangeException(nameof(compiler), compiler, null)
        };
    }
}