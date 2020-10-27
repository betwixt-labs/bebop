using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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
        public const MethodImplOptions HotPath =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    #else
        public const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
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


        public static BebopView From(ReadOnlyMemory<byte> buffer) => From(buffer.Span);

        public static BebopView From(ReadOnlySpan<byte> buffer) => new BebopView(buffer.ToArray().AsSpan());

        public static BebopView From(Memory<byte> buffer) => new BebopView(buffer.Span);

        public static BebopView From(byte[] buffer) => new BebopView(buffer);

        public static BebopView From(Span<byte> buffer) => new BebopView(buffer);



        /// <summary>
        ///     Creates a read-only slice of the underlying <see cref="_buffer"/> containing all currently written data.
        /// </summary>
        [MethodImpl(HotPath)]
        public ReadOnlySpan<byte> Slice() => _buffer.Slice(0, Length);

        /// <summary>
        /// Copies the contents of <see cref="Slice"/> into a new array.  This heap
        /// allocates, so should generally be avoided for writing and only used when setting a decoded message property.
        /// </summary>
        [MethodImpl(HotPath)]
        public byte[] ToArray() => Slice().ToArray();


        /// <summary>
        ///     Creates a new Bebop view
        /// </summary>
        /// <param name="buffer">the buffer of data that will be read from / written to</param>
        private BebopView(Span<byte> buffer)
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
            unsafe
            {
                var startIndex = Position;
                // eat bytes until we find the null terminator
                while (ReadByte() != 0) { /*NOOP*/ }

                var stringByteCount = unchecked(Position - startIndex - 1);
                fixed (byte* o = &_buffer.Slice(startIndex, stringByteCount).GetPinnableReference())
                {
                    var charCount = Encoding.UTF8.GetMaxCharCount(stringByteCount);
                    fixed (char* c = new char[charCount])
                    {
                        var writtenChars = Encoding.UTF8.GetChars(o, stringByteCount, c, charCount);
                        return new string(c, 0, writtenChars);
                    }
                }
            }
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

        [MethodImpl(HotPath)]
        public void WriteString(string value)
        {
            unsafe
            {
                fixed (char* c = value)
                {
                    var size = Encoding.UTF8.GetByteCount(c, value.Length);
                    var index = Length;
                    GrowBy(size);
                    fixed (byte* o = &_buffer.Slice(index, size).GetPinnableReference())
                    {
                        Encoding.UTF8.GetBytes(c, value.Length, o, size);
                        WriteByte(0);
                    }
                }
            }
        }

        [MethodImpl(HotPath)]
        public void WriteByte(bool value)
        {
            var index = Length;
            GrowBy(1);
            _buffer[index] = (!value ? 0 : 1);
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