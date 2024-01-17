namespace Chord.Common.Extensions;

internal static class BinaryExtensions
{

    public static int ReadVarInt32(this Stream stream)
    {
        int result = 0;
        int shift = 0;
        byte byteVal;
        do
        {
            byteVal = (byte)stream.ReadByte();
            result |= (byteVal & 0x7F) << shift;
            shift += 7;
        } while ((byteVal & 0x80) != 0);
        return result;
    }

    public static sbyte ReadVarInt7(this BinaryReader reader) => (sbyte)(reader.ReadVarInt32() & 0b11111111);
    public static int ReadVarInt32(this BinaryReader reader)
    {
        int result = 0;
        int shift = 0;
        byte byteVal;
        do
        {
            byteVal = reader.ReadByte();
            result |= (byteVal & 0x7F) << shift;
            shift += 7;
        } while ((byteVal & 0x80) != 0);
        return result;
    }


    public static byte ReadVarUInt7(this BinaryReader reader) => (byte)(reader.ReadVarUInt32() & 0b1111111);

    public static uint ReadVarUInt32(this BinaryReader reader)
    {
        var result = 0u;
        var shift = 0;
        while (true)
        {
            uint value = reader.ReadByte();
            result |= ((value & 0x7F) << shift);
            if ((value & 0x80) == 0)
                break;
            shift += 7;
        }

        return result;
    }



    public static void WriteVarInt32(this BinaryWriter writer, int value)
    {
        bool more = true;
        while (more)
        {
            byte byteVal = (byte)(value & 0x7F);
            value >>= 7;
            more = value != 0;
            if (more)
            {
                byteVal |= 0x80;
            }
            writer.Write(byteVal);
        }
    }
}