using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Extensions;

namespace Core.Logging
{

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

        /// <summary>
        /// Format and write a <see cref="SpanException"/> 
        /// </summary>
        private async Task WriteSpanError(SpanException ex)
        {
            var message = Formatter switch
            {
                LogFormatter.MSBuild => $"{ex.Span.FileName}({ex.Span.StartColonString(',')}) : error BOP{ex.ErrorCode}: {ex.Message}",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] Issue located in '{ex.Span.FileName}' at {ex.Span.StartColonString()}: {ex.Message}",
                LogFormatter.JSON => $@"{{""Message"": ""{ex.Message.EscapeString()}"", ""Span"": {ex.Span.ToJson()}}}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message);
        }

        /// <summary>
        /// Format and write a list of <see cref="SpanException"/>
        /// </summary>
        public async Task WriteSpanErrors(List<SpanException> exs)
        {
            var messages = exs.Select(ex =>
            {
                return Formatter switch
                {
                    LogFormatter.MSBuild => $"{ex.Span.FileName}({ex.Span.StartColonString(',')}) : error BOP{ex.ErrorCode}: {ex.Message}",
                    LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] Issue located in '{ex.Span.FileName}' at {ex.Span.StartColonString()}: {ex.Message}",
                    LogFormatter.JSON => $@"{{""Message"": ""{ex.Message.EscapeString()}"", ""Span"": {ex.Span.ToJson()}}}",
                    _ => throw new ArgumentOutOfRangeException()
                };
            });
            string message;
            if (Formatter == LogFormatter.JSON)
            {
                message = "[" + string.Join(", ", messages) + "]";
            }
            else
            {
                message = string.Join("\n", messages);
            }
            await Console.Error.WriteLineAsync(message);
        }

        /// <summary>
        /// Format and write a <see cref="FileNotFoundException"/> 
        /// </summary>
        private async Task WriteFileNotFoundError(FileNotFoundException ex)
        {
            const int msBuildErrorCode = 404;
            var message = Formatter switch
            {
                LogFormatter.MSBuild => $"{ReservedWords.CompilerName} : fatal error BOP{msBuildErrorCode}: cannot open file '{ex.FileName}'",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] Unable to open file '{ex.FileName}'",
                LogFormatter.JSON => $@"{{""Message"": ""{ex.Message.EscapeString()}"", ""FileName"": {ex?.FileName?.EscapeString()}}}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message);
        }

        /// <summary>
        /// Format and write a <see cref="CompilerException"/> 
        /// </summary>
        private async Task WriteCompilerException(CompilerException ex)
        {
            var message = Formatter switch
            {
                LogFormatter.MSBuild => $"{ReservedWords.CompilerName} : fatal error BOP{ex.ErrorCode}: {ex.Message}",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] {ex.Message}",
                LogFormatter.JSON => $@"{{""Message"": ""{ex.Message.EscapeString()}""}}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message);
        }

        /// <summary>
        /// Writes an exception with no dedicated formatting method.
        /// </summary>
        private async Task WriteBaseError(Exception ex)
        {
            // for when we don't know the actual error.
            const int msBuildErrorCode = 1000;
            var message = Formatter switch
            {
                LogFormatter.MSBuild => $"{ReservedWords.CompilerName} : fatal error BOP{msBuildErrorCode}: {ex.Message}",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] {ex.Message}",
                LogFormatter.JSON => $@"{{""Message"": ""{ex.Message.EscapeString()}""}}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message);
        }

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
