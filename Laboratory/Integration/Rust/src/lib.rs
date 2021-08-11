mod schema;
pub use schema::*;
use bebop::prelude::*;

pub fn make_library() -> Library<'static> {
    Library {
        albums: bebop::collection! {
            "Giant Steps" => Album::StudioAlbum {
                tracks: vec![
                    Song {
                        title: Some("Giant Steps"),
                        year: Some(1959),
                        performers: Some(vec![
                            Musician {
                                name: "John Coltrane",
                                plays: Instrument::Piano
                            }
                        ])
                    },
                    Song {
                        title: Some("A Night in Tunisia"),
                        year: Some(1942),
                        performers: Some(vec![
                            Musician {
                                name: "Dizzy Gillespie",
                                plays: Instrument::Trumpet,
                            },
                            Musician {
                                name: "Count Basie",
                                plays: Instrument::Piano,
                            },
                        ])
                    },
                    Song {
                        title: Some("Groovin' High"),
                        year: None,
                        performers: None
                    },
                ]
            },
            "Adam's Apple" => Album::LiveAlbum {
                tracks: None,
                venue_name: Some("Tunisia"),
                concert_date: Some(Date::from_secs_since_unix_epoch(528205479)),
            },
            "Milestones" => Album::StudioAlbum {
                tracks: vec![]
            },
            "Brilliant Corners" => Album::LiveAlbum {
                venue_name: Some("Night's Palace"),
                concert_date: None,
                tracks: Some(vec![
                    Song {
                        title: None,
                        year: Some(1965),
                        performers: Some(vec![
                            Musician {
                                name: "Carmell Jones",
                                plays: Instrument::Trumpet,
                            },
                            Musician {
                                name: "Joe Henderson",
                                plays: Instrument::Sax,
                            },
                            Musician {
                                name: "Teddy Smith",
                                plays: Instrument::Clarinet,
                            },
                        ])
                    }
                ])
            }
        },
    }
}
