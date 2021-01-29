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

struct Guid {
    /// The GUID bytes, in .NET "ToByteArray" order.
    /// https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-5.0
    uint8_t m_bytes[16];

    Guid() = default;
    Guid(const uint8_t *bytes) { std::memcpy(m_bytes, bytes, 16); }
    Guid(Guid const &other) : Guid(other.m_bytes) {}

    static Guid fromString(const std::string &string) {
        Guid g;
        const char* s = string.c_str();
        for (const auto i : layout) {
            if (i == dash) {
                // Skip over a possible dash in the string.
                if (*s == '-') s++;
            } else {
                // Read two hex digits from the string.
                uint8_t high = *s++;
                uint8_t low = *s++;
                g.m_bytes[i] = (asciiToHex[high] << 4) | asciiToHex[low];
            }
        }

        return g;
    }

    std::string toString(GuidStyle style = GuidStyle::Dashes) {
        std::string result;
        result.reserve(36);

        for (const auto i : layout) {
            if (i == dash) {
                if (style == GuidStyle::Dashes) result += '-';
            } else {
                uint8_t a = m_bytes[i];
                result += nibbleToHex[a >> 4];
                result += nibbleToHex[a & 0xf];
            }
        }

        return result;
    }

    bool operator<(const Guid &other) const {
        for (size_t i = 0; i < 16; i++) {
            if (m_bytes[i] < other.m_bytes[i]) return true;
            if (m_bytes[i] > other.m_bytes[i]) return false;
        }
        return false;
    }

    bool operator==(const Guid &other) const {
        for (size_t i = 0; i < 16; i++) {
            if (m_bytes[i] != other.m_bytes[i]) return false;
        }
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

class BebopReader {
    const uint8_t *m_pointer;
    BebopReader() {}
public:
    BebopReader(BebopReader const&) = delete;
    void operator=(BebopReader const&) = delete;
    static BebopReader& instance(const uint8_t *buffer) {
        static BebopReader instance;
        instance.m_pointer = buffer;
        return instance;
    }

    const uint8_t *pointer() const { return m_pointer; }
    void seek(const uint8_t *pointer) { m_pointer = pointer; }

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
    BebopWriter() {}
public:
    BebopWriter(BebopWriter const&) = delete;
    void operator=(BebopWriter const&) = delete;
    static BebopWriter& instance() {
        static BebopWriter instance;
        instance.m_buffer = std::make_unique<std::vector<uint8_t>>();
        return instance;
    }

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
        for (const auto byte : value.m_bytes) {
            m_buffer->push_back(byte);
        }
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
