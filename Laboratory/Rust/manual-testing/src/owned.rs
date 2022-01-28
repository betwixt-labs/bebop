use bebop::{Date, DeResult, Record, SeResult, SubRecord};
use std::collections::HashMap;
use std::io::Write;

// if it is a fixed-sized type, use same definition, skip defining the other parts as well
pub type Instrument = super::Instrument;

#[derive(Clone, Debug, PartialEq)]
pub struct Performer {
    pub name: String,
    pub plays: Instrument,
}

impl<'raw> From<super::Performer<'raw>> for Performer {
    fn from(value: crate::Performer) -> Self {
        Self {
            name: value.name.into(),
            plays: value.plays,
        }
    }
}

// Literally no reason anyone should ever do this since you could just do &Performer instead.
// impl<'ow> From<&'ow Performer> for super::Performer<'ow> {
//     fn from(value: &'ow Performer) -> Self {
//         Self {
//             name: &value.name,
//             plays: value.plays,
//         }
//     }
// }

// apparently this looks identical, so we can make a few changes like shown here to distinguish or
// generate the exact same code but with the correct types subbed in.
impl SubRecord<'_> for Performer {
    const MIN_SERIALIZED_SIZE: usize = super::Performer::MIN_SERIALIZED_SIZE;

    #[inline]
    fn serialized_size(&self) -> usize {
        self.name.serialized_size() +
        self.plays.serialized_size()
    }

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        Ok(
            self.name._serialize_chained(dest)? +
            self.plays._serialize_chained(dest)?
        )
    }

    fn _deserialize_chained(raw: &[u8]) -> DeResult<(usize, Self)> {
        let mut i = 0;
        if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
            let missing = Self::MIN_SERIALIZED_SIZE - (raw.len() - i);
            return Err(::bebop::DeserializeError::MoreDataExpected(missing));
        }

        let (read, v0) = <String>::_deserialize_chained(&raw[i..])?;
        i += read;
        let (read, v1) = <Instrument>::_deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            name: v0,
            plays: v1,
        }))
    }
}

impl Record<'_> for Performer {}

#[derive(Clone, Debug, PartialEq, Default)]
pub struct Song {
    pub title: Option<String>,
    pub year: Option<u16>,
    pub performers: Option<Vec<Performer>>,
}

impl<'raw> From<super::Song<'raw>> for Song {
    fn from(value: crate::Song<'raw>) -> Self {
        Self {
            title: value.title.map(Into::into),
            year: value.year,
            performers: value.performers.map(|value| value.into_iter().map(Into::into).collect()),
        }
    }
}

#[derive(Clone, Debug, PartialEq)]
pub enum Album {
    Unknown,
    StudioAlbum {
        tracks: Vec<Song>,
    },
    LiveAlbum {
        tracks: Option<Vec<Song>>,
        venue_name: Option<String>,
        concert_date: Option<Date>,
    },
}

impl<'raw> From<super::Album<'raw>> for Album {
    fn from(value: crate::Album<'raw>) -> Self {
        match value {
            super::Album::Unknown => Album::Unknown,
            super::Album::StudioAlbum { tracks } =>
                Album::StudioAlbum { tracks: tracks.into_iter().map(Into::into).collect() },
            super::Album::LiveAlbum { tracks, venue_name, concert_date } =>
                Album::LiveAlbum {
                    tracks: tracks.map(|value| value.into_iter().map(Into::into).collect()),
                    venue_name: venue_name.map(Into::into),
                    concert_date
                },
        }
    }
}

#[derive(Clone, Debug, PartialEq)]
pub struct Library {
    pub albums: HashMap<String, Album>,
}

impl<'raw> From<super::Library<'raw>> for Library {
    fn from(value: crate::Library<'raw>) -> Self {
        Self {
            albums: value.albums
                .into_iter()
                .map(|(key, value)| (key.into(), value.into()))
                .collect()
        }
    }
}

#[test]
fn ensure_ownership_works() {
    let p = {
        let v = vec![21,31,2,4,52];
        Performer::deserialize(v.as_ref())
    };
    drop(p)
}
