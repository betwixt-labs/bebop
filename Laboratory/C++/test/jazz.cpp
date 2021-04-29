#include "../gen/jazz.hpp"
#include <cstdio>
#include <map>
#include <vector>

int main() {
    bebop::Guid g = bebop::Guid::fromString("81c6987b-48b7-495f-ad01-ec20cc5f5be1");
    printf("%s\n", g.toString().c_str());

    Song s;
    s.title = "Donna Lee";
    s.year = 1974;
    Musician m = {"Charlie Parker", Instrument::Sax};
    s.performers = {10, m};
    std::map<bebop::Guid, Song> songs {{g, s}};
    Library l {songs};

    std::vector<uint8_t> buf;
    printf("Computed byte count before encoding: %zu\n", l.byteCount());
    size_t bytesWritten = Library::encodeInto(l, buf);
    printf("Bytes written, reported by encodeInto(): %lu\n", bytesWritten);
    printf("Byte count of encoded buffer: %lu\n", buf.size());
    for (auto x : buf) printf(" %02x", x);
    printf("\n");

    Library l2;
    size_t bytesRead = Library::decodeInto(buf, l2);
    printf("Read %zu bytes\n", bytesRead);
    printf("%s\n", l2.songs.at(g).title.value().c_str());
    printf("%d\n", l2.songs.at(g).year.value());
    printf("%s\n", l2.songs.at(g).performers.value()[0].name.c_str());

    buf = std::vector<uint8_t> { 123, 123, 123, 123, 123 };
    try {
        Library::decodeInto(buf, l2);
    } catch (bebop::MalformedPacketException& e) {
        printf("Decoding a malformed packet correctly threw an exception\n");
    }
}

