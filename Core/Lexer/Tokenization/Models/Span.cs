using System;
using System.Linq;
using Core.Meta.Extensions;

namespace Core.Lexer.Tokenization.Models
{
    /// <summary>
    ///     A value representing a span of characters in a schema.
    /// </summary>
    /// <remarks>
    ///     This structure uses a zero-based index to represent the matrix of a file, however most GUI text editors (such as
    ///     VSCode)
    ///     will show the cursor position as being off by one from what is stored here.
    /// </remarks>
    [Serializable]
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

        public Span Combine(Span other) => Combine(this, other);

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
            var c = StartLine.CompareTo(other.StartLine);
            if (c != 0) return c;
            c = StartColumn.CompareTo(other.StartColumn);
            if (c != 0) return c;
            c = EndLine.CompareTo(other.EndLine);
            if (c != 0) return c;
            c = EndColumn.CompareTo(other.EndColumn);
            return c;
        }


        public string ToJson()
        {
            return $"{{\"FileName\":\"{FileName.EscapeString()}\",\"StartLine\":{StartLine},\"EndLine\":{EndLine},\"StartColumn\":{StartColumn},\"EndColumn\":{EndColumn},\"Lines\":{Lines}}}";
        }

        public override bool Equals(object? obj) => obj is Span span && Equals(span);

        public override int GetHashCode() => HashCode.Combine(StartLine, EndLine, StartColumn, EndColumn);

        public override string ToString() => $"L{StartLine}C{StartColumn}:L{EndLine}C{EndColumn}";

        public string StartColonString(char separator = ':') => $"{StartLine + 1}{separator}{StartColumn + 1}";
    }
}