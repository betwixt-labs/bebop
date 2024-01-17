using System.Text.Json;

namespace Chord.Common;

public sealed record PackedFile(string Name, byte[] Data, string Alias)
{
    public byte[] ToArray() => JsonSerializer.SerializeToUtf8Bytes(this, JsonContext.Default.PackedFile);
    public static PackedFile FromBytes(byte[] bytes) => JsonSerializer.Deserialize(bytes, JsonContext.Default.PackedFile) ?? throw new JsonException();
    public override string ToString() => JsonSerializer.Serialize(this, JsonContext.Default.PackedFile);
}