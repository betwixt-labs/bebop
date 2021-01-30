#pragma once

#include <chrono>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <memory>
#include <string>
#include <vector>

#define BEBOP_ASSUME_LITTLE_ENDIAN 1

namespace bebop {

/// A "tick" is a ten-millionth of a second, or 100ns.
using Tick = std::ratio<1, 10000000>;
using TickDuration = std::chrono::duration<int64_t, Tick>;

namespace {
    /// The number of ticks between 1/1/0001 and 1/1/1970.
    const int64_t ticksBetweenEpochs = 621355968000000000;
}

enum class GuidStyle {
    Dashes,
    NoDashes,
};

#pragma pack(push, 1)
struct Guid {
    /// The GUID data is stored the way it is to match the memory layout
    /// of a _GUID in Windows: the idea is to "trick" P/Invoke into recognizing
    /// our type as corresponding to a .NET "Guid".
    uint32_t m_a;
    uint16_t m_b;
    uint16_t m_c;
    uint8_t m_d;
    uint8_t m_e;
    uint8_t m_f;
    uint8_t m_g;
    uint8_t m_h;
    uint8_t m_i;
    uint8_t m_j;
    uint8_t m_k;

    Guid() = default;
    Guid(const uint8_t* bytes) {
#if BEBOP_ASSUME_LITTLE_ENDIAN
        m_a = *reinterpret_cast<const uint32_t*>(bytes + 0);
        m_b = *reinterpret_cast<const uint16_t*>(bytes + 4);
        m_c = *reinterpret_cast<const uint16_t*>(bytes + 6);
#else
        m_a = bytes[0]
            | (static_cast<uint32_t>(bytes[1]) << 8)
            | (static_cast<uint32_t>(bytes[2]) << 16)
            | (static_cast<uint32_t>(bytes[3]) << 24);
        m_b = bytes[4]
            | (static_cast<uint16_t>(bytes[5]) << 8);
        m_c = bytes[6]
            | (static_cast<uint16_t>(bytes[7]) << 8);
#endif
        m_d = bytes[8];
        m_e = bytes[9];
        m_f = bytes[10];
        m_g = bytes[11];
        m_h = bytes[12];
        m_i = bytes[13];
        m_j = bytes[14];
        m_k = bytes[15];
    }
    Guid(Guid const& other) {
        m_a = other.m_a;
        m_b = other.m_b;
        m_c = other.m_c;
        m_d = other.m_d;
        m_e = other.m_e;
        m_f = other.m_f;
        m_g = other.m_g;
        m_h = other.m_h;
        m_i = other.m_i;
        m_j = other.m_j;
        m_k = other.m_k;
    }

    static Guid fromString(const std::string& string) {
        uint8_t bytes[16];
        const char* s = string.c_str();
        
        for (const auto i : layout) {
            if (i == dash) {
                // Skip over a possible dash in the string.
                if (*s == '-') s++;
            } else {
                // Read two hex digits from the string.
                uint8_t high = *s++;
                uint8_t low = *s++;
                bytes[i] = (asciiToHex[high] << 4) | asciiToHex[low];
            }
        }

        return Guid(bytes);
    }

    std::string toString(GuidStyle style = GuidStyle::Dashes) {
        int size = style == GuidStyle::Dashes ? 36 : 32;
        const char* dash = style == GuidStyle::Dashes ? "-" : "";
        std::unique_ptr<char[]> buffer(new char[size+1]);
        snprintf(buffer.get(), size+1, "%08x%s%04x%s%04x%s%02x%02x%s%02x%02x%02x%02x%02x%02x",
            m_a, dash, m_b, dash, m_c, dash, m_d, m_e, dash, m_f, m_g, m_h, m_i, m_j, m_k);
        return std::string(buffer.get(), buffer.get() + size);
    }

    bool operator<(const Guid& other) const {
        if (m_a < other.m_a) return true;
        if (m_a > other.m_a) return false;
        if (m_b < other.m_b) return true;
        if (m_b > other.m_b) return false;
        if (m_c < other.m_c) return true;
        if (m_c > other.m_c) return false;
        if (m_d < other.m_d) return true;
        if (m_d > other.m_d) return false;
        if (m_e < other.m_e) return true;
        if (m_e > other.m_e) return false;
        if (m_f < other.m_f) return true;
        if (m_f > other.m_f) return false;
        if (m_g < other.m_g) return true;
        if (m_g > other.m_g) return false;
        if (m_h < other.m_h) return true;
        if (m_h > other.m_h) return false;
        if (m_i < other.m_i) return true;
        if (m_i > other.m_i) return false;
        if (m_j < other.m_j) return true;
        if (m_j > other.m_j) return false;
        if (m_k < other.m_k) return true;
        if (m_k > other.m_k) return false;
        return false;
    }

