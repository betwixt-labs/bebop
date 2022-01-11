using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Lexer.Tokenization.Models;
using Core.Meta;

namespace Core.Logging
{
    public class MiscErrorCodes
    {
        public const int FileNotFound = 404;
        public const int Unknown = 1000;
    }

    public record Diagnostic(Severity Severity, string Message, int ErrorCode, Span? Span) {}

    /// <summary>
    /// A central logging factory
    /// </summary>
    public class Lager
    {
        public LogFormatter Formatter { get; set; }

        private Lager(LogFormatter logFormatter)
        {
            Formatter = logFormatter;
        }

        /// <summary>
        /// Write text to standard out without formatting.
        /// </summary>
        public static async Task StandardOut(string message)
        {
            await Console.Out.WriteLineAsync(message);
        }

        /// <summary>
        /// Write text to standard error without formatting.
        /// </summary>
        public static async Task StandardError(string message)
        {
            await Console.Error.WriteLineAsync(message);
        }

        private string FormatDiagnostic(Diagnostic diagnostic)
        {
            var span = diagnostic.Span;
            var message = diagnostic.Message;
            if (diagnostic.Severity == Severity.Warning)
            {
                message += $" (To disable this warning, run bebopc with `--no-warn {diagnostic.ErrorCode}`)";
            }
            switch (Formatter)
            {
                case LogFormatter.MSBuild:
                    var where = span == null ? ReservedWords.CompilerName : $"{span?.FileName}({span?.StartColonString(',')})";
                    return $"{where} : {diagnostic.Severity.ToString().ToLowerInvariant()} BOP{diagnostic.ErrorCode}: {message}";
                case LogFormatter.Structured:
                    where = span == null ? "" : $"Issue located in '{span?.FileName}' at {span?.StartColonString()}: ";
                    return $"[{DateTime.Now}][Compiler][{diagnostic.Severity}] {where}{message}";
                case LogFormatter.JSON:
                    var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
                    return JsonSerializer.Serialize(diagnostic, options);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string FormatSpanError(SpanException ex) =>
            FormatDiagnostic(new(ex.Severity, ex.Message, ex.ErrorCode, ex.Span));

        /// <summary>
        /// Format and write a list of <see cref="SpanException"/>
        /// </summary>
        public async Task WriteSpanErrors(List<SpanException> exs)
        {
            var messages = exs.Select(FormatSpanError);
            var joined = Formatter switch
            {
                LogFormatter.JSON => "[" + string.Join(",\n", messages) + "]",
                _ => string.Join("\n", messages),
            };

            if (string.IsNullOrWhiteSpace(joined))
            {
                // Don't print a single blank line.
                return;
            }
            await Console.Error.WriteLineAsync(joined);
        }

        /// <summary>
        /// Format and write a <see cref="FileNotFoundException"/> 
        /// </summary>
        private async Task WriteFileNotFoundError(FileNotFoundException ex) =>
            await Console.Error.WriteLineAsync(FormatDiagnostic(new(
                Severity.Error, "Unable to open file: " + ex?.FileName, MiscErrorCodes.FileNotFound, null)));

        /// <summary>
        /// Format and write a <see cref="CompilerException"/> 
        /// </summary>
        private async Task WriteCompilerException(CompilerException ex) =>
            await Console.Error.WriteLineAsync(FormatDiagnostic(new(
                Severity.Error, ex.Message, ex.ErrorCode, null)));

        /// <summary>
        /// Writes an exception with no dedicated formatting method.
        /// </summary>
        private async Task WriteBaseError(Exception ex) =>
            await Console.Error.WriteLineAsync(FormatDiagnostic(new(
                Severity.Error, ex.Message, MiscErrorCodes.Unknown, null)));

        /// <summary>
        /// Writes an exception to standard error.
        /// </summary>
        public async Task Error(Exception ex)
        {
            switch (ex)
            {
                case SpanException span:
                    await WriteSpanErrors(new List<SpanException>() { span });
                    break;
                case FileNotFoundException file:
                    await WriteFileNotFoundError(file);
                    break;
                case CompilerException compiler:
                    await WriteCompilerException(compiler);
                    break;
                default:
                    await WriteBaseError(ex);
                    break;
            }
        }

        /// <summary>
        /// Creates a new logger that uses the specified <paramref name="logFormatter"/>
        /// </summary>
        /// <param name="logFormatter">The formatter to control how data is structured.</param>
        /// <returns>A new <see cref="Lager"/> instance</returns>
        public static Lager CreateLogger(LogFormatter logFormatter)
        {
            if (!Enum.IsDefined(typeof(LogFormatter), logFormatter))
            {
                throw new ArgumentOutOfRangeException(nameof(logFormatter),
                    "Value should be defined in the Formatter enum.");
            }
            return new(logFormatter);
        }

    }
}
