using System.Text.Json;
using System.Text.RegularExpressions;
using Chord.Common.Wasm;
using Semver;

namespace Chord.Common;

internal sealed partial class ChordManifestConverter
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bool",
        "byte",
        "uint8",
        "uint16",
        "int16",
        "uint32",
        "int32",
        "uint64",
        "int64",
        "float32",
        "float64",
        "string"
    };

    internal static string ValidateType(string? type)
    {
        const string message = "Invalid type";
        if (string.IsNullOrWhiteSpace(type))
            throw new JsonException(message, new FormatException("Type cannot be empty."));
        if (!ValidTypes.Contains(type))
            throw new JsonException(message, new FormatException("Unknown or valid type."));
        return type.ToLowerInvariant();
    }

    internal static Uri ValidateUrl(string? url)
    {
        const string message = "Invalid URL";
        if (string.IsNullOrWhiteSpace(url))
            throw new JsonException(message, new FormatException("URL cannot be empty."));
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new JsonException(message, new FormatException("URL is not in the correct format."));
        if (!uri.IsAbsoluteUri)
            throw new JsonException(message, new FormatException("URL must be absolute."));
        if (uri.Scheme != "https")
            throw new JsonException(message, new FormatException("URL must use HTTPS."));

        return uri;
    }

    internal static string ValidateChordName(string? pluginName)
    {
        const string message = "Invalid extension name";
        if (string.IsNullOrWhiteSpace(pluginName))
            throw new JsonException(message, new FormatException("Extension name cannot be empty."));
        if (pluginName.StartsWith("@", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonException(message, new FormatException("Scoped extension name is not supported."));
        }
        if (!ExtensionNameRegex().IsMatch(pluginName))
            throw new JsonException(message, new FormatException("Extension name is not in the correct format."));
        if (pluginName.Length > 214)
            throw new JsonException(message, new FormatException("Extension name is too long."));
        return pluginName;
    }

    internal static string ValidateDescription(string? description)
    {
        const string message = "Invalid description";
        if (string.IsNullOrWhiteSpace(description))
            throw new JsonException(message, new FormatException("Description cannot be empty."));
        if (description.Length > 280)
            throw new JsonException(message, new FormatException("Description is too long."));
        return description;
    }
    internal static string ValidateEmail(string? email)
    {
        const string message = "Invalid email";
        if (string.IsNullOrWhiteSpace(email))
            throw new JsonException(message, new FormatException("Email cannot be empty."));
        if (email.Length > 254)
            throw new JsonException(message, new FormatException("Email is too long."));
        if (!EmailRegex().IsMatch(email))
            throw new JsonException(message, new FormatException("Email is not in the correct format."));

        return email;
    }

    internal static SemVersion ValidateChordVersion(string? pluginVersion)
    {
        const string message = "Invalid version";
        if (string.IsNullOrWhiteSpace(pluginVersion))
            throw new JsonException(message, new FormatException("Version cannot be empty."));

        if (!Semver.SemVersion.TryParse(pluginVersion, Semver.SemVersionStyles.Strict, out var v))
            throw new JsonException(message, new FormatException("Version is not in the correct SemVer format."));
        return v;
    }

    internal static SemVersionRange ValidateVersionRange(string? version)
    {
        const string message = "Invalid version range";
        if (string.IsNullOrWhiteSpace(version))
            throw new JsonException(message, new FormatException("Version range cannot be empty."));
        if (version.Equals("*", StringComparison.OrdinalIgnoreCase))
            throw new JsonException(message, new FormatException("Version range cannot be *."));
        if (!Semver.SemVersionRange.TryParseNpm(version, out var v))
            throw new JsonException(message, new FormatException("Version range is not in the correct SemVer/npm format."));
        return v;
    }

    internal static ChordAuthor ValidateAuthor(string? name, Uri? url, string? email)
    {
        const string message = "Invalid author";
        if (string.IsNullOrWhiteSpace(name))
            throw new JsonException(message, new FormatException("Author name cannot be empty."));

        if (name.Length > 50)
            throw new JsonException(message, new FormatException("Author name is too long."));

        return new ChordAuthor(name, url, email);
    }

    internal static string ValidateContributedAlias(string? contributedAlias)
    {
        const string message = "Invalid contributed alias";
        if (string.IsNullOrWhiteSpace(contributedAlias))
            throw new JsonException(message, new FormatException("Contributed alias cannot be empty."));
        if (!ContributedGeneratorAliasRegex().IsMatch(contributedAlias))
            throw new JsonException(message, new FormatException("Contributed alias cannot override a built-in generator."));
        if (contributedAlias.Length > 7)
            throw new JsonException(message, new FormatException("Contributed alias is too long."));
        return contributedAlias;
    }

    internal static Regex ValidateRegex(string? regex)
    {
        const string message = "Invalid regex";
        if (string.IsNullOrWhiteSpace(regex))
            throw new JsonException(message, new FormatException("Regex cannot be empty."));
        try
        {
            return new Regex(regex, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            throw new JsonException(message, new FormatException("Unable to create Regex from input.", ex));
        }
    }

    internal static WasmCompiler ValidateCompiler(string? compiler)
    {
        const string message = "Invalid compiler";
        if (string.IsNullOrWhiteSpace(compiler))
            throw new JsonException(message, new FormatException("Compiler cannot be empty."));
        return compiler.ToLowerInvariant() switch
        {
            "as" => WasmCompiler.AssemblyScript,
            "tinygo" => WasmCompiler.TinyGo,
            "javy" => WasmCompiler.Javy,
            _ => throw new JsonException(message, new FormatException("Unknown compiler."))
        };
    }

    internal static string ValidateGeneratorAlias(string? generatorAlias)
    {
        const string message = "Invalid generator alias";
        if (string.IsNullOrWhiteSpace(generatorAlias))
            throw new JsonException(message, new FormatException("Generator alias cannot be empty."));
        var alias = generatorAlias.Trim();
        if (!GeneratorAliasRegex().IsMatch(generatorAlias))
            throw new JsonException(message, new FormatException("Generator alias is not in the correct format."));
        if (generatorAlias.Length > 7)
            throw new JsonException(message, new FormatException("Generator alias is too long."));
        return alias;
    }

    internal static string ValidateIdentifier(string? indentifer)
    {
        const string message = "Invalid identifier";
        if (string.IsNullOrWhiteSpace(indentifer))
            throw new JsonException(message, new FormatException("Identifier cannot be empty."));
        if (!IdentifierRegex().IsMatch(indentifer))
            throw new JsonException(message, new FormatException("Identifier is not in the correct format."));
        if (indentifer.Length > 32)
            throw new JsonException(message, new FormatException("Identifier is too long."));
        return indentifer;
    }

    internal static ChordDecoratorTargets ValidateDecoratorTargets(string? decoratorTargets)
    {
        const string message = "Invalid decorator targets";
        if (string.IsNullOrWhiteSpace(decoratorTargets))
            throw new JsonException(message, new FormatException("Decorator targets cannot be empty."));

        if (!DecoratorTargetRegex().IsMatch(decoratorTargets))
            throw new JsonException(message, new FormatException("Decorator targets is not in the correct format."));


        if (decoratorTargets.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return ChordDecoratorTargets.All;
        }

        ChordDecoratorTargets result = ChordDecoratorTargets.None;

        var values = decoratorTargets.Split('|');
        foreach (var value in values)
        {
            result |= value.Trim().ToLower() switch
            {
                "enum" => ChordDecoratorTargets.Enum,
                "message" => ChordDecoratorTargets.Message,
                "struct" => ChordDecoratorTargets.Struct,
                "union" => ChordDecoratorTargets.Union,
                "field" => ChordDecoratorTargets.Field,
                "service" => ChordDecoratorTargets.Service,
                "method" => ChordDecoratorTargets.Method,
                _ => throw new JsonException(message, new FormatException("Unknown decorator targets value."))
            };
        }
        if (result == ChordDecoratorTargets.None)
            throw new JsonException(message, new FormatException("Decorator targets cannot be None."));

        return result;
    }


    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?:(?:@(?:[a-z0-9-*~][a-z0-9-*._~]*)?/[a-z0-9-._~])|[a-z0-9-~])[a-z0-9-._~]*$")]
    private static partial Regex ExtensionNameRegex();

    [GeneratedRegex(@"^(?!cs$|py$|ts$|rust$|dart$|cpp$)[a-z]+$")]
    private static partial Regex ContributedGeneratorAliasRegex();

    [GeneratedRegex(@"^[a-z]{1,7}$")]
    private static partial Regex GeneratorAliasRegex();

    [GeneratedRegex(@"^[a-zA-Z ]+$")]
    internal static partial Regex AlphabeticalRegex();


    [GeneratedRegex(@"^(all|((enum|message|struct|union|field|service|method)(\\|(enum|message|struct|union|field|service|method))*))$")]
    private static partial Regex DecoratorTargetRegex();

    [GeneratedRegex(@"^[a-z]+([A-Z][a-z]+)*$")]
    private static partial Regex IdentifierRegex();
}