using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Compiler.Lexer.Tokenization.Models
{
    /// <summary>
    ///     A value representing a span of characters in a schema.
    /// </summary>
    /// <remarks>
    ///     This structure uses a zero-based index to represent the matrix of a file, however most GUI text editors (such as
    ///     VSCode)
    ///     will show the cursor position as being off by one from what is stored here.
    /// </remarks>
    public readonly struct Span : IEquatable<Span>, IComparable<Span>
    {
        /// <summary>
        ///     Creates a single-character span at specified line and column.
        /// </summary>
        /// <param name="fileName">The document's file name.</param>
        /// <param name="line">The start/end document line.</param>
        /// <param name="column">The start/end document column.</param>
        public Span(string fileName, int line, int column) : this(fileName, line, column, line, column)
        {

        }

        /// <summary>
        ///     Creates a span within the specified document coordinates.
        /// </summary>
        /// <param name="fileName">The document's file name.</param>
        /// <param name="startLine">The starting document line.</param>
        /// <param name="startColumn">The starting document column.</param>
        /// <param name="endLine">The ending document line.</param>
        /// <param name="endColumn">The ending document column.</param>
        public Span(string fileName, int startLine, int startColumn, int endLine, int endColumn)
        {
            FileName = fileName;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        /// <summary>
        ///     The source file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     The starting line position.
        /// </summary>
        public int StartLine { get; }

        /// <summary>
        ///     The ending line position.
        /// </summary>
        public int EndLine { get; }

        /// <summary>
        ///     The starting column position.
        /// </summary>
        public int StartColumn { get; }

        /// <summary>
        ///     The ending column position.
        /// </summary>
        public int EndColumn { get; }

        /// <summary>
        ///     The number of lines in the document coordinates.
        /// </summary>
        public int Lines => EndLine - StartLine + 1;

        /// <summary>
        ///     Extends the span to the next line. Returns a new Span with the same start position, ending on next line at column
        ///     0.
        /// </summary>
        public static Span StartOfFile(string fileName) => new Span(fileName, 0, 0);

        /// <summary>
        ///     Extends the span to the next line. Returns a new Span with the same start position, ending on next line at column
        ///     0.
        /// </summary>
        [JsonIgnore]
        public Span ExtendLine => new Span(FileName, StartLine, StartColumn, EndLine + 1, 0);

        /// <summary>
        ///     Returns a new empty span at column 0 on the next line.
        /// </summary>
        [JsonIgnore]
        public Span StartOfNextLine => new Span(FileName, EndLine + 1, 0);

        /// <summary>
        ///     Extends the span to the next column. Returns a new Span with the same start position, ending on the same line at
        ///     the next column.
        /// </summary>
        [JsonIgnore]
        public Span ExtendColumn => new Span(FileName, StartLine, StartColumn, EndLine, EndColumn + 1);

        /// <summary>
        ///     Returns a new span starting on the same line, at the next column.
        /// </summary>
        [JsonIgnore]
        public Span Next => new Span(FileName, EndLine, EndColumn + 1);

        /// <summary>
        ///     Returns a new span with the same start position, ending on the same line at the previous column.
        /// </summary>
        [JsonIgnore]
        public Span PreviousColumn => new Span(FileName, StartLine, StartColumn, EndLine, EndColumn - 1);

        /// <summary>
        ///     Returns a new span starting at the end of the current position.
        /// </summary>
        [JsonIgnore]
        public Span End => new Span(FileName, EndLine, EndColumn);

        /// <summary>
        /// Return a span with the same start position that continues on the same line for <paramref name="length"/> characters.
        /// </summary>
        public Span WithLength(uint length) => new Span(FileName, StartLine, StartColumn, StartLine, (int)(StartColumn + length));

        public Span Combine(Span other) => Combine(this, other);

        public Span WithFileName(string newFileName) => new Span(newFileName, StartLine, StartColumn, EndLine, EndColumn);

        public static Span Combine(params Span[] a)
        {
            var min = a.Min();
            var max = a.Max();
            return new Span(a[0].FileName, min.StartLine, min.StartColumn, max.EndLine, max.EndColumn);
        }

        public static bool operator ==(Span x, Span y) => x.Equals(y);

        public static bool operator !=(Span x, Span y) => !x.Equals(y);

        public bool Equals(Span other) => other.StartLine == StartLine && other.EndLine == EndLine &&
            other.StartColumn == StartColumn && other.EndColumn == EndColumn;

        public int CompareTo(Span other)
        {
            if (Equals(other))
            {
                return 0;
            }

            if (other.StartLine < StartLine || other.StartLine == StartLine && other.StartColumn < StartColumn)
            {
                return 1;
            }

            return -1;
        }

        public override bool Equals(object? obj) => obj is Span span && Equals(span);

        public override int GetHashCode() => HashCode.Combine(StartLine, EndLine, StartColumn, EndColumn);

        public override string ToString() => $"L{StartLine}C{StartColumn}:L{EndLine}C{EndColumn}";

        public string StartColonString() => $"{StartLine + 1}:{StartColumn + 1}";
    }
}