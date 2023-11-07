import { Guid } from "bebop";
import { Library, ILibrary, Instrument, Album, Song } from "./schema"

export function makelib(): Library {
    return new Library({
        albums: new Map([
            ["Giant Steps", Album.fromStudioAlbum({
                tracks: [
                    new Song({
                        title: "Giant Steps",
                        year: 1959,
                        performers: [{ name: "John Coltrane", plays: Instrument.Piano, id: Guid.parseGuid("ff990458-a276-4b71-b2e3-57d49470b949")}],
                    }),
                    new Song({
                        title: "A Night in Tunisia",
                        year: 1942,
                        performers: [
                            { name: "Dizzy Gillespie", plays: Instrument.Trumpet, id: Guid.parseGuid("84f4b320-0f1e-463e-982c-78772fabd74d") },
                            { name: "Count Basie", plays: Instrument.Piano, id: Guid.parseGuid("b28d54d6-a3f7-48bf-a07a-117c15cf33ef") },
                        ]
                    }),
                    new Song({
                        title: "Groovin' High"
                    })
                ]
            })],
            ["Adam's Apple", Album.fromLiveAlbum({
                venueName: "Tunisia",
                concertDate: new Date(528205479000)
            })
            ],
            ["Milestones", Album.fromStudioAlbum({
                tracks: []
            })
            ],
            ["Brilliant Corners", Album.fromLiveAlbum({
                venueName: "Night's Palace",
                tracks: [
                    new Song({
                        year: 1965,
                        performers: [
                            { name: "Carmell Jones", plays: Instrument.Trumpet, id: Guid.parseGuid("f7c31724-0387-4ac9-b6f0-361bb9415c1b") },
                            { name: "Joe Henderson", plays: Instrument.Sax, id: Guid.parseGuid("bb4facf3-c65a-46dd-a96f-73ca6d1cf3f6") },
                            { name: "Teddy Smith", plays: Instrument.Clarinet,  id: Guid.parseGuid("91ffb47f-2a38-4876-8186-1f267cc21706") }
                        ]
                    })
                ]
            })
            ],
        ])
    });
}
