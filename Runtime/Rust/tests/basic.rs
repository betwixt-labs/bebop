//! Manual implementation which can be used for testing coherence and also for testing performance
//!
//! ```
//! const int32 PianoKeys = 88;
//! const guid ImportantProductID = "a3628ec7-28d4-4546-ad4a-f6ebf5375c96";
//!
//! enum Instrument {
//!     Sax = 0;
//!     Trumpet = 1;
//!     Clarinet = 2;
//! }
//!
//! struct Performer {
//!     string name;
//!     Instrument plays;
//! }
//!
//! message Song {
//!     1 -> string title;
//!     2 -> uint16 year;
//!     3 -> Performer[] performers;
//! }
//!
//! union Album {
//!     1 -> struct StudioAlbum {
//!         Song[] tracks;
//!     }
//!     2 -> message LiveAlbum {
//!         1 -> Song[] tracks;
//!         2 -> string venueName;
//!         3 -> date concertDate;
//!     }
//! }
//! ```

use crate::Album::{StudioAlbum, Unknown};
use bebop::serialization::{read_len, DeserializeError, Result, LEN_SIZE};
use bebop::*;
use std::convert::{TryFrom, TryInto};

// Constants which are the same for all implementations

/// Generated from `const PianoKeys`
pub const PIANO_KEYS: i32 = 88;

/// Generated from `const ImportantProductID`
pub const IMPORTANT_PRODUCT_ID: Guid = Guid::from_be_bytes([
    0xa3, 0x62, 0x8e, 0xc7, 0x28, 0xd4, 0x45, 0x46, 0xad, 0x4a, 0xf6, 0xeb, 0xf5, 0x37, 0x5c, 0x96,
]);

/// Generated from `enum Instrument`
pub enum Instrument {
    Sax = 0,
    Trumpet = 1,
    Clarinet = 2,
}

impl TryFrom<u32> for Instrument {
    type Error = DeserializeError;

    fn try_from(value: u32) -> Result<Self> {
        match value {
            0 => Ok(Instrument::Sax),
            1 => Ok(Instrument::Trumpet),
            2 => Ok(Instrument::Clarinet),
            _ => Err(DeserializeError::InvalidEnumDiscriminator),
        }
    }
}

impl<'de> Deserialize<'de> for Instrument {
    #[inline]
    fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)> {
        let (n, v) = u32::deserialize_chained(raw)?;
        Ok((n, v.try_into()?))
    }
}

/// Generated from `struct Performer`
pub struct Performer<'de> {
    pub name: &'de str,
    pub plays: Instrument,
}

impl<'de> Deserialize<'de> for Performer<'de> {
    fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)> {
        let (read, name) = <&str>::deserialize_chained(raw)?;
        let mut i = read;
        let (read, plays) = Instrument::deserialize_chained(&raw[i..])?;
        i += read;
        Ok((i, Self { name, plays }))
    }
}

/// Generated from `message Song`
pub struct Song<'de> {
    /// Field 1
    pub title: Option<&'de str>,
    /// Field 2
    pub year: Option<u16>,
    /// Field 3
    pub performers: Option<Vec<Performer<'de>>>,
}

impl<'de> Default for Song<'de> {
    fn default() -> Self {
        Song {
            title: None,
            year: None,
            performers: None,
        }
    }
}

impl<'de> Deserialize<'de> for Song<'de> {
    fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut song = Song::default();

        while i < len + LEN_SIZE {
            let di = raw[i];
            i += 1;
            match di {
                0 => {
                    // Reached the end, in theory... Check performed after loop
                    break;
                }
                1 => {
                    let (read, title) = <&str>::deserialize_chained(&raw[i..])?;
                    i += read;
                    song.title = Some(title);
                }
                2 => {
                    let (read, year) = u16::deserialize_chained(&raw[i..])?;
                    i += read;
                    song.year = Some(year);
                }
                3 => {
                    let (read, performers) = <Vec<Performer>>::deserialize_chained(&raw[i..])?;
                    i += read;
                    song.performers = Some(performers);
                }
                _ => {
                    // Ignore unknown message field and all that come after
                    i = len + LEN_SIZE;
                    break;
                }
            }
        }
        if i != len + LEN_SIZE {
            // We either ended past where we were supposed to or found a null terminator early,
            // both imply something bad happened.
            return Err(DeserializeError::CorruptFrame);
        }

        Ok((i, song))
    }
}

/// Generated from `union Album`
pub enum Album<'de> {
    Unknown,
    /// Generated from `struct Album::StudioAlbum`
    StudioAlbum { tracks: Vec<Song<'de>> },
    /// Generated from `message Album::LiveAlbum`
    LiveAlbum {
        tracks: Option<Vec<Song<'de>>>,
        venueName: Option<&'de str>,
        concertDate: Option<Date>,
    },
}

impl<'de> Deserialize<'de> for Album<'de> {
    fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)> {
        let len = read_len(&raw)?;
        let di = raw[LEN_SIZE];
        let mut i = LEN_SIZE + 1;
        let album = match di {
            1 => {
                let (read, tracks) = <Vec<Song>>::deserialize_chained(&raw[i..])?;
                i += read;
                Album::StudioAlbum { tracks }
            },
            2 => {
                // See deserialize_chained for Song as an example of what this code would look like
                todo!()
            },
            _ => {
                i = len + LEN_SIZE;
                Album::Unknown
            }
        };
        Ok((i, album))
    }
}
