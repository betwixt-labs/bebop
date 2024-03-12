using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using Core.Logging;

namespace Compiler.Options
{
    /// <summary>
    /// Represents a command line option for specifying the format of diagnostic messages.
    /// </summary>
    public class DiagnosticFormatOption : CliOption<LogFormatter>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticFormatOption"/> class.
        /// </summary>
        public DiagnosticFormatOption() : base(name: CliStrings.DiagnosticFormatFlag, aliases: ["-df"])
        {
            Description = "Specifies the format which diagnostic messages are printed in.";
            AllowMultipleArgumentsPerToken = false;
            Validators.Add(result =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(token)) return;
                if (!IsLogFormatter(token))
                {
                    result.AddError($"Invalid diagnostic format '{token}'.");
                    return;
                }
            });

            CustomParser = new((ArgumentResult result) =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value?.Trim();
                return Parse(token);
            });


            DefaultValueFactory = (a) =>
            {
                return LogFormatter.Enhanced;
            };
        }

        public static bool IsLogFormatter(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            return token.ToLowerInvariant() switch
            {
                "enhanced" => true,
                "json" => true,
                "msbuild" => true,
                _ => false,
            };
        }

        public static LogFormatter Parse(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return LogFormatter.Enhanced;
            return token.ToLowerInvariant() switch
            {
                "enhanced" => LogFormatter.Enhanced,
                "json" => LogFormatter.JSON,
                "msbuild" => LogFormatter.MSBuild,
                _ => throw new ArgumentException($"Invalid diagnostic format '{token}'."),
            };
        }
    }
}