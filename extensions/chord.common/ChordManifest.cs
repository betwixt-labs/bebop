using System.Text.Json;
using System.Text.RegularExpressions;
using Chord.Common.Wasm;
using Semver;

namespace Chord.Common
{
    /// <summary>
    /// Represents the manifest of a extension, detailing its properties such as name, description, version, etc.
    /// </summary>
    public sealed partial record ChordManifest(
        /// <summary>
        /// The name of the extension, part of URL, command line argument, and folder name. Must be URL-safe, no uppercase, <= 214 characters.
        /// </summary>
        string Name,

        bool IsPrivate,

        /// <summary>
        /// The description of the extension.
        /// </summary>
        string Description,

        /// <summary>
        /// The version of the extension, in semver format without comparison operators.
        /// </summary>
        SemVersion Version,

        /// <summary>
        /// License of the extension. Must be one of the specified standard licenses or a string.
        /// </summary>
        string License,

        /// <summary>
        /// Path to the compiled extension, a valid relative file path.
        /// </summary>
        string Bin,

        /// <summary>
        /// Defines how to build the extension, including command, args, env, and shell.
        /// </summary>
        BuildCommand Build,

        /// <summary>
        /// Defines what the extension contributes to bebopc, including generator, , and extends.
        /// </summary>
        ChordContribution Contributions,

        ChordEngine Engine,

        /// <summary>
        /// An array of authors of the extension. Optional.
        /// </summary>
        ChordAuthor? Author = null,

        /// <summary>
        /// An object of dependencies with extension name as key and semver range as value. Optional.
        /// </summary>
        ChordDependency[]? Dependencies = null,

        /// <summary>
        /// Where to report issues about the extension. Optional.
        /// </summary>
        ChordBugTracker? BugTracker = null,

        /// <summary>
        /// Path to an auxiliary file that will be packaged with the extension. Optional.
        /// </summary>
        Dictionary<string, ChordPack>? Pack = null,

        Uri? Repository = null,
        Uri? Homepage = null,
        string? ReadMe = null
    )
    {

        public static ChordManifest FromFile(string path) => FromJson(System.IO.File.ReadAllText(path));

        public static ChordManifest FromJson(string json) =>
        JsonSerializer.Deserialize<ChordManifest>(json, JsonContext.Default.ChordManifest)
        ?? throw new JsonException();

        public static ChordManifest FromBytes(byte[] bytes) =>
        JsonSerializer.Deserialize(bytes, JsonContext.Default.ChordManifest) ?? throw new JsonException();

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, JsonContext.Default.ChordManifest);
        }
    }

    public sealed record BuildCommand(
        string Script,
        WasmCompiler Compiler,
        ScriptShell Shell,
        string[]? Args = null,
        Dictionary<string, string>? Env = null
    );

    public sealed record ChordDependency(
        string Name,
        SemVersionRange Version
    );

    public sealed record ChordAuthor(
        string Name,
        Uri? Url = null,
        string? Email = null
    );

    public sealed record ChordBugTracker(
        Uri Url,
        string? Email = null
    );

    public sealed record ChordPack(
        string AuxilaryFile
    );

    public sealed record ChordDecorator(
        string Identifier,
        string Description,
        ChordDecoratorTargets Targets,
        bool AllowMultiple,
        DecoratorParameter[]? Parameters = null
    );

    public sealed record ChordEngine(
        SemVersionRange Bebopc
    );

    public sealed record DefaultValueContainer(object DefaultValue)
    {
        public T GetValue<T>() where T : struct
        {
            if (DefaultValue is T t)
                return t;
            throw new InvalidOperationException($"Value is not of type {typeof(T)}");
        }

        public override string ToString() => DefaultValue.ToString() ?? throw new InvalidOperationException();
    }

    public sealed record DecoratorParameter(
        string Identifier,
        string Description,
        string Type,
        bool Required,
        DefaultValueContainer? DefaultValue = default,
        Regex? Validator = null,
        string? ValidationErrorReason = null
    );

    public abstract record ChordContribution(
        ContributionType Type,
        ChordDecorator[]? Decorators = null
    );

    public sealed record ChordGenerator(
        string Name,
        string Alias,
        ChordDecorator[]? Decorators = null
    ) : ChordContribution(ContributionType.Generator, Decorators);

    public sealed record ChordExtender(
        string[] Aliases,
        ChordDecorator[]? Decorators = null
    ) : ChordContribution(ContributionType.Extender, Decorators);
}
