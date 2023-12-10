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
        public DiagnosticFormatOption() : base(name: "--diagnostic-format", aliases: new[] { "-df" })
        {
            Description = "The format to use for diagnostic messages.";
            AllowMultipleArgumentsPerToken = false;

            Validators.Add(result =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(token)) return;
                if (Enum.TryParse<LogFormatter>(token, true, out _)) return;
                result.AddError($"Invalid diagnostic format '{token}'.");
            });

            CustomParser = new((ArgumentResult result) =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(token)) return LogFormatter.Enhanced;
                return Enum.Parse<LogFormatter>(token, true);
            });

            DefaultValueFactory = (_) => LogFormatter.Enhanced;
        }
    }
}