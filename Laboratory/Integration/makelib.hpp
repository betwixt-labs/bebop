#pragma once

#include <variant>
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
