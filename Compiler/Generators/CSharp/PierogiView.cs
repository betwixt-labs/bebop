using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace Compiler.Generators.CSharp
{
    /// <summary>
    ///     A low-level interface for encoding and decoding Pierogi structured data
    /// </summary>
    public ref struct PierogiView
    {
        /// <summary>
        /// </summary>
        private Span<byte> _buffer;

        public static PierogiView Create() => new PierogiView(Array.Empty<byte>());

        public byte[] ToArray() => _buffer.Slice(0, Length).ToArray();

        public PierogiView(byte[] buffer)
        {
            _buffer = buffer;
            Position = 0;
            Length = _buffer.Length;
        }

        public int Position { get; set; }
        public int Length { get; set; }

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
        public int ReadInt32()
        {
            var value = ReadUInt32() | 0;
            return unchecked((int) ((value & 1) != 0 ? ~(value >> 1) : value >> 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public uint ReadUInt32()
        {
            var count = 0;
            var shift = 0;
            byte b;
            do
            {
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return unchecked((uint) count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public byte ReadByte() => _buffer[Position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Guid ReadGuid()
        {
            const int size = 16;
            var guid = new Guid(_buffer.Slice(Position, size));
            Position += size;
            return guid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public long ReadInt64()
        {
            var value = ReadUInt64();
            var half = value >> 1;
            return unchecked((long) ((value & 1) != 0 ? ~half : half));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ulong ReadUInt64()
        {
            long count = 0;
            var shift = 0;
            byte b;
            do
            {
                b = ReadByte();

                count |= (long) (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return unchecked((ulong) count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public short ReadInt16()
        {
            var value = ReadUInt32() | 0;
            return unchecked((short) ((value & 1) != 0 ? ~(value >> 1) : value >> 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ushort ReadUInt16()
        {
            var value = ReadUInt32() | 0;
            return unchecked((ushort) value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void GrowBy(int amount)
        {
            if ((Length & 0xC0000000) != 0)
            {
                throw new Exception("ByteBuffer: cannot grow buffer beyond 2 gigabytes.");
            }
            if (Length + amount > _buffer.Length)
            {
                var newBuffer = new Span<byte>(new byte[(Length + amount) << 1]);
                _buffer.CopyTo(newBuffer);
                _buffer = newBuffer;
            }
            Length += amount;
        }

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
           guid.ToByteArray().AsSpan().CopyTo(_buffer.Slice(index - 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint) ((value << 1) ^ (value >> 31))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteUInt32(uint value)
        {
            do
            {
                var b = value & 127;
                value >>= 7;
                WriteByte(unchecked((byte) (value > 0 ? b | 128 : b)));
            } while (value > 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteString(string value)
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
                    throw new Exception("Cannot encode a string containing the null character");
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
    }
}