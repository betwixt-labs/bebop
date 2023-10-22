from enum import Enum
from python_bebop import BebopWriter, BebopReader
from uuid import UUID
import math
from datetime import datetime

class BasicTypes2:
    a_bool: bool
    a_byte: int
    a_int16: int
    a_uint16: int
    a_int32: int
    a_uint32: int
    a_int64: int
    a_uint64: int
    a_float32: float
    a_float64: float
    a_string: str
    a_guid: UUID
    a_date: datetime

    def __init__(self,     a_bool: bool, a_byte: int, a_int16: int, a_uint16: int, a_int32: int, a_uint32: int, a_int64: int, a_uint64: int, a_float32: float, a_float64: float, a_string: str, a_guid: UUID, a_date: datetime    ):
        self.a_bool = a_bool
        self.a_byte = a_byte
        self.a_int16 = a_int16
        self.a_uint16 = a_uint16
        self.a_int32 = a_int32
        self.a_uint32 = a_uint32
        self.a_int64 = a_int64
        self.a_uint64 = a_uint64
        self.a_float32 = a_float32
        self.a_float64 = a_float64
        self.a_string = a_string
        self.a_guid = a_guid
        self.a_date = a_date

    @staticmethod
    def encode(message: "BasicTypes2"):
        writer = BebopWriter()
        BasicTypes2.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "BasicTypes2", writer: BebopWriter):
        writer.writeBool(message.a_bool)

        writer.writeByte(message.a_byte)

        writer.writeInt16(message.a_int16)

        writer.writeUint16(message.a_uint16)

        writer.writeInt32(message.a_int32)

        writer.writeUint32(message.a_uint32)

        writer.writeInt64(message.a_int64)

        writer.writeUint64(message.a_uint64)

        writer.writeFloat32(message.a_float32)

        writer.writeFloat64(message.a_float64)

        writer.writeString(message.a_string)

        writer.writeGuid(message.a_guid)

        writer.writeDate(message.a_date)

    @classmethod
    def _read_from(cls, reader: BebopReader):
        field0 = reader.readBool()

        field1 = reader.readByte()

        field2 = reader.readInt16()

        field3 = reader.readUint16()

        field4 = reader.readInt32()

        field5 = reader.readUint32()

        field6 = reader.readInt64()

        field7 = reader.readUint64()

        field8 = reader.readFloat32()

        field9 = reader.readFloat64()

        field10 = reader.readString()

        field11 = reader.readGuid()

        field12 = reader.readDate()

        return BasicTypes2(a_bool=field0, a_byte=field1, a_int16=field2, a_uint16=field3, a_int32=field4, a_uint32=field5, a_int64=field6, a_uint64=field7, a_float32=field8, a_float64=field9, a_string=field10, a_guid=field11, a_date=field12)

    @staticmethod
    def decode(buffer) -> "BasicTypes2":
        return BasicTypes2._read_from(BebopReader(buffer))


