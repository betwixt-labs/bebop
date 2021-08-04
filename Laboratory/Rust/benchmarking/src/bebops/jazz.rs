//!
//! This code was generated by a tool.
//!
//!
//!   bebopc version:
//!       0.0.1-20210803-2106
//!
//!
//!   bebopc source:
//!       https://github.com/RainwayApp/bebop
//!
//!
//! Changes to this file may cause incorrect behavior and will be lost if
//! the code is regenerated.
//!

#![allow(warnings)]

use ::std::io::Write as _;
use ::core::convert::TryInto as _;

#[repr(u32)]
#[derive(Copy, Clone, Debug, Eq, PartialEq)]
pub enum Instrument {
    Sax = 0,
    Trumpet = 1,
    Clarinet = 2,
    Piano = 3,
    Cello = 4,
}

impl ::core::convert::TryFrom<u32> for Instrument {
    type Error = ::bebop::DeserializeError;

    fn try_from(value: u32) -> ::bebop::DeResult<Self> {
        match value {
            0 => Ok(Instrument::Sax),
            1 => Ok(Instrument::Trumpet),
            2 => Ok(Instrument::Clarinet),
            3 => Ok(Instrument::Piano),
            4 => Ok(Instrument::Cello),
            d => Err(::bebop::DeserializeError::InvalidEnumDiscriminator(d)),
        }
    }
}

impl ::core::convert::From<Instrument> for u32 {
    fn from(value: Instrument) -> Self {
        match value {
            Instrument::Sax => 0,
            Instrument::Trumpet => 1,
            Instrument::Clarinet => 2,
            Instrument::Piano => 3,
            Instrument::Cello => 4,
        }
    }
}

impl<'raw> ::bebop::SubRecord<'raw> for Instrument {
    const MIN_SERIALIZED_SIZE: usize = ::bebop::ENUM_SIZE;

    #[inline]
    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        u32::from(*self)._serialize_chained(dest)
    }

    #[inline]
    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let (n, v) = u32::_deserialize_chained(raw)?;
        Ok((n, v.try_into()?))
    }
}


#[derive(Clone, Debug, PartialEq)]
pub struct Performer<'raw> {
    pub name: &'raw str,
    pub plays: Instrument,
}

impl<'raw> ::bebop::SubRecord<'raw> for Performer<'raw> {
    const MIN_SERIALIZED_SIZE: usize =
        <&'raw str>::MIN_SERIALIZED_SIZE +
        <Instrument>::MIN_SERIALIZED_SIZE;

    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        Ok(
            self.name._serialize_chained(dest)? +
            self.plays._serialize_chained(dest)?
        )
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let mut i = 0;
        if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
            let missing = Self::MIN_SERIALIZED_SIZE - (raw.len() - i);
            return Err(::bebop::DeserializeError::MoreDataExpected(missing));
        }

        let (read, v0) = <&'raw str>::_deserialize_chained(&raw[i..])?;
        i += read;
        let (read, v1) = <Instrument>::_deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            name: v0,
            plays: v1,
        }))
    }
}

impl<'raw> ::bebop::Record<'raw> for Performer<'raw> {}

#[derive(Clone, Debug, PartialEq, Default)]
pub struct Song<'raw> {
    /// Field 1
    pub title: ::core::option::Option<&'raw str>,
    /// Field 2
    pub year: ::core::option::Option<u16>,
    /// Field 3
    pub performers: ::core::option::Option<::std::vec::Vec<Performer<'raw>>>,
}

