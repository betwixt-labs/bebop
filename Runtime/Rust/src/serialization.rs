use std::convert::TryInto;
use std::error::Error;
use std::fmt::{Debug, Display, Formatter};
use std::io::Write;

pub type Result<T> = core::result::Result<T, DeserializeError>;
pub enum DeserializeError {
    /// Returns the number of additional bytes expected (at a minimum, may be returned on a subsequent call in special cases).
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

/// Bebop message type which can be serialized and deserialized.
pub trait Serialize: Sized {
    // TODO: test performance of this versus a generic Write
    fn serialize(&self, dest: &mut dyn Write);
}

pub trait Deserialize<'de>: Sized {
    /// Deserialize this as the root message
    #[inline(always)]
    fn deserialize(raw: &'de [u8]) -> Result<Self> {
        Ok(Self::deserialize_chained(extract_body(raw)?)?.1)
    }

    /// Deserialize this object as a sub component of a larger message. Returns a tuple of
    /// (bytes_read, deserialized_value).
    fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)>;
}

impl<'de> Deserialize<'de> for &'de str {
    fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)> {
        let len = read_len(raw)?;
        let raw_str = &raw[4..len + 4];
        #[cfg(not(feature = "unchecked"))]
        {
            Ok((len + 4, std::str::from_utf8(raw_str)?))
        }
        #[cfg(feature = "unchecked")]
        unsafe {
            Ok((len + 4, std::str::from_utf8_unchecked(raw_str)))
        }
    }
}

macro_rules! impl_deserialize_for_num {
    ($t:ty) => {
        impl<'de> Deserialize<'de> for $t {
            #[inline]
            fn deserialize_chained(raw: &'de [u8]) -> Result<(usize, Self)> {
                Ok((
                    core::mem::size_of::<$t>(),
                    <$t>::from_le_bytes(
                        raw[0..core::mem::size_of::<$t>()]
                            .try_into()
                            .map_err(|_| DeserializeError::CorruptFrame)?,
                    ),
                ))
            }
        }
    };
}

impl_deserialize_for_num!(u8);
// no signed byte type at this time
impl_deserialize_for_num!(u16);
impl_deserialize_for_num!(i16);
impl_deserialize_for_num!(u32);
impl_deserialize_for_num!(i32);
impl_deserialize_for_num!(f32);
impl_deserialize_for_num!(u64);
impl_deserialize_for_num!(i64);
impl_deserialize_for_num!(f64);

/// Reads a message from a buffer and returns the deserialized value. The buffer does not have to be
/// exact sized, but it should contain the entire object.
///
/// Returns the body bytes (excluding the null byte).
///
/// This should only be called from within an auto-implemented deserialize function or for byte
/// hacking.
pub fn extract_body(raw: &[u8]) -> Result<&[u8]> {
    let len = read_len(raw)?;
    let body = &raw[4..];
    debug_assert!(len > 0);
    if body.len() < len {
        // frame does not contain all the data
        return Err(DeserializeError::MoreDataExpected(len - body.len()));
    }

    if body[len - 1] != 0x00 {
        // this is probably not a real frame since the null byte is not there
        return Err(DeserializeError::CorruptFrame);
    }

    // return all of the body but the null byte
    Ok(&body[0..len - 1])
}

/// Read a 4-byte length value from the front of the raw data.
///
/// This should only be called from within an auto-implemented deserialize function or for byte
/// hacking.
#[inline(always)]
pub fn read_len(raw: &[u8]) -> Result<usize> {
    Ok(u32::deserialize_chained(&raw)?.1 as usize)
}
