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

struct InnerM {
  static const size_t minimalEncodedSize = 4;
  std::optional<int32_t> x;

  static void encodeInto(const InnerM& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    InnerM::encodeInto(message, writer);
  }

  static void encodeInto(const InnerM& message, ::bebop::Writer& writer) {
    const auto pos = writer.reserveMessageLength();
    const auto start = writer.length();
    if (message.x.has_value()) {
      writer.writeByte(1);
      writer.writeInt32(message.x.value());
    }
    writer.writeByte(0);
    const auto end = writer.length();
    writer.fillMessageLength(pos, end - start);
  }

  static InnerM decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    InnerM result;
    InnerM::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static InnerM decode(std::vector<uint8_t> sourceBuffer) {
    return InnerM::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static InnerM decode(::bebop::Reader& reader) {
    InnerM result;
    InnerM::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, InnerM& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    InnerM::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, InnerM& target) {
    InnerM::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, InnerM& target) {
    const auto length = reader.readLengthPrefix();
    const auto end = reader.pointer() + length;
    while (true) {
      switch (reader.readByte()) {
        case 0:
          return;
        case 1:
          target.x = reader.readInt32();
          break;
        default:
          reader.seek(end);
          return;
      }
    }
  }
};

struct InnerS {
  static const size_t minimalEncodedSize = 1;
  bool y;

  static void encodeInto(const InnerS& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    InnerS::encodeInto(message, writer);
  }

  static void encodeInto(const InnerS& message, ::bebop::Writer& writer) {
    writer.writeBool(message.y);
  }

  static InnerS decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    InnerS result;
    InnerS::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static InnerS decode(std::vector<uint8_t> sourceBuffer) {
    return InnerS::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static InnerS decode(::bebop::Reader& reader) {
    InnerS result;
    InnerS::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, InnerS& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    InnerS::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, InnerS& target) {
    InnerS::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, InnerS& target) {
    target.y = reader.readBool();
  }
};

struct OuterM {
  static const size_t minimalEncodedSize = 4;
  std::optional<InnerM> innerM;
  std::optional<InnerS> innerS;

  static void encodeInto(const OuterM& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    OuterM::encodeInto(message, writer);
  }

  static void encodeInto(const OuterM& message, ::bebop::Writer& writer) {
    const auto pos = writer.reserveMessageLength();
    const auto start = writer.length();
    if (message.innerM.has_value()) {
      writer.writeByte(1);
      InnerM::encodeInto(message.innerM.value(), writer);
    }
    if (message.innerS.has_value()) {
      writer.writeByte(2);
      InnerS::encodeInto(message.innerS.value(), writer);
    }
    writer.writeByte(0);
    const auto end = writer.length();
    writer.fillMessageLength(pos, end - start);
  }

  static OuterM decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    OuterM result;
    OuterM::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static OuterM decode(std::vector<uint8_t> sourceBuffer) {
    return OuterM::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static OuterM decode(::bebop::Reader& reader) {
    OuterM result;
    OuterM::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, OuterM& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    OuterM::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, OuterM& target) {
    OuterM::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, OuterM& target) {
    const auto length = reader.readLengthPrefix();
    const auto end = reader.pointer() + length;
    while (true) {
      switch (reader.readByte()) {
        case 0:
          return;
        case 1:
          target.innerM.emplace(InnerM::decode(reader));
          break;
        case 2:
          target.innerS.emplace(InnerS::decode(reader));
          break;
        default:
          reader.seek(end);
          return;
      }
    }
  }
};

struct OuterS {
  static const size_t minimalEncodedSize = 5;
  InnerM innerM;
  InnerS innerS;

  static void encodeInto(const OuterS& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    OuterS::encodeInto(message, writer);
  }

  static void encodeInto(const OuterS& message, ::bebop::Writer& writer) {
    InnerM::encodeInto(message.innerM, writer);
    InnerS::encodeInto(message.innerS, writer);
  }

  static OuterS decode(const uint8_t* sourceBuffer, size_t sourceBufferSize) {
    OuterS result;
    OuterS::decodeInto(sourceBuffer, sourceBufferSize, result);
    return result;
  }

  static OuterS decode(std::vector<uint8_t> sourceBuffer) {
    return OuterS::decode(sourceBuffer.data(), sourceBuffer.size());
  }

  static OuterS decode(::bebop::Reader& reader) {
    OuterS result;
    OuterS::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, size_t sourceBufferSize, OuterS& target) {
    ::bebop::Reader reader{sourceBuffer, sourceBufferSize};
    OuterS::decodeInto(reader, target);
  }

  static void decodeInto(std::vector<uint8_t> sourceBuffer, OuterS& target) {
    OuterS::decodeInto(sourceBuffer.data(), sourceBuffer.size(), target);
  }

  static void decodeInto(::bebop::Reader& reader, OuterS& target) {
    InnerM::decodeInto(reader, target.innerM);
    InnerS::decodeInto(reader, target.innerS);
  }
};

