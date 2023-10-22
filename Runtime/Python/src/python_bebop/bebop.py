from struct import pack
from uuid import UUID
from datetime import datetime


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
        self.index += 1
        return self._buffer[self.index]

    def read_uint16(self):
        v = self._buffer[self.index : self.index + 2]
        self.index += 2
        return int(v)

    read_int16 = read_uint16

    def read_uint32(self):
        v = self._buffer[self.index : self.index + 4]
        self.index += 4
        return int(v)

    read_int32 = read_uint32

    def read_uint64(self):
        v = self._buffer[self.index : self.index + 8]
        self.index += 8
        return int(v)

    read_int64 = read_uint64

    def read_float32(self):
        v = self._buffer[self.index : self.index + 4]
        self.index += 4
        return float(v)

    def read_float64(self):
        v = self._buffer[self.index : self.index + 8]
        self.index += 8
        return float(v)

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
        return str(v)

    def read_guid(self) -> UUID:
        g = UUID(bytes_le=self._buffer[self.index : self.index + 16])
        self.index += 16
        return g

    def read_date(self) -> datetime:
        low = self.read_uint32()
        high = self.read_uint32() & 0x3FFFFFFF
        msSince1AD = 429496.7296 * high + 0.0001 * low
        return datetime.fromtimestamp(round(msSince1AD - 62135596800000))

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

    def write_byte(self, val: bytes):
        self._buffer.append(val)

    def write_uint16(self, val: int):
        self._buffer.append(pack("<I", val))

    def write_int16(self, val: int):
        self._buffer.append(pack("<i", val))

    def write_uint32(self, val: int):
        self._buffer.append(pack("<L", val))

    def write_int32(self, val: int):
        self._buffer.append(pack("<l", val))

    def write_uint64(self, val: int):
        self._buffer.append(pack("<Q", val))

    def write_int64(self, val: int):
        self._buffer.append(pack("<q", val))

    def write_float32(self, val: float):
        self._buffer.append(pack("<f", val))

    def write_float64(self, val: float):
        self._buffer.append(pack("<D", val))

    def write_bool(self, val: bool):
        self.write_byte(pack("<?", val))

    def write_bytes(self, val: bytearray):
        byte_count = len(val)
        self.write_uint32(byte_count)
        if byte_count == 0:
            return
        self._buffer.extend(val)

    def write_string(self, val: str):
        if len(val) == 0:
            self.write_uint32(0)
            return
        self.write_bytes(val.encode())

    def write_guid(self, guid: UUID):
        self.write_bytes(guid.bytes_le)

    def write_date(self, date: datetime):
        ms = date.microsecond / 1000
        msSince1AD = ms + 62135596800000
        low = round(msSince1AD % 429496.7296 * 10000)
        high = round(msSince1AD / 429496.7296) | 0x40000000
        self.write_uint32(low)
        self.write_uint32(high)

    def write_enum(self, val: int):
        self.write_uint32(val)

    def fill_message_length(self, position: int, message_length: int):
        self._buffer[position] = pack("<i", message_length)

    def to_list(self):
        return list(self._buffer)
