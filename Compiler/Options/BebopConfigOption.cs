using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Compiler.Options
{
    /// <summary>
    /// Represents a command line option for specifying Bebop configuration.
    /// </summary>
    public class BebopConfigOption : CliOption<BebopConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BebopConfigOption"/> class.
        /// </summary>
        public BebopConfigOption() : base(name: "--config", aliases: new[] { "-c" })
        {
            Description = "Path to a bebop.json file. If not specified, the compiler will search for a bebop.json file in the current directory and its parent directories.";
            AllowMultipleArgumentsPerToken = false;
            AcceptLegalFilePathsOnly();

            Validators.Add(static result =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(token)) return;
                if (new FileInfo(token).Exists) return;
                result.AddError($"Specified config '{token}' does not exist.");
            });

            DefaultValueFactory = (_) => BebopConfig.FromFile(BebopConfig.Locate());

            CustomParser = new((ArgumentResult result) =>
            {
                var configPath = result.Tokens.SingleOrDefault()?.Value;
                var config = new FileInfo(configPath!);
                return BebopConfig.FromFile(config.FullName);
            });
        }
    }
}