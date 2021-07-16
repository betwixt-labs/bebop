use std::fmt::{self, Display, Formatter};
use std::ops::Deref;
use std::str::FromStr;

/// The Microsoft ordering for GUID bytes, where each GUID_MAPPING[i] is the ith byte if stored in
/// big endian format.
const BYTE_MAP: [usize; 16] = [3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15];

/// A GUID is a unique identifier. Stored internally in the Microsoft Guid format to support
/// zero-copy deserialization
#[derive(Eq, PartialEq, Hash, Clone, Copy, Debug)]
#[repr(transparent)]
pub struct Guid([u8; 16]);

impl Deref for Guid {
    type Target = [u8; 16];

    fn deref(&self) -> &Self::Target {
        &self.0
    }
}

impl Display for Guid {
    fn fmt(&self, f: &mut Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "{:02x}{:02x}{:02x}{:02x}-{:02x}{:02x}-{:02x}{:02x}-{:02x}{:02x}-{:02x}{:02x}{:02x}{:02x}{:02x}{:02x}",
            self.0[BYTE_MAP[0]],
            self.0[BYTE_MAP[1]],
            self.0[BYTE_MAP[2]],
            self.0[BYTE_MAP[3]],
            self.0[BYTE_MAP[4]],
            self.0[BYTE_MAP[5]],
            self.0[BYTE_MAP[6]],
            self.0[BYTE_MAP[7]],
            self.0[BYTE_MAP[8]],
            self.0[BYTE_MAP[9]],
            self.0[BYTE_MAP[10]],
            self.0[BYTE_MAP[11]],
            self.0[BYTE_MAP[12]],
            self.0[BYTE_MAP[13]],
            self.0[BYTE_MAP[14]],
            self.0[BYTE_MAP[15]],
        )
    }
}

impl FromStr for Guid {
    type Err = &'static str;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        match s.len() {
            36 => Self::from_str_with_hyphens(s),
            32 => Self::from_str_without_hyphens(s),
            _ => Err("Invalid length, not a GUID"),
        }
    }
}

const GUID_PARSE_ERR: &str = "Failed to parse GUID bytes";

impl Guid {
    /// Convert from a byte array ordered by
    /// https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-5.0#System_Guid_ToByteArray
    /// Will just read first 16 bytes
    pub const fn from_ms_bytes(raw: &[u8; 16]) -> Self {
        Self([
            raw[0], raw[1], raw[2], raw[3], raw[4], raw[5], raw[6], raw[7], raw[8], raw[9],
            raw[10], raw[11], raw[12], raw[13], raw[14], raw[15],
        ])
    }

    /// Mimic format produced by
    /// https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-5.0#System_Guid_ToByteArray
    pub const fn to_ms_bytes(self) -> [u8; 16] {
        self.0
    }

    fn from_str_without_hyphens(s: &str) -> Result<Self, &'static str> {
        let mut buf = [0u8; 16];
        // this is inefficient because of the unicode representation used by Rust but probably
        // fine for now
        for i in 0..16 {
            if let Ok(v) = u8::from_str_radix(&s[i * 2..i * 2 + 1], 16) {
                buf[i] = v
            } else {
                return Err(GUID_PARSE_ERR);
            }
        }
        Ok(Self::from_be_bytes(buf))
    }

    #[inline]
    fn from_str_with_hyphens(s: &str) -> Result<Self, &'static str> {
        // avoid extra copy
        let without_hyphens: String = s.split('-').collect();
        Self::from_str_without_hyphens(&without_hyphens)
    }

    pub const fn from_le_bytes(b: [u8; 16]) -> Self {
        Self([
            b[BYTE_MAP[15]],
            b[BYTE_MAP[14]],
            b[BYTE_MAP[13]],
            b[BYTE_MAP[12]],
            b[BYTE_MAP[11]],
            b[BYTE_MAP[10]],
            b[BYTE_MAP[9]],
            b[BYTE_MAP[8]],
            b[BYTE_MAP[7]],
            b[BYTE_MAP[6]],
            b[BYTE_MAP[5]],
            b[BYTE_MAP[4]],
            b[BYTE_MAP[3]],
            b[BYTE_MAP[2]],
            b[BYTE_MAP[1]],
            b[BYTE_MAP[0]],
        ])
    }

    /// Get the little endian bytes of this GUID.
    pub const fn to_le_bytes(self) -> [u8; 16] {
        [
            self.0[BYTE_MAP[15]],
            self.0[BYTE_MAP[14]],
            self.0[BYTE_MAP[13]],
            self.0[BYTE_MAP[12]],
            self.0[BYTE_MAP[11]],
            self.0[BYTE_MAP[10]],
            self.0[BYTE_MAP[9]],
            self.0[BYTE_MAP[8]],
            self.0[BYTE_MAP[7]],
            self.0[BYTE_MAP[6]],
            self.0[BYTE_MAP[5]],
            self.0[BYTE_MAP[4]],
            self.0[BYTE_MAP[3]],
            self.0[BYTE_MAP[2]],
            self.0[BYTE_MAP[1]],
            self.0[BYTE_MAP[0]],
        ]
    }

    pub const fn from_be_bytes(b: [u8; 16]) -> Self {
        Self([
            b[BYTE_MAP[0]],
            b[BYTE_MAP[1]],
            b[BYTE_MAP[2]],
            b[BYTE_MAP[3]],
            b[BYTE_MAP[4]],
            b[BYTE_MAP[5]],
            b[BYTE_MAP[6]],
            b[BYTE_MAP[7]],
            b[BYTE_MAP[8]],
            b[BYTE_MAP[9]],
            b[BYTE_MAP[10]],
            b[BYTE_MAP[11]],
            b[BYTE_MAP[12]],
            b[BYTE_MAP[13]],
            b[BYTE_MAP[14]],
            b[BYTE_MAP[15]],
        ])
    }

    /// Get the big endian bytes of this GUID.
    pub const fn to_be_bytes(self) -> [u8; 16] {
        [
            self.0[BYTE_MAP[0]],
            self.0[BYTE_MAP[1]],
            self.0[BYTE_MAP[2]],
            self.0[BYTE_MAP[3]],
            self.0[BYTE_MAP[4]],
            self.0[BYTE_MAP[5]],
            self.0[BYTE_MAP[6]],
            self.0[BYTE_MAP[7]],
            self.0[BYTE_MAP[8]],
            self.0[BYTE_MAP[9]],
            self.0[BYTE_MAP[10]],
            self.0[BYTE_MAP[11]],
            self.0[BYTE_MAP[12]],
            self.0[BYTE_MAP[13]],
            self.0[BYTE_MAP[14]],
            self.0[BYTE_MAP[15]],
        ]
    }
}
