namespace Chord.Common.Wasm.Types;

/// <summary>
/// The standard section identifiers.
/// </summary>
public enum SectionId : byte
{
    /// <summary>
    /// Custom sections have the id 0. They are intended to be used for debugging information or third-party extensions, and are ignored by the WebAssembly semantics. Their contents consist of a name further identifying the custom section, followed by an uninterpreted sequence of bytes for custom use.
    /// </summary>
    Custom,
    /// <summary>
    /// The type section has the id 1. It decodes into a vector of function types that represent the  component of a module.
    /// </summary>
    Type,
    /// <summary>
    /// The import section has the id 2. It decodes into a vector of imports that represent the  component of a module.
    /// </summary>
    Import,
    /// <summary>
    /// The function section has the id 3. It decodes into a vector of type indices that represent the  fields of the functions in the  component of a module. The  and  fields of the respective functions are encoded separately in the code section.
    /// </summary>
    Function,
    /// <summary>
    /// The table section has the id 4. It decodes into a vector of tables that represent the  component of a module.
    /// </summary>
    Table,
    /// <summary>
    /// The memory section has the id 5. It decodes into a vector of memories that represent the  component of a module.
    /// </summary>
    Memory,
    /// <summary>
    /// The global section has the id 6. It decodes into a vector of globals that represent the  component of a module.
    /// </summary>
    Global,
    /// <summary>
    /// The export section has the id 7. It decodes into a vector of exports that represent the  component of a module.
    /// </summary>
    Export,
    /// <summary>
    /// The start section has the id 8. It decodes into an optional start function that represents the  component of a module.
    /// </summary>
    Start,
    /// <summary>
    /// The element section has the id 9. It decodes into a vector of element segments that represent the  component of a module.
    /// </summary>
    Element,
    /// <summary>
    ///The code section has the id 10. It decodes into a vector of code entries that are pairs of value type vectors and expressions. They represent the  and  field of the functions in the  component of a module. The  fields of the respective functions are encoded separately in the function section.
    /// </summary>
    Code,
    /// <summary>
    /// The data section has the id 11. It decodes into a vector of data segments that represent the  component of a module.
    /// </summary>
    Data,
    /// <summary>
    /// The data count section has the id 12. It decodes into an optional u32 that represents the number of data segments in the data section. If this count does not match the length of the data segment vector, the module is malformed.
    /// </summary>
    DataCount
}

static class SectionExtensions
{
    public static bool IsValid(this SectionId section) => section >= SectionId.Custom && section <= SectionId.DataCount;
}