using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Core.Generators;

namespace Compiler.Options
{
    /// <summary>
    /// Represents a command line option for specifying generator configurations.
    /// </summary>
    public class GeneratorOption : CliOption<Core.Generators.GeneratorConfig[]>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorOption"/> class.
        /// </summary>
        public GeneratorOption() : base(name: "--generator", new[] { "-g" })
        {
            Description = "The generator to use.";
            AllowMultipleArgumentsPerToken = false;
            CustomParser = new((ArgumentResult result) =>
            {
                if (result.Tokens.Count == 0)
                {
                    return Array.Empty<Core.Generators.GeneratorConfig>();
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
        private static Core.Generators.GeneratorConfig? ParseGeneratorToken(string token, ArgumentResult result)
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
            var outputPath = remaining[0];
           

            // If the output path is 'stdout', no need to validate it as a file path
            if (!string.Equals("stdout", outputPath, StringComparison.OrdinalIgnoreCase))
            {
                var invalidPathChars = Path.GetInvalidPathChars();
                var invalidCharactersIndex = outputPath.IndexOfAny(invalidPathChars);

                if (invalidCharactersIndex >= 0)
                {
                    result.AddError($"Invalid character '{outputPath[invalidCharactersIndex]}' in output path of generator '{generatorAlias}': '{outputPath}'.");
                    return null;
                }

                outputPath = Path.GetFullPath(outputPath);
            }

            var additionalOptions = remaining.Skip(1).Select(s => s.Split('=')).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
            return new Core.Generators.GeneratorConfig(generatorAlias, outputPath, additionalOptions);
        }
    }
}