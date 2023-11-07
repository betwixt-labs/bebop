mod schema;
pub use schema::*;
use bebop::prelude::*;
use std::str::FromStr;

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
                                plays: Instrument::Piano,
                                id: bebop::Guid::from_str("ff990458-a276-4b71-b2e3-57d49470b949").unwrap(),
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
                                id: bebop::Guid::from_str("84f4b320-0f1e-463e-982c-78772fabd74d").unwrap(),
                            },
                            Musician {
                                name: "Count Basie",
                                plays: Instrument::Piano,
                                id: bebop::Guid::from_str("b28d54d6-a3f7-48bf-a07a-117c15cf33ef").unwrap(),
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
                                id: bebop::Guid::from_str("f7c31724-0387-4ac9-b6f0-361bb9415c1b").unwrap(),
                            },
                            Musician {
                                name: "Joe Henderson",
                                plays: Instrument::Sax,
                                id: bebop::Guid::from_str("bb4facf3-c65a-46dd-a96f-73ca6d1cf3f6").unwrap(),
                            },
                            Musician {
                                name: "Teddy Smith",
                                plays: Instrument::Clarinet,
                                id: bebop::Guid::from_str("91ffb47f-2a38-4876-8186-1f267cc21706").unwrap(),
                            },
                        ])
                    }
                ])
            }
        },
    }
}
