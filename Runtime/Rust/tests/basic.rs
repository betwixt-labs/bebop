//! Manual implementation which can be used for testing coherence and also for testing performance
//!
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
            d => Err(DeserializeError::InvalidEnumDiscriminator(d)),
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

impl<'raw> SubRecord<'raw> for Instrument {
    const MIN_SERIALIZED_SIZE: usize = ENUM_SIZE;

    #[inline]
    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        u32::from(*self).serialize(dest)
    }

    #[inline]
    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let (n, v) = u32::deserialize_chained(raw)?;
        Ok((n, v.try_into()?))
    }
}

impl<'raw> Record<'raw> for Instrument {}

/// Generated from `struct Performer`
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Performer<'raw> {
    pub name: &'raw str,
    pub plays: Instrument,
}

/// Generated from `struct Performer`
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct OwnedPerformer {
    pub name: String,
    pub plays: Instrument,
}

impl<'raw, 'ow: 'raw> From<&'ow OwnedPerformer> for Performer<'raw> {
    #[inline]
    fn from(owned: &'ow OwnedPerformer) -> Self {
        Self {
            name: &*owned.name,
            plays: owned.plays,
        }
    }
}

impl<'raw> From<Performer<'raw>> for OwnedPerformer {
    #[inline]
    fn from(borrowed: Performer<'raw>) -> Self {
        Self {
            name: borrowed.name.to_owned(),
            plays: borrowed.plays,
        }
    }
}

impl<'raw> SubRecord<'raw> for Performer<'raw> {
    const MIN_SERIALIZED_SIZE: usize =
        <&str>::MIN_SERIALIZED_SIZE + Instrument::MIN_SERIALIZED_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        Ok(self.name.serialize(dest)? + self.plays.serialize(dest)?)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
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

impl<'raw> Record<'raw> for Performer<'raw> {}

impl<'raw> SubRecord<'raw> for OwnedPerformer {
    const MIN_SERIALIZED_SIZE: usize = Performer::MIN_SERIALIZED_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        // basically the same code
        todo!()
    }

    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        // basically the same code
        todo!()
    }
}

impl<'raw> Record<'raw> for OwnedPerformer {}

/// Generated from `message Song`
#[derive(Clone, Debug, Eq, PartialEq, Default)]
pub struct Song<'raw> {
    /// Field 1
    pub title: Option<&'raw str>,
    /// Field 2
    pub year: Option<u16>,
    /// Field 3
    pub performers: Option<Vec<Performer<'raw>>>,
}

/// Generated from `message Song`
#[derive(Clone, Debug, Eq, PartialEq, Default)]
pub struct OwnedSong {
    pub title: Option<String>,
    pub year: Option<u16>,
    pub performers: Option<Vec<OwnedPerformer>>,
}

impl<'raw, 'ow: 'raw> From<&'ow OwnedSong> for Song<'raw> {
    #[inline]
    fn from(owned: &'ow OwnedSong) -> Self {
        Self {
            title: owned.title.as_ref().map(|v| v.as_str()),
            year: owned.year,
            performers: owned
                .performers
                .as_ref()
                .map(|v| v.iter().map(Into::into).collect()),
        }
    }
}

impl<'raw> From<Song<'raw>> for OwnedSong {
    #[inline]
    fn from(borrowed: Song<'raw>) -> Self {
        Self {
            title: borrowed.title.map(ToOwned::to_owned),
            year: borrowed.year,
            performers: borrowed
                .performers
                .map(|v| v.into_iter().map(Into::into).collect()),
        }
    }
}

impl<'raw> SubRecord<'raw> for Song<'raw> {
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

    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
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

impl<'raw> Record<'raw> for Song<'raw> {}

impl<'raw> SubRecord<'raw> for OwnedSong {
    const MIN_SERIALIZED_SIZE: usize = Song::MIN_SERIALIZED_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        // basically the same code
        todo!()
    }

    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        // basically the same code
        todo!()
    }
}

impl<'raw> Record<'raw> for OwnedSong {}

/// Generated from `union Album`
#[derive(Clone, Debug, Eq, PartialEq)]
pub enum Album<'raw> {
    Unknown,
    /// Generated from `struct Album::StudioAlbum`
    StudioAlbum {
        tracks: Vec<Song<'raw>>,
    },
    /// Generated from `message Album::LiveAlbum`
    LiveAlbum {
        tracks: Option<Vec<Song<'raw>>>,
        venue_name: Option<&'raw str>,
        concert_date: Option<Date>,
    },
}

/// Generated from `union Album`
/// TODO: each of these should actually be defined as structs globally with the enum variants
///   being a simple tuple struct
#[derive(Clone, Debug, Eq, PartialEq)]
pub enum OwnedAlbum {
    Unknown,
    /// Generated from `struct Album::StudioAlbum`
    StudioAlbum {
        tracks: Vec<OwnedSong>,
    },
    /// Generated from `message Album::LiveAlbum`
    LiveAlbum {
        tracks: Option<Vec<OwnedSong>>,
        venue_name: Option<String>,
        concert_date: Option<Date>,
    },
}

impl<'raw, 'ow: 'raw> From<&'ow OwnedAlbum> for Album<'raw> {
    #[inline]
    fn from(owned: &'ow OwnedAlbum) -> Self {
        match owned {
            OwnedAlbum::Unknown => Self::Unknown,
            OwnedAlbum::StudioAlbum { ref tracks } => Self::StudioAlbum {
                tracks: tracks.iter().map(Into::into).collect(),
            },
            OwnedAlbum::LiveAlbum {
                ref tracks,
                ref venue_name,
                ref concert_date,
            } => Self::LiveAlbum {
                tracks: tracks.as_ref().map(|v| v.iter().map(Into::into).collect()),
                venue_name: venue_name.as_ref().map(|v| v.as_str()),
                concert_date: *concert_date,
            },
        }
    }
}

impl<'raw> From<Album<'raw>> for OwnedAlbum {
    #[inline]
    fn from(borrowed: Album<'raw>) -> Self {
        match borrowed {
            Album::Unknown => Self::Unknown,
            Album::StudioAlbum { tracks } => Self::StudioAlbum {
                tracks: tracks.into_iter().map(Into::into).collect(),
            },
            Album::LiveAlbum {
                tracks,
                venue_name,
                concert_date,
            } => Self::LiveAlbum {
                tracks: tracks.map(|v| v.into_iter().map(Into::into).collect()),
                venue_name: venue_name.map(|v| v.to_owned()),
                concert_date,
            },
        }
    }
}

impl<'raw> SubRecord<'raw> for Album<'raw> {
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

    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(&raw)?;
        if raw.len() < len + LEN_SIZE {
            return Err(DeserializeError::MoreDataExpected(
                len + LEN_SIZE - raw.len(),
            ));
        }
        let mut i = LEN_SIZE + 1;
        let album = match raw[LEN_SIZE] {
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
        if !cfg!(feature = "unchecked") && i != len + LEN_SIZE {
            debug_assert!(i > len + LEN_SIZE);
            Err(DeserializeError::CorruptFrame)
        } else {
            Ok((i, album))
        }
    }
}

impl<'raw> Record<'raw> for Album<'raw> {}

impl<'raw> SubRecord<'raw> for OwnedAlbum {
    const MIN_SERIALIZED_SIZE: usize = Album::MIN_SERIALIZED_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        // basically the same code
        todo!()
    }

    fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        // basically the same code
        todo!()
    }
}

impl<'raw> Record<'raw> for OwnedAlbum {}
