using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Core.Generators;
using Core.Meta.Extensions;
namespace Compiler.Options
{
    /// <summary>
    /// Represents a command line option for specifying generator configurations.
    /// </summary>
    public class GeneratorOption : CliOption<GeneratorConfig[]>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorOption"/> class.
        /// </summary>
        public GeneratorOption() : base(name: CliStrings.GeneratorFlag, ["-g"])
        {
            Description = "Specifies code generators to use for compilation.";
            AllowMultipleArgumentsPerToken = false;
            CustomParser = new((ArgumentResult result) =>
            {
                if (result.Tokens.Count == 0)
                {
                    return [];
                }
                return result.Tokens.Select(t => ParseGeneratorToken(t.Value, result)).Where(c => c is not null).Select(c => c!).ToArray();
            });
        }

        /// <summary>
        /// Parses a generator token into a <see cref="GeneratorConfig"/> object.
        /// </summary>
        /// <param name="token">The generator token to parse.</param>
        /// <param name="result">The argument result to which errors can be added.</param>
        /// <returns>A <see cref="GeneratorConfig"/> object if the token could be parsed; otherwise, null.</returns>
        private static GeneratorConfig? ParseGeneratorToken(string token, ArgumentResult result)
        {
            var parts = token.Split(':');
            if (parts.Length != 2)
            {
                result.AddError($"Incomplete generator token specified '{token}'.");
                return null;
            }

            var generatorAlias = parts[0].Trim().ToLower();
            if (!GeneratorUtils.ImplementedGeneratorNames.ContainsKey(generatorAlias))
            {
                result.AddError($"Unknown generator alias '{generatorAlias}'.");
                return null;
            }
            var remaining = parts[1].Split(',');
            var outputPath = remaining[0] ?? string.Empty;


            // If the output path is 'stdout', no need to validate it as a file path
            if (!string.Equals("stdout", outputPath, StringComparison.OrdinalIgnoreCase) && !outputPath.IsLegalFilePath(out var invalidCharacterIndex))
            {
                if (invalidCharacterIndex >= 0)
                {
                    result.AddError($"Invalid character '{outputPath[invalidCharacterIndex]}' in output path of generator '{generatorAlias}': '{outputPath}'.");
                    return null;
                }
            }

            var additionalOptions = remaining.Skip(1).Select(s => s.Split('=')).Where(p => p.Length == 2).ToDictionary(p => p[0].Trim(), p => p[1].Trim());

            // Initialize default values
            var services = TempoServices.Both;
            var emitNotice = true;
            var emitBinarySchema = true;
            string @namespace = string.Empty;

            foreach (var option in additionalOptions)
            {
                switch (option.Key.ToLower())
                {
                    case "services":
                        if (!Enum.TryParse<TempoServices>(option.Value, true, out var parsedServices))
                        {
                            result.AddError($"Invalid value '{option.Value}' for option 'services'.");
                            break;
                        }
                        services = parsedServices;
                        break;
                    case "emitnotice":
                        if (!bool.TryParse(option.Value, out var parsedEmitNotice))
                        {
                            result.AddError($"Invalid value '{option.Value}' for option 'emitNotice'.");
                            break;
                        }
                        emitNotice = parsedEmitNotice;
                        break;
                    case "emitbinaryschema":
                        if (!bool.TryParse(option.Value, out var parsedEmitBinarySchema))
                        {
                            result.AddError($"Invalid value '{option.Value}' for option 'emitBinarySchema'.");
                            break;
                        }
                        emitBinarySchema = parsedEmitBinarySchema;
                        break;
                    case "namespace":
                        if (string.IsNullOrWhiteSpace(option.Value))
                        {
                            result.AddError($"Invalid value '{option.Value}' for option 'namespace'.");
                            break;
                        }
                        @namespace = option.Value;
                        break;
                }
            }
            return new GeneratorConfig(generatorAlias, outputPath, services, emitNotice, @namespace, emitBinarySchema, additionalOptions);
        }
    }
}