using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Compiler.Generators.CSharp
{
    /// <summary>
    ///     Represents an error that occurs during Pierogi reading and writing
    /// </summary>
    public class PierogiException : Exception
    {
        public PierogiException()
        {
        }

        public PierogiException(string message)
            : base(message)
        {
        }

        public PierogiException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    ///     A low-level interface for encoding and decoding Pierogi structured data
    /// </summary>
    public ref struct PierogiView
    {
        /// <summary>
        ///     A contiguous region of memory that contains the contents of a Pierogi message
        /// </summary>
        private Span<byte> _buffer;

        /// <summary>
        ///     Allocates a new <see cref="PierogiView"/> instance backed by an empty array.
        /// </summary>
        /// <returns></returns>
        public static PierogiView Create() => new PierogiView(Array.Empty<byte>());

        /// <summary>
        ///     Returns a read-only span of all the data present in the underlying <see cref="_buffer"/>
        /// </summary>
        public ReadOnlySpan<byte> Data => _buffer.Slice(0, Length);


        /// <summary>
        ///     Creates a new Pierogi view
        /// </summary>
        /// <param name="buffer">the buffer of data that will be read from / written to</param>
        public PierogiView(Span<byte> buffer)
        {
            _buffer = buffer;
            Position = 0;
            Length = _buffer.Length;
        }

        /// <summary>
        ///     The current byte position of the view
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        ///     The amount of bytes that have been written to the underlying buffer.
        ///     <remarks>
        ///         This is not the same as the <see cref="_buffer"/> length which contains null-bytes due to look-ahead
        ///         allocation.
        ///     </remarks>
        /// </summary>
        public int Length { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ushort ReadUInt16()
        {
            const int size = 2;
            var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public short ReadInt16()
        {
            const int size = 2;
            var value = BinaryPrimitives.ReadInt16LittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public uint ReadUInt32()
        {
            const int size = 4;
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int ReadInt32()
        {
            const int size = 4;
            var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ulong ReadUInt64()
        {
            const int size = 8;
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public long ReadInt64()
        {
            const int size = 8;
            var value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float ReadFloat32()
        {
            const int size = 4;
            var value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double ReadFloat64()
        {
            const int size = 8;
            var value = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteUInt16(ushort value)
        {
            const int size = 2;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(Position, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteInt16(short value)
        {
            const int size = 2;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteInt16LittleEndian(_buffer.Slice(index, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteUInt32(uint value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(index, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteInt32(int value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(index, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteUInt64(ulong value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(index, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteInt64(long value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(index, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteFloat32(float value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteSingleLittleEndian(_buffer.Slice(index, size), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteFloat64(double value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            BinaryPrimitives.WriteDoubleLittleEndian(_buffer.Slice(index, size), value);
        }

        /// <summary>
        ///     Reads a null-terminated string from the underlying buffer
        /// </summary>
        /// <returns>A UTF-16 encoded string</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public string ReadString()
        {
            var result = new StringBuilder();

            while (true)
            {
                int codePoint;

                // decode UTF-8
                var a = ReadByte();
                if (a < 0xC0)
                {
                    codePoint = a;
                }
                else
                {
                    var b = ReadByte();
                    if (a < 0xE0)
                    {
                        codePoint = ((a & 0x1F) << 6) | (b & 0x3F);
                    }
                    else
                    {
                        var c = ReadByte();
                        if (a < 0xF0)
                        {
                            codePoint = ((a & 0x0F) << 12) | ((b & 0x3F) << 6) | (c & 0x3F);
                        }
                        else
                        {
                            var d = ReadByte();
                            codePoint = ((a & 0x07) << 18) | ((b & 0x3F) << 12) | ((c & 0x3F) << 6) | (d & 0x3F);
                        }
                    }
                }

                // strings are null-terminated
                if (codePoint == 0)
                {
                    break;
                }

                // encode UTF-16
                if (codePoint < 0x10000)
                {
                    result.Append((char) codePoint, 1);
                }
                else
                {
                    codePoint -= 0x10000;

                    result.Append(new[] {(char) ((codePoint >> 10) + 0xD800), (char) (codePoint % 0x0400 + 0xDC00)});
                }
            }
            return result.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte ReadByte() => _buffer[Position++];

        /// <summary>
        ///     Reads a <see cref="Guid"/> from the underlying buffer and advances the advances the current position by 16 bytes.
        /// </summary>
        /// <returns>A well-formed Guid structure instances.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Guid ReadGuid()
        {
            const int size = 16;
            var index = Position;
            Position += size;
            return new Guid(_buffer.Slice(index, size));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void GrowBy(int amount)
        {
            if ((Length & 0xC0000000) != 0)
            {
                throw new PierogiException("A Pierogi View cannot grow beyond 2 gigabytes.");
            }
            if (Length + amount > _buffer.Length)
            {
                var newBuffer = new Span<byte>(new byte[(Length + amount) << 1]);
                _buffer.CopyTo(newBuffer);
                _buffer = newBuffer;
            }
            Length += amount;
        }

        /// <summary>
        ///     Reads a length-prefixed byte array from the buffer
        /// </summary>
        /// <returns>An array of bytes</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte[] ReadBytes()
        {
            var length = ReadUInt32();
            var data = _buffer.Slice(Position, (int) length).ToArray();
            Position += (int) length;
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteBytes(byte[] value)
        {
            WriteUInt32(unchecked((uint) value.Length));
            var index = Length;
            GrowBy(value.Length);
            value.AsSpan().CopyTo(_buffer.Slice(index, value.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteGuid(Guid guid)
        {
            const int size = 16;
            var index = Length;
            GrowBy(size);
            guid.TryWriteBytes(_buffer.Slice(index));
        }

        /// <summary>
        ///     Writes a UTF-16 encoded string to the underlying buffer
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteString(in string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                // decode UTF-16
                var a = value[i];
                int codePoint;
                if (i + 1 == value.Length || a < 0xD800 || a >= 0xDC00)
                {
                    codePoint = unchecked((byte) a);
                }
                else
                {
                    var b = value[++i];
                    codePoint = unchecked((byte) ((a << 10) + b + (0x10000 - (0xD800 << 10) - 0xDC00)));
                }
                // strings are null-terminated, so a string cannot contain a null character.
                if (codePoint == 0)
                {
                    throw new PierogiException("Cannot encode a string containing the null character.");
                }
                // encode UTF-8
                if (codePoint < 0x80)
                {
                    WriteByte(unchecked((byte) codePoint));
                }
                else
                {
                    if (codePoint < 0x800)
                    {
                        WriteByte(unchecked((byte) (((codePoint >> 6) & 0x1F) | 0xC0)));
                    }
                    else
                    {
                        if (codePoint < 0x10000)
                        {
                            WriteByte(unchecked((byte) (((codePoint >> 12) & 0x0F) | 0xE0)));
                        }
                        else
                        {
                            WriteByte(unchecked((byte) (((codePoint >> 18) & 0x07) | 0xF0)));
                            WriteByte(unchecked((byte) (((codePoint >> 12) & 0x3F) | 0x80)));
                        }
                        WriteByte(unchecked((byte) (((codePoint >> 6) & 0x3F) | 0x80)));
                    }
                    WriteByte(unchecked((byte) ((codePoint & 0x3F) | 0x80)));
                }
            }
            WriteByte(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteByte(byte value)
        {
            var index = Length;
            GrowBy(1);
            _buffer[index] = value;
        }


        /// <summary>
        ///     Reserve some space to write a message's length prefix, and return its index.
        ///     The length is stored as a little-endian fixed-width unsigned 32 - bit integer, so 4 bytes are reserved.
        /// </summary>
        /// <returns>The index to later write the message's length to.</returns>
        public int ReserveMessageLength() {
            var i = Length;
            GrowBy(4);
            return i;
        }

        /// <summary>
        ///     Fill in a message's length prefix.
        /// </summary>
        /// <param name="position">The position in the buffer of the message's length prefix.</param>
        /// <param name="messageLength">The message length to write.</param>
        public void FillMessageLength(int position, uint messageLength) {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(position, 4), messageLength);
        }

        /// <summary>
        ///     Read out a message's length prefix.
        /// </summary>
        public uint ReadMessageLength()
        {
            var result = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(Position, 4));
            Position += 4;
            return result;
        }
    }
}