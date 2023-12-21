using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Core.Generators
{
    /// <summary>
    /// Represents a configuration for a generator.
    /// </summary>
    public sealed record GeneratorConfig
    {
        public GeneratorConfig(string alias, string outFile): this(alias, outFile, TempoServices.Both, true, string.Empty, true, null)
        {

        }

        //// <summary>
        /// Initializes a new instance of the <see cref="GeneratorConfig"/> class with all parameters.
        /// </summary>
        public GeneratorConfig(string alias,
                               string outFile,
                               TempoServices services,
                               bool emitNotice,
                               string @namespace,
                               bool emitBinarySchema,
                               Dictionary<string, string>? options)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(alias, nameof(alias));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(outFile, nameof(outFile));
            Alias = alias;
            OutFile = outFile;
            Services = services;
            EmitNotice = emitNotice;
            EmitBinarySchema = emitBinarySchema;
            Namespace = string.IsNullOrWhiteSpace(@namespace) ? string.Empty : @namespace;
            Options = options ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        [JsonIgnore]
        public string Alias { get; init; }
        public string OutFile { get; init; }
        public TempoServices Services { get; init; }
        public bool EmitNotice { get; init; }
        public bool EmitBinarySchema { get; init; }
        public string Namespace { get; init; }
        private Dictionary<string, string> Options { get; init; }

        public int OptionCount => Options.Count;

        /// <summary>
        /// Gets a boolean option value.
        /// </summary>
        public bool GetOptionBoolValue(string key, bool defaultValue = false)
        {
            return Options.TryGetValue(key, out var value)
                ? bool.TryParse(value, out var boolValue)
                    ? boolValue
                    : defaultValue
                : defaultValue;
        }

        /// <summary>
        /// Gets an enum option value.
        /// </summary>
        public T GetOptionEnumValue<T>(string key, T? defaultValue = null) where T : struct, Enum
        {
            return Options.TryGetValue(key, out var value)
                ? Enum.TryParse<T>(value, out var enumValue)
                    ? enumValue
                    : defaultValue ?? default
                : defaultValue ?? default;
        }

        /// <summary>
        /// Gets a raw option value.
        /// </summary>
        public string? GetOptionRawValue(string key)
        {
            return Options.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Gets an integer option value.
        /// </summary>
        public int? GetOptionIntValue(string key)
        {
            return Options.TryGetValue(key, out var value)
                ? int.TryParse(value, out var intValue)
                    ? intValue
                    : null
                : null;
        }

        /// <summary>
        /// Adds a new option to the configuration.
        /// </summary>
        /// <param name="key">The key of the option.</param>
        /// <param name="value">The value of the option.</param>
        public void AddOption(string key, string value)
        {
            Options.Add(key, value);
        }

        /// <summary>
        /// Adds multiple options to the configuration.
        /// </summary>
        /// <param name="options">The options to add, represented as a dictionary.</param>
        public void AddOptions(Dictionary<string, string> options)
        {
            foreach (var (key, value) in options)
            {
                Options.Add(key, value);
            }
        }

        public KeyValuePair<string, string>[] GetOptions()
        {
            var options = new KeyValuePair<string, string>[Options.Count];
            for (var i = 0; i < options.Length; i++)
            {
                options[i] = Options.ElementAt(i);
            }
            return options;
        }
    }
}