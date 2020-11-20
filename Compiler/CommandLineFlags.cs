using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Generators;
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
        /// <param name="usageExample"></param>
        public CommandLineFlagAttribute(string name, string helpText, string usageExample = "")
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
    }

#endregion

    /// <summary>
    /// </summary>
    public class CommandLineFlags
    {
        [CommandLineFlag("lang", "Generate source file for a given language", "--lang (ts|cs|dart)")]
        public string? Language { get; private set; }

        [CommandLineFlag("namespace", "When this option is specified generated code will use namespaces", "--lang cs --namespace [package]")]
        public string? Namespace { get; private set; }

        [CommandLineFlag("dir", "Parse and generate code from a directory of schemas", "--lang ts --dir [input dir]")]
        public string? SchemaDirectory { get; private set; }

        [CommandLineFlag("files", "Parse and generate code from a list of schemas", "--files [file1] [file2] ...")]
        public List<string>? SchemaFiles { get; private set; }

        [CommandLineFlag("out", "The file generated code will be written to", "--lang cs --dir [input dir] --out [output file]")]
        public string? OutputFile { get; private set; }

        [CommandLineFlag("check", "Only check a given schema is valid", "--check [file.bop] [file2.bop] ...")]
        public List<string>? CheckSchemaFiles { get; private set; }

        /// <summary>
        /// When set to true the process will output the product version and exit with a zero return code.
        /// </summary>
        [CommandLineFlag("version", "Show version info and exit.", "")]
        public bool Version { get; private set; }

        /// <summary>
        /// When set to true the process will output the <see cref="HelpText"/> and exit with a zero return code.
        /// </summary>
        [CommandLineFlag("help", "Show this text and exit.", "")]
        public bool Help { get; private set; }

        public string? HelpText { get; private init; }

        #region Static


        /// <summary>
        ///     Hide the constructor to prevent direct initialization
        /// </summary>
        private CommandLineFlags()
        {
        }

    #endregion

    #region Parsing

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
                    var value = string.Join(" ", args.SkipWhile(i => i != $"--{key}").Skip(1).TakeWhile(i => !i.StartsWith("--")));
                    arguments.Add(key, value);
                }
            }
            return arguments;
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
            var props = (from p in typeof(CommandLineFlags).GetProperties()
                let attr = p.GetCustomAttributes(typeof(CommandLineFlagAttribute), true)
                where attr.Length == 1
                select new {Property = p, Attribute = attr.First() as CommandLineFlagAttribute}).ToList();

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

            flagStore = new CommandLineFlags {HelpText = stringBuilder.ToString()};

            var parsedFlags = GetFlags(args);
            if (parsedFlags.Count == 0)
            {
                errorMessage = "No commandline flags found.";
                return false;
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
                if (propertyType == typeof(bool))
                {
                    flag.Property.SetValue(flagStore, true);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(parsedValue))
                {
                    errorMessage = $"Commandline flag \"{flag.Attribute.Name}\" was not assigned a value.";
                    return false;
                }
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type itemType = propertyType.GetGenericArguments()[0];
                    if (!(Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType)) is IList genericList))
                    {
                        errorMessage = $"Failed to activate \"{flag.Property.Name}\".";
                        return false;
                    }
                    foreach (var item in parsedValue.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        genericList.Add(Convert.ChangeType(item.Trim(), itemType));
                    }
                    flag.Property.SetValue(flagStore, genericList, null);
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
    }

#endregion
}
