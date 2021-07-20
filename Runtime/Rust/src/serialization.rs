use crate::serialization::DeserializeError::MoreDataExpected;
use crate::{Date, Guid};
use std::collections::HashMap;
use std::convert::TryInto;
use std::error::Error;
use std::fmt::{Debug, Display, Formatter};
use std::hash::Hash;
use std::io::Write;

pub type Len = u32;
/// Size of length data
pub const LEN_SIZE: usize = core::mem::size_of::<Len>();
/// Size of an enum
pub const ENUM_SIZE: usize = 4;

pub type DeResult<T> = core::result::Result<T, DeserializeError>;
pub type SeResult<T> = core::result::Result<T, SerializeError>;

pub enum DeserializeError {
    /// Returns the number of additional bytes expected at a minimum.
    /// This will will never be an overestimate.
    MoreDataExpected(usize),
    /// The data seems to be invalid and cannot be deserialized.
    CorruptFrame,
    /// There was an issue with a string encoding
    Utf8EncodingError(std::str::Utf8Error),
    InvalidEnumDiscriminator,
}

impl From<std::str::Utf8Error> for DeserializeError {
    fn from(err: std::str::Utf8Error) -> Self {
        DeserializeError::Utf8EncodingError(err)
    }
}

impl Display for DeserializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "DeserializeError")
    }
}

impl Debug for DeserializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        (self as &dyn Display).fmt(f)
    }
}

impl Error for DeserializeError {}


pub enum SerializeError {

}

impl Display for SerializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "SerializeError")
    }
}

impl Debug for SerializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        (self as &dyn Display).fmt(f)
    }
}

impl Error for SerializeError {}

/// Bebop message type which can be serialized and deserialized.
pub trait Record<'de>: Sized {
    const MIN_SERIALIZED_SIZE: usize;

    // TODO: test performance of this versus a generic Write
    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize>;
    // fn serialize_dyn(&self, dest: &mut dyn Write) -> SeResult<usize>;

    /// Deserialize this as the root message
    #[inline(always)]
    fn deserialize(raw: &'de [u8]) -> DeResult<Self> {
        Ok(Self::deserialize_chained(raw)?.1)
    }

    /// Deserialize this object as a sub component of a larger message. Returns a tuple of
    /// (bytes_read, deserialized_value).
    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)>;
}

impl<'de> Record<'de> for &'de str {
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        todo!()
    }

    // fn serialize_dyn(&self, dest: &mut dyn Write) -> SeResult<usize> {
    //     todo!()
    // }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let raw_str = &raw[LEN_SIZE..len + LEN_SIZE];
        if raw_str.len() < len {
            return Err(MoreDataExpected(len - raw_str.len()));
        }
        #[cfg(not(feature = "unchecked"))]
        {
            Ok((len + LEN_SIZE, std::str::from_utf8(raw_str)?))
        }
        #[cfg(feature = "unchecked")]
        unsafe {
            Ok((len + LEN_SIZE, std::str::from_utf8_unchecked(raw_str)))
        }
    }
}

impl<'de, T> Record<'de> for Vec<T>
where
    T: Record<'de>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        todo!()
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut v = Vec::with_capacity(len);
        for _ in 0..len {
            let (read, t) = T::deserialize_chained(&raw[i..])?;
            i += read;
            v.push(t);
        }
        Ok((i, v))
    }
}

impl<'de, K, V> Record<'de> for HashMap<K, V>
where
    K: Record<'de> + Eq + Hash,
    V: Record<'de>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        todo!()
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut m = HashMap::with_capacity(len);
        for _ in 0..len {
            let (read, k) = K::deserialize_chained(&raw[i..])?;
            i += read;
            let (read, v) = V::deserialize_chained(&raw[i..])?;
            i += read;
            m.insert(k, v);
        }
        Ok((i, m))
    }
}

impl<'de> Record<'de> for Guid {
    const MIN_SERIALIZED_SIZE: usize = 16;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        todo!()
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        Ok((
            16,
            Guid::from_ms_bytes(
                raw[0..16]
                    .try_into()
                    .map_err(|_| DeserializeError::MoreDataExpected(16 - raw.len()))?,
            ),
        ))
    }
}

impl<'de> Record<'de> for Date {
    const MIN_SERIALIZED_SIZE: usize = 8;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        todo!()
    }

    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        let (read, date) = u64::deserialize_chained(&raw)?;
        Ok((read, Date::from_ticks(date)))
    }
}

impl<'de> Record<'de> for bool {
    const MIN_SERIALIZED_SIZE: usize = 1;

    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        todo!()
    }

    #[inline]
    fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        if let Some(&b) = raw.first() {
            Ok((1, b > 0))
        } else {
            Err(DeserializeError::MoreDataExpected(1))
        }
    }
}

macro_rules! impl_record_for_num {
    ($t:ty) => {
        impl<'de> Record<'de> for $t {
            const MIN_SERIALIZED_SIZE: usize = core::mem::size_of::<$t>();

            fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
                todo!()
            }

            #[inline]
            fn deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
                Ok((
                    core::mem::size_of::<$t>(),
                    <$t>::from_le_bytes(raw[0..core::mem::size_of::<$t>()].try_into().map_err(
                        |_| {
                            DeserializeError::MoreDataExpected(
                                core::mem::size_of::<$t>() - raw.len(),
                            )
                        },
                    )?),
                ))
            }
        }
    };
}

impl_record_for_num!(u8);
// no signed byte type at this time
impl_record_for_num!(u16);
impl_record_for_num!(i16);
impl_record_for_num!(u32);
impl_record_for_num!(i32);
impl_record_for_num!(f32);
impl_record_for_num!(u64);
impl_record_for_num!(i64);
impl_record_for_num!(f64);

/// Read a 4-byte length value from the front of the raw data.
///
/// This should only be called from within an auto-implemented deserialize function or for byte
/// hacking.
#[inline(always)]
pub fn read_len(raw: &[u8]) -> DeResult<usize> {
    Ok(Len::deserialize_chained(&raw)?.1 as usize)
}
