namespace Chord.Common.Wasm.Types;

/// <summary>
/// The kind of import
/// </summary>
public enum ImportKind
{
    /// <summary>
    /// A function import
    /// </summary>
    Function = 0,
    /// <summary>
    /// A table import
    /// </summary>
    Table = 1,
    /// <summary>
    /// A memory import
    /// </summary>
    Memory = 2,
    /// <summary>
    /// A global import
    /// </summary>
    Global = 3
}