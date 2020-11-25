using System;
using System.IO;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Meta;

namespace Core.Logging
{

    /// <summary>
    /// A central logging factory
    /// </summary>
    public class Lager
    {
        private readonly LogFormatter _logFormatter;

        private Lager(LogFormatter logFormatter)
        {
            _logFormatter = logFormatter;
        }

        /// <summary>
        /// Write text to standard out without formatting.
        /// </summary>
        public static async Task StandardOut(string message)
        {
            await Console.Out.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Write text to standard error without formatting.
        /// </summary>
        public static async Task StandardError(string message)
        {
            await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Format and write a <see cref="SpanException"/> 
        /// </summary>
        private async Task WriteSpanError(SpanException ex)
        {
            var message = _logFormatter switch
            {
                LogFormatter.MSBuild => $"{ex.Span.FileName}({ex.Span.StartColonString(',')}) : error BOP{ex.ErrorCode}: Compiler error: {ex.Message}",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] Issue located in \"{ex.Span.FileName}\" at {ex.Span.StartColonString()}: {ex.Message}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Format and write a <see cref="FileNotFoundException"/> 
        /// </summary>
        private async Task WriteFileNotFoundError(FileNotFoundException ex)
        {
            const int msBuildErrorCode = 404;
            var message = _logFormatter switch
            {
                LogFormatter.MSBuild => $"{ReservedWords.CompilerName} : fatal error BOP{msBuildErrorCode}: cannot open file '{ex.FileName}'",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] Unable to open file \"{ex.FileName}\"",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Format and write a <see cref="CompilerException"/> 
        /// </summary>
        private async Task WriteCompilerException(CompilerException ex)
        {
            var message = _logFormatter switch
            {
                LogFormatter.MSBuild => $"{ReservedWords.CompilerName} : fatal error BOP{ex.ErrorCode}: {ex.Message}",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] {ex.Message}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes an exception with no dedicated formatting method.
        /// </summary>
        private async Task WriteBaseError(Exception ex)
        {
            // for when we don't know the actual error.
            const int msBuildErrorCode = 1000;
            var message = _logFormatter switch
            {
                LogFormatter.MSBuild => $"{ReservedWords.CompilerName} : fatal error BOP{msBuildErrorCode}: {ex.Message}",
                LogFormatter.Structured => $"[{DateTime.Now}][Compiler][Error] {ex.Message}",
                _ => throw new ArgumentOutOfRangeException()
            };
            await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes an exception to standard error.
        /// </summary>
        public async Task Error(Exception ex)
        {
            switch (ex)
            {
                case SpanException span:
                    await WriteSpanError(span).ConfigureAwait(false);
                    break;
                case FileNotFoundException file:
                    await WriteFileNotFoundError(file).ConfigureAwait(false);
                    break;
                case CompilerException compiler:
                    await WriteCompilerException(compiler).ConfigureAwait(false);
                    break;
                default:
                    await WriteBaseError(ex).ConfigureAwait(false);
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