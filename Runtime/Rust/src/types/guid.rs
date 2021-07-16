use std::ops::{Deref, DerefMut};
use std::fmt::{self, Display, Formatter};
use std::str::FromStr;

/// A GUID is a unique identifier. Stored internally as a u128 in Big Endian format.
#[derive(Eq, PartialEq, Hash, Clone, Copy, Debug)]
pub struct Guid(u128);

impl Deref for Guid {
    type Target = u128;

    fn deref(&self) -> &Self::Target {
        &self.0
    }
}

impl DerefMut for Guid {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut self.0
    }
}

impl Display for Guid {
    fn fmt(&self, f: &mut Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "{:08x}-{:04x}-{:04x}-{:04x}-{:012x}",
            (self.0 >> (128 - 8 * 4)) as u16,
            (self.0 >> (128 - 12 * 4)) as u8,
            (self.0 >> (128 - 16 * 4)) as u8,
            (self.0 >> (128 - 20 * 4)) as u8,
            self.0 as u64 & 0xFFFFFFFFFFFF
        )
    }
}

impl From<Guid> for Vec<u8> {
    fn from(guid: Guid) -> Self {
        guid.0.to_be_bytes().into()
    }
}

impl FromStr for Guid {
    type Err = std::num::ParseIntError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        todo!()
    }
}

macro_rules! get_byte {
    (($n:)) => {

    }
}

impl Guid {
    /// Mimic format produced by
    /// https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-5.0#System_Guid_ToByteArray
    pub const fn to_ms_bytes(self) -> [u8; 16] {
        [
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
        ]
    }

    /// Convert from a byte array ordered by
    /// https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-5.0#System_Guid_ToByteArray
    pub const fn from_ms_bytes(raw: &[u8]) -> Self {
        todo!()
    }

    pub const fn to_le_bytes(&self) -> [u8; 16] {
        self.0.to_le_bytes()
    }

    pub const fn to_be_bytes(&self) -> [u8; 16] {
        self.0.to_be_bytes()
    }
}
