using System;
using System.Diagnostics;

namespace Compiler.LangServer
{
    /// <summary>
    /// Represents a text line.
    /// </summary>
    [DebuggerDisplay("{Text}")]
    public sealed class TextLine
    {
        /// <summary>
        /// Gets the line index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the line offset.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the line break.
        /// </summary>
        public char[] LineBreak { get; }

        /// <summary>
        /// Gets the line text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the line length.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextLine"/> class.
        /// </summary>
        /// <param name="index">The line index.</param>
        /// <param name="text">The line text.</param>
        /// <param name="offset">The line offset.</param>
        /// <param name="lineBreak">The line break.</param>
        public TextLine(int index, string text, int offset, char[]? lineBreak = null)
        {
            Index = index;
            Text = text ?? string.Empty;
            Offset = offset;
            LineBreak = lineBreak ?? Array.Empty<char>();
            Length = Text.Length;
        }
    }
}