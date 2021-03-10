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

/// Option B: an "encodedData" field, that "decode" is called a second time on.
struct B {
  static const size_t minimalEncodedSize = 12;
  uint32_t protocolVersion;
  uint32_t incomingOpcode;
  std::vector<uint8_t> encodedData;

  static void encodeInto(const B& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B::encodeInto(message, writer);
  }

  static void encodeInto(const B& message, ::bebop::Writer& writer) {
    writer.writeUint32(message.protocolVersion);
    writer.writeUint32(message.incomingOpcode);
    writer.writeBytes(message.encodedData);
  }

  static B decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B result;
    B::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B decode(std::vector<uint8_t> sourceBuffer) {
    return B::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B decode(::bebop::Reader& reader) {
    B result;
    B::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B& target) {
    B::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B& target) {
    target.protocolVersion = reader.readUint32();
    target.incomingOpcode = reader.readUint32();
    target.encodedData = reader.readBytes();
  }
};

struct B1 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1;

  int32_t i1;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B1& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B1::encodeInto(message, writer);
  }

  static void encodeInto(const B1& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i1);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B1 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B1 result;
    B1::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B1 decode(std::vector<uint8_t> sourceBuffer) {
    return B1::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B1 decode(::bebop::Reader& reader) {
    B1 result;
    B1::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B1& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B1::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B1& target) {
    B1::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B1& target) {
    target.i1 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B2 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2;

  int32_t i2;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B2& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B2::encodeInto(message, writer);
  }

  static void encodeInto(const B2& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i2);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B2 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B2 result;
    B2::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B2 decode(std::vector<uint8_t> sourceBuffer) {
    return B2::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B2 decode(::bebop::Reader& reader) {
    B2 result;
    B2::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B2& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B2::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B2& target) {
    B2::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B2& target) {
    target.i2 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B3 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x3;

  int32_t i3;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B3& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B3::encodeInto(message, writer);
  }

  static void encodeInto(const B3& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i3);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B3 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B3 result;
    B3::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B3 decode(std::vector<uint8_t> sourceBuffer) {
    return B3::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B3 decode(::bebop::Reader& reader) {
    B3 result;
    B3::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B3& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B3::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B3& target) {
    B3::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B3& target) {
    target.i3 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B4 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x4;

  int32_t i4;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B4& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B4::encodeInto(message, writer);
  }

  static void encodeInto(const B4& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i4);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B4 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B4 result;
    B4::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B4 decode(std::vector<uint8_t> sourceBuffer) {
    return B4::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B4 decode(::bebop::Reader& reader) {
    B4 result;
    B4::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B4& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B4::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B4& target) {
    B4::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B4& target) {
    target.i4 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B5 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x5;

  int32_t i5;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B5& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B5::encodeInto(message, writer);
  }

  static void encodeInto(const B5& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i5);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B5 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B5 result;
    B5::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B5 decode(std::vector<uint8_t> sourceBuffer) {
    return B5::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B5 decode(::bebop::Reader& reader) {
    B5 result;
    B5::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B5& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B5::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B5& target) {
    B5::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B5& target) {
    target.i5 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B6 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x6;

  int32_t i6;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B6& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B6::encodeInto(message, writer);
  }

  static void encodeInto(const B6& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i6);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B6 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B6 result;
    B6::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B6 decode(std::vector<uint8_t> sourceBuffer) {
    return B6::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B6 decode(::bebop::Reader& reader) {
    B6 result;
    B6::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B6& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B6::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B6& target) {
    B6::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B6& target) {
    target.i6 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B7 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x7;

  int32_t i7;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B7& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B7::encodeInto(message, writer);
  }

  static void encodeInto(const B7& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i7);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B7 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B7 result;
    B7::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B7 decode(std::vector<uint8_t> sourceBuffer) {
    return B7::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B7 decode(::bebop::Reader& reader) {
    B7 result;
    B7::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B7& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B7::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B7& target) {
    B7::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B7& target) {
    target.i7 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B8 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x8;

  int32_t i8;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B8& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B8::encodeInto(message, writer);
  }

  static void encodeInto(const B8& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i8);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B8 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B8 result;
    B8::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B8 decode(std::vector<uint8_t> sourceBuffer) {
    return B8::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B8 decode(::bebop::Reader& reader) {
    B8 result;
    B8::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B8& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B8::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B8& target) {
    B8::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B8& target) {
    target.i8 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B9 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x9;

  int32_t i9;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B9& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B9::encodeInto(message, writer);
  }

  static void encodeInto(const B9& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i9);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B9 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B9 result;
    B9::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B9 decode(std::vector<uint8_t> sourceBuffer) {
    return B9::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B9 decode(::bebop::Reader& reader) {
    B9 result;
    B9::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B9& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B9::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B9& target) {
    B9::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B9& target) {
    target.i9 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B10 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0xA;

  int32_t i10;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B10& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B10::encodeInto(message, writer);
  }

  static void encodeInto(const B10& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i10);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B10 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B10 result;
    B10::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B10 decode(std::vector<uint8_t> sourceBuffer) {
    return B10::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B10 decode(::bebop::Reader& reader) {
    B10 result;
    B10::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B10& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B10::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B10& target) {
    B10::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B10& target) {
    target.i10 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B11 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0xB;

  int32_t i11;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B11& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B11::encodeInto(message, writer);
  }

  static void encodeInto(const B11& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i11);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B11 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B11 result;
    B11::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B11 decode(std::vector<uint8_t> sourceBuffer) {
    return B11::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B11 decode(::bebop::Reader& reader) {
    B11 result;
    B11::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B11& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B11::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B11& target) {
    B11::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B11& target) {
    target.i11 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B12 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0xC;

  int32_t i12;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B12& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B12::encodeInto(message, writer);
  }

  static void encodeInto(const B12& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i12);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B12 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B12 result;
    B12::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B12 decode(std::vector<uint8_t> sourceBuffer) {
    return B12::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B12 decode(::bebop::Reader& reader) {
    B12 result;
    B12::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B12& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B12::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B12& target) {
    B12::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B12& target) {
    target.i12 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B13 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0xD;

  int32_t i13;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B13& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B13::encodeInto(message, writer);
  }

  static void encodeInto(const B13& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i13);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B13 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B13 result;
    B13::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B13 decode(std::vector<uint8_t> sourceBuffer) {
    return B13::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B13 decode(::bebop::Reader& reader) {
    B13 result;
    B13::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B13& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B13::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B13& target) {
    B13::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B13& target) {
    target.i13 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B14 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0xE;

  int32_t i14;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B14& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B14::encodeInto(message, writer);
  }

  static void encodeInto(const B14& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i14);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B14 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B14 result;
    B14::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B14 decode(std::vector<uint8_t> sourceBuffer) {
    return B14::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B14 decode(::bebop::Reader& reader) {
    B14 result;
    B14::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B14& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B14::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B14& target) {
    B14::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B14& target) {
    target.i14 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B15 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0xF;

  int32_t i15;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B15& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B15::encodeInto(message, writer);
  }

  static void encodeInto(const B15& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i15);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B15 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B15 result;
    B15::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B15 decode(std::vector<uint8_t> sourceBuffer) {
    return B15::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B15 decode(::bebop::Reader& reader) {
    B15 result;
    B15::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B15& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B15::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B15& target) {
    B15::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B15& target) {
    target.i15 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B16 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x10;

  int32_t i16;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B16& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B16::encodeInto(message, writer);
  }

  static void encodeInto(const B16& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i16);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B16 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B16 result;
    B16::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B16 decode(std::vector<uint8_t> sourceBuffer) {
    return B16::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B16 decode(::bebop::Reader& reader) {
    B16 result;
    B16::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B16& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B16::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B16& target) {
    B16::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B16& target) {
    target.i16 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B17 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x11;

  int32_t i17;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B17& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B17::encodeInto(message, writer);
  }

  static void encodeInto(const B17& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i17);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B17 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B17 result;
    B17::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B17 decode(std::vector<uint8_t> sourceBuffer) {
    return B17::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B17 decode(::bebop::Reader& reader) {
    B17 result;
    B17::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B17& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B17::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B17& target) {
    B17::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B17& target) {
    target.i17 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B18 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x12;

  int32_t i18;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B18& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B18::encodeInto(message, writer);
  }

  static void encodeInto(const B18& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i18);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B18 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B18 result;
    B18::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B18 decode(std::vector<uint8_t> sourceBuffer) {
    return B18::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B18 decode(::bebop::Reader& reader) {
    B18 result;
    B18::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B18& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B18::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B18& target) {
    B18::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B18& target) {
    target.i18 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B19 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x13;

  int32_t i19;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B19& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B19::encodeInto(message, writer);
  }

  static void encodeInto(const B19& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i19);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B19 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B19 result;
    B19::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B19 decode(std::vector<uint8_t> sourceBuffer) {
    return B19::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B19 decode(::bebop::Reader& reader) {
    B19 result;
    B19::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B19& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B19::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B19& target) {
    B19::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B19& target) {
    target.i19 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B20 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x14;

  int32_t i20;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B20& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B20::encodeInto(message, writer);
  }

  static void encodeInto(const B20& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i20);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B20 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B20 result;
    B20::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B20 decode(std::vector<uint8_t> sourceBuffer) {
    return B20::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B20 decode(::bebop::Reader& reader) {
    B20 result;
    B20::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B20& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B20::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B20& target) {
    B20::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B20& target) {
    target.i20 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B21 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x15;

  int32_t i21;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B21& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B21::encodeInto(message, writer);
  }

  static void encodeInto(const B21& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i21);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B21 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B21 result;
    B21::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B21 decode(std::vector<uint8_t> sourceBuffer) {
    return B21::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B21 decode(::bebop::Reader& reader) {
    B21 result;
    B21::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B21& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B21::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B21& target) {
    B21::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B21& target) {
    target.i21 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B22 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x16;

  int32_t i22;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B22& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B22::encodeInto(message, writer);
  }

  static void encodeInto(const B22& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i22);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B22 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B22 result;
    B22::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B22 decode(std::vector<uint8_t> sourceBuffer) {
    return B22::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B22 decode(::bebop::Reader& reader) {
    B22 result;
    B22::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B22& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B22::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B22& target) {
    B22::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B22& target) {
    target.i22 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B23 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x17;

  int32_t i23;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B23& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B23::encodeInto(message, writer);
  }

  static void encodeInto(const B23& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i23);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B23 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B23 result;
    B23::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B23 decode(std::vector<uint8_t> sourceBuffer) {
    return B23::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B23 decode(::bebop::Reader& reader) {
    B23 result;
    B23::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B23& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B23::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B23& target) {
    B23::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B23& target) {
    target.i23 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B24 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x18;

  int32_t i24;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B24& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B24::encodeInto(message, writer);
  }

  static void encodeInto(const B24& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i24);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B24 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B24 result;
    B24::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B24 decode(std::vector<uint8_t> sourceBuffer) {
    return B24::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B24 decode(::bebop::Reader& reader) {
    B24 result;
    B24::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B24& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B24::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B24& target) {
    B24::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B24& target) {
    target.i24 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B25 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x19;

  int32_t i25;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B25& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B25::encodeInto(message, writer);
  }

  static void encodeInto(const B25& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i25);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B25 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B25 result;
    B25::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B25 decode(std::vector<uint8_t> sourceBuffer) {
    return B25::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B25 decode(::bebop::Reader& reader) {
    B25 result;
    B25::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B25& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B25::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B25& target) {
    B25::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B25& target) {
    target.i25 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B26 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1A;

  int32_t i26;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B26& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B26::encodeInto(message, writer);
  }

  static void encodeInto(const B26& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i26);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B26 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B26 result;
    B26::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B26 decode(std::vector<uint8_t> sourceBuffer) {
    return B26::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B26 decode(::bebop::Reader& reader) {
    B26 result;
    B26::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B26& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B26::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B26& target) {
    B26::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B26& target) {
    target.i26 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B27 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1B;

  int32_t i27;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B27& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B27::encodeInto(message, writer);
  }

  static void encodeInto(const B27& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i27);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B27 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B27 result;
    B27::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B27 decode(std::vector<uint8_t> sourceBuffer) {
    return B27::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B27 decode(::bebop::Reader& reader) {
    B27 result;
    B27::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B27& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B27::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B27& target) {
    B27::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B27& target) {
    target.i27 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B28 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1C;

  int32_t i28;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B28& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B28::encodeInto(message, writer);
  }

  static void encodeInto(const B28& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i28);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B28 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B28 result;
    B28::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B28 decode(std::vector<uint8_t> sourceBuffer) {
    return B28::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B28 decode(::bebop::Reader& reader) {
    B28 result;
    B28::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B28& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B28::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B28& target) {
    B28::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B28& target) {
    target.i28 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B29 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1D;

  int32_t i29;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B29& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B29::encodeInto(message, writer);
  }

  static void encodeInto(const B29& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i29);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B29 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B29 result;
    B29::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B29 decode(std::vector<uint8_t> sourceBuffer) {
    return B29::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B29 decode(::bebop::Reader& reader) {
    B29 result;
    B29::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B29& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B29::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B29& target) {
    B29::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B29& target) {
    target.i29 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B30 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1E;

  int32_t i30;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B30& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B30::encodeInto(message, writer);
  }

  static void encodeInto(const B30& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i30);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B30 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B30 result;
    B30::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B30 decode(std::vector<uint8_t> sourceBuffer) {
    return B30::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B30 decode(::bebop::Reader& reader) {
    B30 result;
    B30::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B30& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B30::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B30& target) {
    B30::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B30& target) {
    target.i30 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B31 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x1F;

  int32_t i31;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B31& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B31::encodeInto(message, writer);
  }

  static void encodeInto(const B31& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i31);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B31 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B31 result;
    B31::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B31 decode(std::vector<uint8_t> sourceBuffer) {
    return B31::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B31 decode(::bebop::Reader& reader) {
    B31 result;
    B31::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B31& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B31::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B31& target) {
    B31::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B31& target) {
    target.i31 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B32 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x20;

  int32_t i32;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B32& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B32::encodeInto(message, writer);
  }

  static void encodeInto(const B32& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i32);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B32 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B32 result;
    B32::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B32 decode(std::vector<uint8_t> sourceBuffer) {
    return B32::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B32 decode(::bebop::Reader& reader) {
    B32 result;
    B32::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B32& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B32::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B32& target) {
    B32::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B32& target) {
    target.i32 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B33 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x21;

  int32_t i33;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B33& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B33::encodeInto(message, writer);
  }

  static void encodeInto(const B33& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i33);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B33 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B33 result;
    B33::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B33 decode(std::vector<uint8_t> sourceBuffer) {
    return B33::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B33 decode(::bebop::Reader& reader) {
    B33 result;
    B33::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B33& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B33::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B33& target) {
    B33::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B33& target) {
    target.i33 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B34 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x22;

  int32_t i34;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B34& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B34::encodeInto(message, writer);
  }

  static void encodeInto(const B34& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i34);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B34 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B34 result;
    B34::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B34 decode(std::vector<uint8_t> sourceBuffer) {
    return B34::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B34 decode(::bebop::Reader& reader) {
    B34 result;
    B34::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B34& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B34::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B34& target) {
    B34::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B34& target) {
    target.i34 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B35 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x23;

  int32_t i35;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B35& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B35::encodeInto(message, writer);
  }

  static void encodeInto(const B35& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i35);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B35 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B35 result;
    B35::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B35 decode(std::vector<uint8_t> sourceBuffer) {
    return B35::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B35 decode(::bebop::Reader& reader) {
    B35 result;
    B35::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B35& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B35::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B35& target) {
    B35::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B35& target) {
    target.i35 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B36 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x24;

  int32_t i36;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B36& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B36::encodeInto(message, writer);
  }

  static void encodeInto(const B36& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i36);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B36 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B36 result;
    B36::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B36 decode(std::vector<uint8_t> sourceBuffer) {
    return B36::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B36 decode(::bebop::Reader& reader) {
    B36 result;
    B36::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B36& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B36::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B36& target) {
    B36::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B36& target) {
    target.i36 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B37 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x25;

  int32_t i37;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B37& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B37::encodeInto(message, writer);
  }

  static void encodeInto(const B37& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i37);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B37 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B37 result;
    B37::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B37 decode(std::vector<uint8_t> sourceBuffer) {
    return B37::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B37 decode(::bebop::Reader& reader) {
    B37 result;
    B37::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B37& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B37::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B37& target) {
    B37::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B37& target) {
    target.i37 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B38 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x26;

  int32_t i38;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B38& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B38::encodeInto(message, writer);
  }

  static void encodeInto(const B38& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i38);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B38 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B38 result;
    B38::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B38 decode(std::vector<uint8_t> sourceBuffer) {
    return B38::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B38 decode(::bebop::Reader& reader) {
    B38 result;
    B38::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B38& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B38::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B38& target) {
    B38::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B38& target) {
    target.i38 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B39 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x27;

  int32_t i39;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B39& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B39::encodeInto(message, writer);
  }

  static void encodeInto(const B39& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i39);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B39 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B39 result;
    B39::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B39 decode(std::vector<uint8_t> sourceBuffer) {
    return B39::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B39 decode(::bebop::Reader& reader) {
    B39 result;
    B39::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B39& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B39::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B39& target) {
    B39::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B39& target) {
    target.i39 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B40 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x28;

  int32_t i40;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B40& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B40::encodeInto(message, writer);
  }

  static void encodeInto(const B40& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i40);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B40 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B40 result;
    B40::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B40 decode(std::vector<uint8_t> sourceBuffer) {
    return B40::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B40 decode(::bebop::Reader& reader) {
    B40 result;
    B40::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B40& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B40::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B40& target) {
    B40::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B40& target) {
    target.i40 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B41 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x29;

  int32_t i41;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B41& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B41::encodeInto(message, writer);
  }

  static void encodeInto(const B41& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i41);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B41 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B41 result;
    B41::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B41 decode(std::vector<uint8_t> sourceBuffer) {
    return B41::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B41 decode(::bebop::Reader& reader) {
    B41 result;
    B41::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B41& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B41::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B41& target) {
    B41::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B41& target) {
    target.i41 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B42 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2A;

  int32_t i42;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B42& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B42::encodeInto(message, writer);
  }

  static void encodeInto(const B42& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i42);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B42 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B42 result;
    B42::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B42 decode(std::vector<uint8_t> sourceBuffer) {
    return B42::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B42 decode(::bebop::Reader& reader) {
    B42 result;
    B42::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B42& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B42::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B42& target) {
    B42::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B42& target) {
    target.i42 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B43 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2B;

  int32_t i43;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B43& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B43::encodeInto(message, writer);
  }

  static void encodeInto(const B43& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i43);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B43 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B43 result;
    B43::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B43 decode(std::vector<uint8_t> sourceBuffer) {
    return B43::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B43 decode(::bebop::Reader& reader) {
    B43 result;
    B43::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B43& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B43::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B43& target) {
    B43::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B43& target) {
    target.i43 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B44 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2C;

  int32_t i44;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B44& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B44::encodeInto(message, writer);
  }

  static void encodeInto(const B44& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i44);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B44 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B44 result;
    B44::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B44 decode(std::vector<uint8_t> sourceBuffer) {
    return B44::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B44 decode(::bebop::Reader& reader) {
    B44 result;
    B44::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B44& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B44::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B44& target) {
    B44::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B44& target) {
    target.i44 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B45 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2D;

  int32_t i45;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B45& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B45::encodeInto(message, writer);
  }

  static void encodeInto(const B45& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i45);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B45 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B45 result;
    B45::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B45 decode(std::vector<uint8_t> sourceBuffer) {
    return B45::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B45 decode(::bebop::Reader& reader) {
    B45 result;
    B45::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B45& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B45::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B45& target) {
    B45::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B45& target) {
    target.i45 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B46 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2E;

  int32_t i46;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B46& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B46::encodeInto(message, writer);
  }

  static void encodeInto(const B46& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i46);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B46 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B46 result;
    B46::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B46 decode(std::vector<uint8_t> sourceBuffer) {
    return B46::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B46 decode(::bebop::Reader& reader) {
    B46 result;
    B46::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B46& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B46::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B46& target) {
    B46::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B46& target) {
    target.i46 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B47 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x2F;

  int32_t i47;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B47& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B47::encodeInto(message, writer);
  }

  static void encodeInto(const B47& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i47);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B47 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B47 result;
    B47::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B47 decode(std::vector<uint8_t> sourceBuffer) {
    return B47::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B47 decode(::bebop::Reader& reader) {
    B47 result;
    B47::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B47& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B47::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B47& target) {
    B47::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B47& target) {
    target.i47 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B48 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x30;

  int32_t i48;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B48& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B48::encodeInto(message, writer);
  }

  static void encodeInto(const B48& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i48);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B48 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B48 result;
    B48::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B48 decode(std::vector<uint8_t> sourceBuffer) {
    return B48::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B48 decode(::bebop::Reader& reader) {
    B48 result;
    B48::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B48& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B48::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B48& target) {
    B48::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B48& target) {
    target.i48 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B49 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x31;

  int32_t i49;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B49& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B49::encodeInto(message, writer);
  }

  static void encodeInto(const B49& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i49);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B49 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B49 result;
    B49::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B49 decode(std::vector<uint8_t> sourceBuffer) {
    return B49::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B49 decode(::bebop::Reader& reader) {
    B49 result;
    B49::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B49& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B49::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B49& target) {
    B49::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B49& target) {
    target.i49 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct B50 {
  static const size_t minimalEncodedSize = 41;
  static const uint32_t opcode = 0x32;

  int32_t i50;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const B50& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    B50::encodeInto(message, writer);
  }

  static void encodeInto(const B50& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i50);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static B50 decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    B50 result;
    B50::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static B50 decode(std::vector<uint8_t> sourceBuffer) {
    return B50::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static B50 decode(::bebop::Reader& reader) {
    B50 result;
    B50::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, B50& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    B50::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, B50& target) {
    B50::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, B50& target) {
    target.i50 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

