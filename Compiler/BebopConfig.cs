using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Generators;

namespace Compiler
{
    /// <summary>
    /// A strongly typed representation of the bebop.json file.
    /// </summary>
    public class BebopConfig
    {
        /// <summary>
        /// Specifies a list of code generators to target during compilation.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("generators")]
        public GeneratorConfig[]? Generators { get; set; }

        /// <summary>
        /// Specifies a namespace that generated code will use.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("namespace")]
        [JsonConverter(typeof(MinMaxLengthCheckConverter))]
        public string? Namespace { get; set; }

        /// <summary>
        /// Specifies an array of filenames or patterns to compile. These filenames are resolved
        /// relative to the directory containing the bebop.json file. If no 'include' property is
        /// present in a bebop.json, the compiler defaults to including all files in the containing
        /// directory and subdirectories except those specified by 'exclude'.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("include")]
        public string[]? Include { get; set; }

        /// <summary>
        /// Specifies an array of filenames or patterns that should be skipped when resolving
        /// include. The 'exclude' property only affects the files included via the 'include'
        /// property.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("exclude")]
        public string[]? Exclude { get; set; }

        /// <summary>
        /// Settings for the watch mode in bebopc.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("watchOptions")]
        public WatchOptions? WatchOptions { get; set; }

        public static BebopConfig? FromJson(string json) => JsonSerializer.Deserialize<BebopConfig>(json, Settings);

        private static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
        {
            Converters =
            {
                ServicesConverter.Singleton
            },
        };
    }

    public partial class GeneratorConfig
    {
        /// <summary>
        /// Specify the code generator schemas will be compiled to.
        /// </summary>
        [JsonPropertyName("alias")]
        public string? Alias { get; set; }

        /// <summary>
        /// Specify the version of the language the code generator should target.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("langVersion")]
        public string? LangVersion { get; set; }

        /// <summary>
        /// Specify if the code generator should produces a notice at the start of the output file
        /// stating code was auto-generated.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("noGenerationNotice")]
        public bool? NoGenerationNotice { get; set; }

        /// <summary>
        /// Specify a file that bundles all generated code into one file.
        /// </summary>
        [JsonPropertyName("outFile")]
        public string? OutFile { get; set; }

        /// <summary>
        /// By default, bebopc generates a concrete client and a service base class. This property
        /// can be used to limit bebopc asset generation.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("services")]
        public TempoServices? Services { get; set; } 
    }

    /// <summary>
    /// Settings for the watch mode in bebopc.
    /// </summary>
    public partial class WatchOptions
    {
        /// <summary>
        /// Remove a list of directories from the watch process.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("excludeDirectories")]
        public string[]? ExcludeDirectories { get; set; }

        /// <summary>
        /// Remove a list of files from the watch mode's processing.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("excludeFiles")]
        public string[]? ExcludeFiles { get; set; }
    }

    internal class MinMaxLengthCheckConverter : JsonConverter<string>
    {
        public override bool CanConvert(Type t) => t == typeof(string);

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value?.Length >= 1)
            {
                return value;
            }
            throw new Exception("Cannot unmarshal type string");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (value.Length >= 1)
            {
                JsonSerializer.Serialize(writer, value, options);
                return;
            }
            throw new Exception("Cannot marshal type string");
        }

        public static readonly MinMaxLengthCheckConverter Singleton = new MinMaxLengthCheckConverter();
    }

    internal class ServicesConverter : JsonConverter<TempoServices>
    {
        public override bool CanConvert(Type t) => t == typeof(TempoServices);

        public override TempoServices Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "both":
                    return TempoServices.Both;
                case "client":
                    return TempoServices.Client;
                case "none":
                    return TempoServices.None;
                case "server":
                    return TempoServices.Server;
            }
            throw new Exception("Cannot unmarshal type Services");
        }

        public override void Write(Utf8JsonWriter writer, TempoServices value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case TempoServices.Both:
                    JsonSerializer.Serialize(writer, "both", options);
                    return;
                case TempoServices.Client:
                    JsonSerializer.Serialize(writer, "client", options);
                    return;
                case TempoServices.None:
                    JsonSerializer.Serialize(writer, "none", options);
                    return;
                case TempoServices.Server:
                    JsonSerializer.Serialize(writer, "server", options);
                    return;
            }
            throw new Exception("Cannot marshal type Services");
        }

        public static readonly ServicesConverter Singleton = new ServicesConverter();
    }
}