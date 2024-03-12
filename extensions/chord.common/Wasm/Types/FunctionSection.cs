namespace Chord.Common.Wasm.Types;

public sealed record FunctionSection(uint[] TypeIndices) : Section(SectionId.Function, TypeIndices.Length);