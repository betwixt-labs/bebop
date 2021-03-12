#pragma once
#include <cstddef>
#include <cstdint>
#include <map>
#include <memory>
#include <optional>
#include <string>
#include <variant>
#include <vector>
#include "bebop.hpp"

struct BasicArrays {
  static const size_t minimalEncodedSize = 48;
  std::vector<bool> a_bool;
  std::vector<uint8_t> a_byte;
  std::vector<int16_t> a_int16;
  std::vector<uint16_t> a_uint16;
  std::vector<int32_t> a_int32;
  std::vector<uint32_t> a_uint32;
  std::vector<int64_t> a_int64;
  std::vector<uint64_t> a_uint64;
  std::vector<float> a_float32;
  std::vector<double> a_float64;
  std::vector<std::string> a_string;
  std::vector<::bebop::Guid> a_guid;

  static void encodeInto(const BasicArrays& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    BasicArrays::encodeInto(message, writer);
  }

  template<typename T = ::bebop::Writer> static void encodeInto(const BasicArrays& message, T& writer) {
    {
      const auto length0 = message.a_bool.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_bool) {
        writer.writeBool(i0);
      }
    }
    writer.writeBytes(message.a_byte);
    {
      const auto length0 = message.a_int16.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_int16) {
        writer.writeInt16(i0);
      }
    }
    {
      const auto length0 = message.a_uint16.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_uint16) {
        writer.writeUint16(i0);
      }
    }
    {
      const auto length0 = message.a_int32.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_int32) {
        writer.writeInt32(i0);
      }
    }
    {
      const auto length0 = message.a_uint32.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_uint32) {
        writer.writeUint32(i0);
      }
    }
    {
      const auto length0 = message.a_int64.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_int64) {
        writer.writeInt64(i0);
      }
    }
    {
      const auto length0 = message.a_uint64.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_uint64) {
        writer.writeUint64(i0);
      }
    }
    {
      const auto length0 = message.a_float32.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_float32) {
        writer.writeFloat32(i0);
      }
    }
    {
      const auto length0 = message.a_float64.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_float64) {
        writer.writeFloat64(i0);
      }
    }
    {
      const auto length0 = message.a_string.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_string) {
        writer.writeString(i0);
      }
    }
    {
      const auto length0 = message.a_guid.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a_guid) {
        writer.writeGuid(i0);
      }
    }
  }

  static BasicArrays decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    BasicArrays result;
    BasicArrays::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static BasicArrays decode(std::vector<uint8_t> sourceBuffer) {
    return BasicArrays::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static BasicArrays decode(::bebop::Reader& reader) {
    BasicArrays result;
    BasicArrays::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, BasicArrays& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    BasicArrays::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, BasicArrays& target) {
    BasicArrays::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, BasicArrays& target) {
    {
      const auto length0 = reader.readUint32();
      target.a_bool = std::vector<bool>();
      target.a_bool.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        bool x0;
        x0 = reader.readBool();
        target.a_bool.push_back(x0);
      }
    }
    target.a_byte = reader.readBytes();
    {
      const auto length0 = reader.readUint32();
      target.a_int16 = std::vector<int16_t>();
      target.a_int16.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        int16_t x0;
        x0 = reader.readInt16();
        target.a_int16.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_uint16 = std::vector<uint16_t>();
      target.a_uint16.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        uint16_t x0;
        x0 = reader.readUint16();
        target.a_uint16.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_int32 = std::vector<int32_t>();
      target.a_int32.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        int32_t x0;
        x0 = reader.readInt32();
        target.a_int32.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_uint32 = std::vector<uint32_t>();
      target.a_uint32.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        uint32_t x0;
        x0 = reader.readUint32();
        target.a_uint32.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_int64 = std::vector<int64_t>();
      target.a_int64.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        int64_t x0;
        x0 = reader.readInt64();
        target.a_int64.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_uint64 = std::vector<uint64_t>();
      target.a_uint64.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        uint64_t x0;
        x0 = reader.readUint64();
        target.a_uint64.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_float32 = std::vector<float>();
      target.a_float32.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        float x0;
        x0 = reader.readFloat32();
        target.a_float32.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_float64 = std::vector<double>();
      target.a_float64.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        double x0;
        x0 = reader.readFloat64();
        target.a_float64.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_string = std::vector<std::string>();
      target.a_string.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        std::string x0;
        x0 = reader.readString();
        target.a_string.push_back(x0);
      }
    }
    {
      const auto length0 = reader.readUint32();
      target.a_guid = std::vector<::bebop::Guid>();
      target.a_guid.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        ::bebop::Guid x0;
        x0 = reader.readGuid();
        target.a_guid.push_back(x0);
      }
    }
  }

  void encodeInto(std::vector<uint8_t>& targetBuffer) { BasicArrays::encodeInto(*this, targetBuffer); }
  void encodeInto(::bebop::Writer& writer) { BasicArrays::encodeInto(*this, writer); }

  size_t byteCount() {
    ::bebop::ByteCounter counter{};
    BasicArrays::encodeInto<::bebop::ByteCounter>(*this, counter);
    return counter.length();
  }
};

struct TestInt32Array {
  static const size_t minimalEncodedSize = 4;
  std::vector<int32_t> a;

  static void encodeInto(const TestInt32Array& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    TestInt32Array::encodeInto(message, writer);
  }

  template<typename T = ::bebop::Writer> static void encodeInto(const TestInt32Array& message, T& writer) {
    {
      const auto length0 = message.a.size();
      writer.writeUint32(length0);
      for (const auto& i0 : message.a) {
        writer.writeInt32(i0);
      }
    }
  }

  static TestInt32Array decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    TestInt32Array result;
    TestInt32Array::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static TestInt32Array decode(std::vector<uint8_t> sourceBuffer) {
    return TestInt32Array::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static TestInt32Array decode(::bebop::Reader& reader) {
    TestInt32Array result;
    TestInt32Array::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, TestInt32Array& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    TestInt32Array::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, TestInt32Array& target) {
    TestInt32Array::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, TestInt32Array& target) {
    {
      const auto length0 = reader.readUint32();
      target.a = std::vector<int32_t>();
      target.a.reserve(length0);
      for (size_t i0 = 0; i0 < length0; i0++) {
        int32_t x0;
        x0 = reader.readInt32();
        target.a.push_back(x0);
      }
    }
  }

  void encodeInto(std::vector<uint8_t>& targetBuffer) { TestInt32Array::encodeInto(*this, targetBuffer); }
  void encodeInto(::bebop::Writer& writer) { TestInt32Array::encodeInto(*this, writer); }

  size_t byteCount() {
    ::bebop::ByteCounter counter{};
    TestInt32Array::encodeInto<::bebop::ByteCounter>(*this, counter);
    return counter.length();
  }
};

