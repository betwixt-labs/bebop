using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Semver;
using Chord.Common.Wasm;
using Chord.Common.Extensions;

namespace Chord.Common;

internal sealed partial class ChordManifestConverter : JsonConverter<ChordManifest>
{
    private const string ErrorMessage = "Invalid chord manifest.";
    public override ChordManifest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("Expected StartObject token."));
        }
        bool isPrivate = false;
        string? name = null;
        string? description = null;
        SemVersion? version = null;
        string? license = null;
        string? bin = null;
        Uri? repository = null;
        Uri? homepage = null;
        BuildCommand? build = null;
        ChordContribution? contributions = null;
        ChordAuthor? author = null;
        ChordDependency[]? dependencies = null;
        ChordBugTracker? bugTracker = null;
        Dictionary<string, ChordPack>? pack = null;
        ChordEngine? engine = null;
        string? readme = null;
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                throw new JsonException(ErrorMessage, new FormatException("Expected PropertyName token."));
            }
            var propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "$schema":
                    reader.Skip();
                    break;
                case "name":
                    name = ValidateChordName(reader.GetString());
                    break;
                case "private":
                    isPrivate = reader.GetBoolean();
                    break;
                case "description":
                    description = ValidateDescription(reader.GetString());
                    break;
                case "version":
                    version = ValidateChordVersion(reader.GetString());
                    break;
                case "license":
                    license = reader.GetNonNullOrWhiteSpaceString();
                    break;
                case "bin":
                    bin = reader.GetNonNullOrWhiteSpaceString();
                    break;
                case "build":
                    build = GetBuildCommand(ref reader, options);
                    break;
                case "bugs":
                    bugTracker = GetBugTracker(ref reader, options);
                    break;
                case "author":
                    author = GetAuthor(ref reader, options);
                    break;
                case "repository":
                    repository = ValidateUrl(reader.GetString());
                    break;
                case "homepage":
                    homepage = ValidateUrl(reader.GetString());
                    break;
                case "pack":
                    pack = GetChordPack(ref reader, options);
                    break;
                case "dependencies":
                    dependencies = GetChordDependencies(ref reader, options);
                    break;
                case "contributes":
                    contributions = GetChordContribution(ref reader, options);
                    break;
                case "readme":
                    readme = reader.GetNonNullOrWhiteSpaceString();
                    break;
                case "engine":
                    engine = GetChordEngine(ref reader, options);
                    break;
                default:
                    throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'name' defined in 'chord' manifest."));
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'description' defined in 'chord' manifest."));
        }
        if (version is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'version' defined in 'chord' manifest."));
        }
        if (string.IsNullOrWhiteSpace(license))
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'license' defined in 'chord' manifest."));
        }

        if (string.IsNullOrWhiteSpace(bin))
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'bin' defined in 'chord' manifest."));
        }

        if (!bin.IsLegalFilePath(out var index))
        {
            throw new JsonException(ErrorMessage, new FormatException($"Bin path is invalid at index {index}"));
        }
        if (!string.IsNullOrWhiteSpace(readme) && !readme.IsLegalFilePath(out index))
        {
            throw new JsonException(ErrorMessage, new FormatException($"Readme path is invalid at index {index}"));
        }
        if (build is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'build' defined in 'chord' manifest."));
        }
        if (contributions is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'contributes' defined in 'chord' manifest."));
        }
        if (engine is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'engine' defined in 'chord' manifest."));
        }

        return new ChordManifest(
            name,
            isPrivate,
            description,
            version,
            license,
            bin,
            build,
            contributions,
            engine,
            author,
            dependencies,
            bugTracker,
            pack,
            repository,
            homepage,
            readme
        );
    }

    private ChordEngine? GetChordEngine(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for engine"));
        }
        SemVersionRange? bebopc = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "bebopc":
                        bebopc = ValidateVersionRange(reader.GetString());
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        if (bebopc is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'bebopc' defined in 'engine'"));
        }
        return new ChordEngine(bebopc);
    }

    private static BuildCommand GetBuildCommand(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for build"));
        }
        string? script = null;
        WasmCompiler compiler = WasmCompiler.None;
        ScriptShell shell = ScriptShell.None;
        string[]? args = null;
        Dictionary<string, string>? env = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "script":
                        script = reader.GetNonNullOrWhiteSpaceString();
                        break;
                    case "args":
                        args = JsonSerializer.Deserialize<string[]>(ref reader, JsonContext.Default.StringArray);
                        break;
                    case "env":
                        env = reader.ReadDictionary<string, string>();
                        break;
                    case "shell":
                        shell = reader.GetNonNullOrWhiteSpaceString().ToLowerInvariant() switch
                        {
                            "bash" => ScriptShell.Bash,
                            "sh" => ScriptShell.Sh,
                            "cmd" => ScriptShell.Cmd,
                            "powershell" => ScriptShell.Powershell,
                            "pwsh" => ScriptShell.Pwsh,
                            "python" => ScriptShell.Python,
                            _ => throw new JsonException(ErrorMessage, new FormatException($"Invalid shell "))
                        };
                        break;
                    case "compiler":
                        compiler = ValidateCompiler(reader.GetString());
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }

            }
        }

        if (script is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'command' defined in 'build'"));
        }
        if (compiler is WasmCompiler.None)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'compiler' defined in 'build'"));
        }
        return new BuildCommand(script, compiler, shell, args, env);
    }

    private static ChordAuthor GetAuthor(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for author"));
        }
        string? name = null;
        Uri? url = null;
        string? email = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "name":
                        name = reader.GetNonNullOrWhiteSpaceString();
                        break;
                    case "url":
                        url = ValidateUrl(reader.GetString());
                        break;
                    case "email":
                        email = ValidateEmail(reader.GetString());
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        return ValidateAuthor(name, url, email);
    }

    private static ChordBugTracker GetBugTracker(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for bug tracker"));
        }

        Uri? url = null;
        string? email = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "url":
                        url = ValidateUrl(reader.GetString());
                        break;
                    case "email":
                        email = ValidateEmail(reader.GetString());
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        if (url is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'url' defined in 'bugs'"));
        }
        return new ChordBugTracker(url, email);
    }

    private static Dictionary<string, ChordPack> GetChordPack(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for author"));
        }

        var packs = new Dictionary<string, ChordPack>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var alias = ValidateGeneratorAlias(reader.GetString());
                reader.Read();
                packs.Add(alias, GetChordPackValue(ref reader, options));
            }
        }
        if (packs.Count == 0)
        {
            throw new JsonException(ErrorMessage, new FormatException("Empty 'pack' collection defined in 'contributions'"));
        }
        if (packs.Count != packs.Select(x => x.Key).Distinct().Count())
        {
            throw new JsonException(ErrorMessage, new FormatException("Duplicate pack aliases defined in 'contributions'"));
        }
        return packs;
    }

    private static ChordPack GetChordPackValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for author"));
        }

        string? auxilaryFile = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "auxiliaryFile":
                        auxilaryFile = reader.GetNonNullOrWhiteSpaceString();
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        if (auxilaryFile is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'auxiliaryFile' defined in 'pack'"));
        }
        if (!auxilaryFile.IsLegalFilePath(out var index))
        {
            throw new JsonException(ErrorMessage, new FormatException($"Auxiliary file path is invalid at index {index}"));
        }
        return new ChordPack(auxilaryFile);
    }

    private static ChordDependency[] GetChordDependencies(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for dependencies"));
        }

        var dependencies = new List<ChordDependency>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var dependencyName = ValidateChordName(reader.GetString());
                reader.Read();
                var dependencyVersion = ValidateVersionRange(reader.GetString());
                dependencies.Add(new ChordDependency(dependencyName, dependencyVersion));
            }
        }
        return [.. dependencies];
    }


    private static ChordContribution GetChordContribution(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for contributions"));
        }
        ChordDecorator[]? decorators = null;
        ChordGenerator? generator = null;
        string[]? extendedAliases = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "decorators":
                        decorators = GetChordDecorators(ref reader, options).ToArray();
                        break;
                    case "generator":
                        generator = GetChordGenerator(ref reader, options);
                        break;
                    case "extends":
                        extendedAliases = JsonSerializer.Deserialize<string[]>(ref reader, JsonContext.Default.StringArray);
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        if (generator is not null && extendedAliases is not null)
        {
            throw new JsonException(ErrorMessage, new FormatException("Cannot define both 'generator' and 'extends' in 'contributions'"));
        }
        if (generator is null && extendedAliases is null)
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'generator' or 'extends' defined in 'contributions'"));
        }

        if (generator is not null)
        {
            return generator with { Decorators = decorators };
        }
        if (extendedAliases is not null)
        {
            var aliases = extendedAliases.Select(ValidateGeneratorAlias).ToList();
            if (aliases.Count == 0)
            {
                throw new JsonException(ErrorMessage, new FormatException("Empty 'extends' collection defined in 'contributions'"));
            }
            if (aliases.Count != aliases.Distinct().Count())
            {
                throw new JsonException(ErrorMessage, new FormatException("Duplicate generator aliases defined in 'contributions'"));
            }
            return new ChordExtender(aliases.ToArray(), decorators);
        }
        throw new JsonException(ErrorMessage, new FormatException("No 'generator' or 'extends' defined in 'contributions'"));
    }

    private static List<ChordDecorator> GetChordDecorators(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for decorators"));
        }

        var decorators = new List<ChordDecorator>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var identifier = ValidateIdentifier(reader.GetString());
                reader.Read(); // Move to the value

                // Ensure the value is an object
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for decorator"));
                }
                decorators.Add(GetChordDecorator(ref reader, options, identifier));
            }
        }
        if (decorators.Count == 0)
        {
            throw new JsonException(ErrorMessage, new FormatException("Empty 'decorators' collection defined in 'contributions'"));
        }
        if (decorators.Count != decorators.Select(x => x.Identifier).Distinct().Count())
        {
            throw new JsonException(ErrorMessage, new FormatException("Duplicate decorator identifiers defined in 'contributions'"));
        }
        return decorators;
    }

    private static ChordDecorator GetChordDecorator(ref Utf8JsonReader reader, JsonSerializerOptions options, string decoratorIdentifier)
    {
        string? description = null;
        ChordDecoratorTargets targets = ChordDecoratorTargets.All;
        bool allowMultiple = false;
        List<DecoratorParameter>? parameters = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to the value

                switch (propertyName)
                {
                    case "description":
                        description = ValidateDescription(reader.GetString());
                        break;
                    case "targets":
                        targets = ValidateDecoratorTargets(reader.GetString());
                        break;
                    case "allowMultiple":
                        allowMultiple = reader.GetBoolean();
                        break;
                    case "parameters":
                        parameters = GetDecoratorParameters(ref reader, options);
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new JsonException(ErrorMessage, new FormatException($"No 'description' defined in decorator[{decoratorIdentifier}]"));
        }
        if (targets is ChordDecoratorTargets.None)
        {
            throw new JsonException(ErrorMessage, new FormatException($"No 'targets' defined in decorator[{decoratorIdentifier}]"));
        }
        if (parameters is not null)
        {
            if (parameters.Count == 0)
            {
                throw new JsonException(ErrorMessage, new FormatException($"Empty 'parameters' collection defined in decorator[{decoratorIdentifier}]"));
            }
            if (parameters.Count != parameters.Select(x => x.Identifier).Distinct().Count())
            {
                throw new JsonException(ErrorMessage, new FormatException($"Duplicate parameter identifiers defined in decorator[{decoratorIdentifier}]"));
            }
            bool nonRequiredParameterEncountered = false;
            foreach (var parameter in parameters)
            {
                if (!parameter.Required)
                {
                    nonRequiredParameterEncountered = true;
                }
                else if (nonRequiredParameterEncountered)
                {
                    // A required parameter is found after a non-required parameter
                    throw new JsonException(ErrorMessage, new FormatException($"Required parameter[{parameter.Identifier}] defined after non-required parameter in decorator[{decoratorIdentifier}]"));
                }
            }
        }
        return new ChordDecorator(decoratorIdentifier, description, targets, allowMultiple, parameters?.ToArray());
    }

    private static List<DecoratorParameter> GetDecoratorParameters(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for parameters"));
        }
        var parameters = new List<DecoratorParameter>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var parameterName = ValidateIdentifier(reader.GetString());
                reader.Read(); // Move to the value

                string? description = null;
                bool required = false;
                JsonElement? defaultRaw = null;
                DefaultValueContainer? defaultValue = null;
                string? type = null;
                Regex? validator = null;
                string? validatorErrorReason = null;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read(); // Move to the value
                        switch (propertyName)
                        {
                            case "description":
                                description = ValidateDescription(reader.GetString());
                                break;
                            case "required":
                                required = reader.GetBoolean();
                                break;
                            case "default":
                                defaultRaw = JsonDocument.ParseValue(ref reader).RootElement;
                                break;
                            case "type":
                                type = ValidateType(reader.GetString());
                                break;
                            case "validator":
                                validator = ValidateRegex(reader.GetString());
                                break;
                            case "validatorErrorReason":
                                validatorErrorReason = reader.GetNonNullOrWhiteSpaceString();
                                break;
                            default:
                                throw new JsonException($"Property '{propertyName}' is not expected.");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    throw new JsonException(ErrorMessage, new FormatException($"No 'description' defined in parameter[{parameterName}]"));
                }
                if (string.IsNullOrWhiteSpace(type))
                {
                    throw new JsonException(ErrorMessage, new FormatException($"No 'type' defined in parameter[{parameterName}]"));
                }
                if (defaultRaw is not null && required)
                {
                    throw new JsonException(ErrorMessage, new FormatException($"Parameter '{parameterName}' is required but has a default value."));
                }
                if (defaultRaw is null && !required)
                {
                    throw new JsonException(ErrorMessage, new FormatException($"Parameter '{parameterName}' is not required but has no default value."));
                }
                if (defaultRaw is not null)
                {
                    defaultValue = type switch
                    {
                        "bool" => new DefaultValueContainer(defaultRaw.Value.GetBoolean()),
                        "byte" => new DefaultValueContainer(defaultRaw.Value.GetByte()),
                        "uint8" => new DefaultValueContainer(defaultRaw.Value.GetByte()),
                        "uint16" => new DefaultValueContainer(defaultRaw.Value.GetUInt16()),
                        "int16" => new DefaultValueContainer(defaultRaw.Value.GetInt16()),
                        "uint32" => new DefaultValueContainer(defaultRaw.Value.GetUInt32()),
                        "int32" => new DefaultValueContainer(defaultRaw.Value.GetInt32()),
                        "uint64" => new DefaultValueContainer(defaultRaw.Value.GetUInt64()),
                        "int64" => new DefaultValueContainer(defaultRaw.Value.GetInt64()),
                        "float32" => new DefaultValueContainer(defaultRaw.Value.GetSingle()),
                        "float64" => new DefaultValueContainer(defaultRaw.Value.GetDouble()),
                        "string" => new DefaultValueContainer(defaultRaw.Value.GetString() ?? throw new JsonException(ErrorMessage, new FormatException($"Parameter '{parameterName}' has a null default value."))),
                        _ => throw new JsonException(ErrorMessage, new FormatException($"Parameter '{parameterName}' has an invalid type."))
                    };
                }
                if (validator is not null && validatorErrorReason is null)
                {
                    throw new JsonException(ErrorMessage, new FormatException($"Parameter '{parameterName}' has a validator but no validator error reason."));
                }
                parameters.Add(new DecoratorParameter(parameterName, description, type, required, defaultValue, validator, validatorErrorReason));
            }
        }
        return parameters;
    }


    private static ChordGenerator GetChordGenerator(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(ErrorMessage, new FormatException("expected StartObject token for generator"));
        }
        string? name = null;
        string? alias = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "name":
                        name = reader.GetNonNullOrWhiteSpaceString();
                        break;
                    case "alias":
                        alias = ValidateContributedAlias(reader.GetString());
                        break;
                    default:
                        throw new JsonException(ErrorMessage, new FormatException($"Property '{propertyName}' is not expected."));
                }
            }
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'name' defined in 'generator'"));
        }
        if (!AlphabeticalRegex().IsMatch(name))
        {
            throw new JsonException(ErrorMessage, new FormatException($"Generator name '{name}' is not alphabetical."));
        }
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new JsonException(ErrorMessage, new FormatException("No 'alias' defined in 'generator'"));
        }
        return new ChordGenerator(name, alias);
    }

    public override void Write(Utf8JsonWriter writer, ChordManifest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteBoolean("private", value.IsPrivate);
        writer.WriteString("description", value.Description);
        writer.WriteString("version", value.Version.ToString());
        writer.WriteString("license", value.License);
        writer.WriteString("bin", value.Bin);
        writer.WriteStartObject("build");
        writer.WriteString("script", value.Build.Script);
        writer.WriteString("compiler", value.Build.Compiler.ToCompilerString());
        writer.WriteString("shell", value.Build.Shell.ToString().ToLowerInvariant());
        if (value.Build.Env is not null)
        {
            writer.WriteStartObject("env");
            foreach (var (key, val) in value.Build.Env)
            {
                writer.WriteString(key, val);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        if (value.Author is not null)
        {
            writer.WriteStartObject("author");
            writer.WriteString("name", value.Author.Name);
            if (value.Author.Url is not null)
            {
                writer.WriteString("url", value.Author.Url.ToString());
            }
            if (value.Author.Email is not null)
            {
                writer.WriteString("email", value.Author.Email);
            }
            writer.WriteEndObject();
        }
        if (value.BugTracker is not null)
        {
            writer.WriteStartObject("bugs");
            writer.WriteString("url", value.BugTracker.Url.ToString());
            if (value.BugTracker.Email is not null)
            {
                writer.WriteString("email", value.BugTracker.Email);
            }
            writer.WriteEndObject();
        }
        if (value.Repository is not null)
        {
            writer.WriteString("repository", value.Repository.ToString());
        }
        if (value.Homepage is not null)
        {
            writer.WriteString("homepage", value.Homepage.ToString());
        }
        if (value.Pack is not null)
        {
            writer.WriteStartObject("pack");
            foreach (var (key, val) in value.Pack)
            {
                writer.WriteStartObject(key);
                writer.WriteString("auxiliaryFile", val.AuxilaryFile);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        writer.WriteStartObject("engine");
        writer.WriteString("bebopc", value.Engine.Bebopc.ToString());
        writer.WriteEndObject();

        writer.WriteStartObject("contributes");
        if (value.Contributions is ChordGenerator generator)
        {
            writer.WriteStartObject("generator");
            writer.WriteString("name", generator.Name);
            writer.WriteString("alias", generator.Alias);
            writer.WriteEndObject();
        }
        else if (value.Contributions is ChordExtender extender)
        {
            writer.WriteStartArray("extends");
            foreach (var alias in extender.Aliases)
            {
                writer.WriteStringValue(alias);
            }
            writer.WriteEndArray();
        }
        if (value.Contributions.Decorators is not null)
        {
            writer.WriteStartObject("decorators");
            foreach (var decorator in value.Contributions.Decorators)
            {
                writer.WriteStartObject(decorator.Identifier);
                writer.WriteString("description", decorator.Description);
                writer.WriteString("targets", decorator.Targets.ToString());
                writer.WriteBoolean("allowMultiple", decorator.AllowMultiple);
                if (decorator.Parameters is not null)
                {
                    writer.WriteStartObject("parameters");
                    foreach (var parameter in decorator.Parameters)
                    {
                        writer.WriteStartObject(parameter.Identifier);
                        writer.WriteString("description", parameter.Description);
                        writer.WriteString("type", parameter.Type);
                        writer.WriteBoolean("required", parameter.Required);
                        if (parameter.DefaultValue is not null)
                        {
                            writer.WritePropertyName("default");
                            switch (parameter.DefaultValue.DefaultValue)
                            {
                                case bool boolValue:
                                    writer.WriteBooleanValue(boolValue);
                                    break;
                                case byte byteValue:
                                    writer.WriteNumberValue(byteValue);
                                    break;
                                case ushort ushortValue:
                                    writer.WriteNumberValue(ushortValue);
                                    break;
                                case short shortValue:
                                    writer.WriteNumberValue(shortValue);
                                    break;
                                case uint uintValue:
                                    writer.WriteNumberValue(uintValue);
                                    break;
                                case int intValue:
                                    writer.WriteNumberValue(intValue);
                                    break;
                                case ulong ulongValue:
                                    writer.WriteNumberValue(ulongValue);
                                    break;
                                case long longValue:
                                    writer.WriteNumberValue(longValue);
                                    break;
                                case float floatValue:
                                    writer.WriteNumberValue(floatValue);
                                    break;
                                case double doubleValue:
                                    writer.WriteNumberValue(doubleValue);
                                    break;
                                case string stringValue:
                                    writer.WriteStringValue(stringValue);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(parameter.DefaultValue));
                            }
                        }
                        if (parameter.Validator is not null)
                        {
                            writer.WriteString("validator", parameter.Validator.ToString());
                        }
                        if (parameter.ValidationErrorReason is not null)
                        {
                            writer.WriteString("validatorErrorReason", parameter.ValidationErrorReason);
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
