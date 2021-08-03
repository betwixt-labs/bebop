using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Generators;
using Core.Logging;
using Core.Meta;

namespace Compiler
{
    #region Records
    /// <summary>
    /// Represents a code generator passed to bebopc.
    /// </summary>
    /// <param name="Alias">The alias of the code generator.</param>
    /// <param name="OutputFile">The quailified file that the generator will produce.</param>
    /// <param name="LangVersion">If set this value defines the version of the language generated code will use.</param>
    public record CodeGenerator(string Alias, string OutputFile, Version? LangVersion);
    #endregion

    #region FlagAttribute

    /// <summary>
    ///     Models an application command-line flag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandLineFlagAttribute : Attribute
    {
        /// <summary>
        ///     Creates a new command-line flag attribute
        /// </summary>
        /// <param name="name">The name of the command-line flag.</param>
        /// <param name="helpText">A detailed description of flag.</param>
        /// <param name="usageExample">An example of how to use the attributed flag.</param>
        /// <param name="isGeneratorFlag">Indicates if a flag is used to generate code.</param>
        public CommandLineFlagAttribute(string name,
            string helpText,
            string usageExample = "",
            bool isGeneratorFlag = false,
            bool valuesRequired = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (string.IsNullOrWhiteSpace(helpText))
            {
                throw new ArgumentNullException(nameof(helpText));
            }
            Name = name;
            HelpText = helpText;
            UsageExample = usageExample;
            IsGeneratorFlag = isGeneratorFlag;
            ValuesRequired = valuesRequired;
        }


        /// <summary>
        ///     The name command-line flag. This name is usually a single english word.
        /// </summary>
        /// <remarks>
        ///     For compound words you should use a hyphen separator rather than camel casing.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        ///     A detailed description of the command-line flag.
        /// </summary>
        public string HelpText { get; }

        /// <summary>
        ///     If any an example of the parameter that is used in conjunction with the flag
        /// </summary>
        public string UsageExample { get; }

        /// <summary>
        ///     If this property is set to true the attributed command-line flag is used to instantiate a code generator.
        /// </summary>
        public bool IsGeneratorFlag { get; }

        /// <summary>
        ///     If this property is true, the flag requires values (arguments) to be set.
        /// </summary>
        public bool ValuesRequired { get;  }
    }