class BasicArrays2:
    a_bool: list[bool]
    a_byte: bytearray
    a_int16: list[int]
    a_uint16: list[int]
    a_int32: list[int]
    a_uint32: list[int]
    a_int64: list[int]
    a_uint64: list[int]
    a_float32: list[float]
    a_float64: list[float]
    a_string: list[str]
    a_guid: list[UUID]

    def __init__(self,     a_bool: list[bool], a_byte: bytearray, a_int16: list[int], a_uint16: list[int], a_int32: list[int], a_uint32: list[int], a_int64: list[int], a_uint64: list[int], a_float32: list[float], a_float64: list[float], a_string: list[str], a_guid: list[UUID]    ):
        self.a_bool = a_bool
        self.a_byte = a_byte
        self.a_int16 = a_int16
        self.a_uint16 = a_uint16
        self.a_int32 = a_int32
        self.a_uint32 = a_uint32
        self.a_int64 = a_int64
        self.a_uint64 = a_uint64
        self.a_float32 = a_float32
        self.a_float64 = a_float64
        self.a_string = a_string
        self.a_guid = a_guid

    @staticmethod
    def encode(message: "BasicArrays2"):
        writer = BebopWriter()
        BasicArrays2.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "BasicArrays2", writer: BebopWriter):
        length0 = message.a_bool.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeBool(message.a_bool[i0])

        writer.writeBytes(message.a_byte)

        length0 = message.a_int16.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeInt16(message.a_int16[i0])

        length0 = message.a_uint16.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeUint16(message.a_uint16[i0])

        length0 = message.a_int32.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeInt32(message.a_int32[i0])

        length0 = message.a_uint32.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeUint32(message.a_uint32[i0])

        length0 = message.a_int64.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeInt64(message.a_int64[i0])

        length0 = message.a_uint64.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeUint64(message.a_uint64[i0])

        length0 = message.a_float32.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeFloat32(message.a_float32[i0])

        length0 = message.a_float64.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeFloat64(message.a_float64[i0])

        length0 = message.a_string.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeString(message.a_string[i0])

        length0 = message.a_guid.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeGuid(message.a_guid[i0])

    @classmethod
    def _read_from(cls, reader: BebopReader):
        length0 = reader.readUint32()
        field0 = []
        for i0 in range(length0):
            x0 = reader.readBool()
            field0[i0] = x0

        field1 = reader.readBytes()

        length0 = reader.readUint32()
        field2 = []
        for i0 in range(length0):
            x0 = reader.readInt16()
            field2[i0] = x0

        length0 = reader.readUint32()
        field3 = []
        for i0 in range(length0):
            x0 = reader.readUint16()
            field3[i0] = x0

        length0 = reader.readUint32()
        field4 = []
        for i0 in range(length0):
            x0 = reader.readInt32()
            field4[i0] = x0

        length0 = reader.readUint32()
        field5 = []
        for i0 in range(length0):
            x0 = reader.readUint32()
            field5[i0] = x0

        length0 = reader.readUint32()
        field6 = []
        for i0 in range(length0):
            x0 = reader.readInt64()
            field6[i0] = x0

        length0 = reader.readUint32()
        field7 = []
        for i0 in range(length0):
            x0 = reader.readUint64()
            field7[i0] = x0

        length0 = reader.readUint32()
        field8 = []
        for i0 in range(length0):
            x0 = reader.readFloat32()
            field8[i0] = x0

        length0 = reader.readUint32()
        field9 = []
        for i0 in range(length0):
            x0 = reader.readFloat64()
            field9[i0] = x0

        length0 = reader.readUint32()
        field10 = []
        for i0 in range(length0):
            x0 = reader.readString()
            field10[i0] = x0

        length0 = reader.readUint32()
        field11 = []
        for i0 in range(length0):
            x0 = reader.readGuid()
            field11[i0] = x0

        return BasicArrays2(a_bool=field0, a_byte=field1, a_int16=field2, a_uint16=field3, a_int32=field4, a_uint32=field5, a_int64=field6, a_uint64=field7, a_float32=field8, a_float64=field9, a_string=field10, a_guid=field11)

    @staticmethod
    def decode(buffer) -> "BasicArrays2":
        return BasicArrays2._read_from(BebopReader(buffer))


class S2:
    x: int
    y: int

    def __init__(self,     x: int, y: int    ):
        self.x = x
        self.y = y

    @staticmethod
    def encode(message: "S2"):
        writer = BebopWriter()
        S2.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "S2", writer: BebopWriter):
        writer.writeInt32(message.x)

        writer.writeInt32(message.y)

    @classmethod
    def _read_from(cls, reader: BebopReader):
        field0 = reader.readInt32()

        field1 = reader.readInt32()

        return S2(x=field0, y=field1)

    @staticmethod
    def decode(buffer) -> "S2":
        return S2._read_from(BebopReader(buffer))


