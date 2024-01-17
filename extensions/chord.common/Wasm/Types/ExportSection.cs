namespace Chord.Common.Wasm.Types;

public sealed record ExportSection(Export[] Exports) : Section(SectionId.Export, Exports.Length);