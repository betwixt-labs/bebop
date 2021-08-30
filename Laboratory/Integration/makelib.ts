import {Library, ILibrary, Instrument} from "./schema"

export function makelib(): ILibrary {
    return {
        albums: new Map([
            ["Giant Steps", {
                discriminator: 1, value: {
                    tracks: [
                        {
                            title: "Giant Steps",
                            year: 1959,
                            performers: [{name: "John Coltrane", plays: Instrument.Piano}],
                        }, {
                            title: "A Night in Tunisia",
                            year: 1942,
                            performers: [
                                {name: "Dizzy Gillespie", plays: Instrument.Trumpet},
                                {name: "Count Basie", plays: Instrument.Piano},
                            ]
                        }, {
                            title: "Groovin' High"
                        }
                    ]
                }
            }],
            ["Adam's Apple", {
                discriminator: 2, value: {
                    venueName: "Tunisia",
                    concertDate: new Date(528205479000)
                }
            }],
            ["Milestones", {
                discriminator: 1, value: {
                    tracks: []
                }
            }],
            ["Brilliant Corners", {
                discriminator: 2, value: {
                    venueName: "Night's Palace",
                    tracks: [{
                        year: 1965,
                        performers: [
                            {name: "Carmell Jones", plays: Instrument.Trumpet},
                            {name: "Joe Henderson", plays: Instrument.Sax},
                            {name: "Teddy Smith", plays: Instrument.Clarinet}
                        ]
                    }]
                }
            }],
        ])
    }
}
