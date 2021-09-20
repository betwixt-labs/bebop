use crate::bebops::jazz as bb;
use crate::protos::jazz as pr;
use bebop::Date;
use protobuf::ProtobufEnum;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::convert::TryInto;

pub fn make_library() -> Library {
    // to whomever is reading this, yes, I was very lazy in pulling details from wikipedia.
    Library {
        albums: bebop::collection! {
            "Giant Steps".into() => Album::StudioAlbum {
                tracks: vec![
                    Song {
                        title: Some("Giant Steps".into()),
                        year: Some(1959),
                        performers: Some(vec![
                            Performer {
                                name: "John Coltrane".into(),
                                plays: Instrument::Piano
                            }
                        ])
                    },
                    Song {
                        title: Some("A Night in Tunisia".into()),
                        year: Some(1942),
                        performers: Some(vec![
                            Performer {
                                name: "Dizzy Gillespie".into(),
                                plays: Instrument::Trumpet,
                            },
                            Performer {
                                name: "Frank Paparelli".into(),
                                plays: Instrument::Piano,
                            },
                            Performer {
                                name: "Count Basie".into(),
                                plays: Instrument::Piano,
                            },
                        ])
                    },
                    Song {
                        title: Some("Groovin' High".into()),
                        year: None,
                        performers: None
                    },
                ]
            },
            "Adam's Apple".into() => Album::LiveAlbum {
                venue_name: Some("Tunisia".into()),
                concert_date: Some(1978),
                tracks: None
            },
            "Milestones".into() => Album::StudioAlbum {
                tracks: vec![]
            },
            "Blue Train".into() => Album::LiveAlbum {
                venue_name: Some("Cape Verdean".into()),
                concert_date: None,
                tracks: Some(vec![
                    Song {
                        title: Some("'Round Midnight".into()),
                        year: Some(1986),
                        performers: Some(vec![
                            Performer {
                                name: "Freddie Hubbard".into(),
                                plays: Instrument::Trumpet,
                            },
                            Performer {
                                name: "Ron Carter".into(),
                                plays: Instrument::Cello,
                            },
                        ])
                    },
                    Song {
                        title: Some("Bounding with Bud".into()),
                        year: Some(1946),
                        performers: None
                    },
                ])
            },
            "Brilliant Corners".into() => Album::StudioAlbum {
                tracks: vec![
                    Song {
                        title: Some("Song for My Father".into()),
                        year: Some(1965),
                        performers: Some(vec![
                            Performer {
                                name: "Horace Silver".into(),
                                plays: Instrument::Piano,
                            },
                            Performer {
                                name: "Carmell Jones".into(),
                                plays: Instrument::Trumpet,
                            },
                            Performer {
                                name: "Joe Henderson".into(),
                                plays: Instrument::Sax,
                            },
                            Performer {
                                name: "Teddy Smith".into(),
                                plays: Instrument::Cello,
                            },
                        ])
                    },
                    Song {
                        title: Some("Yardbird Suite".into()),
                        year: Some(1946),
                        performers: Some(vec![
                            Performer {
                                name: "Charlie Parker".into(),
                                plays: Instrument::Sax,
                            }
                        ])
                    }
                ]
            }
        },
    }
}

#[derive(Serialize, Deserialize, Copy, Clone)]
pub enum Instrument {
    Sax = 0,
    Trumpet = 1,
    Clarinet = 2,
    Piano = 3,
    Cello = 4,
}

impl From<Instrument> for u32 {
    fn from(s: Instrument) -> Self {
        match s {
            Instrument::Sax => 0,
            Instrument::Trumpet => 1,
            Instrument::Clarinet => 2,
            Instrument::Piano => 3,
            Instrument::Cello => 4,
        }
    }
}

impl From<Instrument> for bb::Instrument {
    #[inline]
    fn from(v: Instrument) -> Self {
        u32::from(v).try_into().unwrap()
    }
}

