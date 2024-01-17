using System.Text.Json;
using System.Text.Json.Serialization;
using Chord.Compiler.Internal.API.Responses;

namespace Chord.Compiler.Internal;


[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    AllowTrailingCommas = true,
    DefaultBufferSize = 10,
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    Converters = [typeof(RegistryCatalogConverter)])]
[JsonSerializable(typeof(PublishResponse))]
[JsonSerializable(typeof(RegistryCatalog))]
[JsonSerializable(typeof(RegistryVersion))]
internal partial class JsonContext : JsonSerializerContext
{
}
