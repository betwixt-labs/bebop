#include "../gen/models.hpp"
#include <cstdio>
#include <map>
#include <vector>

int main() {
    bebop::Guid g = bebop::Guid::fromString("81c6987b-48b7-495f-ad01-ec20cc5f5be1");
    printf("%s\n", g.toString().c_str());
    
    Song s;
    s.title = "Donna Lee";
    s.year = 1974;
    s.performers = {{"Charlie Parker", Instrument::Sax}};
    std::map<bebop::Guid, Song> songs {{g, s}};
    Library l {songs};
    
    std::vector<uint8_t> buf;
    Library::encodeInto(l, buf);
    for (auto x : buf) printf(" %02x", x);
    printf("\n");

    Library l2;
    Library::decodeInto(buf.data(), l2);
    printf("%s\n", l2.songs.at(g).title.value().c_str());
    printf("%d\n", l2.songs.at(g).year.value());
    printf("%s\n", l2.songs.at(g).performers.value()[0].name.c_str());
}

