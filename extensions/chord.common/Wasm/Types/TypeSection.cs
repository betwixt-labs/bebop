namespace Chord.Common.Wasm.Types;

public sealed record TypeSection(FunctionType[] Types) : Section(SectionId.Type, Types.Length);