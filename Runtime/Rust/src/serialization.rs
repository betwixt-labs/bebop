use std::io::{Write, Read};
use std::error::Error;
use std::fmt::{Display, Debug, Formatter};

pub enum DeserializeError {}

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


pub trait Serialize {
    fn serialize(&self, dest: &mut dyn Write);
}

pub trait Deserialize<'de>: Sized {
    fn deserialize(raw: &'de [u8]) -> Result<Self, DeserializeError>;
}