class SomeMaps2:
    m1: dict[bool, bool]
    m2: dict[str, dict[str, str]]
    m3: list[dict[int, list[dict[bool, S2]]]]
    m4: list[dict[str, list[float]]]

    def __init__(self,     m1: dict[bool, bool], m2: dict[str, dict[str, str]], m3: list[dict[int, list[dict[bool, S2]]]], m4: list[dict[str, list[float]]]    ):
        self.m1 = m1
        self.m2 = m2
        self.m3 = m3
        self.m4 = m4

    @staticmethod
    def encode(message: "SomeMaps2"):
        writer = BebopWriter()
        SomeMaps2.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "SomeMaps2", writer: BebopWriter):
        writer.writeUint32(message.m1.length)
        for e0 in message.m1.entries:
            writer.writeBool(e0.key)
            writer.writeBool(e0.value)

        writer.writeUint32(message.m2.length)
        for e0 in message.m2.entries:
            writer.writeString(e0.key)
            writer.writeUint32(e0.value.length)
            for e1 in e0.value.entries:
                writer.writeString(e1.key)
                writer.writeString(e1.value)

        length0 = message.m3.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeUint32(message.m3[i0].length)
            for e1 in message.m3[i0].entries:
                writer.writeInt32(e1.key)
                length2 = e1.value.length
                writer.writeUint32(length2)
                for i2 in range(length2):
                    writer.writeUint32(e1.value[i2].length)
                    for e3 in e1.value[i2].entries:
                        writer.writeBool(e3.key)
                        S2.encodeInto(e3.value, writer)

        length0 = message.m4.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeUint32(message.m4[i0].length)
            for e1 in message.m4[i0].entries:
                writer.writeString(e1.key)
                length2 = e1.value.length
                writer.writeUint32(length2)
                for i2 in range(length2):
                    writer.writeFloat32(e1.value[i2])

    @classmethod
    def _read_from(cls, reader: BebopReader):
        length0 = reader.readUint32()
        field0 = {}
        for i0 in range(length0):
            k0 = reader.readBool()
            v0 = reader.readBool()
            field0[k0] = v0

        length0 = reader.readUint32()
        field1 = {}
        for i0 in range(length0):
            k0 = reader.readString()
            length1 = reader.readUint32()
            v0 = {}
            for i1 in range(length1):
                k1 = reader.readString()
                v1 = reader.readString()
                v0[k1] = v1

            field1[k0] = v0

        length0 = reader.readUint32()
        field2 = []
        for i0 in range(length0):
            length1 = reader.readUint32()
            x0 = {}
            for i1 in range(length1):
                k1 = reader.readInt32()
                length2 = reader.readUint32()
                v1 = []
                for i2 in range(length2):
                    length3 = reader.readUint32()
                    x2 = {}
                    for i3 in range(length3):
                        k3 = reader.readBool()
                        v3 = S2.readFrom(reader)
                        x2[k3] = v3

                    v1[i2] = x2

                x0[k1] = v1

            field2[i0] = x0

        length0 = reader.readUint32()
        field3 = []
        for i0 in range(length0):
            length1 = reader.readUint32()
            x0 = {}
            for i1 in range(length1):
                k1 = reader.readString()
                length2 = reader.readUint32()
                v1 = []
                for i2 in range(length2):
                    x2 = reader.readFloat32()
                    v1[i2] = x2

                x0[k1] = v1

            field3[i0] = x0

        return SomeMaps2(m1=field0, m2=field1, m3=field2, m4=field3)

    @staticmethod
    def decode(buffer) -> "SomeMaps2":
        return SomeMaps2._read_from(BebopReader(buffer))


class TestInt32Array2:
    a: list[int]

    def __init__(self,     a: list[int]    ):
        self.a = a

    @staticmethod
    def encode(message: "TestInt32Array2"):
        writer = BebopWriter()
        TestInt32Array2.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "TestInt32Array2", writer: BebopWriter):
        length0 = message.a.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeInt32(message.a[i0])

    @classmethod
    def _read_from(cls, reader: BebopReader):
        length0 = reader.readUint32()
        field0 = []
        for i0 in range(length0):
            x0 = reader.readInt32()
            field0[i0] = x0

        return TestInt32Array2(a=field0)

    @staticmethod
    def decode(buffer) -> "TestInt32Array2":
        return TestInt32Array2._read_from(BebopReader(buffer))


