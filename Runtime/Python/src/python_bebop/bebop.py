from struct import pack, unpack
from uuid import UUID
from datetime import datetime
from enum import Enum

ticksBetweenEpochs = 621355968000000000
dateMask = 0x3fffffffffffffff

class BebopReader:
    """
    A wrapper around a bytearray for reading Bebop base types from it.

    It is used by the code that `bebopc --lang python` generates. 
    You shouldn't need to use it directly.
    """

    _emptyByteList = bytearray()
    _emptyString = ""

    def __init__(self, buffer=None):
        self._buffer = buffer if buffer is not None else bytearray()
        self.index = 0

    @classmethod
    def from_buffer(cls, buffer: bytearray):
        return cls(buffer)

    def _skip(self, amount: int):
        self.index += amount

    def read_byte(self):
        byte = self._buffer[self.index]
        self.index += 1
        return byte

    def read_uint16(self):
        v = self._buffer[self.index : self.index + 2]
        self.index += 2
        return int.from_bytes(v, byteorder="little")

    def read_int16(self):
        v = self._buffer[self.index : self.index + 2]
        self.index += 2
        return int.from_bytes(v, byteorder="little", signed=True)

    def read_uint32(self):
        v = self._buffer[self.index : self.index + 4]
        self.index += 4
        return int.from_bytes(v, byteorder="little")

    def read_int32(self):
        v = self._buffer[self.index : self.index + 4]
        self.index += 4
        return int.from_bytes(v, byteorder="little", signed=True)

    def read_uint64(self):
        v = self._buffer[self.index : self.index + 8]
        self.index += 8
        return int.from_bytes(v, byteorder="little")

    def read_int64(self):
        v = self._buffer[self.index : self.index + 8]
        self.index += 8
        return int.from_bytes(v, byteorder="little", signed=True)

    def read_float32(self):
        v = self._buffer[self.index : self.index + 4]
        self.index += 4
        return unpack("<f", bytearray(v))[0]

    def read_float64(self):
        v = self._buffer[self.index : self.index + 8]
        self.index += 8
        return unpack("<d", bytearray(v))[0]

    def read_bool(self):
        return self.read_byte() != 0

    def read_bytes(self):
        length = self.read_uint32()
        if length == 0:
            return self._emptyByteList
        v = self._buffer[self.index : self.index + length]
        self.index += length
        return v

    def read_string(self):
        length = self.read_uint32()
        if length == 0:
            return self._emptyString
        v = self._buffer[self.index : self.index + length]
        self.index += length
        return "".join([chr(c) for c in v])

    def read_guid(self) -> UUID:
        g = UUID(bytes_le=bytes(self._buffer[self.index : self.index + 16]))
        self.index += 16
        return g

    def read_date(self) -> datetime:
        ticks = self.read_uint64() & dateMask
        ms = (ticks - ticksBetweenEpochs)
        return datetime.fromtimestamp(ms)

    def read_enum(self, values: list):
        return values[self.read_uint32()]

    read_message_length = read_uint32


class BebopWriter:
    """
    A wrapper around a bytearray for writing Bebop base types from it.

    It is used by the code that `bebopc --lang python` generates. 
    You shouldn't need to use it directly.
    """

    def __init__(self):
        self._buffer = bytearray()
        self.length = 0

    def _guarantee_buffer_length(self):
        """
        Replace _buffer with a bytearray twice the size required to ensure buffer is always big enough
        """
        if self.length > len(self._buffer):
            new_length = min(2 * len(self._buffer), self.length)
            data = bytearray([0] * new_length)
            data[:len(self._buffer)] = self._buffer
            self._buffer = data

    def _grow_by(self, amount: int):
        self.length += amount
        self._guarantee_buffer_length()

    def write_byte(self, val: bytes):
        self.length += 1
        self._buffer.append(val)

    def write_uint16(self, val: int):
        self.length += 2
        self._buffer += pack("<H", val)

    def write_int16(self, val: int):
        self.length += 2
        self._buffer += pack("<h", val)

    def write_uint32(self, val: int):
        self.length += 4
        self._buffer += pack("<I", val)

    def write_int32(self, val: int):
        self.length += 4
        self._buffer += pack("<i", val)

    def write_uint64(self, val: int):
        self.length += 8
        self._buffer += pack("<Q", val)

    def write_int64(self, val: int):
        self.length += 8
        self._buffer += pack("<q", val)

    def write_float32(self, val: float):
        self.length += 4
        self._buffer += pack("<f", val)

    def write_float64(self, val: float):
        self.length += 8
        self._buffer += pack("<d", val)

    def write_bool(self, val: bool):
        self.write_byte(val)

    def write_bytes(self, val: bytearray, write_msg_length: bool = True):
        byte_count = len(val)
        if write_msg_length:
            self.write_uint32(byte_count)
        if byte_count == 0:
            return
        self.length += len(val)
        self._buffer.extend(val)

    def write_string(self, val: str):
        if len(val) == 0:
            self.write_uint32(0)
            return
        self.write_bytes(val.encode())

    def write_guid(self, guid: UUID):
        self.write_bytes(guid.bytes_le, write_msg_length=False)

    def write_date(self, date: datetime):
        ms = int(datetime.timestamp(date))
        ticks = ms + ticksBetweenEpochs
        self.write_uint64(ticks & dateMask)

    def write_enum(self, val: Enum):
        self.write_uint32(val.value)

    def reserve_message_length(self):
        """
        Reserve some space to write a message's length prefix, and return its index.
        The length is stored as a little-endian fixed-width unsigned 32-bit integer, so 4 bytes are reserved.
        """
        i = self.length
        self._grow_by(4)
        return i

    def fill_message_length(self, position: int, message_length: int):
        self._buffer[position:position+2] = message_length.to_bytes(2, "little")

    def to_list(self):
        return list(self._buffer)
