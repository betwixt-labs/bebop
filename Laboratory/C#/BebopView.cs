using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Bebop.Exceptions;
using static System.Runtime.CompilerServices.Unsafe;
using static System.Runtime.InteropServices.MemoryMarshal;

namespace Bebop
{
    /// <summary>
    ///     A low-level interface for encoding and decoding Bebop structured data
    ///     TODO disable big-endian systems
    /// </summary>
    public ref struct BebopView
    {
        private static readonly UTF8Encoding UTF8 = new UTF8Encoding();


        const int MaxStackSize = 256;
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
        public static BebopView Create() => new(Array.Empty<byte>());

        [MethodImpl(HotPath)]
        public static BebopView From(Memory<byte> buffer) => From(buffer.Span);

        [MethodImpl(HotPath)]
        public static BebopView From(byte[] buffer) => new(buffer);

        [MethodImpl(HotPath)]
        public static BebopView From(Span<byte> buffer) => new(buffer);


        /// <summary>
        ///     Creates a read-only slice of the underlying <see cref="_buffer"/> containing all currently written data.
        /// </summary>
        [MethodImpl(HotPath)]
        public ReadOnlySpan<byte> Slice() => _buffer.Slice(0, Length);

        /// <summary>
        ///     Copies the contents of <see cref="Slice"/> into a new array.  This heap
        ///     allocates, so should generally be avoided for writing and only used when setting a decoded message property.
        /// </summary>
        [MethodImpl(HotPath)]
        public byte[] ToArray() => Slice().ToArray();

        private BebopView(Span<byte> buffer)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new BebopViewException("Big-endian systems are not supported by Bebop.");
            }
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
            var value = ReadUnaligned<ushort>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public short ReadInt16()
        {
            const int size = 2;
            var value = ReadUnaligned<short>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public uint ReadUInt32()
        {
            const int size = 4;
            var value = ReadUnaligned<uint>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public int ReadInt32()
        {
            const int size = 4;
            var value = ReadUnaligned<int>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public ulong ReadUInt64()
        {
            const int size = 8;
            var value = ReadUnaligned<ulong>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public long ReadInt64()
        {
            const int size = 8;
            var value = ReadUnaligned<long>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public float ReadFloat32()
        {
            const int size = 4;
            var value = ReadUnaligned<float>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public double ReadFloat64()
        {
            const int size = 8;
            var value = ReadUnaligned<double>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return value;
        }

        [MethodImpl(HotPath)]
        public void WriteUInt16(ushort value)
        {
            const int size = 2;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        private static uint ConvertEnum<T>(T enumValue) where T : struct, Enum =>
           As<T, uint>(ref enumValue);

        [MethodImpl(HotPath)]
        private static T ConvertFrom<T>(uint constant) where T : struct, Enum =>
            As<uint, T>(ref constant);

        [MethodImpl(HotPath)]
        public T ReadEnum<T>() where T : struct, Enum
        {
           return ConvertFrom<T>(ReadUInt32());
        }

        [MethodImpl(HotPath)]
        public void WriteEnum<T>(T value) where T : struct, Enum
        {
            WriteUInt32(ConvertEnum<T>(value));
        }

        [MethodImpl(HotPath)]
        public void WriteFloat32s(float[] value)
        {
            WriteUInt32(unchecked((uint) value.Length));
            var index = Length;
            GrowBy(value.Length);
            AsBytes(value.AsSpan()).CopyTo(_buffer.Slice(index, value.Length));
        }

        [MethodImpl(HotPath)]
        public void WriteFloat64s(double[] value)
        {
            WriteUInt32(unchecked((uint) value.Length));
            var index = Length;
            GrowBy(value.Length);
            AsBytes(value.AsSpan()).CopyTo(_buffer.Slice(index, value.Length));
        }

        [MethodImpl(HotPath)]
        public void WriteInt16(short value)
        {
            const int size = 2;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public void WriteUInt32(uint value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public void WriteInt32(int value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public void WriteUInt64(ulong value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public void WriteInt64(long value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public void WriteFloat32(float value)
        {
            const int size = 4;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public void WriteFloat64(double value)
        {
            const int size = 8;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), value);
        }

        [MethodImpl(HotPath)]
        public DateTime ReadDate()
        {
            return DateTime.FromBinary(ReadInt64());
        }

        /// <summary>
        ///     Reads a null-terminated string from the underlying buffer
        /// </summary>
        /// <returns>A UTF-16 encoded string</returns>
        [MethodImpl(HotPath)]
        public string ReadString()
        {
            var stringByteCount = unchecked((int) ReadUInt32());
            var stringSpan = _buffer.Slice(Position, stringByteCount);
            Position += stringByteCount;


        #if AGGRESSIVE_OPTIMIZE
            return UTF8.GetString(stringSpan);
        #else
            unsafe
            {
                fixed (byte* bytePtr = stringSpan)
                {
                    return UTF8.GetString(bytePtr, stringByteCount);
                }
            }
        #endif
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
            return ReadUnaligned<Guid>(ref GetReference(_buffer.Slice(index, size)));
        }

        [MethodImpl(HotPath)]
        public void GrowBy(int amount)
        {
            if ((Length & 0xC0000000) != 0)
            {
                throw new BebopViewException("A Bebop View cannot grow beyond 2 gigabytes.");
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
            var length = unchecked((int) ReadUInt32());
            var index = Position;
            Position += length;
            return _buffer.Slice(index, length).ToArray();
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
        public void WriteDate(DateTime date)
        {
            WriteInt64(date.ToUniversalTime().ToBinary());
        }

        [MethodImpl(HotPath)]
        public void WriteGuid(Guid guid)
        {
            const int size = 16;
            var index = Length;
            GrowBy(size);
            WriteUnaligned(ref GetReference(_buffer.Slice(index, size)), guid);
        }

        [MethodImpl(HotPath)]
        public void WriteString(string value)
        {
            unsafe
            {
                fixed (char* c = value)
                {
                    var size = UTF8.GetByteCount(c, value.Length);
                    WriteUInt32(unchecked((uint) size));
                    var index = Length;
                    GrowBy(size);
                    fixed (byte* o = _buffer.Slice(index, size))
                    {
                        UTF8.GetBytes(c, value.Length, o, size);
                    }
                }
            }
        }

        [MethodImpl(HotPath)]
        public void WriteByte(bool value)
        {
            const int size = 1;
            var index = Length;
            GrowBy(size);
            _buffer[index] = !value ? 0 : 1;
        }

        [MethodImpl(HotPath)]
        public void WriteByte(byte value)
        {
            const int size = 1;

            var index = Length;
            GrowBy(size);
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
            const int size = 4;
            var i = Length;
            GrowBy(size);
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
            const int size = 4;
            WriteUnaligned(ref GetReference(_buffer.Slice(position, size)), messageLength);
        }

        /// <summary>
        ///     Read out a message's length prefix.
        /// </summary>
        [MethodImpl(HotPath)]
        public uint ReadMessageLength()
        {
            const int size = 4;
            var result = ReadUnaligned<uint>(ref GetReference(_buffer.Slice(Position, size)));
            Position += size;
            return result;
        }
    }
}