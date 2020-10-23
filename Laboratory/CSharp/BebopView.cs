using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Bebop
{
    /// <summary>
    ///     Represents an error that occurs during Bebop reading and writing
    /// </summary>
    public class BebopException : Exception
    {
        public BebopException()
        {
        }

        public BebopException(string message)
            : base(message)
        {
        }

        public BebopException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    ///     A low-level interface for encoding and decoding Bebop structured data
    ///     TODO disable big-endian systems
    /// </summary>
    public ref struct BebopView
    {
    #if AGGRESSIVE_OPTIMIZE
        internal const MethodImplOptions HotPath =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    #else
        internal const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
    #endif

        /// <summary>
        ///     A contiguous region of memory that contains the contents of a Bebop message
        /// </summary>
        private Span<byte> _buffer;

        /// <summary>
        ///     Allocates a new <see cref="BebopView"/> instance backed by an empty array.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(HotPath)]
        public static BebopView Create() => new BebopView(Array.Empty<byte>());

        /// <summary>
        ///     Returns a read-only span of all the data present in the underlying <see cref="_buffer"/>
        /// </summary>
        public ReadOnlySpan<byte> Data => _buffer.Slice(0, Length);


        /// <summary>
        ///     Creates a new Bebop view
        /// </summary>
        /// <param name="buffer">the buffer of data that will be read from / written to</param>
        public BebopView(Span<byte> buffer)
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

        [MethodImpl(HotPath)]
        public ushort ReadUInt16()
        {
            const int size = 2;
            var value = MemoryMarshal.Read<ushort>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public short ReadInt16()
        {
            const int size = 2;
            var value = MemoryMarshal.Read<short>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public uint ReadUInt32()
        {
            const int size = 4;
            var value = MemoryMarshal.Read<uint>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public int ReadInt32()
        {
            const int size = 4;
            var value = MemoryMarshal.Read<int>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public ulong ReadUInt64()
        {
            const int size = 8;
            var value = MemoryMarshal.Read<ulong>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public long ReadInt64()
        {
            const int size = 8;
            var value = MemoryMarshal.Read<long>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public float ReadFloat32()
        {
            const int size = 4;
            var value = MemoryMarshal.Read<float>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public double ReadFloat64()
        {
            const int size = 8;
            var value = MemoryMarshal.Read<double>(_buffer.Slice(Position, size));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public void WriteUInt16(ushort value)
        {
            const int size = 2;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteEnum(Enum value)
        {
            WriteUInt32(Convert.ToUInt32(value));
        }

        [MethodImpl(HotPath)]
        public void WriteFloat32s(float[] value)
        {
            WriteUInt32(unchecked((uint) value.Length));
            var index = Length;
            GrowBy(value.Length);
            MemoryMarshal.AsBytes(value.AsSpan()).CopyTo(_buffer.Slice(index, value.Length));
        }

        [MethodImpl(HotPath)]
        public void WriteFloat64s(double[] value)
        {
            WriteUInt32(unchecked((uint) value.Length));
            var index = Length;
            GrowBy(value.Length);
            MemoryMarshal.AsBytes(value.AsSpan()).CopyTo(_buffer.Slice(index, value.Length));
        }

        [MethodImpl(HotPath)]
        public void WriteInt16(short value)
        {
            const int size = 2;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteUInt32(uint value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteInt32(int value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteUInt64(ulong value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteInt64(long value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteFloat32(float value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        [MethodImpl(HotPath)]
        public void WriteFloat64(double value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index, size), ref value);
        }

        /// <summary>
        ///     Reads a null-terminated string from the underlying buffer
        /// </summary>
        /// <returns>A UTF-16 encoded string</returns>
        [MethodImpl(HotPath)]
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

        [MethodImpl(HotPath)]
        public byte ReadByte() => _buffer[Position++];

        /// <summary>
        ///     Reads a <see cref="Guid"/> from the underlying buffer and advances the advances the current position by 16 bytes.
        /// </summary>
        /// <returns>A well-formed Guid structure instances.</returns>
        [MethodImpl(HotPath)]
        public Guid ReadGuid()
        {
            const int size = 16;
            var index = Position;
            Position += size;
            return MemoryMarshal.Read<Guid>(_buffer.Slice(index, size));
        }

        [MethodImpl(HotPath)]
        public void GrowBy(int amount)
        {
            if ((Length & 0xC0000000) != 0)
            {
                throw new BebopException("A Bebop View cannot grow beyond 2 gigabytes.");
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
        [MethodImpl(HotPath)]
        public byte[] ReadBytes()
        {
            var length = ReadUInt32();
            var data = _buffer.Slice(Position, (int) length).ToArray();
            Position += (int) length;
            return data;
        }

        [MethodImpl(HotPath)]
        public void WriteBytes(byte[] value)
        {
            WriteUInt32(unchecked((uint) value.Length));
            var index = Length;
            GrowBy(value.Length);
            value.AsSpan().CopyTo(_buffer.Slice(index, value.Length));
        }

        [MethodImpl(HotPath)]
        public void WriteGuid(Guid guid)
        {
            const int size = 16;
            var index = Length;
            GrowBy(size);
            MemoryMarshal.Write(_buffer.Slice(index), ref guid);
        }

        /// <summary>
        ///     Writes a UTF-16 encoded string to the underlying buffer
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(HotPath)]
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
                    throw new BebopException("Cannot encode a string containing the null character.");
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

        [MethodImpl(HotPath)]
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
        [MethodImpl(HotPath)]
        public int ReserveMessageLength()
        {
            var i = Length;
            GrowBy(4);
            return i;
        }

        /// <summary>
        ///     Fill in a message's length prefix.
        /// </summary>
        /// <param name="position">The position in the buffer of the message's length prefix.</param>
        /// <param name="messageLength">The message length to write.</param>
        [MethodImpl(HotPath)]
        public void FillMessageLength(int position, uint messageLength)
        {
            MemoryMarshal.Write(_buffer.Slice(position, 4), ref messageLength);
        }

        /// <summary>
        ///     Read out a message's length prefix.
        /// </summary>
        [MethodImpl(HotPath)]
        public uint ReadMessageLength()
        {
            var result = MemoryMarshal.Read<uint>(_buffer.Slice(Position, 4));
            Position += 4;
            return result;
        }
    }
}