impl From<Instrument> for pr::Instrument {
    #[inline]
    fn from(v: Instrument) -> Self {
        Self::from_i32(u32::from(v) as i32).unwrap()
    }
}

#[derive(Serialize, Deserialize, Clone)]
pub struct Performer {
    pub name: String,
    pub plays: Instrument,
}

impl<'a> From<&'a Performer> for bb::Performer<'a> {
    #[inline]
    fn from(v: &'a Performer) -> Self {
        Self {
            name: v.name.as_str(),
            plays: v.plays.into(),
        }
    }
}

impl From<Performer> for pr::Performer {
    #[inline]
    fn from(v: Performer) -> Self {
        let mut p = Self::new();
        p.set_name(v.name);
        p.set_plays(v.plays.into());
        p
    }
}

#[derive(Serialize, Deserialize, Clone)]
pub struct Song {
    pub title: Option<String>,
    pub year: Option<u16>,
    pub performers: Option<Vec<Performer>>,
}

impl<'a> From<&'a Song> for bb::Song<'a> {
    fn from(v: &'a Song) -> Self {
        Self {
            title: v.title.as_deref(),
            year: v.year,
            performers: v
                .performers
                .as_ref()
                .map(|v| v.iter().map(Into::into).collect()),
        }
    }
}

impl From<Song> for pr::Song {
    fn from(v: Song) -> Self {
        let mut s = Self::new();
        if let Some(title) = v.title {
            s.set_title(title)
        }
        if let Some(year) = v.year {
            s.set_year(year as u32)
        }
        if let Some(performers) = v.performers {
            s.set_performers(performers.into_iter().map(Into::into).collect())
        }
        s
    }
}

#[derive(Serialize, Deserialize, Clone)]
pub enum Album {
    StudioAlbum {
        tracks: Vec<Song>,
    },
    LiveAlbum {
        tracks: Option<Vec<Song>>,
        venue_name: Option<String>,
        concert_date: Option<u64>,
    },
}

impl<'a> From<&'a Album> for bb::Album<'a> {
    fn from(v: &'a Album) -> Self {
        match v {
            Album::StudioAlbum { tracks } => Self::StudioAlbum {
                tracks: tracks.iter().map(Into::into).collect(),
            },
            Album::LiveAlbum {
                tracks,
                venue_name,
                concert_date,
            } => Self::LiveAlbum {
                tracks: tracks.as_ref().map(|v| v.iter().map(Into::into).collect()),
                venue_name: venue_name.as_deref(),
                concert_date: concert_date.map(Date::from_ticks),
            },
        }
    }
}

impl From<Album> for pr::Album {
    fn from(v: Album) -> Self {
        let mut s = Self::new();
        match v {
            Album::StudioAlbum { tracks } => {
                s.set_tracks(tracks.into_iter().map(Into::into).collect())
            }
            Album::LiveAlbum {
                tracks,
                venue_name,
                concert_date,
            } => {
                if let Some(tracks) = tracks {
                    s.set_tracks(tracks.into_iter().map(Into::into).collect())
                }
                if let Some(venue_name) = venue_name {
                    s.set_venumeName(venue_name)
                }
                if let Some(concert_date) = concert_date {
                    s.set_concertDate(concert_date)
                }
            }
        }
        s
    }
}

#[derive(Serialize, Deserialize, Clone)]
pub struct Library {
    pub albums: HashMap<String, Album>,
}

impl<'a> From<&'a Library> for bb::Library<'a> {
    #[inline]
    fn from(v: &'a Library) -> Self {
        Self {
            albums: v
                .albums
                .iter()
                .map(|(name, album)| (name.as_str(), album.into()))
                .collect(),
        }
    }
}

impl From<Library> for pr::Library {
    #[inline]
    fn from(v: Library) -> Self {
        let mut s = Self::new();
        s.set_albums(
            v.albums
                .into_iter()
                .map(|(name, album)| (name, album.into()))
                .collect(),
        );
        s
    }
}
