using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Core.Logging;
using Core.Meta;
using Errata;

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
        public BebopConfigOption() : base(name: CliStrings.ConfigFlag, aliases: ["-c"])
        {
            Description = "Compile the project given the path to its configuration file.";
            AllowMultipleArgumentsPerToken = false;
            AcceptLegalFilePathsOnly();

            Validators.Add(static result =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(token)) return;
                if (new FileInfo(token).Exists) return;
                result.AddError($"Specified config '{token}' does not exist.");
            });

            DefaultValueFactory = (_) =>
            {
                var path = BebopConfig.Locate();
                if (string.IsNullOrEmpty(path)) return BebopConfig.Default;
                return BebopConfig.FromFile(path);
            };

            CustomParser = new((ArgumentResult result) =>
            {
                var configPath = result.Tokens.SingleOrDefault()?.Value;
                var config = new FileInfo(configPath!);
                return BebopConfig.FromFile(config.FullName);
            });
        }
    }
}