    #endregion
    /// <summary>
    /// Static extension methods for the command-line flag types.
    /// </summary>
    public static class CommandLineExtensions
    {
        /// <summary>
        /// Determines if the provided <paramref name="flags"/> contains the specified <paramref name="flagName"/>
        /// </summary>
        /// <param name="flags">The collection of flags to check.</param>
        /// <param name="flagName">The name of the flag to look for.</param>
        /// <returns>true if the flag is present, otherwise false.</returns>
        public static bool HasFlag(this List<CommandLineFlag> flags, string flagName)
        {
            return flags.Any(f => f.Name.Equals(flagName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds the flag associated with the specified <paramref name="flagName"/>
        /// </summary>
        /// <param name="flags">The collection of flags to check.</param>
        /// <param name="flagName">The name of the flag to look for.</param>
        /// <returns>An instance of the desired flag.</returns>
        public static CommandLineFlag GetFlag(this List<CommandLineFlag> flags, string flagName)
        {
            return flags.Find(f => f.Name.Equals(flagName, StringComparison.OrdinalIgnoreCase))!;
        }

    }
    /// <summary>
    /// Represents a parsed command-line flag and its values.
    /// </summary>
    public class CommandLineFlag
    {
        /// <summary>
        /// Creates a new command-line flag
        /// </summary>
        /// <param name="name">The name of the flag</param>
        /// <param name="values">The values (if any) corresponding to the flag.</param>
        public CommandLineFlag(string name, string[] values)
        {
            Name = name;
            Values = values;
        }
        /// <summary>
        /// Indicates if the current flag has any values assigned to it.
        /// </summary>
        /// <returns>true if the flag has values, otherwise false.</returns>
        public bool HasValues() => Values is { Length: > 0 };

        /// <summary>
        /// Returns the first value associated with the current flag.
        /// </summary>
        /// <returns></returns>
        public string? GetValue()
        {
            return Values.FirstOrDefault()?.Trim();
        }

        /// <summary>
        /// A collection of all values associated with the current flag.
        /// </summary>
        public string[] Values { get; init; }
        /// <summary>
        /// The name of the current flag.
        /// </summary>
        public string Name { get; init; }
    }

    /// <summary>
    ///     A class for constructing and parsing all available commands.
    /// </summary>
    public class CommandLineFlags
    {
        /// <summary>
        ///     The name of the config file used by bebopc.
        /// </summary>
        private const string ConfigFileName = "bebop.json";

        [CommandLineFlag("config", "Initializes the compiler from the specified configuration file.",
            "--config bebop.json")]
        public string? ConfigFile { get; private set; }

        [CommandLineFlag("cpp", "Generate C++ source code to the specified file",
            "--cpp ./my/output/HelloWorld.hpp", true)]
        public string? CPlusPlusOutput { get; private set; }
        
        [CommandLineFlag("cs", "Generate C# source code to the specified file", "--cs ./my/output/HelloWorld.cs",
            true)]
        public string? CSharpOutput { get; private set; }
        
        [CommandLineFlag("dart", "Generate Dart source code to the specified file",
            "--dart ./my/output/HelloWorld.dart", true)]
        public string? DartOutput { get; private set; }

        [CommandLineFlag("rust", "Generate Rust source code to the specified file", "--rust ./my/output/HelloWorld.rs",
            true)]
        public string? RustOutput { get; private set; }
        
        [CommandLineFlag("ts", "Generate TypeScript source code to the specified file",
            "--ts ./my/output/HelloWorld.ts", true)]
        public string? TypeScriptOutput { get; private set; }

        [CommandLineFlag("namespace", "When this option is specified generated code will use namespaces",
            "--cs --namespace [package]")]
        public string? Namespace { get; private set; }

        [CommandLineFlag("dir", "Parse and generate code from a directory of schemas", "--ts --dir [input dir]")]
        public string? SchemaDirectory { get; private set; }

        [CommandLineFlag("files", "Parse and generate code from a list of schemas", "--files [file1] [file2] ...")]
        public List<string>? SchemaFiles { get; private set; }

        [CommandLineFlag("check", "Checks that the provided schema files are valid, or entire project defined by bebop.json if no files provided", "--check [file.bop] [file2.bop] ...", false, false)]
        public List<string>? CheckSchemaFiles { get; private set; }

        [CommandLineFlag("check-schema", "Reads a schema from stdin and validates it.", "--check-schema < [schema text]")]
        public string? CheckSchemaFile { get; private set; }

        /// <summary>
        ///     When set to true the process will output the product version and exit with a zero return code.
        /// </summary>
        [CommandLineFlag("version", "Show version info and exit.", "--version")]
        public bool Version { get; private set; }

        /// <summary>
        ///     When set to true the process will output the <see cref="HelpText"/> and exit with a zero return code.
        /// </summary>
        [CommandLineFlag("help", "Show this text and exit.", "--help")]
        public bool Help { get; private set; }

        /// <summary>
        ///     Controls how loggers format data.
        /// </summary>
        [CommandLineFlag("log-format", "Defines the formatter that will be used with logging.",
            "--log-format (structured|msbuild|json)")]
        public LogFormatter LogFormatter { get; private set; }

        /// <summary>
        ///     An optional flag to set the version of a language a code generator will use. 
        /// </summary>
        [CommandLineFlag("cs-version", "Defines the C# language version the C# generator will target.",
            "--cs ./my/output/HelloWorld.cs --cs-version (9.0|8.0)")]
        public Version? CSharpVersion { get; private set; }

        public string HelpText { get; }

        /// <summary>
        /// Finds a language version flag set for a generator.
        /// </summary>
        private Version? GetGeneratorVersion(CommandLineFlagAttribute attribute)
        {
            foreach (var flag in GetFlagAttributes())
            {
                if ($"{attribute.Name}-version".Equals(flag.Attribute.Name, StringComparison.OrdinalIgnoreCase) && flag.Property.GetValue(this) is Version value)
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        ///     Returns the alias and output file of all command-line specified code generators.
        /// </summary>
        public IEnumerable<CodeGenerator> GetParsedGenerators()
        {
            foreach (var flag in GetFlagAttributes())
            {
                if (flag.Attribute.IsGeneratorFlag && flag.Property.GetValue(this) is string value)
                {
                    yield return new CodeGenerator(flag.Attribute.Name, value, GetGeneratorVersion(flag.Attribute));
                }
            }
        }

        #region Static

        /// <summary>
        ///     Walks all properties in <see cref="CommandLineFlags"/> and maps them to their assigned
        ///     <see cref="CommandLineFlagAttribute"/>
        /// </summary>
        /// <returns></returns>
        private static List<(PropertyInfo Property, CommandLineFlagAttribute Attribute)> GetFlagAttributes()
        {
            return (from p in typeof(CommandLineFlags).GetProperties()
                    let attr = p.GetCustomAttributes(typeof(CommandLineFlagAttribute), true)
                    where attr.Length == 1
                    select (p, attr.First() as CommandLineFlagAttribute))
                .Select(t => ((PropertyInfo, CommandLineFlagAttribute))t)
                .ToList();
        }

        /// <summary>
        ///     Hide the constructor to prevent direct initializationW
        /// </summary>
        private CommandLineFlags(string helpText)
        {
            HelpText = helpText;
        }

        /// <summary>
        ///     Searches recursively upward to locate the config file belonging to <see cref="ConfigFileName"/>.
        /// </summary>
        /// <returns>The fully qualified path to the config file, or null if not found.</returns>
        public static string? FindBebopConfig()
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

        #endregion

        #region Parsing

        /// <summary>
        ///     Parses an array of command-line flags into dictionary.
        /// </summary>
        /// <param name="args">The flags to be parsed.</param>
        /// <returns>A dictionary containing all parsed flags and their value if any.</returns>
        private static List<CommandLineFlag> GetFlags(string[] args)
        {
            var flags = new List<CommandLineFlag>();
            foreach (var token in args)
            {
                if (token.StartsWith("--"))
                {
                    var key = new string(token.SkipWhile(c => c == '-').ToArray()).ToLowerInvariant();
                    var value = args.SkipWhile(i => i != $"--{key}").Skip(1).TakeWhile(i => !i.StartsWith("--")).ToArray();

                    flags.Add(new CommandLineFlag(key, value));
                }
            }
            return flags;
        }

        /// <summary>
        ///     Attempts to find the <see cref="LogFormatter"/> flag and parse its value.
        /// </summary>
        /// <param name="args">The command-line arguments to sort through</param>
        /// <returns>
        ///     If the <see cref="LogFormatter"/> flag was present an had a valid value, that enum member will be returned.
        ///     Otherwise the default formatter is used.
        /// </returns>
        public static LogFormatter FindLogFormatter(string[] args)
        {
            var flags = GetFlags(args);
            foreach (var flag in flags)
            {
                if (flag.Name.Equals("log-format", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<LogFormatter>(flag.GetValue(), true, out var parsedEnum))
                {
                    return parsedEnum;
                }
            }
            return LogFormatter.Structured;
        }


        /// <summary>
        ///     Attempts to parse command-line flags into a <see cref="CommandLineFlags"/> instance
        /// </summary>
        /// <param name="args">the array of arguments to parse</param>
        /// <param name="flagStore">An instance which contains all parsed flags and their values</param>
        /// <param name="errorMessage">A human-friendly message describing why parsing failed.</param>
        /// <returns>
        ///     If the provided
        ///     <param name="args"></param>
        ///     were parsed this method returns true.
        /// </returns>
        public static bool TryParse(string[] args, out CommandLineFlags flagStore, out string errorMessage)
        {
            errorMessage = string.Empty;
            var props = GetFlagAttributes();

            var stringBuilder = new IndentedStringBuilder();

            stringBuilder.AppendLine("Usage:");
            stringBuilder.Indent(4);
            foreach (var prop in props.Where(prop => !string.IsNullOrWhiteSpace(prop.Attribute.UsageExample)))
            {
                stringBuilder.AppendLine($"{ReservedWords.CompilerName} {prop.Attribute.UsageExample}");
            }
            stringBuilder.Dedent(4);

            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine("Options:");
            stringBuilder.Indent(4);
            foreach (var prop in props)
            {
                stringBuilder.AppendLine($"--{prop.Attribute.Name}  {prop.Attribute.HelpText}");
            }

            flagStore = new CommandLineFlags(stringBuilder.ToString());

            var parsedFlags = GetFlags(args);

            if (parsedFlags.Count == 0)
            {
                // if no flags passed in try and find the bebop.json config
                if (TryParseConfig(flagStore, FindBebopConfig()))
                {
                    return true;
                }
                errorMessage = "No command-line flags found.";
                return false;
            }

            if (parsedFlags.HasFlag("help"))
            {
                flagStore.Help = true;
                return true;
            }

            if (parsedFlags.HasFlag("version"))
            {
                flagStore.Version = true;
                return true;
            }

            // if the config flag is passed in load settings from that path, otherwise search for it.
            var bebopConfig = parsedFlags.HasFlag("config")
                ? parsedFlags.GetFlag("config").GetValue()
                : FindBebopConfig();
            // if bebop.json exist load it. the values in the JSON file are written to the store.
            if (!string.IsNullOrWhiteSpace(bebopConfig))
            {
                if (new FileInfo(bebopConfig).Exists)
                {
                    if (!TryParseConfig(flagStore, bebopConfig))
                    {
                        errorMessage = $"Failed to parse bebop configuration file at '{bebopConfig}'";
                        return false;
                    }
                }
                else
                {
                    errorMessage = $"Bebop configuration file not found at '{bebopConfig}'";
                    return false;
                }
            }

            var validFlagNames = props.Select(p => p.Attribute.Name).ToHashSet();
            if (parsedFlags.Find(x => !validFlagNames.Contains(x.Name)) is CommandLineFlag unrecognizedFlag)
            {
                errorMessage = $"Unrecognized flag: --{unrecognizedFlag.Name}";
                return false;
            }

            // parse all present command-line flags
            // any flag on the command-line that was also present in bebop.json will be overwritten.
            foreach (var flag in props)
            {
                if (!parsedFlags.HasFlag(flag.Attribute.Name))
                {
                    continue;
                }

                var parsedFlag = parsedFlags.GetFlag(flag.Attribute.Name);
                var propertyType = flag.Property.PropertyType;
                if (flag.Attribute.Name.Equals("check-schema"))
                {
                    using var reader = new StreamReader(Console.OpenStandardInput());
                    flagStore.CheckSchemaFile = reader.ReadToEnd();
                    continue;
                }
                if (propertyType == typeof(bool))
                {
                    flag.Property.SetValue(flagStore, true);
                    continue;
                }
                if (flag.Attribute.ValuesRequired && !parsedFlag.HasValues())
                {
                    errorMessage = $"command-line flag '{flag.Attribute.Name}' was not assigned any values.";
                    return false;
                }
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type itemType = propertyType.GetGenericArguments()[0];
                    if (!(Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType)) is IList genericList))
                    {
                        errorMessage = $"Failed to activate '{flag.Property.Name}'.";
                        return false;
                    }

                    // file paths wrapped in quotes may contain spaces. 
                    foreach (var item in parsedFlag.Values)
                    {
                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }
                        // remove double quotes from the string so file paths can be parsed properly.
                        genericList.Add(Convert.ChangeType(item.Trim(), itemType));
                    }
                    flag.Property.SetValue(flagStore, genericList, null);
                }
                else if (propertyType.IsEnum)
                {
                    if (!Enum.TryParse(propertyType, parsedFlag.GetValue(), true, out var parsedEnum))
                    {
                        errorMessage = $"Failed to parse '{parsedFlag.GetValue()}' into a member of '{propertyType}'.";
                        return false;
                    }
                    flag.Property.SetValue(flagStore, parsedEnum, null);
                }
                else if (propertyType == typeof(Version))
                {
                    if (System.Version.TryParse(parsedFlag.GetValue(), out var version) && version is Version)
                    {
                        flag.Property.SetValue(flagStore, version, null);
                    }
                }
                else
                {
                    flag.Property.SetValue(flagStore, Convert.ChangeType(parsedFlag.GetValue(), flag.Property.PropertyType),
                        null);
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        ///     Parses the bebop config file and assigns entries to their corresponding command-line flag.
        /// </summary>
        /// <param name="flagStore">A <see cref="CommandLineFlags"/> instance.</param>
        /// <param name="configPath">The fully qualified path to the bebop config file, or null to trigger searching.</param>
        /// <returns>true if the config could be parsed without error, otherwise false.</returns>
        private static bool TryParseConfig(CommandLineFlags flagStore, string? configPath)
        {
            
            if (string.IsNullOrWhiteSpace(configPath))
            {
                return false;
            }
            if (!new FileInfo(configPath).Exists)
            {
                return false;
            }
            var configFile = new FileInfo(configPath);

            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = doc.RootElement;
            if (root.TryGetProperty("inputFiles", out var inputFileElement))
            {
                flagStore.SchemaFiles = new List<string>(inputFileElement.GetArrayLength());
                foreach (var fileElement in inputFileElement.EnumerateArray())
                {
                    if (fileElement.GetString() is not { } filePath || configFile.DirectoryName is null)
                    {
                        continue;
                    }
                    flagStore.SchemaFiles.Add(Path.GetFullPath(Path.Combine(configFile.DirectoryName, filePath)));
                }
            }
            if (root.TryGetProperty("inputDirectory", out var inputDirectoryElement) && configFile.DirectoryName is not null)
            {
                var inputDirectory = inputDirectoryElement.GetString();
                if (inputDirectory is not null)
                {
                    flagStore.SchemaDirectory = Path.GetFullPath(Path.Combine(configFile.DirectoryName, inputDirectory));
                }
            }
            if (root.TryGetProperty("namespace", out var nameSpaceElement))
            {
                flagStore.Namespace = nameSpaceElement.GetString();
            }
            if (root.TryGetProperty("generators", out var generatorsElement))
            {
                foreach (var generatorElement in generatorsElement.EnumerateArray())
                {
                    if (generatorElement.TryGetProperty("alias", out var aliasElement) &&
                        generatorElement.TryGetProperty("outputFile", out var outputElement))
                    {
                        foreach (var flagAttribute in GetFlagAttributes()
                            .Where(flagAttribute => flagAttribute.Attribute.IsGeneratorFlag &&
                                flagAttribute.Attribute.Name.Equals(aliasElement.GetString())))
                        {
                            var outputElementPath = outputElement.GetString();
                            if (configFile.DirectoryName is not null && outputElementPath is not null)
                            {
                                flagAttribute.Property.SetValue(flagStore, Path.GetFullPath(Path.Combine(configFile.DirectoryName, outputElementPath)));
                            }
                            if (generatorElement.TryGetProperty("langVersion", out var langVersion) && System.Version.TryParse(langVersion.ToString(), out var version))
                            {

                                foreach (var flag in GetFlagAttributes())
                                {
                                    if ($"{flagAttribute.Attribute.Name}-version".Equals(flag.Attribute.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        flag.Property.SetValue(flagStore, version, null);
                                    }
                                }

                            }
                        }

                    }
                }
            }
            return true;
        }
    }

    #endregion
}