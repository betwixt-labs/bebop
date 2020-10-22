using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Compiler.IO.Interfaces;
using Compiler.Lexer.Extensions;

namespace Compiler.IO
{
    /// <summary>
    ///     SchemaReader is a read-only specialized wrapper that can look-ahead without consuming the underlying bytes, making
    ///     it useful
    ///     for tokenization.
    /// </summary>
    public class SchemaReader : Stream,
        ISchemaReader
    {
        private readonly byte[] _buffer;
        private readonly Stream _stream;
        private int _position;
        private readonly string _schemaFileName;

        public SchemaReader(Stream stream, string schemaFileName)
        {
            _stream = stream;
            _buffer = new byte[128];
            _schemaFileName = schemaFileName;
        }

        public string SourcePath => _schemaFileName;

        /// <summary>
        ///     Get or set the current position of the underlying stream
        /// </summary>
        public override long Position
        {
            get => _stream.Position - _position;
            set
            {
                _stream.Position = value;
                // this needs to be done AFTER the call to _stream.Position to avoid a NotSupportedException, 
                // in which case we don't want to change the position
                _position = 0;
            }
        }

        /// <inheritdoc/>
        public int Peek()
        {
            var buffer = new byte[1];
            var peeked = Peek(buffer, 0, 1);
            if (peeked <= 0)
            {
                return -1;
            }
           
            var ch = buffer[^1];
            if (ch.IsControlChar())
            {
                return -1;
            }
            return ch;
        }

        /// <inheritdoc/>
        public char PeekChar()
        {
            var buffer = new byte[1];
            var peeked = Peek(buffer, 0, 1);
            if (peeked <= 0)
            {
                return '\0';
            }
            var ch = buffer[^1];
            if (ch.IsControlChar())
            {
                return '\0';
            }
            return (char) ch;
        }

        public int CurrentPosition => ((int) Position);


        /// <inheritdoc/>
        public char GetChar()
        {
            var read = Read();
            if (read <= 0)
            {
                return '\0';
            }
            var ch = (byte) read;
            if (ch.IsControlChar())
            {
                return '\0';
            }
            return (char)ch;
        }


        /// <summary>
        ///     Peeks at a maximum of count bytes, or less if the stream ends before that number of bytes can be read.
        ///     Calls to this method do not influence subsequent calls to Read() and Peek().
        ///     Please note that this method will always peek count bytes unless the end of the stream is reached before that - in
        ///     contrast to the Read()
        ///     method, which might read less than count bytes, even though the end of the stream has not been reached.
        /// </summary>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the values between
        ///     offset and
        ///     (offset + number-of-peeked-bytes - 1) replaced by the bytes peeked from the current source.
        /// </param>
        /// <param name="offset">
        ///     The zero-based byte offset in buffer at which to begin storing the data peeked from the current
        ///     stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to be peeked from the current stream.</param>
        /// <returns>
        ///     The total number of bytes peeked into the buffer. If it is less than the number of bytes requested then the
        ///     end of the stream has been reached.
        /// </returns>
        protected virtual int Peek(byte[] buffer, int offset, int count)
        {
            if (count > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    "must be smaller than peekable size, which is " + _buffer.Length);
            }

            while (_position < count)
            {
                var bytesRead = _stream.Read(_buffer, _position, count - _position);
                // EOF means end of stream
                if (bytesRead == 0)
                {
                    break;
                }

                _position += bytesRead;
            }

            var peeked = Math.Min(count, _position);
            Buffer.BlockCopy(_buffer, 0, buffer, offset, peeked);
            return peeked;
        }


        /// <summary>
        ///     Reads a byte from the underlying stream and advances the position within the stream by one byte. Will use cached
        ///     results if available.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        private int Read()
        {
            if (_position > 0)
            {
                _position--;
                var firstByte = _buffer[0];
                // pop the remaining bytes in the buffer
                if (_position > 0)
                {
                    Buffer.BlockCopy(_buffer, 1, _buffer, 0, _position);
                }
                return firstByte;
            }
            return _stream.ReadByte();
        }

        /// <summary>
        ///     Releases the unmanaged resources used by the underlying stream and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     true to release both managed and unmanaged resources; false to release only unmanaged
        ///     resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Sets cursor position of the underlying stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">
        ///     A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new
        ///     position.
        /// </param>
        /// <returns>The new position within the underlying stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Current)
            {
                offset -= _position;
            }
            var ret = _stream.Seek(offset, origin);
            // this needs to be done AFTER the call to _stream.Seek() to avoid a NotSupportedException,
            // in which case we don't want to change the position
            _position = 0;
            return ret;
        }


    #region Base Stream Overrides

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => false;
        public override bool CanTimeout => _stream.CanTimeout;
        public override bool CanRead => true;

        public override Task FlushAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Length => _stream.Length;

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void WriteByte(byte value) => throw new NotSupportedException();

        public override int ReadByte() => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = new CancellationToken()) => throw new NotSupportedException();

        public override IAsyncResult BeginWrite(byte[] buffer,
            int offset,
            int count,
            AsyncCallback? callback,
            object? state) => throw new NotSupportedException();

    #endregion
    }
}