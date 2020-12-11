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
#region FlagAttribute

    /// <summary>
    ///     Models an application commandline flag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandLineFlagAttribute : Attribute
    {
        /// <summary>
        ///     Creates a new commandline flag attribute
        /// </summary>
        /// <param name="name">The name of the commandline flag.</param>
        /// <param name="helpText">A detailed description of flag.</param>
        /// <param name="usageExample">An example of how to use the attributed flag.</param>
        /// <param name="isGeneratorFlag">Indicates if a flag is used to generate code.</param>
        public CommandLineFlagAttribute(string name,
            string helpText,
            string usageExample = "",
            bool isGeneratorFlag = false)
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
        }

        /// <summary>
        ///     The name commandline flag. This name is usually a single english word.
        /// </summary>
        /// <remarks>
        ///     For compound words you should use a hyphen separator rather than camel casing.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        ///     A detailed description of the commandline flag.
        /// </summary>
        public string HelpText { get; }

        /// <summary>
        ///     If any an example of the parameter that is used in conjunction with the flag
        /// </summary>
        public string UsageExample { get; }

        /// <summary>
        ///     If this property is set to true the attributed commandline flag is used to instantiate a code generator.
        /// </summary>
        public bool IsGeneratorFlag { get; }
    }

#endregion

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

        [CommandLineFlag("cs", "Generate C# source code to the specified file", "--cs ./cowboy/bebop/HelloWorld.cs",
            true)]
        public string? CSharpOutput { get; private set; }

        [CommandLineFlag("ts", "Generate TypeScript source code to the specified file",
            "--ts ./cowboy/bebop/HelloWorld.ts", true)]
        public string? TypeScriptOutput { get; private set; }

        [CommandLineFlag("dart", "Generate Dart source code to the specified file",
            "--dart ./cowboy/bebop/HelloWorld.dart", true)]
        public string? DartOutput { get; private set; }

        [CommandLineFlag("namespace", "When this option is specified generated code will use namespaces",
            "--lang cs --namespace [package]")]
        public string? Namespace { get; private set; }

        [CommandLineFlag("dir", "Parse and generate code from a directory of schemas", "--lang ts --dir [input dir]")]
        public string? SchemaDirectory { get; private set; }

        [CommandLineFlag("files", "Parse and generate code from a list of schemas", "--files [file1] [file2] ...")]
        public List<string>? SchemaFiles { get; private set; }

        [CommandLineFlag("check", "Checks that the provided schema files are valid", "--check [file.bop] [file2.bop] ...")]
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

        public string HelpText { get; }

        /// <summary>
        ///     Returns the alias and output file of all commandline specified code generators.
        /// </summary>
        public IEnumerable<(string Alias, string OutputFile)> GetParsedGenerators()
        {
            foreach (var flag in GetFlagAttributes())
            {
                if (flag.Attribute.IsGeneratorFlag && flag.Property.GetValue(this) is string value)
                {
                    yield return (flag.Attribute.Name, value);
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
                .Select(t => ((PropertyInfo, CommandLineFlagAttribute)) t)
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
                if (Directory.GetParent(workingDirectory) is not {Exists: true} parent)
                {
                    break;
                }
                workingDirectory = parent.FullName;
                if (parent.GetFiles(ConfigFileName)?.FirstOrDefault() is {Exists: true} file)
                {
                    configFile = file.FullName;
                }
            }
            return configFile;
        }

    #endregion

    #region Parsing

        /// <summary>
        /// Removes double quotes from the start and end of a string.
        /// </summary>
        /// <param name="input">the string to alter</param>
        /// <returns>the string without double quotes.</returns>
        private static string TrimDoubleQuotes(string input)
        {
            return input.Trim('"');
        }

        /// <summary>
        ///     Parses an array of commandline flags into dictionary.
        /// </summary>
        /// <param name="args">The flags to be parsed.</param>
        /// <returns>A dictionary containing all parsed flags and their value if any.</returns>
        private static Dictionary<string, string> GetFlags(string[] args)
        {
            var arguments = new Dictionary<string, string>();
            foreach (var token in args)
            {
                if (token.StartsWith("--"))
                {
                    var key = new string(token.SkipWhile(c => c == '-').ToArray()).ToLowerInvariant();
                    var value = string.Join(" ",
                        args.SkipWhile(i => i != $"--{key}").Skip(1).TakeWhile(i => !i.StartsWith("--")));
                    arguments.Add(key, value);
                }
            }
            return arguments;
        }

        /// <summary>
        ///     Attempts to find the <see cref="LogFormatter"/> flag and parse it's value.
        /// </summary>
        /// <param name="args">The commandline arguments to sort through</param>
        /// <returns>
        ///     If the <see cref="LogFormatter"/> flag was present an had a valid value, that enum member will be returned.
        ///     Otherwise the default formatter is used.
        /// </returns>
        public static LogFormatter FindLogFormatter(string[] args)
        {
            var flags = GetFlags(args);
            foreach (var flag in flags)
            {
                if (flag.Key.Equals("log-format", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<LogFormatter>(flag.Value, true, out var parsedEnum))
                {
                    return parsedEnum;
                }
            }
            return LogFormatter.Structured;
        }


        /// <summary>
        ///     Attempts to parse commandline flags into a <see cref="CommandLineFlags"/> instance
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
                if (TryParseConfig(flagStore))
                {
                    return true;
                }
                errorMessage = "No commandline flags found.";
                return false;
            }

            if (parsedFlags.ContainsKey("config"))
            {
                var configPath = parsedFlags["config"];
                if (string.IsNullOrWhiteSpace(configPath))
                {
                    configPath = null;
                }
                return TryParseConfig(flagStore, configPath);
            }

            if (parsedFlags.ContainsKey("help"))
            {
                flagStore.Help = true;
                return true;
            }

            if (parsedFlags.ContainsKey("version"))
            {
                flagStore.Version = true;
                return true;
            }

            foreach (var flag in props)
            {
                if (!parsedFlags.ContainsKey(flag.Attribute.Name))
                {
                    continue;
                }

                var parsedValue = parsedFlags[flag.Attribute.Name]?.Trim();
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
                if (string.IsNullOrWhiteSpace(parsedValue))
                {
                    errorMessage = $"Commandline flag '{flag.Attribute.Name}' was not assigned a value.";
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
                    foreach (var item in Regex.Matches(parsedValue, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value))
                    {
                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }
                        // remove double quotes from the string so file paths can be parsed properly.
                        genericList.Add(Convert.ChangeType(TrimDoubleQuotes(item).Trim(), itemType));
                    }
                    flag.Property.SetValue(flagStore, genericList, null);
                }
                else if (propertyType.IsEnum)
                {
                    if (!Enum.TryParse(propertyType, parsedValue, true, out var parsedEnum))
                    {
                        errorMessage = $"Failed to parse '{parsedValue}' into a member of '{propertyType}'.";
                        return false;
                    }
                    flag.Property.SetValue(flagStore, parsedEnum, null);
                }
                else if (propertyType == typeof(string))
                {
                    flag.Property.SetValue(flagStore, Convert.ChangeType(TrimDoubleQuotes(parsedValue), flag.Property.PropertyType),
                        null);
                }
                else
                {
                    flag.Property.SetValue(flagStore, Convert.ChangeType(parsedValue, flag.Property.PropertyType),
                        null);
                }
            }
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        ///     Parses the bebop config file and assigns entries to their corresponding commandline flag.
        /// </summary>
        /// <param name="flagStore">A <see cref="CommandLineFlags"/> instance.</param>
        /// <param name="configPath">The fully qualified path to the bebop config file, or null to trigger searching.</param>
        /// <returns>true if the config could be parsed without error, otherwise false.</returns>
        private static bool TryParseConfig(CommandLineFlags flagStore, string? configPath = null)
        {
            configPath ??= FindBebopConfig();
            if (string.IsNullOrWhiteSpace(configPath))
            {
                return false;
            }
            if (!new FileInfo(configPath).Exists)
            {
                return false;
            }
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = doc.RootElement;
            if (root.TryGetProperty("inputFiles", out var inputFileElement))
            {
                flagStore.SchemaFiles = new List<string>(inputFileElement.GetArrayLength());
                foreach (var fileElement in inputFileElement.EnumerateArray())
                {
                    if (fileElement.GetString() is not { } filePath)
                    {
                        continue;
                    }
                    flagStore.SchemaFiles.Add(filePath);
                }
            }
            if (root.TryGetProperty("inputDirectory", out var inputDirectoryElement))
            {
                flagStore.SchemaDirectory = inputDirectoryElement.GetString();
            }
            if (root.TryGetProperty("generators", out var generatorsElement))
            {
                foreach (var generatorElement in generatorsElement.EnumerateArray())
                {
                    if (generatorElement.TryGetProperty("alias", out var aliasElement) &&
                        generatorElement.TryGetProperty("destinationPath", out var destinationElement))
                    {
                        foreach (var flagAttribute in GetFlagAttributes()
                            .Where(flagAttribute => flagAttribute.Attribute.IsGeneratorFlag &&
                                flagAttribute.Attribute.Name.Equals(aliasElement.GetString())))
                        {
                            flagAttribute.Property.SetValue(flagStore, destinationElement.GetString());
                        }
                    }
                }
            }
            return true;
        }
    }

#endregion
}