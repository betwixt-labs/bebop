from uuid import UUID
from datetime import datetime

"""
A wrapper around a bytearray for reading Bebop base types from it.

It is used by the code that `bebopc --lang python` generates. 
You shouldn't need to use it directly.
"""
class BebopReader:

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
        self.index += 1
        return self._buffer[self.index]

    def read_uint16(self):
        v = self._buffer[self.index:self.index+2]
        self.index += 2
        return int(v)

    read_int16 = read_uint16

    def read_uint32(self):
        v = self._buffer[self.index:self.index+4]
        self.index += 4
        return int(v)

    read_int32 = read_uint32

    def read_uint64(self):
        v = self._buffer[self.index:self.index+8]
        self.index += 8
        return int(v)

    read_int64 = read_uint64

    def read_float32(self):
        v = self._buffer[self.index:self.index+4]
        self.index += 4
        return float(v)

    def read_float64(self):
        v = self._buffer[self.index:self.index+8]
        self.index += 8
        return float(v)

    def read_bool(self):
        return self.read_byte() != 0

    def read_bytes(self):
        length = self.read_uint32()
        if length == 0:
            return self._emptyByteList
        v = self._buffer[self.index:self.index+length]
        self.index += length
        return v

    def read_string(self):
        length = self.read_uint32()
        if length == 0:
            return self._emptyString
        v = self._buffer[self.index:self.index+length]
        self.index += length
        return str(v)

    def read_guid(self) -> UUID:
        g = UUID(bytes_le=self._buffer[self.index:self.index+16])
        self.index += 16
        return g

    def read_date(self) -> datetime:
        low = self.read_uint32()
        high = self.read_uint32() & 0x3fffffff
        msSince1AD = 429496.7296 * high + 0.0001 * low
        return datetime.fromtimestamp(round(msSince1AD - 62135596800000))

    def read_enum(self, values: list):
        return values[self.read_uint32()]

    read_message_length = read_uint32


"""
A wrapper around a bytearray for writing Bebop base types from it.

It is used by the code that `bebopc --lang dart` generates. 
You shouldn't need to use it directly.
"""
class BebopWriter:

    def __init__(self):
        self._buffer = bytearray(length=256)
        self.length = 0

    def _guarantee_buffer_len(self, length):
        if length > 