impl<'raw> ::bebop::SubRecord<'raw> for Song<'raw> {
    const MIN_SERIALIZED_SIZE: usize = ::bebop::LEN_SIZE + 1;
    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        let mut buf = ::std::vec::Vec::new();
        if let Some(ref v) = self.title {
            buf.push(1);
            v._serialize_chained(&mut buf)?;
        }
        if let Some(ref v) = self.year {
            buf.push(2);
            v._serialize_chained(&mut buf)?;
        }
        if let Some(ref v) = self.performers {
            buf.push(3);
            v._serialize_chained(&mut buf)?;
        }
        buf.push(0);
        ::bebop::write_len(dest, buf.len())?;
        dest.write_all(&buf)?;
        Ok(buf.len() + ::bebop::LEN_SIZE)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let mut i = 0;
        let len = ::bebop::read_len(&raw[i..])? + ::bebop::LEN_SIZE;
        i += ::bebop::LEN_SIZE;

        #[cfg(not(feature = "unchecked"))]
        if len == 0 {
            return Err(::bebop::DeserializeError::CorruptFrame);
        }

        if raw.len() < len {
            return Err(::bebop::DeserializeError::MoreDataExpected(len - raw.len()));
        }

        let mut _title = None;
        let mut _year = None;
        let mut _performers = None;

        #[cfg(not(feature = "unchecked"))]
        let mut last = 0;

        while i < len {
            let di = raw[i];

            #[cfg(not(feature = "unchecked"))]
            if di != 0 {
                if di < last {
                    return Err(::bebop::DeserializeError::CorruptFrame);
                }
                last = di;
            }

            i += 1;
            match di {
                0 => {
                    break;
                }
                1 => {
                    #[cfg(not(feature = "unchecked"))]
                    if _title.is_some() {
                        return Err(::bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <&'raw str>::_deserialize_chained(&raw[i..])?;
                    i += read;
                    _title = Some(value)
                }
                2 => {
                    #[cfg(not(feature = "unchecked"))]
                    if _year.is_some() {
                        return Err(::bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <u16>::_deserialize_chained(&raw[i..])?;
                    i += read;
                    _year = Some(value)
                }
                3 => {
                    #[cfg(not(feature = "unchecked"))]
                    if _performers.is_some() {
                        return Err(::bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <::std::vec::Vec<Performer<'raw>>>::_deserialize_chained(&raw[i..])?;
                    i += read;
                    _performers = Some(value)
                }
                _ => {
                    i = len;
                    break;
                }
            }
        }

        if i != len {
            debug_assert!(i > len);
            return Err(::bebop::DeserializeError::CorruptFrame)
        }

        Ok((i, Self {
            title: _title,
            year: _year,
            performers: _performers,
        }))
    }
}

impl<'raw> ::bebop::Record<'raw> for Song<'raw> {}

#[derive(Clone, Debug, PartialEq)]
pub struct StudioAlbum<'raw> {
    pub tracks: ::std::vec::Vec<Song<'raw>>,
}

impl<'raw> ::bebop::SubRecord<'raw> for StudioAlbum<'raw> {
    const MIN_SERIALIZED_SIZE: usize =
        <::std::vec::Vec<Song<'raw>>>::MIN_SERIALIZED_SIZE;

    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        Ok(
            self.tracks._serialize_chained(dest)?
        )
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let mut i = 0;
        if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
            let missing = Self::MIN_SERIALIZED_SIZE - (raw.len() - i);
            return Err(::bebop::DeserializeError::MoreDataExpected(missing));
        }

        let (read, v0) = <::std::vec::Vec<Song<'raw>>>::_deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            tracks: v0,
        }))
    }
}

impl<'raw> ::bebop::Record<'raw> for StudioAlbum<'raw> {}

#[derive(Clone, Debug, PartialEq, Default)]
pub struct LiveAlbum<'raw> {
    /// Field 1
    pub tracks: ::core::option::Option<::std::vec::Vec<Song<'raw>>>,
    /// Field 2
    pub venue_name: ::core::option::Option<&'raw str>,
    /// Field 3
    pub concert_date: ::core::option::Option<::bebop::Date>,
}

