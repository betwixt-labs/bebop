using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Linq;
using Core.Exceptions;
using Core.Meta.Extensions;
using Core.Logging;
using Core.Internal;

namespace Core.Meta;

/// <summary>
/// Represents the configuration for Bebop compiler and runtime behavior.
/// </summary>
public partial class BebopConfig
{
    /// <summary>
    ///     The name of the config file used by bebopc.
    /// </summary>
    public const string ConfigFileName = "bebop.json";

    internal const string DefaultIncludeGlob = "**/*.bop";

    /// <summary>
    /// Gets or sets the file inclusion patterns for the Bebop compiler.
    /// </summary>
    [JsonPropertyName("include")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] Includes { get; set; } = [DefaultIncludeGlob];

    /// <summary>
    /// Gets or sets the file exclusion patterns for the Bebop compiler.
    /// </summary>
    [JsonPropertyName("exclude")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] Excludes { get; set; } = [];

    /// <summary>
    /// Gets or sets the configurations for each code generator.
    /// </summary>
    [JsonPropertyName("generators")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeneratorConfig[] Generators { get; set; } = [];

    /// <summary>
    /// Gets or sets the options for file system watching.
    /// </summary>
    [JsonPropertyName("watchOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WatchOptions WatchOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the warning codes to be suppressed by the Bebop compiler.
    /// </summary>
    [JsonPropertyName("noWarn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[] SupressedWarningCodes { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether the Bebop compiler should emit code.
    /// </summary>
    [JsonPropertyName("noEmit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool NoEmit { get; set; } = false;

    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string> Extensions { get; set; } = [];

    [JsonIgnore]
    public string WorkingDirectory { get; private set; } = null!;

    /// <summary>
    /// Resolves the file paths included in the configuration using the specified glob patterns.
    /// </summary>
    /// <returns>An enumerable of full file paths that match the include patterns and do not match the exclude patterns.</returns>
    public IEnumerable<string> ResolveIncludes()
    {
        List<string> files = [];

        var matcher = new Matcher();
        // Add relative paths
        matcher.AddIncludePatterns(Includes.Where(p => !Path.IsPathRooted(p) || !p.Contains("**")));
        matcher.AddExcludePatterns(Excludes);
        var matches = matcher.Execute(new SafeDirectoryInfoWrapper(new DirectoryInfo(WorkingDirectory))).Files;
        files.AddRange(matches.Select(match => Path.GetFullPath(Path.Combine(WorkingDirectory, match.Path))));

        foreach (var include in Includes)
        {
            // Handle non-glob absolute paths
            if (Path.IsPathRooted(include) && !include.Contains('*'))
            {
                if (File.Exists(include) && !Excludes.Contains(include))
                {
                    files.Add(include);
                }
            }
            // Handle absolute paths with glob patterns, including recursive patterns
            else if (Path.IsPathRooted(include) && include.Contains('*'))
            {
                string? baseDirPath;
                string? globPattern;

                if (include.Contains("**"))
                {
                    var separatorIndex = include.IndexOf("**");
                    baseDirPath = include[..separatorIndex];
                    globPattern = include[separatorIndex..];
                }
                else
                {
                    baseDirPath = Path.GetDirectoryName(include);
                    if (baseDirPath is null)
                    {
                        // Handle error or invalid path
                        continue; // Skip this iteration
                    }
                    globPattern = include[baseDirPath.Length..].TrimStart(Path.DirectorySeparatorChar);
                }
                var baseDirInfo = new DirectoryInfo(baseDirPath);
                if (baseDirInfo.Exists)
                {
                    var dirMatcher = new Matcher();
                    dirMatcher.AddInclude(globPattern);
                    var dirMatches = dirMatcher.Execute(new SafeDirectoryInfoWrapper(baseDirInfo)).Files;
                    files.AddRange(dirMatches.Select(match => Path.Combine(baseDirPath, match.Path)));
                }
            }
        }
        return files.Distinct(); // To avoid duplicates if any
    }

    /// <summary>
    /// Searches recursively upwards to locate the Bebop configuration file.
    /// </summary>
    /// <returns>The fully qualified path to the configuration file, or null if not found.</returns>
    public static string? Locate()
    {
        try
        {
            var workingDirectory = Directory.GetCurrentDirectory();
            var configFile = Directory.GetFiles(workingDirectory, ConfigFileName).FirstOrDefault();
            while (string.IsNullOrWhiteSpace(configFile))
            {
                if (Directory.GetParent(workingDirectory) is not { Exists: true } parent)
                {
                    break;
                }
                workingDirectory = parent.FullName;
                if (parent.GetFiles(ConfigFileName)?.FirstOrDefault() is { Exists: true } file)
                {
                    configFile = file.FullName;
                }
            }
            return configFile;
        }
        catch (Exception ex)
        {
            if (DiagnosticLogger.Instance is { TraceEnabled: true } logger)
            {
                logger.WriteDiagonstic(ex);
            }
            return null;
        }
    }

    /// <summary>
    /// Loads a BebopConfig from a file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>The deserialized BebopConfig object.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified configuration file is not found.</exception>
    public static BebopConfig FromFile(string? configPath)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(configPath, nameof(configPath));
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Failed to find bebop.json", configPath);

        var json = File.ReadAllText(configPath);
        var config = FromJson(json);
        config.WorkingDirectory = Path.GetDirectoryName(configPath) ?? throw new DirectoryNotFoundException("Failed to find directory containing bebop.json");
        return config;
    }

    /// <summary>
    /// Deserializes a BebopConfig from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized BebopConfig object.</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
    public static BebopConfig FromJson(string json) => JsonSerializer.Deserialize(json, JsonContext.Default.BebopConfig) ?? throw new JsonException("Failed to deserialize bebop.json");

    /// <summary>
    /// Serializes the BebopConfig to a JSON string.
    /// </summary>
    /// <returns>The current configuration as JSON</returns>.
    public string ToJson() => JsonSerializer.Serialize(this, JsonContext.Default.BebopConfig);

    public static BebopConfig Default => new()
    {
        WorkingDirectory = Directory.GetCurrentDirectory(),
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            throw new CompilerException("working directory is not defined.");
        }
        if (Directory.Exists(WorkingDirectory) is false)
        {
            throw new CompilerException($"working directory '{WorkingDirectory}' does not exist.");
        }
        if (Includes is { Length: > 0 })
        {
            for (var i = 0; i < Includes.Length; i++)
            {
                var include = Includes[i];
                if (string.IsNullOrWhiteSpace(include))
                {
                    throw new CompilerException($"include pattern at index {i} is null or whitespace");
                }
                if (!include.IsLegalFilePathOrGlob(out var invalidCharIndex))
                {
                    throw new CompilerException($"include pattern at index {i} is not a valid path or glob pattern{(invalidCharIndex >= 0 ? $": invalid character '{include[invalidCharIndex]}' at index {invalidCharIndex}" : ".")}");
                }
            }
        }
        if (Excludes is { Length: > 0 })
        {
            for (var i = 0; i < Excludes.Length; i++)
            {
                var exclude = Excludes[i];
                if (string.IsNullOrWhiteSpace(exclude))
                {
                    throw new CompilerException($"exclude pattern at index {i} is null or whitespace");
                }
                if (!exclude.IsLegalFilePathOrGlob(out var invalidCharIndex))
                {
                    throw new CompilerException($"exclude pattern at index {i} is not a valid path or glob pattern{(invalidCharIndex >= 0 ? $": invalid character '{exclude[invalidCharIndex]}' at index {invalidCharIndex}" : ".")}");
                }
            }
        }
        foreach (var generator in Generators)
        {
            if (string.IsNullOrWhiteSpace(generator.Alias))
            {
                throw new CompilerException("Generator alias is null or whitespace");
            }
            if (!generator.OutFile.IsLegalFilePath(out var invalidCharIndex))
            {
                throw new CompilerException($"Generator outFile '{generator.OutFile}' is not a valid file path{(invalidCharIndex >= 0 ? $": invalid character '{generator.OutFile[invalidCharIndex]}' at index {invalidCharIndex}" : ".")}");
            }
            if (!string.IsNullOrEmpty(generator.Namespace) && !generator.Namespace.IsValidNamespace())
            {
                throw new CompilerException($"Generator namespace '{generator.Namespace}' is not a valid namespace.");
            }

            if (generator is { OptionCount: > 0 })
            {
                foreach (var option in generator.GetOptions())
                {
                    if (string.IsNullOrWhiteSpace(option.Key))
                    {
                        throw new CompilerException($"Generator option key is null or whitespace");
                    }
                    if (string.IsNullOrWhiteSpace(option.Value))
                    {
                        throw new CompilerException($"Generator option value is null or whitespace");
                    }
                }
            }
        }

        for (var i = 0; i < WatchOptions.ExcludeDirectories.Length; i++)
        {
            var excludeDirectory = WatchOptions.ExcludeDirectories[i];
            if (string.IsNullOrWhiteSpace(excludeDirectory))
            {
                throw new CompilerException($"exclude directory at index {i} is null or whitespace");
            }
            if (!excludeDirectory.IsLegalPathOrGlob(out var invalidCharIndex))
            {
                throw new CompilerException($"exclude directory at index {i} is not a valid path or glob pattern{(invalidCharIndex >= 0 ? $": invalid character '{excludeDirectory[invalidCharIndex]}' at index {invalidCharIndex}" : ".")}");
            }
        }
        for (var i = 0; i < WatchOptions.ExcludeFiles.Length; i++)
        {
            var excludeFile = WatchOptions.ExcludeFiles[i];
            if (string.IsNullOrWhiteSpace(excludeFile))
            {
                throw new CompilerException($"exclude file at index {i} is null or whitespace");
            }
            if (!excludeFile.IsLegalFilePathOrGlob(out var invalidCharIndex))
            {
                throw new CompilerException($"exclude file at index {i} is not a valid file path or glob pattern{(invalidCharIndex >= 0 ? $": invalid character '{excludeFile[invalidCharIndex]}' at index {invalidCharIndex}" : ".")}");
            }
        }
    }
}

public class WatchOptions
{
    /// <summary>
    /// Gets or sets a list of directories to be excluded from file system watching.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("excludeDirectories")]
    public string[] ExcludeDirectories { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of files to be excluded from file system watching.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("excludeFiles")]
    public string[] ExcludeFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the output of the watch process should be preserved.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("preserveWatchOutput")]
    public bool PreserveWatchOutput { get; set; } = false;
}

/// <summary>
/// Provides custom JSON serialization and deserialization for <see cref="BebopConfig"/>.
/// This converter handles the unique JSON structure of the Bebop configuration.
/// </summary>
public class BebopConfigConverter : JsonConverter<BebopConfig>
{
    /// <summary>
    /// Reads and converts the JSON to an instance of <see cref="BebopConfig"/>.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
    /// <param name="typeToConvert">The type of object to convert to.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The deserialized <see cref="BebopConfig"/> object from the JSON.</returns>
    /// <exception cref="JsonException">Thrown when the JSON structure does not meet the expected format.</exception>
    public override BebopConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException("expected StartObject token");
        }
        var bebopConfig = new BebopConfig();
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return bebopConfig;
            }
            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                throw new JsonException("expected PropertyName token");
            }
            var propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "include":
                    var includes = JsonSerializer.Deserialize<string[]>(ref reader, options);
                    if (includes is not { Length: > 0 })
                    {
                        includes = [BebopConfig.DefaultIncludeGlob];
                    }
                    bebopConfig.Includes = includes;
                    break;
                case "exclude":
                    bebopConfig.Excludes = JsonSerializer.Deserialize<string[]>(ref reader, options) ?? [];
                    break;
                case "generators":
                    bebopConfig.Generators = ReadGenerators(ref reader, options);
                    break;
                case "watchOptions":
                    bebopConfig.WatchOptions = ReadWatchOptions(ref reader, options);
                    break;
                case "noWarn":
                    bebopConfig.SupressedWarningCodes = JsonSerializer.Deserialize<int[]>(ref reader, options) ?? [];
                    break;
                case "noEmit":
                    bebopConfig.NoEmit = reader.GetBoolean();
                    break;
                case "extensions":
                    bebopConfig.Extensions = ReadExtensions(ref reader);
                    break;
                default:
                    throw new JsonException($"unexpected property {propertyName}");
            }
        }
        return bebopConfig;
    }

    /// <summary>
    /// Reads the generator configurations from the JSON reader and constructs an array of <see cref="GeneratorConfig"/>.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>An array of <see cref="GeneratorConfig"/> objects.</returns>
    /// <exception cref="JsonException">Thrown when the JSON structure of the generators is not valid.</exception>
    private static Dictionary<string, string> ReadExtensions(ref Utf8JsonReader reader)
    {
        var extensions = new Dictionary<string, string>();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("expected StartObject token for options");
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var extensionName = reader.GetString();
                if (string.IsNullOrWhiteSpace(extensionName))
                {
                    throw new JsonException("extension name is null or whitespace");
                }
                reader.Read(); // Move to the value
                if (reader.TokenType is not JsonTokenType.String)
                {
                    throw new JsonException("expected String token for extension value");
                }

                var extensionValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(extensionValue))
                {
                    throw new JsonException($"version for '{extensionName}' is null or whitespace");
                }
                extensions[extensionName] = extensionValue;
            }
        }
        return extensions;
    }

    private static GeneratorConfig[] ReadGenerators(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var generators = new List<GeneratorConfig>();

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("expected StartObject token for generators");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var alias = reader.GetString();
                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new JsonException("unable to read generator alias");
                }
                reader.Read();

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("expected StartObject token for generator config");
                }
                bool simpleConstructor = true;
                string? outFile = null;
                TempoServices? services = null;
                bool? emitNotice = null;
                bool? emitBinarySchema = null;
                string? @namespace = null;
                var generatorOptions = new Dictionary<string, string>();

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propName = reader.GetString();
                        reader.Read(); // Move to the value

                        switch (propName)
                        {
                            case "outFile":
                                outFile = reader.GetString();
                                break;
                            case "services":
                                services = JsonSerializer.Deserialize<TempoServices>(ref reader, options);
                                simpleConstructor = false;
                                break;
                            case "emitNotice":
                                emitNotice = reader.GetBoolean();
                                simpleConstructor = false;
                                break;
                            case "emitBinarySchema":
                                emitBinarySchema = reader.GetBoolean();
                                simpleConstructor = false;
                                break;
                            case "namespace":
                                @namespace = reader.GetString();
                                simpleConstructor = false;
                                break;
                            case "options":
                                ReadGeneratorOptions(ref reader, generatorOptions);
                                simpleConstructor = false;
                                break;
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(outFile))
                {
                    throw new JsonException("'outFile' is null or whitespace");
                }
                EnsureLegalFilePath(outFile);

                GeneratorConfig generatorConfig;
                if (simpleConstructor)
                {
                    // Use the minimal constructor if only required properties are present
                    generatorConfig = new GeneratorConfig(alias, outFile);
                }
                else
                {
                    // Use the full constructor if any optional properties are present
                    generatorConfig = new GeneratorConfig(alias,
                                                          outFile,
                                                          services.GetValueOrDefault(TempoServices.Both),
                                                          emitNotice.GetValueOrDefault(true),
                                                          @namespace ?? string.Empty,
                                                          emitBinarySchema.GetValueOrDefault(true),
                                                          generatorOptions);
                }
                generators.Add(generatorConfig);
            }
        }
        return generators.ToArray();
    }


    /// <summary>
    /// Reads the generator configurations from the JSON reader and constructs an array of <see cref="GeneratorConfig"/>.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>An array of <see cref="GeneratorConfig"/> objects.</returns>
    /// <exception cref="JsonException">Thrown when the JSON structure of the generators is not valid.</exception>
    private static void ReadGeneratorOptions(ref Utf8JsonReader reader, Dictionary<string, string> options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("expected StartObject token for options");
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var optionKey = reader.GetString();
                if (string.IsNullOrWhiteSpace(optionKey))
                {
                    throw new JsonException("option key is null or whitespace");
                }
                reader.Read(); // Move to the value
                if (reader.TokenType is not JsonTokenType.String)
                {
                    throw new JsonException("expected String token for option value");
                }

                var optionValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(optionValue))
                {
                    throw new JsonException($"option value for '{optionKey}' is null or whitespace");
                }
                options[optionKey] = optionValue;
            }
        }
    }

    /// <summary>
    /// Reads watch options from the JSON reader and returns an instance of <see cref="WatchOptions"/>.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>An instance of <see cref="WatchOptions"/>.</returns>
    /// <exception cref="JsonException">Thrown when the JSON structure of the watch options is not valid.</exception>
    private static WatchOptions ReadWatchOptions(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var watchOptions = new WatchOptions();
        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException("expected StartObject token for watchOptions");
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propName = reader.GetString();
                reader.Read(); // Move to the value

                switch (propName)
                {
                    case "excludeDirectories":
                        watchOptions.ExcludeDirectories = JsonSerializer.Deserialize<string[]>(ref reader, options) ?? [];
                        break;
                    case "excludeFiles":
                        watchOptions.ExcludeFiles = JsonSerializer.Deserialize<string[]>(ref reader, options) ?? [];
                        break;
                    case "preserveWatchOutput":
                        watchOptions.PreserveWatchOutput = reader.GetBoolean();
                        break;
                }
            }
        }
        return watchOptions;
    }


    /// <summary>
    /// Writes the <see cref="BebopConfig"/> object to JSON.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="value">The <see cref="BebopConfig"/> object to serialize.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
    public override void Write(Utf8JsonWriter writer, BebopConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Includes is { Length: > 0 })
        {
            // Write "include" array
            writer.WritePropertyName("include");
            writer.WriteStartArray();
            foreach (var include in value.Includes)
            {
                writer.WriteStringValue(include);
            }
            writer.WriteEndArray();
        }

        // Write "exclude" array
        if (value.Excludes is { Length: > 0 })
        {
            writer.WritePropertyName("exclude");
            writer.WriteStartArray();
            foreach (var exclude in value.Excludes)
            {
                writer.WriteStringValue(exclude);
            }
            writer.WriteEndArray();
        }

        if (value.Generators is { Length: > 0 })
        {
            // Write "generators" object
            WriteGenerators(writer, value.Generators, options);
        }

        if (value.WatchOptions is { ExcludeDirectories.Length: > 0 } or { ExcludeFiles.Length: > 0 })
        {
            // Write "watchOptions" object
            WriteWatchOptions(writer, value.WatchOptions, options);
        }
        if (value.SupressedWarningCodes is { Length: > 0 })
        {
            writer.WritePropertyName("noWarn");
            writer.WriteStartArray();
            foreach (var warningCode in value.SupressedWarningCodes)
            {
                writer.WriteNumberValue(warningCode);
            }
            writer.WriteEndArray();
        }
        if (value.NoEmit)
        {
            writer.WriteBoolean("noEmit", value.NoEmit);
        }

        writer.WriteEndObject();
    }

    private static void WriteGenerators(Utf8JsonWriter writer, GeneratorConfig[] generators, JsonSerializerOptions options)
    {
        writer.WriteStartObject("generators");

        foreach (var generator in generators)
        {
            writer.WritePropertyName(generator.Alias);

            writer.WriteStartObject();
            writer.WriteString("outFile", generator.OutFile);

            if (generator.Services is not TempoServices.Both)
                JsonSerializer.Serialize(writer, generator.Services, options);

            if (generator.EmitNotice is false)
                writer.WriteBoolean("emitNotice", generator.EmitNotice);

            if (generator.EmitBinarySchema is false)
                writer.WriteBoolean("emitBinarySchema", generator.EmitBinarySchema);

            if (!string.IsNullOrWhiteSpace(generator.Namespace))
                writer.WriteString("namespace", generator.Namespace);

            if (generator is { OptionCount: > 0 })
            {
                writer.WriteStartObject("options");
                foreach (var option in generator.GetOptions())
                {
                    writer.WritePropertyName(option.Key);
                    writer.WriteStringValue(option.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static void WriteWatchOptions(Utf8JsonWriter writer, WatchOptions watchOptions, JsonSerializerOptions options)
    {
        writer.WriteStartObject("watchOptions");
        if (watchOptions.ExcludeDirectories is { Length: > 0 })
        {
            writer.WritePropertyName("excludeDirectories");
            writer.WriteStartArray();
            foreach (var excludeDirectory in watchOptions.ExcludeDirectories)
            {
                writer.WriteStringValue(excludeDirectory);
            }
            writer.WriteEndArray();
        }

        if (watchOptions.ExcludeFiles is { Length: > 0 })
        {
            writer.WritePropertyName("excludeFiles");
            writer.WriteStartArray();
            foreach (var excludeFile in watchOptions.ExcludeFiles)
            {
                writer.WriteStringValue(excludeFile);
            }
            writer.WriteEndArray();
        }

        if (watchOptions.PreserveWatchOutput is true)
        {
            writer.WriteBoolean("preserveWatchOutput", watchOptions.PreserveWatchOutput);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Ensures that the given file path and file name do not contain any illegal characters.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <exception cref="JsonException">Thrown if the file path or file name contains illegal characters.</exception>
    private static void EnsureLegalFilePath(string path)
    {
        // Check for invalid path characters
        if (!path.IsLegalPath(out var invalidPathCharIndex))
        {
            throw new JsonException($"The path '{path}' contains invalid characters: '{path[invalidPathCharIndex]}'");
        }

        // Extract the file name from the path and check for invalid file name characters
        if (!path.IsLegalFilePath(out var invalidFileNameCharIndex))
        {
            throw new JsonException($"The file name '{Path.GetFileName(path)}' contains invalid characters: '{path[invalidFileNameCharIndex]}'");
        }
    }

}