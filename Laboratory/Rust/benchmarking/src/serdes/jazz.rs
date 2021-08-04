use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Serialize, Deserialize)]
pub enum Instrument {
    Sax = 0,
    Trumpet = 1,
    Clarinet = 2,
    Piano = 3,
    Cello = 4,
}

impl From<u32> for Instrument {
    fn from(v: u32) -> Self {
        match v {
            0 => Instrument::Sax,
            1 => Instrument::Trumpet,
            2 => Instrument::Clarinet,
            3 => Instrument::Piano,
            4 => Instrument::Cello,
            _ => unreachable!()
        }
    }
}

#[derive(Serialize, Deserialize)]
pub struct Performer {
    pub name: String,
    pub plays: Instrument,
}

#[derive(Serialize, Deserialize)]
pub struct Song {
    pub title: Option<String>,
    pub year: Option<u16>,
    pub performers: Option<Vec<Performer>>,
}

#[derive(Serialize, Deserialize)]
pub struct Library {
    pub songs: HashMap<String, Song>,
}

#[derive(Serialize, Deserialize)]
pub enum Album {
    StdioAlbum {
        tracks: Vec<Song>,
    },
    LiveAlbum {
        tracks: Vec<Song>,
        venue_name: String,
        concert_date: u64,
    },
}
