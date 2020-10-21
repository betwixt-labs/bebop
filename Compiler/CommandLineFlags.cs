using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Compiler.Generators;

namespace Compiler
{
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
        /// ///
        /// <param name="isRequired">Whether or not the flag is required.</param>
        /// <param name="helpText">A detailed description of flag.</param>
        /// <param name="usageExample"></param>
        public CommandLineFlagAttribute(string name, bool isRequired, string helpText, string usageExample = "")
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
            IsRequired = isRequired;
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
        ///     Determines if the flag is required to run the application.
        /// </summary>
        public bool IsRequired { get; }
    }

    /// <summary>
    /// </summary>
    public class CommandLineFlags
    {
        /// <summary>
        ///     Used for generating usage text.
        /// </summary>
        private const string ProcessName = "pierogic";

        /// <summary>
        ///     Hide the constructor to prevent direct initialization
        /// </summary>
        private CommandLineFlags()
        {
        }


        [CommandLineFlag("server", false, "I'm a flag", "--server 8080")]
        public int ServerPort { get; private set; }

        [CommandLineFlag("fake", false, "I am also a flag", "--fake dogma")]
        public string Fake { get; private set; }

        [CommandLineFlag("files", false, "Testerino", "--files [file1, file 2]")]
        public List<string> Files { get; private set; }

        public string HelpText { get; private init; }

        /// <summary>
        ///     Parses an array of commandline flags into dictionary.
        /// </summary>
        /// <param name="args">The flags to be parsed.</param>
        /// <returns>A dictionary containing all parsed flags and their value if any.</returns>
        private static Dictionary<string, string> GetFlags(string[] args) => args
            .Zip(args.Skip(1).Concat(new[] {string.Empty}), (first, second) => new {first, second})
            .Where(pair => pair.first.StartsWith("-", StringComparison.Ordinal))
            .ToDictionary(pair => new string(pair.first.SkipWhile(c => c == '-').ToArray()).ToLowerInvariant(),
                g => g.second.StartsWith("-", StringComparison.Ordinal) ? string.Empty : g.second);


        /// <summary>
        ///     Attempts to parse commandline flags into a <see cref="CommandLineFlags"/> instance
        /// </summary>
        /// <param name="args">the array of arguments to parse</param>
        /// <param name="flagStore">An instance which contains all parsed flags and their values</param>
        /// <param name="errorMessage">A human-friendly message describing why parsing failed.</param>
        /// <returns>If the provided <param name="args"></param> were parsed this method returns true.</returns>
        public static bool TryParse(string[] args, out CommandLineFlags flagStore, out string errorMessage)
        {
            var props = (from p in typeof(CommandLineFlags).GetProperties()
                let attr = p.GetCustomAttributes(typeof(CommandLineFlagAttribute), true)
                where attr.Length == 1
                select new {Property = p, Attribute = attr.First() as CommandLineFlagAttribute}).ToList();

            var stringBuilder = new IndentedStringBuilder();

            stringBuilder.AppendLine("Usage:");
            stringBuilder.Indent(4);
            foreach (var prop in props)
            {
                stringBuilder.AppendLine($"{ProcessName} {prop.Attribute.UsageExample}");
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

            foreach (var flag in props)
            {
                if (flag.Attribute.IsRequired && !parsedFlags.ContainsKey(flag.Attribute.Name))
                {
                    errorMessage = $"Required commandline flag \"{flag.Attribute.Name}\" is missing.";
                    return false;
                }
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
                    foreach (var item in parsedValue.Split(","))
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
}