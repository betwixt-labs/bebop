namespace Chord.Common.Wasm.Types;

/// <summary>
/// A custom section in the wasm binary. Contains arbitrary bytes that may have meaning in certain environments.
/// </summary>
/// <param name="Name">The name of the custom section</param>
/// <param name="Content">The content of the custom section</param>
public record CustomSection(string Name, byte[] Content) : Section(SectionId.Custom, Content.Length + 1);