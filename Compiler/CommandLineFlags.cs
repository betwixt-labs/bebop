using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core.Generators;
using Core.Logging;
using Core.Meta;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Compiler
{
    #region Records
    /// <summary>
    /// Represents a code generator passed to bebopc.
    /// </summary>
    /// <param name="Alias">The alias of the code generator.</param>
    /// <param name="OutputFile">The quailified file that the generator will produce.</param>
    /// <param name="Services">The control for which service compontents will be generated.</param>
    /// <param name="LangVersion">If set this value defines the version of the language generated code will use.</param>
    public record CodeGenerator(string Alias, string OutputFile, TempoServices Services, Version? LangVersion);
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
            bool valuesRequired = true,
            bool hideFromHelp = false)
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
            HideFromHelp = hideFromHelp;
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
        public bool ValuesRequired { get; }

        /// <summary>
        ///     If this property is true, the command-line flag will be hidden from help output.
        /// </summary>
        public bool HideFromHelp { get; }
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

        [CommandLineFlag("py", "Generate Python source code to the specified file",
            "--py ./my/output/HelloWorld.py", true)]
        public string? PythonOutput { get; private set; }

        [CommandLineFlag("namespace", "When this option is specified generated code will use namespaces",
            "--cs --namespace [package]")]
        public string? Namespace { get; private set; }

        [CommandLineFlag("skip-generated-notice", "Flag to disable generating the file header announcing the file was autogenerated by bebop.", "--rust --skip-generation-notice", valuesRequired: false)]
        public bool SkipGeneratedNotice { get; private set; }

        [CommandLineFlag("emit-binary-schema", "Flag to enable experimental binary schemas in generated code.", "--ts --emit-binary-schema", valuesRequired: false)]
        public bool EmitBinarySchema { get; private set; }

        [CommandLineFlag("dir", "Parse and generate code from a directory of schemas", "--ts --dir [input dir]")]
        public string? SchemaDirectory { get; private set; }

        [CommandLineFlag("files", "Parse and generate code from a list of schemas", "--files [file1] [file2] ...")]
        public List<string>? SchemaFiles { get; private set; }

        [CommandLineFlag("check", "Checks that the provided schema files are valid, or entire project defined by bebop.json if no files provided", "--check [file.bop] [file2.bop] ...", false, false)]
        public List<string>? CheckSchemaFiles { get; private set; }

        [CommandLineFlag("check-schema", "Reads a schema from stdin and validates it.", "--check-schema < [schema text]")]
        public string? CheckSchemaFile { get; private set; }

        [CommandLineFlag("in", "Reads a schema from stdin.", "--in < [schema text]")]
        public string? StandardInput { get; private set; }

        [CommandLineFlag("out", "Writes a compiled schema to standard out.", "--out > [output file]")]
        public bool StandardOutput { get; private set; }

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

        [CommandLineFlag("langserv", "Starts the language server", "--langserv", hideFromHelp: true)]
        public bool LanguageServer { get; private set; }

        [CommandLineFlag("debug", "Waits for a debugger to attach", "--debug", hideFromHelp: true)]
        public bool Debug { get; private set; }

        [CommandLineFlag("watch", "Watches schemas for changes and regenerates code", "--watch")]
        public bool Watch { get; private set; }

        [CommandLineFlag("watch-excluded-directories", "Directories which willbe excluded from the watch process. Supports globs.", "--watch-excluded-directories [directory1] [directory2] ...")]
        public List<string>? WatchExcludeDirectories { get; private set; }

        [CommandLineFlag("watch-excluded-files", "Files which willbe excluded from the watch process. Supports globs.", "--watch-excluded-files [file1] [file2] ...")]
        public List<string>? WatchExcludeFiles { get; private set; }

        [CommandLineFlag("preserve-watch-output", "Whether to keep outdated console output in watch mode instead of clearing the screen every time a change happened.", "--watch --preserve-watch-output")]
        public bool PreserveWatchOutput { get; private set; }

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

        [CommandLineFlag("no-warn", "Disable a list of warning codes", "--no-warn 200 201 202")]
        public List<string>? NoWarn { get; private set; }

        [CommandLineFlag("quiet", "Suppresses all output including errors", "--quiet")]
        public bool Quiet { get; private set; }

        public string HelpText { get; }

        public string WorkingDirectory { get; private set; }

        private readonly Dictionary<string, TempoServices> ServiceConfigs = new Dictionary<string, TempoServices>()
        {
            ["cpp"] = TempoServices.Both,
            ["cs"] = TempoServices.Both,
            ["dart"] = TempoServices.Both,
            ["rust"] = TempoServices.Both,
            ["ts"] = TempoServices.Both,
            ["py"] = TempoServices.Both
        };

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
                    yield return new CodeGenerator(flag.Attribute.Name, value, ServiceConfigs[flag.Attribute.Name], GetGeneratorVersion(flag.Attribute));
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
            return LogFormatter.Enhanced;
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
            foreach (var prop in props.Where(p => !p.Attribute.HideFromHelp))
            {
                stringBuilder.AppendLine($"--{prop.Attribute.Name}  {prop.Attribute.HelpText}");
            }

            flagStore = new CommandLineFlags(stringBuilder.ToString());

            var parsedFlags = GetFlags(args);

            // prevent the user from passing both --langserv and --config
            // and also for the compiler trying to find a config file when running as a language server
            if (parsedFlags.HasFlag("langserv"))
            {
                flagStore.LanguageServer = true;
                return true;
            }


            string? configPath;
            if (parsedFlags.HasFlag("config"))
            {
                configPath = parsedFlags.GetFlag("config").GetValue();
                if (string.IsNullOrWhiteSpace(configPath))
                {
                    errorMessage = $"'--config' must be followed by the explicit path to a bebop.json config";
                    return false;
                }
            }
            else
            {
                configPath = BebopConfig.Locate();
            }

            var rootDirectory = !string.IsNullOrWhiteSpace(configPath) ? Path.GetDirectoryName(configPath) : Directory.GetCurrentDirectory();
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                errorMessage = "Failed to determine the working directory.";
                return false;
            }

            flagStore.WorkingDirectory = rootDirectory;


            var parsedConfig = false;
            // always parse the config, we'll override any values with command-line flags
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                if (!new FileInfo(configPath).Exists)
                {
                    errorMessage = $"Bebop configuration file not found at '{configPath}'";
                    return false;
                }
                parsedConfig = TryParseConfig(flagStore, configPath);
                if (!parsedConfig)
                {
                    errorMessage = $"Failed to parse Bebop configuration file at '{configPath}'";
                    return false;
                }
            }

            // if we didn't parse a 'bebop.json' and no flags were passed, we can't continue.
            if (!parsedConfig && parsedFlags.Count == 0)
            {
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
                 if (flag.Attribute.Name.Equals("in"))
                {
                    using var reader = new StreamReader(Console.OpenStandardInput());
                    flagStore.StandardInput = reader.ReadToEnd();
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

                    if (flag.Attribute.Name.Equals("watch-excluded-directories") || flag.Attribute.Name.Equals("watch-excluded-files"))
                    {
                        var excluded = FindFiles(rootDirectory, parsedFlag.Values, Array.Empty<string>());
                        flag.Property.SetValue(flagStore, excluded, null);
                    }
                    else
                    {
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


        private static List<string> FindFiles(string rootDirectory, string[] includes, string[] excludes)
        {
            var matcher = new Matcher();
            matcher.AddIncludePatterns(includes);
            matcher.AddExcludePatterns(excludes);
            IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(rootDirectory);
            return matchingFiles.ToList();
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
            var configFile = new FileInfo(configPath);
            if (!configFile.Exists)
            {
                return false;
            }
            var configDirectory = configFile.DirectoryName;
            if (string.IsNullOrWhiteSpace(configDirectory))
            {
                return false;
            }

            var bebopConfig = BebopConfig.FromJson(File.ReadAllText(configPath));
            if (bebopConfig is null)
            {
                return false;
            }

            const string defaultIncludeGlob = "**/*.bop";
            var includeGlob = bebopConfig.Include ?? new string[] { defaultIncludeGlob };
            var excludeGlob = bebopConfig.Exclude ?? Array.Empty<string>();
            flagStore.SchemaFiles = FindFiles(configDirectory, includeGlob, excludeGlob);
            flagStore.Namespace = bebopConfig.Namespace;
            if (bebopConfig.Generators is null)
            {
                return false;
            }
            foreach (var generator in bebopConfig.Generators)
            {
                if (generator.Alias is null || generator.OutFile is null)
                {
                    return false;
                }
                foreach (var flagAttribute in GetFlagAttributes()
                            .Where(flagAttribute => flagAttribute.Attribute.IsGeneratorFlag &&
                                flagAttribute.Attribute.Name.Equals(generator.Alias)))
                {
                    flagAttribute.Property.SetValue(flagStore, Path.GetFullPath(Path.Combine(configDirectory, generator.OutFile)));
                    if (generator.Services.HasValue)
                    {
                        flagStore.ServiceConfigs[generator.Alias] = generator.Services.Value;
                    }
                    else
                    {
                        flagStore.ServiceConfigs[generator.Alias] = TempoServices.Both;
                    }
                    if (generator.LangVersion is not null && System.Version.TryParse(generator.LangVersion, out var version))
                    {
                        foreach (var flag in GetFlagAttributes())
                        {
                            if ($"{flagAttribute.Attribute.Name}-version".Equals(flag.Attribute.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                flag.Property.SetValue(flagStore, version, null);
                            }
                        }
                    }
                    if (generator.NoGenerationNotice is not null && generator.NoGenerationNotice.Value is true) {
                        flagStore.SkipGeneratedNotice = true;
                    }
                    if (generator.EmitBinarySchema is not null && generator.EmitBinarySchema is true) {
                        flagStore.EmitBinarySchema = true;
                    }
                }
            }

            if (bebopConfig.WatchOptions is not null)
            {
                if (bebopConfig.WatchOptions.ExcludeDirectories is not null)
                {
                    flagStore.WatchExcludeDirectories = bebopConfig.WatchOptions.ExcludeDirectories.ToList();
                }
                if (bebopConfig.WatchOptions.ExcludeFiles is not null)
                {
                    flagStore.WatchExcludeFiles = bebopConfig.WatchOptions.ExcludeFiles.ToList();
                }
            }
            return true;
        }
    }

    #endregion
}