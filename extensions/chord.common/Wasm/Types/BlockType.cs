using Chord.Common.Wasm.Types;

namespace Chord.Common.Wasm;
/// <summary>
/// Types for use as block signatures.
/// </summary>
public enum BlockType : sbyte
{
    /// <summary>
    /// 32-bit integer value-type, equivalent to .NET's <see cref="int"/> and <see cref="uint"/>.
    /// </summary>
    Int32 = -0x01,
    /// <summary>
    /// 64-bit integer value-type, equivalent to .NET's <see cref="long"/> and <see cref="ulong"/>.
    /// </summary>
    Int64 = -0x02,
    /// <summary>
    /// 32-bit floating point value-type, equivalent to .NET's <see cref="float"/>.
    /// </summary>
    Float32 = -0x03,
    /// <summary>
    /// 64-bit floating point value-type, equivalent to .NET's <see cref="double"/>.
    /// </summary>
    Float64 = -0x04,
    /// <summary>
    /// Pseudo type for representing an empty block type.
    /// </summary>
    Empty = -0x40,
}

static class BlockTypeExtensions
{
    public static bool TryToValueType(this BlockType blockType, out WebAssemblyValueType valueType)
    {
        switch (blockType)
        {
            default:
            case BlockType.Empty:
                valueType = default;
                return false;
            case BlockType.Int32:
                valueType = WebAssemblyValueType.Int32;
                break;
            case BlockType.Int64:
                valueType = WebAssemblyValueType.Int64;
                break;
            case BlockType.Float32:
                valueType = WebAssemblyValueType.Float32;
                break;
            case BlockType.Float64:
                valueType = WebAssemblyValueType.Float64;
                break;
        }

        return true;
    }

    public static string ToTypeString(this BlockType blockType) => blockType switch
    {
        BlockType.Int32 => "i32",
        BlockType.Int64 => "i64",
        BlockType.Float32 => "f32",
        BlockType.Float64 => "f64",
        BlockType.Empty => "",
        _ => "?",
    };
}