using Chord.Common.Internal;

namespace Chord.Common.Wasm.Types;


public enum FunctionKind : sbyte
{
    /// <summary>
    /// A function.
    /// </summary>
    Function = 0x60,
}

/// <summary>
/// Types suitable when a value is expected.
/// </summary>
public enum WebAssemblyValueType : sbyte
{
    /// <summary>
    /// 32-bit integer value-type, equivalent to .NET's <see cref="int"/> and <see cref="uint"/>.
    /// </summary>
    Int32 = 0x7f,
    /// <summary>
    /// 64-bit integer value-type, equivalent to .NET's <see cref="long"/> and <see cref="ulong"/>.
    /// </summary>
    Int64 = 0x7e,
    /// <summary>
    /// 32-bit floating point value-type, equivalent to .NET's <see cref="float"/>.
    /// </summary>
    Float32 = 0x7d,
    /// <summary>
    /// 64-bit floating point value-type, equivalent to .NET's <see cref="double"/>.
    /// </summary>
    Float64 = 0x7c,
}

public static class ValueTypeExtensions
{
    public static System.Type ToSystemType(this WebAssemblyValueType valueType) => valueType switch
    {
        WebAssemblyValueType.Int32 => typeof(int),
        WebAssemblyValueType.Int64 => typeof(long),
        WebAssemblyValueType.Float32 => typeof(float),
        WebAssemblyValueType.Float64 => typeof(double),
        _ => throw new System.ArgumentOutOfRangeException(nameof(valueType), $"{nameof(WebAssemblyValueType)} {valueType} not recognized."),
    };

    public static string ToTypeString(this WebAssemblyValueType valueType) => valueType switch
    {
        WebAssemblyValueType.Int32 => "i32",
        WebAssemblyValueType.Int64 => "i64",
        WebAssemblyValueType.Float32 => "f32",
        WebAssemblyValueType.Float64 => "f64",
        _ => throw new System.ArgumentOutOfRangeException(nameof(valueType), $"{nameof(WebAssemblyValueType)} {valueType} not recognized."),
    };

    private static readonly RegeneratingWeakReference<Dictionary<System.Type, WebAssemblyValueType>> systemTypeToValueType
        = new(() => new Dictionary<System.Type, WebAssemblyValueType>
        {
                { typeof(int), WebAssemblyValueType.Int32 },
                { typeof(long), WebAssemblyValueType.Int64 },
                { typeof(float), WebAssemblyValueType.Float32 },
                { typeof(double), WebAssemblyValueType.Float64 },
        });

    public static bool TryConvertToValueType(this System.Type type, out WebAssemblyValueType value) => systemTypeToValueType.Reference.TryGetValue(type, out value);

    public static bool IsSupported(this System.Type type) => systemTypeToValueType.Reference.ContainsKey(type);
}