using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chord.Common;


[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    AllowTrailingCommas = true,
    DefaultBufferSize = 10,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    Converters = [typeof(ChordManifestConverter)])]
[JsonSerializable(typeof(ChordManifest))]
[JsonSerializable(typeof(ChordAuthor))]
[JsonSerializable(typeof(ChordBugTracker))]
[JsonSerializable(typeof(ChordContribution))]
[JsonSerializable(typeof(ChordDependency))]
[JsonSerializable(typeof(ChordDependency[]))]
[JsonSerializable(typeof(ChordDecorator))]
[JsonSerializable(typeof(ChordDecorator[]))]
[JsonSerializable(typeof(BuildCommand))]
[JsonSerializable(typeof(ScriptShell))]
[JsonSerializable(typeof(ChordDecoratorTargets))]
[JsonSerializable(typeof(Wasm.Types.Section))]
[JsonSerializable(typeof(Wasm.Types.Section[]))]
[JsonSerializable(typeof(Wasm.Types.ImportSection))]
[JsonSerializable(typeof(Wasm.Types.ExportSection))]
[JsonSerializable(typeof(Wasm.Types.Import))]
[JsonSerializable(typeof(Wasm.Types.Import[]))]
[JsonSerializable(typeof(Wasm.Types.Export))]
[JsonSerializable(typeof(Wasm.Types.Export[]))]
[JsonSerializable(typeof(Wasm.Types.ImportKind))]
[JsonSerializable(typeof(Wasm.Types.SectionId))]
[JsonSerializable(typeof(Wasm.Types.CustomSection))]
[JsonSerializable(typeof(Wasm.Types.CustomSection[]))]
[JsonSerializable(typeof(Wasm.Types.ExportKind))]
[JsonSerializable(typeof(Wasm.WasmModule))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(PackedFile))]
internal partial class JsonContext : JsonSerializerContext { }