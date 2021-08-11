#pragma once

#include <variant>
#include <cassert>
#include <vector>
#include <cstdio>
#include "schema.hpp"

Library make_library() {
    return Library {
        std::map<std::string, Album> {
            {"Giant Steps", { StudioAlbum {{
                Song {"Giant Steps", 1959, std::vector<Musician> {
                    Musician {"John Coltrane", Instrument::Piano}
                } },
                Song {"A Night in Tunisia", 1942, std::vector<Musician> {
                    Musician {"Dizzy Gillespie", Instrument::Trumpet},
                    Musician {"Count Basie", Instrument::Piano}
                }},
                Song {"Groovin' High", {}, {}}
            }}}},
            {"Adam's Apple", { LiveAlbum {
                {},
                "Tunisia",
                bebop::TickDuration { 5282054790000000 }
            }}},
            {"Milestones", { StudioAlbum {
                std::vector<Song>()
            }}},
            {"Brilliant Corners", {LiveAlbum {
                std::vector<Song> {
                    Song { {}, 1965, std::vector<Musician> {
                        Musician { "Carmell Jones", Instrument::Trumpet },
                        Musician { "Joe Henderson", Instrument::Sax },
                        Musician { "Teddy Smith", Instrument::Clarinet }
                    }},
                },
                "Night's Palace",
                {}
            }}}
        }
    };
}

void is_valid(Library &lib) {
    assert(lib.albums.size() == 4);
    {
        auto album = std::get<StudioAlbum>(lib.albums["Giant Steps"].variant);
        assert(album.tracks.size() == 3);
        {
            auto track = album.tracks[0];
            assert(track.title.value() == "Giant Steps");
            assert(track.year.value() == 1959);
            {
                auto performers = track.performers.value();
                assert(performers.size() == 1);
                assert(performers[0].name == "John Coltrane");
                assert(performers[0].plays == Instrument::Piano);
            }
        }
        {
            auto track = album.tracks[1];
            assert(track.title.value() == "A Night in Tunisia");
            assert(track.year.value() == 1942);
            {
                auto performers = track.performers.value();
                assert(performers.size() == 2);
                assert(performers[0].name == "Dizzy Gillespie");
                assert(performers[0].plays == Instrument::Trumpet);
                assert(performers[1].name == "Count Basie");
                assert(performers[1].plays == Instrument::Piano);
            }
        }
        {
            auto track = album.tracks[2];
            assert(track.title.value() == "Groovin' High");
            assert(!track.year);
            assert(!track.performers);
        }
    }
    {
        auto album = std::get<LiveAlbum>(lib.albums["Adam's Apple"].variant);
        assert(!album.tracks);
        assert(album.venueName.value() == "Tunisia");
        assert(album.concertDate.value() == bebop::TickDuration { 5282054790000000 });
    }
    {
        auto album = std::get<StudioAlbum>(lib.albums["Milestones"].variant);
        assert(album.tracks.empty());
    }
    {
        auto album = std::get<LiveAlbum>(lib.albums["Brilliant Corners"].variant);
        assert(album.venueName.value() == "Night's Palace");
        assert(!album.concertDate);
        auto tracks = album.tracks.value();
        assert(tracks.size() == 1);
        auto track = tracks[0];
        assert(!track.title);
        assert(track.year.value() == 1965);
        auto performers = track.performers.value();
        assert(performers.size() == 3);
        assert(performers[0].name == "Carmell Jones");
        assert(performers[0].plays == Instrument::Trumpet);
        assert(performers[1].name == "Joe Henderson");
        assert(performers[1].plays == Instrument::Sax);
        assert(performers[2].name == "Teddy Smith");
        assert(performers[2].plays == Instrument::Clarinet);
    }
}
