using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.IO.Interfaces;
using Core.Lexer.Tokenization.Models;

namespace Core.IO
{
    /// <summary>
    ///     SchemaReader is a read-only specialized wrapper that can look-ahead without consuming the underlying bytes, making it useful for tokenization.
    /// </summary>
    public class SchemaReader : ISchemaReader
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
            return new SchemaReader(schemaPaths.Select(path => (path, File.ReadAllText(path) + Environment.NewLine)).ToList());
        }

        private string CurrentFile => _schemas[_schemaIndex].Item2;
        private char CurrentChar => CurrentFile[_position];
        private int CurrentFileLength => CurrentFile.Length;
        private bool NoFilesLeft => _schemaIndex >= _schemas.Count;
        private bool AtEndOfCurrentFile => !NoFilesLeft && _position >= CurrentFileLength;

        private string CurrentFileName => _schemas[_schemaIndex].Item1;
        public Span CurrentSpan() => NoFilesLeft ? new Span("(eof)", 0, 0) : new Span(CurrentFileName, _currentLine, _currentColumn);

        /// <inheritdoc/>
        public int Peek()
        {
            if (NoFilesLeft) return -1;
            return CurrentChar;
        }

        /// <inheritdoc/>
        public char PeekChar()
        {
            if (NoFilesLeft) return '\0';
            return CurrentChar;
        }

        /// <inheritdoc/>
        public char GetChar()
        {
            if (NoFilesLeft) return '\0';
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
            while (AtEndOfCurrentFile)
            {
                _schemaIndex++;
                _position = 0;
                _currentLine = 0;
                _currentColumn = 0;
            }
            return ch;
        }

        /// <inheritdoc/>
        public async Task<bool> AddFile(string absolutePath)
        {
            var fullPath = Path.GetFullPath(absolutePath);
            if (!_schemas.Any(t => Path.GetFullPath(t.Item1) == fullPath))
            {
                var text = await File.ReadAllTextAsync(fullPath);
                _schemas.Add((fullPath, text + Environment.NewLine));
                return true;
            }
            return false;
        }
    }
}