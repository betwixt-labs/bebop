from enum import Enum
from python_bebop import BebopWriter, BebopReader
from uuid import UUID
import math
from datetime import datetime

class BasicTypes:
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
    def encode(message: "BasicTypes"):
        writer = BebopWriter()
        BasicTypes.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "BasicTypes", writer: BebopWriter):
        writer.write_bool(message.a_bool)

        writer.write_byte(message.a_byte)

        writer.write_int16(message.a_int16)

        writer.write_uint16(message.a_uint16)

        writer.write_int32(message.a_int32)

        writer.write_uint32(message.a_uint32)

        writer.write_int64(message.a_int64)

        writer.write_uint64(message.a_uint64)

        writer.write_float32(message.a_float32)

        writer.write_float64(message.a_float64)

        writer.write_string(message.a_string)

        writer.write_guid(message.a_guid)

        writer.write_date(message.a_date)

    @classmethod
    def read_from(cls, reader: BebopReader):
        field0 = reader.read_bool()

        field1 = reader.read_byte()

        field2 = reader.read_int16()

        field3 = reader.read_uint16()

        field4 = reader.read_int32()

        field5 = reader.read_uint32()

        field6 = reader.read_int64()

        field7 = reader.read_uint64()

        field8 = reader.read_float32()

        field9 = reader.read_float64()

        field10 = reader.read_string()

        field11 = reader.read_guid()

        field12 = reader.read_date()

        return BasicTypes(a_bool=field0, a_byte=field1, a_int16=field2, a_uint16=field3, a_int32=field4, a_uint32=field5, a_int64=field6, a_uint64=field7, a_float32=field8, a_float64=field9, a_string=field10, a_guid=field11, a_date=field12)

    @staticmethod
    def decode(buffer) -> "BasicTypes":
        return BasicTypes.read_from(BebopReader(buffer))


class M:
    a: float
    b: float


    @staticmethod
    def encode(message: "M"):
        writer = BebopWriter()
        M.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "M", writer: BebopWriter):
        pos = writer.reserve_message_length()
        start = writer.length
        if message.a is not None:
          writer.write_byte(1)
          writer.write_float32(message.a)
        if message.b is not None:
          writer.write_byte(2)
          writer.write_float64(message.b)
        writer.write_byte(0)
        end = writer.length
        writer.fill_message_length(pos, end - start)

    @classmethod
    def read_from(cls, reader: BebopReader):
        message = M()
        length = reader.read_message_length()
        end = reader.index + length
        while True:
          byte = reader.read_byte()
          if byte == 0:
              return message
          elif byte == 1:
              message.a = reader.read_float32()
          elif byte == 2:
              message.b = reader.read_float64()
          else:
              reader.index = end
              return message

    @staticmethod
    def decode(buffer) -> "M":
        return M.read_from(BebopReader(buffer))


class S:
    x: int
    y: int

    def __init__(self,     x: int, y: int    ):
        self.x = x
        self.y = y

    @staticmethod
    def encode(message: "S"):
        writer = BebopWriter()
        S.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "S", writer: BebopWriter):
        writer.write_int32(message.x)

        writer.write_int32(message.y)

    @classmethod
    def read_from(cls, reader: BebopReader):
        field0 = reader.read_int32()

        field1 = reader.read_int32()

        return S(x=field0, y=field1)

    @staticmethod
    def decode(buffer) -> "S":
        return S.read_from(BebopReader(buffer))


class SomeMaps:
    m1: dict[bool, bool]
    m2: dict[str, dict[str, str]]
    m3: list[dict[int, list[dict[bool, S]]]]
    m4: list[dict[str, list[float]]]
    m5: dict[UUID, M]

    def __init__(self,     m1: dict[bool, bool], m2: dict[str, dict[str, str]], m3: list[dict[int, list[dict[bool, S]]]], m4: list[dict[str, list[float]]], m5: dict[UUID, M]    ):
        self.m1 = m1
        self.m2 = m2
        self.m3 = m3
        self.m4 = m4
        self.m5 = m5

    @staticmethod
    def encode(message: "SomeMaps"):
        writer = BebopWriter()
        SomeMaps.encode_into(message, writer)
        return writer.to_list()


    @staticmethod
    def encode_into(message: "SomeMaps", writer: BebopWriter):
        writer.write_uint32(len(message.m1))
        for key0, val0 in message.m1.items():
            writer.write_bool(key0)
            writer.write_bool(val0)

        writer.write_uint32(len(message.m2))
        for key0, val0 in message.m2.items():
            writer.write_string(key0)
            writer.write_uint32(len(val0))
            for key1, val1 in val0.items():
                writer.write_string(key1)
                writer.write_string(val1)

        length0 = len(message.m3)
        writer.write_uint32(length0)
        for i0 in range(length0):
            writer.write_uint32(len(message.m3[i0]))
            for key1, val1 in message.m3[i0].items():
                writer.write_int32(key1)
                length2 = len(val1)
                writer.write_uint32(length2)
                for i2 in range(length2):
                    writer.write_uint32(len(val1[i2]))
                    for key3, val3 in val1[i2].items():
                        writer.write_bool(key3)
                        S.encode_into(val3, writer)

        length0 = len(message.m4)
        writer.write_uint32(length0)
        for i0 in range(length0):
            writer.write_uint32(len(message.m4[i0]))
            for key1, val1 in message.m4[i0].items():
                writer.write_string(key1)
                length2 = len(val1)
                writer.write_uint32(length2)
                for i2 in range(length2):
                    writer.write_float32(val1[i2])

        writer.write_uint32(len(message.m5))
        for key0, val0 in message.m5.items():
            writer.write_guid(key0)
            M.encode_into(val0, writer)

    @classmethod
    def read_from(cls, reader: BebopReader):
        length0 = reader.read_uint32()
        field0 = {}
        for i0 in range(length0):
            k0 = reader.read_bool()
            v0 = reader.read_bool()
            field0[k0] = v0

        length0 = reader.read_uint32()
        field1 = {}
        for i0 in range(length0):
            k0 = reader.read_string()
            length1 = reader.read_uint32()
            v0 = {}
            for i1 in range(length1):
                k1 = reader.read_string()
                v1 = reader.read_string()
                v0[k1] = v1

            field1[k0] = v0

        length0 = reader.read_uint32()
        field2 = []
        for i0 in range(length0):
            length1 = reader.read_uint32()
            x0 = {}
            for i1 in range(length1):
                k1 = reader.read_int32()
                length2 = reader.read_uint32()
                v1 = []
                for i2 in range(length2):
                    length3 = reader.read_uint32()
                    x2 = {}
                    for i3 in range(length3):
                        k3 = reader.read_bool()
                        v3 = S.read_from(reader)
                        x2[k3] = v3

                    v1.append(x2)

                x0[k1] = v1

            field2.append(x0)

        length0 = reader.read_uint32()
        field3 = []
        for i0 in range(length0):
            length1 = reader.read_uint32()
            x0 = {}
            for i1 in range(length1):
                k1 = reader.read_string()
                length2 = reader.read_uint32()
                v1 = []
                for i2 in range(length2):
                    x2 = reader.read_float32()
                    v1.append(x2)

                x0[k1] = v1

            field3.append(x0)

        length0 = reader.read_uint32()
        field4 = {}
        for i0 in range(length0):
            k0 = reader.read_guid()
            v0 = M.read_from(reader)
            field4[k0] = v0

        return SomeMaps(m1=field0, m2=field1, m3=field2, m4=field3, m5=field4)

    @staticmethod
    def decode(buffer) -> "SomeMaps":
        return SomeMaps.read_from(BebopReader(buffer))


