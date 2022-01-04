use std::error::Error;
use std::fmt::{Debug, Display, Formatter};
use std::io;

pub enum DeserializeError {
    /// Returns the number of additional bytes expected at a minimum.
    /// This will will never be an overestimate.
    MoreDataExpected(usize),
    /// The data seems to be invalid and cannot be deserialized.
    CorruptFrame,
    /// There was an issue with a string encoding
    Utf8EncodingError(std::str::Utf8Error),
    InvalidEnumDiscriminator(i128),
    /// A message type had multiple definitions for the same field
    DuplicateMessageField,
}

impl From<std::str::Utf8Error> for DeserializeError {
    fn from(err: std::str::Utf8Error) -> Self {
        DeserializeError::Utf8EncodingError(err)
    }
}

impl Display for DeserializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        match self {
            DeserializeError::MoreDataExpected(bytes) => {
                write!(f, "Deserialization Error, {} more bytes expected", bytes)
            }
            DeserializeError::CorruptFrame => {
                write!(f, "Deserialization Error, corrupt frame data")
            }
            DeserializeError::Utf8EncodingError(err) => {
                write!(f, "Deserialization Error, Utf-8 encoding error: {}", err)
            }
            DeserializeError::InvalidEnumDiscriminator(d) => {
                write!(f, "Deserialization Error, Invalid enum discriminator {}", d)
            }
            DeserializeError::DuplicateMessageField => write!(
                f,
                "Deserialization Error, duplicate message field encountered"
            ),
        }
    }
}

impl Debug for DeserializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        (self as &dyn Display).fmt(f)
    }
}

impl Error for DeserializeError {}

pub enum SerializeError {
    IoError(io::Error),
    LengthExceeds32Bits,
    CannotSerializeUnknownUnion,
}

impl From<io::Error> for SerializeError {
    fn from(err: io::Error) -> Self {
        SerializeError::IoError(err)
    }
}

impl Display for SerializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        match self {
            SerializeError::IoError(err) => write!(f, "Serialization Error, IO Error: {}", err),
            SerializeError::LengthExceeds32Bits => write!(
                f,
                "Serialization Error, length value does not fit within 32 bits"
            ),
            SerializeError::CannotSerializeUnknownUnion => {
                write!(f, "Serialization Error, cannot write unknown union value")
            }
        }
    }
}

impl Debug for SerializeError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        (self as &dyn Display).fmt(f)
    }
}

impl Error for SerializeError {}

pub type DeResult<T> = core::result::Result<T, DeserializeError>;
pub type SeResult<T> = core::result::Result<T, SerializeError>;
