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

  static std::unique_ptr<std::vector<uint8_t>> encode(const Musician &message) {
    auto &writer = bebop::BebopWriter::instance();
    Musician::encodeInto(message, writer);
    return std::move(writer.buffer());
  }

  static void encodeInto(const Musician &message, bebop::BebopWriter &view) {
    view.writeString(message.name);
    view.writeUint32(static_cast<uint32_t>(message.plays));
  }

  static Musician decode(const uint8_t *buffer) {
    Musician result;
    Musician::readIntoFrom(result, bebop::BebopReader::instance(buffer));
    return result;
  }

  static void readIntoFrom(Musician &target, bebop::BebopReader& view) {
    target.name = view.readString();
    target.plays = static_cast<Instrument>(view.readUint32());
  }
};

struct Song {
  std::optional<std::string> title;
  std::optional<uint16_t> year;
  std::optional<std::vector<Musician>> performers;

  static std::unique_ptr<std::vector<uint8_t>> encode(const Song &message) {
    auto &writer = bebop::BebopWriter::instance();
    Song::encodeInto(message, writer);
    return std::move(writer.buffer());
  }

  static void encodeInto(const Song &message, bebop::BebopWriter &view) {
    const auto pos = view.reserveMessageLength();
    const auto start = view.length();
    if (message.title.has_value()) {
      view.writeByte(1);
      view.writeString(message.title.value());
    }
    if (message.year.has_value()) {
      view.writeByte(2);
      view.writeUint16(message.year.value());
    }
    if (message.performers.has_value()) {
      view.writeByte(3);
      {
      const auto length0 = message.performers.value().size();
      view.writeUint32(length0);
      for (const auto &i0 : message.performers.value()) {
        Musician::encodeInto(i0, view);
      }
    }
    }
    view.writeByte(0);
    const auto end = view.length();
    view.fillMessageLength(pos, end - start);
  }

  static Song decode(const uint8_t *buffer) {
    Song result;
    Song::readIntoFrom(result, bebop::BebopReader::instance(buffer));
    return result;
  }

  static void readIntoFrom(Song &target, bebop::BebopReader& view) {
    const auto length = view.readMessageLength();
    const auto end = view.pointer() + length;
    while (true) {
      switch (view.readByte()) {
        case 0:
          return;
        case 1:
          target.title = view.readString();
          break;
        case 2:
          target.year = view.readUint16();
          break;
        case 3:
          {
        const auto length0 = view.readUint32();
        target.performers = std::vector<Musician>();
        target.performers->reserve(length0);
        for (size_t i0 = 0; i0 < length0; i0++) {
          Musician x0;
          Musician::readIntoFrom(x0, view);
          target.performers->push_back(x0);
        }
      }
          break;
        default:
          view.seek(end);
          return;
      }
    }
  }
};

struct Library {
  std::map<bebop::Guid, Song> songs;

  static std::unique_ptr<std::vector<uint8_t>> encode(const Library &message) {
    auto &writer = bebop::BebopWriter::instance();
    Library::encodeInto(message, writer);
    return std::move(writer.buffer());
  }

  static void encodeInto(const Library &message, bebop::BebopWriter &view) {
    view.writeUint32(message.songs.size());
    for (const auto &e0 : message.songs) {
      view.writeGuid(e0.first);
      Song::encodeInto(e0.second, view);
    }
  }

  static Library decode(const uint8_t *buffer) {
    Library result;
    Library::readIntoFrom(result, bebop::BebopReader::instance(buffer));
    return result;
  }

  static void readIntoFrom(Library &target, bebop::BebopReader& view) {
    {
      const auto length0 = view.readUint32();
      target.songs = std::map<bebop::Guid, Song>();
      for (size_t i0 = 0; i0 < length0; i0++) {
        bebop::Guid k0;
        Song v0;
        k0 = view.readGuid();
        Song::readIntoFrom(v0, view);
        target.songs[k0] = v0;
      }
    }
  }
};

