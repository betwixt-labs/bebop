import { Library, ILibrary, Instrument, Album } from "./schema"

export function makelib(): Library {
    return new Library({
        albums: new Map([
            ["Giant Steps", Album.fromStudioAlbum({
                tracks: [
                    {
                        title: "Giant Steps",
                        year: 1959,
                        performers: [{ name: "John Coltrane", plays: Instrument.Piano }],
                    }, {
                        title: "A Night in Tunisia",
                        year: 1942,
                        performers: [
                            { name: "Dizzy Gillespie", plays: Instrument.Trumpet },
                            { name: "Count Basie", plays: Instrument.Piano },
                        ]
                    }, {
                        title: "Groovin' High"
                    }
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
                tracks: [{
                    year: 1965,
                    performers: [
                        { name: "Carmell Jones", plays: Instrument.Trumpet },
                        { name: "Joe Henderson", plays: Instrument.Sax },
                        { name: "Teddy Smith", plays: Instrument.Clarinet }
                    ]
                }]
            })
            ],
        ])
    });
}