    bool operator==(const Guid& other) const {
        if (m_a != other.m_a) return false;
        if (m_b != other.m_b) return false;
        if (m_c != other.m_c) return false;
        if (m_d != other.m_d) return false;
        if (m_e != other.m_e) return false;
        if (m_f != other.m_f) return false;
        if (m_g != other.m_g) return false;
        if (m_h != other.m_h) return false;
        if (m_i != other.m_i) return false;
        if (m_j != other.m_j) return false;
        if (m_k != other.m_k) return false;
        return true;
    }

private:
    static constexpr int dash = -1;
    static constexpr int layout[] = {3, 2, 1, 0, dash, 5, 4, dash, 7, 6, dash, 8, 9, dash, 10, 11, 12, 13, 14, 15};
    static constexpr char nibbleToHex[16] = {'0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f'};
    static constexpr uint8_t asciiToHex[256] = {
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,  1,  2,  3,  4,  5,  6,  7,  8,  9,  0,  0,  0,  0,  0,  0,
        0, 10, 11, 12, 13, 14, 15,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0, 10, 11, 12, 13, 14, 15,  // and the rest is zeroes
    };
};
#pragma pack(pop)

class BebopReader {
    const uint8_t* m_pointer;
public:
    BebopReader(const uint8_t* buffer) : m_pointer(buffer) {}
    BebopReader(BebopReader const&) = delete;
    void operator=(BebopReader const&) = delete;

    const uint8_t* pointer() const { return m_pointer; }
    void seek(const uint8_t* pointer) { m_pointer = pointer; }

    void skip(size_t amount) { m_pointer += amount; }

    uint8_t readByte() { return *m_pointer++; }

    uint16_t readUint16() {
#if BEBOP_ASSUME_LITTLE_ENDIAN
        const auto v = *reinterpret_cast<const uint16_t*>(m_pointer);
        m_pointer += 2;
        return v;
#else
        const uint16_t b0 = *m_pointer++;
        const uint16_t b1 = *m_pointer++;
        return (b1 << 8) | b0;
#endif
    }

    uint32_t readUint32() {
#if BEBOP_ASSUME_LITTLE_ENDIAN
        const auto v = *reinterpret_cast<const uint32_t*>(m_pointer);
        m_pointer += 4;
        return v;
#else
        const uint32_t b0 = *m_pointer++;
        const uint32_t b1 = *m_pointer++;
        const uint32_t b2 = *m_pointer++;
        const uint32_t b3 = *m_pointer++;
        return (b3 << 24) | (b2 << 16) | (b1 << 8) | b0;
#endif
    }

    uint64_t readUint64() {
#if BEBOP_ASSUME_LITTLE_ENDIAN
        const auto v = *reinterpret_cast<const uint64_t*>(m_pointer);
        m_pointer += 8;
        return v;
#else
        const uint64_t b0 = *m_pointer++;
        const uint64_t b1 = *m_pointer++;
        const uint64_t b2 = *m_pointer++;
        const uint64_t b3 = *m_pointer++;
        const uint64_t b4 = *m_pointer++;
        const uint64_t b5 = *m_pointer++;
        const uint64_t b6 = *m_pointer++;
        const uint64_t b7 = *m_pointer++;
        return (b7 << 0x38) | (b6 << 0x30) | (b5 << 0x28) | (b4 << 0x20) | (b3 << 0x18) | (b2 << 0x10) | (b1 << 0x08) | b0;
#endif
    }

    int16_t readInt16() { return static_cast<uint16_t>(readUint16()); }
    int32_t readInt32() { return static_cast<uint32_t>(readUint32()); }
    int64_t readInt64() { return static_cast<uint64_t>(readUint64()); }

    float readFloat32() {
        const auto v = readUint32();
        return reinterpret_cast<const float&>(v);
    }

    double readFloat64() {
        const auto v = readUint64();
        return reinterpret_cast<const double&>(v);
    }

    bool readBool() {
        return readByte() != 0;
    }

