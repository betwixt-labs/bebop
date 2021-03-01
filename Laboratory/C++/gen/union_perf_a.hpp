#include <cstddef>
#include <cstdint>
#include <map>
#include <memory>
#include <optional>
#include <string>
#include <variant>
#include <vector>
#include "bebop.hpp"

struct A1 {
  int32_t i1;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A1& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A1::encodeInto(message, writer);
  }

  static void encodeInto(const A1& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i1);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A1 decode(const uint8_t *sourceBuffer) {
    A1 result;
    A1::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A1& target) {
    ::bebop::Reader reader{sourceBuffer};
    A1::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A1& target) {
    target.i1 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A2 {
  int32_t i2;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A2& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A2::encodeInto(message, writer);
  }

  static void encodeInto(const A2& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i2);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A2 decode(const uint8_t *sourceBuffer) {
    A2 result;
    A2::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A2& target) {
    ::bebop::Reader reader{sourceBuffer};
    A2::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A2& target) {
    target.i2 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A3 {
  int32_t i3;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A3& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A3::encodeInto(message, writer);
  }

  static void encodeInto(const A3& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i3);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A3 decode(const uint8_t *sourceBuffer) {
    A3 result;
    A3::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A3& target) {
    ::bebop::Reader reader{sourceBuffer};
    A3::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A3& target) {
    target.i3 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A4 {
  int32_t i4;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A4& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A4::encodeInto(message, writer);
  }

  static void encodeInto(const A4& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i4);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A4 decode(const uint8_t *sourceBuffer) {
    A4 result;
    A4::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A4& target) {
    ::bebop::Reader reader{sourceBuffer};
    A4::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A4& target) {
    target.i4 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A5 {
  int32_t i5;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A5& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A5::encodeInto(message, writer);
  }

  static void encodeInto(const A5& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i5);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A5 decode(const uint8_t *sourceBuffer) {
    A5 result;
    A5::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A5& target) {
    ::bebop::Reader reader{sourceBuffer};
    A5::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A5& target) {
    target.i5 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A6 {
  int32_t i6;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A6& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A6::encodeInto(message, writer);
  }

  static void encodeInto(const A6& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i6);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A6 decode(const uint8_t *sourceBuffer) {
    A6 result;
    A6::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A6& target) {
    ::bebop::Reader reader{sourceBuffer};
    A6::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A6& target) {
    target.i6 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A7 {
  int32_t i7;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A7& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A7::encodeInto(message, writer);
  }

  static void encodeInto(const A7& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i7);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A7 decode(const uint8_t *sourceBuffer) {
    A7 result;
    A7::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A7& target) {
    ::bebop::Reader reader{sourceBuffer};
    A7::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A7& target) {
    target.i7 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A8 {
  int32_t i8;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A8& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A8::encodeInto(message, writer);
  }

  static void encodeInto(const A8& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i8);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A8 decode(const uint8_t *sourceBuffer) {
    A8 result;
    A8::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A8& target) {
    ::bebop::Reader reader{sourceBuffer};
    A8::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A8& target) {
    target.i8 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A9 {
  int32_t i9;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A9& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A9::encodeInto(message, writer);
  }

  static void encodeInto(const A9& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i9);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A9 decode(const uint8_t *sourceBuffer) {
    A9 result;
    A9::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A9& target) {
    ::bebop::Reader reader{sourceBuffer};
    A9::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A9& target) {
    target.i9 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A10 {
  int32_t i10;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A10& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A10::encodeInto(message, writer);
  }

  static void encodeInto(const A10& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i10);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A10 decode(const uint8_t *sourceBuffer) {
    A10 result;
    A10::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A10& target) {
    ::bebop::Reader reader{sourceBuffer};
    A10::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A10& target) {
    target.i10 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A11 {
  int32_t i11;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A11& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A11::encodeInto(message, writer);
  }

  static void encodeInto(const A11& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i11);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A11 decode(const uint8_t *sourceBuffer) {
    A11 result;
    A11::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A11& target) {
    ::bebop::Reader reader{sourceBuffer};
    A11::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A11& target) {
    target.i11 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A12 {
  int32_t i12;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A12& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A12::encodeInto(message, writer);
  }

  static void encodeInto(const A12& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i12);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A12 decode(const uint8_t *sourceBuffer) {
    A12 result;
    A12::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A12& target) {
    ::bebop::Reader reader{sourceBuffer};
    A12::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A12& target) {
    target.i12 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A13 {
  int32_t i13;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A13& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A13::encodeInto(message, writer);
  }

  static void encodeInto(const A13& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i13);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A13 decode(const uint8_t *sourceBuffer) {
    A13 result;
    A13::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A13& target) {
    ::bebop::Reader reader{sourceBuffer};
    A13::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A13& target) {
    target.i13 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A14 {
  int32_t i14;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A14& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A14::encodeInto(message, writer);
  }

  static void encodeInto(const A14& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i14);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A14 decode(const uint8_t *sourceBuffer) {
    A14 result;
    A14::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A14& target) {
    ::bebop::Reader reader{sourceBuffer};
    A14::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A14& target) {
    target.i14 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A15 {
  int32_t i15;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A15& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A15::encodeInto(message, writer);
  }

  static void encodeInto(const A15& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i15);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A15 decode(const uint8_t *sourceBuffer) {
    A15 result;
    A15::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A15& target) {
    ::bebop::Reader reader{sourceBuffer};
    A15::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A15& target) {
    target.i15 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A16 {
  int32_t i16;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A16& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A16::encodeInto(message, writer);
  }

  static void encodeInto(const A16& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i16);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A16 decode(const uint8_t *sourceBuffer) {
    A16 result;
    A16::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A16& target) {
    ::bebop::Reader reader{sourceBuffer};
    A16::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A16& target) {
    target.i16 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A17 {
  int32_t i17;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A17& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A17::encodeInto(message, writer);
  }

  static void encodeInto(const A17& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i17);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A17 decode(const uint8_t *sourceBuffer) {
    A17 result;
    A17::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A17& target) {
    ::bebop::Reader reader{sourceBuffer};
    A17::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A17& target) {
    target.i17 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A18 {
  int32_t i18;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A18& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A18::encodeInto(message, writer);
  }

  static void encodeInto(const A18& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i18);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A18 decode(const uint8_t *sourceBuffer) {
    A18 result;
    A18::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A18& target) {
    ::bebop::Reader reader{sourceBuffer};
    A18::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A18& target) {
    target.i18 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A19 {
  int32_t i19;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A19& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A19::encodeInto(message, writer);
  }

  static void encodeInto(const A19& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i19);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A19 decode(const uint8_t *sourceBuffer) {
    A19 result;
    A19::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A19& target) {
    ::bebop::Reader reader{sourceBuffer};
    A19::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A19& target) {
    target.i19 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A20 {
  int32_t i20;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A20& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A20::encodeInto(message, writer);
  }

  static void encodeInto(const A20& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i20);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A20 decode(const uint8_t *sourceBuffer) {
    A20 result;
    A20::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A20& target) {
    ::bebop::Reader reader{sourceBuffer};
    A20::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A20& target) {
    target.i20 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A21 {
  int32_t i21;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A21& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A21::encodeInto(message, writer);
  }

  static void encodeInto(const A21& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i21);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A21 decode(const uint8_t *sourceBuffer) {
    A21 result;
    A21::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A21& target) {
    ::bebop::Reader reader{sourceBuffer};
    A21::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A21& target) {
    target.i21 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A22 {
  int32_t i22;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A22& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A22::encodeInto(message, writer);
  }

  static void encodeInto(const A22& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i22);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A22 decode(const uint8_t *sourceBuffer) {
    A22 result;
    A22::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A22& target) {
    ::bebop::Reader reader{sourceBuffer};
    A22::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A22& target) {
    target.i22 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A23 {
  int32_t i23;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A23& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A23::encodeInto(message, writer);
  }

  static void encodeInto(const A23& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i23);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A23 decode(const uint8_t *sourceBuffer) {
    A23 result;
    A23::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A23& target) {
    ::bebop::Reader reader{sourceBuffer};
    A23::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A23& target) {
    target.i23 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A24 {
  int32_t i24;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A24& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A24::encodeInto(message, writer);
  }

  static void encodeInto(const A24& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i24);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A24 decode(const uint8_t *sourceBuffer) {
    A24 result;
    A24::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A24& target) {
    ::bebop::Reader reader{sourceBuffer};
    A24::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A24& target) {
    target.i24 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A25 {
  int32_t i25;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A25& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A25::encodeInto(message, writer);
  }

  static void encodeInto(const A25& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i25);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A25 decode(const uint8_t *sourceBuffer) {
    A25 result;
    A25::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A25& target) {
    ::bebop::Reader reader{sourceBuffer};
    A25::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A25& target) {
    target.i25 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A26 {
  int32_t i26;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A26& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A26::encodeInto(message, writer);
  }

  static void encodeInto(const A26& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i26);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A26 decode(const uint8_t *sourceBuffer) {
    A26 result;
    A26::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A26& target) {
    ::bebop::Reader reader{sourceBuffer};
    A26::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A26& target) {
    target.i26 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A27 {
  int32_t i27;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A27& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A27::encodeInto(message, writer);
  }

  static void encodeInto(const A27& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i27);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A27 decode(const uint8_t *sourceBuffer) {
    A27 result;
    A27::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A27& target) {
    ::bebop::Reader reader{sourceBuffer};
    A27::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A27& target) {
    target.i27 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A28 {
  int32_t i28;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A28& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A28::encodeInto(message, writer);
  }

  static void encodeInto(const A28& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i28);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A28 decode(const uint8_t *sourceBuffer) {
    A28 result;
    A28::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A28& target) {
    ::bebop::Reader reader{sourceBuffer};
    A28::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A28& target) {
    target.i28 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A29 {
  int32_t i29;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A29& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A29::encodeInto(message, writer);
  }

  static void encodeInto(const A29& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i29);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A29 decode(const uint8_t *sourceBuffer) {
    A29 result;
    A29::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A29& target) {
    ::bebop::Reader reader{sourceBuffer};
    A29::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A29& target) {
    target.i29 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A30 {
  int32_t i30;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A30& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A30::encodeInto(message, writer);
  }

  static void encodeInto(const A30& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i30);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A30 decode(const uint8_t *sourceBuffer) {
    A30 result;
    A30::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A30& target) {
    ::bebop::Reader reader{sourceBuffer};
    A30::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A30& target) {
    target.i30 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A31 {
  int32_t i31;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A31& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A31::encodeInto(message, writer);
  }

  static void encodeInto(const A31& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i31);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A31 decode(const uint8_t *sourceBuffer) {
    A31 result;
    A31::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A31& target) {
    ::bebop::Reader reader{sourceBuffer};
    A31::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A31& target) {
    target.i31 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A32 {
  int32_t i32;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A32& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A32::encodeInto(message, writer);
  }

  static void encodeInto(const A32& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i32);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A32 decode(const uint8_t *sourceBuffer) {
    A32 result;
    A32::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A32& target) {
    ::bebop::Reader reader{sourceBuffer};
    A32::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A32& target) {
    target.i32 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A33 {
  int32_t i33;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A33& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A33::encodeInto(message, writer);
  }

  static void encodeInto(const A33& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i33);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A33 decode(const uint8_t *sourceBuffer) {
    A33 result;
    A33::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A33& target) {
    ::bebop::Reader reader{sourceBuffer};
    A33::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A33& target) {
    target.i33 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A34 {
  int32_t i34;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A34& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A34::encodeInto(message, writer);
  }

  static void encodeInto(const A34& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i34);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A34 decode(const uint8_t *sourceBuffer) {
    A34 result;
    A34::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A34& target) {
    ::bebop::Reader reader{sourceBuffer};
    A34::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A34& target) {
    target.i34 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A35 {
  int32_t i35;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A35& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A35::encodeInto(message, writer);
  }

  static void encodeInto(const A35& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i35);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A35 decode(const uint8_t *sourceBuffer) {
    A35 result;
    A35::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A35& target) {
    ::bebop::Reader reader{sourceBuffer};
    A35::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A35& target) {
    target.i35 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A36 {
  int32_t i36;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A36& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A36::encodeInto(message, writer);
  }

  static void encodeInto(const A36& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i36);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A36 decode(const uint8_t *sourceBuffer) {
    A36 result;
    A36::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A36& target) {
    ::bebop::Reader reader{sourceBuffer};
    A36::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A36& target) {
    target.i36 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A37 {
  int32_t i37;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A37& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A37::encodeInto(message, writer);
  }

  static void encodeInto(const A37& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i37);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A37 decode(const uint8_t *sourceBuffer) {
    A37 result;
    A37::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A37& target) {
    ::bebop::Reader reader{sourceBuffer};
    A37::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A37& target) {
    target.i37 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A38 {
  int32_t i38;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A38& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A38::encodeInto(message, writer);
  }

  static void encodeInto(const A38& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i38);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A38 decode(const uint8_t *sourceBuffer) {
    A38 result;
    A38::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A38& target) {
    ::bebop::Reader reader{sourceBuffer};
    A38::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A38& target) {
    target.i38 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A39 {
  int32_t i39;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A39& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A39::encodeInto(message, writer);
  }

  static void encodeInto(const A39& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i39);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A39 decode(const uint8_t *sourceBuffer) {
    A39 result;
    A39::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A39& target) {
    ::bebop::Reader reader{sourceBuffer};
    A39::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A39& target) {
    target.i39 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A40 {
  int32_t i40;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A40& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A40::encodeInto(message, writer);
  }

  static void encodeInto(const A40& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i40);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A40 decode(const uint8_t *sourceBuffer) {
    A40 result;
    A40::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A40& target) {
    ::bebop::Reader reader{sourceBuffer};
    A40::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A40& target) {
    target.i40 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A41 {
  int32_t i41;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A41& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A41::encodeInto(message, writer);
  }

  static void encodeInto(const A41& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i41);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A41 decode(const uint8_t *sourceBuffer) {
    A41 result;
    A41::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A41& target) {
    ::bebop::Reader reader{sourceBuffer};
    A41::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A41& target) {
    target.i41 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A42 {
  int32_t i42;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A42& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A42::encodeInto(message, writer);
  }

  static void encodeInto(const A42& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i42);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A42 decode(const uint8_t *sourceBuffer) {
    A42 result;
    A42::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A42& target) {
    ::bebop::Reader reader{sourceBuffer};
    A42::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A42& target) {
    target.i42 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A43 {
  int32_t i43;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A43& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A43::encodeInto(message, writer);
  }

  static void encodeInto(const A43& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i43);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A43 decode(const uint8_t *sourceBuffer) {
    A43 result;
    A43::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A43& target) {
    ::bebop::Reader reader{sourceBuffer};
    A43::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A43& target) {
    target.i43 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A44 {
  int32_t i44;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A44& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A44::encodeInto(message, writer);
  }

  static void encodeInto(const A44& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i44);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A44 decode(const uint8_t *sourceBuffer) {
    A44 result;
    A44::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A44& target) {
    ::bebop::Reader reader{sourceBuffer};
    A44::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A44& target) {
    target.i44 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A45 {
  int32_t i45;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A45& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A45::encodeInto(message, writer);
  }

  static void encodeInto(const A45& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i45);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A45 decode(const uint8_t *sourceBuffer) {
    A45 result;
    A45::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A45& target) {
    ::bebop::Reader reader{sourceBuffer};
    A45::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A45& target) {
    target.i45 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A46 {
  int32_t i46;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A46& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A46::encodeInto(message, writer);
  }

  static void encodeInto(const A46& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i46);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A46 decode(const uint8_t *sourceBuffer) {
    A46 result;
    A46::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A46& target) {
    ::bebop::Reader reader{sourceBuffer};
    A46::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A46& target) {
    target.i46 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A47 {
  int32_t i47;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A47& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A47::encodeInto(message, writer);
  }

  static void encodeInto(const A47& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i47);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A47 decode(const uint8_t *sourceBuffer) {
    A47 result;
    A47::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A47& target) {
    ::bebop::Reader reader{sourceBuffer};
    A47::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A47& target) {
    target.i47 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A48 {
  int32_t i48;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A48& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A48::encodeInto(message, writer);
  }

  static void encodeInto(const A48& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i48);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A48 decode(const uint8_t *sourceBuffer) {
    A48 result;
    A48::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A48& target) {
    ::bebop::Reader reader{sourceBuffer};
    A48::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A48& target) {
    target.i48 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A49 {
  int32_t i49;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A49& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A49::encodeInto(message, writer);
  }

  static void encodeInto(const A49& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i49);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A49 decode(const uint8_t *sourceBuffer) {
    A49 result;
    A49::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A49& target) {
    ::bebop::Reader reader{sourceBuffer};
    A49::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A49& target) {
    target.i49 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

struct A50 {
  int32_t i50;
  uint64_t u;
  double f;
  std::string s;
  ::bebop::Guid g;
  bool b;

  static void encodeInto(const A50& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A50::encodeInto(message, writer);
  }

  static void encodeInto(const A50& message, ::bebop::Writer& writer) {
    writer.writeInt32(message.i50);
    writer.writeUint64(message.u);
    writer.writeFloat64(message.f);
    writer.writeString(message.s);
    writer.writeGuid(message.g);
    writer.writeBool(message.b);
  }

  static A50 decode(const uint8_t *sourceBuffer) {
    A50 result;
    A50::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A50& target) {
    ::bebop::Reader reader{sourceBuffer};
    A50::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A50& target) {
    target.i50 = reader.readInt32();
    target.u = reader.readUint64();
    target.f = reader.readFloat64();
    target.s = reader.readString();
    target.g = reader.readGuid();
    target.b = reader.readBool();
  }
};

/// Option A: put the whole thing in a union.
struct U {
  std::variant<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14, A15, A16, A17, A18, A19, A20, A21, A22, A23, A24, A25, A26, A27, A28, A29, A30, A31, A32, A33, A34, A35, A36, A37, A38, A39, A40, A41, A42, A43, A44, A45, A46, A47, A48, A49, A50> variant;
  static void encodeInto(const U& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    U::encodeInto(message, writer);
  }

  static void encodeInto(const U& message, ::bebop::Writer& writer) {
    const auto pos = writer.reserveMessageLength();
    const auto start = writer.length();
    const uint8_t discriminator = message.variant.index() + 1;
    writer.writeByte(discriminator);
    switch (discriminator) {
     case 1:
      A1::encodeInto(std::get<0>(message.variant), writer);
      break;
     case 2:
      A2::encodeInto(std::get<1>(message.variant), writer);
      break;
     case 3:
      A3::encodeInto(std::get<2>(message.variant), writer);
      break;
     case 4:
      A4::encodeInto(std::get<3>(message.variant), writer);
      break;
     case 5:
      A5::encodeInto(std::get<4>(message.variant), writer);
      break;
     case 6:
      A6::encodeInto(std::get<5>(message.variant), writer);
      break;
     case 7:
      A7::encodeInto(std::get<6>(message.variant), writer);
      break;
     case 8:
      A8::encodeInto(std::get<7>(message.variant), writer);
      break;
     case 9:
      A9::encodeInto(std::get<8>(message.variant), writer);
      break;
     case 10:
      A10::encodeInto(std::get<9>(message.variant), writer);
      break;
     case 11:
      A11::encodeInto(std::get<10>(message.variant), writer);
      break;
     case 12:
      A12::encodeInto(std::get<11>(message.variant), writer);
      break;
     case 13:
      A13::encodeInto(std::get<12>(message.variant), writer);
      break;
     case 14:
      A14::encodeInto(std::get<13>(message.variant), writer);
      break;
     case 15:
      A15::encodeInto(std::get<14>(message.variant), writer);
      break;
     case 16:
      A16::encodeInto(std::get<15>(message.variant), writer);
      break;
     case 17:
      A17::encodeInto(std::get<16>(message.variant), writer);
      break;
     case 18:
      A18::encodeInto(std::get<17>(message.variant), writer);
      break;
     case 19:
      A19::encodeInto(std::get<18>(message.variant), writer);
      break;
     case 20:
      A20::encodeInto(std::get<19>(message.variant), writer);
      break;
     case 21:
      A21::encodeInto(std::get<20>(message.variant), writer);
      break;
     case 22:
      A22::encodeInto(std::get<21>(message.variant), writer);
      break;
     case 23:
      A23::encodeInto(std::get<22>(message.variant), writer);
      break;
     case 24:
      A24::encodeInto(std::get<23>(message.variant), writer);
      break;
     case 25:
      A25::encodeInto(std::get<24>(message.variant), writer);
      break;
     case 26:
      A26::encodeInto(std::get<25>(message.variant), writer);
      break;
     case 27:
      A27::encodeInto(std::get<26>(message.variant), writer);
      break;
     case 28:
      A28::encodeInto(std::get<27>(message.variant), writer);
      break;
     case 29:
      A29::encodeInto(std::get<28>(message.variant), writer);
      break;
     case 30:
      A30::encodeInto(std::get<29>(message.variant), writer);
      break;
     case 31:
      A31::encodeInto(std::get<30>(message.variant), writer);
      break;
     case 32:
      A32::encodeInto(std::get<31>(message.variant), writer);
      break;
     case 33:
      A33::encodeInto(std::get<32>(message.variant), writer);
      break;
     case 34:
      A34::encodeInto(std::get<33>(message.variant), writer);
      break;
     case 35:
      A35::encodeInto(std::get<34>(message.variant), writer);
      break;
     case 36:
      A36::encodeInto(std::get<35>(message.variant), writer);
      break;
     case 37:
      A37::encodeInto(std::get<36>(message.variant), writer);
      break;
     case 38:
      A38::encodeInto(std::get<37>(message.variant), writer);
      break;
     case 39:
      A39::encodeInto(std::get<38>(message.variant), writer);
      break;
     case 40:
      A40::encodeInto(std::get<39>(message.variant), writer);
      break;
     case 41:
      A41::encodeInto(std::get<40>(message.variant), writer);
      break;
     case 42:
      A42::encodeInto(std::get<41>(message.variant), writer);
      break;
     case 43:
      A43::encodeInto(std::get<42>(message.variant), writer);
      break;
     case 44:
      A44::encodeInto(std::get<43>(message.variant), writer);
      break;
     case 45:
      A45::encodeInto(std::get<44>(message.variant), writer);
      break;
     case 46:
      A46::encodeInto(std::get<45>(message.variant), writer);
      break;
     case 47:
      A47::encodeInto(std::get<46>(message.variant), writer);
      break;
     case 48:
      A48::encodeInto(std::get<47>(message.variant), writer);
      break;
     case 49:
      A49::encodeInto(std::get<48>(message.variant), writer);
      break;
     case 50:
      A50::encodeInto(std::get<49>(message.variant), writer);
      break;
    }
    const auto end = writer.length();
    writer.fillMessageLength(pos, end - start);
  }

  static U decode(const uint8_t *sourceBuffer) {
    U result;
    U::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, U& target) {
    ::bebop::Reader reader{sourceBuffer};
    U::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, U& target) {
    const auto length = reader.readMessageLength();
    const auto end = reader.pointer() + length;
    switch (reader.readByte()) {
      case 1:
        target.variant.emplace<0>();
        A1::decodeInto(reader, std::get<0>(target.variant));
        break;
      case 2:
        target.variant.emplace<1>();
        A2::decodeInto(reader, std::get<1>(target.variant));
        break;
      case 3:
        target.variant.emplace<2>();
        A3::decodeInto(reader, std::get<2>(target.variant));
        break;
      case 4:
        target.variant.emplace<3>();
        A4::decodeInto(reader, std::get<3>(target.variant));
        break;
      case 5:
        target.variant.emplace<4>();
        A5::decodeInto(reader, std::get<4>(target.variant));
        break;
      case 6:
        target.variant.emplace<5>();
        A6::decodeInto(reader, std::get<5>(target.variant));
        break;
      case 7:
        target.variant.emplace<6>();
        A7::decodeInto(reader, std::get<6>(target.variant));
        break;
      case 8:
        target.variant.emplace<7>();
        A8::decodeInto(reader, std::get<7>(target.variant));
        break;
      case 9:
        target.variant.emplace<8>();
        A9::decodeInto(reader, std::get<8>(target.variant));
        break;
      case 10:
        target.variant.emplace<9>();
        A10::decodeInto(reader, std::get<9>(target.variant));
        break;
      case 11:
        target.variant.emplace<10>();
        A11::decodeInto(reader, std::get<10>(target.variant));
        break;
      case 12:
        target.variant.emplace<11>();
        A12::decodeInto(reader, std::get<11>(target.variant));
        break;
      case 13:
        target.variant.emplace<12>();
        A13::decodeInto(reader, std::get<12>(target.variant));
        break;
      case 14:
        target.variant.emplace<13>();
        A14::decodeInto(reader, std::get<13>(target.variant));
        break;
      case 15:
        target.variant.emplace<14>();
        A15::decodeInto(reader, std::get<14>(target.variant));
        break;
      case 16:
        target.variant.emplace<15>();
        A16::decodeInto(reader, std::get<15>(target.variant));
        break;
      case 17:
        target.variant.emplace<16>();
        A17::decodeInto(reader, std::get<16>(target.variant));
        break;
      case 18:
        target.variant.emplace<17>();
        A18::decodeInto(reader, std::get<17>(target.variant));
        break;
      case 19:
        target.variant.emplace<18>();
        A19::decodeInto(reader, std::get<18>(target.variant));
        break;
      case 20:
        target.variant.emplace<19>();
        A20::decodeInto(reader, std::get<19>(target.variant));
        break;
      case 21:
        target.variant.emplace<20>();
        A21::decodeInto(reader, std::get<20>(target.variant));
        break;
      case 22:
        target.variant.emplace<21>();
        A22::decodeInto(reader, std::get<21>(target.variant));
        break;
      case 23:
        target.variant.emplace<22>();
        A23::decodeInto(reader, std::get<22>(target.variant));
        break;
      case 24:
        target.variant.emplace<23>();
        A24::decodeInto(reader, std::get<23>(target.variant));
        break;
      case 25:
        target.variant.emplace<24>();
        A25::decodeInto(reader, std::get<24>(target.variant));
        break;
      case 26:
        target.variant.emplace<25>();
        A26::decodeInto(reader, std::get<25>(target.variant));
        break;
      case 27:
        target.variant.emplace<26>();
        A27::decodeInto(reader, std::get<26>(target.variant));
        break;
      case 28:
        target.variant.emplace<27>();
        A28::decodeInto(reader, std::get<27>(target.variant));
        break;
      case 29:
        target.variant.emplace<28>();
        A29::decodeInto(reader, std::get<28>(target.variant));
        break;
      case 30:
        target.variant.emplace<29>();
        A30::decodeInto(reader, std::get<29>(target.variant));
        break;
      case 31:
        target.variant.emplace<30>();
        A31::decodeInto(reader, std::get<30>(target.variant));
        break;
      case 32:
        target.variant.emplace<31>();
        A32::decodeInto(reader, std::get<31>(target.variant));
        break;
      case 33:
        target.variant.emplace<32>();
        A33::decodeInto(reader, std::get<32>(target.variant));
        break;
      case 34:
        target.variant.emplace<33>();
        A34::decodeInto(reader, std::get<33>(target.variant));
        break;
      case 35:
        target.variant.emplace<34>();
        A35::decodeInto(reader, std::get<34>(target.variant));
        break;
      case 36:
        target.variant.emplace<35>();
        A36::decodeInto(reader, std::get<35>(target.variant));
        break;
      case 37:
        target.variant.emplace<36>();
        A37::decodeInto(reader, std::get<36>(target.variant));
        break;
      case 38:
        target.variant.emplace<37>();
        A38::decodeInto(reader, std::get<37>(target.variant));
        break;
      case 39:
        target.variant.emplace<38>();
        A39::decodeInto(reader, std::get<38>(target.variant));
        break;
      case 40:
        target.variant.emplace<39>();
        A40::decodeInto(reader, std::get<39>(target.variant));
        break;
      case 41:
        target.variant.emplace<40>();
        A41::decodeInto(reader, std::get<40>(target.variant));
        break;
      case 42:
        target.variant.emplace<41>();
        A42::decodeInto(reader, std::get<41>(target.variant));
        break;
      case 43:
        target.variant.emplace<42>();
        A43::decodeInto(reader, std::get<42>(target.variant));
        break;
      case 44:
        target.variant.emplace<43>();
        A44::decodeInto(reader, std::get<43>(target.variant));
        break;
      case 45:
        target.variant.emplace<44>();
        A45::decodeInto(reader, std::get<44>(target.variant));
        break;
      case 46:
        target.variant.emplace<45>();
        A46::decodeInto(reader, std::get<45>(target.variant));
        break;
      case 47:
        target.variant.emplace<46>();
        A47::decodeInto(reader, std::get<46>(target.variant));
        break;
      case 48:
        target.variant.emplace<47>();
        A48::decodeInto(reader, std::get<47>(target.variant));
        break;
      case 49:
        target.variant.emplace<48>();
        A49::decodeInto(reader, std::get<48>(target.variant));
        break;
      case 50:
        target.variant.emplace<49>();
        A50::decodeInto(reader, std::get<49>(target.variant));
        break;
      default:
        reader.seek(end); // do nothing?
        return;
    }
  }
};

struct A {
  uint32_t containerOpcode;
  uint32_t protocolVersion;
  U u;

  static void encodeInto(const A& message, std::vector<uint8_t> &targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    A::encodeInto(message, writer);
  }

  static void encodeInto(const A& message, ::bebop::Writer& writer) {
    writer.writeUint32(message.containerOpcode);
    writer.writeUint32(message.protocolVersion);
    U::encodeInto(message.u, writer);
  }

  static A decode(const uint8_t *sourceBuffer) {
    A result;
    A::decodeInto(sourceBuffer, result);
    return result;
  }

  static void decodeInto(const uint8_t *sourceBuffer, A& target) {
    ::bebop::Reader reader{sourceBuffer};
    A::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, A& target) {
    target.containerOpcode = reader.readUint32();
    target.protocolVersion = reader.readUint32();
    U::decodeInto(reader, target.u);
  }
};

