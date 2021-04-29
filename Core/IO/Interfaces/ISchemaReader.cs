using System.Threading.Tasks;
using Core.Lexer.Extensions;
using Core.Lexer.Tokenization.Models;

namespace Core.IO.Interfaces
{
    public interface ISchemaReader
    {
        /// <summary>
        ///     Returns an empty span at the current schema position.
        /// </summary>
        public Span CurrentSpan();

        /// <summary>
        ///     Returns an empty span at the most recent end-of-file position.
        /// </summary>
        public Span LatestEofSpan();

        /// <summary>
        ///     Returns the next available character but does not consume it.
        /// </summary>
        /// <returns>
        ///     A char literal, or \0 if there are no characters left to be read.
        ///     At the end of each individual input file, an artificial <see cref="CharExtensions.FileSeparator"/> is returned.
        /// </returns>
        public char PeekChar();

        /// <summary>
        ///     Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        ///     A char literal, or \0 if there are no characters left to be read.
        ///     At the end of each individual input file, an artificial <see cref="CharExtensions.FileSeparator"/> is returned.
        /// </returns>
        public char GetChar();

        /// <summary>
        ///     Append a file path to be read.
        /// </summary>
        /// <returns>True if a new file was actually added and must now be tokenized; false if this path was a duplicate.</returns>
        public Task<bool> AddFile(string path);
    }
}