    std::vector<uint8_t> readBytes() {
        const auto length = readUint32();
        std::vector<uint8_t> v(m_pointer, m_pointer + length);
        m_pointer += length;
        return v;
    }

    std::string readString() {
        const auto length = readUint32();
        std::string v(m_pointer, m_pointer + length);
        m_pointer += length;
        return v;
    }

    Guid readGuid() {
        Guid guid { m_pointer };
        m_pointer += 16;
        return guid;
    }

    // Read a date (as ticks since the Unix Epoch).
    TickDuration readDate() {
        const uint64_t ticks = readUint64() & 0x3fffffffffffffff;
        return TickDuration(ticks - ticksBetweenEpochs);
    }

    uint32_t readMessageLength() { return readUint32(); }
};

class BebopWriter {
    std::unique_ptr<std::vector<uint8_t>> m_buffer;
public:
    BebopWriter() : m_buffer(std::make_unique<std::vector<uint8_t>>()) {}
    BebopWriter(BebopWriter const&) = delete;
    void operator=(BebopWriter const&) = delete;

    std::unique_ptr<std::vector<uint8_t>> buffer() {
        return std::move(m_buffer);
    }

    size_t length() { return m_buffer->size(); }

    void writeByte(uint8_t value) { m_buffer->push_back(value); }
    void writeUint16(uint16_t value) {
        m_buffer->push_back(value);
        m_buffer->push_back(value >> 8);
    }
    void writeUint32(uint32_t value) {
        m_buffer->push_back(value);
        m_buffer->push_back(value >> 8);
        m_buffer->push_back(value >> 16);
        m_buffer->push_back(value >> 24);
    }
    void writeUint64(uint64_t value) {
        m_buffer->push_back(value);
        m_buffer->push_back(value >> 0x08);
        m_buffer->push_back(value >> 0x10);
        m_buffer->push_back(value >> 0x18);
        m_buffer->push_back(value >> 0x20);
        m_buffer->push_back(value >> 0x28);
        m_buffer->push_back(value >> 0x30);
        m_buffer->push_back(value >> 0x38);
    }

    void writeInt16(int16_t value) { writeUint16(static_cast<uint16_t>(value)); }
    void writeInt32(int32_t value) { writeUint32(static_cast<uint32_t>(value)); }
    void writeInt64(int64_t value) { writeUint64(static_cast<uint64_t>(value)); }
    void writeFloat32(float value) { writeUint32(*reinterpret_cast<uint32_t*>(&value)); }
    void writeFloat64(double value) { writeUint64(*reinterpret_cast<uint64_t*>(&value)); }
    void writeBool(bool value) { writeByte(value); }

    void writeBytes(std::vector<uint8_t> value) {
        const auto byteCount = value.size();
        writeUint32(byteCount);
        m_buffer->insert(m_buffer->end(), value.begin(), value.end());
    }

    void writeString(std::string value) {
        const auto byteCount = value.size();
        writeUint32(byteCount);
        m_buffer->insert(m_buffer->end(), value.begin(), value.end());
    }

    void writeGuid(Guid value) {
        writeUint32(value.m_a);
        writeUint16(value.m_b);
        writeUint16(value.m_c);
        writeByte(value.m_d);
        writeByte(value.m_e);
        writeByte(value.m_f);
        writeByte(value.m_g);
        writeByte(value.m_h);
        writeByte(value.m_i);
        writeByte(value.m_j);
        writeByte(value.m_k);
    }

    void writeDate(TickDuration duration) {
        writeUint64(duration.count() + ticksBetweenEpochs);
    }

    /// Reserve some space to write a message's length prefix, and return its index.
    /// The length is stored as a little-endian fixed-width unsigned 32-bit integer, so 4 bytes are reserved.
    size_t reserveMessageLength() {
        const auto n = m_buffer->size();
        m_buffer->resize(n + 4);
        return n;
    }

    /// Fill in a message's length prefix.
    void fillMessageLength(size_t position, uint32_t messageLength) {
#if BEBOP_ASSUME_LITTLE_ENDIAN
        *reinterpret_cast<uint32_t*>(m_buffer->data() + position) = messageLength;
#else
        m_buffer[position++] = messageLength;
        m_buffer[position++] = messageLength >> 8;
        m_buffer[position++] = messageLength >> 16;
        m_buffer[position++] = messageLength >> 24;
#endif
    }
};

} // namespace bebop
