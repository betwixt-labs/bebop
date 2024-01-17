namespace Chord.Common.Wasm.Types;

/// <summary>
/// The kind of export
/// </summary>
public enum ExportKind : byte
{
    /// <summary>
    /// A function export
    /// </summary>
    Function = 0,
    /// <summary>
    /// A table export
    /// </summary>
    Table = 1,
    /// <summary>
    /// A memory export
    /// </summary>
    Memory = 2,
    /// <summary>
    /// A global export
    /// </summary>
    Global = 3
}