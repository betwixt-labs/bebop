using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Lexer.Extensions;
using Core.Lexer.Tokenization.Models;

namespace Core.IO
{
    /// <summary>
    ///     SchemaReader is a read-only specialized wrapper that can look-ahead without consuming the underlying bytes, making it useful for tokenization.
    /// </summary>
    public class SchemaReader
    {
        // Name and contents of the schema files we're processing.
        private readonly List<(string, string)> _schemas;

        // The current position into the schema list.
        private int _schemaIndex = 0;

        // The position into the current schema file.
        private int _position = 0;

        private int _currentLine = 0;
        private int _currentColumn = 0;

        private SchemaReader(List<(string, string)> schemas) {
            _schemas = schemas;
        }

        public static SchemaReader FromTextualSchema(string textualSchema)
        {
            return new SchemaReader(new List<(string, string)> { ("(unknown)", textualSchema) });
        }

        public static SchemaReader FromSchemaPaths(IEnumerable<string> schemaPaths)
        {
            return new SchemaReader(schemaPaths.Select(path => (path, File.ReadAllText(path))).ToList());
        }

        private string CurrentFile => _schemas[_schemaIndex].Item2;
        private char CurrentChar => CurrentFile[_position];
        private int CurrentFileLength => CurrentFile.Length;
        private bool NoFilesLeft => _schemaIndex >= _schemas.Count;
        private bool AtEndOfCurrentFile => !NoFilesLeft && _position >= CurrentFileLength;

        private string CurrentFileName => _schemas[_schemaIndex].Item1;

        /// <summary>
        ///     Returns an empty span at the current schema position.
        /// </summary>
        public Span CurrentSpan() => NoFilesLeft ? _latestEofSpan : new Span(CurrentFileName, _currentLine, _currentColumn);

        private Span _latestEofSpan = new Span("(unknown file)", 0, 0);
        /// <summary>
        ///     Returns an empty span at the most recent end-of-file position.
        /// </summary>
        public Span LatestEofSpan() => _latestEofSpan;

        /// <summary>
        ///     Returns the next available character but does not consume it.
        /// </summary>
        /// <returns>
        ///     A char literal, or \0 if there are no characters left to be read.
        ///     At the end of each individual input file, an artificial <see cref="CharExtensions.FileSeparator"/> is returned.
        /// </returns>
        public char PeekChar()
        {
            if (NoFilesLeft) return '\0';
            if (AtEndOfCurrentFile) return CharExtensions.FileSeparator;
            return CurrentChar;
        }

        /// <summary>
        ///     Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        ///     A char literal, or \0 if there are no characters left to be read.
        ///     At the end of each individual input file, an artificial <see cref="CharExtensions.FileSeparator"/> is returned.
        /// </returns>
        public char GetChar()
        {
            if (NoFilesLeft) return '\0';
            if (AtEndOfCurrentFile)
            {
                _latestEofSpan = CurrentSpan();
                _schemaIndex++;
                _position = 0;
                _currentLine = 0;
                _currentColumn = 0;
                return CharExtensions.FileSeparator;
            }
            var ch = CurrentChar;
            if (ch == '\n')
            {
                _currentLine++;
                _currentColumn = 0;
            } else
            {
                _currentColumn++;
            }
            _position++;
            return ch;
        }

        /// <summary>
        ///     Append a file path to be read.
        /// </summary>
        /// <returns>True if a new file was actually added and must now be tokenized; false if this path was a duplicate.</returns>
        public async Task<bool> AddFile(string absolutePath)
        {
            var fullPath = Path.GetFullPath(absolutePath);
            if (!_schemas.Any(t => Path.GetFullPath(t.Item1) == fullPath))
            {
                var text = await File.ReadAllTextAsync(fullPath);
                _schemas.Add((fullPath, text));
                return true;
            }
            return false;
        }
    }
}