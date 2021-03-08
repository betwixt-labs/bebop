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

  static InnerM decode(const uint8_t* sourceBuffer) {
    InnerM result;
    InnerM::decodeInto(sourceBuffer, result);
    return result;
  }

  static InnerM decode(::bebop::Reader& reader) {
    InnerM result;
    InnerM::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, InnerM& target) {
    ::bebop::Reader reader{sourceBuffer};
    InnerM::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, InnerM& target) {
    const auto length = reader.readMessageLength();
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
  bool y;

  static void encodeInto(const InnerS& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    InnerS::encodeInto(message, writer);
  }

  static void encodeInto(const InnerS& message, ::bebop::Writer& writer) {
    writer.writeBool(message.y);
  }

  static InnerS decode(const uint8_t* sourceBuffer) {
    InnerS result;
    InnerS::decodeInto(sourceBuffer, result);
    return result;
  }

  static InnerS decode(::bebop::Reader& reader) {
    InnerS result;
    InnerS::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, InnerS& target) {
    ::bebop::Reader reader{sourceBuffer};
    InnerS::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, InnerS& target) {
    target.y = reader.readBool();
  }
};

struct OuterM {
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

  static OuterM decode(const uint8_t* sourceBuffer) {
    OuterM result;
    OuterM::decodeInto(sourceBuffer, result);
    return result;
  }

  static OuterM decode(::bebop::Reader& reader) {
    OuterM result;
    OuterM::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, OuterM& target) {
    ::bebop::Reader reader{sourceBuffer};
    OuterM::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, OuterM& target) {
    const auto length = reader.readMessageLength();
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

  static OuterS decode(const uint8_t* sourceBuffer) {
    OuterS result;
    OuterS::decodeInto(sourceBuffer, result);
    return result;
  }

  static OuterS decode(::bebop::Reader& reader) {
    OuterS result;
    OuterS::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, OuterS& target) {
    ::bebop::Reader reader{sourceBuffer};
    OuterS::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, OuterS& target) {
    InnerM::decodeInto(reader, target.innerM);
    InnerS::decodeInto(reader, target.innerS);
  }
};

