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

use bebop::serialization::{
    read_len, write_len, DeResult, DeserializeError, SeResult, SerializeError, ENUM_SIZE, LEN_SIZE,
};
use bebop::*;
use std::convert::{TryFrom, TryInto};
use std::io::Write;

// Constants which are the same for all implementations

/// Generated from `const PianoKeys`
pub const PIANO_KEYS: i32 = 88;

/// Generated from `const ImportantProductID`
pub const IMPORTANT_PRODUCT_ID: Guid = Guid::from_be_bytes([
    0xa3, 0x62, 0x8e, 0xc7, 0x28, 0xd4, 0x45, 0x46, 0xad, 0x4a, 0xf6, 0xeb, 0xf5, 0x37, 0x5c, 0x96,
]);

/// Generated from `enum Instrument`
#[repr(u32)]
#[derive(Copy, Clone, Debug, Eq, PartialEq)]
pub enum Instrument {
    Sax = 0,
    Trumpet = 1,
    Clarinet = 2,
}

impl TryFrom<u32> for Instrument {
    type Error = DeserializeError;

    fn try_from(value: u32) -> DeResult<Self> {
        match value {
            0 => Ok(Instrument::Sax),
            1 => Ok(Instrument::Trumpet),
            2 => Ok(Instrument::Clarinet),
            _ => Err(DeserializeError::InvalidEnumDiscriminator),
        }
    }
}

impl From<Instrument> for u32 {
    fn from(value: Instrument) -> Self {
        match value {
            Instrument::Sax => 0,
            Instrument::Trumpet => 1,
            Instrument::Clarinet => 2,
        }
    }
}

impl<'de> Record<'de> for Instrument {
    const MIN_SERIALIZED_SIZE: usize = ENUM_SIZE;

    #[inline]
    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        u32::from(*self).serialize(dest)
    }

    #[inline]
    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let (n, v) = u32::deserialize_chained(raw)?;
        Ok((n, v.try_into()?))
    }
}

/// Generated from `struct Performer`
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Performer<'de> {
    pub name: &'de str,
    pub plays: Instrument,
}

impl<'de> Record<'de> for Performer<'de> {
    const MIN_SERIALIZED_SIZE: usize =
        <&str>::MIN_SERIALIZED_SIZE + Instrument::MIN_SERIALIZED_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        Ok(self.name.serialize(dest)? + self.plays.serialize(dest)?)
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            return Err(DeserializeError::MoreDataExpected(
                raw.len() - Self::MIN_SERIALIZED_SIZE,
            ));
        }
        let (read, name) = <&str>::deserialize_chained(raw)?;
        let mut i = read;
        let (read, plays) = Instrument::deserialize_chained(&raw[i..])?;
        i += read;
        Ok((i, Self { name, plays }))
    }
}

/// Generated from `message Song`
#[derive(Clone, Debug, Eq, PartialEq, Default)]
pub struct Song<'de> {
    /// Field 1
    pub title: Option<&'de str>,
    /// Field 2
    pub year: Option<u16>,
    /// Field 3
    pub performers: Option<Vec<Performer<'de>>>,
}

impl<'de> Record<'de> for Song<'de> {
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE + 1;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        let mut buf = Vec::new();
        if let Some(title) = self.title {
            buf.push(1);
            title.serialize(&mut buf)?;
        }
        if let Some(year) = self.year {
            buf.push(2);
            year.serialize(&mut buf)?;
        }
        if let Some(ref performers) = self.performers {
            buf.push(3);
            performers.serialize(&mut buf)?;
        }
        buf.push(0);
        write_len(dest, buf.len())?;
        dest.write_all(&buf)?;
        Ok(buf.len() + LEN_SIZE)
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;

        #[cfg(not(feature = "unchecked"))]
        if len == 0 {
            // a null message should still be length 1
            return Err(DeserializeError::CorruptFrame);
        }

        if raw.len() < len + LEN_SIZE {
            return Err(DeserializeError::MoreDataExpected(
                len + LEN_SIZE - raw.len(),
            ));
        }
        let mut i = LEN_SIZE;
        let mut song = Song::default();

        #[cfg(not(feature = "unchecked"))]
        let mut last = 0;

        while i < len + LEN_SIZE {
            let di = raw[i];

            #[cfg(not(feature = "unchecked"))]
            if di != 0 {
                // since fields should always be serialized in order, out of order fields implies
                // the frame is corrupted
                if di < last {
                    return Err(DeserializeError::CorruptFrame);
                }
                last = di;
            }

            i += 1;
            match di {
                0 => {
                    // Reached the end, in theory... Check performed after loop
                    break;
                }
                1 => {
                    #[cfg(not(feature = "unchecked"))]
                    if song.title.is_some() {
                        return Err(DeserializeError::DuplicateMessageField);
                    }
                    let (read, title) = <&str>::deserialize_chained(&raw[i..])?;
                    i += read;
                    song.title = Some(title);
                }
                2 => {
                    #[cfg(not(feature = "unchecked"))]
                    if song.year.is_some() {
                        return Err(DeserializeError::DuplicateMessageField);
                    }
                    let (read, year) = u16::deserialize_chained(&raw[i..])?;
                    i += read;
                    song.year = Some(year);
                }
                3 => {
                    #[cfg(not(feature = "unchecked"))]
                    if song.performers.is_some() {
                        return Err(DeserializeError::DuplicateMessageField);
                    }
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
            debug_assert!(i > len + LEN_SIZE);
            Err(DeserializeError::CorruptFrame)
        } else {
            Ok((i, song))
        }
    }
}

/// Generated from `union Album`
#[derive(Clone, Debug, Eq, PartialEq)]
pub enum Album<'de> {
    Unknown,
    /// Generated from `struct Album::StudioAlbum`
    StudioAlbum {
        tracks: Vec<Song<'de>>,
    },
    /// Generated from `message Album::LiveAlbum`
    LiveAlbum {
        tracks: Option<Vec<Song<'de>>>,
        venue_name: Option<&'de str>,
        concert_date: Option<Date>,
    },
}

impl<'de> Record<'de> for Album<'de> {
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE + 1;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        let mut buf = Vec::new();
        match self {
            Album::Unknown => {
                return Err(SerializeError::CannotSerializeUnknownUnion);
            }
            Album::StudioAlbum { tracks } => {
                buf.push(1);
                tracks.serialize(&mut buf)?;
            }
            Album::LiveAlbum {
                tracks,
                venue_name,
                concert_date,
            } => {
                buf.push(2);
                // see `serialize` for Song as an example of what this code would look like
            }
        }
        write_len(dest, buf.len())?;
        dest.write_all(&buf)?;
        Ok(buf.len() + LEN_SIZE)
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(&raw)?;
        if raw.len() < len + LEN_SIZE {
            return Err(DeserializeError::MoreDataExpected(
                len + LEN_SIZE - raw.len(),
            ));
        }
        let di = raw[LEN_SIZE];
        let mut i = LEN_SIZE + 1;
        let album = match di {
            1 => {
                let (read, tracks) = <Vec<Song>>::deserialize_chained(&raw[i..])?;
                i += read;
                Album::StudioAlbum { tracks }
            }
            2 => {
                // See `deserialize_chained` for Song as an example of what this code would look like
                todo!()
            }
            _ => {
                i = len + LEN_SIZE;
                Album::Unknown
            }
        };
        if i != len + LEN_SIZE {
            debug_assert!(i > len + LEN_SIZE);
            Err(DeserializeError::CorruptFrame)
        } else {
            Ok((i, album))
        }
    }
}