class ArrayOfStrings2:
    strings: list[str]

    def __init__(self,     strings: list[str]    ):
        self.strings = strings

    @staticmethod
    def encode(message: "ArrayOfStrings2"):
        writer = BebopWriter()
        ArrayOfStrings2.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "ArrayOfStrings2", writer: BebopWriter):
        length0 = message.strings.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            writer.writeString(message.strings[i0])

    @classmethod
    def _read_from(cls, reader: BebopReader):
        length0 = reader.readUint32()
        field0 = []
        for i0 in range(length0):
            x0 = reader.readString()
            field0[i0] = x0

        return ArrayOfStrings2(strings=field0)

    @staticmethod
    def decode(buffer) -> "ArrayOfStrings2":
        return ArrayOfStrings2._read_from(BebopReader(buffer))


class StructOfStructs:
    basicTypes: BasicTypes2
    basicArrays: BasicArrays2

    def __init__(self,     basicTypes: BasicTypes2, basicArrays: BasicArrays2    ):
        self.basicTypes = basicTypes
        self.basicArrays = basicArrays

    @staticmethod
    def encode(message: "StructOfStructs"):
        writer = BebopWriter()
        StructOfStructs.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "StructOfStructs", writer: BebopWriter):
        BasicTypes2.encodeInto(message.basicTypes, writer)

        BasicArrays2.encodeInto(message.basicArrays, writer)

    @classmethod
    def _read_from(cls, reader: BebopReader):
        field0 = BasicTypes2.readFrom(reader)

        field1 = BasicArrays2.readFrom(reader)

        return StructOfStructs(basicTypes=field0, basicArrays=field1)

    @staticmethod
    def decode(buffer) -> "StructOfStructs":
        return StructOfStructs._read_from(BebopReader(buffer))


class EmptyStruct:


    @staticmethod
    def encode(message: "EmptyStruct"):
        writer = BebopWriter()
        EmptyStruct.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "EmptyStruct", writer: BebopWriter):
        pass

    @classmethod
    def _read_from(cls, reader: BebopReader):
        return EmptyStruct()

    @staticmethod
    def decode(buffer) -> "EmptyStruct":
        return EmptyStruct._read_from(BebopReader(buffer))


class OpcodeStruct:
    x: int
    opcode = 0x444C5257


    def __init__(self,     x: int    ):
        self.x = x

    @staticmethod
    def encode(message: "OpcodeStruct"):
        writer = BebopWriter()
        OpcodeStruct.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "OpcodeStruct", writer: BebopWriter):
        writer.writeInt32(message.x)

    @classmethod
    def _read_from(cls, reader: BebopReader):
        field0 = reader.readInt32()

        return OpcodeStruct(x=field0)

    @staticmethod
    def decode(buffer) -> "OpcodeStruct":
        return OpcodeStruct._read_from(BebopReader(buffer))


class ShouldNotHaveLifetime:
    v: list[OpcodeStruct]

    def __init__(self,     v: list[OpcodeStruct]    ):
        self.v = v

    @staticmethod
    def encode(message: "ShouldNotHaveLifetime"):
        writer = BebopWriter()
        ShouldNotHaveLifetime.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "ShouldNotHaveLifetime", writer: BebopWriter):
        length0 = message.v.length
        writer.writeUint32(length0)
        for i0 in range(length0):
            OpcodeStruct.encodeInto(message.v[i0], writer)

    @classmethod
    def _read_from(cls, reader: BebopReader):
        length0 = reader.readUint32()
        field0 = []
        for i0 in range(length0):
            x0 = OpcodeStruct.readFrom(reader)
            field0[i0] = x0

        return ShouldNotHaveLifetime(v=field0)

    @staticmethod
    def decode(buffer) -> "ShouldNotHaveLifetime":
        return ShouldNotHaveLifetime._read_from(BebopReader(buffer))


