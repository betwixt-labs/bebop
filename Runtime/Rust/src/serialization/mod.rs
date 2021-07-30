use std::collections::HashMap;
use std::convert::TryInto;
use std::hash::Hash;
use std::io::Write;

pub use error::*;

use crate::{Date, Guid, SliceWrapper};

pub mod error;

pub type Len = u32;
/// Size of length data
pub const LEN_SIZE: usize = core::mem::size_of::<Len>();
/// Size of an enum
pub const ENUM_SIZE: usize = 4;

/// Bebop message type which can be serialized and deserialized.
pub trait Record<'raw>: SubRecord<'raw> {
    const OPCODE: Option<u32> = None;

    /// Deserialize this record
    #[inline(always)]
    fn deserialize(raw: &'raw [u8]) -> DeResult<Self> {
        Ok(Self::_deserialize_chained(raw)?.1)
    }

    /// Serialize this record. It is highly recommend to use a buffered writer.
    #[inline(always)]
    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        self._serialize_chained(dest)
    }
}

/// Internal trait used to reduce the amount of code that needs to be generated.
pub trait SubRecord<'raw>: Sized {
    const MIN_SERIALIZED_SIZE: usize;

    // TODO: support async serialization
    // fn serialize_async<W: AsyncWrite>(&self, dest: &mut W) -> impl Future<Type=SeResult<usize>>;

    // TODO: test performance of this versus a generic Write
    /// Should only be called from generated code!
    /// Serialize this record. It is highly recommend to use a buffered writer.
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize>;

    /// Should only be called from generated code!
    /// Deserialize this object as a sub component of a larger message. Returns a tuple of
    /// (bytes_read, deserialized_value).
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)>;
}

pub trait Buildable {
    type Builder: Default + From<Self::Builder>;

    #[inline]
    fn builder() -> Self::Builder {
        Self::Builder::default()
    }
}

impl<'raw> SubRecord<'raw> for &'raw str {
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        let raw = self.as_bytes();
        write_len(dest, raw.len())?;
        dest.write_all(raw)?;
        Ok(LEN_SIZE + raw.len())
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let raw_str = &raw[LEN_SIZE..len + LEN_SIZE];
        if raw_str.len() < len {
            return Err(DeserializeError::MoreDataExpected(len - raw_str.len()));
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

impl<'raw, T> SubRecord<'raw> for Vec<T>
where
    T: SubRecord<'raw>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        write_len(dest, self.len())?;
        let mut i = LEN_SIZE;
        for v in self.iter() {
            i += v._serialize_chained(dest)?;
        }
        Ok(i)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut v = Vec::with_capacity(len);
        for _ in 0..len {
            let (read, t) = T::_deserialize_chained(&raw[i..])?;
            i += read;
            v.push(t);
        }
        Ok((i, v))
    }
}

impl<'raw, K, V> SubRecord<'raw> for HashMap<K, V>
where
    K: SubRecord<'raw> + Eq + Hash,
    V: SubRecord<'raw>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        write_len(dest, self.len())?;
        let mut i = LEN_SIZE;
        for (k, v) in self.iter() {
            i += k._serialize_chained(dest)?;
            i += v._serialize_chained(dest)?;
        }
        Ok(i)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut m = HashMap::with_capacity(len);
        for _ in 0..len {
            let (read, k) = K::_deserialize_chained(&raw[i..])?;
            i += read;
            let (read, v) = V::_deserialize_chained(&raw[i..])?;
            i += read;
            m.insert(k, v);
        }
        Ok((i, m))
    }
}

impl<'raw> SubRecord<'raw> for Guid {
    const MIN_SERIALIZED_SIZE: usize = 16;

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        dest.write_all(&self.to_ms_bytes())?;
        Ok(16)
    }

    #[inline]
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
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

impl<'raw> SubRecord<'raw> for Date {
    const MIN_SERIALIZED_SIZE: usize = 8;

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        self.to_ticks()._serialize_chained(dest)
    }

    #[inline]
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let (read, date) = u64::_deserialize_chained(&raw)?;
        Ok((read, Date::from_ticks(date)))
    }
}

impl<'de> SubRecord<'de> for bool {
    const MIN_SERIALIZED_SIZE: usize = 1;

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        dest.write_all(&[if *self { 1 } else { 0 }])?;
        Ok(1)
    }

    #[inline]
    fn _deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        if let Some(&b) = raw.first() {
            Ok((1, b > 0))
        } else {
            Err(DeserializeError::MoreDataExpected(1))
        }
    }
}

impl<'raw, T> SubRecord<'raw> for SliceWrapper<'raw, T>
where
    T: Sized + Copy + SubRecord<'raw>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        write_len(dest, self.len())?;
        match *self {
            SliceWrapper::Raw(raw) => {
                dest.write_all(&raw)?;
                Ok(raw.len() + LEN_SIZE)
            }
            SliceWrapper::Cooked(ary) => {
                #[cfg(target_endian = "big")]
                todo!();

                let b: &[u8] = unsafe {
                    std::slice::from_raw_parts(
                        ary.as_ptr() as *const u8,
                        ary.len() * core::mem::size_of::<T>(),
                    )
                };
                dest.write_all(b)?;
                Ok(b.len() + LEN_SIZE)
            }
        }
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let bytes = len * std::mem::size_of::<T>() + LEN_SIZE;
        if bytes > raw.len() {
            return Err(DeserializeError::MoreDataExpected(bytes - raw.len()));
        }
        Ok((
            bytes,
            if std::mem::align_of::<T>() == 1 {
                SliceWrapper::from_cooked(&unsafe {
                    std::slice::from_raw_parts((&raw[LEN_SIZE..bytes]).as_ptr() as *const T, len)
                })
            } else {
                SliceWrapper::from_raw(&raw[LEN_SIZE..bytes])
            },
        ))
    }
}

macro_rules! impl_record_for_num {
    ($t:ty) => {
        impl<'raw> SubRecord<'raw> for $t {
            const MIN_SERIALIZED_SIZE: usize = core::mem::size_of::<$t>();

            #[inline]
            fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
                dest.write_all(&self.to_le_bytes())?;
                Ok(core::mem::size_of::<$t>())
            }

            #[inline]
            fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
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
    Ok(Len::_deserialize_chained(&raw)?.1 as usize)
}

/// Write a 4-byte length value to the writer.
///
/// This should only be called from within an auto-implemented deserialize function or for byte
/// hacking.
#[inline(always)]
pub fn write_len<W: Write>(dest: &mut W, len: usize) -> SeResult<()> {
    if len > u32::MAX as usize {
        Err(SerializeError::LengthExceeds32Bits)
    } else {
        (len as u32)._serialize_chained(dest)?;
        Ok(())
    }
}
