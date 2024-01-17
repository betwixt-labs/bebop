namespace Chord.Common.Wasm.Types;

public sealed record Import(string Module, string Field, ImportKind Kind, uint Index)
{
    public override string ToString()
    {
        return $"import{Kind switch
        {
            ImportKind.Function => $"(func {Module}.{Field} {Index})",
            ImportKind.Table => $"(table {Module}.{Field} {Index})",
            ImportKind.Memory => $"(memory {Module}.{Field} {Index})",
            ImportKind.Global => $"(global {Module}.{Field} {Index})",
            _ => throw new ArgumentOutOfRangeException()
        }}";
    }
}