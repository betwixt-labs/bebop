namespace Chord.Common.Wasm.Types;

public sealed record ImportSection(Import[] Imports) : Section(SectionId.Import, Imports.Length);