impl<'raw> ::bebop::SubRecord<'raw> for LiveAlbum<'raw> {
    const MIN_SERIALIZED_SIZE: usize = ::bebop::LEN_SIZE + 1;
    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        let mut buf = ::std::vec::Vec::new();
        if let Some(ref v) = self.tracks {
            buf.push(1);
            v._serialize_chained(&mut buf)?;
        }
        if let Some(ref v) = self.venue_name {
            buf.push(2);
            v._serialize_chained(&mut buf)?;
        }
        if let Some(ref v) = self.concert_date {
            buf.push(3);
            v._serialize_chained(&mut buf)?;
        }
        buf.push(0);
        ::bebop::write_len(dest, buf.len())?;
        dest.write_all(&buf)?;
        Ok(buf.len() + ::bebop::LEN_SIZE)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let mut i = 0;
        let len = ::bebop::read_len(&raw[i..])? + ::bebop::LEN_SIZE;
        i += ::bebop::LEN_SIZE;

        #[cfg(not(feature = "unchecked"))]
        if len == 0 {
            return Err(::bebop::DeserializeError::CorruptFrame);
        }

        if raw.len() < len {
            return Err(::bebop::DeserializeError::MoreDataExpected(len - raw.len()));
        }

        let mut _tracks = None;
        let mut _venue_name = None;
        let mut _concert_date = None;

        #[cfg(not(feature = "unchecked"))]
        let mut last = 0;

        while i < len {
            let di = raw[i];

            #[cfg(not(feature = "unchecked"))]
            if di != 0 {
                if di < last {
                    return Err(::bebop::DeserializeError::CorruptFrame);
                }
                last = di;
            }

            i += 1;
            match di {
                0 => {
                    break;
                }
                1 => {
                    #[cfg(not(feature = "unchecked"))]
                    if _tracks.is_some() {
                        return Err(::bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <::std::vec::Vec<Song<'raw>>>::_deserialize_chained(&raw[i..])?;
                    i += read;
                    _tracks = Some(value)
                }
                2 => {
                    #[cfg(not(feature = "unchecked"))]
                    if _venue_name.is_some() {
                        return Err(::bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <&'raw str>::_deserialize_chained(&raw[i..])?;
                    i += read;
                    _venue_name = Some(value)
                }
                3 => {
                    #[cfg(not(feature = "unchecked"))]
                    if _concert_date.is_some() {
                        return Err(::bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <::bebop::Date>::_deserialize_chained(&raw[i..])?;
                    i += read;
                    _concert_date = Some(value)
                }
                _ => {
                    i = len;
                    break;
                }
            }
        }

        if i != len {
            debug_assert!(i > len);
            return Err(::bebop::DeserializeError::CorruptFrame)
        }

        Ok((i, Self {
            tracks: _tracks,
            venue_name: _venue_name,
            concert_date: _concert_date,
        }))
    }
}

impl<'raw> ::bebop::Record<'raw> for LiveAlbum<'raw> {}

#[derive(Clone, Debug, PartialEq)]
pub enum Album<'raw> {
    /// An unknown type which is likely defined in a newer version of the schema.
    Unknown,

    /// Discriminator 1
    StudioAlbum {
        tracks: ::std::vec::Vec<Song<'raw>>,
    },

    /// Discriminator 2
    LiveAlbum {
        /// Field 1
        tracks: ::core::option::Option<::std::vec::Vec<Song<'raw>>>,
        /// Field 2
        venue_name: ::core::option::Option<&'raw str>,
        /// Field 3
        concert_date: ::core::option::Option<::bebop::Date>,
    },
}

impl<'raw> ::bebop::SubRecord<'raw> for Album<'raw> {
    const MIN_SERIALIZED_SIZE: usize = ::bebop::LEN_SIZE + 1;

    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        let mut buf = ::std::vec::Vec::new();
        match self {
            Album::Unknown => {
                return Err(::bebop::SerializeError::CannotSerializeUnknownUnion);
            }
            Album::StudioAlbum {
                tracks: ref _tracks,
            }
            => {
                buf.push(1);
                _tracks._serialize_chained(&mut buf);
            }
            Album::LiveAlbum {
                tracks: ref _tracks,
                venue_name: ref _venue_name,
                concert_date: ref _concert_date,
            }
            => {
                buf.push(2);
                let mut msgbuf = ::std::vec::Vec::new();
                if let Some(ref v) = _tracks {
                    msgbuf.push(1);
                    v._serialize_chained(&mut msgbuf)?;
                }
                if let Some(ref v) = _venue_name {
                    msgbuf.push(2);
                    v._serialize_chained(&mut msgbuf)?;
                }
                if let Some(ref v) = _concert_date {
                    msgbuf.push(3);
                    v._serialize_chained(&mut msgbuf)?;
                }
                msgbuf.push(0);
                ::bebop::write_len(&mut buf, msgbuf.len())?;
                &mut buf.write_all(&msgbuf)?;
            }
        }
        ::bebop::write_len(dest, buf.len() - 1);
        dest.write_all(&buf)?;
        Ok(buf.len() + ::bebop::LEN_SIZE)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let len = ::bebop::read_len(&raw)? + ::bebop::LEN_SIZE + 1;
        if raw.len() < len {
            return Err(::bebop::DeserializeError::MoreDataExpected(len - raw.len()));
        }
        let mut i = ::bebop::LEN_SIZE + 1;
        let de = match raw[::bebop::LEN_SIZE] {
            1 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = Self::MIN_SERIALIZED_SIZE - (raw.len() - i);
                    return Err(::bebop::DeserializeError::MoreDataExpected(missing));
                }

                let (read, v0) = <::std::vec::Vec<Song<'raw>>>::_deserialize_chained(&raw[i..])?;
                i += read;

                Album::StudioAlbum {
                    tracks: v0,
                }
            }
            2 => {
                let len = ::bebop::read_len(&raw[i..])? + i + ::bebop::LEN_SIZE;
                i += ::bebop::LEN_SIZE;

                #[cfg(not(feature = "unchecked"))]
                if len == 0 {
                    return Err(::bebop::DeserializeError::CorruptFrame);
                }

                if raw.len() < len {
                    return Err(::bebop::DeserializeError::MoreDataExpected(len - raw.len()));
                }

                let mut _tracks = None;
                let mut _venue_name = None;
                let mut _concert_date = None;

                #[cfg(not(feature = "unchecked"))]
                let mut last = 0;

                while i < len {
                    let di = raw[i];

                    #[cfg(not(feature = "unchecked"))]
                    if di != 0 {
                        if di < last {
                            return Err(::bebop::DeserializeError::CorruptFrame);
                        }
                        last = di;
                    }

                    i += 1;
                    match di {
                        0 => {
                            break;
                        }
                        1 => {
                            #[cfg(not(feature = "unchecked"))]
                            if _tracks.is_some() {
                                return Err(::bebop::DeserializeError::DuplicateMessageField);
                            }
                            let (read, value) = <::std::vec::Vec<Song<'raw>>>::_deserialize_chained(&raw[i..])?;
                            i += read;
                            _tracks = Some(value)
                        }
                        2 => {
                            #[cfg(not(feature = "unchecked"))]
                            if _venue_name.is_some() {
                                return Err(::bebop::DeserializeError::DuplicateMessageField);
                            }
                            let (read, value) = <&'raw str>::_deserialize_chained(&raw[i..])?;
                            i += read;
                            _venue_name = Some(value)
                        }
                        3 => {
                            #[cfg(not(feature = "unchecked"))]
                            if _concert_date.is_some() {
                                return Err(::bebop::DeserializeError::DuplicateMessageField);
                            }
                            let (read, value) = <::bebop::Date>::_deserialize_chained(&raw[i..])?;
                            i += read;
                            _concert_date = Some(value)
                        }
                        _ => {
                            i = len;
                            break;
                        }
                    }
                }

                if i != len {
                    debug_assert!(i > len);
                    return Err(::bebop::DeserializeError::CorruptFrame)
                }

                Album::LiveAlbum {
                    tracks: _tracks,
                    venue_name: _venue_name,
                    concert_date: _concert_date,
                }
            }
            _ => {
                i = len;
                Album::Unknown
            }
        };
        if !cfg!(feature = "unchecked") && i != len {
            debug_assert!(i > len);
            Err(::bebop::DeserializeError::CorruptFrame)
        }
        else {
            Ok((i, de))
        }
    }

}

impl<'raw> ::bebop::Record<'raw> for Album<'raw> {}

#[derive(Clone, Debug, PartialEq)]
pub struct Library<'raw> {
    pub albums: ::std::collections::HashMap<&'raw str, Album<'raw>>,
}

impl<'raw> ::bebop::SubRecord<'raw> for Library<'raw> {
    const MIN_SERIALIZED_SIZE: usize =
        <::std::collections::HashMap<&'raw str, Album<'raw>>>::MIN_SERIALIZED_SIZE;

    fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> ::bebop::SeResult<usize> {
        Ok(
            self.albums._serialize_chained(dest)?
        )
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> ::bebop::DeResult<(usize, Self)> {
        let mut i = 0;
        if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
            let missing = Self::MIN_SERIALIZED_SIZE - (raw.len() - i);
            return Err(::bebop::DeserializeError::MoreDataExpected(missing));
        }

        let (read, v0) = <::std::collections::HashMap<&'raw str, Album<'raw>>>::_deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            albums: v0,
        }))
    }
}

impl<'raw> ::bebop::Record<'raw> for Library<'raw> {}

