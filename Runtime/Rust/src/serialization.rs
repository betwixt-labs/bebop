use std::convert::TryInto;
use std::error::Error;
use std::fmt::{Debug, Display, Formatter};
use std::io::Write;

pub enum DeserializeError {
    /// Returns the number of additional bytes expected (at a minimum, may be returned on a subsequent call in special cases).
    MoreDataExpected(usize),
    /// The data seems to be invalid and cannot be deserialized.
    CorruptFrame,
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
pub trait Message<'de>: Sized {
    // TODO: test performance of this versus a generic Write
    fn serialize(&self, dest: &mut dyn Write);
    fn deserialize(raw: &'de [u8]) -> Result<Self, DeserializeError>;
}

/// Reads a message from a buffer and returns the deserialized value. The buffer does not have to be
/// exact sized, but it should contain the entire object.
///
/// Returns the body bytes (excluding the null byte).
///
/// This should only be called from within an auto-implemented deserialize function.
pub fn extract_body(r: &[u8]) -> Result<&[u8], DeserializeError> {
    let (len, body) = r.split_at(4);
    let len = u32::from_le_bytes(
        len.try_into()
            // fails if frame is too small to even contain the length
            .map_err(|_| DeserializeError::MoreDataExpected(5 - r.len()))?,
    ) as usize;
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
