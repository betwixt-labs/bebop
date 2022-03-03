using System;
using System.Collections.Generic;

namespace Compiler.LangServer
{
    internal sealed class TextCursor
    {
        private readonly string _buffer;
        private int _position;

        public bool CanRead => _position < _buffer.Length;
        public int Position => _position;
        public char Current => _buffer[Position];
        public int Length => _buffer.Length;

        public TextCursor(string content)
        {
            _buffer = content;
            _position = 0;
        }

        public char Peek()
        {
            return Peek(0);
        }

        public char Peek(int offset)
        {
            if (offset < 0)
            {
                throw new InvalidOperationException("Offset must be greater than or equal to 0");
            }

            var pos = _position + offset;
            if (pos >= _buffer.Length)
            {
                return '\0';
            }

            return _buffer[pos];
        }

        public void Move(int position)
        {
            _position = position;
        }

        public char Read()
        {
            var result = Peek();
            if (result != '\0')
            {
                _position++;
            }

            return result;
        }

        public string Slice(int start, int stop)
        {
            return _buffer.Substring(start, stop - start);
        }

        public static List<TextLine> Split(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lines = new List<TextLine>();
            var buffer = new TextCursor(text);
            var index = 0;
            var offset = 0;

            while (buffer.CanRead)
            {
                var current = buffer.Read();
                if (current == '\r' && buffer.Peek(0) == '\n')
                {
                    buffer.Read(); // Consume the \n

                    var line = new TextLine(
                        index, buffer.Slice(offset, buffer.Position - 2),
                        offset, new char[] { '\r', '\n' });

                    lines.Add(line);
                    offset += line.Length + line.LineBreak.Length;
                    index++;
                }
                else if (current == '\n')
                {
                    var line = new TextLine(
                        index, buffer.Slice(offset, buffer.Position - 1),
                        offset, new char[] { '\n' });

                    lines.Add(line);
                    offset += line.Length + line.LineBreak.Length;
                    index++;
                }
            }

            if (offset < buffer.Length)
            {
                lines.Add(
                    new TextLine(
                        index,
                        buffer.Slice(offset, buffer.Position),
                        offset,
                        null));
            }

            return lines;
        }
    }
}