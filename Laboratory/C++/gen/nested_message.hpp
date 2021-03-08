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

struct NestedInner {
  std::optional<int32_t> x;

  static void encodeInto(const NestedInner& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    NestedInner::encodeInto(message, writer);
  }

  static void encodeInto(const NestedInner& message, ::bebop::Writer& writer) {
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

  static NestedInner decode(const uint8_t* sourceBuffer) {
    NestedInner result;
    NestedInner::decodeInto(sourceBuffer, result);
    return result;
  }

  static NestedInner decode(::bebop::Reader& reader) {
    NestedInner result;
    NestedInner::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, NestedInner& target) {
    ::bebop::Reader reader{sourceBuffer};
    NestedInner::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, NestedInner& target) {
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

struct NestedOuter {
  std::optional<NestedInner> inner;

  static void encodeInto(const NestedOuter& message, std::vector<uint8_t>& targetBuffer) {
    ::bebop::Writer writer{targetBuffer};
    NestedOuter::encodeInto(message, writer);
  }

  static void encodeInto(const NestedOuter& message, ::bebop::Writer& writer) {
    const auto pos = writer.reserveMessageLength();
    const auto start = writer.length();
    if (message.inner.has_value()) {
      writer.writeByte(1);
      NestedInner::encodeInto(message.inner.value(), writer);
    }
    writer.writeByte(0);
    const auto end = writer.length();
    writer.fillMessageLength(pos, end - start);
  }

  static NestedOuter decode(const uint8_t* sourceBuffer) {
    NestedOuter result;
    NestedOuter::decodeInto(sourceBuffer, result);
    return result;
  }

  static NestedOuter decode(::bebop::Reader& reader) {
    NestedOuter result;
    NestedOuter::decodeInto(reader, result);
    return result;
  }

  static void decodeInto(const uint8_t* sourceBuffer, NestedOuter& target) {
    ::bebop::Reader reader{sourceBuffer};
    NestedOuter::decodeInto(reader, target);
  }

  static void decodeInto(::bebop::Reader& reader, NestedOuter& target) {
    const auto length = reader.readMessageLength();
    const auto end = reader.pointer() + length;
    while (true) {
      switch (reader.readByte()) {
        case 0:
          return;
        case 1:
          target.inner.emplace(NestedInner::decode(reader));
          break;
        default:
          reader.seek(end);
          return;
      }
    }
  }
};

