namespace Chord.Common.Wasm.Types;

public sealed record Export(string Name, ExportKind Kind, uint Index)
{
    public override string ToString()
    {
        return $"export{Kind switch
        {
            ExportKind.Function => $"(func {Name} {Index})",
            ExportKind.Table => $"(table {Name} {Index})",
            ExportKind.Memory => $"(memory {Name} {Index})",
            ExportKind.Global => $"(global {Name} {Index})",
            _ => throw new ArgumentOutOfRangeException()
        }}";
    }
}