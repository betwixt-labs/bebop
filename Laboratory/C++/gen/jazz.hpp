#include <cstddef>
#include <cstdint>
#include <map>
#include <memory>
#include <optional>
#include <string>
#include <vector>
#include "bebop.hpp"

enum class Instrument {
  Sax = 0,
  Trumpet = 1,
  Clarinet = 2,
};

struct Musician {
  std::string name;
  Instrument plays;

  static std::unique_ptr<std::vector<uint8_t>> encode(const Musician& message) {
    bebop::BebopWriter writer{};
    Musician::encodeInto(message, writer);
    return writer.buffer();
  }

  static void encodeInto(const Musician& message, bebop::BebopWriter& writer) {
    writer.writeString(message.name);
    writer.writeUint32(static_cast<uint32_t>(message.plays));
  }

  static Musician decode(const uint8_t *buffer) {
    Musician result;
    bebop::BebopReader reader{buffer};
    Musician::decodeInto(result, reader);
    return result;
  }

  static void decodeInto(Musician& target, bebop::BebopReader& reader) {
    target.name = reader.readString();
    target.plays = static_cast<Instrument>(reader.readUint32());
  }
};

struct Song {
  std::optional<std::string> title;
  std::optional<uint16_t> year;
  std::optional<std::vector<Musician>> performers;

  static std::unique_ptr<std::vector<uint8_t>> encode(const Song& message) {
    bebop::BebopWriter writer{};
    Song::encodeInto(message, writer);
    return writer.buffer();
  }

  static void encodeInto(const Song& message, bebop::BebopWriter& writer) {
    const auto pos = writer.reserveMessageLength();
    const auto start = writer.length();
    if (message.title.has_value()) {
      writer.writeByte(1);
      writer.writeString(message.title.value());
    }
    if (message.year.has_value()) {
      writer.writeByte(2);
      writer.writeUint16(message.year.value());
    }
    if (message.performers.has_value()) {
      writer.writeByte(3);
      {
        const auto length0 = message.performers.value().size();
        writer.writeUint32(length0);
        for (const auto& i0 : message.performers.value()) {
          Musician::encodeInto(i0, writer);
        }
      }
    }
    writer.writeByte(0);
    const auto end = writer.length();
    writer.fillMessageLength(pos, end - start);
  }

  static Song decode(const uint8_t *buffer) {
    Song result;
    bebop::BebopReader reader{buffer};
    Song::decodeInto(result, reader);
    return result;
  }

  static void decodeInto(Song& target, bebop::BebopReader& reader) {
    const auto length = reader.readMessageLength();
    const auto end = reader.pointer() + length;
    while (true) {
      switch (reader.readByte()) {
        case 0:
          return;
        case 1:
          target.title = reader.readString();
          break;
        case 2:
          target.year = reader.readUint16();
          break;
        case 3:
          {
            const auto length0 = reader.readUint32();
            target.performers = std::vector<Musician>();
            target.performers->reserve(length0);
            for (size_t i0 = 0; i0 < length0; i0++) {
              Musician x0;
              Musician::decodeInto(x0, reader);
              target.performers->push_back(x0);
            }
          }
          break;
        default:
          reader.seek(end);
          return;
      }
    }
  }
};

struct Library {
  std::map<bebop::Guid, Song> songs;

  static std::unique_ptr<std::vector<uint8_t>> encode(const Library& message) {
    bebop::BebopWriter writer{};
    Library::encodeInto(message, writer);
    return writer.buffer();
  }

  static void encodeInto(const Library& message, bebop::BebopWriter& writer) {
    writer.writeUint32(message.songs.size());
    for (const auto& e0 : message.songs) {
      writer.writeGuid(e0.first);
      Song::encodeInto(e0.second, writer);
    }
  }

  static Library decode(const uint8_t *buffer) {
    Library result;
    bebop::BebopReader reader{buffer};
    Library::decodeInto(result, reader);
    return result;
  }

  static void decodeInto(Library& target, bebop::BebopReader& reader) {
    {
      const auto length0 = reader.readUint32();
      target.songs = std::map<bebop::Guid, Song>();
      for (size_t i0 = 0; i0 < length0; i0++) {
        bebop::Guid k0;
        k0 = reader.readGuid();
        Song& v0 = target.songs.operator[](k0);
        Song::decodeInto(v0, reader);
      }
    }
  }
};

