namespace Chord.Common.Wasm.Types
{
    /// <summary>
    /// A section in the wasm binary
    /// </summary>
    public abstract record Section(SectionId Id, int Size);
}